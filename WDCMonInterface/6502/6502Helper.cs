using WDCMonInterface.Devices;

namespace WDCMonInterface
{
    class C6502Helper
    {
        private static class InstructionLengths
        {
            public const int ACCUMULATOR = 1;
            public const int IMMEDIATE = 2;
            public const int IMPLIED = 1;
            public const int RELATIVE = 2;
            public const int ABSOLUTE = 3;
            public const int ZEROPAGE = 2;
            public const int INDIRECT = 3;
            public const int ABSOLUTE_INDEXED = 3;
            public const int ZEROPAGE_INDEXED = 2;
            public const int INDEXED_INDIRECT = 2;
            public const int INDIRECT_INDEXED = 2;
        }

        public static int[] InstructionLengthsByOpCode = new int[256] {
            InstructionLengths.IMPLIED + 1, InstructionLengths.INDEXED_INDIRECT,    0,                              0,  2,                                      InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  3,                                  InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDIRECT_INDEXED,    2,                              0,  2,                                      InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    1,                              0,  3,                                  InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.ABSOLUTE,    InstructionLengths.INDEXED_INDIRECT,    0,                              0,  InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDIRECT_INDEXED,    2,                              0,  InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    1,                              0,  InstructionLengths.ABSOLUTE_INDEXED,InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.IMPLIED,     InstructionLengths.INDEXED_INDIRECT,    0,                              0,  0,                                      InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDIRECT_INDEXED,    2,                              0,  0,                                      InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     0,  0,                                  InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.IMPLIED,     InstructionLengths.INDEXED_INDIRECT,    0,                              0,  2,                                      InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  InstructionLengths.INDIRECT,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDEXED_INDIRECT,    2,                              0,  InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE_INDEXED,InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDEXED_INDIRECT,    0,                              0,  InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, 0,                                      InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDIRECT_INDEXED,    2,                              0,  InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     0,  3,                                  InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.IMMEDIATE,   InstructionLengths.INDEXED_INDIRECT,    InstructionLengths.IMMEDIATE,   0,  InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDEXED_INDIRECT,    2,                              0,  InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE_INDEXED,InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.IMMEDIATE,   InstructionLengths.INDEXED_INDIRECT,    0,                              0,  InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     1,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDIRECT_INDEXED,    2,                              0,  0,                                      InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     1,  0,                                  InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3,
            InstructionLengths.IMMEDIATE,   InstructionLengths.INDEXED_INDIRECT,    0,                              0,  InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            InstructionLengths.ZEROPAGE,            2,                              InstructionLengths.IMPLIED, InstructionLengths.IMMEDIATE,           InstructionLengths.IMPLIED,     0,  InstructionLengths.ABSOLUTE,        InstructionLengths.ABSOLUTE,            InstructionLengths.ABSOLUTE,            3,
            InstructionLengths.RELATIVE,    InstructionLengths.INDEXED_INDIRECT,    2,                              0,  0,                                      InstructionLengths.ZEROPAGE_INDEXED,    InstructionLengths.ZEROPAGE_INDEXED,    2,                              InstructionLengths.IMPLIED, InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.IMPLIED,     0,  0,                                  InstructionLengths.ABSOLUTE_INDEXED,    InstructionLengths.ABSOLUTE_INDEXED,    3
        };

