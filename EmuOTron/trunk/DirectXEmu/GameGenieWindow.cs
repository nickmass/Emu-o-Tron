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
    public partial class GameGenieWindow : Form
    {
        int codeCount;
        public GameGenieWindow()
        {
            InitializeComponent();
        }
        public void AddCodes(GameGenie[] gameGenieCodes, int codeCount)
        {
            for (int i = 0; i < codeCount; i++)
            {
                this.codeCount++;
                lstCodes.Items.Add(gameGenieCodes[i].code);
            }
        }
        public GameGenie[] GetCodes()
        {
            GameGenie[] gameGeneieCodes = new GameGenie[this.codeCount];
            for (int i = 0; i < this.codeCount; i++)
            {
                gameGeneieCodes[i] = new GameGenie((string)lstCodes.Items[i]);
            }
            return gameGeneieCodes;
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (txtCode.Text.Length == 6 || txtCode.Text.Length == 8)
            {
                codeCount++;
                lstCodes.Items.Add(txtCode.Text.ToUpper());
                txtCode.Text = "";
            }
            else
            {
                MessageBox.Show("Game Genie codes are required to be 6 or 8 characters long.");
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstCodes.SelectedIndex != -1)
            {
                lstCodes.Items.RemoveAt(lstCodes.SelectedIndex);
                codeCount--;
            }
        }

        private void txtCode_TextChanged(object sender, EventArgs e)
        {
            txtCode.Text = txtCode.Text.ToUpper();
            for (int i = 0; i < txtCode.Text.Length; i++)
            {
                if(txtCode.Text[i] != 'A' &&
                    txtCode.Text[i] != 'P' &&
                    txtCode.Text[i] != 'Z' &&
                    txtCode.Text[i] != 'L' &&
                    txtCode.Text[i] != 'G' &&
                    txtCode.Text[i] != 'I' &&
                    txtCode.Text[i] != 'T' &&
                    txtCode.Text[i] != 'Y' &&
                    txtCode.Text[i] != 'E' &&
                    txtCode.Text[i] != 'O' &&
                    txtCode.Text[i] != 'X' &&
                    txtCode.Text[i] != 'U' &&
                    txtCode.Text[i] != 'K' &&
                    txtCode.Text[i] != 'S' &&
                    txtCode.Text[i] != 'V' && 
                    txtCode.Text[i] != 'N')
                {
                    txtCode.Text = txtCode.Text.Remove(i, 1);
                    i--;
                }
            }
            txtCode.SelectionStart = txtCode.Text.Length;
            txtCode.SelectionLength = 0;
        }
    }
}
