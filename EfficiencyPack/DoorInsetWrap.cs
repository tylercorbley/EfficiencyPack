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
    public class DoorInsetWrap : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current Revit document
            Document doc = commandData.Application.ActiveUIDocument.Document;
            DoorFireRating doorFireRating = new DoorFireRating();
            // Get the selected doors
            ICollection<ElementId> selectedDoorIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            // Check if the door is a FamilyInstance
            // Collect host walls of selected doors
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Adjust Door Wrapping");
                foreach (ElementId doorId in selectedDoorIds)
                {
                    Element door = doc.GetElement(doorId);
                    if (door is FamilyInstance doorInstance)
                    {
                        // Retrieve the host wall of the door
                        Wall hostWall_1 = doorFireRating.GetDoorHostWall(doc, door);
                        //Check if the door has a valid host wall
                        ElementId hostId = hostWall_1.Id;
                        Element hostElement = doc.GetElement(hostId);
                        if (hostWall_1.Name.Contains("CMU") || hostWall_1.Name.Contains("Masonry"))
                        {
                            SetInsetParameter(doc, doorId, 1);
                        }
                        else
                        {
                            SetInsetParameter(doc, doorId, 0);
                        }
                    }
                    else
                    {
                    }
                }
                trans.Commit();
            }
            return Result.Succeeded;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        public static void SetInsetParameter(Document doc, ElementId doorId, int Value)
        {
            // Get the door element
            FamilyInstance door = doc.GetElement(doorId) as FamilyInstance;

            if (door != null)
            {
                // Get the parameter by name
                Parameter insetParam = door.LookupParameter("Inset");

                if (insetParam != null && insetParam.StorageType == StorageType.Integer)
                {
                    // Set the parameter value
                    insetParam.Set(Value);
                }
            }
        }
    }
}
