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
                if(gameGenieCodes[i].code != "DUMMY")
                    lstCodes.Items.Add(gameGenieCodes[i].code);
                else
                    lstCodes.Items.Add("(" + gameGenieCodes[i].address.ToString("X4") + " : " + gameGenieCodes[i].value.ToString("X2") + ")");
            }
        }
        public GameGenie[] GetCodes()
        {
            GameGenie[] gameGeneieCodes = new GameGenie[this.codeCount];
            for (int i = 0; i < this.codeCount; i++)
            {
                if (((string)lstCodes.Items[i]).IndexOf("(") == 0)
                {
                    byte value = byte.Parse(((string)lstCodes.Items[i]).Substring(8,2), System.Globalization.NumberStyles.HexNumber, null);
                    ushort address = ushort.Parse(((string)lstCodes.Items[i]).Substring(1,4), System.Globalization.NumberStyles.HexNumber, null);
                    gameGeneieCodes[i] = new GameGenie(address, value);
                }
                else
                {
                    gameGeneieCodes[i] = new GameGenie((string)lstCodes.Items[i]);
                }
            }
            return gameGeneieCodes;
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            int value = 0;
            if (txtValue.Text != "")
                value = int.Parse(txtValue.Text, System.Globalization.NumberStyles.HexNumber, null);
            int address = 0;
            if (txtAddress.Text != "")
                address = int.Parse(txtAddress.Text, System.Globalization.NumberStyles.HexNumber, null);
            if (txtCode.Text.Length == 6 || txtCode.Text.Length == 8)
            {
                codeCount++;
                lstCodes.Items.Add(txtCode.Text.ToUpper());
                boxChange = true;
                txtCode.Text = "";
                txtAddress.Text = "";
                txtValue.Text = "";
                boxChange = false;
            }
            else if (!(address < 0x0000 || address > 0xFFFF || value < 0x00 || value > 0xFF))
            {
                codeCount++;
                lstCodes.Items.Add("(" + address.ToString("X4") + " : " + value.ToString("X2") + ")");
                boxChange = true;
                txtCode.Text = "";
                txtAddress.Text = "";
                txtValue.Text = "";
                boxChange = false;
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
            if (boxChange)
                return;
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
            boxChange = true;
            if (txtCode.Text.Length == 6 || txtCode.Text.Length == 8)
            {
                txtAddress.Text = (GameGenie.ReverseCode(txtCode.Text) & 0xFFFF).ToString("X4");
                txtValue.Text = ((GameGenie.ReverseCode(txtCode.Text) >> 16) & 0xFF).ToString("X2");
            }
            else
            {
                txtAddress.Text = "";
                txtValue.Text = "";
            }

            boxChange = false;

        }
        bool boxChange = false;
        private void txtValue_TextChanged(object sender, EventArgs e)
        {
            if (boxChange)
                return;
            txtValue.Text = txtValue.Text.ToUpper();
            for (int i = 0; i < txtValue.Text.Length; i++)
            {
                if (txtValue.Text[i] != '0' &&
                    txtValue.Text[i] != '1' &&
                    txtValue.Text[i] != '2' &&
                    txtValue.Text[i] != '3' &&
                    txtValue.Text[i] != '4' &&
                    txtValue.Text[i] != '5' &&
                    txtValue.Text[i] != '6' &&
                    txtValue.Text[i] != '7' &&
                    txtValue.Text[i] != '8' &&
                    txtValue.Text[i] != '9' &&
                    txtValue.Text[i] != 'A' &&
                    txtValue.Text[i] != 'B' &&
                    txtValue.Text[i] != 'C' &&
                    txtValue.Text[i] != 'D' &&
                    txtValue.Text[i] != 'E' &&
                    txtValue.Text[i] != 'F')
                {
                    txtValue.Text = txtValue.Text.Remove(i, 1);
                    i--;
                }
            }
            txtValue.SelectionStart = txtValue.Text.Length;
            txtValue.SelectionLength = 0;
            txtAddress.Text = txtAddress.Text.ToUpper();
            for (int i = 0; i < txtAddress.Text.Length; i++)
            {
                if (txtAddress.Text[i] != '0' &&
                    txtAddress.Text[i] != '1' &&
                    txtAddress.Text[i] != '2' &&
                    txtAddress.Text[i] != '3' &&
                    txtAddress.Text[i] != '4' &&
                    txtAddress.Text[i] != '5' &&
                    txtAddress.Text[i] != '6' &&
                    txtAddress.Text[i] != '7' &&
                    txtAddress.Text[i] != '8' &&
                    txtAddress.Text[i] != '9' &&
                    txtAddress.Text[i] != 'A' &&
                    txtAddress.Text[i] != 'B' &&
                    txtAddress.Text[i] != 'C' &&
                    txtAddress.Text[i] != 'D' &&
                    txtAddress.Text[i] != 'E' &&
                    txtAddress.Text[i] != 'F')
                {
                    txtAddress.Text = txtAddress.Text.Remove(i, 1);
                    i--;
                }
            }
            txtAddress.SelectionStart = txtAddress.Text.Length;
            txtAddress.SelectionLength = 0;
            int value = 0;
            if(txtValue.Text != "")
                value = int.Parse(txtValue.Text, System.Globalization.NumberStyles.HexNumber, null);
            int address = 0;
            if(txtAddress.Text != "")
                address = int.Parse(txtAddress.Text, System.Globalization.NumberStyles.HexNumber, null);
            boxChange = true;
            if (address < 0x8000 || address > 0xFFFF || value < 0x00 || value > 0xFF)
                txtCode.Text = "";
            else
                txtCode.Text = GameGenie.CreateCode(address, value);
            boxChange = false;
        }
    }
}
