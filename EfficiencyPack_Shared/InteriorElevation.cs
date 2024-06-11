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
    public class InteriorElevation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current Revit application and document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get the currently selected rooms
            List<Room> selectedRooms = GetSelectedRooms(uiDoc);

            if (selectedRooms.Count == 0)
            {
                TaskDialog.Show("No Rooms Selected", "Please select at least one room.");
                return Result.Cancelled;
            }
            ElementId elementId = GetCurrentViewId(uiDoc);

            List<string> viewTemplateNames = GetElevationViewTemplateNames(doc);
            viewTemplateNames.Sort();
            FrmIntElev formIntElev = new FrmIntElev(viewTemplateNames);
            formIntElev.Height = 200;
            formIntElev.Width = 500;
            formIntElev.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            int counter = 0;
            if (formIntElev.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string viewTypeName = formIntElev.GetSelectedViewType();
                ViewFamilyType viewTypeID = GetElevationViewTemplateByName(doc, viewTypeName);
                using (Transaction trans = new Transaction(doc, "Create Room Elevations"))
                {
                    try
                    {
                        trans.Start();

                        // Create elevations for each selected room
                        foreach (Room room in selectedRooms)
                        {
                            // Get the room's location point
                            LocationPoint roomLocation = room.Location as LocationPoint;
                            XYZ roomLocationPoint = roomLocation.Point;

                            // Get the level associated with the room
                            Level roomLevel = doc.GetElement(room.LevelId) as Level;

                            // Create a new elevation view for the room
                            //ViewFamilyType elevationType = new FilteredElementCollector(doc)
                            //    .OfClass(typeof(ViewFamilyType))
                            //    .Cast<ViewFamilyType>()
                            //    .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.Elevation);

                            // ViewSection elevationView = ViewSection.CreateElevation(doc, elevationType.Id, roomLocationPoint, roomLevel.Id);
                            //ElevationMarker elevMark = CreateElevationMarker();
                            ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, viewTypeID.Id, roomLocationPoint, 1);

                            //create 4 internal elevations for on each marker index
                            string roomname = GetRoomNameById(doc, room.Id);
                            for (int i = 0; i < 4; i++)
                            {
                                ViewSection elevationView = marker.CreateElevation(doc, elementId, i);
                                // Set the view name as the room's name
                                string letter = ToAlpha(i);
                                elevationView.Name = "IE - " + room.Number + " - " + roomname + " - " + letter;
                                counter++;
                            }
                            // Make the elevation view active
                            //uiDoc.ActiveView = elevationView;
                        }

                        trans.Commit();
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        TaskDialog.Show("Error", ex.Message);
                        return Result.Failed;
                    }
                }
            }
            TaskDialog.Show("OK", $"You created {counter} elevations!");
            return Result.Succeeded;
        }
        private ElementId GetCurrentViewId(UIDocument uiDoc)
        {
            View activeView = uiDoc.ActiveView;
            if (activeView != null)
            {
                return activeView.Id;
            }

            return ElementId.InvalidElementId;
        }
        private ViewFamilyType GetElevationViewTemplateByName(Document doc, string templateName)
        {
            List<ViewFamilyType> elevationViewTemplates = GetElevationViewTemplates(doc);

            // Find the elevation view template with the matching name
            ViewFamilyType template = elevationViewTemplates
                .FirstOrDefault(vft => vft.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));

            return template;
        }
        private List<string> GetElevationViewTemplateNames(Document doc)
        {
            List<ViewFamilyType> elevationViewTemplates = GetElevationViewTemplates(doc);

            // Get the names of the elevation view templates
            List<string> templateNames = elevationViewTemplates
                .Select(vft => vft.Name)
                .ToList();

            return templateNames;
        }
        private string GetRoomNameById(Document doc, ElementId roomId)
        {
            Room room = doc.GetElement(roomId) as Room;
            if (room != null)
            {
                Parameter parameter = room.get_Parameter(BuiltInParameter.ROOM_NAME);
                if (parameter != null && parameter.HasValue)
                {
                    string roomName = parameter.AsString();
                    return roomName;
                }
            }

            return string.Empty;
        }
        private static string ToAlpha(int i)
        {
            string result = "";
            do
            {
                result = (char)((i) % 26 + 'A') + result;
                i = (i - 1) / 26;
            } while (i > 0);
            return result;
        }
        private List<ViewFamilyType> GetElevationViewTemplates(Document doc)
        {
            // Get all view family types
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector
                .OfClass(typeof(ViewFamilyType))
                .WhereElementIsElementType()
                .ToList();

            // Filter elevation view templates
            List<ViewFamilyType> elevationViewTemplates = new List<ViewFamilyType>();

            foreach (Element element in viewFamilyTypes)
            {
                ViewFamilyType viewFamilyType = element as ViewFamilyType;
                if (viewFamilyType != null && viewFamilyType.ViewFamily == ViewFamily.Elevation)// && viewFamilyType.IsTemplate)
                {
                    elevationViewTemplates.Add(viewFamilyType);
                }
            }

            return elevationViewTemplates;
        }
        private List<Room> GetSelectedRooms(UIDocument uiDoc)
        {
            List<Room> selectedRooms = new List<Room>();

            // Get the selection from the active document
            ICollection<ElementId> selectionIds = uiDoc.Selection.GetElementIds();

            // Filter the selection to only rooms
            FilteredElementCollector collector = new FilteredElementCollector(uiDoc.Document, selectionIds);
            ICollection<Element> selectedElements = collector.OfClass(typeof(SpatialElement)).OfCategory(BuiltInCategory.OST_Rooms).ToElements();

            // Cast the selected elements to rooms
            selectedRooms = selectedElements.Cast<Room>().ToList();

            return selectedRooms;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}