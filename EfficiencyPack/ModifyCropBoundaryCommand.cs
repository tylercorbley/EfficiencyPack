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
    public class ModifyCropBoundaryCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current document
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Get the selected elements on the active view
                ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
                if (selectedIds.Count == 0)
                {
                    message = "Please select at least one elevation view.";
                    return Result.Failed;
                }

                using (Transaction trans = new Transaction(doc, "Modify Crop Boundary"))
                {
                    trans.Start();

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
                                    // Get the crop box of the elevation view
                                    BoundingBoxXYZ cropBox = view.CropBox;

                                    // Increase the height by 12 feet
                                    double height = cropBox.Max.Z - cropBox.Min.Z;
                                    cropBox.Min = new XYZ(cropBox.Min.X, cropBox.Min.Y, cropBox.Min.Z);
                                    cropBox.Max = new XYZ(cropBox.Max.X, cropBox.Max.Y + 12, cropBox.Max.Z);

                                    // Update the crop box
                                    view.CropBox = cropBox;
                                }
                            }
                        }
                    }

                    trans.Commit();
                }

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
    }
}
