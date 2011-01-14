using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Threading;
using EmuoTron;

namespace DirectXEmu
{
    public struct TestSuite
    {
        public string name;
        public string basepath;
        public List<Test> tests;
    }
    public struct Test
    {
        public string file;
        public TestType type;
        public string name;
        public string movie;
        public int frame;
        public List<ComparisonGroup> comparisonGroups;
    }
    public struct ComparisonGroup
    {
        public string name;
        public string fail;
        public string pass;
        public List<Comparison> comparisons;
    }
    public struct Comparison
    {
        public CompareType type;
        public int address;
        public uint crc;
        public int value;
        public ComparisonOperator op;
    }
    public enum ComparisonOperator
    {
        equal,
        notequal,
        greaterthan,
        lessthan
    }
    public enum TestType
    {
        movie,
        noinput
    }
    public enum CompareType
    {
        memory,
        screenshot
    }
    public partial class Console : Form
    {
        StringBuilder output;
        StringBuilder outputClean;
        TestSuite testSuite;
        string openFirst = @"C:\Users\NickMass\Documents\Visual Studio 2010\Projects\DirectXEmu\DirectXEmu\Test\test.xml";
        bool running;
        bool close = false;
        public Console()
        {
            output = new StringBuilder();
            outputClean = new StringBuilder();
            InitializeComponent();
            if (openFirst != null)
            {
                OpenXml(File.Open(openFirst, FileMode.Open), openFirst);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            close = true;
            Close();
        }
        private void OpenXml(Stream fs, string path)
        {
            XmlTextReader xmlReader = new XmlTextReader(fs);
            TestSuite testSuite = new TestSuite();
            testSuite.tests = new List<Test>();
            testSuite.basepath = Path.GetDirectoryName(path);
            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "testsuite")
                    while (xmlReader.MoveToNextAttribute())
                        if (xmlReader.Name == "name")
                            testSuite.name = xmlReader.Value;
                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "test")
                {
                    Test test = new Test();
                    test.comparisonGroups = new List<ComparisonGroup>();
                    while (xmlReader.MoveToNextAttribute())
                    {
                        if (xmlReader.Name == "rom")
                            test.file = xmlReader.Value;
                        if (xmlReader.Name == "name")
                            test.name = xmlReader.Value;
                    }
                    while (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "test") && xmlReader.Read())
                    {
                        if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "movie")
                        {
                            test.type = TestType.movie;
                            while (xmlReader.MoveToNextAttribute())
                                if (xmlReader.Name == "file")
                                    test.movie = xmlReader.Value;
                        }
                        if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "noinput")
                        {
                            test.type = TestType.noinput;
                            while (xmlReader.MoveToNextAttribute())
                                if (xmlReader.Name == "frame")
                                    test.frame = IntParse(xmlReader.Value);
                        }
                        if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "compare")
                        {
                            ComparisonGroup comparisons = new ComparisonGroup();
                            comparisons.comparisons = new List<Comparison>();
                            while (xmlReader.MoveToNextAttribute())
                            {
                                if (xmlReader.Name == "fail")
                                    comparisons.fail = xmlReader.Value;
                                if (xmlReader.Name == "success")
                                    comparisons.pass = xmlReader.Value;
                                if (xmlReader.Name == "name")
                                    comparisons.name = xmlReader.Value;
                            }
                            while (!(xmlReader.NodeType == XmlNodeType.EndElement && xmlReader.Name == "compare") && xmlReader.Read())
                            {
                                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "screenshot")
                                {
                                    Comparison compare = new Comparison();
                                    compare.type = CompareType.screenshot;
                                    while (xmlReader.MoveToNextAttribute())
                                    {
                                        if (xmlReader.Name == "crc")
                                            compare.crc = (uint)IntParse(xmlReader.Value);
                                        if (xmlReader.Name == "comparison")
                                        {
                                            if (xmlReader.Value == "equal")
                                                compare.op = ComparisonOperator.equal;
                                            if (xmlReader.Value == "notequal")
                                                compare.op = ComparisonOperator.notequal;
                                            if (xmlReader.Value == "lessthan")
                                                compare.op = ComparisonOperator.lessthan;
                                            if (xmlReader.Value == "greaterthan")
                                                compare.op = ComparisonOperator.greaterthan;
                                        }
                                    }
                                    comparisons.comparisons.Add(compare); //God this looks retarded
                                }
                                if (xmlReader.NodeType == XmlNodeType.Element && xmlReader.Name == "memory")
                                {
                                    Comparison compare = new Comparison();
                                    compare.type = CompareType.memory;
                                    while (xmlReader.MoveToNextAttribute())
                                    {
                                        if (xmlReader.Name == "address")
                                            compare.address = IntParse(xmlReader.Value);
                                        if (xmlReader.Name == "value")
                                            compare.value = IntParse(xmlReader.Value);
                                        if (xmlReader.Name == "comparison")
                                        {
                                            if (xmlReader.Value == "equal")
                                                compare.op = ComparisonOperator.equal;
                                            if (xmlReader.Value == "notequal")
                                                compare.op = ComparisonOperator.notequal;
                                            if (xmlReader.Value == "lessthan")
                                                compare.op = ComparisonOperator.lessthan;
                                            if (xmlReader.Value == "greaterthan")
                                                compare.op = ComparisonOperator.greaterthan;
                                        }
                                    }
                                    comparisons.comparisons.Add(compare); //God this looks retarded
                                }
                            }
                            test.comparisonGroups.Add(comparisons);
                        }
                    }
                    testSuite.tests.Add(test);
                }
            }
            xmlReader.Close();
            running = true;
            this.testSuite = testSuite;
            Thread testThread = new Thread(new ThreadStart(RunTest));
            testThread.Start();
        }
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenXml(openFileDialog.OpenFile(), openFileDialog.FileName);
            }
        }
        NESCore testCore;
        public void RunTest()
        {
            output.Append(@"{\rtf1\ansi {\colortbl ;\red255\green255\blue255;\red0\green200\blue0;\red200\green0\blue0;}");
            output.Append(@"\cf1Running " + testSuite.name + @":\par");
            foreach (Test test in testSuite.tests)
            {
                output.Append(@"\cf1" + test.name + ": ");
                testCore = new NESCore(SystemType.NTSC, Path.Combine(testSuite.basepath, test.file), "", 44100, 1);
                testCore.SetControllers(ControllerType.Controller, ControllerType.Controller, false);
                int frame = 0;
                if (test.type == TestType.movie)
                {
                    StreamReader fm2 = File.OpenText(Path.Combine(testSuite.basepath, test.movie));
                    Controller player1 = new Controller();
                    Controller player2 = new Controller();
                    bool movieEnd = Fm2Reader(fm2, ref player1, ref player2);
                    while (!close && !movieEnd)
                    {
                        testCore.Start(player1, player2, false);
                        testCore.APU.ResetBuffer();
                        movieEnd = Fm2Reader(fm2, ref player1, ref player2);
                    }

                }
                else if(test.type == TestType.noinput)
                {
                    while (frame++ != test.frame && !close)
                    {
                        testCore.Start(new Controller(), new Controller(), false);
                        testCore.APU.ResetBuffer();
                    }
                }
                foreach (ComparisonGroup compares in test.comparisonGroups)
                {
                    bool pass = true;
                    uint result = 0;
                    //output.Append(compares.name + ": ");
                    foreach (Comparison compare in compares.comparisons)
                    {
                        if (compare.type == CompareType.memory)
                        {
                            switch (compare.op)
                            {
                                default:
                                case ComparisonOperator.equal:
                                    if (testCore.Memory[compare.address] != compare.value)
                                        pass = false;
                                    result = testCore.Memory[compare.address];
                                    break;
                                case ComparisonOperator.notequal:
                                    if (testCore.Memory[compare.address] == compare.value)
                                        pass = false;
                                    result = testCore.Memory[compare.address];
                                    break;
                                case ComparisonOperator.lessthan:
                                    if (testCore.Memory[compare.address] >= compare.value)
                                        pass = false;
                                    result = testCore.Memory[compare.address];
                                    break;
                                case ComparisonOperator.greaterthan:
                                    if (testCore.Memory[compare.address] <= compare.value)
                                        pass = false;
                                    result = testCore.Memory[compare.address];
                                    break;
                            }
                        }
                        else if (compare.type == CompareType.screenshot)
                        {
                            uint CRC = GetScreenCRC(testCore.PPU.screen);
                            switch (compare.op)
                            {
                                default:
                                case ComparisonOperator.equal:
                                    if (compare.crc != CRC)
                                        pass = false;
                                    result = CRC;
                                    break;
                                case ComparisonOperator.notequal:
                                    if (compare.crc == CRC)
                                        pass = false;
                                    result = CRC;
                                    break;
                                case ComparisonOperator.lessthan:
                                    if (compare.crc >= CRC)
                                        pass = false;
                                    result = CRC;
                                    break;
                                case ComparisonOperator.greaterthan:
                                    if (compare.crc <= CRC)
                                        pass = false;
                                    result = CRC;
                                    break;
                            }
                        }
                    }
                    if (pass)
                        output.Append(@"\cf2" + compares.pass + @"\par");
                    else
                        output.Append(@"\cf3" + compares.fail + @"\par");
                }
            }
            output.Append(@"\cf1Tests Complete\par");
            running = false;
            finalDraw = true;
        }
        public int IntParse(string str)
        {
            if (str.IndexOf("0x") == 0)
                return int.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber, null);
            else
                return int.Parse(str);
        }
        public uint GetScreenCRC(int[,] scanlines)
        {
            uint crc = 0xFFFFFFFF;
            for (int y = 0; y < 240; y++)
                for (int x = 0; x < 256; x++)
                    crc = CRC32.crc32_adjust(crc, (byte)(scanlines[x,y] & 0x3F));
            crc ^= 0xFFFFFFFF;
            return crc;
        }
        bool finalDraw;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (running || finalDraw)
            {
                txtOutput.Rtf = output.ToString() + "}";
                txtOutput.SelectionStart = txtOutput.TextLength;
                txtOutput.ScrollToCaret();
                finalDraw = false;
            }
        }

        private bool Fm2Reader(StreamReader fm2File, ref Controller player1, ref Controller player2)
        {
            String line = " ";
            while (line[0] != '|')
            {
                line = fm2File.ReadLine();
                if(fm2File.EndOfStream)
                    return fm2File.EndOfStream;
            }
            player1.right = line[3] != '.';
            player1.left = line[4] != '.';
            player1.down = line[5] != '.';
            player1.up = line[6] != '.';
            player1.start = line[7] != '.';
            player1.select = line[8] != '.';
            player1.b = line[9] != '.';
            player1.a = line[10] != '.';
            player2.right = line[12] != '.';
            player2.left = line[13] != '.';
            player2.down = line[14] != '.';
            player2.up = line[15] != '.';
            player2.start = line[16] != '.';
            player2.select = line[17] != '.';
            player2.b = line[18] != '.';
            player2.a = line[19] != '.';
            return fm2File.EndOfStream;

        }
        private void Console_FormClosing(object sender, FormClosingEventArgs e)
        {
            close = true;
        }
    }
}
