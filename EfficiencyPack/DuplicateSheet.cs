#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using View = Autodesk.Revit.DB.View;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class DuplicateViewsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Get the current sheet
            ViewSheet currentSheet = doc.ActiveView as ViewSheet;
            if (currentSheet == null)
            {
                TaskDialog.Show("Error", "Please select a sheet view.");
                return Result.Cancelled;
            }

            // Get the title block element from the current sheet
            FamilySymbol titleBlockSymbol = GetTitleBlockSymbol(currentSheet);
            if (titleBlockSymbol == null)
            {
                TaskDialog.Show("Error", "The current sheet does not have a valid title block.");
                return Result.Cancelled;
            }

            Element titleBlock = titleBlockSymbol.Family;
            //TaskDialog.Show("Title Block", $"Title Block Name: {titleBlock.Name}");

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Duplicate Views");
                // Create a new sheet
                ViewSheet newSheet = DuplicateSheet(currentSheet, titleBlockSymbol.Id);
                if (newSheet == null)
                {
                    TaskDialog.Show("Error", "Failed to duplicate the sheet.");
                    return Result.Failed;
                }
                // Collect all the views on the sheet
                List<View> viewsOnSheet = GetViewsOnSheet(currentSheet).ToList();
                View dependentView = null;
                ElementId newViewId = ElementId.InvalidElementId;
                foreach (View testView in viewsOnSheet)
                {
                    if (testView.CanViewBeDuplicated(ViewDuplicateOption.WithDetailing))
                    {
                        newViewId = testView.Duplicate(ViewDuplicateOption.WithDetailing);
                        dependentView = testView.Document.GetElement(newViewId) as View;
                        // Apply the view template from the original view to the duplicated view
                        ElementId viewTemplateId = GetViewTemplateId(doc, testView);
                        if (viewTemplateId != ElementId.InvalidElementId)
                        {
                            ApplyViewTemplate(doc, newViewId, viewTemplateId);
                        }

                        // Place the duplicated view on the new sheet
                        XYZ viewportPosition = GetViewportPosition(doc, currentSheet, testView.Id);
                        if (viewportPosition != null)
                        {
                            Viewport.Create(doc, newSheet.Id, newViewId, viewportPosition);
                        }
                    }
                }
                // Collect all schedules placed on the sheet
                List<ViewSchedule> schedules = CollectSchedules(currentSheet);
                foreach (ViewSchedule sched in schedules)
                {
                    // Place the duplicated view on the new sheet
                    XYZ viewportPosition = GetSchedulePosition(currentSheet, sched.Name);
                    if (viewportPosition != null)
                    {
                        ScheduleSheetInstance.Create(doc, newSheet.Id, sched.Id, viewportPosition);
                    }
                }
                trans.Commit();
            }

            return Result.Succeeded;
        }
        private XYZ GetSchedulePosition(ViewSheet sheet, string scheduleName)
        {
            Document doc = sheet.Document;

            // Collect all viewports on the sheet
            FilteredElementCollector viewportCollector = new FilteredElementCollector(doc, sheet.Id);
            viewportCollector.OfClass(typeof(ScheduleSheetInstance));
            var viewports = viewportCollector.ToElements();

            foreach (ScheduleSheetInstance viewport in viewports)
            {
                ElementId viewId = viewport.ScheduleId;
                View view = doc.GetElement(viewId) as View;

                if (view is ViewSchedule schedule && schedule.Name == scheduleName)
                {
                    return viewport.Point;
                }
            }

            return null;
        }
        private List<ViewSchedule> CollectSchedules(ViewSheet sheet)
        {
            Document doc = sheet.Document;

            List<ViewSchedule> schedules = new List<ViewSchedule>();
            // Collect all viewports on the sheet
            FilteredElementCollector viewportCollector = new FilteredElementCollector(doc, sheet.Id);
            viewportCollector.OfClass(typeof(ScheduleSheetInstance));
            var viewports = viewportCollector.ToElements();

            foreach (ScheduleSheetInstance sched in viewports)
            {
                ElementId schedId = sched.ScheduleId;
                View schedule_1 = doc.GetElement(schedId) as View;

                if (schedule_1 is ViewSchedule)
                {
                    if (!schedule_1.Name.Contains("Revision"))
                    {
                        schedules.Add(schedule_1 as ViewSchedule);
                    }
                }
            }

            return schedules;
        }
        private ViewSheet DuplicateSheet(ViewSheet sheet, ElementId titleBlock)
        {
            Document doc = sheet.Document;
            string sheetName = sheet.Name;

            // Check if the sheet name already exists
            ViewSheet existingSheet = GetSheetByName(doc, sheetName);
            if (existingSheet != null)
            {
                // If the sheet name exists, increment it numerically
                int sheetNumber = 1;
                string newSheetName = sheetName;
                while (GetSheetByName(doc, newSheetName) != null)
                {
                    sheetNumber++;
                    newSheetName = $"{sheetName}_{sheetNumber}";
                }
                sheetName = newSheetName;
            }

            ViewSheet newSheet = ViewSheet.Create(doc, titleBlock);
            newSheet.Name = sheetName;
            return newSheet;
        }
        private FamilySymbol GetTitleBlockSymbol(ViewSheet sheet)
        {
            Document doc = sheet.Document;

            // Get the title block category
            Category titleBlockCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_TitleBlocks);

            // Get all elements of the title block category on the sheet
            FilteredElementCollector collector = new FilteredElementCollector(doc, sheet.Id);
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
            collector.OfClass(typeof(FamilyInstance));

            FamilyInstance titleBlockInstance = collector.Cast<FamilyInstance>().FirstOrDefault();

            if (titleBlockInstance == null)
            {
                return null;
            }

            // Get the family symbol of the title block
            FamilySymbol titleBlockSymbol = doc.GetElement(titleBlockInstance.GetTypeId()) as FamilySymbol;

            return titleBlockSymbol;
        }
        private FamilyInstance GetTitleBlockInstance(ViewSheet sheet)
        {
            var collector = new FilteredElementCollector(sheet.Document, sheet.Id)
                .OfClass(typeof(FamilyInstance))
                .WhereElementIsNotElementType();

            foreach (FamilyInstance instance in collector)
            {
                if (instance.Symbol.Family.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks)
                {
                    return instance;
                }
            }

            return null;
        }
        // Helper method to retrieve viewports placed on a sheet
        private IEnumerable<Viewport> GetViewports(Document doc, ViewSheet sheet)
        {
            return new FilteredElementCollector(doc, sheet.Id)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>();
        }

        // Helper method to calculate the viewport position on the new sheet based on the position on the original sheet
        private XYZ GetViewportPosition(Document doc, ViewSheet sheet, ElementId viewId)
        {
            IEnumerable<Viewport> viewports = GetViewports(doc, sheet);
            Viewport originalViewport = viewports.FirstOrDefault(vp => vp.ViewId == viewId);
            if (originalViewport != null)
            {
                BoundingBoxXYZ boundingBox = originalViewport.get_BoundingBox(sheet);
                XYZ center = (boundingBox.Min + boundingBox.Max) * 0.5;
                return center;
            }
            return null;
        }

        // Helper method to retrieve the view template ID of a view
        private ElementId GetViewTemplateId(Document doc, Element view)
        {
            Parameter parameter = view.get_Parameter(BuiltInParameter.VIEW_TEMPLATE_FOR_SCHEDULE);
            if (parameter != null && parameter.HasValue)
            {
                return parameter.AsElementId();
            }
            return ElementId.InvalidElementId;
        }

        // Helper method to apply the view template to a view
        private void ApplyViewTemplate(Document doc, ElementId viewId, ElementId viewTemplateId)
        {
            View view = doc.GetElement(viewId) as View;
            if (view != null)
            {
                using (Transaction trans = new Transaction(doc))
                {
                    //trans.Start("Apply View Template");
                    view.ViewTemplateId = viewTemplateId;
                    //trans.Commit();
                }
            }
        }

        // Custom selection filter to filter out non-title block elements during selection
        private class TitleBlockSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                if (elem is FamilyInstance familyInstance && familyInstance.Symbol.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks)
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }

        // Helper method to generate a unique sheet name by appending a number if necessary
        private string GetUniqueSheetName(Document doc, string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;

            while (GetSheetByName(doc, uniqueName) != null)
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            return uniqueName;
        }

        // Helper method to retrieve a sheet by name
        private ViewSheet GetSheetByName(Document doc, string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(ViewSheet));
            return collector.Cast<ViewSheet>().FirstOrDefault(sheet => sheet.Name.Equals(name));
        }

        // Helper method to generate a unique view name by appending a number if necessary
        private string GetUniqueViewName(Document doc, string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;

            while (GetViewByName(doc, uniqueName) != null)
            {
                uniqueName = $"{baseName}_{counter}";
                counter++;
            }

            return uniqueName;
        }

        // Helper method to retrieve a view by name
        private Element GetViewByName(Document doc, string name)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(View));
            return collector.Cast<View>().FirstOrDefault(view => view.Name.Equals(name));
        }
        private IEnumerable<View> GetViewsOnSheet(ViewSheet sheet)
        {
            // Collect all the viewports on the sheet
            IEnumerable<ElementId> viewportIds = sheet.GetAllViewports();

            // Retrieve the corresponding views for each viewport
            foreach (ElementId viewportId in viewportIds)
            {
                Viewport viewport = sheet.Document.GetElement(viewportId) as Viewport;
                if (viewport != null)
                {
                    View view = sheet.Document.GetElement(viewport.ViewId) as View;
                    if (view != null)
                    {
                        yield return view;
                    }
                }
            }
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
