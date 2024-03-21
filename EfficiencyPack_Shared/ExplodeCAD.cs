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
                //Reference selectedRef = uiDoc.Selection.PickObject(ObjectType.Element);

                // Get the selected element
                if (uiApp.ActiveUIDocument.Selection.GetElementIds().Count == 0)
                {
                    TaskDialog.Show("Selection Required", "Please select a CAD import element.");
                    return Result.Cancelled;
                }

                Element selectedElement = doc.GetElement(uiApp.ActiveUIDocument.Selection.GetElementIds().First());


                List<string> subcategoryNames = ExtractCADSubcategories(selectedElement);

                //Element selectedElement = doc.GetElement(selectedRef);

                List<string> lineStyles = GetAllLineStyleNames(doc);

                FrmExplodeCAD formCAD = new FrmExplodeCAD(lineStyles, subcategoryNames);
                //formCAD.Height = 550;
                //formCAD.Width = 550;
                formCAD.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                (List<Line> lines, List<PolyLine> polyLines) = ExtractCADGeometry(doc);

                //formCAD.UpdateListView(subcategoryNames);


                if (formCAD.ShowDialog() == System.Windows.Forms.DialogResult.OK)


                    // Check if the selected element is a CAD import
                    if (selectedElement is ImportInstance cadInstance)
                    {
                        // Get the CAD link document
                        //Document cadDoc = cadInstance.GetLinkDocument();

                        // Ensure that the CAD document is not null
                        //if (cadDoc != null)
                        //{
                        //    // Get the list of line styles from the CAD document
                        //    List<string> CADlineStyles = GetCADLineStyles(cadDoc);

                        //    // Display the line styles (for testing)
                        //    foreach (string lineStyle in lineStyles)
                        //    {
                        //        TaskDialog.Show("Line Styles", lineStyle);
                        //    }
                        //}
                        //else
                        //{
                        //    TaskDialog.Show("Error", "Failed to retrieve CAD link document.");
                        //}
                    }
                    else
                    {
                        TaskDialog.Show("Error", "Please select a CAD import element.");
                    }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private List<string> GetCADLineStyles(Document cadDoc)
        {
            List<string> lineStyles = new List<string>();

            // Implement logic to retrieve line styles from the CAD document
            // You can use the API methods available for working with CAD documents

            return lineStyles;
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
        public static (List<Line>, List<PolyLine>) ExtractCADGeometry(Document doc)
        {
            List<Line> lines = new List<Line>();
            List<PolyLine> polyLines = new List<PolyLine>();

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ImportInstance))
                .WhereElementIsNotElementType();

            foreach (ImportInstance importInst in collector)
            {
                Options options = new Options();
                options.ComputeReferences = true;
                options.IncludeNonVisibleObjects = true;

                GeometryElement geoElem1 = importInst.get_Geometry(options);
                if (geoElem1 != null)
                {
                    foreach (GeometryObject geoObj1 in geoElem1)
                    {
                        GeometryInstance geoInst = geoObj1 as GeometryInstance;
                        if (geoInst != null)
                        {
                            GeometryElement geoElem2 = geoInst.GetInstanceGeometry();
                            if (geoElem2 != null)
                            {
                                foreach (GeometryObject geoObj2 in geoElem2)
                                {
                                    if (geoObj2 is Line line)
                                    {
                                        lines.Add(line);
                                    }
                                    else if (geoObj2 is PolyLine polyLine)
                                    {
                                        polyLines.Add(polyLine);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (lines, polyLines);
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
