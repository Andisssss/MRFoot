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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DAid.Clients
{
    public class Client
    {
        string hmdpath = "C:/Users/Lietotajs/Desktop/balls/OculusIntegration_trial.exe"; // change as needed
        string guipath = "C:/Users/Lietotajs/Desktop/Clientgui/bin/Debug/clientgui.exe"; // change as needed, need to run once gui alone
        string portFilePath = "C:/Users/Lietotajs/Desktop/Clientgui/bin/Debug/selected_ports.txt"; // change as needed

        private Process _hmdProcess;
        private readonly Server _server;
        private VisualizationWindow _visualizationWindow;
        private bool _isCalibrated = false;
        private bool _isVisualizing = false;

        private bool _bypassHMD = false; // New bypass flag

        // Internal sensor values (CoP)
        private double _copXLeft = 0, _copYLeft = 0;
        private double _copXRight = 0, _copYRight = 0;

private ExerciseData _currentExercise;
        private int _currentPhase;
        

        private TcpClient _hmdClient;
        private NetworkStream _hmdStream;

        private TcpClient _guiClient;
        private NetworkStream _guiStream;

        // New flag for exercise active state.

        // To store current exercise ID for feedback messages.
        private int currentExerciseID = 0;

        public Client(Server server)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Client started. Enter commands: connect, calibrate, start, stop, hmd, gui, exit");
            OpenGUI();
            _bypassHMD = true; // Bypass HMD connection for now

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("> ");
                string command = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrWhiteSpace(command))
                    continue;

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
                            await HandleStartCommand();
                            break;
                        case "stop":
                            HandleStopCommand();
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
            CloseHMD();
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
                    _server.HandlePortResponse(port1, port2);
                });

            Console.WriteLine("[Client]: HandleConnectCommandAsync completed.");
            File.Delete(portFilePath);
        }

        private async Task ListenForPortFileAsync(string filePath)
        {
            Console.WriteLine($"[Client]: Waiting for file '{filePath}'...");
            while (!File.Exists(filePath))
                await Task.Delay(500);
            Console.WriteLine($"[Client]: File '{filePath}' detected.");
        }

        private void HandleCalibrateCommand()
        {
            if (_isCalibrated)
            {
                Console.WriteLine("Sensors are already calibrated. Use 'start' to begin visualization.");
                SendMessageToGUI("Sensors are already calibrated. Use 'start' to begin visualization.");
                return;
            }
            Console.WriteLine("Requesting server to calibrate connected devices...");
            SendMessageToGUI("Requesting server to calibrate connected devices...");
            Console.WriteLine("[Calibration]: Stand with both feet. Lift each foot one at a time after 1 second.");
             SendMessageToGUI("[Calibration]: Stand with both feet. Lift each foot one at a time after 1 second.");
            
            _server.HandleCalibrateCommand();
            _isCalibrated = true;
            Console.WriteLine("Calibration completed. Use 'start' to begin visualization.");
        }

        private async Task HandleStartCommand()
        {
            if (!_isCalibrated)
            {
                Console.WriteLine("Calibration is required before starting visualization. Use 'calibrate' first.");
                SendMessageToGUI("Calibration is required before starting visualization. Use 'calibrate' first.");
                return;
            }
            if (_isVisualizing)
            {
                Console.WriteLine("Visualization is already running.");
                SendMessageToGUI("Visualization is already running.");
                return;
            }
            _server.StartDataStream();
            OpenVisualizationWindow();
            ConnectToHMD("127.0.0.1", 9001);
            SubscribeToDeviceUpdates();
            _isVisualizing = true;

            var exercises = ExerciseList.Exercises;
            var completedExerciseSets = new HashSet<int>();
            // repeating exercise groups after left and right
            var repeatSet = new Dictionary<int, List<int>>
    {
        { 2, new List<int> { 1, 2 } },  // Repeat 1 & 2 after 2
        { 10, new List<int> { 9, 10 } } // Repeat 9 & 10 after 10
    };

 for (int i = 0; i < exercises.Count; i++)
    {
        var exercise = exercises[i];
        int count = 0;
            
                
        SendExerciseConfiguration(exercise);
        if (i == 1 && count == 0)
            {
                count++;
                Thread.Sleep(1000); // sends left leg stance for exercise 1 and delays so the client isnt ahead
            }
         if (!completedExerciseSets.Contains(exercise.RepetitionID))
         {
              if (exercise.Intro > 0)
            {
                await Task.Delay(2000).ConfigureAwait(false);
                Console.WriteLine($"[Intro]: Waiting {exercise.Intro} sec...");
                await Task.Delay(exercise.Intro * 1000).ConfigureAwait(false);
            }

            if (exercise.Demo > 0)
            {
                Console.WriteLine($"[Demo]: Showing {exercise.Demo} sec...");
                await Task.Delay(exercise.Demo * 1000).ConfigureAwait(false);
            }
         }
         if (exercise.PreparationCop > 0)
            {
                await CheckPreparationCop(exercise.PreparationCop,exercise.LegsUsed);
            }
            
        await RunExerciseAsync(exercise).ConfigureAwait(false);
        if (repeatSet.TryGetValue(exercise.RepetitionID, out var repeatExercises) && !completedExerciseSets.Contains(exercise.RepetitionID))
        {
            completedExerciseSets.Add(exercise.RepetitionID);
            Console.WriteLine($"Repeating Exercises: {string.Join(", ", repeatExercises)}...");
            foreach (var repeatID in repeatExercises)
            {
                var repeatExercise = exercises.FirstOrDefault(e => e.RepetitionID == repeatID);
                
                if (repeatExercise != null)
                {
                    await CheckPreparationCop(repeatExercise.PreparationCop,repeatExercise.LegsUsed);
                    SendExerciseConfiguration(repeatExercise);
                    await RunExerciseAsync(repeatExercise).ConfigureAwait(false);
                }
            }
        }
    }
    Console.WriteLine("All exercises completed!");
    _isVisualizing = false;
}

    private async Task CheckPreparationCop(int duration, string activeLeg)
{
    Console.WriteLine($"[Preparation CoP]: Checking for {duration} sec (Active Leg: {activeLeg})...");
    DateTime startTime = DateTime.Now;
    
    while (true) 
    {
        bool isFootValid = false;
        (double Min, double Max) copRangeX = (-2.0, 2.0);
        (double Min, double Max) copRangeY = (-2.0, 2.0);

        double copXLeft = _copXLeft, copYLeft = _copYLeft;
        double copXRight = _copXRight, copYRight = _copYRight;

        if (activeLeg == "left")
        {
            isFootValid = copXLeft >= copRangeX.Min && copXLeft <= copRangeX.Max &&
                          copYLeft >= copRangeY.Min && copYLeft <= copRangeY.Max;
        }
        else if (activeLeg == "right")
        {
            isFootValid = copXRight >= copRangeX.Min && copXRight <= copRangeX.Max &&
                          copYRight >= copRangeY.Min && copYRight <= copRangeY.Max;
        }
        else if (activeLeg == "both") 
        {
            isFootValid = (copXLeft >= copRangeX.Min && copXLeft <= copRangeX.Max &&
                           copYLeft >= copRangeY.Min && copYLeft <= copRangeY.Max) &&
                          (copXRight >= copRangeX.Min && copXRight <= copRangeX.Max &&
                           copYRight >= copRangeY.Min && copYRight <= copRangeY.Max);
        }

        if (isFootValid)
        {
            if ((DateTime.Now - startTime).TotalSeconds >= duration)
            {
                Console.WriteLine($"[Preparation CoP]: {activeLeg} foot correctly positioned for the required time.");
                return;
            }
        }
        else
        {
            startTime = DateTime.Now; 
        }

        await Task.Delay(1000).ConfigureAwait(false); 
    }
}
        private async Task RunExerciseAsync(ExerciseData exercise)
        {
            if (exercise.RepetitionID == 1 || exercise.RepetitionID == 2 ){
                await Task.Delay(3000).ConfigureAwait(false);  // shows exercise  text for 3 seconds so both client and hmd wait
            }
            Console.WriteLine($"[Exercise]: {exercise.Name} started for {exercise.TimingCop} seconds...");
            SendMessageToGUI($"[Exercise]: {exercise.Name} started for {exercise.TimingCop} seconds...");
            DateTime exerciseStartTime = DateTime.Now;
            int phaseIndex = 0;
            List<int> previousZonesLeft = new List<int>();
            List<int> previousZonesRight = new List<int>();

            while ((DateTime.Now - exerciseStartTime).TotalSeconds < exercise.TimingCop)
            {
                var phase = exercise.ZoneSequence[phaseIndex];

                Console.WriteLine($"[Phase {phaseIndex + 1}]: {phase.Duration} sec");

                DateTime phaseStartTime = DateTime.Now;
                bool lostBalance = false;
                DateTime outOfZoneTimeLeft = DateTime.MinValue;
                DateTime outOfZoneTimeRight = DateTime.MinValue;
            List<int> currentZonesLeft = new List<int>();
       List<int> currentZonesRight = new List<int>();

                while ((DateTime.Now - phaseStartTime).TotalSeconds < phase.Duration &&
                       (DateTime.Now - exerciseStartTime).TotalSeconds < exercise.TimingCop)
                {
                    double copXLeft = _copXLeft, copYLeft = _copYLeft;
                    double copXRight = _copXRight, copYRight = _copYRight;
                                currentZonesLeft.Clear();
             currentZonesRight.Clear();

                    if (exercise.LegsUsed == "right")
                    {
                        currentZonesRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);

                    }
                    else if (exercise.LegsUsed == "left")
                    {
                        currentZonesLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);

                    }
                    else if (exercise.LegsUsed == "both")
                    {
                        if (exercise.RepetitionID == 5 || exercise.RepetitionID == 6 ){
    if (phaseIndex == 2 || phaseIndex == 3){
        currentZonesLeft = AddCopLeft(exercise, phaseIndex);
    }
                        currentZonesLeft = Feedback(copXLeft, copYLeft, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                        currentZonesRight = Feedback(copXRight, copYRight, phase.GreenZoneX, phase.GreenZoneY, phase.RedZoneX, phase.RedZoneY);
                    }
                    foreach (int zone in currentZonesLeft)
                    {
                        if (!previousZonesLeft.Contains(zone) && zone != 1 && zone != 7){
                            Console.WriteLine($"[Exercise]: Left Foot Changed to Zone {zone}");
                            SendMessageToGUI($"[Exercise]: Left Foot Changed to Zone {zone}");
                            SendFeedback(new List<int> { zone }, "Left");
                        }

                    }
                    foreach (int zone in currentZonesRight)
                    {
                         if (!previousZonesRight.Contains(zone) && zone != 1 && zone != 7){
                        Console.WriteLine($"[Exercise]: Right Foot Changed to Zone {currentZonesRight}");
                        SendMessageToGUI($"[Exercise]: Right Foot Changed to Zone {currentZonesRight}");

                        SendFeedback(new List<int> { zone }, "Right");
                        }
                    }
                              previousZonesLeft = new List<int>(currentZonesLeft);
          previousZonesRight = new List<int>(currentZonesRight);
                     if (currentZonesLeft.Contains(1) || currentZonesRight.Contains(1))
                        {
                            //Console.WriteLine($"[Exercise]: Right Foot Changed to Zone {zone}");
                            outOfZoneTimeLeft = DateTime.MinValue;
                            outOfZoneTimeRight = DateTime.MinValue;
                        }
                    else
                        {
                            if (currentZonesLeft.Count > 0 && outOfZoneTimeLeft == DateTime.MinValue)
                            {
                                outOfZoneTimeLeft = DateTime.Now;
                            }
                            if (currentZonesRight.Count > 0 && outOfZoneTimeRight == DateTime.MinValue)
                            {
                                outOfZoneTimeRight = DateTime.Now;
                            }
                        bool leftFootOutTooLong = (outOfZoneTimeLeft != DateTime.MinValue) &&
                                                  ((DateTime.Now - outOfZoneTimeLeft).TotalSeconds >= 4);
                        bool rightFootOutTooLong = (outOfZoneTimeRight != DateTime.MinValue) &&
                                                   ((DateTime.Now - outOfZoneTimeRight).TotalSeconds >= 4);

                        if (leftFootOutTooLong || rightFootOutTooLong)
                        {
                            lostBalance = true;
                            break;
                        }
                    }
                }

                if (lostBalance)
                {
                    Console.WriteLine("You lost balance, restarting exercise...");
                    SendMessageToGUI("You lost balance, restarting exercise...");
                    if (exercise.LegsUsed == "both" || exercise.LegsUsed == "left")
                    {
                        if (!previousZonesLeft.Contains(7))
                            {
                                SendFeedback(new List<int> { 7 }, "Left");
                            }
                    }
                    if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
                    {
                        if (!previousZonesRight.Contains(7))
                            {
                                SendFeedback(new List<int> { 7 }, "Right");
                            }
                    }
                    await Task.Delay(5000).ConfigureAwait(false);
                    exerciseStartTime = DateTime.Now;
                    continue;
                }
                phaseIndex++;
                if (phaseIndex >= exercise.ZoneSequence.Count)
                {
                    phaseIndex = 0;
                }
            }
           Console.WriteLine("[Client]: Put leg down");
           SendMessageToGUI("[Client]: Put leg down");
           
        await Task.Delay(exercise.Release*1000);

                if (exercise.LegsUsed == "both" || exercise.LegsUsed == "left")
                {
                var leftFeedbacks = previousZonesLeft
                        .Where(feedback => feedback != 1 && feedback != 7) // Ignore green zone & balance lost
                        .ToList();

                    if (leftFeedbacks.Any())
                    {
                        SendFeedback(leftFeedbacks, "Left");
                    }
                }
                if (exercise.LegsUsed == "both" || exercise.LegsUsed == "right")
                {
                    var rightFeedbacks = previousZonesRight
                        .Where(feedback => feedback != 1 && feedback != 7)
                        .ToList();

                    if (rightFeedbacks.Any())
                    {
                        SendFeedback(rightFeedbacks, "Right");
                    }
                }
            }
        }

        private List<int> Feedback(double copX, double copY,  
                          (double Min, double Max) greenZoneX, (double Min, double Max) greenZoneY, 
                          (double Min, double Max) redZoneX, (double Min, double Max) redZoneY)
{
    List<int> feedbacks = new List<int>();

    if (copX >= greenZoneX.Min && copX <= greenZoneX.Max &&
        copY >= greenZoneY.Min && copY <= greenZoneY.Max)
    {
        return new List<int> { 1 }; // Green Zone
    }

    if (copX >= redZoneX.Min && copX <= redZoneX.Max &&
        copY >= redZoneY.Min && copY <= redZoneY.Max)
    {
        feedbacks.Add(2); // Red Zone
    }
    if (copX < 0) feedbacks.Add(3); // Back
    if (copX > 0) feedbacks.Add(4); // Front
    if (copY > 0) feedbacks.Add(5); // Right
    if (copY < 0) feedbacks.Add(6); // Left

    return feedbacks.Count > 0 ? feedbacks : new List<int> { 0 };
}

                private List<int> AddCopLeft(ExerciseData exercise, int phaseIndex) //manually added cop zones for exercises 5&6, for lead/back leg
                {
                    var adjustedZones = new List<int>();
                    (int, (double, double), (double, double), (double, double), (double, double)) phaseData;

                    if (exercise.RepetitionID == 5 && phaseIndex == 2)
                    {
                        phaseData = (2, (-1.5, 1.5), (0.3, 5.5), (-2.0, 2.0), (0.0, 6.0));
                    }
                    else if (exercise.RepetitionID == 5 && phaseIndex == 3)
                    {
                        phaseData = (2, (-1.5, 1.5), (-3.0, 3.0), (-1.9, 1.9), (-5.0, 5.0));
                    }
                    else if (exercise.RepetitionID == 6 && phaseIndex == 2)
                    {
                        phaseData = (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0));
                    }
                    else if (exercise.RepetitionID == 6 && phaseIndex == 3)
                    {
                        phaseData = (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0));
                    }
                    return adjustedZones;
                }
        private void SendFeedback(List<int> feedbackCodes, string foot)
        {
            var feedbackMessage = new FeedbackMessage
            {
                MessageType = "Feedback",
                RepetitionID = currentExerciseID,
                Foot = foot,
                Zone = feedbackCodes
            };
            SendDataToHMD(feedbackMessage);
        }

        private void SendExerciseConfiguration(ExerciseData exercise)
        {
            Console.WriteLine($"[Feedback]: Sending exercise configuration for exercise {exercise.RepetitionID}");
            var configMessage = new ExerciseConfigMessage
            {
                MessageType = "ExerciseConfig",
                RepetitionID = exercise.RepetitionID,
                Name = exercise.Name,
                LegsUsed = exercise.LegsUsed,
                Intro = exercise.Intro,
                Demo = exercise.Demo,
                PreparationCop = exercise.PreparationCop,
                TimingCop = exercise.TimingCop,
                Release = exercise.Release,
                Sets = exercise.Sets,
                ZoneSequence = exercise.ZoneSequence
            };
            SendDataToHMD(configMessage); 
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
            CloseHMD();
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
         if ((_currentExercise?.RepetitionID == 5 || _currentExercise?.RepetitionID == 6) && _currentPhase == 4)
        {
            Console.WriteLine($"[Client]: Skipping CoP check for Exercise {_currentExercise.RepetitionID}, Phase 4.");
            return; 
        }
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
                visualizationThread.SetApartmentState(ApartmentState.STA);
                visualizationThread.IsBackground = true;
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
            if (_bypassHMD)
            {
                Console.WriteLine("HMD is bypassed. Commands disabled.");
                return;
            }
            Console.WriteLine("1. Connect to HMD\n2. Disconnect from HMD\n3. Exit HMD Menu");
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim();
            if (input == "1")
                ConnectToHMD("127.0.0.1", 9001);
            else if (input == "2")
                DisconnectFromHMD();
        }

       private void ConnectToHMD(string ipAddress, int port)
{
    if (_bypassHMD)
            {
                Console.WriteLine("HMD bypassed. Not connecting.");
                return;
            }
    try
    {
        // Ensure HMD application is running
        string processName = Path.GetFileNameWithoutExtension(hmdpath);
        Process[] hmdProcesses = Process.GetProcessesByName(processName);
        if (hmdProcesses.Length == 0)
        {
            Console.WriteLine("Starting HMD application...");
            try
            {
                _hmdProcess = Process.Start(hmdpath);
                Thread.Sleep(5000); // Give it time to start
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start HMD application: {ex.Message}");
                return;
            }
        }

        // Check if already connected (more robust check)
        if (_hmdClient != null && _hmdClient.Connected)
        {
            try
            {
                if (_hmdClient.Client.Poll(0, SelectMode.SelectRead) && _hmdClient.Client.Available == 0)
                {
                    Console.WriteLine("Connection lost. Reconnecting...");
                }
                else
                {
                    Console.WriteLine("Already connected to HMD.");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection check failed: {ex.Message}");
            }
        }

        // Attempt to connect with retries
        int retries = 4;
        int delay = 3000;
        for (int i = 0; i < retries; i++)
        {
            try
            {
                // Close previous failed client before retrying
                _hmdClient?.Close();
                _hmdClient = new TcpClient(ipAddress, port);
                _hmdStream = _hmdClient.GetStream();
                
                Console.WriteLine("HMD Connected.");
                return; // Connection successful
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection attempt {i + 1} failed: {ex.Message}. Retrying...");
                Thread.Sleep(delay);
            }
        }

        Console.WriteLine("Failed to connect to HMD after multiple attempts.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error connecting to HMD: {ex.Message}");
    }
}


        private void DisconnectFromHMD()
        {
            if (_bypassHMD)
            {
                Console.WriteLine("HMD bypassed. Not disconnecting.");
                return;
            }
            _hmdStream?.Close();
            _hmdClient?.Close();
            _hmdStream = null;
            _hmdClient = null;
            Console.WriteLine("HMD Disconnected.");
        }

        private void CloseHMD()
{
    if (_bypassHMD)
            {
                Console.WriteLine("HMD bypassed. Not closing.");
                return;
            }
    try
    {
        if (_hmdProcess != null && !_hmdProcess.HasExited)
        {
            _hmdStream?.Close();
            _hmdClient?.Close();
            _hmdStream = null;
            _hmdClient = null;
            Console.WriteLine("Closing HMD application...");
            _hmdProcess.Kill();  // Kill the process
            _hmdProcess.WaitForExit();  // Ensure the process has exited before continuing
            Console.WriteLine("HMD application closed.");
        }
        else
        {
            Console.WriteLine("HMD application is not running.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to close HMD application: {ex.Message}");
    }
}

        private void SendDataToHMD(object data)
        {
            if (_bypassHMD)
            {
                Console.WriteLine("HMD bypassed. Data not sent.");
                return;
            }
            try
            {
                if(data != null){
                      string jsonData = JsonSerializer.Serialize(data);
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);
                _hmdStream.Write(dataBytes, 0, dataBytes.Length);
                _hmdStream.Flush();  
                }else{
                    Console.WriteLine("no work");
    
                }
                
            }
            catch (Exception ex) { Console.WriteLine($"Error sending data: {ex.Message}"); }
        }

        //################################### GUI communication ########################################
        private void OpenGUI()
        {
            try
            {
                Process.Start(guipath);
                Console.WriteLine("GUI launched. Waiting for connection...");
                Thread.Sleep(2000);
                _guiClient = new TcpClient("127.0.0.1", 5555);
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
                        break;
                    }
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    Console.WriteLine($"[GUI]: {response}");
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
                        await HandleStartCommand();
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
                    break;
                }
            }
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

                Process[] processes = Process.GetProcessesByName("clientgui");
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

    // Message classes for JSON serialization
    public class FeedbackMessage
    {
        public string MessageType { get; set; }
        public int RepetitionID { get; set; }
        public string Foot { get; set; }
        public  List<int> Zone { get; set; }
    }
    

    public class ExerciseConfigMessage
    {
        public string MessageType { get; set; }
        public int RepetitionID { get; set; }
        public string Name { get; set; }
        public string LegsUsed { get; set; }
        public int Intro { get; set; }
        public int Demo { get; set; }
        public int PreparationCop { get; set; }
        public int TimingCop { get; set; }
        public int Release { get; set; }
        public int Switch { get; set; }
        public int Sets { get; set; }
        public System.Collections.Generic.List<ZoneSequenceItem> ZoneSequence { get; set; }
    }
}
