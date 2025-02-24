using System;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

public class SensorAdapter
{
    private SerialPort serialPort;
    private const byte StartByte = 0xF0;
    private const byte StopByte = 0x55;
    private const int PacketLength = 47;

    private readonly byte[] buffer = new byte[2048]; 

    private string moduleName = "Unknown";
    public string ModuleName => moduleName; 
    private int bufferPos = 0;

    private const int DefaultBaudRate = 92600;
    private readonly int[] RightSensorPositions = { 30, 32, 38, 40 };
    private readonly int[] LeftSensorPositions = { 42, 30, 40, 44 }; // should be 32 (black wire, now orange), 30, 40, 38 (blue wire, now purple), can put 30,32,38,40
    private int[] SensorPositions;

    private readonly double[] XPositions = { 2.0, -2.0, 2.0, -2.0 }; //for right leg
    private readonly double[] YPositions = { 6.0, 6.0, -6.0, -6.0 };
    //double[] RESISTANCE_MULTIPLIER = new double[]{ 0.125, 0.25, 2.00, 4.00};
    private double[] sensorResistance = new double[4];
    private (double x0, double y0) calibrationOffsetsLeft = (0, 0);
    private (double x0, double y0) calibrationOffsetsRight = (0, 0);
    private bool isStreaming = false;
    private readonly object syncLock = new object();
    public string DeviceId { get; } 
    private double minPressureStored = 0.0;
    private double[] sensorOffsets = new double[4];
    private double maxPressureStored = 0.0;

    public bool moduleNameRetrieved = false;
    
    public event EventHandler<(string ModuleName, bool IsLeftSock)> ModuleInfoUpdated;

    public event EventHandler<string> RawDataReceived;
    public event EventHandler<(double CoPX, double CoPY, double[] Pressures)> CoPUpdated;

      public SensorAdapter(string deviceId)
    {
        DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        SensorPositions = RightSensorPositions;
    }
    public void Initialize(string comPort, int baudRate = DefaultBaudRate)
    {
            if (serialPort != null && serialPort.IsOpen)
            {
                Console.WriteLine($"[SensorAdapter {DeviceId}]: Serial port already initialized.");
                return;
            }
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
            //ConfigureBTS1("8");
            //ConfigureBTS234("1");
            //ConfigureBTS8("*,8");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error initializing on {comPort}: {ex.Message}");
            throw;
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
    public void ConfigureBTS8(string config) => SendCommand($"BTS8={config}");
    public void ConfigureBTS1(string config) => SendCommand($"BTS1={config}");
    public void ConfigureBTS234(string config) {
        SendCommand($"BTS2={config}");
        SendCommand($"BTS3={config}");
        SendCommand($"BTS4={config}");
        }


    public void Cleanup()
    {
        lock (syncLock)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    StopSensorStream();
                    serialPort.Close();
                    Console.WriteLine($"[SensorAdapter {DeviceId}]: Serial port closed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SensorAdapter {DeviceId}]: Error closing serial port: {ex.Message}");
                }
            }
        }
    }

    public void StartSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && !isStreaming)
            {
                SendCommand("BT^START");
                isStreaming = true;
            }
        }
    }

    public void StopSensorStream()
    {
        lock (syncLock)
        {
            if (serialPort?.IsOpen == true && isStreaming)
            {
                SendCommand("BT^STOP");
                isStreaming = false;
                Console.WriteLine("[SensorAdapter]: Data stream stopped.");
            }
        }
    }

public double[] GetSensorPressures()
{
    lock (syncLock)
    {
        return (double[])sensorResistance.Clone();
    }
}

private (double x0, double y0) calibrationOffsets = (0, 0);

