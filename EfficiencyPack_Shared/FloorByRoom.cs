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
    public class FloorByRoom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Collect all the floor type names
                List<string> floorTypeNames = CollectFloorTypeNames(doc);

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
                FrmFlrRm formFloorRoom = new FrmFlrRm(floorTypeNames);
                formFloorRoom.Height = 250;
                formFloorRoom.Width = 550;
                formFloorRoom.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                int counter = 0;
                if (formFloorRoom.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedFloorType = formFloorRoom.GetSelectedFloorType();
                    double raiseHeight = formFloorRoom.GetFloorHeight();
                    // Create floors based on the outline of each selected room
                    using (Transaction t = new Transaction(doc, "Create Floors"))
                    {
                        t.Start();

                        foreach (Room room in selectedRooms)
                        {
                            // Get the outline of the room
                            IList<IList<BoundarySegment>> roomBoundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                            if (roomBoundaries.Count > 0)
                            {
                                // Create a list to hold the CurveLoops for each room boundary
                                IList<CurveLoop> allCurveLoops = new List<CurveLoop>();

                                foreach (var boundary in roomBoundaries)
                                {
                                    // Create a list to hold the CurveLoops for the current room boundary
                                    IList<CurveLoop> curveLoops = new List<CurveLoop>();

                                    foreach (BoundarySegment segment in boundary)
                                    {
                                        Curve curve = segment.GetCurve();
                                        CurveLoop curveLoop = new CurveLoop();
                                        curveLoop.Append(curve);
                                        curveLoops.Add(curveLoop);
                                    }

                                    // Join the CurveLoops for the current room boundary
                                    CurveLoop joinedCurveLoop = JoinCurveLoops(curveLoops);

                                    if (joinedCurveLoop != null)
                                    {
                                        allCurveLoops.Add(joinedCurveLoop);
                                    }
                                }
                                // Get the level of the room
                                Level roomLevel = doc.GetElement(room.LevelId) as Level;

                                // Select a floor type based on your criteria
                                ElementId selectedFloorTypeId = GetFloorTypeIdByName(doc, selectedFloorType);

                                // Create floors based on the room outline and floor type
                                Floor floor = Floor.Create(doc, allCurveLoops.ToList(), selectedFloorTypeId, roomLevel.Id);
                                RaiseFloor(floor, raiseHeight);
                                counter++;
                            }
                        }

                        t.Commit();
                    }
                    TaskDialog.Show("OK!", $"You Created {counter} Floors!");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        private void RaiseFloor(Floor floor, double height)
        {
            Parameter heightOffsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);

            if (heightOffsetParam != null && heightOffsetParam.StorageType == StorageType.Double)
            {
                double currentHeightOffset = heightOffsetParam.AsDouble();
                double newHeightOffset = currentHeightOffset + height;

                heightOffsetParam.Set(newHeightOffset);
            }
        }

        private List<string> CollectFloorTypeNames(Document doc)
        {
            // Collect all the floor types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FloorType));
            List<string> floorTypeNames = collector.Cast<FloorType>().Select(ft => ft.Name).ToList();

            return floorTypeNames;
        }

        private ElementId GetFloorTypeIdByName(Document doc, string floorTypeName)
        {
            // Collect all the floor types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FloorType));

            foreach (FloorType floorType in collector)
            {
                if (floorType.Name.Equals(floorTypeName))
                {
                    return floorType.Id;
                }
            }

            return null; // Floor type with the given name not found
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
