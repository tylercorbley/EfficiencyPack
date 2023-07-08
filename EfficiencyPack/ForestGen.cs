#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ForestGen : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;
            // this is a variable for the current Revit model
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            // Get the array of curves (e.g., from user selection)
            List<Curve> curves = GetCurvesFromSelection(uidoc);

            // Join the curves into a closed polygon
            CurveLoop curveLoop = CreateClosedPolygon(curves);
            //3 curve data
            if (curveLoop == null)
            {
                TaskDialog.Show("Error", "Failed to create a closed polygon.");
                return Result.Failed;
            }

            // Specify the number of random points you want to create
            int numPoints = 10;

            //5 get tree family
            List<string> treeType = GetAllTreeTypes(doc);
            if (treeType.Count > 0)
            {

                FrmForestGen forestGen = new FrmForestGen(treeType);
                forestGen.Height = 250;
                forestGen.Width = 350;
                forestGen.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
                int counter = 0;
                if (forestGen.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedTreeType = forestGen.GetSelectedTreeType();
                    String family = GetFamilyOfElementTypeByName(doc, selectedTreeType);
                    numPoints = forestGen.HowManyTrees();

                    // Create random XYZ points within the polygon
                    List<XYZ> randomPoints = CreateRandomPointsWithinPolygon(curveLoop, numPoints);
                    //transaction
                    using (Transaction t = new Transaction(doc))
                    {
                        t.Start("Create Random Forest");
                        // Create point elements at the random XYZ positions
                        foreach (XYZ point in randomPoints)
                        {
                            FamilySymbol tree = GetFamilySymbolByName(doc, family, selectedTreeType);
                            tree.Activate();
                            FamilyInstance newTree = doc.Create.NewFamilyInstance(point, tree, StructuralType.NonStructural);
                            counter++;
                            //7 rotate trees randomly
                            RandomlyRotateElement(newTree, point);
                            //revit family. rotate (num degrees or something)

                        }
                        t.Commit();
                    }
                    TaskDialog.Show("OK!", $"You Created {counter} Trees!");
                }
            }
            else
            {
                TaskDialog.Show("OOPS!", $"No planting types in the model. Please add some first.");
            }

            return Result.Succeeded;
        }
        public void RandomlyRotateElement(Element element, XYZ point)
        {
            Document doc = element.Document;

            // Instantiate a Random object
            Random random = new Random();

            // Get the element's location point
            LocationPoint location = element.Location as LocationPoint;
            if (location == null)
            {
                return; // Skip if the element doesn't have a valid location
            }

            XYZ origin = location.Point;
            XYZ adder = new XYZ(0, 0, 10);

            // Generate a random angle in radians
            double angle = random.NextDouble() * 2 * Math.PI;

            // Generate a random rotation axis
            XYZ axis = new XYZ(random.NextDouble(), random.NextDouble(), random.NextDouble()).Normalize();

            // Create a line for rotation axis passing through the e    lement's location
            Line rotationAxis = Line.CreateBound(point, point + adder);

            // Apply the rotation to the element's location
            location.Rotate(rotationAxis, angle);

            doc.Regenerate();
        }
        public String GetFamilyOfElementTypeByName(Document doc, string typeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Element element = collector.OfClass(typeof(ElementType)).FirstOrDefault(e => e.Name == typeName);

            if (element is ElementType elementType)
            {
                return elementType.FamilyName;
            }

            return null;
        }
        private List<string> GetAllTreeTypes(Document doc)
        {
            List<String> plantingElementTypes = new List<String>();

            // Define the category of elements to filter
            BuiltInCategory category = BuiltInCategory.OST_Planting;

            // Create a filter for the specified category
            ElementCategoryFilter categoryFilter = new ElementCategoryFilter(category);

            // Create a filtered element collector with the category filter
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> elements = collector.WherePasses(categoryFilter).OfClass(typeof(ElementType)).ToElements();

            foreach (Element element in elements)
            {
                ElementType elementType = element as ElementType;
                if (elementType != null)
                {
                    plantingElementTypes.Add(elementType.Name);
                }
            }

            return plantingElementTypes;
        }
        internal FamilySymbol GetFamilySymbolByName(Document doc, string famName, string fsName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FamilySymbol));

            foreach (FamilySymbol fs in collector)
            {
                if (fs.Name == fsName && fs.FamilyName == famName)
                    return fs;
            }
            return null;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        private List<Curve> GetCurvesFromSelection(UIDocument uidoc)
        {
            // Retrieve the selected elements from the active view
            ICollection<ElementId> selectedElementIds = uidoc.Selection.GetElementIds();
            List<Curve> curves = new List<Curve>();

            // Iterate through the selected elements and find curves
            foreach (ElementId elementId in selectedElementIds)
            {
                Element element = uidoc.Document.GetElement(elementId);
                if (element is CurveElement curveElement)
                {
                    curves.Add(curveElement.GeometryCurve);
                }
            }

            return curves;
        }
        private CurveLoop CreateClosedPolygon(List<Curve> curves)
        {
            // Check if the curves form a closed loop
            CurveLoop curveLoop = CurveLoop.Create(curves);
            if (!curveLoop.IsOpen())
            {
                return curveLoop;
            }

            // If the loop is open, try to close it by finding the closest endpoints
            List<Curve> closedCurves = new List<Curve>(curves);
            bool foundClosedLoop = false;

            foreach (Curve curve in curves)
            {
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);

                foreach (Curve otherCurve in curves)
                {
                    if (curve.Equals(otherCurve))
                        continue;

                    XYZ otherStart = otherCurve.GetEndPoint(0);
                    XYZ otherEnd = otherCurve.GetEndPoint(1);

                    if (start.IsAlmostEqualTo(otherEnd))
                    {
                        closedCurves.Add(Line.CreateBound(otherEnd, start));
                        foundClosedLoop = true;
                        break;
                    }

                    if (end.IsAlmostEqualTo(otherStart))
                    {
                        closedCurves.Add(Line.CreateBound(end, otherStart));
                        foundClosedLoop = true;
                        break;
                    }
                }

                if (foundClosedLoop)
                    break;
            }

            return foundClosedLoop ? CurveLoop.Create(closedCurves) : null;
        }
        private List<XYZ> CreateRandomPointsWithinPolygon(CurveLoop polygon, int numPoints)
        {
            List<XYZ> randomPoints = new List<XYZ>();

            BoundingBoxXYZ boundingBox = null;
            bool isFirstCurve = true;

            foreach (Curve curve in polygon)
            {
                XYZ start = curve.GetEndPoint(0);
                XYZ end = curve.GetEndPoint(1);

                BoundingBoxXYZ curveBoundingBox = new BoundingBoxXYZ();
                curveBoundingBox.Min = new XYZ(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Min(start.Z, end.Z));
                curveBoundingBox.Max = new XYZ(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y), Math.Max(start.Z, end.Z));

                if (isFirstCurve)
                {
                    boundingBox = curveBoundingBox;
                    isFirstCurve = false;
                }
                else
                {
                    boundingBox.Min = new XYZ(Math.Min(boundingBox.Min.X, curveBoundingBox.Min.X),
                                              Math.Min(boundingBox.Min.Y, curveBoundingBox.Min.Y),
                                              Math.Min(boundingBox.Min.Z, curveBoundingBox.Min.Z));
                    boundingBox.Max = new XYZ(Math.Max(boundingBox.Max.X, curveBoundingBox.Max.X),
                                              Math.Max(boundingBox.Max.Y, curveBoundingBox.Max.Y),
                                              Math.Max(boundingBox.Max.Z, curveBoundingBox.Max.Z));
                }
            }

            Random random = new Random();
            int pointsCreated = 0;

            while (pointsCreated < numPoints)
            {
                XYZ randomPoint = GenerateRandomPointWithinBoundingBox(boundingBox, random);
                if (IsPointInsidePolygon(randomPoint, polygon))
                {
                    randomPoints.Add(randomPoint);
                    pointsCreated++;
                }
            }
            return randomPoints;
        }
        private XYZ GenerateRandomPointWithinBoundingBox(BoundingBoxXYZ boundingBox, Random random)
        {
            double minX = boundingBox.Min.X;
            double minY = boundingBox.Min.Y;
            double minZ = boundingBox.Min.Z;

            double maxX = boundingBox.Max.X;
            double maxY = boundingBox.Max.Y;
            double maxZ = boundingBox.Max.Z;

            double randomX = random.NextDouble() * (maxX - minX) + minX;
            double randomY = random.NextDouble() * (maxY - minY) + minY;
            double randomZ = random.NextDouble() * (maxZ - minZ) + minZ;

            return new XYZ(randomX, randomY, randomZ);
        }
        private bool IsPointInsidePolygon(XYZ point, CurveLoop polygon)
        {
            int intersectionCount = 0;
            Line ray = Line.CreateBound(point, new XYZ(point.X + 1000, point.Y, point.Z));

            foreach (Curve curve in polygon)
            {
                SetComparisonResult result = curve.Intersect(ray, out IntersectionResultArray results);
                if (result == SetComparisonResult.Overlap)
                {
                    foreach (IntersectionResult intersectResult in results)
                    {
                        if (intersectResult.XYZPoint.DistanceTo(point) > 1e-6)
                        {
                            intersectionCount++;
                        }
                    }
                }
            }
            return intersectionCount % 2 == 1;
        }
    }
}