public bool Calibrate(bool isLeftSock)
{
    double maxPressure = double.MinValue, minPressure = double.MaxValue;
    double totalX = 0, totalY = 0;
    int sampleCount = 0;
    DateTime startTime = DateTime.Now;

    Console.WriteLine("[Calibration]: Stand with both feet. Lift each foot one at a time after 1 second.");

    while ((DateTime.Now - startTime).TotalSeconds < 10)
    {
        lock (syncLock)
        {
            double totalPressure = sensorResistance.Sum();
            for (int i = 0; i < sensorResistance.Length; i++)
                {
                    //Console.Write($"S{i + 1}: {sensorResistance[i]:F6} | ");
                }
                //Console.WriteLine($"Total: {totalPressure:F6}");
            if (totalPressure > 0 && sensorResistance.All(r => r > 0))
            {
                maxPressure = Math.Max(maxPressure, totalPressure);
                minPressure = Math.Min(minPressure, totalPressure);

                double copX = sensorResistance.Zip(XPositions, (p, x) => p * x).Sum() / totalPressure;
                double copY = sensorResistance.Zip(YPositions, (p, y) => p * y).Sum() / totalPressure;

                totalX += copX;
                totalY += copY;
                sampleCount++;
            }
        }

        Thread.Sleep(100); // Collect data every 100ms
    }

    // Compute Pressure Range
    double pressureRange = maxPressure - minPressure;

    // Ensure valid calibration
    if (pressureRange <= 0 || sampleCount == 0)
    {
        Console.WriteLine("[Calibration]: Calibration failed. Invalid pressure range.");
        return false;
    }
    minPressureStored = minPressure;
    maxPressureStored = maxPressure;
    if (isLeftSock)
    {
        calibrationOffsetsLeft = (totalX / sampleCount, totalY / sampleCount);
        Console.WriteLine($"[Calibration]: Left Foot Offset X: {calibrationOffsetsLeft.x0}, Y: {calibrationOffsetsLeft.y0}");
    }
    else
    {
        calibrationOffsetsRight = (totalX / sampleCount, totalY / sampleCount);
        Console.WriteLine($"[Calibration]: Right Foot Offset X: {calibrationOffsetsRight.x0}, Y: {calibrationOffsetsRight.y0}");
    }
    Console.WriteLine($"[Calibration]: Completed. Pmax: {maxPressure}, Pmin: {minPressure}");
    return true;
}

private double[] MovingAverageFilter(double[] rawData, int windowSize)
{
    double[] filteredData = new double[rawData.Length];
    int halfWindowSize = windowSize / 2;

    for (int i = 0; i < rawData.Length; i++)
    {
        double sum = 0;
        int count = 0;

        for (int j = i - halfWindowSize; j <= i + halfWindowSize; j++)
        {
            if (j >= 0 && j < rawData.Length)
            {
                sum += rawData[j];
                count++;
            }
        }

        filteredData[i] = sum / count;
    }

    return filteredData;
}

    private void SendCommand(string command)
    {
          lock (syncLock)
        {
            try
            {
                if (serialPort?.IsOpen == true)
                {
                    serialPort.WriteLine(command + "\r");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SensorAdapter {DeviceId}]: Error sending command '{command}': {ex.Message}");
            }
        }
    }


