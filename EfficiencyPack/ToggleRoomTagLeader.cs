using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ToggleRoomTagLeader : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            //Document doc = uiDoc.Document;
            Document doc = uiApp.ActiveUIDocument.Document;

            try
            {
                // Get the currently selected room tags
                IList<ElementId> selectedElementIds = (IList<ElementId>)uiDoc.Selection.GetElementIds();
                if (selectedElementIds.Count == 0)
                {
                    TaskDialog.Show("Error", "Please select one or more room tags.");
                    return Result.Cancelled;
                }

                Transaction t = new Transaction(doc);
                t.Start("Toggle Room Tag Leader");

                foreach (ElementId selectedElementId in selectedElementIds)
                {
                    Element selectedElement = doc.GetElement(selectedElementId);
                    RoomTag selectedTag = selectedElement as RoomTag;

                    if (selectedTag != null)
                    {
                        bool leaderVisible = selectedTag.HasLeader;
                        selectedTag.HasLeader = !leaderVisible;
                    }
                }

                t.Commit();
                t.Dispose();

                TaskDialog.Show("Success", "Room tag leaders toggled successfully.");
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