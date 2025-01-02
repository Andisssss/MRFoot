using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Net.Sockets;
using DAid.Servers;
using System.Linq;
using System.Text.Json;

namespace DAid.Clients
{
    public class Client
    {
        private readonly Server _server;
        private VisualizationWindow _visualizationWindow;
        private bool _isCalibrated = false;
        private bool _isVisualizing = false;
        private TcpClient _hmdClient;
        private NetworkStream _hmdStream;

        public Client(Server server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Client started. Enter commands: connect, calibrate, start, stop, hmd, exit");

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower();

                if (string.IsNullOrWhiteSpace(command)) continue;

                if (command == "exit")
                {
                    Console.WriteLine("Stopping client...");
                    _server.Stop();
                    DisconnectFromHMD();
                    return;
                }

                try
                {
                    switch (command)
                    {
                        case "connect":
                            await HandleConnectCommandAsync(cancellationToken);
                            break;
                        case "calibrate":
                            await HandleCalibrateCommandAsync(cancellationToken);
                            break;
                        case "start":
                            HandleStartCommand();
                            break;
                        case "stop":
                            HandleStopCommand();
                            break;
                        case "hmd":
                            HandleHMDCommand();
                            break;
                        default:
                            Console.WriteLine("Unknown command. Valid commands: connect, calibrate, start, stop, hmd, exit.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing command '{command}': {ex.Message}");
                }
            }
        }

        private async Task HandleConnectCommandAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Connecting to the server...");
            await _server.HandleCommandAsync("connect", cancellationToken);
            Console.WriteLine("Connection completed. Use 'calibrate' to start calibration.");
        }

        private async Task HandleCalibrateCommandAsync(CancellationToken cancellationToken)
        {
            if (_isCalibrated)
            {
                Console.WriteLine("Sensors are already calibrated. Use 'start' to begin visualization.");
                return;
            }

            Console.WriteLine("Calibrating sensors...");
            await _server.HandleCommandAsync("calibrate", cancellationToken);

            _isCalibrated = true;
            Console.WriteLine("Calibration completed. Data stream will remain active. Use 'start' to begin visualization.");
        }

        private void HandleStartCommand()
        {
            if (!_isCalibrated)
            {
                Console.WriteLine("Calibration is required before starting visualization. Use 'calibrate' first.");
                return;
            }

            if (_isVisualizing)
            {
                Console.WriteLine("Visualization is already running.");
                return;
            }

            Console.WriteLine("Starting visualization...");
            OpenVisualizationWindow();
            SubscribeToDeviceUpdates();
            _isVisualizing = true;
        }

        private void HandleStopCommand()
        {
            if (!_isVisualizing)
            {
                Console.WriteLine("Visualization is not running.");
                return;
            }

            Console.WriteLine("Stopping visualization...");
            CloseVisualizationWindow();
            _isVisualizing = false;
        }

