using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ImportTypes : IExternalCommand
    {
        private UIApplication uiApp;
        private Document sourceDoc;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                uiApp = commandData.Application;
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
                    sourceDoc = uiApp.Application.OpenDocumentFile(sourceFilePath);
                    // Step 2: Get a list of all types from the selected Revit model
                    List<(string, string, string)> types = GetTypesFromRevitModel(sourceDoc);
                    List<(string, string, string)> activeTypes = GetTypesFromRevitModel(doc);
                    // Filter out items from the 'types' list that are already present in the 'activeTypes' list
                    List<(string, string, string)> filteredTypes = types
                        .Where(t => !activeTypes.Contains(t))
                        .ToList();
                    filteredTypes = filteredTypes.OrderBy(t => t.Item3, StringComparer.OrdinalIgnoreCase).ToList();
                    filteredTypes = filteredTypes.OrderBy(t => t.Item1, StringComparer.OrdinalIgnoreCase).ToList();

                    FrmImportTypes form = new FrmImportTypes(filteredTypes);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        List<(string, string, string)> selectedTypes = form.GetSelectedTypes();
                        // List<(string, string, string)> selectedTypes = ShowImportTypesForm(types);

                        // Start a transaction to import or load the selected types into the active model
                        using (Transaction transaction = new Transaction(doc, "Import Types"))
                        {
                            transaction.Start();

                            // Step 5: Import or load the selected types into the active model
                            ImportTypesIntoActiveModel(selectedTypes, sourceDoc, doc);

                            // Commit the transaction
                            transaction.Commit();
                        }

                        // Close the source document
                        sourceDoc.Close(false);
                        TaskDialog.Show("Success", $"{selectedTypes.Count} Types have been imported successfully.");

                    }
                    else
                    {
                        TaskDialog.Show("Info", "No types found to import.");
                    }
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        //private FamilySymbol GetTypeFromDocument(Document doc, string typeName, string familyName)
        //{
        //    // Get all elements of category OST_FamilySymbol (types) in the selected document
        //    FilteredElementCollector collector = new FilteredElementCollector(doc)
        //        //.OfCategory(BuiltInCategory.OST_FamilySymbols)
        //        .WhereElementIsElementType();

        //    // Iterate through the collected elements
        //    foreach (Element element in collector)
        //    {
        //        // Ensure the element is a FamilySymbol
        //        if (element is FamilySymbol familySymbol)
        //        {
        //            // Check if the Family and Type Name match the input parameters
        //            if (familySymbol.Family.Name == familyName && familySymbol.Name == typeName)
        //            {
        //                // Return the matching FamilySymbol
        //                return familySymbol;
        //            }
        //        }
        //    }

        //    // If no matching FamilySymbol is found, return null
        //    return null;
        //}
        private List<(string, string, string)> GetTypesFromRevitModel(Document doc)
        {
            HashSet<(string, string, string)> uniqueTypes = new HashSet<(string, string, string)>();

            // Get all categories in the document
            Categories categories = doc.Settings.Categories;
            foreach (Category category in categories)
            {

                // Get the built-in category corresponding to the current category
                BuiltInCategory builtInCategory = (BuiltInCategory)category.Id.IntegerValue;

                // Get all elements of the current category that are types
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfCategory(builtInCategory)
                    .WhereElementIsElementType();

                foreach (Element element in collector)
                {
                    // Skip categories that cannot have types
                    if (element.Category.Name.Contains("dwg") || element.Name.Contains("jpg") || element.Name.Contains("png") || element.Name.Contains("rvt"))
                    {
                    }
                    else
                    {
                        string systemFamilyType;
                        if (element is FamilySymbol familySymbol)
                        {
                            systemFamilyType = familySymbol.Family.Name;
                        }
                        else if (element is ElementType elementType)
                        {
                            systemFamilyType = elementType.FamilyName;
                        }
                        else
                        {
                            systemFamilyType = element.Category.Name;
                        }
                        string typeName = element.Name;
                        string familyName = element.Category.Name;

                        // Add the tuple to the set (automatically handles uniqueness)
                        uniqueTypes.Add((familyName, typeName, systemFamilyType));
                        // }
                    }
                }
            }

            // Convert the unique types set to a list and return it
            return uniqueTypes.ToList();
        }
        private void ImportTypesIntoActiveModel(List<(string, string, string)> types, Document sourceDoc, Document activeDoc)
        {
            // Iterate through the list of types
            foreach (var type in types)
            {
                string familyName = type.Item1;
                string typeName = type.Item2;
                string systemFamilyType = type.Item3;

                // Get the ElementIds of the types from the source document
                ICollection<ElementId> typeIds = GetTypeFromDocument(sourceDoc, typeName, familyName);

                // Copy the elements from the source document to the active document
                ICollection<ElementId> copiedIds = ElementTransformUtils.CopyElements(sourceDoc, typeIds, activeDoc, null, new CopyPasteOptions());

                // If needed, perform additional operations on the copied elements
            }
        }
        private ICollection<ElementId> GetTypeFromDocument(Document doc, string typeName, string familyName)
        {
            ICollection<ElementId> typeIds = new List<ElementId>();

            // Get all categories in the document
            Categories categories = doc.Settings.Categories;
            foreach (Category category in categories)
            {
                // Get the built-in category corresponding to the current category
                BuiltInCategory builtInCategory = (BuiltInCategory)category.Id.IntegerValue;

                // Get all elements of the current category that are types
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfCategory(builtInCategory)
                    .WhereElementIsElementType();

                foreach (Element element in collector)
                {
                    // Check if the Family and Type Name match the input parameters
                    if (element.Name == typeName && element.Category.Name == familyName)
                    {
                        // Add the ElementId of the matching FamilySymbol to the list
                        typeIds.Add(element.Id);
                    }
                }
            }

            return typeIds;
        }
        public static string GetMethod()
        {
            return MethodBase.GetCurrentMethod().DeclaringType?.FullName;
        }
    }
}
