using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfficiencyPack
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ViewTypeImportTool : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Open a dialog to select the source Revit file
                var openFileDialog = new System.Windows.Forms.OpenFileDialog();
                openFileDialog.Filter = "Revit Files (*.rvt)|*.rvt";
                openFileDialog.Title = "Select Source Revit File";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string sourceFilePath = openFileDialog.FileName;
                    // Open the source Revit file
                    Document sourceDoc = uiApp.Application.OpenDocumentFile(sourceFilePath);

                    // Load view types from source document
                    List<ViewFamilyType> sourceViewTypes = LoadViewTypes(sourceDoc);

                    // Load view types from active document
                    List<ViewFamilyType> activeViewTypes = LoadViewTypes(doc);

                    // Print unique view types from source document
                    List<ViewFamilyType> uniqueViewTypes = GetUniqueViewTypes(sourceViewTypes, activeViewTypes);
                    int counter = 0;
                    // Create new view types in the active document based on the unique view types found in the source document
                    using (Transaction transaction = new Transaction(doc, "Import View Types"))
                    {
                        transaction.Start();
                        //((Autodesk.Revit.DB.ElementType)viewType_Type.Element).FamilyName
                        foreach (var viewType in uniqueViewTypes)
                        {
                            string viewType_Type = viewType.FamilyName;//family 
                            string viewTypeName = viewType.Name;
                            foreach (var viewTypeActive in activeViewTypes)
                            {
                                string viewTypeActive_Type = viewTypeActive.FamilyName; // Family 

                                if (viewType_Type == viewTypeActive_Type)
                                {
                                    ViewFamilyType newViewType = DuplicateViewFamilyType(doc, viewTypeActive, viewTypeName);
                                    newViewType.get_Parameter(BuiltInParameter.ELEVATN_TAG).Set(viewType.get_Parameter(BuiltInParameter.ELEVATN_TAG).AsElementId());
                                    newViewType.get_Parameter(BuiltInParameter.CALLOUT_TAG).Set(viewType.get_Parameter(BuiltInParameter.CALLOUT_TAG).AsElementId());
                                    newViewType.get_Parameter(BuiltInParameter.SECTION_TAG).Set(viewType.get_Parameter(BuiltInParameter.SECTION_TAG).AsElementId());
                                    newViewType.get_Parameter(BuiltInParameter.VIEWER_REFERENCE_LABEL_TEXT).Set(viewType.get_Parameter(BuiltInParameter.VIEWER_REFERENCE_LABEL_TEXT).AsValueString());//
                                    ElementId templateId = FindViewTemplateByName(doc, viewType.get_Parameter(BuiltInParameter.DEFAULT_VIEW_TEMPLATE).AsValueString());
                                    if (templateId != null)
                                    {
                                        newViewType.get_Parameter(BuiltInParameter.DEFAULT_VIEW_TEMPLATE).Set(templateId);//
                                    }
                                    newViewType.get_Parameter(BuiltInParameter.ASSIGN_TEMPLATE_ON_VIEW_CREATION).Set(1);//
                                    //newViewType.get_Parameter(BuiltInParameter.PLAN_VIEW_VIEW_DIR).Set(viewType.get_Parameter(BuiltInParameter.PLAN_VIEW_VIEW_DIR).AsValueString());
                                    counter++;
                                    break;
                                }

                            }
                        }

                        // Commit the transaction
                        transaction.Commit();
                    }

                    // Close the source document
                    sourceDoc.Close(false);

                    // Show a success message
                    TaskDialog.Show("Success", $"{counter} View types have been imported successfully.");
                }
                else
                {
                    TaskDialog.Show("Info", "No view types found to import.");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private List<ViewFamilyType> LoadViewTypes(Document doc)
        {
            List<ViewFamilyType> viewTypes = new List<ViewFamilyType>();

            // Get all view family types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector.OfClass(typeof(ViewFamilyType)).ToElements();

            foreach (Element elem in viewFamilyTypes)
            {
                ViewFamilyType viewType = elem as ViewFamilyType;
                if (viewType != null)
                {
                    viewTypes.Add(viewType);
                }
            }

            return viewTypes;
        }

        private List<ViewFamilyType> GetUniqueViewTypes(List<ViewFamilyType> sourceViewTypes, List<ViewFamilyType> activeViewTypes)
        {
            List<ViewFamilyType> uniqueViewTypes = new List<ViewFamilyType>();

            foreach (var viewType in sourceViewTypes)
            {
                // Check if the view type exists in the active document
                if (!activeViewTypes.Any(vt => vt.Name == viewType.Name))
                {
                    // Add unique view types to the list
                    uniqueViewTypes.Add(viewType);
                }
            }

            return uniqueViewTypes;
        }
        // Function to create a copy of an existing ViewFamilyType with a specific name
        public static ViewFamilyType DuplicateViewFamilyType(Document doc, ViewFamilyType sourceType, string newTypeName)
        {
            // Check if the newTypeName already exists
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector.OfClass(typeof(ViewFamilyType)).ToElements();

            foreach (Element elem in viewFamilyTypes)
            {
                ViewFamilyType existingType = elem as ViewFamilyType;
                if (existingType != null && existingType.Name == newTypeName)
                {
                    // Return null if a ViewFamilyType with the newTypeName already exists
                    return null;
                }
            }
            // Duplicate the sourceType
            ViewFamilyType copiedType = sourceType.Duplicate(newTypeName) as ViewFamilyType;

            // Return the copied ViewFamilyType
            return copiedType;
        }
        private ElementId FindViewTemplateByName(Document doc, string viewTemplateName)
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
        private ElementId FindViewElevationTagByName(Document doc, string ElevationTagName)
        {
            FilteredElementCollector viewTemplateCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType();

            foreach (Element viewTemplateElem in viewTemplateCollector)
            {
                View viewTemplate = viewTemplateElem as View;
                if (viewTemplate != null && viewTemplate.Name.Equals(ElevationTagName, StringComparison.OrdinalIgnoreCase))
                {
                    return viewTemplate.Id;
                }
            }

            return null;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
