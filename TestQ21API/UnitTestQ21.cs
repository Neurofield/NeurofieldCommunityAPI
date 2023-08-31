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
        var api = new NeurofieldCommunityQ21API(PcanChannel.Usb01);

        var deviceType = api.EEGDeviceType;

        double scaleFactor;

        switch (deviceType)
        {
            case NeurofieldCommunityDeviceType.EEG21RevK:
                // New FPGALess device scale set to 4.5 volts divided by 2^24 and ADC gain=12 (i.e. without any external instrumentation amplifier analog gain)
                // 4500000 / 8388608 / 12 =  0.044703483581543
                // also multiply by -1 to change the polarity
                scaleFactor = -0.044703483581543;
                break;
                
            case NeurofieldCommunityDeviceType.EEG21RevA:
                // Divide by ADC gain (2) and Neurofield external instrumentation amplifier analog gain (6.6667 for EEG21Rev A).                        
                // 4500000 / 8388608 / 6.6667 / 2 = 0.040233115106831. Refer to the API doc.            
                // also multiply by -1 to change the polarity
                scaleFactor = -0.040233115106831;
                break;
            
            default: // Other versions
                // Divide by ADC gain (2) and Neurofield external instrumentation amplifier analog gain (12.85 for others).                        
                // 4500000 / 8388608 / 12.85 / 2 = 0.020873221905779. Refer to the API doc.            
                // also multiply by -1 to change the polarity
                scaleFactor = -0.020873221905779;
                break;
        }
        
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
            // Receive single time data. (timeouts and throws an exception after ~1.5 seconds if no data received)
            var rawData = api.ReceiveSingleEEGDataSample(out var time);

            for (var iChannel = 0; iChannel < 20; iChannel++)
                eegData[iSample, iChannel] = rawData[iChannel] * scaleFactor;

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