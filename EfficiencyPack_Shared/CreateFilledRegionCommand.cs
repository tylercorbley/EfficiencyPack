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
    public class CreateFilledRegionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current Revit application and document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Get the selected elements on the active view
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
            if (selectedIds.Count == 0)
            {
                message = "Please select at least one elevation view.";
                return Result.Failed;
            }
            List<string> lineStyles = GetAllLineStyleNames(doc);
            List<string> filledStyles = GetFilledRegionTypeNames(doc);
            double offset = 1;
            FrmDonut curForm = new FrmDonut(lineStyles, filledStyles);
            curForm.Height = 200;
            curForm.Width = 650;
            curForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            string lineStyleName = curForm.GetSelectedLineStyle();
            string fillName = curForm.GetSelectedFillRegion();

            if (curForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element is Viewport viewport)
                    {
                        // Check if the viewport is placed on a sheet and has an associated elevation view
                        ViewSheet sheet = doc.GetElement(viewport.SheetId) as ViewSheet;
                        if (sheet != null && viewport.ViewId != ElementId.InvalidElementId)
                        {
                            Element viewElement = doc.GetElement(viewport.ViewId);
                            if (viewElement is View view && view.ViewType == ViewType.Elevation)
                            {

                                ElementId lineStyle = GetLineStyleIdByName(doc, lineStyleName);
                                ElementId filledRegionType = GetFilledRegionTypeByName(doc, fillName);
                                offset = curForm.getOffset();
                                // Create a filled region boundary loop
                                IList<CurveLoop> boundaryLoops = new List<CurveLoop>();
                                XYZ viewNormal = view.ViewDirection;
                                CurveLoop CropBoxBoundary = GetCropBoxBoundary(view);
                                boundaryLoops.Add(CropBoxBoundary);
                                CurveLoop CenteredBoxCurveOffset2 = CurveLoop.CreateViaOffset(CropBoxBoundary, offset, viewNormal);
                                boundaryLoops.Add(CenteredBoxCurveOffset2);
                                // Start a new transaction
                                using (Transaction trans = new Transaction(doc, "Place Filled Region"))
                                {
                                    trans.Start();

                                    // Create a new filled region
                                    FilledRegion filledRegion = FilledRegion.Create(doc, filledRegionType, view.Id, boundaryLoops);
                                    filledRegion.SetLineStyleId(lineStyle);
                                    trans.Commit();
                                }
                            }
                        }
                    }
                }
            }
            return Result.Succeeded;
        }
        public static List<string> GetFilledRegionTypeNames(Document doc)
        {
            // Create a collector to gather all FilledRegionTypes in the project
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> filledRegionTypes = collector
                .OfClass(typeof(FilledRegionType))
                .ToElements();

            // Extract the names of the filled region types
            List<string> filledRegionTypeNames = filledRegionTypes
                .Cast<FilledRegionType>()
                .Select(frt => frt.Name)
                .ToList();

            return filledRegionTypeNames;
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
        public ElementId GetLineStyleIdByName(Document doc, string lineStyleName)
        {
            // Get all line styles in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<ElementId> lineStyleIds = collector.OfClass(typeof(GraphicsStyle)).ToElementIds();

            // Find the line style with the specified name
            ElementId lineStyleId = lineStyleIds.FirstOrDefault(id =>
            {
                GraphicsStyle lineStyle = doc.GetElement(id) as GraphicsStyle;
                return lineStyle != null && lineStyle.Name == lineStyleName;
            });

            return lineStyleId;
        }
        public CurveLoop GetCropBoxBoundary(View view)
        {
            // Get the crop region shape manager
            ViewCropRegionShapeManager shapeManager = view.GetCropRegionShapeManager();

            // Get the crop shape
            IList<CurveLoop> cropShape = shapeManager.GetCropShape();
            CurveLoop desiredCurveLoop = cropShape.FirstOrDefault();

            return desiredCurveLoop;
        }
        public ElementId GetFilledRegionTypeByName(Document doc, string typeName)
        {
            // Get the filled region types in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> filledRegionTypes = collector.OfClass(typeof(FilledRegionType)).ToElements();

            // Find the filled region type with the specified name
            Element filledRegionType = filledRegionTypes.FirstOrDefault(x => x.Name.Equals(typeName));

            // Return the Element ID of the filled region type, or null if not found
            return filledRegionType?.Id;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}