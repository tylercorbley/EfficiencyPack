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
    public class DoorFireRating : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current Revit document
            Document doc = commandData.Application.ActiveUIDocument.Document;

            // Get the selected doors
            ICollection<ElementId> selectedDoorIds = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            // Check if the door is a FamilyInstance
            // Collect host walls of selected doors
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Adjust Fire Rating");
                foreach (ElementId doorId in selectedDoorIds)
                {
                    Element door = doc.GetElement(doorId);
                    if (door is FamilyInstance doorInstance)
                    {
                        // Retrieve the host wall of the door
                        Wall hostWall_1 = GetDoorHostWall(doc, door);
                        //Check if the door has a valid host wall
                        ElementId hostId = hostWall_1.Id;
                        Element hostElement = doc.GetElement(hostId);
                        if (hostWall_1.Name.Contains("Exterior"))
                        {
                            if (GetFireRatingParameter(doc, hostId) == "1 HR")  // Do something if the host wall is 1 HR
                            {
                                SetFireRatingParameter(doc, doorId, "45 MIN.");
                            }
                            else if (GetFireRatingParameter(doc, hostId) == "2 HR") // Do something if the host wall is 2 HR
                            {
                                SetFireRatingParameter(doc, doorId, "90 MIN.");
                            }
                            else // Do something if the host wall is not rated
                            {
                                SetFireRatingParameter(doc, doorId, "-");
                            }
                        }
                        else
                        {
                            if (GetFireRatingParameter(doc, hostId) == "1 HR")  // Do something if the host wall is 1 HR
                            {
                                SetFireRatingParameter(doc, doorId, "20 MIN.");
                            }
                            else if (GetFireRatingParameter(doc, hostId) == "2 HR") // Do something if the host wall is 2 HR
                            {
                                SetFireRatingParameter(doc, doorId, "45 MIN.");
                            }
                            else // Do something if the host wall is not rated
                            {
                                SetFireRatingParameter(doc, doorId, "-");
                            }
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
        private Wall GetDoorHostWall(Document doc, Element door)
        {
            // Check if the door is a FamilyInstance
            if (door is FamilyInstance doorInstance)
            {
                // Get the door's host ID
                ElementId hostId = doorInstance.Host.Id;

                // Check if the host ID is valid and corresponds to a wall
                if (hostId != ElementId.InvalidElementId)
                {
                    Element hostElement = doc.GetElement(hostId);
                    if (hostElement is Wall hostWall)
                    {
                        return hostWall;
                    }
                }
            }

            return null;
        }
        public static string GetFireRatingParameter(Document doc, ElementId wallId)
        {
            // Get the wall element
            Wall wall = doc.GetElement(wallId) as Wall;
            WallType wallType = GetWallType(doc, wallId);
            String Param = GetTypeParameterByName(wallType, "Fire Rating");
            if (wall != null)
            {
                if (Param != null)
                {
                    // Get the parameter value as a string
                    return Param;
                }
            }

            return string.Empty;
        }
        public static WallType GetWallType(Document doc, ElementId wallId)
        {
            // Get the wall element
            Wall wall = doc.GetElement(wallId) as Wall;

            if (wall != null)
            {
                // Get the wall type
                ElementId typeId = wall.GetTypeId();
                WallType wallType = doc.GetElement(typeId) as WallType;

                return wallType;
            }

            return null;
        }
        public static string GetTypeParameterByName(WallType wallType, string paramName)
        {
            if (wallType != null)
            {
                // Get the parameter by name from the wall type
                Parameter param = wallType.LookupParameter(paramName);

                if (param != null && param.StorageType == StorageType.String)
                {
                    // Get the parameter value as a string
                    return param.AsString();
                }
            }

            return string.Empty;
        }
        public static void SetFireRatingParameter(Document doc, ElementId doorId, string fireRatingValue)
        {
            // Get the door element
            FamilyInstance door = doc.GetElement(doorId) as FamilyInstance;

            if (door != null)
            {
                // Get the parameter by name
                Parameter fireRatingParam = door.LookupParameter("Fire Rating");

                if (fireRatingParam != null && fireRatingParam.StorageType == StorageType.String)
                {
                    // Set the parameter value
                    fireRatingParam.Set(fireRatingValue);
                }
            }
        }
    }
}
