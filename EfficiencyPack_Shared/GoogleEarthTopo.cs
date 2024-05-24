using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace EfficiencyPack
{
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
                    int size = Int32.Parse(form.Radius);
                    int resolution = Int32.Parse(form.Resolution);

                    // Call Python script with the address
                    IList<XYZ> points = CallPythonScript(address, size, resolution);

                    // Get the lowest level and a Toposolid type
                    ElementId levelId = GetLowestLevelId(doc);
                    ElementId toposolidTypeId = GetToposolidTypeId(doc);
#if REVIT2023 || REVIT2024
                    // Create TopoSolid
                    using (Transaction trans = new Transaction(doc, "Create TopoSolid"))
                    {
                        trans.Start();

                        Toposolid.Create(doc, points, toposolidTypeId, levelId);

                        trans.Commit();
                    }
#endif
                }
            }

            return Result.Succeeded;
        }

        private IList<XYZ> CallPythonScript(string address, int size, int resolution)
        {
            IList<XYZ> points = new List<XYZ>();

            try
            {
                // Initialize Python engine
                Runtime.PythonDLL = @"C:\Program Files (x86)\Microsoft Visual Studio\Shared\Python39_64\python39.dll";
                //PythonEngine.PythonPath = @"C:\Python311\Lib\site-packages\pythonnet\runtime\Python.Runtime.dll"; // Specify the path to your Python DLL
                PythonEngine.Initialize();

                using (Py.GIL())
                {
                    dynamic sys = Py.Import("sys");
                    sys.path.append(@"C:\Users\Tyler\source\repos\EfficiencyPack\PythonApplication1");

                    dynamic module = Py.Import("GoogleEarthTopo");
                    dynamic coords = module.process_address(address, size, resolution);

                    foreach (var coord in coords)
                    {
                        double x = (double)coord[0] * 3.28084; // Convert from meters to feet
                        double y = (double)coord[1] * 3.28084; // Convert from meters to feet
                        double z = (double)coord[2] * 3.28084; // Convert from meters to feet
                        points.Add(new XYZ(x, y, z));
                    }
                }

                // Shutdown Python engine
                PythonEngine.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return points;
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

        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}
