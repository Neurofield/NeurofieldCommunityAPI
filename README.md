# NeurofieldCommunityAPI

Neurofield Community API is the public version of the NeurofieldAPI which is used to control the Neurofield products. Detailed information about the products can be found on the [Neurofield Web Site](https://neurofield.org/).

This repository specifically contains the API for the [20 channel Q21 EEG Acquisition device](https://neurofield.org/neurofield-products#q21). Q21 is a USA FDA (Food and Drug Administration) approved, high resolution, DC coupled 20 channel simultaneus sampling amplifier with very low input noise and high Common Mode Rejection Ratio. 

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

        for (var iSample = 0; iSample < nSamples; iSample++)
        {
            // Note: in a real application, you should call GetSingleSample
            // in a timer callback or in a separate task, background worker or a process
            // here, we are calling it in a for loop for demo
            
            // Receive single time data.
            // GetSingleSample function takes about ~4 milli-seconds to return since we are sampling at a 256 Hz rate.  
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
```