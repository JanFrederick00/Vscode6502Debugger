using System.IO.Ports;

namespace WDCMonInterface.Devices
{

    class WDCMon : ISystemInterface
    {
        public ushort ScratchLocation => 0x7D00;
        SerialPort port;
        public WDCMon(string port)
        {
            this.port = new SerialPort(port, 57600, Parity.None, 8, StopBits.One);
            this.port.Open();
        }

        private byte[] read_bytes(int numBytes)
        {
            byte[] buffer = new byte[numBytes];

            for (int i = 0; i < numBytes; ++i)
            {
                while (port.BytesToRead <= 0) Thread.Sleep(0);
                var read_block = new byte[port.BytesToRead];
                port.Read(read_block, 0, read_block.Length);
                for (int x = 0; x < read_block.Length; ++x)
                {
                    buffer[i] = read_block[x];
                    ++i;
                }
                --i;
            }

            return buffer;
        }

        public enum Command
        {
            SYNC = 0x00,
            ECHO = 0x01,
            READ_DATA_FROM_PC = 0x02,
            WRITE_DATA_TO_PC = 0x03,
            GET_INFO = 0x04,
            EXEC = 0x05,
            NOOP = 0x06,
            UNUSED = 0x07,
        }

        private byte[] SendCommand(Command cmd, byte[] payload, int numResponseBytes)
        {
            port.Write(new byte[] { 0x55, 0xAA }, 0, 2);
            var response = port.ReadByte();
            if (response != 0xCC) throw new InvalidDataException($"Expected response: 0xCC, actual: 0x{response:X2}");
            port.Write(new byte[] { (byte)cmd }, 0, 1);
            if (payload != null && payload.Length > 0) port.Write(payload, 0, payload.Length);
            if (numResponseBytes > 0) return read_bytes(numResponseBytes);// port.Read(data, 0, numResponseBytes);
            return Array.Empty<byte>();
        }

        public byte[] GetInformation()
        {
            return SendCommand(Command.GET_INFO, new byte[0], 29);
        }

        public byte[] ReadMemory(ushort offset, ushort len)
        {
            byte[] payload = new byte[] {
                (byte)(offset & 0xFF), (byte)(offset >> 8), 0x00,
                (byte)(len & 0xFF), (byte)(len >> 8),
            };

            return SendCommand(Command.WRITE_DATA_TO_PC, payload, len);
        }

        public void WriteMemory(ushort offset, byte[] data)
        {
            byte[] payload = new byte[data.Length + 5];
            payload[0] = (byte)(offset & 0xFF); payload[1] = (byte)(offset >> 8);
            payload[2] = 0x00;
            payload[3] = (byte)(data.Length & 0xFF);
            payload[4] = (byte)(data.Length >> 8);

            for (int i = 0; i < data.Length; ++i)
            {
                payload[i + 5] = data[i];
            }

            SendCommand(Command.READ_DATA_FROM_PC, payload, 0);
        }

        public struct ProcessorState
        {
            public byte A;
            public byte X;
            public byte Y;
            public ushort PC;
            public byte SP;
            public byte Status;
        }

        public ProcessorState GetProcessorState()
        {
            var ShadowRegisters = ReadMemory(0x7e00, 16);
            return new ProcessorState()
            {
                A = ShadowRegisters[0],
                X = ShadowRegisters[2],
                Y = ShadowRegisters[4],
                PC = (ushort)(ShadowRegisters[6] | ShadowRegisters[7] << 8),
                SP = ShadowRegisters[0x0A],
                Status = ShadowRegisters[0x0C]
            };
        }

        public void SetProcessorState(ProcessorState state)
        {
            var ShadowRegisters = ReadMemory(0x7e00, 16);
            ShadowRegisters[0] = state.A;
            ShadowRegisters[1] = 0x00;
            ShadowRegisters[2] = state.X;
            ShadowRegisters[3] = 0x00;
            ShadowRegisters[4] = state.Y;
            ShadowRegisters[5] = 0x00;
            ShadowRegisters[6] = (byte)(state.PC & 0xFF);
            ShadowRegisters[7] = (byte)(state.PC >> 8);
            ShadowRegisters[0x0A] = state.SP;
            ShadowRegisters[0x0C] = state.Status;
            WriteMemory(0x7e00, ShadowRegisters);
        }

        public enum ExecutionHaltReason
        {
            BREAKPOINT = 2,
            NMI = 7,
        }

        public ExecutionHaltReason ContinueExecution()
        {
            var b = SendCommand(Command.EXEC, new byte[0], 1);
            return (ExecutionHaltReason)b[0];
        }

        public ExecutionHaltReason ContinueExecution(ushort nextInstruction)
        {
            var stat = GetProcessorState();
            stat.PC = nextInstruction;
            SetProcessorState(stat);
            return ContinueExecution();
        }
    }
}