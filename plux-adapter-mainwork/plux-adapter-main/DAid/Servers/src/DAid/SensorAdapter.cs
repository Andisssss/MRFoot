using System;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class SensorAdapter
{
    private SerialPort serialPort;
    private const byte StartByte = 0xF0;
    private const byte StopByte = 0x55;
    private const int PacketLength = 47;
    private readonly byte[] buffer = new byte[1024];
    private int bufferPos = 0;

    private const int DefaultBaudRate = 92600;
    private readonly int[] SensorPositions = { 30, 32, 38, 40 };
    private readonly double[] XPositions = { 6.0, -6.0, 6.0, -6.0 };
    private readonly double[] YPositions = { 2.0, 2.0, -2.0, -2.0 };

    private double[] sensorResistance = new double[4];
    private double[] sensorOffsets = new double[4];
    private bool isStreaming = false;
    private readonly object syncLock = new object();

    public event EventHandler<string> RawDataReceived;
    public event EventHandler<(double CoPX, double CoPY, double[] Pressures)> CoPUpdated;

    public void Initialize(string comPort, int baudRate = DefaultBaudRate)
    {
        try
        {
            serialPort = new SerialPort(comPort, baudRate)
            {
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                ReadTimeout = 5000,
                WriteTimeout = 5000
            };

            serialPort.DataReceived += DataReceivedHandler;
            serialPort.Open();
            Console.WriteLine($"[SensorAdapter]: Initialized on {comPort} at {baudRate} baud.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error initializing on {comPort}: {ex.Message}");
            throw;
        }
    }

    public void Cleanup()
    {
        lock (syncLock)
        {
            StopSensorStream();
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                Console.WriteLine("[SensorAdapter]: Serial port closed.");
            }
        }
    }

    public void StartSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && !isStreaming)
            {
                serialPort.WriteLine("BT^START\r");
                isStreaming = true;
                Console.WriteLine("[SensorAdapter]: Data stream started.");
            }
        }
    }

    public static List<string> ScanPorts()
    {
        try
        {
            var ports = SerialPort.GetPortNames().ToList();
            return ports;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error scanning ports: {ex.Message}");
            return new List<string>();
        }
    }

    public double[] GetSensorPressures()
    {
        lock (syncLock)
        {
            return (double[])sensorResistance.Clone();
        }
    }

    public void StopSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && isStreaming)
            {
                serialPort.WriteLine("BT^STOP\r");
                isStreaming = false;
                Console.WriteLine("[SensorAdapter]: Data stream stopped.");
            }
        }
    }

    public bool Calibrate()
    {
        Console.WriteLine("[SensorAdapter]: Starting calibration...");
        double[] sumValues = new double[SensorPositions.Length];
        int sampleCount = 0;
        DateTime startTime = DateTime.Now;

        while ((DateTime.Now - startTime).TotalSeconds < 10)
        {
            lock (syncLock)
            {
                if (sensorResistance.All(r => r > 0))
                {
                    for (int i = 0; i < SensorPositions.Length; i++)
                    {
                        sumValues[i] += 1.0 / sensorResistance[i]; // Inverted calibration
                    }
                    sampleCount++;
                }
            }
            Thread.Sleep(100);
        }

        if (sampleCount > 0)
        {
            for (int i = 0; i < SensorPositions.Length; i++)
            {
                sensorOffsets[i] = sumValues[i] / sampleCount;
                Console.WriteLine($"[Calibration]: Sensor {i + 1} Offset: {sensorOffsets[i]:F2}");
            }
            return true;
        }
        return false;
    }

    private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        if (serialPort == null) return;

        try
        {
            int bytesToRead = serialPort.BytesToRead;
            byte[] incomingData = new byte[bytesToRead];
            serialPort.Read(incomingData, 0, bytesToRead);
            RawDataReceived?.Invoke(this, BitConverter.ToString(incomingData));
            ProcessIncomingData(incomingData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Data reception error: {ex.Message}");
        }
    }

    private void ProcessIncomingData(byte[] incomingData)
    {
        lock (syncLock)
        {
            if (incomingData.Length + bufferPos > buffer.Length)
            {
                Console.WriteLine("[SensorAdapter]: Incoming data exceeds buffer size.");
                return;
            }

            Array.Copy(incomingData, 0, buffer, bufferPos, incomingData.Length);
            bufferPos += incomingData.Length;

            while (bufferPos >= PacketLength)
            {
                int startIndex = Array.IndexOf(buffer, StartByte, 0, bufferPos);
                if (startIndex == -1) break;

                if (startIndex + PacketLength <= bufferPos)
                {
                    byte[] packet = new byte[PacketLength];
                    Array.Copy(buffer, startIndex, packet, 0, PacketLength);

                    if (ValidatePacket(packet))
                    {
                        ExtractSensorValues(packet);
                        CalculateAndNotifyCoP();
                    }

                    bufferPos -= startIndex + PacketLength;
                    Array.Copy(buffer, startIndex + PacketLength, buffer, 0, bufferPos);
                }
                else
                {
                    break;
                }
            }
        }
    }

    private bool ValidatePacket(byte[] packet)
    {
        byte calculatedChecksum = CalculateChecksum(packet, PacketLength - 2);
        byte receivedChecksum = packet[PacketLength - 2];
        return calculatedChecksum == receivedChecksum && packet[PacketLength - 1] == StopByte;
    }

    private void ExtractSensorValues(byte[] packet)
    {
        lock (syncLock)
        {
            for (int i = 0; i < SensorPositions.Length; i++)
            {
                int pos = SensorPositions[i];
                int rawValue = (packet[pos] << 8) | packet[pos + 1];

                sensorResistance[i] = 1.0 / rawValue; 
            }
        }
    }

    private void CalculateAndNotifyCoP()
    {
        lock (syncLock)
        {
            double totalPressure = sensorResistance.Sum();

            if (totalPressure <= 0)
            {
                Console.WriteLine("[SensorAdapter]: Total pressure is zero. CoP set to (0, 0).");
                Task.Run(() => CoPUpdated?.Invoke(this, (0, 0, sensorResistance)));
                return;
            }

            double copX = sensorResistance.Zip(XPositions, (p, x) => p * x).Sum() / totalPressure;
            double copY = sensorResistance.Zip(YPositions, (p, y) => p * y).Sum() / totalPressure;

            Task.Run(() => CoPUpdated?.Invoke(this, (copX, copY, sensorResistance)));
        }
    }

    private byte CalculateChecksum(byte[] data, int length)
    {
        int sum = data.Take(length).Sum(b => b);
        return (byte)(sum & 0xFF);
    }
}
