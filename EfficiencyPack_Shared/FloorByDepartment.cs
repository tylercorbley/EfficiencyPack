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
    public class FloorByDepartment : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
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
            List<Room> rooms = new List<Room>();
            foreach (ElementId id in selectedIds)
            {
                Element element = doc.GetElement(id);
                if (element is Room room)
                {
                    rooms.Add(room);
                }
            }

            // Get all floor types
            FilteredElementCollector floorTypeCollector = new FilteredElementCollector(doc);
            ICollection<Element> floorTypes = floorTypeCollector
                .OfClass(typeof(FloorType))
                .WhereElementIsElementType()
                .ToList();
            int counter = 0;
            double raiseHeight = .25 / 12;
            // Iterate through each room and create a floor
            using (Transaction trans = new Transaction(doc, "Place Floors"))
            {
                trans.Start();

                foreach (Room room in rooms)
                {
                    // Get the department parameter value of the room
                    string department = room.get_Parameter(BuiltInParameter.ROOM_DEPARTMENT).AsString();

                    // Find the floor type with matching keynote parameter
                    FloorType floorType = floorTypes
                        .Cast<FloorType>()
                        .FirstOrDefault(ft => ft.get_Parameter(BuiltInParameter.KEYNOTE_PARAM).AsString() == department);

                    if (floorType != null)
                    {
                        if (floorType.get_Parameter(BuiltInParameter.KEYNOTE_PARAM).AsString() != null)
                        {
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
                                Level level = doc.GetElement(room.LevelId) as Level;

                                // Create a new floor using the NewFloor method
                                Floor floor = Floor.Create(doc, allCurveLoops.ToList(), floorType.Id, level.Id);
                                RaiseFloor(floor, raiseHeight);
                                counter++;
                            }
                        }
                    }
                }

                trans.Commit();
            }
            TaskDialog.Show("OK!", $"You Created {counter} Floors!");

            return Result.Succeeded;
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