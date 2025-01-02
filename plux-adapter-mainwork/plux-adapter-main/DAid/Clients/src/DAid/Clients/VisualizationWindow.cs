using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DAid.Clients
{
    public class VisualizationWindow : Form
    {
        private const int CanvasSize = 400; 
        private double copX = 0; // Center of Pressure X
        private double copY = 0; // Center of Pressure Y
        private double[] sensorPressures = Array.Empty<double>(); 

        public VisualizationWindow()
        {
            // Initialize the visualization window
            this.Text = "Real-Time CoP Visualization";
            this.Size = new Size(CanvasSize + 40, CanvasSize + 60);
            this.DoubleBuffered = true;

            // Handle window close events
            this.FormClosing += (sender, e) =>
            {
                Application.Exit();
            };

            this.Shown += (sender, e) => Console.WriteLine("[VisualizationWindow]: Visualization started.");
        }

        /// <summary>
        /// Updates the visualization with new Center of Pressure (CoP) and sensor pressure data.
        /// </summary>
        public void UpdateVisualization(double x, double y, double[] pressures)
        {
            if (pressures == null || pressures.Length == 0)
            {
                Console.WriteLine("[VisualizationWindow]: No pressure data to update.");
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateVisualization(x, y, pressures)));
                return;
            }

            copX = x;
            copY = y;
            sensorPressures = pressures;

            for (int i = 0; i < sensorPressures.Length; i++)
            {
               // Console.WriteLine($"[VisualizationWindow]: Sensor {i + 1} Pressure={sensorPressures[i]:F2}");
            }

            Invalidate(); // Trigger a repaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;

            // Clear the canvas
            graphics.Clear(Color.White);

            // Draw grid lines
            graphics.DrawLine(Pens.Gray, CanvasSize / 2, 0, CanvasSize / 2, CanvasSize);
            graphics.DrawLine(Pens.Gray, 0, CanvasSize / 2, CanvasSize, CanvasSize / 2);

            if (sensorPressures.Length == 0)
            {
                DrawNoDataMessage(graphics);
                return;
            }

            // Scale CoP to canvas dimensions
            float scaledX = (float)(CanvasSize / 2 + copX * (CanvasSize / 16));
            float scaledY = (float)(CanvasSize / 2 - copY * (CanvasSize / 16));

            // Draw CoP on the canvas
            graphics.FillEllipse(Brushes.Red, scaledX - 5, scaledY - 5, 10, 10);

            // Draw sensor pressures
            DrawPressures(graphics);
        }

        private void DrawPressures(Graphics graphics)
{
    // Sensor positions relative to the canvas
    double[] XPositions = { 4.0, -4.0, 4.0, -4.0 };
    double[] YPositions = { 2.0, 2.0, -2.0, -2.0 };

    // Get the maximum pressure to normalize intensities
    double maxPressure = GetMaxPressure(sensorPressures);
    if (maxPressure <= 0.001)
    {
        maxPressure = 1; // Prevent division by zero
    }

    for (int i = 0; i < sensorPressures.Length; i++)
    {
        // Normalize intensity based on maximum pressure
        float intensity = (float)(sensorPressures[i] / maxPressure);
        intensity = Math.Max(0, Math.Min(1, intensity)); // Clamp values between 0 and 1

        // Ensure visible red even for low pressures
        int redValue = (int)(255 * intensity);
        redValue = Math.Max(50, redValue); // Set minimum visibility threshold for red

        // Determine color based on intensity
        Color pressureColor = Color.FromArgb(redValue, 0, 0);

        // Scale positions to canvas
        float scaledX = (float)(CanvasSize / 2 + XPositions[i] * (CanvasSize / 16));
        float scaledY = (float)(CanvasSize / 2 - YPositions[i] * (CanvasSize / 16));

        // Draw sensor visualization
        graphics.FillEllipse(new SolidBrush(pressureColor), scaledX - 10, scaledY - 10, 20, 20);
        graphics.DrawString($"S{i + 1}", new Font("Arial", 8), Brushes.Black, scaledX - 5, scaledY - 15);

        //Console.WriteLine($"Sensor {i + 1}: Pressure={sensorPressures[i]:F2}, Intensity={intensity:F2}");
    }
}



        private void DrawNoDataMessage(Graphics graphics)
        {
            // Display a message when no data is available
            graphics.DrawString("No Data Available", new Font("Arial", 16, FontStyle.Bold), Brushes.Gray,
                new PointF(CanvasSize / 2 - 100, CanvasSize / 2 - 20));
        }

        private double GetMaxPressure(double[] pressures)
        {
            return pressures.Length > 0 ? pressures.Max() : 1.0; // Default to 1.0 to prevent division by zero
        }
    }
}
