#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class FamilyFileSizeReporter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current Revit application and document
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Autodesk.Revit.DB.Document document = uiDoc.Document;
            Dictionary<string, double> familyFileSizes = new Dictionary<string, double>();
            // Get all the loaded families in the document
            FilteredElementCollector collector = new FilteredElementCollector(document);
            ICollection<Element> families = collector.OfClass(typeof(Family)).ToElements();

            // Iterate over each family and calculate the file size
            foreach (Family family in families)
            {
                // Get the external file reference of the family
                ExternalFileReference externalFileRef = family.GetExternalFileReference();

                // Get the family file path
                ModelPath familyPath1 = externalFileRef.GetPath();
                String familyPath = familyPath1.ToString();

                // Create a FileInfo object to retrieve the file size
                FileInfo fileInfo = new FileInfo(familyPath);

                // Get the file size in bytes
                long fileSizeInBytes = fileInfo.Length;

                // Convert the file size to megabytes
                double fileSizeInMB = fileSizeInBytes / (1024.0 * 1024.0);

                // Store the file size for each family
                familyFileSizes.Add(family.Name, fileSizeInMB);

                // Sort the families based on file size (largest to smallest)
                var sortedFamilies = familyFileSizes.OrderByDescending(x => x.Value);

                // Create a new dictionary to store the sorted families
                Dictionary<string, double> sortedFamilyFileSizes = sortedFamilies.ToDictionary(x => x.Key, x => x.Value);
                // Print the file size for each family
                Console.WriteLine($"Family Name: {family.Name}");
                Console.WriteLine("File Size:");
                Console.WriteLine($"   Bytes: {fileSizeInBytes}");
                Console.WriteLine($"   Megabytes: {fileSizeInMB}");
                Console.WriteLine();
            }
            return Result.Succeeded;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }

}
