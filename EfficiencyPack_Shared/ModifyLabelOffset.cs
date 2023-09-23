#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ModifyLabelOffset : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Get the selected elements on the active view
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count == 0)
                {
                    message = "Please select at least one view.";
                    return Result.Failed;
                }

                using (Transaction trans = new Transaction(doc, "Change Label Offset"))
                {
                    trans.Start();

                    foreach (ElementId id in selectedIds)
                    {
                        Element element = doc.GetElement(id);
                        if (element is Viewport viewport)
                        {
                            // Get the view of the selected viewport
                            View view = doc.GetElement(viewport.ViewId) as View;

                            // Get the view name parameter
                            string viewName = view.Name;

                            // Get the view title on sheet parameter
                            Parameter titleOnSheetParam = viewport.get_Parameter(BuiltInParameter.VIEWPORT_VIEW_NAME);
                            string titleOnSheet = titleOnSheetParam.AsString();
                            double titleLength = 0;

                            // Calculate the length of the title
                            if (titleOnSheet == null)
                            {
                                titleLength = viewName.Length;
                            }
                            else
                            {
                                titleLength = titleOnSheet.Length;
                            }

                            viewport.LabelLineLength = titleLength * 3 / 32 / 12 + 3 / 32 / 12;

                        }
                    }

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
