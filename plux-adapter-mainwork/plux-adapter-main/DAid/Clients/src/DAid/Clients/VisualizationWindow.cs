using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DAid.Clients
{
    public class VisualizationWindow : Form
    {
        private const int CanvasSize = 400;
        private const int SockSpacing = CanvasSize + 50; // Spacing between socks
        private const int DataTimeoutMilliseconds = 2000; // Timeout to consider data stale

        private double copXLeft = 0, copYLeft = 0;
        private double[] sensorPressuresLeft = Array.Empty<double>();
        private DateTime lastLeftDataUpdate = DateTime.MinValue;

        private double copXRight = 0, copYRight = 0;
        private double[] sensorPressuresRight = Array.Empty<double>();
        private DateTime lastRightDataUpdate = DateTime.MinValue;

        public VisualizationWindow()
        {
            this.Text = "Real-Time CoP Visualization";
            this.Size = new Size(SockSpacing * 2, CanvasSize + 150); // Adjusted height
            this.DoubleBuffered = true;

            this.FormClosing += (sender, e) => Application.Exit();
            this.Shown += (sender, e) => Console.WriteLine("[VisualizationWindow]: Visualization started.");
        }

        public void UpdateVisualization(
            double xLeft, double yLeft, double[] pressuresLeft,
            double xRight, double yRight, double[] pressuresRight)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateVisualization(xLeft, yLeft, pressuresLeft, xRight, yRight, pressuresRight)));
                return;
            }

            // Update left sock data
            if (pressuresLeft.Length > 0)
            {
                copXLeft = xLeft;
                copYLeft = yLeft;
                sensorPressuresLeft = pressuresLeft;
                lastLeftDataUpdate = DateTime.Now;
            }

            // Update right sock data
            if (pressuresRight.Length > 0)
            {
                copXRight = xRight;
                copYRight = yRight;
                sensorPressuresRight = pressuresRight;
                lastRightDataUpdate = DateTime.Now;
            }

            Invalidate(); // Always trigger a repaint
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var graphics = e.Graphics;
            graphics.Clear(Color.White);

            bool hasLeftData = (DateTime.Now - lastLeftDataUpdate).TotalMilliseconds < DataTimeoutMilliseconds;
            bool hasRightData = (DateTime.Now - lastRightDataUpdate).TotalMilliseconds < DataTimeoutMilliseconds;
            //Console.WriteLine($"[Debug]:Pressures: {string.Join(", ", sensorPressuresLeft)}");


     if (hasLeftData)
    DrawSockVisualization(graphics, copXLeft, copYLeft, sensorPressuresLeft.Select(p => p * 10).ToArray(), SockSpacing / 2, false);
else
    DrawNoDataMessage(graphics, SockSpacing / 2);

if (hasRightData)
    DrawSockVisualization(graphics, copXRight, copYRight, sensorPressuresRight.Select(p => p * 10).ToArray(), SockSpacing + SockSpacing / 2, true);
else
    DrawNoDataMessage(graphics, SockSpacing + SockSpacing / 2);
        }

        private void DrawSockVisualization(Graphics graphics, double copX, double copY, double[] pressures, int xOffset, bool isRightSock)
        {
            // Draw grid lines with labels
            DrawGrid(graphics, xOffset);

            if (pressures.Length == 0)
                return;

            // Scale CoP to canvas
            float scaledX = (float)(xOffset + CanvasSize / 2 + copX * (CanvasSize / 8));
            float scaledY = (float)(CanvasSize / 2 - copY * (CanvasSize / 8));

            // Draw CoP with larger visibility
            graphics.FillEllipse(Brushes.Red, scaledX - 8, scaledY - 8, 16, 16);
            graphics.DrawString($"CoP X: {copX:F2}, Y: {copY:F2}", new Font("Arial", 10, FontStyle.Bold), Brushes.Black, scaledX + 10, scaledY);

            // Draw pressures
            DrawPressures(graphics, pressures, xOffset, isRightSock);
        }

        private void DrawGrid(Graphics graphics, int xOffset)
        {
            Pen gridPen = new Pen(Color.LightGray, 1);
            Font font = new Font("Arial", 8);
            Brush textBrush = Brushes.Black;

            // Draw center lines
            graphics.DrawLine(Pens.Gray, xOffset, CanvasSize / 2, xOffset + CanvasSize, CanvasSize / 2);
            graphics.DrawLine(Pens.Gray, xOffset + CanvasSize / 2, 0, xOffset + CanvasSize / 2, CanvasSize);

            // Draw grid markers
            for (int i = -3; i <= 3; i++)
            {
                int x = (int)(xOffset + CanvasSize / 2 + i * (CanvasSize / 8));
                int y = (int)(CanvasSize / 2 + i * (CanvasSize / 8));
                graphics.DrawLine(gridPen, x, CanvasSize / 2 - 5, x, CanvasSize / 2 + 5);
                graphics.DrawLine(gridPen, xOffset + CanvasSize / 2 - 5, y, xOffset + CanvasSize / 2 + 5, y);
                graphics.DrawString(i.ToString(), font, textBrush, x - 5, CanvasSize / 2 + 10);
                graphics.DrawString(i.ToString(), font, textBrush, xOffset + CanvasSize / 2 - 20, y - 5);
            }
        }

        private void DrawPressures(Graphics graphics, double[] pressures, int xOffset, bool isRightSock)
        {
            double[] XPositions = { 3.0, -3.0, 3.0, -3.0 };
            double[] YPositions = { 6.0, 6.0, -6.0, -6.0 };

    if (!isRightSock)
    {
        XPositions = XPositions.Select(x => -x).ToArray();
    }

    double maxPressure = pressures.Length > 0 ? pressures.Max() : 1.0;
    if (maxPressure <= 0.001) maxPressure = 1.0;

    for (int i = 0; i < pressures.Length; i++)
    {
        float intensity = (float)(pressures[i] / maxPressure);
        intensity = Math.Max(0, Math.Min(1, intensity));

        Color pressureColor = Color.FromArgb((int)(255 * intensity), 0, 0);
        float scaledX = (float)(xOffset + CanvasSize / 2 + XPositions[i] * (CanvasSize / 16));
        float scaledY = (float)(CanvasSize / 2 - YPositions[i] * (CanvasSize / 16));

        graphics.FillEllipse(new SolidBrush(pressureColor), scaledX - 12, scaledY - 12, 24, 24);
        graphics.DrawString($"S{i + 1}", new Font("Arial", 8), Brushes.Black, scaledX - 5, scaledY - 15);
    }
        }

        private void DrawNoDataMessage(Graphics graphics, int xOffset)
        {
            graphics.DrawString("No Data", new Font("Arial", 16, FontStyle.Bold), Brushes.Gray,
                new PointF(xOffset + CanvasSize / 4 - 50, CanvasSize / 2 - 20));
        }
    }
}
