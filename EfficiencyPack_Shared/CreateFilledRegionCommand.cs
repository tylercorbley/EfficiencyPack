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


            ElementId lineStyle = GetLineStyleIdByName(doc, "5");
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
                            // Create a filled region boundary loop
                            IList<CurveLoop> boundaryLoops = new List<CurveLoop>();
                            XYZ viewNormal = view.ViewDirection;
                            ElementId filledRegionType = GetFilledRegionTypeByName(doc, "Solid Fill - White (D)");
                            CurveLoop CropBoxBoundary = GetCropBoxBoundary(view);
                            boundaryLoops.Add(CropBoxBoundary);
                            CurveLoop CenteredBoxCurveOffset = CurveLoop.CreateViaOffset(CropBoxBoundary, .5, viewNormal);
                            boundaryLoops.Add(CenteredBoxCurveOffset);
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
            return Result.Succeeded;
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