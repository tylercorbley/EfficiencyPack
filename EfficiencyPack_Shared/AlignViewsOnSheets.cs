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
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

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

                XYZ referencePoint = referenceViewport.GetBoxCenter();

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
                        XYZ currentPoint = vp.GetBoxCenter();
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
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}