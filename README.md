# NeurofieldCommunityAPI

Neurofield Community API is the public version of the NeurofieldAPI which is used to control the Neurofield products. Detailed information about the products can be found on the [Neurofield Web Site](https://neurofield.org/).

This repository specifically contains the API for the [20 channel Q21 EEG Acquisition device](https://neurofield.org/neurofield-products#q21)

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
```