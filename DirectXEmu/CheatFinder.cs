using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DirectXEmu
{
    public partial class CheatFinder : Form
    {
        EmuoTron.Debug debug;
        List<ushort> results = new List<ushort>();
        //List<ushort, byte> unkownResults = new List<ushort, byte>();
        Dictionary<ushort, byte> unkownResults = new Dictionary<ushort, byte>();
        public CheatFinder(EmuoTron.Debug debug)
        {
            this.debug = debug;
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                int value = int.Parse(txtCompare.Text, System.Globalization.NumberStyles.HexNumber) & 0xFF;
                results.Clear();
                switch (cboOp.SelectedIndex)
                {
                    case -1:
                        return;
                    case 0:
                        for (ushort i = 0; i < 0xFFFF; i++)
                            if (debug.Peek(i) == value && debug.PeekMirror(i) == i)
                                results.Add(i);
                        if (debug.Peek(0xFFFF) == value && debug.PeekMirror(0xFFFF) == 0xFFFF)
                            results.Add(0xFFFF);
                        break;
                    case 1:
                        for (ushort i = 0; i < 0xFFFF; i++)
                            if (debug.Peek(i) != value && debug.PeekMirror(i) == i)
                                results.Add(i);
                        if (debug.Peek(0xFFFF) != value && debug.PeekMirror(0xFFFF) == 0xFFFF)
                            results.Add(0xFFFF);
                        break;
                    case 2:
                        for (ushort i = 0; i < 0xFFFF; i++)
                            if (debug.Peek(i) > value && debug.PeekMirror(i) == i)
                                results.Add(i);
                        if (debug.Peek(0xFFFF) > value && debug.PeekMirror(0xFFFF) == 0xFFFF)
                            results.Add(0xFFFF);
                        break;
                    case 3:
                        for (ushort i = 0; i < 0xFFFF; i++)
                            if (debug.Peek(i) < value && debug.PeekMirror(i) == i)
                                results.Add(i);
                        if (debug.Peek(0xFFFF) < value && debug.PeekMirror(0xFFFF) == 0xFFFF)
                            results.Add(0xFFFF);
                        break;
                }
                lstResults.Items.Clear();
                if (results.Count > 500)
                    lstResults.Items.Add(results.Count.ToString() + " results");
                else
                    foreach (ushort item in results)
                        lstResults.Items.Add(item.ToString("X4") + " : " + debug.Peek(item).ToString("X2"));
            }
            catch (FormatException formatEx)
            {
                MessageBox.Show("Comparison value must be a hex integer from 0x00 to 0xFF.");
            }
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                int value = int.Parse(txtCompare.Text, System.Globalization.NumberStyles.HexNumber) & 0xFF;
                List<ushort> results = new List<ushort>();
                switch (cboOp.SelectedIndex)
                {
                    case -1:
                        return;
                    case 0:
                        foreach (ushort i in this.results)
                            if (debug.Peek(i) == value)
                                results.Add(i);
                        break;
                    case 1:
                        foreach (ushort i in this.results)
                            if (debug.Peek(i) != value)
                                results.Add(i);
                        break;
                    case 2:
                        foreach (ushort i in this.results)
                            if (debug.Peek(i) > value)
                                results.Add(i);
                        break;
                    case 3:
                        foreach (ushort i in this.results)
                            if (debug.Peek(i) < value)
                                results.Add(i);
                        break;
                }
                this.results = results;
                lstResults.Items.Clear();
                if (results.Count > 500)
                    lstResults.Items.Add(results.Count.ToString() + " results");
                else
                    foreach (ushort item in results)
                        lstResults.Items.Add(item.ToString("X4") + " : " + debug.Peek(item).ToString("X2"));
            }
            catch (FormatException formatEx)
            {
                MessageBox.Show("Comparison value must be a hex integer from 0x00 to 0xFF.");
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnUnkSearch_Click(object sender, EventArgs e)
        {
            unkownResults.Clear();
            for (ushort i = 0; i < 0xFFFF; i++)
                if (debug.PeekMirror(i) == i)
                    unkownResults.Add(i, debug.Peek(i));
            if (debug.PeekMirror(0xFFFF) == 0xFFFF)
                unkownResults.Add(0xFFFF, debug.Peek(0xFFFF));
            lstResults.Items.Clear();
            if (unkownResults.Count > 500)
                lstResults.Items.Add(unkownResults.Count.ToString() + " results");
            else
                foreach (KeyValuePair<ushort, byte> item in this.unkownResults)
                    lstResults.Items.Add(item.Key.ToString("X4") + " : " + item.Value.ToString("X2"));
        }

        private void btnUnkFilter_Click(object sender, EventArgs e)
        {
            Dictionary<ushort, byte> unkownResults = new Dictionary<ushort, byte>();
            switch (cboUnknown.SelectedIndex)
            {
                case -1:
                    return;
                case 0:
                    foreach (KeyValuePair<ushort, byte> i in this.unkownResults)
                        if (i.Value != debug.Peek(i.Key))
                            unkownResults.Add(i.Key, debug.Peek(i.Key));
                    break;
                case 1:
                    foreach (KeyValuePair<ushort, byte> i in this.unkownResults)
                        if (i.Value == debug.Peek(i.Key))
                            unkownResults.Add(i.Key, debug.Peek(i.Key));
                    break;
                case 2:
                    foreach (KeyValuePair<ushort, byte> i in this.unkownResults)
                        if (i.Value < debug.Peek(i.Key))
                            unkownResults.Add(i.Key, debug.Peek(i.Key));
                    break;
                case 3:
                    foreach (KeyValuePair<ushort, byte> i in this.unkownResults)
                        if (i.Value > debug.Peek(i.Key))
                            unkownResults.Add(i.Key, debug.Peek(i.Key));
                    break;
            }
            this.unkownResults = unkownResults;
            lstResults.Items.Clear();
            if (unkownResults.Count > 500)
                lstResults.Items.Add(unkownResults.Count.ToString() + " results");
            else
                foreach (KeyValuePair<ushort, byte> item in this.unkownResults)
                    lstResults.Items.Add(item.Key.ToString("X4") + " : " + item.Value.ToString("X2"));

        }

    }
}
