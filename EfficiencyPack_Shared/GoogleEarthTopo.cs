using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace EfficiencyPack
{
    public class TopoXYZ
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }
    [Transaction(TransactionMode.Manual)]
    public class GoogleEarthTopo : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            // this is a variable for the Revit application
            UIApplication uiapp = commandData.Application;
            // this is a variable for the current Revit model
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            // Show form to get address from user
            using (FrmGEE form = new FrmGEE())
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string address = form.Address;
                    // Check if the address is null or empty
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        MessageBox.Show("Please enter a valid address.", "Invalid Address", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return Result.Failed; // Exit the command early
                    }
                    double size = Convert.ToDouble(form.Radius); // 100000 * 0.3048;
                    string sizeStr = Convert.ToString(size).ToString(CultureInfo.InvariantCulture);
                    //double resolution = Convert.ToDouble(form.Resolution);
                    string resolution = form.Resolution.ToString(CultureInfo.InvariantCulture);

                    // Get the path to the user's AppData folder
                    string appDataFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                    // Path to the executable you want to run
                    string exePath = $@"{appDataFolderPath}\Autodesk\Revit\Addins\EfficiencyPack\GoogleEarthTopo\GoogleEarthTopo.exe";
                    //TaskDialog.Show("test", exePath);
                    //Clipboard.SetText(exePath);
                    // Arguments to pass to the executable
                    string arguments = $"\"{address}\" \"{sizeStr}\" \"{resolution}\"";

                    // Create a new process start info
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true // Hide the window
                    };

                    // Start the process
                    using (Process process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        int exitCode = process.ExitCode;

                        // Log output and error streams
                        //TaskDialog.Show("Debug", $"Output: {output}\nError: {error}\nExit Code: {exitCode}");

                        if (exitCode != 0)
                        {
                            TaskDialog.Show("Error", $"Error: {error}\nExit Code: {exitCode}");
                            return Result.Failed;
                        }


                        // Parse the output into a list of XYZ points
                        IList<TopoXYZ> points = ParseOutput(output);

                        // Get the lowest level and a Toposolid type
                        ElementId levelId = GetLowestLevelId(doc);
                        ElementId toposolidTypeId = GetToposolidTypeId(doc);
#if REVIT2023||REVIT2024
                        // Create TopoSolid
                        using (Transaction trans = new Transaction(doc, "Create TopoSolid"))
                        {
                            trans.Start();
                            List<XYZ> revitPoints = points.Select(p => new XYZ(p.X * 3.28084, p.Y * 3.28084, p.Z * 3.28084)).ToList();
                            Toposolid.Create(doc, revitPoints, toposolidTypeId, levelId);

                            trans.Commit();
                        }
#endif
                    }
                }

                return Result.Succeeded;
            }
        }
        private ElementId GetLowestLevelId(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<Level> levels = collector.OfClass(typeof(Level)).Cast<Level>().ToList();
            Level lowestLevel = levels.OrderBy(level => level.Elevation).FirstOrDefault();
            return lowestLevel?.Id ?? ElementId.InvalidElementId;
        }
        private ElementId GetToposolidTypeId(Document doc)
        {
#if REVIT2023 || REVIT2024
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<ToposolidType> toposolidTypes = collector.OfClass(typeof(ToposolidType)).Cast<ToposolidType>().ToList();
            ToposolidType toposolidType = toposolidTypes.FirstOrDefault();
            return toposolidType?.Id ?? ElementId.InvalidElementId;
#endif
#if REVIT2022
            return null;
#endif

        }
        private IList<TopoXYZ> ParseOutput(string output)
        {
            var points = new List<TopoXYZ>();
            try
            {
                // Deserialize the JSON array of arrays into a list of lists
                var coordinates = JsonConvert.DeserializeObject<List<List<double>>>(output);

                // Convert the list of lists into a list of TopoXYZ objects
                foreach (var coordinate in coordinates)
                {
                    if (coordinate.Count == 3)
                    {
                        points.Add(new TopoXYZ
                        {
                            X = coordinate[0],
                            Y = coordinate[1],
                            Z = coordinate[2]
                        });
                    }
                    else
                    {
                        throw new Exception("Invalid coordinate data.");
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error parsing output: {ex.Message}");
            }
            return points;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}