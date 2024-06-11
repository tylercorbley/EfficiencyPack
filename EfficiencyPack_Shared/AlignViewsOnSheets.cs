#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class AlignViewsOnSheets : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            try
            {
                // Step 1: Select a view on a sheet
                Reference refView = uidoc.Selection.PickObject(ObjectType.Element, new ViewportSelectionFilter(), "Select the reference viewport on a sheet.");
                Viewport referenceViewport = doc.GetElement(refView) as Viewport;
                if (referenceViewport == null)
                {
                    message = "Selected element is not a viewport.";
                    return Result.Failed;
                }

                //XYZ referencePoint = referenceViewport.GetBoxCenter();
                XYZ referencePoint = getReferencePoint(doc, referenceViewport);

                // Step 3: Select several views on other sheets
                IList<Reference> selectedViewRefs = uidoc.Selection.PickObjects(ObjectType.Element, new ViewportSelectionFilter(), "Select viewports on other sheets to align.");
                List<Viewport> viewportsToAlign = new List<Viewport>();

                foreach (Reference r in selectedViewRefs)
                {
                    Viewport vp = doc.GetElement(r) as Viewport;
                    if (vp != null)
                    {
                        viewportsToAlign.Add(vp);
                    }
                }

                // Step 4: Confirm action and align views
                using (Transaction trans = new Transaction(doc, "Align Views on Sheets"))
                {
                    trans.Start();

                    foreach (Viewport vp in viewportsToAlign)
                    {
                        //XYZ currentPoint = vp.GetBoxCenter();
                        XYZ currentPoint = getReferencePoint(doc, vp);
                        XYZ translationVector = referencePoint - currentPoint;

                        ElementTransformUtils.MoveElement(doc, vp.Id, translationVector);
                    }

                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

        }
        private XYZ GetProjectBasePoint(Viewport viewport, Document doc)
        {
            // Get the view associated with the viewport
            View view = doc.GetElement(viewport.ViewId) as View;

            // Get the Project Base Point (PBP) from the project
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_ProjectBasePoint);
            collector.OfClass(typeof(BasePoint));

            BasePoint projectBasePoint = collector.FirstElement() as BasePoint;
            XYZ basePointLocation = null;

            if (projectBasePoint != null)
            {
                basePointLocation = (projectBasePoint.get_BoundingBox(view).Min + projectBasePoint.get_BoundingBox(view).Max) / 2;
            }

            return basePointLocation;
        }
        private class ViewportSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                return elem is Viewport;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return true;
            }
        }
        private XYZ getReferencePoint(Document doc, Viewport referenceViewport)
        {
            View V = (View)doc.GetElement(referenceViewport.ViewId);

            Outline VPoln = referenceViewport.GetBoxOutline();
            //Viewport outline in Sheet coords
            BoundingBoxUV Voln = V.Outline;
            //View outline
            int Scale = V.Scale;

            //Transform for view coords (very important for rotated view plans set to true north etc.)
            //completely not important when view plan is not rotated
            Transform T = Transform.Identity;
            T.BasisX = V.RightDirection;
            T.BasisY = V.UpDirection;
            T.BasisZ = V.ViewDirection;
            T.Origin = V.Origin;

            dynamic Voln_cen = (Voln.Min + Voln.Max) / 2;
            //View outline centre
            XYZ VPcen = (VPoln.MaximumPoint + VPoln.MinimumPoint) / 2;
            //Viewport centre
            VPcen = new XYZ(VPcen.X, VPcen.Y, 0);
            //Zero z

            //Correction offset from VCen to centre of Viewport in sheet coords 
            XYZ Offset = VPcen - new XYZ(Voln_cen.U, Voln_cen.V, 0);

            // Get the project base point for the reference view
            XYZ referencePoint = GetProjectBasePoint(referenceViewport, doc);
            XYZ referencePointSheet = T.Inverse.OfPoint(referencePoint).Multiply((Double)1 / Scale) + Offset;
            referencePointSheet = new XYZ(referencePointSheet.X, referencePointSheet.Y, 0);

            return referencePointSheet;
        }
        private dynamic getTitleblockLocation(Document doc, Element selectedView)
        {
            // Get the ViewSheet that contains the selected view
            ViewSheet activeSheet = null;
            // Check if the selected view is placed on a sheet
            if (selectedView.OwnerViewId != ElementId.InvalidElementId)
            {
                // Get the owner view (which should be a sheet)
                Element ownerViewElement = doc.GetElement(selectedView.OwnerViewId);

                // Check if the owner view is a ViewSheet
                if (ownerViewElement is ViewSheet sheet)
                {
                    activeSheet = sheet;
                }
            }
            if (activeSheet != null)
            {
                // Do something with the ViewSheet
                TaskDialog.Show("View Sheet Info", $"The selected view '{selectedView.Name}' is placed on the sheet '{activeSheet.Name}'.");
            }
            else
            {
                TaskDialog.Show("View Sheet Info", $"The selected view '{selectedView.Name}' is not placed on a sheet.");
            }
            // Get the location of the title block on the sheet
            // Filter for title block elements
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            // Define the filters separately
            ElementCategoryFilter titleBlockCategoryFilter = new ElementCategoryFilter(BuiltInCategory.OST_TitleBlocks);
            ElementClassFilter titleBlockInstanceFilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementId currentSheetId = activeSheet.Id;
            ElementParameterFilter sheetFilter = new ElementParameterFilter(ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.ID_PARAM), currentSheetId));

            // Use a FilteredElementCollector to retrieve title blocks on the current sheet
            FilteredElementCollector titleBlockCollector = new FilteredElementCollector(doc);
            titleBlockCollector.WherePasses(titleBlockCategoryFilter).WherePasses(titleBlockInstanceFilter).WherePasses(sheetFilter);

            // Get the list of title blocks
            IList<Element> titleBlocks = titleBlockCollector.ToElements();

            //if (titleBlocks.Count != 1)
            //{
            //    message = "There should be exactly one title block on the sheet.";
            //    return Result.Failed;
            //}
            //We use these points only to relate the sheet coord system to the printed lines (offset from corner of title block)
            //i.e. title block can be located anywhere in sheet coord space but when it is printed 
            //such a coord space will be truncated to the extents of the title block
            XYZ TB_Pt_min = titleBlocks[0].get_BoundingBox(activeSheet).Min;
            XYZ TB_Pt_max = titleBlocks[0].get_BoundingBox(activeSheet).Max;
            TB_Pt_min = new XYZ(TB_Pt_min.X, TB_Pt_min.Y, 0);
            //Zero Z
            TB_Pt_max = new XYZ(TB_Pt_max.X, TB_Pt_max.Y, 0);
            //Zero Z
            dynamic TB_TopLeft = new XYZ(TB_Pt_min.X, TB_Pt_max.Y, 0);
            return TB_TopLeft;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}