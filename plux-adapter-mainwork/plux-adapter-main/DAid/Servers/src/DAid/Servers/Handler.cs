using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using NLog;

namespace DAid.Servers
{
    /// <summary>
    /// Manages communication between connected clients and DAid sensors.
    /// </summary>
    public sealed class Handler
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly Server server;
        private readonly MemoryStream stream;
        private readonly CancellationToken token;

        private readonly Dictionary<Device, Cache> devices = new Dictionary<Device, Cache>();
        private readonly object syncLock = new object();

        /// <summary>
        /// Represents a buffer cache for a specific device.
        /// </summary>
        private sealed class Cache
        {
            public readonly byte[] offsets;
            public readonly byte[] buffer;

            public Cache(byte index, byte[] offsets)
            {
                this.offsets = offsets;
                this.buffer = new byte[offsets.Sum(offset => offset) + 5];
                this.buffer[0] = index; // Assign device index at the start of the buffer
            }
        }

        public Handler(Server server, MemoryStream stream, CancellationToken token)
        {
            this.server = server ?? throw new ArgumentNullException(nameof(server));
            this.stream = stream ?? throw new ArgumentNullException(nameof(stream));
            this.token = token;
        }

        /// <summary>
        /// Initiates communication and negotiates requested devices.
        /// </summary>
        public async Task StartAsync()
        {
            try
            {
                Console.WriteLine("[Handler]: Handler started.");
                logger.Info("[Handler]: Handler started.");

                // Read the size of the message
                byte[] sizeBuffer = new byte[1];
                await stream.ReadAsync(sizeBuffer, 0, 1, token);
                int messageSize = sizeBuffer[0];
                Console.WriteLine($"[Handler]: Message size received: {messageSize}");

                // Read the device request message
                byte[] buffer = new byte[messageSize];
                await stream.ReadAsync(buffer, 0, messageSize, token);
                string[] requestedPaths = Encoding.ASCII.GetString(buffer)
                    .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine($"[Handler]: Requested device paths: {string.Join(", ", requestedPaths)}");

                // Retrieve and register devices
                IEnumerable<Device> requestedDevices = GetRequestedDevices(requestedPaths);
                RegisterDevices(requestedDevices);

                // Prepare and send the response buffer
                byte[] response = PrepareResponseBuffer(requestedDevices);
                await stream.WriteAsync(response, 0, response.Length, token);
                Console.WriteLine($"[Handler]: Response buffer sent to client.");

                Console.WriteLine("[Handler]: Subscribing to device events...");
                foreach (var device in requestedDevices)
                {
                    device.RawDataReceived += OnRawDataReceived;
                    device.CoPUpdated += OnCoPUpdated;
                    Console.WriteLine($"[Handler]: Subscribed to events for device {device.Name}");
                }

                Console.WriteLine("[Handler]: Handler setup complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler]: Handler failed to start: {ex.Message}");
                logger.Error($"[Handler]: Handler failed to start: {ex.Message}");
                Stop();
            }
        }

        /// <summary>
        /// Stops the handler and disconnects clients.
        /// </summary>
        public void Stop()
{
    lock (syncLock)
    {
        Console.WriteLine("[Handler]: Stopping handler.");
        logger.Info("[Handler]: Stopping handler.");

        foreach (var device in devices.Keys)
        {
            device.RawDataReceived -= OnRawDataReceived;
            device.CoPUpdated -= OnCoPUpdated; // Unsubscribe from CoP updates
        }

        devices.Clear();
        logger.Info("[Handler]: Handler stopped and devices cleared.");
        Console.WriteLine("[Handler]: Handler stopped and devices cleared.");
    }
}


        /// <summary>
        /// Handles raw data received from devices.
        /// </summary>
        private void OnRawDataReceived(object sender, string rawData)
        {
            if (sender is Device device && devices.ContainsKey(device))
            {
                Console.WriteLine($"[Handler]: Raw data received from device {device.Name}: {rawData}");
            }
        }

        /// <summary>
        /// Handles CoP and pressure data updates from devices.
        /// </summary>
        private void OnCoPUpdated(object sender, (string DeviceName, double CoPX, double CoPY, double[] Pressures) copData)
{
    if (sender is Device device)
    {
        lock (syncLock)
        {
            string pressures = string.Join(", ", copData.Pressures.Select(p => p.ToString("F2")));
            string sockType = device.IsLeftSock ? "Left Sock" : "Right Sock";

            Console.WriteLine($"[Handler]: {sockType} - Device {copData.DeviceName} -> CoP: X={copData.CoPX:F2}, Y={copData.CoPY:F2}, Pressures: {pressures}");
        }
    }
    else
    {
        Console.WriteLine("[Handler]: CoP update received from an unknown source.");
    }
}



        /// <summary>
        /// Prepares the response buffer with device information.
        /// </summary>
        private byte[] PrepareResponseBuffer(IEnumerable<Device> devices)
        {
            var response = new List<byte>();
            foreach (var device in devices)
            {
                response.AddRange(Encoding.ASCII.GetBytes(device.Path));
                response.Add(0); // Null terminator
                response.AddRange(BitConverter.GetBytes((int)device.Frequency));
            }

            return response.ToArray();
        }

        /// <summary>
        /// Retrieves the requested devices from the server's manager.
        /// </summary>
        private IEnumerable<Device> GetRequestedDevices(string[] paths)
        {
            return paths.Length == 0 
                ? server.Manager.GetAllDevices()
                : paths.Select(path => server.Manager.GetAllDevices().FirstOrDefault(d => d.Path == path))
                       .Where(device => device != null);
        }

        /// <summary>
        /// Registers devices with internal caches and connects them.
        /// </summary>
        private void RegisterDevices(IEnumerable<Device> devicesToRegister)
{
    foreach (var device in devicesToRegister)
    {
        if (!devices.ContainsKey(device))
        {
            devices[device] = new Cache(0, new byte[0]);
            Console.WriteLine($"[Handler]: Registering and connecting device at {device.Path}");

            device.Connect();

            // Subscribe to raw data and CoP updates
            device.RawDataReceived += OnRawDataReceived;
            device.CoPUpdated += OnCoPUpdated;

            // Log device information after connection
            Console.WriteLine($"[Handler]: Device {device.ModuleName} connected as {(device.IsLeftSock ? "Left" : "Right")} Sock.");
        }
    }
}
    }
}