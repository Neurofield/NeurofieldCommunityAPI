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
        
        api.StartReceivingEEG();
        
        const int timeLengthSec = 4; // get 4 seconds of data
        
        var samplingRate = (int) NeurofieldCommunityQ21API.NeurofieldSamplingRate;
        
        var nData = samplingRate * timeLengthSec;

        for (var iData = 0; iData < nData; iData++)
        {
            api.ReceiveSingleEEGDataSample(out _);
            if (iData % samplingRate == 0)
                Debug.WriteLine("1 sec Data...");
        }
            
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