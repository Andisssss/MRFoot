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
                logger.Debug($"[Handler]: Message size received: {messageSize}");

                // Read the device request message
                byte[] buffer = new byte[messageSize];
                await stream.ReadAsync(buffer, 0, messageSize, token);
                string[] requestedPaths = Encoding.ASCII.GetString(buffer)
                    .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                Console.WriteLine($"[Handler]: Requested device paths: {string.Join(", ", requestedPaths)}");
                logger.Debug($"[Handler]: Requested device paths: {string.Join(", ", requestedPaths)}");

                // Retrieve and register devices
                IEnumerable<Device> requestedDevices = GetRequestedDevices(requestedPaths);
                RegisterDevices(requestedDevices);

                // Prepare and send the response buffer
                byte[] response = PrepareResponseBuffer(requestedDevices);
                await stream.WriteAsync(response, 0, response.Length, token);
                Console.WriteLine($"[Handler]: Response buffer sent to client.");
                logger.Info($"[Handler]: Response buffer sent to client.");

                // Subscribe to device events
                foreach (Device device in devices.Keys)
                {
                    device.RawDataReceived += OnRawDataReceived;
                    Console.WriteLine($"[Handler]: Subscribed to RawDataReceived for device {device.Name}");
                    logger.Info($"[Handler]: Subscribed to RawDataReceived for device {device.Name}");
                }

                Console.WriteLine("[Handler]: Handler setup complete.");
                logger.Info("[Handler]: Handler setup complete.");
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
            try
            {
                if (sender is Device device && devices.TryGetValue(device, out Cache cache))
                {
                    Console.WriteLine($"[Handler]: Raw data received from device {device.Name}: {rawData}");
                    logger.Debug($"[Handler]: Raw data received from device {device.Name}: {rawData}");

                    byte[] data = Encoding.ASCII.GetBytes(rawData);
                    Array.Copy(data, 0, cache.buffer, 1, data.Length);

                    lock (syncLock)
                    {
                        stream.Write(cache.buffer, 0, cache.buffer.Length);
                        Console.WriteLine($"[Handler]: Data forwarded to client for device {device.Name}");
                        logger.Info($"[Handler]: Data forwarded to client for device {device.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Handler]: Error during data handling: {ex.Message}");
                logger.Warn($"[Handler]: Error during data handling: {ex.Message}");
                Stop();
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

            Console.WriteLine($"[Handler]: Prepared response buffer: {BitConverter.ToString(response.ToArray())}");
            logger.Debug($"[Handler]: Prepared response buffer: {BitConverter.ToString(response.ToArray())}");
            return response.ToArray();
        }

        /// <summary>
        /// Retrieves the requested devices from the server's manager.
        /// </summary>
        private IEnumerable<Device> GetRequestedDevices(string[] paths)
{
    if (paths.Length == 0)
    {
        Console.WriteLine("[Handler]: Requesting all available devices.");
        return server.Manager.GetAllDevices();
    }

    Console.WriteLine($"[Handler]: Requesting devices for paths: {string.Join(", ", paths)}");
    return paths.Select(path => server.Manager.GetAllDevices().FirstOrDefault(d => d.Path == path))
                .Where(device => device != null);
}


        /// <summary>
        /// Registers devices with internal caches and connects them.
        /// </summary>
        private void RegisterDevices(IEnumerable<Device> devicesToRegister)
        {
            byte deviceIndex = 0;

            foreach (var device in devicesToRegister)
            {
                if (!devices.ContainsKey(device))
                {
                    devices[device] = new Cache(deviceIndex++, new byte[0]);
                    Console.WriteLine($"[Handler]: Registering device {device.Name} at {device.Path}");
                    logger.Info($"[Handler]: Registering device {device.Name} at {device.Path}");

                    try
                    {
                        device.Connect();
                        Console.WriteLine($"[Handler]: Device {device.Name} connected successfully.");
                        logger.Info($"[Handler]: Device {device.Name} connected successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Handler]: Failed to connect to device {device.Name}: {ex.Message}");
                        logger.Warn($"[Handler]: Failed to connect to device {device.Name}: {ex.Message}");
                    }
                }
            }
        }
    }
}
