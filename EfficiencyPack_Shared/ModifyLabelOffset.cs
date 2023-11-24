#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class ModifyLabelOffset : IExternalCommand
    {
        static string group1 = "Ifijlt";
        static string group2 = "JLT1234567890abcdeghknopqsuvxyz-";
        static string group3 = "ABCDEFGHKMNOPQRSUVXYZmw";
        static string group4 = "W&@";
        static string group5 = " ";
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
                    message = "Please select at least one view.";
                    return Result.Failed;
                }

                using (Transaction trans = new Transaction(doc, "Change Label Offset"))
                {
                    trans.Start();

                    foreach (ElementId id in selectedIds)
                    {
                        Element element = doc.GetElement(id);
                        if (element is Viewport viewport)
                        {

                            // Get the view of the selected viewport
                            View view = doc.GetElement(viewport.ViewId) as View;

                            // Get the view name parameter
                            string viewName = view.Name;
                            // Get the view title on sheet parameter
                            Parameter titleOnSheetParam = viewport.get_Parameter(BuiltInParameter.VIEWPORT_VIEW_NAME);
                            string titleOnSheet = titleOnSheetParam.AsString();
                            double titleLength = 0;

                            // Calculate the length of the title
                            if (titleOnSheet == null)
                            {
                                titleLength = CalculateTotalWidth(viewName);
                            }
                            else
                            {
                                titleLength = CalculateTotalWidth(titleOnSheet);
                            }
                            viewport.LabelLineLength = (titleLength + .1) / 12;

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
        public static System.String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
        static double CalculateTotalWidth(string input)
        {
            Dictionary<char, int> characterCounts = CalculateCharacterCounts(input);

            double totalWidth = 0.0;

            foreach (var kvp in characterCounts)
            {
                char character = kvp.Key;
                int count = kvp.Value;

                double value = GetCharacterValue(character);

                // Multiply the value by the count and add to the total
                totalWidth += value * count;
            }

            return totalWidth;
        }

        static Dictionary<char, int> CalculateCharacterCounts(string input)
        {
            // Define the regular expression pattern
            string pattern = $@"([{group1}])|([{group2}])|([{group3}])|([{group4}])|([{group5}])";

            // Create a regex object
            Regex regex = new Regex(pattern);

            // Match the input string
            MatchCollection matches = regex.Matches(input);

            // Initialize the character count dictionary
            Dictionary<char, int> characterCounts = new Dictionary<char, int>();

            foreach (Match match in matches)
            {
                for (int groupIndex = 1; groupIndex <= 5; groupIndex++)
                {
                    if (match.Groups[groupIndex].Success)
                    {
                        foreach (Capture capture in match.Groups[groupIndex].Captures)
                        {
                            char character = capture.Value[0];

                            // Update or initialize the count for the character
                            if (characterCounts.ContainsKey(character))
                            {
                                characterCounts[character]++;
                            }
                            else
                            {
                                characterCounts[character] = 1;
                            }
                        }
                    }
                }
            }

            return characterCounts;
        }

        static double GetCharacterValue(char character)
        {
            // Define the mapping for character values based on groups
            if (group1.Contains(character))
            {
                return .0625;
            }
            else if (group2.Contains(character))
            {
                return 0.15;
            }
            else if (group3.Contains(character))
            {
                return .19;
            }
            else if (group4.Contains(character))
            {
                return .24;
            }
            else if (group5.Contains(character))
            {
                return .08;
            }

            // Default value if the character doesn't match any group
            return 0.0;
        }
    }
}
