using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using NLog;

namespace DAid.Servers
{
    /// <summary>
    /// Manages communication and operations for all devices in the system.
    /// </summary>
    public class Manager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, Device> devices = new Dictionary<string, Device>();
        private readonly List<Device> activeDevices = new List<Device>(); // Store multiple active devices
        private readonly object syncLock = new object();

        /// <summary>
        /// Scans for available devices and adds them to the internal devices list.
        /// </summary>
        public void Scan()
        {
            try
            {
                var discoveredDevices = DiscoverDevices();

                lock (syncLock)
                {
                    foreach (var device in discoveredDevices)
                    {
                        if (!devices.ContainsKey(device.Path))
                        {
                            devices[device.Path] = device;
                            logger.Info($"[Manager]: Discovered new device: {device.Name} on {device.Path}");
                        }
                    }
                }

                logger.Info("[Manager]: Device scan completed.");
            }
            catch (Exception ex)
            {
                logger.Error($"[Manager]: Error during device scan: {ex.Message}");
            }
        }

        /// <summary>
        /// Connects to a specified device and adds it to the list of active devices.
        /// </summary>
       /// <summary>
/// Connects to a specified device and adds it to the list of active devices.
/// </summary>
public Device Connect(string path)
{
    lock (syncLock)
    {
        if (devices.TryGetValue(path, out var device))
        {
            try
            {
                device.Connect();
                if (!activeDevices.Contains(device))
                {
                    activeDevices.Add(device);
                    logger.Info($"[Manager]: Device {device.Name} on {device.Path} connected and added to active devices.");
                }
                else
                {
                    logger.Info($"[Manager]: Device {device.Name} on {device.Path} is already active.");
                }

                // Log whether the device is a left or right sock
                string sockType = device.IsLeftSock ? "Left Sock" : "Right Sock";
                logger.Info($"[Manager]: Device {device.Name} identified as {sockType}.");

                return device;
            }
            catch (Exception ex)
            {
                logger.Warn($"[Manager]: Failed to connect to device {device.Name}: {ex.Message}");
            }
        }
        else
        {
            logger.Warn($"[Manager]: No device found at path {path}.");
        }
        return null;
    }
}


        /// <summary>
        /// Retrieves the last connected device (backward compatibility).
        /// </summary>
        public Device GetActiveDevice()
        {
            lock (syncLock)
            {
                return activeDevices.LastOrDefault();
            }
        }

        /// <summary>
        /// Retrieves all currently connected active devices.
        /// </summary>
        public List<Device> GetConnectedDevices()
        {
            lock (syncLock)
            {
                return new List<Device>(activeDevices);
            }
        }

        /// <summary>
        /// Returns all registered devices.
        /// </summary>
        public IEnumerable<Device> GetAllDevices()
        {
            lock (syncLock)
            {
                return devices.Values.ToList();
            }
        }

        /// <summary>
        /// Cleans up and disconnects all devices.
        /// </summary>
        public void Cleanup()
        {
            logger.Info("[Manager]: Cleaning up devices...");

            lock (syncLock)
            {
                foreach (var device in activeDevices)
                {
                    try
                    {
                        logger.Info($"[Manager]: Disconnecting device: {device.Name} on {device.Path}");
                        device.Stop();
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"[Manager]: Failed to disconnect device: {device.Name}: {ex.Message}");
                    }
                }

                activeDevices.Clear();
                devices.Clear();
                Console.WriteLine("[Manager]: Cleanup completed.");
                logger.Info("[Manager]: Cleanup completed.");
            }
        }

        /// <summary>
        /// Discovers actual devices connected to COM ports but does not automatically connect them.
        /// </summary>
        private IEnumerable<Device> DiscoverDevices()
        {
            logger.Info("[Manager]: Discovering devices connected to COM ports...");

            var discoveredDevices = new List<Device>();
            var availablePorts = SerialPort.GetPortNames();

            if (availablePorts.Length == 0)
            {
                logger.Warn("[Manager]: No COM ports available.");
                return discoveredDevices;
            }

            foreach (var port in availablePorts)
            {
                logger.Info($"[Manager]: Attempting to create device for port {port}...");

                try
                {
                    // Use a default frequency (baud rate) as discovery does not connect automatically.
                    var device = new Device(port, 9600, $"Device on {port}");
                    discoveredDevices.Add(device);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Manager]: Failed to create device for port {port}: {ex.Message}");
                }
            }

            return discoveredDevices;
        }
    }
}
