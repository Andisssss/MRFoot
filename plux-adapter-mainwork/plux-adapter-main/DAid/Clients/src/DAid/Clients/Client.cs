using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Net.Sockets;
using DAid.Servers;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.Runtime.InteropServices;

namespace DAid.Clients
{
    public class Client
    {
        
       string guipath= "C:/Users/Lietotajs/Desktop/Clientgui/bin/Debug/clientgui.exe"; //change
        string portFilePath = "C:/Users/Lietotajs/Desktop/Clientgui/bin/Debug/selected_ports.txt"; //change
        private readonly Server _server;
        private VisualizationWindow _visualizationWindow;
        private bool _isCalibrated = false;
        private bool _isVisualizing = false;

        private double _copXLeft = 0, _copYLeft = 0;
        private double _copXRight = 0, _copYRight = 0;

        private TcpClient _hmdClient;
        private NetworkStream _hmdStream;

        private TcpClient _guiClient;
        private NetworkStream _guiStream;

        public Client(Server server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Client started. Enter commands: connect, calibrate, start, stop, hmd, gui, exit");
            OpenGUI();

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
                            HandleCalibrateCommand();
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
                        case "gui":
                             OpenGUI();
                            break;
                        case "exit": 
                        HandleExitCommand();
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

 private void HandleExitCommand()
        {
            Console.WriteLine("Stopping client...");
                    _server.Stop();
                    DisconnectFromHMD();
                     CloseGUI();
        }
                   
      private async Task HandleConnectCommandAsync(CancellationToken cancellationToken)
{
    Console.WriteLine("[Client]: Requesting available COM ports from the server...");

    await _server.HandleConnectCommandAsync(
        cancellationToken,
        async ports =>
        {
            string portsList = string.Join(",", ports);
            SendMessageToGUI(portsList);

            string filePath = portFilePath;
            Console.WriteLine("[Client]: Waiting for selected ports file...");
            await ListenForPortFileAsync(filePath);

            string fileContent = File.ReadAllText(filePath).Trim();
            if (string.IsNullOrEmpty(fileContent))
            {
                Console.WriteLine("[Client]: File is empty. No ports to send.");
                return;
            }

            string[] portArray = fileContent.Split(',');
            if (portArray.Length < 2)
            {
                Console.WriteLine("[Client]: Error - Expected two ports.");
                return;
            }

            string port1 = portArray[0].Trim();
            string port2 = portArray[1].Trim();
            Console.WriteLine($"[Client]: Chosen ports: {port1} and {port2}");

            // Send the selected ports directly to the server
            _server.HandlePortResponse(port1, port2);
        });

    // After completing the connection task, ensure we keep listening for further GUI responses
    Console.WriteLine("[Client]: HandleConnectCommandAsync completed.");
    File.Delete(portFilePath);
}


private async Task ListenForPortFileAsync(string filePath)
{
    Console.WriteLine($"[Client]: Waiting for file '{filePath}'...");
    while (!File.Exists(filePath))
    {
        await Task.Delay(500);
    }
    Console.WriteLine($"[Client]: File '{filePath}' detected.");
}


        private void HandleCalibrateCommand()
        {
            if (_isCalibrated)
            {
                Console.WriteLine("Sensors are already calibrated. Use 'start' to begin visualization.");
                return;
            }

            Console.WriteLine("Requesting server to calibrate connected devices...");
            _server.HandleCalibrateCommand();
            _isCalibrated = true;

            Console.WriteLine("Calibration completed. Use 'start' to begin visualization.");
        }

         private async Task HandleStartCommand()
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
    var exercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == 1);
    if (exercise == null)
    {
        Console.WriteLine("Error: Exercise not found!");
        return;
    }
    OpenVisualizationWindow();
    SubscribeToDeviceUpdates();
    _isVisualizing = true;
    await RunExerciseAsync(exercise);
}

private async Task RunExerciseAsync(ExerciseData exercise)
{
    DateTime startTime = DateTime.Now;
    DateTime outOfZoneTime = DateTime.MinValue;
    bool lostBalance = false;
    DateTime lastRedZoneWarningTime = DateTime.MinValue;
    bool wasInGreenZone = false;
    bool wasInRedZone = false;

    Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.Timing} seconds...");

