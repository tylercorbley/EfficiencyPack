#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class WallsByRoom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Collect all the wall type names
                List<string> wallTypeNames = CollectWallTypeNames(doc);

                // Get the selection of rooms in the Revit document
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                // Filter the selected elements to get only rooms
                List<Room> selectedRooms = new List<Room>();
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element is Room room)
                    {
                        if (room.Area > 10)
                        {
                            selectedRooms.Add(room);
                        }
                    }
                }

                FrmWallRm formWallRoom = new FrmWallRm(wallTypeNames);
                formWallRoom.Height = 250;
                formWallRoom.Width = 550;
                formWallRoom.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                int counter = 0;
                if (formWallRoom.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedWallType = formWallRoom.GetSelectedWallType();
                    // Create floors based on the outline of each selected room
                    using (Transaction t = new Transaction(doc, "Create Walls"))
                    {
                        t.Start();

                        foreach (Room room in selectedRooms)
                        {
                            // Get the outline of the room
                            IList<IList<BoundarySegment>> roomBoundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                            if (roomBoundaries.Count > 0)
                            {
                                foreach (var boundary in roomBoundaries)
                                {
                                    List<Curve> curves = new List<Curve>();

                                    foreach (BoundarySegment segment in boundary)
                                    {
                                        Curve curve = segment.GetCurve();
                                        curves.Add(curve);
                                    }

                                    // Get the level of the room
                                    Level roomLevel = doc.GetElement(room.LevelId) as Level;

                                    // Select a wall type based on your criteria
                                    ElementId selectedWallTypeId = GetWallTypeIdByName(doc, selectedWallType);
                                    WallType wallType = doc.GetElement(selectedWallTypeId) as WallType;
                                    Parameter widthParam = wallType.get_Parameter(BuiltInParameter.WALL_ATTR_WIDTH_PARAM);
                                    double widthInFeet = widthParam.AsDouble();
                                    List<Curve> offsetCurveLoops = OffsetCurves(curves, widthInFeet / 2);

                                    // Create walls based on the room outline and wall type
                                    foreach (Curve curve in offsetCurveLoops)
                                    {
                                        Wall wall = Wall.Create(doc, curve, selectedWallTypeId, roomLevel.Id, formWallRoom.WallHeight(), 0.0, false, false);  // 10.0 is the height of the wall
                                        counter++;
                                    }
                                }
                            }
                        }

                        t.Commit();
                    }
                    TaskDialog.Show("OK!", $"You Created {counter} Walls!");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        public static List<Curve> OffsetCurves(List<Curve> curves, double offsetDistance)
        {
            List<Curve> offsetCurves = new List<Curve>();

            foreach (Curve curve in curves)
            {
                // Create a copy of the curve to avoid modifying the original
                Curve offsetCurve = curve.Clone() as Curve;

                // Get the curve's normal and compute the offset direction
                XYZ curveDirection = curve.ComputeDerivatives(0, true).BasisX.Normalize();
                XYZ curveNormal = XYZ.BasisZ; // Assuming curves are in the XY plane
                XYZ offsetDirection = curveNormal.CrossProduct(curveDirection).Normalize();

                // Offset the curve
                Transform offsetTransform = Transform.CreateTranslation(offsetDirection * offsetDistance);
                offsetCurve = offsetCurve.CreateTransformed(offsetTransform);

                offsetCurves.Add(offsetCurve);
            }

            return offsetCurves;
        }
        private List<string> CollectWallTypeNames(Document doc)
        {
            // Collect all the wall types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));
            List<string> wallTypeNames = collector.Cast<WallType>().Select(wt => wt.Name).ToList();

            return wallTypeNames;
        }
        private ElementId GetWallTypeIdByName(Document doc, string wallTypeName)
        {
            // Collect all the wall types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));

            foreach (WallType wallType in collector)
            {
                if (wallType.Name.Equals(wallTypeName))
                {
                    return wallType.Id;
                }
            }

            return null; // Wall type with the given name not found
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        // Helper method to join multiple CurveLoops
        private CurveLoop JoinCurveLoops(IList<CurveLoop> curveLoops)
        {
            if (curveLoops.Count == 0)
                return null;

            CurveLoop joinedCurveLoop = curveLoops[0];

            for (int i = 1; i < curveLoops.Count; i++)
            {
                foreach (Curve curve in curveLoops[i])
                {
                    joinedCurveLoop.Append(curve);
                }
            }

            return joinedCurveLoop;
        }
    }
}
