#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class DoorSwing : IExternalCommand
    {
        private static bool isActive = false;
        private static UIApplication uiApp;
        private static List<ElementId> newDoorIds = new List<ElementId>();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                uiApp = commandData.Application;
                if (isActive)
                {
                    // Deactivate
                    uiApp.Application.DocumentChanged -= OnDocumentChanged;
                    uiApp.Idling -= OnIdling;
                    isActive = false;
                    TaskDialog.Show("Check Door Phase", "Door Swing Checker has been deactivated.");
                }
                else
                {
                    // Activate
                    uiApp.Application.DocumentChanged += OnDocumentChanged;
                    uiApp.Idling += OnIdling;
                    isActive = true;
                    TaskDialog.Show("Check Door Phase", "Door Swing Checker has been activated.");
                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void OnDocumentChanged(object sender, DocumentChangedEventArgs e)
        {
            Document doc = e.GetDocument();
            foreach (ElementId id in e.GetAddedElementIds())
            {
                Element element = doc.GetElement(id);
                if (element is FamilyInstance familyInstance && familyInstance.Symbol.Family.FamilyCategory.Name == "Doors")
                {
                    newDoorIds.Add(id);
                }
            }
        }

        private void OnIdling(object sender, IdlingEventArgs e)
        {

            if (newDoorIds.Count > 0)
            {
                Document doc = uiApp.ActiveUIDocument.Document;
                using (Transaction trans = new Transaction(doc, "Modify Door Swing"))
                {
                    trans.Start();
                    foreach (ElementId id in newDoorIds)
                    {
                        Element element = doc.GetElement(id);
                        if (element is FamilyInstance familyInstance)
                        {
                            Phase phaseCreated = doc.GetElement(familyInstance.CreatedPhaseId) as Phase;
                            Parameter parameter = familyInstance.LookupParameter("2D - Door Swing");
                            if (parameter != null)
                            {
                                double angleInRadians = (phaseCreated.Name == "Existing") ? 45 * (Math.PI / 180) : 90 * (Math.PI / 180);
                                parameter.Set(angleInRadians);
                            }
                        }
                    }
                    trans.Commit();
                }
                newDoorIds.Clear();
            }
        }

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}