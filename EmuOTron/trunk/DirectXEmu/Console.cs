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
        public int crc;
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
                                    test.frame = Convert.ToInt32(xmlReader.Value);
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
                                            compare.crc = Convert.ToInt32(xmlReader.Value);
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
                testCore = new NESCore(Path.Combine(testSuite.basepath, test.file), "");
                int frame = 0;
                while (frame++ != test.frame && !close)
                {
                    testCore.Start(new Controller(), new Controller(), new Zapper(), new Zapper(), false);
                    testCore.APU.ResetBuffer();
                }
                foreach (ComparisonGroup compares in test.comparisonGroups)
                {
                    bool pass = true;
                    int result = 0;
                    //output.Append(compares.name + ": ");
                    foreach (Comparison compare in compares.comparisons)
                    {
                        if (compare.type == CompareType.memory)
                        {
                            switch (compare.op)
                            {
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
        }
        public int IntParse(string str)
        {
            if (str.IndexOf("0x") == 0)
                return int.Parse(str.Substring(2), System.Globalization.NumberStyles.HexNumber, null);
            else
                return int.Parse(str);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            txtOutput.Rtf = output.ToString() + "}";

        }

        private void Console_FormClosing(object sender, FormClosingEventArgs e)
        {
            close = true;
        }
    }
}
