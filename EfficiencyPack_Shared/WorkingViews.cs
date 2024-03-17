using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class WorkingViews : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            //Document doc = uiDoc.Document;
            Document doc = uiApp.ActiveUIDocument.Document;

            try
            {
                // Step 1: Get all selected levels
                FilteredElementCollector levelCollector = new FilteredElementCollector(doc);
                ICollection<Element> selectedLevels = levelCollector.OfClass(typeof(Level)).ToElements();


                // Step 2: Get the user ID of the user
                string userId = Environment.UserName; // You can customize this as needed

                // Step 3: Create a new view template for the plans and RCP
                string planTemplateName = $"000 - {userId} - Working Floor Plan";
                string rcpTemplateName = $"000 - {userId} - Working Ceiling Plan";
                // Step 3: Retrieve ViewFamilyTypeIds for Plan and RCP views
                ElementId planViewFamilyTypeId = GetViewFamilyTypeId(doc, ViewFamily.FloorPlan);
                ElementId rcpViewFamilyTypeId = GetViewFamilyTypeId(doc, ViewFamily.CeilingPlan);

                if (planViewFamilyTypeId == ElementId.InvalidElementId || rcpViewFamilyTypeId == ElementId.InvalidElementId)
                {
                    // Handle the case where the ViewFamilyTypeIds are not found
                    TaskDialog.Show("Error", "Plan and/or RCP ViewFamilyType not found.");
                    return Result.Failed;
                }

                int counter = 1;
                using (Transaction transaction = new Transaction(doc, "Create Working Views"))
                {
                    transaction.Start();
                    //Step 4 and 5: Create plan and RCP views for each level and assign templates
                    foreach (Element levelElement in selectedLevels)
                    {
                        Level level = levelElement as Level;
                        int count = 1;
                        string planName = $"{userId} - W{counter:D2} - Working {level.Name} Plan";
                        string RCPName = $"{userId} - W{counter:D2} - Working {level.Name} Plan";
                        string folderName = $"000 - {userId} - Working";
                        while (ViewNameExists(doc, planName))
                        {
                            planName = $"{userId}_{count} - W{counter:D2} - Working {level.Name} Plan";
                            RCPName = $"{userId}_{count} - W{counter:D2} - Working {level.Name} Plan";
                            folderName = $"000 - {userId}_{count} - Working";
                            count++;
                        }

                        // Create Plan View
                        ViewPlan planView = ViewPlan.Create(doc, planViewFamilyTypeId, level.Id);
                        planView.Name = planName;

                        // Create RCP View
                        ViewPlan rcpView = ViewPlan.Create(doc, rcpViewFamilyTypeId, level.Id);
                        rcpView.Name = RCPName;

                        ElementId viewTemplate = FindViewTemplateByName(doc, "000 - Working Floor Plan");
                        // Reset view templates to "None"
                        //SetViewTemplate(planView, viewTemplate);
                        //planView.ViewTemplateId = viewTemplate;
                        //rcpView.ViewTemplateId = viewTemplate;
                        ResetViewTemplate(planView);
                        ResetViewTemplate(rcpView);

                        // Set custom parameter value for the RCP view
                        SetCustomParameter(planView, "Folder", folderName);
                        SetCustomParameter(rcpView, "Folder", folderName);

                        counter++;
                    }
                    transaction.Commit();
                    TaskDialog.Show("OK!", $"You Created {(counter - 1) * 2} Plans!");
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
        private ElementId GetViewFamilyTypeId(Document doc, ViewFamily viewFamily)
        {
            // Filter the view family types by view family
            var viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == viewFamily);

            return viewFamilyType != null ? viewFamilyType.Id : ElementId.InvalidElementId;
        }
        public void SetCustomParameter(View view, string parameterName, string parameterValue)
        {
            // Check if the view is valid
            if (view != null)
            {
                // Get the parameter by name
                Parameter parameter = view.LookupParameter(parameterName);

                // Check if the parameter exists
                if (parameter != null)
                {
                    // Set the parameter value as a string
                    parameter.Set(parameterValue);
                }
            }
        }
        public void ResetViewTemplate(View view)
        {
            if (view != null)
            {
                // Unset the view template by setting the ViewTemplateId to ElementId.InvalidElementId
                view.ViewTemplateId = ElementId.InvalidElementId;
            }
        }
        private string GetUniqueViewName(Document doc, string desiredName)
        {
            string uniqueName = desiredName;
            int count = 1;

            while (ViewNameExists(doc, uniqueName))
            {
                uniqueName = $"{desiredName}_{count}";
                count++;
            }

            return uniqueName;
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
        public void SetViewTemplate(View view, ElementId viewTemplateId)
        {
            // Check if the view template ID is valid
            if (viewTemplateId != ElementId.InvalidElementId)
            {
                // Set the view template for the view
                view.ViewTemplateId = viewTemplateId;

            }
            else
            {
                // If the view template ID is invalid, show a warning
                TaskDialog.Show("Warning", "Invalid View Template ID. View template not set.");
            }
        }
        private bool ViewNameExists(Document doc, string name)
        {
            // Check if a view with the given name already exists
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .WhereElementIsNotElementType()
                .Cast<View>()
                .Any(v => v.Name.Equals(name));
        }
    }
}
