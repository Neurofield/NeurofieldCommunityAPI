using System.Diagnostics;
using Peak.Can.Basic;

namespace Q21API;

public class NeurofieldCommunityQ21API : NeurofieldCommunityEEGAPI
{
    public override string ToString()
    {
        return "Dev:" + SelectedEEGDevice.Serial;
    }
    
    public const double NeurofieldSamplingRate = 256.0;
    
    /// <summary>
    /// 6 uA current used to measure impedance
    /// </summary>
    public const double InjectedCurrentForImpedance = 6e-6;
    
    /// <summary>
    /// the total resistance on the board on the impedance measurement line.
    /// </summary>
    public const double ResistorLine = 12000.0;
    
    public List<NeurofieldCommunityDevice> ConnectedEEGDevices { get; private set; }
    
    
    
    public NeurofieldCommunityQ21API(PcanChannel pcanHandle) : base(pcanHandle)
    {
        ConnectedEEGDevices = new List<NeurofieldCommunityDevice>();
        
        foreach (var device in ConnectedDevices)
        {
            if (device.Type == NeurofieldCommunityDeviceType.EEG20RevA ||
                device.Type == NeurofieldCommunityDeviceType.EEG20RevB ||
                device.Type == NeurofieldCommunityDeviceType.EEG21 ||
                device.Type == NeurofieldCommunityDeviceType.EEG21RevA ||
                device.Type == NeurofieldCommunityDeviceType.EEG21RevK
               )
            {
                ConnectedEEGDevices.Add(device);
            }
               
        }
        
        if (ConnectedEEGDevices.Count <= 0)
        {
            Release();
            throw new Exception("No EEG device found");
        }
        
        // Select the first device on this interface
        SelectedEEGDevice = ConnectedEEGDevices[0];
        
    }
    
    /// <summary>
    /// returns true if the selected EEG device has impedance measurement capability
    /// </summary>
    public bool ImpedanceEnabled => SelectedEEGDevice.Type == NeurofieldCommunityDeviceType.EEG21RevK;
    
    #region EEG Device Related Functions
    
    /// <summary>
    /// Get a single impedance measurement from all channels
    /// </summary>
    /// <returns></returns>
    public double[] ReceiveSingleImpedanceSample(CancellationTokenSource cts)
    {
        var stage = (byte) 0;
        var data = new Tuple<int, int>[20];
        
        var impedance = new double[data.Length];
        
        while (true)
        {
            if (cts.IsCancellationRequested)
                return impedance;

            // Receive one CANBUS message                
            var msgRx = ReceiveSingleMessageFromDevice(SelectedEEGDevice, out var header, out _);

            if (header.MessageType == ImpedanceDataRxSequence[stage])
            {
                ExtractImpedanceDataFromMessage(msgRx, stage, data);
                stage++;
            }
            else
            {
                Debug.WriteLine("ADC data sequence error. Expected: " + ImpedanceDataRxSequence[stage] + ". Received: " + header.MessageType);
                stage = 0;
            }

            if (stage >= 20)
                break;
        }
        
        const double numSamples = 15;            
        
        for (var iChannel = 0; iChannel < data.Length; iChannel++)
        {
            var offsetVoltage    = data[iChannel].Item1 * 4.5 / numSamples / 8388608;
            var impedanceVoltage = data[iChannel].Item2 * 4.5 / numSamples / 8388608;
            
            impedance[iChannel] = (impedanceVoltage - offsetVoltage) / InjectedCurrentForImpedance - ResistorLine;

            // prevent impedance from being negative due to measurement errors and resistor line errors.
            if (impedance[iChannel] <= 1000)
                impedance[iChannel] = 1000;
            
        }

        return impedance;
    }
    public override int[] ReceiveSingleEEGDataSample(out ulong time)
    {
        var stage = 0;
        var data = new int[20];
        time = 0;

        while (true)
        {
            // Receive one CANBUS message                
            var msgRx = ReceiveSingleMessageFromDevice(SelectedEEGDevice, out var header, out var timeStamp);                

            if (header.MessageType == ADDataRxSequence[stage])
            {
                ExtractAdDataFromMessage(msgRx, stage, data);
                
                if (stage == 0)
                    time = timeStamp;
                
                stage++;
            }
            else
            {
                Debug.WriteLine("ADC data sequence error. Expected: " + ADDataRxSequence[stage] + ". Received: " + header.MessageType);
                stage = 0;
            }

            if (stage < 10)
                continue;
            
            return data;
        }
    }
    
    public string GetEEGDeviceInfo()
    {
        return "Type: " + SelectedEEGDevice.Type + ", Serial: " + SelectedEEGDevice.Serial;
    }
    
    public byte GetEEGDeviceSerial() => SelectedEEGDevice.Serial;
    
    
    
    #endregion

    
}