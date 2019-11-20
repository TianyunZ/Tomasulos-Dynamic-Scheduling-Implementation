using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Collections;

namespace Tomasulo
{
    public partial class Form1 : Form
    {
        Form2 form2;
        public InstructionUnit instructionUnit = new InstructionUnit();
        Instruction[] originalInstructions;
        ReservationStation loadStation, storeStation, addStation, multiplyStation, branchStation;
        ReorderBuffer ROB;
        FloatingPointRegisters floatRegs;
        IntegerRegisters intRegs;
        FloatingPointMemoryArrary memLocs;
        Speculator speculator;
        private List<Instruction> issuedInstructions = new List<Instruction>();
        private List<bool> predictedBranchTaken = new List<bool>();
        private int[] issueClocks;
        private int [] executeClocks;
        private int [] writeClocks;
        private int[] commitClocks;
        private int step = 0, numIssued = 0;
        int instrOffset = 0;
        int totalIssuedInstr = 0;
        int oldHead = -1;
        string data;
        int loopAddress = -1;//loop指令起始地址
        int[] numbuffer = new int[5] { 2, 2, 2, 2, 1 };//保留站个数初始化
        int[] delay = new int[6] { 2, 2, 2, 2, 2, 1 };//延迟周期初始化
        public int k = 0;
        
        public Form1()
        {
            InitializeComponent();
        }

        public void WriteInstructions()
        {
            string[] sdata = new string[100];//存放每条指令
            string[] stringData = new string[100];
            char[] delimiter = new char[6] { ',',' ','\t','(',')','\n'};
            sdata = data.Split(delimiter);
            int k = 0;
            foreach(string str in sdata)
            {
                if(str != "")
                {
                    stringData[k] = str;
                    k++;
                }
            }

            /*for (int i = 0; i<k; i++)
            {
                Console.WriteLine(stringData[i]);
            }*/
            
            string s1;//第一操作数
            string s2;
            string s3;

            for (int i = 0; i < k-3; i += 4)
            {
                loopAddress++;
                s1 = stringData[i + 1];
                s2 = stringData[i + 2];
                s3 = stringData[i + 3];
                if(stringData[i] == "Loop:")
                {
                    loopAddress--;
                    i-=2;
                    Console.WriteLine("hello");
                }
                else if(stringData[i] == "LD")
                {
                    s2 += '+';
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, s1, s2, s3));
                    i += 1;
                }
                else if(stringData[i] == "SD")
                {
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SD, s1, s2, s3));
                    
                }
                else if(stringData[i] == "MULD")
                {
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.MULD, s1, s2, s3));
                    
                }
                else if(stringData[i] == "DIVD")
                {
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.DIVD, s1, s2, s3));
                    
                }
                else if(stringData[i] == "SUBD")
                {
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SUBD, s1, s2, s3));
                   
                }
                else if(stringData[i] == "ADDD")
                {
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.ADDD, s1, s2, s3));
                   
                }
                else if(stringData[i] == "BNE")
                {
                    s1 = '-' + loopAddress.ToString();
                    instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.BNE, s1, s2, s3));
                    
                }
            }
        }

        public void WriteParts()
        {
            string[] sdata = new string[100];//存放每条指令
            string[] stringData = new string[100];
            char[] delimiter = new char[4] { ',', ' ', '\t', '\n' };
            sdata = data.Split(delimiter);
            int k = 0;
            foreach (string str in sdata)
            {
                if (str != "")
                {
                    stringData[k] = str;
                    k++;
                }
            }
            for(int i = 0; i < k; i+=3)
            {
                if (stringData[i] == "Load")
                {
                    delay[2] = stringData[i + 1][0] - '0';
                    numbuffer[0] = stringData[i + 2][0] - '0';
                }
                else if (stringData[i] == "Store")
                {
                    delay[3] = stringData[i + 1][0] - '0';
                    numbuffer[1] = stringData[i + 2][0] - '0';
                }
                else if (stringData[i] == "Add")
                {
                    delay[0] = stringData[i + 1][0] - '0';
                    numbuffer[2] = stringData[i + 2][0] - '0';
                }
                else if (stringData[i] == "Multi")
                {
                    delay[1] = int.Parse(stringData[i + 1]);
                    delay[4] = int.Parse(stringData[i + 2]);
                    numbuffer[3] = stringData[i + 3][0] - '0';
                    i++;
                }
                else if (stringData[i] == "Branch")
                {
                    delay[5] = int.Parse(stringData[i + 1]);
                    numbuffer[4] = stringData[i + 2][0] - '0';
                }
            }
            for(int j = 0; j < 5; j++)
            {
                Console.WriteLine(delay[j]);
            }
        }

        public void ReadInstructions()//读入文件
        {
            byte[] byData = new byte[1000];
            char[] charData = new char[1000];
            FileStream file = new FileStream("C:\\Users\\zty\\Documents\\大二\\计算机原理与体系结构\\第四次上机\\第二题\\program.txt", FileMode.Open);
            file.Seek(0, SeekOrigin.Begin);
            file.Read(byData, 0, 1000);
            Decoder d = Encoding.Default.GetDecoder();
            d.GetChars(byData, 0, byData.Length, charData, 0);
            data = Encoding.UTF8.GetString(byData);
            WriteInstructions();
            file.Close();
        }

        public void ReadParts()//读入文件
        {
            byte[] byData = new byte[1000];
            char[] charData = new char[1000];
            FileStream file = new FileStream("C:\\Users\\zty\\Documents\\大二\\计算机原理与体系结构\\第四次上机\\第二题\\parts.txt", FileMode.Open);
            file.Seek(0, SeekOrigin.Begin);
            file.Read(byData, 0, 1000);
            Decoder d = Encoding.Default.GetDecoder();
            d.GetChars(byData, 0, byData.Length, charData, 0);
            data = Encoding.UTF8.GetString(byData);
            WriteParts();
            file.Close();
        }

        private void loadBuffers_SelectedIndexChanged(object sender, EventArgs e)
        {}

        public void Init()
        {
            step = 0;
            originalInstructions = instructionUnit.GetCurrentInstructions();

            issueClocks = new int[100];
            executeClocks = new int[100];
            writeClocks = new int[100];
            commitClocks = new int[100];

            for (int i = 0; i < 100; i++)
            {
                issueClocks[i] = -1;
                executeClocks[i] = -1;
                writeClocks[i] = -1;
                commitClocks[i] = -1;
                predictedBranchTaken.Add(false);
            }

            speculator = new Speculator();

            loadStation = new ReservationStation(numbuffer[0], ReservationStation.RSType.Load);
            storeStation = new ReservationStation(numbuffer[1], ReservationStation.RSType.Store);
            addStation = new ReservationStation(numbuffer[2], ReservationStation.RSType.Add);
            multiplyStation = new ReservationStation(numbuffer[3], ReservationStation.RSType.Multiply);
            branchStation = new ReservationStation(numbuffer[4], ReservationStation.RSType.Multiply);

            ROB = new ReorderBuffer();

            floatRegs = new FloatingPointRegisters(30);
            int[] FR = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };
            for (int i = 0; i < 30; i++)
            {
                floatRegs.Set(WaitInfo.WaitState.Avail, 2, i);
            }
            
            intRegs = new IntegerRegisters(30);
            for (int i = 0; i < 30; i++)
            {
                intRegs.Set(WaitInfo.WaitState.Avail, 3, i);
            }
            memLocs = new FloatingPointMemoryArrary(64);
            for (int i = 0; i < 64; i++)
            {
                memLocs.Set(10, i);
            }

            UpdateInstructionQueueBox();
            UpdateIssuedInstructionsBox();
            UpdateReservationStationBoxes();
            UpdateFPRegisterBox();
            UpdateIntRegisterBox();
            UpdateClockCountBox();
            UpdateROBBox();

