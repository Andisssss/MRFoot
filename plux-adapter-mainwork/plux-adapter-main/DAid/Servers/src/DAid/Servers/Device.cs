using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace DAid.Servers
{
    public sealed class Device
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string path;
        private readonly int baudRate;
        private SensorAdapter sensorAdapter;

        private readonly object _syncLock = new object();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event EventHandler<string> RawDataReceived;
        public event EventHandler<(double CoPX, double CoPY, double[] Pressures)> CoPUpdated;

        public bool IsConnected { get; private set; }
        public bool IsStreaming { get; private set; } // Tracks streaming state
        public string Path => path;
        public float Frequency { get; private set; }
        public string Name { get; private set; }

        // Expose the SensorAdapter as a read-only property
        public SensorAdapter SensorAdapter => sensorAdapter;

        public Device(string path, float frequency, string name)
        {
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            this.baudRate = (int)frequency;
            this.Frequency = frequency;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            InitializeSensorAdapter();
        }

        private void InitializeSensorAdapter()
        {
            sensorAdapter = new SensorAdapter();

            // Subscribe to events
            sensorAdapter.RawDataReceived += OnRawDataReceived;
            sensorAdapter.CoPUpdated += OnCoPUpdated;

            logger.Info($"SensorAdapter initialized for device {Name} on {path} with baud rate {baudRate}.");
        }

        public void Connect()
        {
            lock (_syncLock)
            {
                if (IsConnected)
                {
                    logger.Info($"Device {Name} on {path} is already connected.");
                    return;
                }

                logger.Info($"Connecting to device {Name} on {path}...");
                try
                {
                    sensorAdapter.Initialize(path, baudRate);
                    IsConnected = true;
                    logger.Info($"Device {Name} successfully connected on {path}.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to connect to device {Name} on {path}: {ex.Message}");
                    IsConnected = false;
                }
            }
        }

        public void Start()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    logger.Warn($"Cannot start acquisition for device {Name} because it is not connected.");
                    return;
                }

                if (IsStreaming)
                {
                    logger.Info($"Data acquisition for device {Name} is already in progress.");
                    return;
                }

                try
                {
                    sensorAdapter.StartSensorStream();
                    IsStreaming = true;
                    logger.Info($"Data acquisition started for device {Name}.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to start data acquisition for {Name}: {ex.Message}");
                    IsStreaming = false;
                }
            }
        }

        public void Stop()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    logger.Warn($"Cannot stop acquisition for device {Name} on {path} because it is not connected.");
                    return;
                }

                if (!IsStreaming)
                {
                    logger.Info($"Data acquisition for device {Name} is not currently running.");
                    return;
                }

                logger.Info($"Stopping data acquisition for device {Name} on {path}...");
                try
                {
                    cancellationTokenSource.Cancel();
                    sensorAdapter.StopSensorStream();
                    IsStreaming = false;
                    logger.Info($"Device {Name} on {path} stopped successfully.");
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to stop data acquisition for device {Name} on {path}: {ex.Message}");
                }
            }
        }

        public double[] GetSensorPressures()
        {
            if (!IsConnected || !IsStreaming)
            {
                logger.Warn($"Attempted to get sensor pressures for device {Name} while not streaming.");
                return Array.Empty<double>();
            }

            return sensorAdapter.GetSensorPressures();
        }

        private void OnRawDataReceived(object sender, string rawData)
        {
            // Ensure RawDataReceived is triggered asynchronously
            Task.Run(() => RawDataReceived?.Invoke(this, rawData));
        }

        private void OnCoPUpdated(object sender, (double CoPX, double CoPY, double[] Pressures) copData)
        {

            CoPUpdated?.Invoke(this, copData); // Ensure this event is invoked
        }

        public void Calibrate()
        {
            lock (_syncLock)
            {
                if (!IsConnected)
                {
                    logger.Warn($"Cannot calibrate device {Name} because it is not connected.");
                    return;
                }

                logger.Info($"Starting calibration for device {Name}...");
                if (sensorAdapter.Calibrate())
                {
                    logger.Info($"Calibration completed successfully for device {Name}.");
                }
                else
                {
                    logger.Warn($"Calibration failed for device {Name}.");
                }
            }
        }
    }
}
