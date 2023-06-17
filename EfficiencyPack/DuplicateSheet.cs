#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class DuplicateViewsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Get the active sheet
            ViewSheet activeSheet = doc.ActiveView as ViewSheet;
            if (activeSheet == null)
            {
                TaskDialog.Show("Error", "Please select a sheet view.");
                return Result.Cancelled;
            }

            // Select the title block
            Reference titleBlockRef = null;
            try
            {
                titleBlockRef = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element, new TitleBlockSelectionFilter(), "Select the Title Block");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            Element titleBlock = doc.GetElement(titleBlockRef.ElementId);
            if (!(titleBlock is FamilyInstance familyInstance))
            {
                TaskDialog.Show("Error", "The selected element is not a valid title block.");
                return Result.Cancelled;
            }

            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Duplicate Views");

                // Create a new sheet
                ViewSheet newSheet = ViewSheet.Create(doc, familyInstance.Symbol.Id);
                newSheet.Name = GetUniqueSheetName(doc, activeSheet.Name);

                // Duplicate each view on the active sheet
                foreach (ElementId viewId in activeSheet.GetAllPlacedViews())
                {
                    Element view = doc.GetElement(viewId);

                    if (view is ViewPlan || view is ViewSection)
                    {
                        // Duplicate the view
                        ElementId copiedViewId = ElementTransformUtils.CopyElement(doc, viewId, new XYZ(0, 0, 0)).FirstOrDefault();
                        if (copiedViewId != null)
                        {
                            Element copiedView = doc.GetElement(copiedViewId);
                            copiedView.Name = GetUniqueViewName(doc, view.Name);

                            // Apply the view template from the original view to the duplicated view
                            ElementId viewTemplateId = GetViewTemplateId(doc, view);
                            if (viewTemplateId != ElementId.InvalidElementId)
                            {
                                ApplyViewTemplate(doc, copiedViewId, viewTemplateId);
                            }

                            // Place the duplicated view on the new sheet
                            XYZ viewportPosition = GetViewportPosition(doc, activeSheet, viewId);
                            if (viewportPosition != null)
                            {
                                Viewport.Create(doc, newSheet.Id, copiedViewId, viewportPosition);
                            }
                        }
                    }
                }

                trans.Commit();
            }

            return Result.Succeeded;
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
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
