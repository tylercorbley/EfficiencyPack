using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace EfficiencyPack
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ImportTypes : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
                try
                {
            
                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc.Document;
                    int counter = 0;

                      counter++;
                    // Step 1: Select a Revit model to import types from
                    string sourceFilePath = SelectRevitFile();
                    if (string.IsNullOrEmpty(sourceFilePath))
                        return Result.Cancelled;

                    // Step 2: Get a list of all types from the selected Revit model
                    List<RevitType> types = GetTypesFromRevitModel(sourceFilePath);

                    // Step 3: Show the form to select types
                    FrmTypeSelection form = new FrmTypeSelection(types);
                    DialogResult result = form.ShowDialog();
                    if (result != DialogResult.OK)
                        return Result.Cancelled;

                    // Step 4: Retrieve the selected types from the form
                    List<RevitType> selectedTypes = form.GetSelectedTypes();

                    // Step 5: Import or load the selected types into the active model
                    ImportTypesIntoActiveModel(selectedTypes);

            TaskDialog.Show("Success", $"{counter} Types have been imported successfully.");
                    return Result.Succeeded;
                else
                {
                    TaskDialog.Show("Info", "No types found to import.");
                }

                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    return Result.Failed;
                }

            }

        private string SelectRevitFile()
            {
                // Implement logic to open a file dialog and select a Revit model
                // Return the file path of the selected Revit model
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Revit Files (*.rvt)|*.rvt";
                openFileDialog.Title = "Select Revit Model to Import Types From";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
                return null;
            }

            private List<RevitType> GetTypesFromRevitModel(string filePath)
            {
                List<RevitType> types = new List<RevitType>();

                // Implement logic to open the Revit model, iterate through all types,
                // and retrieve their Family and Name properties
                // Populate the types list with the retrieved information
                // You may use the Revit API to access the types in the model

                return types;
            }

            private void ImportTypesIntoActiveModel(List<RevitType> types)
            {
                // Implement logic to import or load the selected types into the active model
                // You may use the Revit API to create or load the types in the model
            }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        }

        // Define a class to represent Revit types with Family and Name properties
        public class RevitType
        {
            public string Family { get; set; }
            public string Name { get; set; }

            public RevitType(string family, string name)
            {
                Family = family;
                Name = name;
            }
        }

        // Define a form for type selection (to be implemented)
        public class FrmTypeSelection : Form
        {
            // Implement the form to display the list of types and allow selection
            // Include methods to get the selected types
            // You can use DataGridView or ListBox to display the types
        }
    }
