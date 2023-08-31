namespace Q21API;



public enum Q21MessageTypes : byte
{
    CANBusQuery = 0,
    SendAtoDData = 3,
    SendAtoDDataMsg2 = 5,
    SendAtoDDataMsg3 = 6,
    SendAtoDDataMsg4 = 7,
    SendAtoDDataMsg5 = 8,
    SendAtoDDataMsg6 = 9,
    SendAtoDDataMsg7 = 10,
    SendAtoDDataMsg8 = 11,
    SendAtoDDataMsg9 = 12,
    SendAtoDDataMsg10 = 13,
    
    ImpedanceCh1 = 0xA0,
    ImpedanceCh2 = 0xA1,
    ImpedanceCh3 = 0xA2,
    ImpedanceCh4 = 0xA3,
    ImpedanceCh5 = 0xA4,
    ImpedanceCh6 = 0xA5,
    ImpedanceCh7 = 0xA6,
    ImpedanceCh8 = 0xA7,
    ImpedanceCh9 = 0xA8,
    ImpedanceCh10 = 0xA9,
    ImpedanceCh11 = 0xAA,
    ImpedanceCh12 = 0xAB,
    ImpedanceCh13 = 0xAC,
    ImpedanceCh14 = 0xAD,
    ImpedanceCh15 = 0xAE,
    ImpedanceCh16 = 0xAF,
    ImpedanceCh17 = 0xB0,
    ImpedanceCh18 = 0xB1,
    ImpedanceCh19 = 0xB2,
    ImpedanceCh20 = 0xB3,
    
    Q20Abort = 255
}

public class NeurofieldExtendedHeader
{
    public bool Slave2Host { get; set; }

    public NeurofieldCommunityDeviceType ModuleType { get; set; }

    public byte Serial { get; set; }

    public Q21MessageTypes MessageType { get; set; }
}

