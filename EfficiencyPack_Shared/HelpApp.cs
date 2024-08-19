#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Diagnostics;
using System.Reflection;

#endregion

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class HelpApp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // URL of the web page you want to open
            string url = "https://github.com/tylercorbley/EfficiencyPack";

            // Open the web page
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return Result.Succeeded;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
}