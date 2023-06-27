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
    public class DoorStorefrontMark : IExternalCommand
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
                        String mark = GetWallMarkValue(hostWall_1);
                        SetMark(doc, doorId, mark);

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
        public static void SetMark(Document doc, ElementId doorId, string Value)
        {
            // Get the door element
            FamilyInstance door = doc.GetElement(doorId) as FamilyInstance;

            if (door != null)
            {
                // Get the parameter by name
                Parameter insetParam = door.LookupParameter("Mark");

                if (insetParam != null && insetParam.StorageType == StorageType.String)
                {
                    // Set the parameter value
                    insetParam.Set(Value);
                }
            }
        }
        public string GetWallMarkValue(Wall wall)
        {
            // Get the document from the wall
            Document document = wall.Document;

            // Retrieve the wall's element ID
            ElementId wallId = wall.Id;

            // Retrieve the wall's element
            Element wallElement = document.GetElement(wallId);

            // Get the parameter by name ("Mark" in this case)
            Parameter markParameter = wallElement.LookupParameter("Mark");

            if (markParameter != null && markParameter.HasValue)
            {
                // Get the parameter value as a string
                string markValue = markParameter.AsString();
                return markValue;
            }

            // Return an empty string if the parameter is not found or has no value
            return string.Empty;
        }
    }
}
