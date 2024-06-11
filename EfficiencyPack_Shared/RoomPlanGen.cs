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
    public class RoomPlanGen : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get selected rooms
            List<Room> selectedRooms = GetSelectedRooms(uiDoc);

            if (selectedRooms.Count == 0)
            {
                TaskDialog.Show("Error", "No rooms selected.");
                return Result.Cancelled;
            }
            // Retrieve the list of view template names
            List<string> viewTemplateNames = GetAllViewFamilyTypeNames(doc);
            viewTemplateNames.Sort();
            // 8. get view family types
            FilteredElementCollector vftCollector = new FilteredElementCollector(doc);
            vftCollector.OfClass(typeof(ViewFamilyType));
            //ViewFamilyType fpVFT = null;
            //ViewFamilyType cpVFT = null;
            List<string> vftNames = new List<String>();
            foreach (ViewFamilyType curVFT in vftCollector)
            {
                vftNames.Add(curVFT.Name);
            }
            FrmRmPlan formRoomPlan = new FrmRmPlan(viewTemplateNames);
            formRoomPlan.Height = 200;
            formRoomPlan.Width = 500;
            formRoomPlan.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            int counter = 0;
            double offset = 2.0;
            //foreach (ViewFamilyType curVFT in vftCollector)
            //{
            //    if (curVFT.ViewFamily == ViewFamily.FloorPlan)
            //    {
            //        fpVFT = curVFT;
            //    }
            //    else if (curVFT.ViewFamily == ViewFamily.CeilingPlan)
            //    {
            //        cpVFT = curVFT;
            //    }
            //}
            if (formRoomPlan.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (Transaction transaction = new Transaction(doc, "Create Plans"))
                {
                    transaction.Start();
                    // Iterate through selected rooms
                    foreach (Room room in selectedRooms)
                    {
                        // Set the view template and view type for the plan view
                        string viewTypeName = formRoomPlan.GetSelectedViewType();
                        //bool planOrRCP = formRoomPlan.PlanOrRCP();
                        ViewFamilyType curVFT = GetViewFamilyTypeIdByName(doc, viewTypeName);
                        //ViewFamilyType curVFT = null;
                        //if (planOrRCP)
                        //{
                        //    curVFT = fpVFT;
                        //}
                        //else
                        //{
                        //    curVFT = cpVFT;
                        //}
                        // Create a plan view
                        ViewPlan planView = CreatePlanViewWithCrop(doc, room, offset, curVFT);
                        // Crop the plan view to the room boundary
                        //CropViewToRoom(doc, planView, room);

                        // Rename the plan view to match the room name and number
                        RenamePlanView(doc, planView, room);
                        counter++;
                    }
                    transaction.Commit();
                }
                TaskDialog.Show("OK!", $"You Created {counter} Plans!");
            }
            return Result.Succeeded;
        }
        private List<string> GetAllViewTemplateNames(Document doc)
        {
            List<string> viewTemplateNames = new List<string>();

            FilteredElementCollector viewTemplateCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(View));

            foreach (Element viewTemplateElem in viewTemplateCollector)
            {
                View viewTemplate = viewTemplateElem as View;
                if (viewTemplate != null && viewTemplate.IsTemplate)
                {
                    viewTemplateNames.Add(viewTemplate.Name);
                }
            }

            return viewTemplateNames;
        }
        private List<string> GetAllViewTypeNames()
        {
            List<string> viewTypeNames = Enum.GetNames(typeof(ViewType)).ToList();

            // Remove additional view type names that are not commonly used
            viewTypeNames.Remove("Internal"); // Internal view type
            viewTypeNames.Remove("ProjectBrowser"); // Project browser view type

            return viewTypeNames;
        }
        private List<string> GetAllViewFamilyTypeNames(Document doc)
        {
            List<string> viewFamilyTypeNames = new List<string>();

            FilteredElementCollector viewFamilyCollector = new FilteredElementCollector(doc);
            viewFamilyCollector.OfClass(typeof(ViewFamilyType));

            foreach (Element viewFamilyTypeElem in viewFamilyCollector)
            {
                ViewFamilyType viewFamilyType = viewFamilyTypeElem as ViewFamilyType;
                if (viewFamilyType != null && viewFamilyType.ViewFamily == ViewFamily.FloorPlan || viewFamilyType.ViewFamily == ViewFamily.CeilingPlan)
                {
                    viewFamilyTypeNames.Add(viewFamilyType.Name);
                }
            }

            return viewFamilyTypeNames;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        private List<Room> GetSelectedRooms(UIDocument uiDoc)
        {
            List<Room> selectedRooms = new List<Room>();

            // Get selected elements
            ICollection<ElementId> selectedElementIds = uiDoc.Selection.GetElementIds();

            foreach (ElementId elementId in selectedElementIds)
            {
                Element element = uiDoc.Document.GetElement(elementId);

                if (element is Room room)
                {
                    selectedRooms.Add(room);
                }
            }

            return selectedRooms;
        }
        private ViewPlan CreatePlanViewWithCrop(Document doc, Room room, double offset, ViewFamilyType curVFT)
        {
            // Create a new plan view
            ViewPlan planView = null;

            // Determine the appropriate view range for the room
            BoundingBoxXYZ boundingBox = room.get_BoundingBox(null);
            double minX = boundingBox.Min.X - offset;
            double minY = boundingBox.Min.Y - offset;
            double minZ = boundingBox.Min.Z - offset;
            double maxX = boundingBox.Max.X + offset;
            double maxY = boundingBox.Max.Y + offset;
            double maxZ = boundingBox.Max.Z + offset;

            // Get the element id for the floor plan view type
            FilteredElementCollector viewCollector = new FilteredElementCollector(doc);
            ElementId floorPlanTypeId = viewCollector.OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == ViewFamily.FloorPlan)?.Id;

            if (floorPlanTypeId != null)
            {
                planView = ViewPlan.Create(doc, curVFT.Id, room.LevelId);
                planView.Name = room.Name + " Plan";

                // Apply the view template to the plan view
                //planView.ViewTemplateId = viewTemplateId;

                // Set the crop box to the adjusted boundaries
                planView.CropBoxActive = true;
                planView.CropBoxVisible = false;
                planView.CropBox = new BoundingBoxXYZ()
                {
                    Min = new XYZ(minX, minY, minZ),
                    Max = new XYZ(maxX, maxY, maxZ)
                };
            }

            return planView;
        }
        private ViewFamilyType GetViewFamilyTypeIdByName(Document doc, string viewFamilyName)
        {
            FilteredElementCollector viewFamilyCollector = new FilteredElementCollector(doc);
            viewFamilyCollector.OfClass(typeof(ViewFamilyType));

            foreach (Element viewFamilyTypeElem in viewFamilyCollector)
            {
                ViewFamilyType viewFamilyType = viewFamilyTypeElem as ViewFamilyType;
                if (viewFamilyType != null && viewFamilyType.Name.Equals(viewFamilyName, StringComparison.OrdinalIgnoreCase))
                {
                    return viewFamilyType;
                }
            }

            return null;
        }
        private ElementId GetViewTemplateIdByName(Document doc, string viewTemplateName)
        {
            FilteredElementCollector viewTemplateCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType();
            // .Where(view => view.IsTemplate && !view.IsTemporaryViewModeEnabled());

            foreach (Element viewTemplateElem in viewTemplateCollector)
            {
                View viewTemplate = viewTemplateElem as View;
                if (viewTemplate != null && viewTemplate.Name.Equals(viewTemplateName, StringComparison.OrdinalIgnoreCase))
                {
                    return viewTemplate.Id;
                }
            }

            return null;
        }
        private void RenamePlanView(Document doc, ViewPlan planView, Room room)
        {
            // Rename the plan view to match the room name and number
            string roomname = GetRoomNameById(doc, room.Id);
            string newName = room.Level.Name + " - " + room.Number + " - " + roomname;
            string newName_2 = GetUniqueViewName(doc, newName);
            planView.Name = newName_2;
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
        private void SetViewTemplateByName(Document doc, View view, string templateName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<ElementId> viewTemplates = collector.OfClass(typeof(View)).ToElementIds();

            foreach (ElementId templateId in viewTemplates)
            {
                Element templateElement = doc.GetElement(templateId);

                if (templateElement.Name.Equals(templateName) && templateElement is View viewTemplate)
                {
                    view.ViewTemplateId = viewTemplate.Id;
                    return;
                }
            }

            TaskDialog.Show("Error", "View template not found: " + templateName);
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
}