        private void HandleHMDCommand()
        {
            Console.WriteLine("HMD Command: Choose an option:");
            Console.WriteLine("1. Connect to HMD");
            Console.WriteLine("2. Send Test Data to HMD");
            Console.WriteLine("3. Disconnect from HMD");

            Console.Write("> ");
            string input = Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    ConnectToHMD("127.0.0.1", 9001); // Localhost for HMD
                    break;

                case "2":
                    SendTestDataToHMD();
                    break;

                case "3":
                    DisconnectFromHMD();
                    break;

                default:
                    Console.WriteLine("Invalid choice. Valid options: 1, 2, 3.");
                    break;
            }
        }

        private void OpenVisualizationWindow()
        {
            if (_visualizationWindow == null || _visualizationWindow.IsDisposed)
            {
                _visualizationWindow = new VisualizationWindow();
                Task.Run(() => System.Windows.Forms.Application.Run(_visualizationWindow));
            }
        }

        private void CloseVisualizationWindow()
        {
            if (_visualizationWindow != null && !_visualizationWindow.IsDisposed)
            {
                _visualizationWindow.Invoke(new Action(() => _visualizationWindow.Close()));
                _visualizationWindow = null;
            }
        }

        private void SubscribeToDeviceUpdates()
        {
            var activeDevice = _server.Manager.GetActiveDevice();

            if (activeDevice == null)
            {
                Console.WriteLine("[Client]: No active device to subscribe to.");
                return;
            }


            activeDevice.CoPUpdated -= OnCoPUpdated;
            activeDevice.CoPUpdated += OnCoPUpdated;

            Console.WriteLine($"[Client]: Subscribed to CoP updates for Device: {activeDevice.Name}");
        }

       private void OnCoPUpdated(object sender, (double CoPX, double CoPY, double[] Pressures) copData)
{
    // Validate CoPX and CoPY to ensure they are valid numbers
    if (double.IsNaN(copData.CoPX) || double.IsInfinity(copData.CoPX))
    {
        Console.WriteLine($"[Warning]: Invalid CoPX value detected: {copData.CoPX}. Skipping update.");
        return;
    }

    if (double.IsNaN(copData.CoPY) || double.IsInfinity(copData.CoPY))
    {
        Console.WriteLine($"[Warning]: Invalid CoPY value detected: {copData.CoPY}. Skipping update.");
        return;
    }

    // Validate Pressures array to ensure no invalid values
    if (copData.Pressures == null || copData.Pressures.Any(double.IsNaN) || copData.Pressures.Any(double.IsInfinity))
    {
        Console.WriteLine($"[Warning]: Invalid pressures array detected. Skipping update.");
        return;
    }

    // Update visualization window if available
    if (_visualizationWindow != null && !_visualizationWindow.IsDisposed)
    {
        _visualizationWindow.Invoke(new Action(() =>
        {
            _visualizationWindow.UpdateVisualization(copData.CoPX, copData.CoPY, copData.Pressures);
        }));
    }

    // Send data to the HMD if connected
    if (_hmdStream != null && _hmdClient.Connected)
    {
        try
        {
            // Serialize and send only valid data
            string jsonData = JsonSerializer.Serialize(new
            {
                CoPX = copData.CoPX,
                CoPY = copData.CoPY,
                Pressures = copData.Pressures
            });

            byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
            _hmdStream.Write(dataBytes, 0, dataBytes.Length);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending data to HMD: {ex.Message}");
        }
    }
}


        private void ConnectToHMD(string ipAddress, int port)
        {
            try
            {
                if (_hmdClient != null && _hmdClient.Connected)
                {
                    Console.WriteLine("Already connected to HMD.");
                    return;
                }

                Console.WriteLine($"Attempting to connect to HMD at {ipAddress}:{port}...");
                _hmdClient = new TcpClient(ipAddress, port);
                _hmdStream = _hmdClient.GetStream();
                Console.WriteLine($"Successfully connected to HMD at {ipAddress}:{port}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to HMD: {ex.Message}");
            }
        }

private void SendTestDataToHMD()
{
    if (_hmdStream == null || !_hmdClient.Connected)
    {
        Console.WriteLine("HMD is not connected. Use 'hmd -> 1' to connect.");
        return;
    }

    try
    {
        // Replace with valid test data
        var testData = new
        {
            CoPX = 0.5,
            CoPY = -0.5,
            Pressures = new double[] { 1.0, 2.0, 3.0, 4.0 }
        };

        string jsonData = JsonSerializer.Serialize(testData);
        byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
        _hmdStream.Write(dataBytes, 0, dataBytes.Length);

        Console.WriteLine($"Test data sent to HMD: {jsonData}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending test data to HMD: {ex.Message}");
    }
}


        // private void SendTestDataToHMD() //real one
        // {
        //     if (_hmdStream == null || !_hmdClient.Connected)
        //     {
        //         Console.WriteLine("HMD is not connected. Use 'hmd -> 1' to connect.");
        //         return;
        //     }

        //     try
        //     {
        //         string testData = System.Text.Json.JsonSerializer.Serialize(new
        //         {
        //             Message = "Test Data",
        //             Timestamp = DateTime.Now
        //         });

        //         byte[] dataBytes = Encoding.UTF8.GetBytes(testData);
        //         _hmdStream.Write(dataBytes, 0, dataBytes.Length);
        //         Console.WriteLine("Test data sent to HMD.");
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Error sending test data to HMD: {ex.Message}");
        //     }
        // }

        private void DisconnectFromHMD()
        {
            try
            {
                _hmdStream?.Close();
                _hmdClient?.Close();
                _hmdStream = null;
                _hmdClient = null;

                Console.WriteLine("HMD disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting HMD: {ex.Message}");
            }
        }
    }
}