        public static InstructionHandler GetHandlerForInstruction(ushort address, ISystemInterface system)
        {
            var instruction_data = system.ReadMemory(address, 3);
            switch (instruction_data[0])
            {
                default:
                    var len = InstructionLengthsByOpCode[instruction_data[0]];
                    if (len == 0) throw new InvalidProgramException($"Illegal Opcode {instruction_data[0]:X2}");
                    return new InstructionHandler(address, system, instruction_data);
                // JMP
                case 0x4C:
                case 0x7C:
                case 0x6C:
                    return new JmpInstructionHandler(address, system, instruction_data);
                // RTS
                case 0x60:
                    return new RTSHandler(address, system, instruction_data, false);
                // RTI
                case 0x40:
                    return new RTSHandler(address, system, instruction_data, true);
                // JSR
                case 0x20:
                    return new JsrInstructionHandler(address, system, instruction_data);
                case 0x10:
                    return new BPLHandler(address, system, instruction_data);
                case 0x30:
                    return new BMIHandler(address, system, instruction_data);
                case 0x50:
                    return new BVCHandler(address, system, instruction_data);
                case 0x70:
                    return new BVSHandler(address, system, instruction_data);
                case 0x80:
                    return new BRAHandler(address, system, instruction_data);
                case 0x90:
                    return new BCCHandler(address, system, instruction_data);
                case 0xD0:
                    return new BNEHandler(address, system, instruction_data);
                case 0xF0:
                    return new BEQHandler(address, system, instruction_data);
                case 0x0F:
                case 0x1F:
                case 0x2F:
                case 0x3F:
                case 0x4F:
                case 0x5F:
                case 0x6F:
                case 0x7F:
                    return new BBRHandler(address, system, instruction_data, instruction_data[0] >> 4);
                case 0x8F:
                case 0x9F:
                case 0xAF:
                case 0xBF:
                case 0xCF:
                case 0xDF:
                case 0xEF:
                case 0xFF:
                    return new BBSHandler(address, system, instruction_data, (instruction_data[0] >> 4) - 8);
            }
        }

        public class InstructionHandler
        {
            internal byte[] instruction_data;
            internal ushort address;
            internal ISystemInterface system;
            public InstructionHandler(ushort address, ISystemInterface system, byte[] instruction_data)
            {
                this.instruction_data = instruction_data;
                this.address = address;
                this.system = system;
            }

            public virtual ushort GetNextInstruction(bool StepOver)
            {
                return (ushort)(address + InstructionLengthsByOpCode[instruction_data[0]]);
            }

            public virtual byte[] RelocateInstruction(ushort Target)
            {
                int iLen = InstructionLengthsByOpCode[instruction_data[0]];
                byte[] data = new byte[iLen + 3];
                for (int i = 0; i < iLen; ++i)
                {
                    data[i] = instruction_data[i];
                }
                data[iLen] = 0x4C;
                var next = GetNextInstruction(true);
                data[iLen + 1] = (byte)(next & 0xFF);
                data[iLen + 2] = (byte)(next >> 8);

                return data;
            }
        }

        abstract class BranchInstructionHandler : InstructionHandler
        {
            public BranchInstructionHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            {

            }

            public abstract bool DoBranch();

            public override ushort GetNextInstruction(bool StepOver)
            {
                if (DoBranch())
                {
                    int relative = instruction_data[1];
                    if ((relative & 0x80) > 0)
                    {
                        relative = (int)(0xFFFFFF00u | this.instruction_data[1]);
                    }
                    return (ushort)(address + 2 + relative);
                }
                else return base.GetNextInstruction(StepOver);
            }

            public override byte[] RelocateInstruction(ushort Target)
            {
                byte[] data = new byte[3];
                data[0] = 0x4C;
                var next = this.GetNextInstruction(false);
                data[1] = (byte)(next & 0xFF);
                data[2] = (byte)(next >> 8);

                return data;
            }
        }

        class BRAHandler : BranchInstructionHandler
        {
            public BRAHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                return true;
            }
        }

