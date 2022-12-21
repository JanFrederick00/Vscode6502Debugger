namespace WDCMonInterface.Devices
{
    interface ISystemInterface
    {
        byte[] ReadMemory(ushort address, ushort length);
        void WriteMemory(ushort address, byte[] data);
        WDCMon.ProcessorState GetProcessorState();
        void SetProcessorState(WDCMon.ProcessorState state);
        ushort ScratchLocation { get; }
    }
}