    while ((DateTime.Now - startTime).TotalSeconds < exercise.Timing)
    {
        if (_visualizationWindow == null || _visualizationWindow.IsDisposed) break;

        bool isInGreenZone = false;
        bool isInRedZone = false;

        if (exercise.LegsUsed.Contains("right"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXRight, _copYRight);
            isInRedZone = exercise.IsInRedZone(_copXRight, _copYRight);
        }
        else if (exercise.LegsUsed.Contains("left"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXLeft, _copYLeft);
            isInRedZone = exercise.IsInRedZone(_copXLeft, _copYLeft);
        }
        else if (exercise.LegsUsed.Contains("both"))
        {
            isInGreenZone = exercise.IsInGreenZone(_copXLeft, _copYLeft) && exercise.IsInGreenZone(_copXRight, _copYRight);
            isInRedZone = exercise.IsInRedZone(_copXLeft, _copYLeft) || exercise.IsInRedZone(_copXRight, _copYRight);
        }

        if (isInGreenZone && !wasInGreenZone)
        {
            Console.WriteLine("Entered Green Zone");
            wasInGreenZone = true;
            wasInRedZone = false;
        }
        else if (isInRedZone && !wasInRedZone)
        {
            Console.WriteLine("Entered Red Zone. Center your leg.");
            wasInRedZone = true;
            wasInGreenZone = false;
        }

        if (isInGreenZone)
        {
            outOfZoneTime = DateTime.MinValue;
        }
        else
        {
            if (outOfZoneTime == DateTime.MinValue)
            {
                outOfZoneTime = DateTime.Now;
            }
            else if ((DateTime.Now - outOfZoneTime).TotalSeconds >= 4)
            {
                lostBalance = true;
                break;
            }
        }
    }