public void RetrieveModuleName()
{
    if (moduleNameRetrieved) return;

    lock (syncLock)
    {
        try
        {
            serialPort.DiscardInBuffer();
            SendCommand("BTS6?");
            Thread.Sleep(1000);

            int bytesToRead = serialPort.BytesToRead;
            if (bytesToRead > 0)
            {
                byte[] incomingData = new byte[bytesToRead];
                serialPort.Read(incomingData, 0, bytesToRead);

                string response = Encoding.ASCII.GetString(incomingData).Trim();
                foreach (var line in response.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (line.IndexOf("Register 6 value:", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        int index = line.IndexOf(':');
                        if (index != -1 && index + 1 < line.Length)
                        {
                            string moduleValue = line.Substring(index + 1).Trim();
                            if (int.TryParse(moduleValue, out int moduleNumber))
                            {
                                moduleName = moduleValue;
                                moduleNameRetrieved = true;

                                bool isLeftSock = moduleNumber % 2 != 0;
                                SensorPositions = isLeftSock ? LeftSensorPositions : RightSensorPositions;
                                ModuleInfoUpdated?.Invoke(this, (moduleName, isLeftSock));
                                return;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SensorAdapter]: Error retrieving module name: {ex.Message}");
        }
    }
}


 private void CheckLeftOrRight(int moduleNumber)
{
    if (moduleNumber % 2 == 0)
    {
        SensorPositions = RightSensorPositions; 
    }
    else
    {
        SensorPositions = LeftSensorPositions; // Reverse sensor order for left sock
    }
}

public event EventHandler<string> ModuleNameRetrieved; // Event to notify when the module name is retrieved

private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
{
    try
    {
        int bytesToRead = serialPort.BytesToRead;
        byte[] incomingData = new byte[bytesToRead];
        serialPort.Read(incomingData, 0, bytesToRead);
        RawDataReceived?.Invoke(this, BitConverter.ToString(incomingData));
        ProcessIncomingData(incomingData);

        if (!moduleNameRetrieved)
        {
            RetrieveModuleName();

            if (moduleName != "Unknown") // Ensure the module name was successfully retrieved
            {
                moduleNameRetrieved = true;
                ModuleNameRetrieved?.Invoke(this, moduleName); // Notify listeners
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[SensorAdapter]: Error receiving data: {ex.Message}");
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

                    if (ValidatePacket(packet)) //print out in console the raw data
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
        double[] rawSensorValues = new double[sensorResistance.Length];

        for (int i = 0; i < SensorPositions.Length; i++)
        {
            int pos = SensorPositions[i];
            int rawValue = (packet[pos] << 8) | packet[pos + 1];
            //double resistanceMultiplier = RESISTANCE_MULTIPLIER[Math.Min(i, RESISTANCE_MULTIPLIER.Length - 1)];
            rawSensorValues[i] = rawValue > 0 ? (1.0 / rawValue) : 0.0;
            //Console.WriteLine($"Sensor {i + 1}: Raw Value = {rawValue}, Multiplier = {resistanceMultiplier:F4}, Resistance = {rawSensorValues[i]:F6}");
       }
        sensorResistance = MovingAverageFilter(rawSensorValues, 4);
    }
}
private void CalculateAndNotifyCoP()
{
    lock (syncLock)
    {
        double totalPressure = sensorResistance.Sum(); 
        double[] adjustedXPositions = SensorPositions == RightSensorPositions 
            ? XPositions.Select(x =>-x).ToArray()  
            : XPositions;

        if (totalPressure <= 0)
        {
            Console.WriteLine("[CoP]: No valid pressure detected, CoP remains at (0,0).");
            Task.Run(() => CoPUpdated?.Invoke(this, (0, 0, sensorResistance)));
            return;
        }
        double Pnorm = (totalPressure - minPressureStored) / (maxPressureStored - minPressureStored);
        Pnorm = Math.Max(0, Math.Min(1, Pnorm));
        double CoPX = sensorResistance.Zip(adjustedXPositions, (p, x) => (p * x )).Sum() / totalPressure; //y*Pnorm
        double CoPY = sensorResistance.Zip(YPositions, (p, y) => (p * y )).Sum() / totalPressure; //x*Pnorm
        if (SensorPositions == LeftSensorPositions)
        {
            CoPX -= calibrationOffsetsLeft.x0;
            CoPY -= calibrationOffsetsLeft.y0;        }
        else
        {
            CoPX -= calibrationOffsetsRight.x0;
            CoPY -= calibrationOffsetsRight.y0;        }
        Task.Run(() => CoPUpdated?.Invoke(this, (CoPX, CoPY, sensorResistance)));
    }
}


    private byte CalculateChecksum(byte[] data, int length)
    {
        int sum = data.Take(length).Sum(b => b);
        return (byte)(sum & 0xFF);
    }
}