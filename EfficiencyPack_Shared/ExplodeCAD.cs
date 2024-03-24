#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Line = Autodesk.Revit.DB.Line;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ExplodeCAD : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Get the selected element
                if (uiApp.ActiveUIDocument.Selection.GetElementIds().Count == 0)
                {
                    TaskDialog.Show("Selection Required", "Please select a CAD import element.");
                    return Result.Cancelled;
                }

                Element selectedElement = doc.GetElement(uiApp.ActiveUIDocument.Selection.GetElementIds().First());

                //Element selectedElement = doc.GetElement(selectedRef);
                List<string> lineStyles = GetAllLineStyleNames(doc);
                List<string> subcategoryNames = ExtractCADSubcategories(selectedElement);

                List<string> lineStylesReturn = new List<string>(); // Example list of line styles
                List<string> subcategoryNamesReturn = new List<string>(); // Example list of subcategory names
                FrmExplodeCAD formCAD = new FrmExplodeCAD(lineStyles, subcategoryNames);
                formCAD.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                if (formCAD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Retrieve the selected line styles paired with their corresponding layer names
                    Dictionary<string, string> selectedItems = formCAD.GetSelectedItemsFromComboBoxes();

                    if (selectedElement is ImportInstance cadInstance)
                    {
                        using (Transaction curves = new Transaction(doc))
                        {
                            curves.Start("Convert DWG to RVT");
                            foreach (var kvp in selectedItems)
                            {
                                string lineStyle = kvp.Value;
                                string layerName = kvp.Key;
                                if (lineStyle != "Skip")
                                {

                                    GraphicsStyle graphicsStyle = GetGraphicsStyleByName(doc, lineStyle);
                                    ImportInstance importInst = (ImportInstance)selectedElement;


                                    Options op = commandData.Application.ActiveUIDocument.Document.Application.Create.NewGeometryOptions();
                                    op.ComputeReferences = true;
                                    op.IncludeNonVisibleObjects = false;
                                    GeometryElement geoElem1 = importInst.get_Geometry(op);
                                    if (geoElem1 != null)
                                    {
                                        foreach (GeometryObject geoObj1 in geoElem1)
                                        {
                                            GeometryInstance geoInst = geoObj1 as GeometryInstance;
                                            if (geoInst != null)
                                            {
                                                GeometryElement geoElem2 = geoInst.GetInstanceGeometry() as GeometryElement;
                                                if (geoElem2 != null)
                                                {
                                                    foreach (GeometryObject geoObj2 in geoElem2)
                                                    {
                                                        ElementId styleid = geoObj2.GraphicsStyleId;
                                                        if (styleid != ElementId.InvalidElementId)
                                                        {
                                                            IList<Autodesk.Revit.DB.Line> lines = new List<Line>();
                                                            IList<PolyLine> polyLines = new List<PolyLine>();
                                                            IList<Arc> arcs = new List<Arc>();
                                                            GraphicsStyle style = (GraphicsStyle)doc.GetElement(styleid);
                                                            string dwglayername = style.GraphicsStyleCategory.Name;
                                                            if (dwglayername == layerName)
                                                            {
                                                                if (geoObj2 is Line)
                                                                {
                                                                    lines.Add(geoObj2 as Line);
                                                                }
                                                                if (geoObj2 is PolyLine)
                                                                {
                                                                    polyLines.Add(geoObj2 as PolyLine);
                                                                }
                                                                if (geoObj2 is Arc)
                                                                {
                                                                    arcs.Add(geoObj2 as Arc);
                                                                }
                                                            }
                                                            IList<Line> polyCurves = ConvertPolyLinesToCurves(polyLines);
                                                            foreach (Line line in polyCurves)
                                                            {
                                                                lines.Add(line);
                                                            }
                                                            foreach (Curve c in lines)
                                                            {
                                                                if (c.Length > 0.01)
                                                                {
                                                                    doc.Create.NewDetailCurve(doc.ActiveView, c).LineStyle = graphicsStyle;
                                                                }
                                                            }
                                                            foreach (Arc c in arcs)
                                                            {
                                                                doc.Create.NewDetailCurve(doc.ActiveView, c).LineStyle = graphicsStyle;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            curves.Commit();
                        }
                    }
                    else
                    {
                        TaskDialog.Show("Error", "Please select a CAD import element.");
                    }

                }
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        public IList<Autodesk.Revit.DB.Line> ConvertPolyLinesToCurves(IList<PolyLine> polyLines)
        {
            IList<Autodesk.Revit.DB.Line> curves = new List<Line>();
            foreach (PolyLine polyLine in polyLines)
            {
                for (int i = 0; i < polyLine.NumberOfCoordinates - 1; i++)
                {
                    XYZ startPoint = polyLine.GetCoordinate(i);
                    XYZ endPoint = polyLine.GetCoordinate(i + 1);
                    Line curveLine = Line.CreateBound(startPoint, endPoint);
                    curves.Add(curveLine);
                }
            }

            return curves;
        }
        public static List<string> ExtractCADSubcategories(Element selectedElement)
        {
            List<string> subcategoryNames = new List<string>();

            // Check if the selected element is an ImportInstance
            if (selectedElement is ImportInstance importInstance)
            {
                Category category = importInstance.Category;
                if (category != null)
                {
                    CategoryNameMap subCategories = category.SubCategories;
                    foreach (Category subCategory in subCategories)
                    {
                        subcategoryNames.Add(subCategory.Name);
                    }
                }
            }

            return subcategoryNames;
        }
        private GraphicsStyle GetGraphicsStyleByName(Autodesk.Revit.DB.Document document, string lineStyleName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(GraphicsStyle));

            GraphicsStyle graphicsStyle = collector.Cast<GraphicsStyle>()
                .FirstOrDefault(style => style.Name.Equals(lineStyleName));

            return graphicsStyle;
        }
        private List<string> GetAllLineStyleNames(Autodesk.Revit.DB.Document doc)
        {
            List<string> results = new List<string>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(GraphicsStyle));

            Category linesCategory = Category.GetCategory(doc, BuiltInCategory.OST_Lines);

            foreach (GraphicsStyle style in collector)
            {
                Category category = style.GraphicsStyleCategory;
                if (category != null && category.Parent != null && category.Parent.Id == linesCategory.Id)
                {
                    results.Add(style.Name);
                }
            }

            return results;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}