        class BPLHandler : BranchInstructionHandler
        {
            public BPLHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 128) == 0);
            }
        }

        class BMIHandler : BranchInstructionHandler
        {
            public BMIHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 128) > 0);
            }
        }

        class BVSHandler : BranchInstructionHandler
        {
            public BVSHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 64) > 0);
            }
        }

        class BVCHandler : BranchInstructionHandler
        {
            public BVCHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 64) == 0);
            }
        }

        class BCCHandler : BranchInstructionHandler
        {
            public BCCHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 0b1) == 0);
            }
        }

        class BCSHandler : BranchInstructionHandler
        {
            public BCSHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 0b01) > 0);
            }
        }

        class BNEHandler : BranchInstructionHandler
        {
            public BNEHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 0b10) == 0);
            }
        }

        class BEQHandler : BranchInstructionHandler
        {
            public BEQHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            { }

            public override bool DoBranch()
            {
                var status = system.GetProcessorState();
                return ((status.Status & 0b10) > 0);
            }
        }

        class BBSHandler : BranchInstructionHandler
        {
            int bit;
            public BBSHandler(ushort address, ISystemInterface system, byte[] instruction_data, int bit) : base(address, system, instruction_data)
            {
                this.bit = bit;
            }

            public override bool DoBranch()
            {
                var zeropage_value = system.ReadMemory(this.instruction_data[1], 1)[0];
                return ((zeropage_value & (1 << 7)) > 0);
            }
        }

        class BBRHandler : BranchInstructionHandler
        {
            int bit;
            public BBRHandler(ushort address, ISystemInterface system, byte[] instruction_data, int bit) : base(address, system, instruction_data)
            {
                this.bit = bit;
            }

            public override bool DoBranch()
            {
                var zeropage_value = system.ReadMemory(this.instruction_data[1], 1)[0];
                return ((zeropage_value & (1 << 7)) == 0);
            }
        }

        class JmpInstructionHandler : InstructionHandler
        {
            public JmpInstructionHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            {

            }

            public override ushort GetNextInstruction(bool StepOver)
            {
                if (instruction_data[0] == 0x4C)
                {
                    return (ushort)((ushort)instruction_data[1] + (((ushort)instruction_data[2]) << 8));
                }
                else
                {
                    var ptrLoc = (ushort)((ushort)instruction_data[1] + (((ushort)instruction_data[2]) << 8));
                    if (instruction_data[0] == 0x7C)
                    {
                        var status = system.GetProcessorState();
                        ptrLoc += status.X;
                    }
                    var addrData = system.ReadMemory(ptrLoc, 2);
                    return (ushort)((ushort)addrData[0] + (((ushort)addrData[1]) << 8));
                }
            }

            public override byte[] RelocateInstruction(ushort Target)
            {
                return instruction_data;
            }
        }

        class JsrInstructionHandler : InstructionHandler
        {
            public JsrInstructionHandler(ushort address, ISystemInterface system, byte[] instruction_data) : base(address, system, instruction_data)
            {

            }

            public override byte[] RelocateInstruction(ushort Target)
            {
                var state = system.GetProcessorState();
                var return_address = address + 2;

                /*
                 *
                 *  Relocating the JSR instruction is more difficult than the other instructions.
                 *  We cannot execute the JSR itself from any other code location, as the RTS-Instruction 
                 *  would return to the scratch space and not the next intended instruction.
                 *  we therefore simulate the instruction by 
                 *  - computing the return address ourselves
                 *  - pushing it to the stack using A as a temporary register
                 *  - pushing the processor status to the stack using A as a temporary register
                 *  - loading A's original value back into A
                 *  - restoring the processor's status register by Pulling it from the stack
                 *  - doing an ordinary JMP to the JSR's target instruction.
                 *  
                 *  This routine works on all 6502 processors and consumes 15 bytes of scratch space.
                 *  
                 */

                byte[] relocated_code = new byte[]
                {
                    0xA9, (byte)((return_address >> 8) & 0xFF),     // LDA (high byte of return address)
                    0x48,                                           // PHA
                    0xA9, (byte)((return_address) & 0xFF),          // LDA (low byte of return address)
                    0x48,                                           // PHA
                    0xA9, state.Status,                             // LDA (status register before relocation)
                    0x48,                                           // PHA
                    0xA9, state.A,                                  // LDA (A register before relocation)
                    0x28,                                           // PLP ; reset processor status to original values
                    0x4C, instruction_data[1], instruction_data[2]  // jmp (jsr target address)
                };
                return relocated_code;
            }

            public override ushort GetNextInstruction(bool StepOver)
            {
                if (StepOver) return base.GetNextInstruction(false);
                else return (ushort)((ushort)instruction_data[1] + (((ushort)instruction_data[2]) << 8));
            }
        }

        class RTSHandler : InstructionHandler
        {
            bool rti;
            public RTSHandler(ushort address, ISystemInterface system, byte[] instruction_data, bool rti) : base(address, system, instruction_data)
            {
                this.rti = rti;
            }

            public override byte[] RelocateInstruction(ushort Target)
            {
                return instruction_data;
            }

            public override ushort GetNextInstruction(bool StepOver)
            {
                var stack = system.ReadMemory(0x100, 0x100);
                var sysState = system.GetProcessorState();

                var sp = (byte)(sysState.SP + 1);
                if (rti) sp += 1;
                ushort addr = stack[sp];
                sp++;
                addr |= (ushort)(stack[sp] << 8);
                if (rti) return addr;
                return (ushort)(addr + 1);
            }
        }
    }

}