    if (lostBalance)
    {
        Console.WriteLine("You lost balance, exercise restarts in 5 seconds...");
        await Task.Delay(5000);
    
        Console.WriteLine($"Restarting {exercise.Name}...");
        await RunExerciseAsync(exercise);
    }
    else
    {
        Console.WriteLine("Good work! Now is a pause for 15 seconds.");
        await Task.Delay(15000);

        int nextExerciseID = exercise.ExerciseID + 1;
        var nextExercise = ExerciseList.Exercises.FirstOrDefault(e => e.ExerciseID == nextExerciseID);
        if (nextExercise != null)
        {
            Console.WriteLine($"➡️ Starting next exercise: {nextExercise.Name}");
            await RunExerciseAsync(nextExercise);
        }
        else
        {
            Console.WriteLine("🎉 All exercises completed! Well done.");
            _isVisualizing = false;
        }
    }
}

       private void HandleStopCommand()
        {
            if (!_isVisualizing)
            {
                Console.WriteLine("[Client]: Visualization is not running.");
                return;
            }
            Console.WriteLine("[Client]: Stopping visualization and data streams...");
            _server.StopDataStream();
            CloseVisualizationWindow();
            _isVisualizing = false;
            Console.WriteLine("[Client]: Visualization and data streams stopped.");
        }

        private void SubscribeToDeviceUpdates()
        {
            var activeDevices = _server.Manager.GetConnectedDevices();

            if (!activeDevices.Any())
            {
                Console.WriteLine("[Client]: No active devices to subscribe to.");
                return;
            }

            foreach (var device in activeDevices)
            {
                device.CoPUpdated -= OnCoPUpdated;
                device.CoPUpdated += OnCoPUpdated;
            }
        }

       private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
{
    if (_visualizationWindow == null || _visualizationWindow.IsDisposed) return;

    if (sender is Device device)
    {
        if (device.IsLeftSock)
        {
            _copXLeft = copData.CoPX;
            _copYLeft = copData.CoPY;
        }
        else
        {
            _copXRight = copData.CoPX;
            _copYRight = copData.CoPY;
        }

        _visualizationWindow.UpdateVisualization(
            xLeft: _copXLeft,
            yLeft: _copYLeft,
            pressuresLeft: device.IsLeftSock ? copData.Pressures : Array.Empty<double>(),
            xRight: _copXRight,
            yRight: _copYRight,
            pressuresRight: !device.IsLeftSock ? copData.Pressures : Array.Empty<double>()
        );
    }
}

      private void OpenVisualizationWindow()
{
    if (_visualizationWindow == null || _visualizationWindow.IsDisposed)
    {
        Thread visualizationThread = new Thread(() =>
        {
            _visualizationWindow = new VisualizationWindow();
            System.Windows.Forms.Application.Run(_visualizationWindow);
        });

        visualizationThread.SetApartmentState(ApartmentState.STA); // Set STA mode for Windows Forms
        visualizationThread.IsBackground = true; // Allows process to exit properly
        visualizationThread.Start();
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
//################################### HMD ########################################
        private void HandleHMDCommand()
        {
            Console.WriteLine("1. Connect to HMD\n2. Disconnect from HMD\n3. Exit HMD Menu");
            Console.Write("> ");

            string input = Console.ReadLine()?.Trim();
            if (input == "1") ConnectToHMD("127.0.0.1", 9001);
            else if (input == "2") DisconnectFromHMD();
        }

        private void ConnectToHMD(string ipAddress, int port)
        {
            try
            {
                if (_hmdClient != null && _hmdClient.Connected) return;
                _hmdClient = new TcpClient(ipAddress, port);
                _hmdStream = _hmdClient.GetStream();
                Console.WriteLine("HMD Connected.");
            }
            catch (Exception ex) { Console.WriteLine($"Error: {ex.Message}"); }
        }

        private void DisconnectFromHMD()
        {
            _hmdStream?.Close();
            _hmdClient?.Close();
            _hmdStream = null;
            _hmdClient = null;
            Console.WriteLine("HMD Disconnected.");
        }

        private void SendDataToHMD(object copData)
        {
            try
            {
                string jsonData = JsonSerializer.Serialize(copData);
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
                _hmdStream.Write(dataBytes, 0, dataBytes.Length);
                _hmdStream.Flush();
            }
            catch (Exception ex) { Console.WriteLine($"Error sending data: {ex.Message}"); }
        }

        //########################### GUI communication ############################
        private void OpenGUI()
{
    try
    {
        Process.Start(guipath);
        Console.WriteLine("GUI launched. Waiting for connection...");

        Thread.Sleep(2000); // Ensure the GUI starts

        _guiClient = new TcpClient("127.0.0.1", 5555); // Connect to the GUI server
        _guiStream = _guiClient.GetStream();
        Console.WriteLine("Connected to GUI.");

        Task.Run(() => ListenForGUIResponses());
        SendMessageToGUI("connect");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to connect to GUI: {ex.Message}");
    }
}

      private async Task ListenForGUIResponses()
{
    byte[] buffer = new byte[1024];

    while (_guiClient?.Connected == true)
    {
        try
        {
            int bytesRead = await _guiStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("[Client]: Connection closed by GUI.");
                break; // The connection has been closed, stop listening.
            }

            string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
            Console.WriteLine($"[GUI]: {response}");

            // Handle specific response messages
            if (response.ToLower() == "connect")
            {
                Console.WriteLine("[Client]: Connect command received from GUI.");
                await HandleConnectCommandAsync(CancellationToken.None);
            }
            if (response.ToLower() == "calibrate")
            {
                Console.WriteLine("[Client]: Calibrate command received from GUI.");
                 HandleCalibrateCommand();
            }
            if (response.ToLower() == "start")
            {
                Console.WriteLine("[Client]: Start command received from GUI.");
                 HandleStartCommand();
            }
            if (response.ToLower() == "stop")
            {
                
                HandleStopCommand();
                Console.WriteLine("[Client]: Stop command received from GUI.");
                 
            }
            if (response.ToLower() == "hmd")
            {
                Console.WriteLine("[Client]: HMD command received from GUI.");
                 HandleHMDCommand();
            }
            if (response.ToLower() == "1")
            {
                Console.WriteLine("[Client]: 1 command received from GUI.");
                 ConnectToHMD("127.0.0.1", 9001);
            }
            if (response.ToLower() == "2")
            {
                Console.WriteLine("[Client]: 2 command received from GUI.");
                 DisconnectFromHMD();
            }
            if (response.ToLower() == "exit")
            {
                Console.WriteLine("[Client]: Exit command received from GUI.");
                
                HandleExitCommand();
                Environment.Exit(0);
               
                
            }
            else
            {
                Console.WriteLine("[Client]: Unrecognized message from GUI.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while listening for GUI responses: {ex.Message}");
            break; // If error occurs, stop listening
        }
    }

    // Ensure the client disconnects properly
    Console.WriteLine("[Client]: Stopped listening for GUI responses.");
}



        private void SendMessageToGUI(string message)
        {
            try
            {
                if (_guiClient == null || !_guiClient.Connected)
                {
                    Console.WriteLine("Not connected to GUI.");
                    return;
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                _guiStream.Write(messageBytes, 0, messageBytes.Length);
                _guiStream.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message to GUI: {ex.Message}");
            }
        }

private void SendCommandToServer(string command)
{
    Console.WriteLine(command);  // Server will read this via Console.ReadLine()
    Console.Out.Flush(); // Ensures the command is sent immediately
    Console.WriteLine($"[Client]: Sent command to server: {command}");
}
// private void ReconnectToServer()
// {
//     try
//     {
//         if (_guiClient == null || !_guiClient.Connected)
//         {
//             Console.WriteLine("[Client]: Attempting to reconnect...");
//             _guiClient = new TcpClient("127.0.0.1", 5555);
//             _guiStream = _guiClient.GetStream();
//             Console.WriteLine("[Client]: Reconnected to GUI.");
//             Task.Run(() => ListenForGUIResponses());
//         }
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"Failed to reconnect: {ex.Message}");
//     }
// }

private void CloseGUI()
{
    try
    {
        if (_guiClient?.Connected == true)
        {
            _guiStream?.Close();
            _guiClient?.Close();
            Console.WriteLine("[Client]: Disconnected from GUI.");
        }

        // Optionally, close the GUI process itself if necessary
        Process[] processes = Process.GetProcessesByName("clientgui");  // The name of the GUI process
        foreach (var process in processes)
        {
            process.Kill();
            Console.WriteLine("[Client]: GUI process terminated.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Client]: Error closing GUI: {ex.Message}");
    }
}




    }
}
