using WDCMonInterface.CompilerIntegration;
using WDCMonInterface.Devices;

namespace WDCMonInterface
{
    class Debugger : ISystemInterface
    {
        public enum DebuggerState
        {
            IDLE,
            RUNNING,
            HALTED
        }

        public DebuggerState State { get; private set; } = DebuggerState.IDLE;

        public ushort ScratchLocation => system.ScratchLocation;

        public ushort BreakAt = 0;
        C6502Helper.InstructionHandler? currInstruction = null;
        readonly WDCMon system;
        public readonly int numLoadedBytes = 0;
        public readonly int numLoadedSegments = 0;

        public Debugger(WDCMon system, DbgFile DebugInformation)
        {
            this.system = system;

            // Load Program into the device's Memory
            foreach (var segment in DebugInformation.Segments.Values)
            {
                if (segment.Size < 1)
                {
                    Console.WriteLine($"Skipping Segment {segment.Name} (size=0)");
                }
                else
                {
                    using var binary = File.OpenRead(segment.oname ?? "");
                    
                    binary.Position = segment.ooffs;
                    byte[] segmentData = new byte[segment.Size];
                    binary.Read(segmentData, 0, segmentData.Length);
                    WriteMemory((ushort)segment.Start, segmentData);
                    numLoadedBytes += segmentData.Length;
                    numLoadedSegments++;
                }
            }
        }

        void StartRun()
        {
            DoQueuedActions();
            State = DebuggerState.RUNNING;
            BreakAt = 0;
            currInstruction = null;
        }

        void UpdateState(WDCMon.ExecutionHaltReason haltedWhy)
        {
            State = DebuggerState.HALTED;
            var ProcessorState = system.GetProcessorState();
            if (haltedWhy == WDCMon.ExecutionHaltReason.BREAKPOINT)
            {
                BreakAt = (ushort)(ProcessorState.PC - 2);
            }
            else
            {
                BreakAt = ProcessorState.PC;
            }

            currInstruction = C6502Helper.GetHandlerForInstruction(BreakAt, this);

            DoQueuedActions();
        }

        public WDCMon.ExecutionHaltReason Execute(ushort address)
        {
            return Step(null, address);
        }

        public WDCMon.ExecutionHaltReason StepOver(ushort? fromAddress = null)
        {
            return Step(true, fromAddress);
        }

        public WDCMon.ExecutionHaltReason StepInto(ushort? fromAddress = null)
        {
            return Step(false, fromAddress);
        }

        WDCMon.ExecutionHaltReason Step(bool? over, ushort? fromAddress = null)
        {
            if (over == null && fromAddress == null && currInstruction == null) return WDCMon.ExecutionHaltReason.BREAKPOINT;
            if (State == DebuggerState.RUNNING) return WDCMon.ExecutionHaltReason.BREAKPOINT;
            ClearTemporaryBreakpoints();
            if (fromAddress != null)
            {
                BreakAt = fromAddress.Value;
                currInstruction = C6502Helper.GetHandlerForInstruction(BreakAt, this);
            }

            if (currInstruction == null) return WDCMon.ExecutionHaltReason.BREAKPOINT;

            if (over != null)
            {
                CreateBreakpointAt(currInstruction.GetNextInstruction(over.Value), BreakpointType.TEMPORARY);
            }

            var addr = BreakAt;

            var bp = CurrentBreakpoint();
            if (bp != null)
            {
                var CodeRelocationAddress = ScratchLocation;
                byte[] relocated_code = currInstruction.RelocateInstruction(CodeRelocationAddress);
                WriteMemory(CodeRelocationAddress, relocated_code);
                addr = CodeRelocationAddress;
            }

            StartRun();
            var why = system.ContinueExecution(addr);
            UpdateState(why);
            return why;
        }

        public WDCMon.ExecutionHaltReason ContinueExecution(ushort continueAddress)
        {
            return Step(null, continueAddress);
        }

        readonly List<Breakpoint> Breakpoints = new();

        public void CreateBreakpointAt(ushort addr, BreakpointType type, int reference = -1)
        {
            var original_value = ReadMemory(addr, 1)[0];
            Breakpoints.Add(new Breakpoint() { address = addr, breakpointType = type, originalValue = original_value, reference = reference });
            WriteMemory(addr, new byte[] { 0x00, });
        }

        public void RemoveBreakpointByReference(int reference)
        {
            foreach (var bp in Breakpoints.Where(b => b.reference == reference).ToArray())
            {
                WriteMemory(bp.address, new byte[] { bp.originalValue });
                Breakpoints.Remove(bp);
            }
        }

        public Breakpoint? CurrentBreakpoint()
        {
            return Breakpoints.Where(b => b.address == BreakAt).FirstOrDefault();
        }

        public void ClearTemporaryBreakpoints()
        {
            var tmpbps = Breakpoints.Where(t => t.breakpointType == BreakpointType.TEMPORARY).ToArray();
            foreach (var bp in tmpbps)
            {
                WriteMemory(bp.address, new byte[] { bp.originalValue });
                Breakpoints.Remove(bp);
            }
        }

        public enum BreakpointType
        {
            REGULAR,
            TEMPORARY
        }

        public class Breakpoint
        {
            public ushort address;
            public byte originalValue;
            public BreakpointType breakpointType;
            public int reference = -1;
        }


        public byte[] ReadMemory(ushort address, ushort length)
        {
            byte[] b = system.ReadMemory(address, length);
            for (int i = 0; i < b.Length; ++i)
            {
                ushort addr = (ushort)(address + i);
                var brkpt = Breakpoints.FirstOrDefault(a => a.address == addr);
                if (brkpt != null)
                {
                    b[i] = brkpt.originalValue;
                }
            }
            return b;
        }

        public void WriteMemory(ushort address, byte[] data)
        {
            system.WriteMemory(address, data);
        }

        public WDCMon.ProcessorState GetProcessorState()
        {
            return system.GetProcessorState();
        }

        public void SetProcessorState(WDCMon.ProcessorState state)
        {
            system.SetProcessorState(state);
        }

        readonly List<Action> toDoBeforeNextContinue = new();

        void DoQueuedActions()
        {
            foreach (var toDo in toDoBeforeNextContinue)
            {
                toDo();
            }
            toDoBeforeNextContinue.Clear();
        }

        public void DoBeforeNextContinue(Action toDo)
        {
            if (State != DebuggerState.RUNNING)
            {
                toDo();
            }
            else
            {
                toDoBeforeNextContinue.Add(toDo);
            }
        }

    }
}