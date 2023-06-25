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
    public class RoomTagCenteringCommandTag : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit application and document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Duplicate Views");
                // Get the current selection
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                // Iterate over each selected element
                foreach (ElementId selectedId in selectedIds)
                {
                    Element selectedElement = doc.GetElement(selectedId);

                    // Check if the selected element is a RoomTag
                    if (selectedElement is RoomTag roomTag)
                    {
                        // Get the associated Room
                        ElementId roomId = roomTag.Room.Id;

                        // If the room is from a linked model, get the actual ElementId
                        if (roomId is ElementId linkedElementId)
                        {
                            Element linkedElement = doc.GetElement(linkedElementId);
                            roomId = linkedElement.GetTypeId();
                        }

                        Room room = roomTag.Room;

                        if (room != null)
                        {
                            // Get the Room's location point
                            LocationPoint location = room.Location as LocationPoint;
                            XYZ roomPosition = location.Point;

                            // Move the Room Tag to the Room's location point
                            ElementTransformUtils.MoveElement(doc, roomTag.Id, roomPosition - roomTag.TagHeadPosition);
                        }
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