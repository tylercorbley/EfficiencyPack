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
    public class DimensionText : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the current Revit application and document
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Autodesk.Revit.DB.Document doc = uiDoc.Document;
                // Get the currently selected elements
                ICollection<ElementId> selectedIds = uiApp.ActiveUIDocument.Selection.GetElementIds();

                // Filter the selected elements to only include dimensions
                List<Dimension> selectedDimensions = new List<Dimension>();
                foreach (ElementId id in selectedIds)
                {
                    Element element = doc.GetElement(id);
                    if (element is Dimension dimension)
                    {
                        selectedDimensions.Add(dimension);
                    }
                }

                // Check if there are any selected dimensions
                if (selectedDimensions.Count == 0)
                {
                    TaskDialog.Show("Error", "Please select one or more dimensions to modify.");
                }

                // Modify the text values of the selected dimensions
                using (Transaction transaction = new Transaction(doc, "Change Dimension Text"))
                {
                    transaction.Start();
                    foreach (Dimension dimension in selectedDimensions)
                    {
                        if (dimension.NumberOfSegments > 1)
                        {

                            foreach (DimensionSegment segment in dimension.Segments)
                            {
                                // Access and modify properties of each segment
                                if (segment.ValueOverride != "EQ" && segment.Below != "V.I.F.")
                                {
                                    segment.ValueOverride = "EQ";
                                    segment.Below = null;
                                }
                                else if (segment.ValueOverride == "EQ")
                                {
                                    segment.ValueOverride = null;
                                    segment.Below = "V.I.F.";
                                }
                                else if (segment.Below == "V.I.F.")
                                {
                                    segment.ValueOverride = null;
                                    segment.Below = null;
                                }
                            }
                        }
                        else if (dimension.NumberOfSegments == 0)
                        {
                            if (dimension.ValueOverride != "EQ" && dimension.Below != "V.I.F.")
                            {
                                dimension.ValueOverride = "EQ";
                                dimension.Below = null;
                            }
                            else if (dimension.ValueOverride == "EQ")
                            {
                                dimension.ValueOverride = null;
                                dimension.Below = "V.I.F.";
                            }
                            else if (dimension.Below == "V.I.F.")
                            {
                                dimension.ValueOverride = null;
                                dimension.Below = null;
                            }
                        }
                    }

                    transaction.Commit();
                }

                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
        // Custom selection filter for dimension elements
        public class DimensionSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element elem)
            {
                // Filter for dimension elements
                return elem is Dimension;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                // Allow selection of dimensions
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