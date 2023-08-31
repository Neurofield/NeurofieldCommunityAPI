using System.Diagnostics;
using Peak.Can.Basic;

namespace Q21API;

public abstract class NeurofieldCommunityEEGAPI : NeurofieldCommunityCANBUSApiBase 
{                
    #region Private/Protected

    /// <summary>
    /// Selected EEG device on this adapter.
    /// </summary>
    protected NeurofieldCommunityDevice SelectedEEGDevice;

    protected static readonly Q21MessageTypes[] ADDataRxSequence = {
        Q21MessageTypes.SendAtoDData,
        Q21MessageTypes.SendAtoDDataMsg2,
        Q21MessageTypes.SendAtoDDataMsg3,
        Q21MessageTypes.SendAtoDDataMsg4,
        Q21MessageTypes.SendAtoDDataMsg5,
        Q21MessageTypes.SendAtoDDataMsg6,
        Q21MessageTypes.SendAtoDDataMsg7,
        Q21MessageTypes.SendAtoDDataMsg8,
        Q21MessageTypes.SendAtoDDataMsg9,
        Q21MessageTypes.SendAtoDDataMsg10,
    };

    protected static readonly Q21MessageTypes[] ImpedanceDataRxSequence = {
        Q21MessageTypes.ImpedanceCh1,
        Q21MessageTypes.ImpedanceCh2,
        Q21MessageTypes.ImpedanceCh3,
            
        Q21MessageTypes.ImpedanceCh4,
        Q21MessageTypes.ImpedanceCh5,
        Q21MessageTypes.ImpedanceCh6,
            
        Q21MessageTypes.ImpedanceCh7,
        Q21MessageTypes.ImpedanceCh8,            
        Q21MessageTypes.ImpedanceCh9,
            
        Q21MessageTypes.ImpedanceCh10,
        Q21MessageTypes.ImpedanceCh11,
        Q21MessageTypes.ImpedanceCh12,
            
        Q21MessageTypes.ImpedanceCh13,
        Q21MessageTypes.ImpedanceCh14,

        Q21MessageTypes.ImpedanceCh15,
        Q21MessageTypes.ImpedanceCh16,

        Q21MessageTypes.ImpedanceCh17,
        Q21MessageTypes.ImpedanceCh18,

        Q21MessageTypes.ImpedanceCh19,
        Q21MessageTypes.ImpedanceCh20,
    };

    protected static void ExtractImpedanceDataFromMessage(PcanMessage msg, int stage, IList<Tuple<int,int>> data)
    {
        if (msg.Length != 8)
            throw new Exception("Unexpected number of data bytes");
            
        // cast to int with sign extension
        var offset  = (((sbyte)msg.Data[0]) << 24) + (msg.Data[1] << 16) + (msg.Data[2] << 8) + msg.Data[3];
        var voltage = (((sbyte)msg.Data[4]) << 24) + (msg.Data[5] << 16) + (msg.Data[6] << 8) + msg.Data[7];

        data[stage] = new Tuple<int, int>(offset, voltage);
    }

    protected static void ExtractAdDataFromMessage(PcanMessage msg, int stage, IList<int> data)
    {
        if (msg.Length != 6)
            throw new Exception("Unexpected number of data bytes");

        var i = stage * 2;

        // cast to int with sign extension
        data[i] = (((sbyte)msg.Data[0]) << 16) + (msg.Data[1] << 8) + msg.Data[2];
        data[i + 1] = (((sbyte)msg.Data[3]) << 16) | msg.Data[4] << 8 | msg.Data[5];
    }

    #endregion

    /// <inheritdoc />
    protected NeurofieldCommunityEEGAPI(PcanChannel pcanHandle) : base(pcanHandle)
    {
            
    }

    public void Switch2ImpedanceMeasurementMode()
    {
        if (SelectedEEGDevice.Type != NeurofieldCommunityDeviceType.EEG21RevK)
            throw new Exception("Only Q21 Rev-K supports switching between EEG / impedance measurement modes.");
        
        var data = new byte[8];
        data[0] = 1;
        data[1] = 1;

        SendMessage(SelectedEEGDevice, 0x20, data);
    }

    public void Switch2EEGMeasurementMode()
    {
        if (SelectedEEGDevice.Type != NeurofieldCommunityDeviceType.EEG21RevK)
            throw new Exception("Only Q21 Rev-K supports switching between EEG / impedance measurement modes.");
        
        var data = new byte[8];            
        SendMessage(SelectedEEGDevice, 0x20, data);
    }        

    public void StartReceivingEEG()
    {
        // ReSharper disable once ConvertToConstant.Local
        var nSamples = 256 * 60 * 60 * 8;

        var data = new byte[8];
        data[0] = (byte)(nSamples >> 24);
        data[1] = (byte)(nSamples >> 16);
        data[2] = (byte)(nSamples >> 8);
            
        // ReSharper disable once IntVariableOverflowInUncheckedContext
        data[3] = (byte) nSamples;
            
        SendMessage(SelectedEEGDevice, 0x03, data);
        
    }


    /// <summary>
    /// blinks the front led of the device three times by requesting 3x100 samples and waiting in between.
    /// </summary>
    public void Blink()
    {
        var nSamples = 100;

        var data = new byte[8];
        data[0] = (byte)(nSamples >> 24);
        data[1] = (byte)(nSamples >> 16);
        data[2] = (byte)(nSamples >> 8);
        data[3] = (byte)(nSamples);
            
        // request 100 samples ~ 400 ms
        SendMessage(SelectedEEGDevice, 0x03, data);
        for (var iSample = 0; iSample < nSamples; iSample++)
            ReceiveSingleEEGDataSample(out _);
            
        Thread.Sleep(400);
            
        SendMessage(SelectedEEGDevice, 0x03, data);
        for (var iSample = 0; iSample < nSamples; iSample++)
            ReceiveSingleEEGDataSample(out _);
            
        Thread.Sleep(400);
            
        SendMessage(SelectedEEGDevice, 0x03, data);
        for (var iSample = 0; iSample < nSamples; iSample++)
            ReceiveSingleEEGDataSample(out _);
    }

    public void AbortReceivingEEG()
    {
        for (var i = 0; i < 50; i++)
            SendMessage(SelectedEEGDevice, 0xFF, new byte[8]);

        Thread.Sleep(100);

        // reset tx/rx buffers
        ResetBuffers();
    }

    public NeurofieldCommunityDeviceType GetEEGDeviceType() => SelectedEEGDevice.Type;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time">msg reception time in microseconds</param>
    /// <returns></returns>
    public abstract int[] ReceiveSingleEEGDataSample(out ulong time);
}