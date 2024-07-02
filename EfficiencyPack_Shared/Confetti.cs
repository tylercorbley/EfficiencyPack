using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using EfficiencyPack_Resources.Properties;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class Confetti : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var window = new ConfettiWindow();
            window.Show();
            return Result.Succeeded;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
    public class ConfettiWindow : Window
    {
        private SKElement skElement;
        private ConfettiSimulation simulation;

        public ConfettiWindow()
        {
            Title = "Confetti Simulation";
            Width = 800;
            Height = 600;

            skElement = new SKElement();
            skElement.PaintSurface += OnPaintSurface;
            Content = skElement;

            simulation = new ConfettiSimulation();

            MouseDown += OnMouseDown;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            simulation.Draw(e.Surface.Canvas, (float)ActualWidth, (float)ActualHeight);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            simulation.CreateConfetti((float)ActualWidth - 100 + 10, (float)ActualHeight - 100 + 20, 100);
            skElement.InvalidateVisual();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            simulation.Update();
            skElement.InvalidateVisual();
        }
    }
    public class ConfettiSimulation
    {
        private List<Item> itemList = new List<Item>();
        private Random random = new Random();
        private SKBitmap crackerImage;

        public ConfettiSimulation()
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(Resources));
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resources in resourceNames)
            {
                System.Diagnostics.Debug.WriteLine(resources);
            }
            // Load the image. You'll need to adjust the path to where your image is stored.
            //crackerImage = SKBitmap.Decode($@"C:\Users\Tyler\source\repos\EfficiencyPack\EfficiencyPack_Resources\Resources\cracker.png");

            string resourceName = "cracker.png";
            crackerImage = LoadImageResource(resourceName);
            if (crackerImage == null)
            {
                // Handle the error - maybe load a default image or show an error message
            }
        }
        private SKBitmap LoadImageResource(string resourceName)
        {
            // Get the assembly where the resource is located
            var resourceAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "EfficiencyPack_Resources");

            if (resourceAssembly == null)
            {
                System.Diagnostics.Debug.WriteLine("EfficiencyPack_Resources assembly not found.");
                return null;
            }

            // The full resource name should include the project's default namespace
            string fullResourceName = $"EfficiencyPack_Resources.Resources.{resourceName}";

            using (var stream = resourceAssembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load resource stream: {fullResourceName}");
                    return null;
                }

                try
                {
                    return SKBitmap.Decode(stream);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to decode image: {ex.Message}");
                    return null;
                }
            }
        }

        public void Update()
        {
            for (int i = itemList.Count - 1; i >= 0; i--)
            {
                var item = itemList[i];
                item.Time++;
                item.Dx *= 0.99f;
                item.Dy += 0.1f;
                item.Dy = Math.Min(item.Dy, 5);
                item.X += item.Dx;
                item.Y += item.Dy;

                if (item.Y > 600) // Assuming 600 is the height of the window
                {
                    itemList.RemoveAt(i);
                }
            }
        }

        public void Draw(SKCanvas canvas, float width, float height)
        {
            canvas.Clear(new SKColor(30, 30, 30));
            canvas.DrawBitmap(crackerImage, new SKRect(width - 100, height - 100, width, height));

            foreach (var item in itemList)
            {
                using (var paint = new SKPaint { Color = new SKColor((byte)item.Red, (byte)item.Green, (byte)item.Blue) })
                {
                    canvas.Save();
                    canvas.Translate(item.X, item.Y);
                    canvas.RotateDegrees(item.Angle);
                    canvas.DrawRect(0, 0, 10 * (float)Math.Cos(item.Time * Math.PI / 180), 20, paint);
                    canvas.Restore();
                }
            }
        }

        public void CreateConfetti(float x, float y, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = new Item
                {
                    X = x,
                    Y = y,
                    Time = (float)random.NextDouble() * 1000,
                    Red = random.Next(100, 256),
                    Green = random.Next(100, 256),
                    Blue = random.Next(100, 256),
                    Angle = (float)random.NextDouble() * 360
                };

                float vecX = (float)random.NextDouble() * -1;
                float vecY = (float)random.NextDouble() * -0.5f;
                float magnitude = (float)Math.Sqrt(vecX * vecX + vecY * vecY);
                vecX /= magnitude;
                vecY /= magnitude;

                float power = (float)random.NextDouble() * 25 + 5;
                item.Dx = vecX * power;
                item.Dy = vecY * power;

                itemList.Add(item);
            }
        }
    }
    public class Item
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Time { get; set; }
        public int Red { get; set; }
        public int Green { get; set; }
        public int Blue { get; set; }
        public float Angle { get; set; }
        public float Dx { get; set; }
        public float Dy { get; set; }
    }
}

