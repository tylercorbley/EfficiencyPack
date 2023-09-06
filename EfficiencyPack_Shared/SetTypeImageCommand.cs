#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SetTypeImageCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the Revit document
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Check if the current document is a family document
            if (!doc.IsFamilyDocument)
            {
                TaskDialog.Show("Error", "This command can only be executed in a family document.");
                return Result.Failed;
            }
            string imageName = PromptForNewTypeImagePath();
            if (imageName != null)
            {
                using (Transaction transaction = new Transaction(doc, "Set Type Image Parameter"))
                {
                    transaction.Start();
                    SetTypeImageParameterForAllTypes(doc, imageName);
                    transaction.Commit();
                }
            }
            return Result.Succeeded;
        }
        public void SetTypeImageParameterForAllTypes(Document doc, string imageName)
        {
            FamilyManager familyManager = doc.FamilyManager;

            foreach (FamilyType type in familyManager.Types)
            {
                familyManager.CurrentType = type;
                SetTypeImageParameter(doc, imageName);
            }
        }
        public void SetTypeImageParameter(Document doc, string imageName)
        {
            FamilyManager familyManager = doc.FamilyManager;
            FamilyParameter parameter = familyManager.get_Parameter(BuiltInParameter.ALL_MODEL_TYPE_IMAGE);

            if (parameter != null && parameter.StorageType == StorageType.ElementId)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(ImageType));

                Element imageElement = collector.FirstOrDefault(e => e.Name.Equals(imageName));

                if (imageElement != null)
                {

                    familyManager.Set(parameter, imageElement.Id);

                }
                else
                {
                    TaskDialog.Show("Error", $"Image with name '{imageName}' not found in the Revit library.");
                }
            }
            else
            {
                TaskDialog.Show("Error", "The 'Type Image' parameter is not found or is of an incorrect type.");
            }
        }
        public ElementId GetImageElementIdByFileName(Document doc, string imageName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(ImageType));
            ICollection<ElementId> imageIds = collector.ToElementIds();

            foreach (ElementId imageId in imageIds)
            {
                Element element = doc.GetElement(imageId);
                Parameter parameter = element.LookupParameter("Image");
                if (parameter != null)
                {
                    string parameterValue = parameter.AsString();
                    if (parameterValue != null && parameterValue.Equals(imageName, StringComparison.OrdinalIgnoreCase))
                    {
                        return imageId;
                    }
                }
            }

            return ElementId.InvalidElementId;
        }
        private string PromptForNewTypeImagePath()
        {
            // Show an input dialog box
            string userInput = Interaction.InputBox("Enter the Type Image name (including extension)", "Change Type Image");

            // Check if user canceled or left the input blank
            if (string.IsNullOrEmpty(userInput) || userInput.ToLower() == "cancel")
            {
                return null; // Return null to indicate cancellation
            }

            // Return user input
            return userInput;
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
