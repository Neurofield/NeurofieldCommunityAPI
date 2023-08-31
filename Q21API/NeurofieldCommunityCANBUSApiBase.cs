using System;
using System.Collections.Generic;
using System.Threading;
using Peak.Can.Basic;

namespace Q21API;

public class NeurofieldCommunityCANBUSApiBase
{
    #region Private

    private PcanChannel _channel;
    
    /// <summary>
    /// allow up to 8 usb PCAN interfaces!
    /// </summary>
    private static readonly PcanChannel[] HandlesArray = {
        PcanChannel.Usb01,
        PcanChannel.Usb02,
        PcanChannel.Usb03,
        PcanChannel.Usb04,
        PcanChannel.Usb05,
        PcanChannel.Usb06,
        PcanChannel.Usb07,
        PcanChannel.Usb08,
    };

    /// <summary>
    /// Help Function used to get an error as text
    /// </summary>
    /// <param name="error"></param>
    /// <returns></returns>
    private string _getFormattedError(PcanStatus error)
    {
        var result = Api.GetErrorText(error, out string errorText);

        if (result != PcanStatus.OK)
            return $"  [{((uint)error):X8}] {error}: Error while retrieving --> {result}";

        return $"  [{((uint)error):X8}] {error}: {errorText}";
    }

    /// <summary>
    /// Configures the PCAN-Trace file for a PCAN-Basic Channel
    /// </summary>
    private void _configureTraceFile()
    {
        //  Configure the maximum size of a trace file to 5 megabytes
        var stsResult = Api.SetValue(_channel, PcanParameter.TraceSize, 5);
        
        if (stsResult != PcanStatus.OK)
            throw new Exception(_getFormattedError(stsResult));
        
        // Configure the way how trace files are created: 
        // * Standard name is used
        // * Existing file is overwritten, 
        // * Only one file is created.
        stsResult = Api.SetValue(_channel, PcanParameter.TraceConfigure, ParameterValue.Trace.SingleFile | ParameterValue.Trace.OverrideExisting);
        
        if (stsResult != PcanStatus.OK)
            throw new Exception(_getFormattedError(stsResult));
    }
    
    private static bool _isStreamMessage(Q21MessageTypes msgType)
    {
        if (msgType == Q21MessageTypes.SendAtoDData)
            return true;

        if (msgType >= Q21MessageTypes.SendAtoDDataMsg2 && msgType <= Q21MessageTypes.SendAtoDDataMsg10)
            return true;

        if (msgType >= Q21MessageTypes.ImpedanceCh1 && msgType <= Q21MessageTypes.ImpedanceCh20)
            return true;

        return false;
    }
    
    private NeurofieldExtendedHeader _decodeNeurofieldExtendedHeader(uint id)
    {
        // Check module type
        var moduleType = (byte)((id & 0x00FF0000) >> 16);
        if (!Enum.IsDefined(typeof(NeurofieldCommunityDeviceType), moduleType))
            return null;
        
        // Check valid msg type from this module
        var msgType = (byte)id;
        if (!Enum.IsDefined(typeof(Q21MessageTypes), msgType))
            return null;

        return new NeurofieldExtendedHeader
        {
            Slave2Host  = (id & 0x1000000) != 0,
            MessageType = (Q21MessageTypes) msgType,
            ModuleType  = (NeurofieldCommunityDeviceType) moduleType,
            Serial      = (byte)((id & 0x0000FF00) >> 8)
        };
        
    }

    private NeurofieldCommunityDevice _processQueryAnswer(PcanMessage msgRx)
    {
        if (msgRx.MsgType != MessageType.Extended)
            throw new Exception("Invalid Message Type received. Is the EEG AMP powered on?");
        
        var header = _decodeNeurofieldExtendedHeader(msgRx.ID);

        if (header == null)
            return null;
        
        if (!header.Slave2Host)
            throw new Exception("Not a Slave message.");
        
        if (_isStreamMessage(header.MessageType)) // discard stream messages
            return null;
        
        if (header.MessageType != Q21MessageTypes.CANBusQuery)
            throw new Exception("Not a Query Answer.");
        
        return new NeurofieldCommunityDevice
        {
            Type = header.ModuleType, 
            Serial = header.Serial
        };

    }

    private PcanMessage _receiveSingleCANBUSMessage(out ulong timestamp)
    {
        var timeoutCounter = 0;
        timestamp = 0;
        
        // receive one CANBUS message
        PcanMessage msgRx;

        while (true)
        {
            var stsResult = Api.Read(_channel, out msgRx, out var timestamp1);
            
            if (stsResult == PcanStatus.InvalidOperation)
                throw new Exception(_getFormattedError(stsResult));
            
            if (stsResult == PcanStatus.BusLight || stsResult == PcanStatus.BusHeavy)
                throw new Exception("Bus error: Is the device still on?");
            
            if (stsResult == PcanStatus.ReceiveQueueEmpty)
            {
                Thread.Sleep(1);
                timeoutCounter += 1;
                if (timeoutCounter >= 1400) // wait at most 1.4 seconds
                    return null;
            }
            else
            {
                timestamp = timestamp1;
                break;
            }
        }

        return msgRx;
    }
    
    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="device">if this is a query message, then device may be null</param>
    /// <param name="msgType"></param>
    /// <param name="data"></param>
    /// <exception cref="Exception"></exception>
    protected void SendMessage(NeurofieldCommunityDevice device, byte msgType, byte[] data)
    {
        uint id=0;
        if (device != null) // if this is a query message, then device may be null
            id = ((uint)device.Type << 16) + ((uint)device.Serial << 8) + msgType;

        var msg = new PcanMessage(id, MessageType.Extended,8,data);

        var stsResult = Api.Write(_channel, msg);

        if (stsResult != PcanStatus.OK)
        {
            if (stsResult == PcanStatus.BusOff) // special case for bus off. Try to recover by un-initialize / initialize
            {
                var tryCount = 0;

                while (tryCount < 30)
                {
                    stsResult = Api.Uninitialize(_channel);
                    
                    if (stsResult != PcanStatus.OK)
                    {
                        tryCount++;
                        continue;
                    }
                    
                    Thread.Sleep(10);
                    
                    stsResult = Api.Initialize(_channel,Bitrate.Pcan500);
                    
                    if (stsResult != PcanStatus.OK)
                    {
                        tryCount++;
                        continue;
                    }
                    
                    Thread.Sleep(10);
                    
                    _configureTraceFile(); // Prepares the PCAN-Basic's PCAN-Trace file
                    
                    stsResult = Api.Write(_channel,msg);
                    
                    if (stsResult != PcanStatus.OK)
                    {
                        tryCount++;
                        continue;
                    }
                    
                    break;
                }
            }else
                throw new Exception("Could not send message over the PCAN-USB interface. Error Code:" + stsResult);
        }
    }
    
