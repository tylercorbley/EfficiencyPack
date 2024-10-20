#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class LinesByRoom : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Autodesk.Revit.DB.Document doc = uiDoc.Document;

                // Get the selection of rooms in the Revit document
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                // Filter the selected elements to get only rooms
                List<Room> selectedRooms = new List<Room>();
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element is Room room)
                    {
                        selectedRooms.Add(room);
                    }
                }

                View curView = GetActiveView(doc);
                List<string> lineStyles = GetAllLineStyleNames(doc);

                FrmDtlLn curForm = new FrmDtlLn(lineStyles);
                curForm.Height = 200;
                curForm.Width = 450;
                curForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

                if (curForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string selectedLineStyle = curForm.GetSelectedLineStyle();

                    // Start a transaction
                    using (Transaction transaction = new Transaction(doc))
                    {
                        transaction.Start("Create Room Boundary Detail Lines");

                        foreach (Room room in selectedRooms)
                        {
                            // Get the outline of the room
                            IList<IList<BoundarySegment>> roomBoundaries = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
                            if (roomBoundaries.Count > 0)
                            {
                                foreach (IList<BoundarySegment> boundary in roomBoundaries)
                                {
                                    CurveLoop curveArray = new CurveLoop();
                                    foreach (BoundarySegment seg in boundary)
                                    {
                                        Curve curve1 = seg.GetCurve();
                                        curveArray.Append(curve1);
                                    }
                                    // Get the room boundary curve loop
                                    //LocationCurve locationCurve = room.Location as LocationCurve;
                                    //Curve curve = locationCurve.Curve;
                                    //CurveLoop curveLoop = new CurveLoop();
                                    //curveLoop.Append(curve);

                                    // Create the negative offset curve loop
                                    double offsetDistance = -.5; // Specify the negative offset distance here
                                    CurveLoop offsetCurveLoop = CurveLoop.CreateViaOffset(curveArray, offsetDistance, XYZ.BasisZ);

                                    // Get the line style name
                                    string lineStyleName = selectedLineStyle; // Specify the line style name here

                                    // Set the detail line style by name
                                    GraphicsStyle graphicsStyle = GetGraphicsStyleByName(doc, lineStyleName);

                                    // Create the detail curves at the room boundaries with the specified line style
                                    foreach (Curve offsetCurve in offsetCurveLoop)
                                    {
                                        DetailCurve detailCurve = doc.Create.NewDetailCurve(curView, offsetCurve) as DetailCurve;
                                        if (detailCurve != null && graphicsStyle != null)
                                        {
                                            detailCurve.LineStyle = graphicsStyle;
                                        }
                                    }
                                }
                            }
                        }
                        transaction.Commit();
                    }
                }
                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
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
        public View GetActiveView(Autodesk.Revit.DB.Document document)
        {
            // Get the active view
            ElementId activeViewId = document.ActiveView.Id;

            // Use a filtered element collector to retrieve the active view
            FilteredElementCollector viewCollector = new FilteredElementCollector(document);
            ICollection<Element> views = viewCollector.OfClass(typeof(View)).ToElements();

            foreach (Element viewElement in views)
            {
                View view = viewElement as View;

                // Check if the view element ID matches the active view ID
                if (view != null && view.Id.Equals(activeViewId))
                {
                    return view;
                }
            }

            return null; // Return null if the active view is not found
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}