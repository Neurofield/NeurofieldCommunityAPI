namespace Q21API;

public enum NeurofieldCommunityDeviceType : byte
{
    Host = 0x00,        //  Host Computer

    EEG20RevA = 0xA1,   //  Neurofield 20 Channel EEG Rev A 
    EEG20RevB = 0xA2,   //  Neurofield 20 Channel EEG Rev B 
    EEG21     = 0xA3,   //  Neurofield 21 Channel EEG 
    EEG21RevA = 0xA4,   //  Neurofield 21 Channel EEG Rev A
    EEG21RevK = 0xA5,   //  FPGALess Q21
}

public class NeurofieldCommunityDevice
{
    public NeurofieldCommunityDeviceType Type { get; set; }

    public byte Serial { get; set; }

    public bool IsSame(NeurofieldCommunityDevice dev2)
    {
        if (dev2.Serial != Serial)
            return false;

        return dev2.Type == Type;
    }
}