    /// <summary>
    /// All connected Neurofield CANBUS devices on this interface
    /// </summary>
    protected List<NeurofieldCommunityDevice> ConnectedDevices { get; private set; }

    /// <summary>
    /// Wait for and receive a message from a single device,
    /// while discarding other messages coming from other devices on the same interface
    /// </summary>
    /// <param name="device"></param>
    /// <param name="header"></param>
    /// <param name="timeStamp"></param>
    /// <returns>received message. Throws a timeout exception if not received</returns>
    protected PcanMessage ReceiveSingleMessageFromDevice(NeurofieldCommunityDevice device, out NeurofieldExtendedHeader header, out ulong timeStamp )
    {
        timeStamp = 0;
        
        while (true)
        {
            var msgRx = _receiveSingleCANBUSMessage(out var timeStamp1);
            
            if (msgRx == null)
                throw new TimeoutException("PCAN Read Timeout");
            
            if (msgRx.MsgType != MessageType.Extended) // not my message type
                continue;
            
            header = _decodeNeurofieldExtendedHeader(msgRx.ID);
            
            if (header == null) // Unknown Device
                continue;
            
            if (!header.Slave2Host || header.Serial != device.Serial || header.ModuleType != device.Type)
                continue; // not my device

            timeStamp = timeStamp1;
            return msgRx;
        }
    }
    
    protected void ResetBuffers()
    {
        // reset tx/rx buffers
        Api.Reset(_channel);
    }

    /// <summary>
    /// This constructor also verifies device connected on this interface.
    /// It takes about 1 second to complete
    /// </summary>
    /// <param name="channel"></param>
    /// <exception cref="Exception"></exception>
    public NeurofieldCommunityCANBUSApiBase(PcanChannel channel)
    {
        // Check for a Plug&Play Handle
        var status = Api.GetValue(channel, PcanParameter.ChannelCondition, out uint buffer);
        
        if (status != PcanStatus.OK)
            throw new Exception("This pcan interface is not available"); 
        
        var condition = (ChannelCondition) buffer;

        if ((condition & ChannelCondition.ChannelAvailable) != ChannelCondition.ChannelAvailable)
        {
            // this interface exist but not available,             
            // we were looking for this and it is not available! Too bad...
            throw new Exception($"The Channel {channel} is NOT available.");
        }
        
        // found the interface          
        _channel = channel;

        try
        {
            status = Api.Initialize(_channel, Bitrate.Pcan500);

            if (status != PcanStatus.OK)
                throw new Exception(_getFormattedError(status));

            _configureTraceFile(); // Prepares the PCAN-Basic's PCAN-Trace file

            // Send the Query message
            SendMessage(null, (byte)Q21MessageTypes.CANBusQuery, new byte[8]);
                
            // Wait 1 seconds to for the responses from connected Neurofield devices (on this interface) appear in the receive queue             
            Thread.Sleep(1000);
                
            ConnectedDevices = new List<NeurofieldCommunityDevice>();

            // read data until queue empty
            while (true)
            {
                status = Api.Read(_channel, out var msgRx);
                    
                if (status == PcanStatus.InvalidOperation || status == PcanStatus.ReceiveQueueEmpty)
                    break;
                    
                if (status == PcanStatus.BusLight)
                    throw new Exception("Bus error: an error counter reached the 'light' limit. Is the device on?");
                    
                var dev = _processQueryAnswer(msgRx);
                    
                if (dev != null)
                    ConnectedDevices.Add(dev);
            }

        }
        catch (Exception)
        {
            Release();
            throw;
        }
    }

    /// <summary>
    /// Returns a list of PCAN interfaces available now
    /// </summary>
    /// <returns></returns>
    public static List<PcanChannel> GetOnlinePcanInterfaces()
    {
        var interfaces = new List<PcanChannel>();

        foreach (var item in HandlesArray)
        {
            // Checks for a Plug&Play Handle
            var stsResult = Api.GetValue(item,PcanParameter.ChannelCondition,out uint buffer);
            
            // continue if this handle is absent or not available
            
            if (stsResult != PcanStatus.OK)
                continue;
                
            var condition = (ChannelCondition) buffer;

            if ((condition & ChannelCondition.ChannelAvailable) != ChannelCondition.ChannelAvailable)
                continue;
            
            // found an interface
            interfaces.Add(item);
        }

        return interfaces;
    }

    public void Release()
    {
        if (_channel == PcanChannel.None)
            return;

        Api.Reset(_channel);
        Api.Uninitialize(_channel);
        _channel = PcanChannel.None;
    }
    
    
}