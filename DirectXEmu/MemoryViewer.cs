﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EmuoTron;

namespace DirectXEmu
{
    public partial class MemoryViewer : Form
    {
        MemoryStore memory;
        bool updated = false;
        int max = 0;
        public MemoryViewer()
        {
            InitializeComponent();

        }
        public void updateMemory(MemoryStore memory, int max)
        {
            this.memory = memory;
            this.updated = true;
            this.max = max;
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            if (updated)
            {
                byte[] data = new byte[max];
                for (int i = 0; i < max; i++)
                {
                    data[i] = memory[i];
                }
                memPane.Data = data;
                memPane.DrawMemory();
                updated = false;
            }
            else
            {
                byte[] data = new byte[max];
                for (int i = 0; i < max; i++)
                {
                    data[i] = memory[i];
                }
                memPane.Data = data;
            }
        }
    }
}
