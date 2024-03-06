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

                // Get the active document
                Document activeDoc = doc;

                // Get the list of view types to import from another Revit file
                List<ViewFamilyType> viewTypesToImport = GetViewTypesToImport(activeDoc);

                if (viewTypesToImport.Any())
                {
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

                        // Import view types from the source Revit file
                        ImportViewTypes(activeDoc, sourceDoc, viewTypesToImport);

                        TaskDialog.Show("Success", "View types have been imported successfully.");
                    }
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

        private List<ViewFamilyType> GetViewTypesToImport(Document doc)
        {
            List<ViewFamilyType> viewTypes = new List<ViewFamilyType>();

            // Get all view family types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector.OfClass(typeof(ViewFamilyType)).ToElements();

            foreach (Element elem in viewFamilyTypes)
            {
                ViewFamilyType viewType = elem as ViewFamilyType;

                // Exclude view types like schedules, legends, etc. based on their view family name
                if (viewType != null && !IsExcludedViewFamily(viewType))
                {
                    viewTypes.Add(viewType);
                }
            }

            return viewTypes;
        }

        private bool IsExcludedViewFamily(ViewFamilyType viewType)
        {
            // Define the list of excluded view family names
            List<string> excludedViewFamilies = new List<string>
            {
                "Schedule", // Example: Exclude schedules
                "Legend",   // Example: Exclude legends
                // Add more excluded view family names as needed
            };

            return excludedViewFamilies.Contains(viewType.ViewFamily.ToString());
        }

        private void ImportViewTypes(Document targetDoc, Document sourceDoc, List<ViewFamilyType> viewTypesToImport)
        {
            // Start a transaction in the target document
            using (Transaction transaction = new Transaction(targetDoc, "Import View Types"))
            {
                transaction.Start();

                // Import each view type from the source document to the target document
                foreach (ViewFamilyType viewType in viewTypesToImport)
                {
                    ElementType duplicatedType = viewType.Duplicate(viewType.Name) as ElementType;

                    // Get the Id of the duplicated element
                    ElementId newTypeId = duplicatedType.Id;

                    // Optionally, you can perform additional operations on the imported view type,
                    // such as renaming it, changing its settings, etc.

                    // For example, to rename the imported view type:
                    // ViewFamilyType newType = targetDoc.GetElement(newTypeId) as ViewFamilyType;
                    // newType.Name = "New View Type Name";
                }

                // Commit the transaction
                transaction.Commit();
            }
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
