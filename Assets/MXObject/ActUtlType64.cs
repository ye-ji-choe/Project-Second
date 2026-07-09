using System;

internal class ActUtlType64
{
    public string ActPassword { get; internal set; }
    public int ActLogicalStationNumber { get; internal set; }

    internal void Close()
    {
        throw new NotImplementedException();
    }

    internal int Open()
    {
        throw new NotImplementedException();
    }

    internal int ReadDeviceRandom2(string allReadAddressesString, int allReadCount, out short v)
    {
        throw new NotImplementedException();
    }

    internal int SetDevice2(string deviceAddress, short writeValue)
    {
        throw new NotImplementedException();
    }

    internal int WriteDeviceRandom2(string deviceAddresses, int count, ref short v)
    {
        throw new NotImplementedException();
    }
}