using System;
using System.Threading;
using System.Threading.Tasks;

namespace DAid.Servers
{
    public class Server
    {
        private readonly object syncLock = new object();
        public Manager Manager { get; }
        private bool isRunning;
        private bool isAcquiringData;
        private SensorAdapter sensorAdapter = null;
private bool isSensorConnected = false;

        private bool isCalibrating = false; // Prevent multiple calibrations

        public Server()
        {
            Manager = new Manager();
        }

        /// <summary>
        /// Starts the server in a separate task, scanning for devices.
        /// </summary>
        public Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            lock (syncLock)
            {
                if (isRunning)
                {
                    Console.WriteLine("[Server]: Already running.");
                    return Task.CompletedTask;
                }

                isRunning = true;
            }

            return Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("[Server]: Starting... Scanning for devices...");
                    Manager.Scan();
                    Console.WriteLine("[Server]: Devices scanned. Ready for commands.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server]: Error during startup: {ex.Message}");
                    Stop();
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Stops the server and cleans up resources.
        /// </summary>
        public void Stop()
        {
            lock (syncLock)
            {
                if (!isRunning)
                {
                    Console.WriteLine("[Server]: Not running.");
                    return;
                }

                Console.WriteLine("[Server]: Stopping...");
                isRunning = false;
                isAcquiringData = false;

                Manager.Cleanup();

                if (sensorAdapter != null)
                {
                    try
                    {
                        sensorAdapter.Cleanup();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server]: Error during sensor cleanup: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Processes a command sent by the client.
        /// </summary>
        public async Task HandleCommandAsync(string command, CancellationToken cancellationToken)
        {
            Console.WriteLine($"[Server]: Command received: {command}");

            try
            {
                switch (command.ToLower())
                {
                    case "connect":
                        await HandleConnectCommandAsync(cancellationToken);
                        break;

                    case "start":
                        HandleStartVisualizationCommand();
                        break;

                    case "calibrate":
                        HandleCalibrateCommand();
                        break;

                    case "stop":
                        HandleStopCommand();
                        break;

                    case "exit":
                        HandleExitCommand();
                        break;

                    default:
                        Console.WriteLine("[Server]: Unknown command. Valid commands: connect, calibrate, start, stop, exit.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error processing command '{command}': {ex.Message}");
            }
        }

        /// <summary>
        /// Connects to a sensor by scanning available COM ports.
        /// </summary>
  public Task HandleConnectCommandAsync(CancellationToken cancellationToken)
{
    Console.WriteLine("[Server]: Scanning available COM ports...");
    var ports = SensorAdapter.ScanPorts();

    if (ports.Count == 0)
    {
        Console.WriteLine("[Server]: No available COM ports.");
        return Task.CompletedTask;
    }

    Console.WriteLine($"[Server]: Available COM ports: {string.Join(", ", ports)}");
    Console.Write("[Server]: Enter the COM port to connect to: ");
    string comPort = Console.ReadLine()?.Trim();

    if (!ports.Contains(comPort))
    {
        Console.WriteLine($"[Server]: Invalid COM port '{comPort}'. Aborting connection.");
        return Task.CompletedTask;
    }

    var connectedDevice = Manager.Connect(comPort);
    if (connectedDevice != null)
    {
        sensorAdapter = connectedDevice.SensorAdapter; // Assign the SensorAdapter
        isSensorConnected = true;                     // Mark the sensor as connected
        Console.WriteLine($"[Server]: Sensor connected on {comPort}. Waiting for further commands.");
    }
    else
    {
        Console.WriteLine($"[Server]: Failed to connect to sensor on {comPort}.");
    }

    return Task.CompletedTask;
}




private void HandleCalibrateCommand()
{
    lock (syncLock)
    {
        if (!isSensorConnected)
        {
            Console.WriteLine("[Server]: No sensor connected. Use 'connect' command first.");
            return;
        }

        if (isCalibrating)
        {
            Console.WriteLine("[Server]: Calibration is already in progress.");
            return;
        }

        if (!isAcquiringData) // Start the data stream if not already running
        {
            Console.WriteLine("[Server]: Starting data stream for calibration...");
            StartDataStream();
        }

        isCalibrating = true; // Set the flag to indicate calibration in progress
    }

    try
    {
        bool success = sensorAdapter.Calibrate();
        Console.WriteLine(success ? "[Server]: Sensor calibrated successfully." : "[Server]: Sensor calibration failed.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Server]: Error during calibration: {ex.Message}");
    }
    finally
    {
        lock (syncLock)
        {
            isCalibrating = false;
        }
    }
}



        /// <summary>
        /// Starts data acquisition from the sensor.
        /// </summary>
        private void StartDataStream()
{
    lock (syncLock)
    {
        if (isAcquiringData)
        {
            Console.WriteLine("[Server]: Data acquisition is already running.");
            return;
        }

        isAcquiringData = true;
    }
    try
    {
        sensorAdapter.StartSensorStream();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Server]: Error starting data stream: {ex.Message}");
        lock (syncLock) { isAcquiringData = false; }
    }
}

        /// <summary>
        /// Handles the start command to begin visualization.
        /// </summary>
        private void HandleStartVisualizationCommand()
        {
            lock (syncLock)
            {
                if (!isSensorConnected)
                {
                    Console.WriteLine("[Server]: No sensor connected. Use 'connect' command first.");
                    return;
                }

                if (!isAcquiringData)
                {
                    Console.WriteLine("[Server]: Data stream is not running. Use 'calibrate' first to start the stream.");
                    return;
                }

                Console.WriteLine("[Server]: Visualization will be handled by the client.");
            }
        }

        /// <summary>
        /// Stops data acquisition from the sensor.
        /// </summary>
        private void HandleStopCommand()
        {
            lock (syncLock)
            {
                if (!isSensorConnected || !isAcquiringData)
                {
                    Console.WriteLine("[Server]: No data acquisition in progress to stop.");
                    return;
                }

                isAcquiringData = false;
            }

            Console.WriteLine("[Server]: Stopping data stream...");
            try
            {
                sensorAdapter.StopSensorStream();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server]: Error stopping data stream: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the server and exits.
        /// </summary>
        private void HandleExitCommand()
        {
            Console.WriteLine("[Server]: Exiting and cleaning up resources...");
            Stop();
        }
    }
}
