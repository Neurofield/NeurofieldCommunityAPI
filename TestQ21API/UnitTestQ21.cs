using System.Diagnostics;
using Peak.Can.Basic;
using Q21API;

namespace TestQ21API;

[TestClass]
public class UnitTestQ21
{
    [TestMethod]
    public void TestEEGDataRead()
    {
        // This constructor also connects to the device and takes about 1 second to complete
        // if no device found on the USB1 interface, throws an exception.
        var api = new NeurofieldCommunityQ21API(PcanChannel.Usb01);

        Debug.WriteLine("Connected to device...");
        Debug.WriteLine("Device Type:" + api.EEGDeviceType);
        Debug.WriteLine("Device Serial Number:" + api.EEGDeviceSerial);
        Debug.WriteLine(api.ImpedanceEnabled ? "Device has impedance measurement capability" : "This model can not measure channel impedance.");

        // Trigger the EEG device to start sending samples over the CAN interface 
        api.StartReceivingEEG();

        const int timeLengthSec = 4; // get 4 seconds of data
        
        var samplingRate = (int) NeurofieldCommunityQ21API.NeurofieldSamplingRate;
        
        var nSamples = samplingRate * timeLengthSec;

        // nSamples x nChannels data array in micro volts
        var eegData = new double[nSamples, 20];

        // in a real application, you should call ReceiveSingleEEGDataSample in a timer or in a separate task/worker/process
        for (var iSample = 0; iSample < nSamples; iSample++)
        {
            // Receive single time data.
            var rawData = api.GetSingleSample(out var time);

            // Write to 4 second buffer
            for (var iChannel = 0; iChannel < 20; iChannel++)
                eegData[iSample, iChannel] = rawData[iChannel];

            if (iSample % samplingRate == 0)
                Debug.WriteLine("1 sec Data...");
        }
            
        // after EEG acquisition is complete, trigger the device to stop sending data and release the CAN-USB interface
        api.AbortReceivingEEG();
        api.Release();
    }
    
    /// <summary>
    /// Read a number of impedance values from Q21
    /// </summary>
    [TestMethod]
    public void TestImpedanceDataRead()
    {
        var api = new NeurofieldCommunityQ21API(PcanChannel.Usb01);

        api.Switch2ImpedanceMeasurementMode();

        const int nData = 20; // number of samples to read
        for (var iData = 0; iData < nData; iData++)                            
            api.ReceiveSingleImpedanceSample(null);                         

        // Send abort
        api.AbortReceivingEEG();           
    }
}