#if false
            for (int c = 0; c < 47; c++)
            {
                clocks++;
                RunOneCycle();
                UpdateClockCountBox();
            }
#endif
        }

        private void Form1_Load(object sender, EventArgs e)//指令初始化
        {
            form2 = new Form2();
            ReadInstructions();
            ReadParts();

            // Temporary. Will eventually get from user.
            /*instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F6", "34+", "R2"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F2", "45+", "R3"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SD, "F2", "3", "R3"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F25", "3+", "0"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.MULD, "F0", "F2", "F4"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SUBD, "F8", "F0", "F6"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.DIVD, "F10", "F0", "F6"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.ADDD, "F6", "F8", "F2"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.BNE, "-8", "F6", "F10"));
            /*instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F6", "34+", "R2"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.BEQ, "-1", "F28", "F29"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F2", "45+", "R3"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SD, "F2", "3", "R3"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.LD, "F25", "3+", "0"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.MULD, "F0", "F2", "F4"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.SUBD, "F8", "F0", "F6"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.DIVD, "F10", "F0", "F6"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.ADDD, "F7", "F8", "F2"));
            instructionUnit.AddInstruction(new Instruction(Instruction.Opcodes.BNE, "-8", "F7", "F10"));*/

            Init();
        }

        private void UpdateROBBox()
        {
            ReorderBuf.Clear();
            ReorderBuf.View = View.Details;
            ReorderBuf.Columns.Add("Entry", 40);
            ReorderBuf.Columns.Add("Busy", 50);
            ReorderBuf.Columns.Add("Inst", 50);
            ReorderBuf.Columns.Add("Dest", 50);
            ReorderBuf.Columns.Add("J", 50);
            ReorderBuf.Columns.Add("K", 50);
            ReorderBuf.Columns.Add("State", 50);
            ReorderBuf.Columns.Add("Result", 50);
            
            for (int i = 0; i < ROB.robSize; i++)
            {
                ListViewItem row = new ListViewItem((i + 1).ToString());
                row.SubItems.Add(ROB.inUse[i].ToString());
                row.SubItems.Add(ROB.instrType[i].ToString());
                row.SubItems.Add((ROB.destType[i] == ReorderBuffer.ROBDestination.FMem ? "MEM" :
                    (ROB.destType[i] == ReorderBuffer.ROBDestination.FReg ? "F" :
                    "R")) + ROB.destLoc[i].ToString());
                row.SubItems.Add((ROB.j[i] == null) ? "" :
                    ROB.j[i].PrintString());
                row.SubItems.Add((ROB.k[i] == null) ? "" :
                    ROB.k[i].PrintString());
                row.SubItems.Add(ROB.state[i].ToString());
                row.SubItems.Add(ROB.result[i].ToString());
                ReorderBuf.Items.Add(row);
            }
        }

        private void UpdateIssuedInstructionsBox()
        {
            issuedInstructionBox.Clear();
            issuedInstructionBox.View = View.Details;
            issuedInstructionBox.Columns.Add("Inst", 50);
            issuedInstructionBox.Columns.Add("Dest", 50);
            issuedInstructionBox.Columns.Add("J", 50);
            issuedInstructionBox.Columns.Add("K", 50);
            issuedInstructionBox.Columns.Add("Issued", 50);
            issuedInstructionBox.Columns.Add("Exec'd", 50);
            issuedInstructionBox.Columns.Add("Written", 50);
            issuedInstructionBox.Columns.Add("Comm'd", 50);

            int i = 0;
            foreach (Instruction inst in issuedInstructions)
            {
                ListViewItem row = new ListViewItem(inst.opcode.ToString());
                row.SubItems.Add(inst.dest);
                row.SubItems.Add(inst.j);
                row.SubItems.Add(inst.k);

                row.SubItems.Add(issueClocks[i].ToString());
                row.SubItems.Add(executeClocks[i].ToString());
                row.SubItems.Add(writeClocks[i].ToString());
                row.SubItems.Add(commitClocks[i].ToString());

                issuedInstructionBox.Items.Add(row);
                i++;
            }
        }

        // Fill box with info in instructions.

        private void UpdateInstructionsBox()
        {
            issuedInstructionBox.Clear();
            issuedInstructionBox.View = View.Details;
            issuedInstructionBox.Columns.Add("Inst", 50);
            issuedInstructionBox.Columns.Add("Dest", 50);
            issuedInstructionBox.Columns.Add("J", 50);
            issuedInstructionBox.Columns.Add("K", 50);
            issuedInstructionBox.Columns.Add("Issued", 50);
            issuedInstructionBox.Columns.Add("Exec'd", 50);
            issuedInstructionBox.Columns.Add("Written", 50);
            issuedInstructionBox.Columns.Add("Comm'd", 50);

            int i = 0;
            foreach (Instruction inst in issuedInstructions)
            {
                ListViewItem row = new ListViewItem(inst.opcode.ToString());
                row.SubItems.Add(inst.dest);
                row.SubItems.Add(inst.j);
                row.SubItems.Add(inst.k);
                
                row.SubItems.Add(issueClocks[i].ToString());
                row.SubItems.Add(executeClocks[i].ToString());
                row.SubItems.Add(writeClocks[i].ToString());
                row.SubItems.Add(commitClocks[i].ToString());

                issuedInstructionBox.Items.Add(row);
                i++;
            }
        }

        // Fill box with info in instructions.
        private void UpdateInstructionQueueBox()
        {
            instructionQueue.Clear();
            instructionQueue.View = View.Details;
            instructionQueue.Columns.Add("Inst", 50);
            instructionQueue.Columns.Add("Dest", 50);
            instructionQueue.Columns.Add("J", 50);
            instructionQueue.Columns.Add("K", 50);

            foreach (Instruction inst in instructionUnit.GetCurrentInstructions())
            {
                ListViewItem row = new ListViewItem(inst.opcode.ToString());
                row.SubItems.Add(inst.dest);
                row.SubItems.Add(inst.j);
                row.SubItems.Add(inst.k);
                instructionQueue.Items.Add(row);
            }
        }

        // Fill box with info in instructions.
        private void UpdateReservationStationBoxes()
        {
            // Add.
            reservationStation1.Clear();
            reservationStation1.View = View.Details;
            reservationStation1.Columns.Add("Name", 50);
            reservationStation1.Columns.Add("Busy", 35);
            reservationStation1.Columns.Add("Op", 50);
            reservationStation1.Columns.Add("Vj", 50);
            reservationStation1.Columns.Add("Vk", 50);
            reservationStation1.Columns.Add("Qj", 50);
            reservationStation1.Columns.Add("Qk", 50);
            reservationStation1.Columns.Add("A", 50);

            for (int i = 0; i < addStation.numBuffers; i++)
            {
                ListViewItem row = new ListViewItem("Add" + (i + 1).ToString());
                row.SubItems.Add(addStation.IsBusy(i).ToString().Substring(0, 1));
                row.SubItems.Add(addStation.opCodes[i].ToString());
                row.SubItems.Add((addStation.Vj[i] == null) ? "" :
                    addStation.Vj[i].PrintString());
                row.SubItems.Add((addStation.Vk[i] == null) ? "" :
                    addStation.Vk[i].PrintString());
                row.SubItems.Add((addStation.Qj[i] == null) ? "" :
                    addStation.Qj[i].waitState.ToString().Substring(0, 1) + (addStation.Qj[i].value + 1));
                row.SubItems.Add((addStation.Qk[i] == null) ? "" :
                    addStation.Qk[i].waitState.ToString().Substring(0, 1) + (addStation.Qk[i].value + 1));
                row.SubItems.Add(addStation.results[i].ToString());
                reservationStation1.Items.Add(row);
            }

            // Multiply.
            reservationStation2.Clear();
            reservationStation2.View = View.Details;
            reservationStation2.Columns.Add("Name", 50);
            reservationStation2.Columns.Add("Busy", 35);
            reservationStation2.Columns.Add("Op", 50);
            reservationStation2.Columns.Add("Vj", 50);
            reservationStation2.Columns.Add("Vk", 50);
            reservationStation2.Columns.Add("Qj", 50);
            reservationStation2.Columns.Add("Qk", 50);
            reservationStation2.Columns.Add("A", 50);

            for (int i = 0; i < multiplyStation.numBuffers; i++)
            {
                ListViewItem row = new ListViewItem("Mult" + (i + 1).ToString());
                row.SubItems.Add(multiplyStation.IsBusy(i).ToString().Substring(0, 1));
                row.SubItems.Add(multiplyStation.opCodes[i].ToString());
                row.SubItems.Add((multiplyStation.Vj[i] == null) ? "" :
                    multiplyStation.Vj[i].PrintString());
                row.SubItems.Add((multiplyStation.Vk[i] == null) ? "" :
                    multiplyStation.Vk[i].PrintString());
                row.SubItems.Add((multiplyStation.Qj[i] == null) ? "" :
                    multiplyStation.Qj[i].waitState.ToString().Substring(0, 1) + (multiplyStation.Qj[i].value + 1));
                row.SubItems.Add((multiplyStation.Qk[i] == null) ? "" :
                    multiplyStation.Qk[i].waitState.ToString().Substring(0, 1) + (multiplyStation.Qk[i].value + 1));
                row.SubItems.Add(multiplyStation.results[i].ToString());
                reservationStation2.Items.Add(row);
            }

            // Load.
            loadBuffers.Clear();
            loadBuffers.View = View.Details;
            loadBuffers.Columns.Add("Name", 50);
            loadBuffers.Columns.Add("Busy", 35);
            loadBuffers.Columns.Add("Addr", 50);
            
            for (int i = 0; i < loadStation.numBuffers; i++)
            {
                ListViewItem row = new ListViewItem("Load" + (i + 1).ToString());
                row.SubItems.Add(loadStation.IsBusy(i).ToString().Substring(0, 1));
                row.SubItems.Add(loadStation.addresses[i].ToString());
                loadBuffers.Items.Add(row);
            }

            // Store.
            storeBuffers.Clear();
            storeBuffers.View = View.Details;
            storeBuffers.Columns.Add("Name", 40);
            storeBuffers.Columns.Add("Busy", 35);
            storeBuffers.Columns.Add("Addr", 50);
            storeBuffers.Columns.Add("Q", 50);
            storeBuffers.Columns.Add("V", 50);

            for (int i = 0; i < storeStation.numBuffers; i++)
            {
                ListViewItem row = new ListViewItem("Store" + (i + 1).ToString());
                row.SubItems.Add(storeStation.IsBusy(i).ToString().Substring(0, 1));
                row.SubItems.Add(storeStation.addresses[i].ToString());
                row.SubItems.Add((storeStation.Qj[i] == null) ? "" :
                    storeStation.Qj[i].waitState.ToString().Substring(0, 1) + (storeStation.Qj[i].value + 1));
                row.SubItems.Add((storeStation.Vj[i] == null) ? "" :
                    storeStation.Vj[i].PrintString());
                storeBuffers.Items.Add(row);
            }
        }

        private void UpdateFPRegisterBox()
        {
            fpRegisters.Clear();
            fpRegisters.View = View.Details;
            fpRegisters.Columns.Add("");
            for (int i = 0; i < floatRegs.GetNumRegs(); i++)
            {
                fpRegisters.Columns.Add("F" + i.ToString());
            }

            ListViewItem row = new ListViewItem("FU");
            for (int i = 0; i < floatRegs.GetNumRegs(); i++)
            {
                switch (floatRegs.Get(i).waitState)
                {
                    case WaitInfo.WaitState.AddStation:
                        row.SubItems.Add("Add" + (floatRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.Avail:
                        row.SubItems.Add(floatRegs.Get(i).value.ToString());
                        break;

                    case WaitInfo.WaitState.Compute:
                        row.SubItems.Add(floatRegs.Get(i).value.ToString());
                        break;

                    case WaitInfo.WaitState.LoadStation:
                        row.SubItems.Add("Load" + (floatRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.MultStation:
                        row.SubItems.Add("Mult" + (floatRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.StoreStation:
                        row.SubItems.Add("Store" + (floatRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.ReorderBuffer:
                        row.SubItems.Add("ROB" + (floatRegs.Get(i).value + 1).ToString());
                        break;
                }
            }
            fpRegisters.Items.Add(row);
        }

        private void UpdateIntRegisterBox()
        {
            intRegisters.Clear();
            intRegisters.View = View.Details;
            intRegisters.Columns.Add("");
            for (int i = 0; i < intRegs.GetNumRegs(); i++)
            {
                intRegisters.Columns.Add("R" + i.ToString());
            }

            ListViewItem row = new ListViewItem("FU");
            for (int i = 0; i < intRegs.GetNumRegs(); i++)
            {
                switch (intRegs.Get(i).waitState)
                {
                    case WaitInfo.WaitState.AddStation:
                        row.SubItems.Add("Add" + (intRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.Avail:
                        row.SubItems.Add(intRegs.Get(i).value.ToString());
                        break;

                    case WaitInfo.WaitState.Compute:
                        row.SubItems.Add(intRegs.Get(i).value.ToString());
                        break;

                    case WaitInfo.WaitState.LoadStation:
                        row.SubItems.Add("Load" + (intRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.MultStation:
                        row.SubItems.Add("Mult" + (intRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.StoreStation:
                        row.SubItems.Add("Store" + (intRegs.Get(i).value + 1).ToString());
                        break;

                    case WaitInfo.WaitState.ReorderBuffer:
                        row.SubItems.Add("ROB" + (intRegs.Get(i).value + 1).ToString());
                        break;
                }
            }
            intRegisters.Items.Add(row);
        }

        // This is the main method that will run a cycle using Tomasulo's Algorithm.
        private void RunOneCycle()
        {   // Do backwards so that only 1 stage is run on each instruction per cycle.
            Commit();
            Write();
            Execute();
            Issue();

            UpdateInstructionQueueBox();
            UpdateIssuedInstructionsBox();
            UpdateReservationStationBoxes();
            UpdateFPRegisterBox();
            UpdateIntRegisterBox();
            UpdateROBBox();
        }

        private WaitInfo FindRegister(string name)
        {
            if (name.Substring(0, 1) == "F")
            {   // Floating Point.
                return floatRegs.Get(Int32.Parse(name.Substring(1)));
            }
            else if (name.Substring(0, 1) == "R")
            {   // Integer.
                //jReg = intRegs.Get(Int32.Parse(instruction.j.Substring(1)));
                return intRegs.Get(Int32.Parse(name.Substring(1)));
            }
            else if (name[name.Length - 1] == '+')
            {   // Number offset.
                return new WaitInfo(float.Parse(name.Substring(0, name.Length - 1)),
                    WaitInfo.WaitState.Avail);
            }
            else
            {   // Number.
                return new WaitInfo(float.Parse(name), WaitInfo.WaitState.Avail);
            }
        }

        private void Issue()
        {
            // If empty.
            if (instructionUnit.GetCurrentInstructions().Length == 0)
            {
                Console.WriteLine("HelloWord!");
                return;
            }
            int bufNum, robNum;
            Instruction instruction = instructionUnit.PeekAtInstruction();
            Operand jReg, kReg;
            WaitInfo wsJ, wsK;

            // Get Source Regs.
            jReg = new Operand(instruction.j);
            kReg = new Operand(instruction.k);
            wsJ = FindRegister(instruction.j);
            wsK = FindRegister(instruction.k);

            switch (instruction.instType)
            {
                case Instruction.InstructionType.Add:
                    if ((bufNum = addStation.GetFreeBuffer()) != -1 && (robNum = ROB.GetFreeBuffer()) != -1)
                    {
                        if (robNum == oldHead) break;
                        //new Operand(instruction.j);
                        // Put in Reservation Station.
                        issuedInstructions.Add(instruction);
                        issueClocks[numIssued++] = step;
                        addStation.PutInBuffer(instructionUnit.GetInstruction(),
                            bufNum, jReg, kReg, wsJ, wsK, ROB, robNum, delay[0]);
                        addStation.instrNum[bufNum] = issuedInstructions.Count - 1;
                        ROB.instrNum[robNum] = issuedInstructions.Count - 1;
                        ROB.ReserveBuffer(robNum, Instruction.InstructionType.Add);
                        if (addStation.dest[bufNum].opType == Operand.OperandType.FloatReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FReg;
                        }
                        else if (addStation.dest[bufNum].opType == Operand.OperandType.IntReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.IReg;
                        }
                        else
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FMem;
                        }
                        ROB.destLoc[robNum] = (int) addStation.dest[bufNum].opVal;

                        // Set Dest Reg.
                        if (instruction.dest.Substring(0, 1) == "F")
                        {   // Float.
                            floatRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        else
                        {   // Int.
                            intRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        totalIssuedInstr++;
                    }
                    else
                    {
                        Console.WriteLine("Stalling due to a structural hazard.");
                    }
                    break;

                case Instruction.InstructionType.Multiply:
                    if ((bufNum = multiplyStation.GetFreeBuffer()) != -1 && (robNum = ROB.GetFreeBuffer()) != -1)
                    {
                        if (robNum == oldHead) break;
                        // Issue.
                        issuedInstructions.Add(instruction);
                        issueClocks[numIssued++] = step;
                        multiplyStation.PutInBuffer(instructionUnit.GetInstruction(),
                            bufNum, jReg, kReg, wsJ, wsK, ROB, robNum, delay[1]);
                        multiplyStation.instrNum[bufNum] = issuedInstructions.Count - 1;
                        ROB.instrNum[robNum] = issuedInstructions.Count - 1;
                        ROB.ReserveBuffer(robNum, Instruction.InstructionType.Multiply);
                        if (multiplyStation.dest[bufNum].opType == Operand.OperandType.FloatReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FReg;
                        }
                        else if (multiplyStation.dest[bufNum].opType == Operand.OperandType.IntReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.IReg;
                        }
                        else
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FMem;
                        }
                        ROB.destLoc[robNum] = (int) multiplyStation.dest[bufNum].opVal;

                        // Set Dest Reg.
                        if (instruction.dest.Substring(0, 1) == "F")
                        {
                            floatRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        else
                        {
                            intRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        totalIssuedInstr++;
                    }
                    else
                    {
                        Console.WriteLine("Stalling due to a structural hazard.");
                    }
                    break;

                case Instruction.InstructionType.Load:
                    if ((bufNum = loadStation.GetFreeBuffer()) != -1 && (robNum = ROB.GetFreeBuffer()) != -1)
                    {
                        if (robNum == oldHead) break;
                        // Issue.
                        issuedInstructions.Add(instruction);
                        issueClocks[numIssued++] = step;
                        loadStation.PutInBuffer(instructionUnit.GetInstruction(),
                            bufNum, jReg, kReg, wsJ, wsK, ROB, robNum, delay[1]);
                        loadStation.instrNum[bufNum] = issuedInstructions.Count - 1;
                        ROB.instrNum[robNum] = issuedInstructions.Count - 1;
                        ROB.ReserveBuffer(robNum, Instruction.InstructionType.Load);
                        if (loadStation.dest[bufNum].opType == Operand.OperandType.FloatReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FReg;
                        }
                        else if (loadStation.dest[bufNum].opType == Operand.OperandType.IntReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.IReg;
                        }
                        else
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FMem;
                        }
                        ROB.destLoc[robNum] = (int) loadStation.dest[bufNum].opVal;

                        // Set Dest Reg.
                        if (instruction.dest.Substring(0, 1) == "F")
                        {
                            floatRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        else
                        {
                            intRegs.Set(WaitInfo.WaitState.ReorderBuffer, robNum,
                            Int32.Parse(instruction.dest.Substring(1)));
                        }
                        totalIssuedInstr++;
                    }
                    else
                    {
                        Console.WriteLine("Stalling due to a structural hazard.");
                    }
                    break;

                case Instruction.InstructionType.Store:
                    if ((bufNum = storeStation.GetFreeBuffer()) != -1 && (robNum = ROB.GetFreeBuffer()) != -1)
                    {
                        if (robNum == oldHead) break;
                        // Issue.
                        issuedInstructions.Add(instruction);
                        issueClocks[numIssued++] = step;
                        storeStation.PutInBuffer(instructionUnit.GetInstruction(),
                            bufNum, jReg, kReg, wsJ, wsK, ROB, robNum, delay[2]);
                        storeStation.instrNum[bufNum] = issuedInstructions.Count - 1;
                        ROB.instrNum[robNum] = issuedInstructions.Count - 1;
                        ROB.ReserveBuffer(robNum, Instruction.InstructionType.Store);
                        if (storeStation.dest[bufNum].opType == Operand.OperandType.FloatReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FReg;
                        }
                        else if (storeStation.dest[bufNum].opType == Operand.OperandType.IntReg)
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.IReg;
                        }
                        else
                        {
                            ROB.destType[robNum] = ReorderBuffer.ROBDestination.FMem;
                        }
                        ROB.destLoc[robNum] = (int) storeStation.dest[bufNum].opVal;
                        totalIssuedInstr++;
                    }
                    else
                    {
                        Console.WriteLine("Stalling due to a structural hazard.");
                    }
                    break;

                case Instruction.InstructionType.Branch:
                    if ((bufNum = branchStation.GetFreeBuffer()) != -1 && (robNum = ROB.GetFreeBuffer()) != -1)
                    {
                        if (robNum == oldHead) break;
                        // First, predict.
                        bool prediction = speculator.GetBranchPrediction();

                        // Issue.
                        issuedInstructions.Add(instruction);
                        issueClocks[numIssued++] = step;
                        branchStation.PutInBuffer(instructionUnit.GetInstruction(),
                            bufNum, jReg, kReg, wsJ, wsK, ROB, robNum, delay[5]);
                        branchStation.instrNum[bufNum] = issuedInstructions.Count - 1;
                        ROB.instrNum[robNum] = issuedInstructions.Count - 1;
                        ROB.ReserveBuffer(robNum, Instruction.InstructionType.Branch);
                        ROB.prediction[robNum] = prediction;
                        totalIssuedInstr++;
                    }
                    else
                    {
                        Console.WriteLine("Stalling due to a structural hazard.");
                    }
                    break;
            }
        }

        private void Execute()
        {
            int result;

            // Add Station.
            for (int i = 0; i < addStation.numBuffers; i++)
            {
                if ((result = addStation.RunExecution(i)) == -1)
                {   // Check operand avalability.
                    if (addStation.Qj[i] != null)
                    {
                        if (addStation.Qj[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            addStation.Vj[i] = new Operand(Operand.OperandType.Num, addStation.Qj[i].value);
                            ROB.j[addStation.robNum[i]] = addStation.Vj[i];
                            addStation.Qj[i] = null;
                        }
                    }
                    if (addStation.Qk[i] != null)
                    {
                        if (addStation.Qk[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            addStation.Vk[i] = new Operand(Operand.OperandType.Num, addStation.Qk[i].value);
                            ROB.k[addStation.robNum[i]] = addStation.Vk[i];
                            addStation.Qk[i] = null;
                        }
                    }
                    addStation.cyclesToComplete[i] = delay[0] - 1;
                    addStation.isReady[i] = false;
                    if (addStation.robNum[i] != -1)
                    {
                        if (ROB.state[addStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[addStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                    addStation.cyclesToComplete[i] = delay[0] - 1;    // Arbitrary, make user-settable later.
                }
                
                else if (result == 0)
                {
                    if (executeClocks[addStation.instrNum[i]] == -1)
                    {
                        executeClocks[addStation.instrNum[i]] = step - delay[0];
                    }
                    if (addStation.robNum[i] != -1)
                    {
                        if (ROB.state[addStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[addStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                }
            }

            // Multiply Station.
            for (int i = 0; i < multiplyStation.numBuffers; i++)
            {
                if ((result = multiplyStation.RunExecution(i)) == -1)
                {   // Check operand availability.
                    if (multiplyStation.Qj[i] != null)
                    {
                        if (multiplyStation.Qj[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            multiplyStation.Vj[i] = new Operand(Operand.OperandType.Num, multiplyStation.Qj[i].value);
                            ROB.j[multiplyStation.robNum[i]] = multiplyStation.Vj[i];
                            multiplyStation.Qj[i] = null;
                        }
                    }
                    if (multiplyStation.Qk[i] != null)
                    {
                        if (multiplyStation.Qk[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            multiplyStation.Vk[i] = new Operand(Operand.OperandType.Num, multiplyStation.Qk[i].value);
                            ROB.k[multiplyStation.robNum[i]] = multiplyStation.Vk[i];
                            multiplyStation.Qk[i] = null;
                        }
                    }
                    if (multiplyStation.robNum[i] != -1)
                    {
                        if (ROB.state[multiplyStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[multiplyStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                    multiplyStation.cyclesToComplete[i] = delay[1] - 1;    // Arbitrary, make user-settable later.
                }
                else if (result == 0)
                {
                    if (executeClocks[multiplyStation.instrNum[i]] == -1)
                    {
                        executeClocks[multiplyStation.instrNum[i]] = step - delay[1];
                    }
                    if (multiplyStation.robNum[i] != -1)
                    {
                        if (ROB.state[multiplyStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[multiplyStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                }
                
            }

            // Load Station.
            for (int i = 0; i < loadStation.numBuffers; i++)
            {
                if (loadStation.addrReady[i])
                {   // Address Computed.
                    loadStation.GetFromMemory(i, memLocs);
                    if (executeClocks[loadStation.instrNum[i]] == -1)
                    {
                        executeClocks[loadStation.instrNum[i]] = step;
                    }
                    if (loadStation.robNum[i] != -1)
                    {
                        if (ROB.state[loadStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[loadStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                }
                else
                {   // Compute Address.
                    if (loadStation.RunExecution(i) == -1)
                    {   // Check operand availability.
                        if (loadStation.Qj[i] != null)
                        {
                            if (loadStation.Qj[i].waitState == WaitInfo.WaitState.Avail)
                            {
                                loadStation.Vj[i] = new Operand(Operand.OperandType.Num, loadStation.Qj[i].value);
                                ROB.j[loadStation.robNum[i]] = loadStation.Vj[i];
                                loadStation.Qj[i] = null;
                            }
                        }
                        if (loadStation.Qk[i] != null)
                        {
                            if (loadStation.Qk[i].waitState == WaitInfo.WaitState.Avail)
                            {
                                loadStation.Vk[i] = new Operand(Operand.OperandType.Num, loadStation.Qk[i].value);
                                ROB.k[loadStation.robNum[i]] = loadStation.Vk[i];
                                loadStation.Qk[i] = null;
                            }
                        }
                    }
                    else
                    {
                        loadStation.addresses[i] = loadStation.ComputeAddress(intRegs, i);
                        loadStation.cyclesToComplete[i] = delay[2];    // Arbitrary, make user-settable later.
                        loadStation.remainingCycles[i] = -1;
                        loadStation.isReady[i] = false;
                        loadStation.addrReady[i] = true;
                    }
                }
                if (loadStation.robNum[i] != -1)
                {
                    if (ROB.state[loadStation.robNum[i]] == ReorderBuffer.State.Issue)
                    {
                        ROB.state[loadStation.robNum[i]] = ReorderBuffer.State.Execute;
                    }
                }
            }

            // Store Station.
            for (int i = 0; i < storeStation.numBuffers; i++)
            {
                if (storeStation.addrReady[i])
                {   // Address Computed.
                    storeStation.BufferValue(i, floatRegs, intRegs);
                    if (executeClocks[storeStation.instrNum[i]] == -1)
                    {
                        executeClocks[storeStation.instrNum[i]] = step;
                    }
                    if (storeStation.robNum[i] != -1)
                    {
                        if (ROB.state[storeStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[storeStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                }
                else
                {   // Compute Address.
                    if (storeStation.RunExecution(i) == -1)
                    {   // Check operand availability.
                        if (storeStation.Qj[i] != null)
                        {
                            if (storeStation.Qj[i].waitState == WaitInfo.WaitState.Avail)
                            {
                                storeStation.Vj[i] = new Operand(Operand.OperandType.Num, storeStation.Qj[i].value);
                                ROB.j[storeStation.robNum[i]] = storeStation.Vj[i];
                                storeStation.Qj[i] = null;
                            }
                        }
                    }
                    else
                    {
                        storeStation.addresses[i] = storeStation.ComputeAddress(intRegs, i);
                        storeStation.cyclesToComplete[i] = delay[3];   // Arbitrary, make user-settable later.
                        storeStation.remainingCycles[i] = -1;
                        storeStation.isReady[i] = false;
                        storeStation.addrReady[i] = true;
                    }
                    if (storeStation.robNum[i] != -1)
                    {
                        if (ROB.state[storeStation.robNum[i]] == ReorderBuffer.State.Issue)
                        {
                            ROB.state[storeStation.robNum[i]] = ReorderBuffer.State.Execute;
                        }
                    }
                }
            }

            // Branch Station.
            for (int i = 0; i < branchStation.numBuffers; i++)
            {
                if ((result = branchStation.RunExecution(i)) == -1)
                {   // Check operand availability.
                    if (branchStation.Qj[i] != null)
                    {
                        if (branchStation.Qj[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            branchStation.Vj[i] = new Operand(Operand.OperandType.Num, branchStation.Qj[i].value);
                            ROB.j[branchStation.robNum[i]] = branchStation.Vj[i];
                            branchStation.Qj[i] = null;
                        }
                    }
                    if (branchStation.Qk[i] != null)
                    {
                        if (branchStation.Qk[i].waitState == WaitInfo.WaitState.Avail)
                        {
                            branchStation.Vk[i] = new Operand(Operand.OperandType.Num, branchStation.Qk[i].value);
                            ROB.k[branchStation.robNum[i]] = branchStation.Vk[i];
                            branchStation.Qk[i] = null;
                        }
                    }
                    branchStation.cyclesToComplete[i] = delay[5] - 1;
                }
                else if (result == 0)
                {
                    if (executeClocks[branchStation.instrNum[i]] == -1)
                    {
                        executeClocks[branchStation.instrNum[i]] = step - delay[5];
                    }
                }

                if (branchStation.robNum[i] != -1)
                {
                    if (ROB.state[branchStation.robNum[i]] == ReorderBuffer.State.Issue)
                    {
                        ROB.state[branchStation.robNum[i]] = ReorderBuffer.State.Execute;
                    }
                }

                if (!predictedBranchTaken[i] && (branchStation.dest[i] != null))
                {
                    predictedBranchTaken[i] = true;
                    // If branch predicted, move PC.
                    if (ROB.prediction[branchStation.robNum[i]] == true)
                    {
                        int amtToBranch = (int) branchStation.dest[i].opVal;
                        if (amtToBranch == 0)
                        {

                        }
                        else
                        {
                            // Clear Instruction Queue.
                            ROB.oldInstructions[branchStation.robNum[i]] = instructionUnit;
                            ROB.oldFRegs[branchStation.robNum[i]] = floatRegs;
                            ROB.oldIRegs[branchStation.robNum[i]] = intRegs;
                            ROB.oldMem[branchStation.robNum[i]] = memLocs;
                            instructionUnit = new InstructionUnit();

                            // Put new instructions in.
                            for (int j = branchStation.instrNum[i] - totalIssuedInstr + 1; j < originalInstructions.Length; j++)
                            {
                                instructionUnit.AddInstruction(originalInstructions[j]);
                            }
                            instrOffset = branchStation.instrNum[i];
                            ROB.instrOffsets[branchStation.robNum[i]] = instrOffset;
                        }
                    }
                }
            }
        }

        public bool state = true;//记录程序是运行true还是暂停false
        private void button1_Click(object sender, EventArgs e)//sender代表button1，e代表//reset
        {
            instructionUnit.ClearInstructions();
            foreach (Instruction inst in originalInstructions)//将指令初始化
            {
                instructionUnit.AddInstruction(inst);
            }
            Init();    //将其他状态初始化
        }

        //private static System.Timers.Timer CheckUpdatetimer = new System.Timers.Timer();
        //CheckUpdatetimer.Interval = 100;
        private void button2_Click(object sender, EventArgs e)//Auto
        {
            while (state)
            {
                Clock_Click(sender, e);//执行一个step
                Thread.Sleep(100);//延迟
                Application.DoEvents();//延迟记录按键行为
            }
        }

        private void button3_Click(object sender, EventArgs e)//pause
        {
            state = !state;
            if (!state)
            {
                //暂停
                button3.Text = "Continue";
            }
            else
            {
                //继续
                button3.Text = "Pause";
                button2_Click(sender, e);
            }
        }

        private void button4_Click(object sender, EventArgs e)//stop
        {
            state = !state;
        }

        private void editInstructions_Click(object sender, EventArgs e)
        {
            form2.parent = this;
            form2.insts = instructionUnit.GetCurrentInstructions();
            form2.Show();
            Hide();
        }

        private void Write()
        {
            // Add Results.
            for (int i = 0; i < addStation.numBuffers; i++)
            {
                
                if (addStation.isReady[i])
                {
                    if (ROB.state[addStation.robNum[i]] == ReorderBuffer.State.Execute)
                    {
                        ROB.state[addStation.robNum[i]] = ReorderBuffer.State.Write;
                    }

                    writeClocks[addStation.instrNum[i]] = step;
                    if (addStation.dest[i].opType == Operand.OperandType.FloatReg)
                    {
                        ROB.destType[addStation.robNum[i]] = ReorderBuffer.ROBDestination.FReg;
                        ROB.destLoc[addStation.robNum[i]] = (int) addStation.dest[i].opVal;
                        ROB.result[addStation.robNum[i]] = addStation.Compute(floatRegs, intRegs, i);
                        ROB.resultWritten[addStation.robNum[i]] = true;
                    }
                    else if (addStation.dest[i].opType == Operand.OperandType.IntReg)
                    {
                        ROB.destType[addStation.robNum[i]] = ReorderBuffer.ROBDestination.IReg;
                        ROB.destLoc[addStation.robNum[i]] = (int) addStation.dest[i].opVal;
                        ROB.result[addStation.robNum[i]] = addStation.Compute(floatRegs, intRegs, i);
                        ROB.resultWritten[addStation.robNum[i]] = true;
                    }
                    else
                    {
                        Console.WriteLine("Don't know what to do with result.");
                    }
                    addStation.Free(i);
                }
            }

            // Multiply Results.
            for (int i = 0; i < multiplyStation.numBuffers; i++)
            {
                if (multiplyStation.isReady[i])
                {
                    if (ROB.state[multiplyStation.robNum[i]] == ReorderBuffer.State.Execute)
                    {
                        ROB.state[multiplyStation.robNum[i]] = ReorderBuffer.State.Write;
                    }

                    writeClocks[multiplyStation.instrNum[i]] = step;
                    if (multiplyStation.dest[i].opType == Operand.OperandType.FloatReg)
                    {
                        ROB.destType[multiplyStation.robNum[i]] = ReorderBuffer.ROBDestination.FReg;
                        ROB.destLoc[multiplyStation.robNum[i]] = (int) multiplyStation.dest[i].opVal;
                        ROB.result[multiplyStation.robNum[i]] = multiplyStation.Compute(floatRegs, intRegs, i);
                        ROB.resultWritten[multiplyStation.robNum[i]] = true;
                    }
                    else if (multiplyStation.dest[i].opType == Operand.OperandType.IntReg)
                    {
                        ROB.destType[multiplyStation.robNum[i]] = ReorderBuffer.ROBDestination.IReg;
                        ROB.destLoc[multiplyStation.robNum[i]] = (int) multiplyStation.dest[i].opVal;
                        ROB.result[multiplyStation.robNum[i]] = multiplyStation.Compute(floatRegs, intRegs, i);
                        ROB.resultWritten[multiplyStation.robNum[i]] = true;
                    }
                    else
                    {
                        Console.WriteLine("Don't know what to do with result.");
                    }
                    multiplyStation.Free(i);
                }
            }

            // Load Results.
            for (int i = 0; i < loadStation.numBuffers; i++)
            {
                if (loadStation.isReady[i])
                {
                    if (ROB.state[loadStation.robNum[i]] == ReorderBuffer.State.Execute)
                    {
                        ROB.state[loadStation.robNum[i]] = ReorderBuffer.State.Write;
                    }

                    writeClocks[loadStation.instrNum[i]] = step;
                    if (loadStation.dest[i].opType == Operand.OperandType.FloatReg)
                    {
                        ROB.destType[loadStation.robNum[i]] = ReorderBuffer.ROBDestination.FReg;
                        ROB.destLoc[loadStation.robNum[i]] = (int) loadStation.dest[i].opVal;
                        ROB.result[loadStation.robNum[i]] = memLocs.Get(loadStation.addresses[i]);
                        ROB.resultWritten[loadStation.robNum[i]] = true;
                    }
                    else if (loadStation.dest[i].opType == Operand.OperandType.IntReg)
                    {
                        ROB.destType[loadStation.robNum[i]] = ReorderBuffer.ROBDestination.IReg;
                        ROB.destLoc[loadStation.robNum[i]] = (int) loadStation.dest[i].opVal;
                        ROB.result[loadStation.robNum[i]] = (int) memLocs.Get(loadStation.addresses[i]);
                        ROB.resultWritten[loadStation.robNum[i]] = true;
                    }
                    else
                    {
                        Console.WriteLine("Don't know what to do with result.");
                    }
                    loadStation.Free(i);
                }
            }

            // Store Results.
            for (int i = 0; i < loadStation.numBuffers; i++)
            {
                if (storeStation.isReady[i])
                {   // Ready.
                    if (ROB.state[storeStation.robNum[i]] == ReorderBuffer.State.Execute)
                    {
                        ROB.state[storeStation.robNum[i]] = ReorderBuffer.State.Write;
                    }
                    ROB.destType[storeStation.robNum[i]] = ReorderBuffer.ROBDestination.FReg;
                    ROB.destLoc[storeStation.robNum[i]] = (int) storeStation.dest[i].opVal;
                    ROB.result[storeStation.robNum[i]] = storeStation.results[i];
                    ROB.resultWritten[storeStation.robNum[i]] = true;
                    storeStation.Free(i);
                }
            }

            // Branch Results.
            for (int i = 0; i < branchStation.numBuffers; i++)
            {
                if (branchStation.isReady[i])
                {
                    if (ROB.state[branchStation.robNum[i]] == ReorderBuffer.State.Execute)
                    {
                        ROB.state[branchStation.robNum[i]] = ReorderBuffer.State.Write;
                    }

                    predictedBranchTaken[i] = false;
                    writeClocks[branchStation.instrNum[i]] = step;
                    ROB.branchAmt[branchStation.robNum[i]] = (int) branchStation.DetermineBranch(floatRegs, intRegs, i);
                    ROB.resultWritten[branchStation.robNum[i]] = true;
                    branchStation.Free(i);
                }
            }
        }

        private void Commit()
        {
            oldHead = -1;
            if (ROB.resultWritten[ROB.head])
            {
                oldHead = ROB.head;
                ROB.state[ROB.head] = ReorderBuffer.State.Commit;
                commitClocks[ROB.instrNum[ROB.head]] = step;
                if (ROB.instrType[ROB.head] == Instruction.InstructionType.Branch)
                {
                    if (ROB.branchAmt[ROB.head] == 0)
                    {
                        speculator.RecordBranchResult(false);
                        if (ROB.prediction[ROB.head] == true)
                        {   // Incorrect prediction. Undo branch.

                            // Restore Old Instruction Queue.
                            instructionUnit = ROB.oldInstructions[ROB.head];
                            floatRegs = ROB.oldFRegs[ROB.head];
                            intRegs = ROB.oldIRegs[ROB.head];
                            memLocs = ROB.oldMem[ROB.head];
                            
                            // Restore Instruction Offset.
                            instrOffset -= ROB.instrOffsets[ROB.head];

                            // Clean ROB.
                            ROB = new ReorderBuffer();
                            floatRegs.ClearROBRefs();
                            intRegs.ClearROBRefs();
                            return;
                        }
                    }
                    else
                    {
                        speculator.RecordBranchResult(true);
                        if (ROB.prediction[ROB.head] == false)
                        {   // Incorrect prediction. Do branch.

                            // Clear Instruction Queue.
                            instructionUnit = new InstructionUnit();

                            // Put new instructions in.
                            int j;
                            for (j = ROB.instrNum[ROB.head] + ROB.branchAmt[ROB.head] - instrOffset; j < originalInstructions.Length; j++)
                            {
                                instructionUnit.AddInstruction(originalInstructions[j]);
                            }
                            instrOffset += j;

                            // Clean ROB.
                            ROB = new ReorderBuffer();
                            floatRegs.ClearROBRefs();
                            intRegs.ClearROBRefs();
                            return;
                        }
                    }
                }
                else
                {
                    switch (ROB.destType[ROB.head])
                    {
                        case ReorderBuffer.ROBDestination.FReg:
                            floatRegs.Set(WaitInfo.WaitState.Avail, ROB.result[ROB.head], ROB.destLoc[ROB.head]);
                            break;

                        case ReorderBuffer.ROBDestination.IReg:
                            intRegs.Set(WaitInfo.WaitState.Avail, (int)ROB.result[ROB.head], ROB.destLoc[ROB.head]);
                            break;

                        case ReorderBuffer.ROBDestination.FMem:
                            memLocs.Set(ROB.result[ROB.head], ROB.destLoc[ROB.head]);
                            break;
                    }
                }

                ROB.CleanBuffer(ROB.head);
                ROB.IncrementHead();

            }
        }

        private void UpdateClockCountBox()
        {
            ClockCount.Text = step.ToString();
        }

        private void instructionQueue_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Clock_Click(object sender, EventArgs e)//step执行函数
        {
            step++;
            RunOneCycle();
            UpdateClockCountBox();
        }
    }
}