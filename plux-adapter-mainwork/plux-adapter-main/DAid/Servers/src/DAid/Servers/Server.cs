using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DAid.Servers
{
    public class Server
    {
        private readonly object syncLock = new object();
        public Manager Manager { get; }

        private string[] ports;
        private bool isRunning;
        private bool isAcquiringData;

        private bool isCalibrating = false; // Prevent multiple calibrations

        // Track connected devices and sensor adapters
        private readonly List<Device> connectedDevices = new List<Device>();
        private readonly List<SensorAdapter> sensorAdapters = new List<SensorAdapter>();

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
                connectedDevices.Clear();
                sensorAdapters.Clear();
            }
        }

                /// <summary>
        /// Connects to a sensor by scanning available COM ports.
        /// </summary>
       public void HandlePortResponse(string port1, string port2)
    {
        // Store the ports in the class-level array
        ports = new string[] { port1, port2 };
    }
     public string[] GetPorts()
    {
        return ports;
    }

public async Task HandleConnectCommandAsync(CancellationToken cancellationToken, Func<List<string>, Task> sendPortsToClient)
{
    // Call HandlePortResponse to get the ports array
    Console.WriteLine("[Server]: Scanning available COM ports...");
var ports = SensorAdapter.ScanPorts();
    

    Console.WriteLine("[Server]: Received COM ports: " + string.Join(", ", ports));

    // Send the list of available COM ports to the client
    await sendPortsToClient(ports.ToList());

string[] coms = GetPorts(); // Here you define the ports directly inside the method

  

    // Loop through the provided ports to connect devices
    for (int i = 0; i < coms.Length; i++)
    {
        
        string comPort = coms[i];

        if (!SensorAdapter.ScanPorts().Contains(comPort))
        {
            Console.WriteLine($"[Server]: Invalid COM port '{comPort}'. Skipping Device {i + 1}.");
            continue;
        }

        // Check if this port is already connected
        if (connectedDevices.Any(d => d.Path == comPort))
        {
            Console.WriteLine($"[Server]: Device on {comPort} is already connected. Skipping.");
            continue;
        }

        var connectedDevice = Manager.Connect(comPort);
        if (connectedDevice != null)
        {
            connectedDevices.Add(connectedDevice);
            sensorAdapters.Add(connectedDevice.SensorAdapter);

            // Log whether the device is a left or right sock
            Console.WriteLine($"[Server]: Device {connectedDevice.ModuleName} is a {(connectedDevice.IsLeftSock ? "Left" : "Right")} Sock.");
        }
        else
        {
            Console.WriteLine($"[Server]: Failed to connect to device on {comPort}.");
        }
    }

    Console.WriteLine("[Server]: All devices connected. Waiting for further commands.");
}

        /// <summary>
        /// Handles the calibrate command.
        /// </summary>
        public void HandleCalibrateCommand()
        {
            lock (syncLock)
            {
                if (!connectedDevices.Any())
                {
                    Console.WriteLine("[Server]: No devices connected. Use 'connect' command first.");
                    return;
                }

                if (isCalibrating)
                {
                    Console.WriteLine("[Server]: Calibration is already in progress.");
                    return;
                }

                if (!isAcquiringData) // Start the data stream if not already running
                {
                    StartDataStream();
                }

                isCalibrating = true; // Set calibration flag inside the lock
            }

            try
            {
                foreach (var device in connectedDevices)
                {
                    try
                    {
                        bool calibrationSuccessful = device.Calibrate();

                        if (calibrationSuccessful)
                        {
                            Console.WriteLine($"[Server]: Calibration completed successfully for device {device.ModuleName}.");
                        }
                        else
                        {
                            Console.WriteLine($"[Server]: Calibration failed for device {device.ModuleName}. No valid samples collected.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server]: Calibration failed for device {device.ModuleName}. Error: {ex.Message}");
                    }
                }
            }
            finally
            {
                lock (syncLock)
                {
                    isCalibrating = false; // Reset calibration flag inside the lock
                }
            }
        }

        /// <summary>
        /// Starts data acquisition for all connected devices.
        /// </summary>
        public void StartDataStream()
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

            foreach (var device in connectedDevices)
            {
                Console.WriteLine($"[Server]: Starting data stream for device {device.ModuleName}...");
                device.Start();
            }
        }

        /// <summary>
        /// Stops the data streams for all connected devices.
        /// </summary>
        public void StopDataStream()
        {
            lock (syncLock)
            {
                if (!isAcquiringData)
                {
                    Console.WriteLine("[Server]: No active data streams to stop.");
                    return;
                }

                Console.WriteLine("[Server]: Stopping data streams for all devices...");
                foreach (var device in connectedDevices)
                {
                    try
                    {
                        device.Stop();
                        Console.WriteLine($"[Server]: Data stream stopped for device {device.ModuleName}.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Server]: Failed to stop data stream for device {device.ModuleName}. Error: {ex.Message}");
                    }
                }

                isAcquiringData = false;
                Console.WriteLine("[Server]: All data streams stopped.");
            }
        }

        private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
        {
            if (sender is Device device)
            {
                string sockType = device.IsLeftSock ? "Left Sock" : "Right Sock";
                //Console.WriteLine($"[Server]: {sockType} CoP for {copData.DeviceName} -> X={copData.CoPX:F2}, Y={copData.CoPY:F2}, Pressures: {string.Join(", ", copData.Pressures.Select(p => p.ToString("F2")))}");
            }
            else
            {
                Console.WriteLine("[Server]: CoP update received from an unknown source.");
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