#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class RoomTagCenteringCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get the current selection
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Duplicate Views");
                // Iterate over each selected element
                foreach (ElementId selectedId in selectedIds)
                {
                    Element selectedElement = doc.GetElement(selectedId);

                    // Check if the selected element is a RoomTag
                    if (selectedElement is RoomTag roomTag)
                    {
                        // Get the associated Room
                        Room room = doc.GetElement(roomTag.Room.Id) as Room;

                        // Get the Room's bounding box
                        BoundingBoxXYZ boundingBox = room.get_BoundingBox(null);

                        // Calculate the center point of the bounding box
                        XYZ centerPoint = (boundingBox.Max + boundingBox.Min) / 2;

                        // Move the Room to the center point
                        ElementTransformUtils.MoveElement(doc, room.Id, centerPoint - ((LocationPoint)room.Location).Point);

                        // Move the Room Tag to the Room's new, centered location
                        ElementTransformUtils.MoveElement(doc, roomTag.Id, centerPoint - ((LocationPoint)roomTag.Location).Point);
                    }
                }
                trans.Commit();
                return Result.Succeeded;
            }
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}