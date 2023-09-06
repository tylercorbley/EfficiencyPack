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
                    string newName_2 = GetUniqueViewName(doc, viewName);

                    // Set the view name
                    using (Transaction trans = new Transaction(doc, "Rename View"))
                    {
                        trans.Start();
                        view.Name = newName_2;
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
        private string GetUniqueViewName(Document doc, string viewName)
        {
            string uniqueName = viewName;
            int counter = 1;

            while (IsViewNameExists(doc, uniqueName))
            {
                uniqueName = $"{viewName} {counter}";
                counter++;
            }

            return uniqueName;
        }
        private bool IsViewNameExists(Document doc, string viewName)
        {
            FilteredElementCollector viewCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(View));

            foreach (Element viewElem in viewCollector)
            {
                View view = viewElem as View;
                if (view != null && view.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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