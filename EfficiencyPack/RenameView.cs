#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Linq;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class RenameView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get the selected room element
            Reference roomRef = uiDoc.Selection.PickObject(ObjectType.Element, new RoomSelectionFilter());
            if (roomRef != null)
            {
                Element roomElement = doc.GetElement(roomRef);
                if (roomElement != null && roomElement is Room room)
                {
                    // Get the full room name
                    string fullRoomName = room.Name;

                    // Extract the room name (without the number)
                    string roomName = GetRoomName(fullRoomName);
                    // Get the current view
                    View view = doc.ActiveView;

                    // Generate the new view name
                    string viewName = $"{room.Level.Name} - {room.Number} - {roomName}";

                    // Set the view name
                    using (Transaction trans = new Transaction(doc, "Rename View"))
                    {
                        trans.Start();
                        view.Name = viewName;
                        trans.Commit();
                    }

                    TaskDialog.Show("Success", "View renamed successfully.");
                    return Result.Succeeded;
                }
                else
                {
                    TaskDialog.Show("Error", "Please select a valid room element.");
                    return Result.Failed;
                }
            }
            else
            {
                TaskDialog.Show("Error", "No room element selected.");
                return Result.Failed;
            }
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        private string GetRoomName(string fullRoomName)
        {
            string[] roomNameParts = fullRoomName.Split(' ');

            // Concatenate all parts of the room name, excluding the last part (room number)
            string roomName = string.Join(" ", roomNameParts.Take(roomNameParts.Length - 1));

            return roomName;
        }
    }
    // Filter class for room selection
    public class RoomSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Room;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}