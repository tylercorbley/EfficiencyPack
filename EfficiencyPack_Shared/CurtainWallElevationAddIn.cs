#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class CurtainWallElevationAddIn : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Get the selected curtain walls
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
                List<ElementId> curtainWallIds = new List<ElementId>();
                ElementId elementId = GetCurrentViewId(uiDoc);
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element is Wall && ((Wall)element).WallType.Kind == WallKind.Curtain)
                    {
                        curtainWallIds.Add(id);
                    }
                }
                ViewFamilyType viewTypeID = GetElevationViewTemplateByName(doc, "600 - Window Elevation");
                int counter = 0;
                // Create elevation views for each selected curtain wall
                using (Transaction trans = new Transaction(doc))
                {
                    trans.Start("Create Elevation Views");

                    foreach (ElementId curtainWallId in curtainWallIds)
                    {
                        string markString = GetWallMarkValue(doc, curtainWallId);
                        if (!IsViewNameExists(doc, markString))
                        {

                            Element curtainWall = doc.GetElement(curtainWallId);

                            // Get the location curve of the curtain wall
                            LocationCurve locationCurve = ((LocationCurve)curtainWall.Location);
                            Curve curve = locationCurve.Curve;

                            // Calculate midpoint of the curve
                            XYZ midpoint = (curve.GetEndPoint(0) + curve.GetEndPoint(1)) * 0.5;

                            // Calculate the direction vector
                            XYZ wallDirection = curve.GetEndPoint(1) - curve.GetEndPoint(0);

                            // Normalize the direction vector
                            XYZ normalizedDirection = wallDirection.Normalize();

                            // Check if the wall is flipped
                            bool isFlipped = ((Wall)curtainWall).Flipped;

                            // Offset distance from the midpoint
                            double offsetDistance = 5.0; // Adjust the offset distance as desired

                            // Determine if the wall is vertically or horizontally oriented in plan
                            bool isVertical = Math.Abs(normalizedDirection.X) < 0.1 && Math.Abs(normalizedDirection.Y) > 0.9;
                            bool isHorizontal = Math.Abs(normalizedDirection.X) > 0.9 && Math.Abs(normalizedDirection.Y) < 0.1;

                            // Calculate the perpendicular vector
                            XYZ perpendicularVector = new XYZ(-normalizedDirection.Y, normalizedDirection.X, 0.0).Normalize();

                            // Reverse the perpendicular vector if the wall is flipped
                            if (isFlipped)
                            {
                                perpendicularVector = -perpendicularVector;
                            }

                            // Offset the view position based on the orientation
                            XYZ offsetPosition = midpoint;
                            if (isVertical)
                            {
                                offsetPosition += perpendicularVector * offsetDistance;
                            }
                            else if (isHorizontal)
                            {
                                offsetPosition += perpendicularVector * -offsetDistance;
                            }

                            // Create an elevation view
                            XYZ upDirection = new XYZ(0, 0, 1); // Up direction for the elevation
                            XYZ viewDirection = normalizedDirection; // View direction for the elevation
                            XYZ viewPosition = offsetPosition; // Position of the elevation (offset position)


                            // Find the appropriate elevation view family type
                            ViewFamilyType elevationType = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault(x => x.ViewFamily == ViewFamily.Elevation);

                            if (elevationType != null)
                            {
                                // Create an elevation view
                                ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, viewTypeID.Id, viewPosition, 1);
                                int elevDir = 0;
                                //create 4 internal elevations for on each marker index
                                if (isHorizontal)
                                {
                                    if (isFlipped)
                                    {
                                        elevDir = 3;
                                    }
                                    else
                                    {
                                        elevDir = 1;
                                    }
                                }
                                else
                                {
                                    if (isFlipped)
                                    {
                                        elevDir = 2;
                                    }
                                    else
                                    {
                                        elevDir = 0;
                                    }
                                }
                                ViewSection elevationView = marker.CreateElevation(doc, elementId, elevDir);
                                // Set the view name as the room's name
                                string newName_2 = GetUniqueViewName(doc, markString);
                                elevationView.Name = newName_2;
                                counter++;
                                //ViewSection elevationView = ViewSection.CreateElevation(doc, elevationType.Id, curtainWallId, -1);
                                //elevationView.SetOrientation(new ViewOrientation3D(viewPosition, viewDirection, upDirection));
                            }
                        }
                    }

                    trans.Commit();
                }
                TaskDialog.Show("OK", $"You created {counter} Frame Elevations!");
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
        private string GetUniqueViewName(Document doc, string viewName)
        {
            string uniqueName = viewName;
            int counter = 1;

            while (IsViewNameExists(doc, uniqueName))
            {
                uniqueName = $"{viewName} {counter}";
                counter++;
            }

            return uniqueName;
        }
        private bool IsViewNameExists(Document doc, string viewName)
        {
            FilteredElementCollector viewCollector = new FilteredElementCollector(doc)
                .OfClass(typeof(View));

            foreach (Element viewElem in viewCollector)
            {
                View view = viewElem as View;
                if (view != null && view.Name.Equals(viewName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        private ElementId GetCurrentViewId(UIDocument uiDoc)
        {
            View activeView = uiDoc.ActiveView;
            if (activeView != null)
            {
                return activeView.Id;
            }

            return ElementId.InvalidElementId;
        }
        private ViewFamilyType GetElevationViewTemplateByName(Document doc, string templateName)
        {
            List<ViewFamilyType> elevationViewTemplates = GetElevationViewTemplates(doc);

            // Find the elevation view template with the matching name
            ViewFamilyType template = elevationViewTemplates
                .FirstOrDefault(vft => vft.Name.Equals(templateName, StringComparison.OrdinalIgnoreCase));

            return template;
        }
        private List<ViewFamilyType> GetElevationViewTemplates(Document doc)
        {
            // Get all view family types
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> viewFamilyTypes = collector
                .OfClass(typeof(ViewFamilyType))
                .WhereElementIsElementType()
                .ToList();

            // Filter elevation view templates
            List<ViewFamilyType> elevationViewTemplates = new List<ViewFamilyType>();

            foreach (Element element in viewFamilyTypes)
            {
                ViewFamilyType viewFamilyType = element as ViewFamilyType;
                if (viewFamilyType != null && viewFamilyType.ViewFamily == ViewFamily.Elevation)// && viewFamilyType.IsTemplate)
                {
                    elevationViewTemplates.Add(viewFamilyType);
                }
            }

            return elevationViewTemplates;
        }
        public string GetWallMarkValue(Document doc, ElementId wall)
        {
            // Get the document from the wall
            //Document document = wall.Document;

            // Retrieve the wall's element ID
            //ElementId wallId = wall.Id;

            // Retrieve the wall's element
            Element wallElement = doc.GetElement(wall);

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