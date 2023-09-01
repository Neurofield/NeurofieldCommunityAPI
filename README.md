# NeurofieldCommunityAPI

Neurofield Community API is the public version of the NeurofieldAPI which is used to control the Neurofield products. Detailed information about the products can be found on the [Neurofield Web Site](https://neurofield.org/).

This repository specifically contains the API for the [20 channel Q21 EEG Acquisition device](https://neurofield.org/neurofield-products#q21). Q21 is an US FDA (Food abnd Drug Administration) approved, high resolution, DC coupled 20 channel simultaneus sampling amplifier with very low input noise and high Common Mode Rejection Ratio. 

<p float="middle">
  <img src="https://3989ac5bcbe1edfc864a-0a7f10f87519dba22d2dbc6233a731e5.ssl.cf2.rackcdn.com/neurofield/img-Q21_copy.jpg?raw=true" width="400"/>     
  &nbsp; &nbsp;
</p>

# Installation

### Steps

* Download and install the latest version of the PCAN drivers from [here](https://www.peak-system.com/PCAN-Basic.239.0.html?&L=1). 
 Make sure you install the PCAN-BASIC api as well as the drivers.
* Clone this repo
* Add a reference to the Q21API project in your application.

# Example Usage:

* Assume that you have the Q21 device connected over the PCAN-USB interface and turned on.
* The following code connects to the device and acquires 4 seconds of data from the device:

```C#
        var api = new NeurofieldCommunityQ21API(PcanChannel.Usb01);

        var deviceType = api.EEGDeviceType;

        double scaleFactor;

        switch (deviceType)
        {
            case NeurofieldCommunityDeviceType.EEG21RevK:
                // Q21 Rev-K device scale set to 4.5 volts divided by 2^24 and ADC gain=12 (i.e. without any external instrumentation amplifier analog gain)
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
```