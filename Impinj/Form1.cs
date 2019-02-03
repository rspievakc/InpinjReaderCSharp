using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Impinj.OctaneSdk;

/*
 * OctaneSDK Workbook (Examples) -> www.go-gddq.com/down/2012-06/12060311273105.pdf * 
 *
 */

namespace Impinj
{
    public partial class Form1 : Form
    {

        // Maps the connected Impinj Readers addresses to to their instances
        static Dictionary<string, ImpinjReader> readers = new Dictionary<string, ImpinjReader>();

        public Form1()
        {
            InitializeComponent();
        }

        public bool ValidateIPv4(string ipString)
        {
            if (String.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        private void label1_Click(object sender, EventArgs e)
        {
        }

        private void address_TextChanged(object sender, EventArgs e)
        {
        }

        private void address_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;

                Button button = this.Controls.Find("connectButton", true)[0] as Button;
                button.PerformClick();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            // The sender can be the textbox (address) or the button (connect).
            
            TextBox addressText = this.Controls.Find("addressText", true)[0] as TextBox;
            string address = addressText.Text; 
            
            if (!ValidateIPv4(address))
            {
                string message = "Endereço IPV4 inválido.";
                string caption = "Erro";
                MessageBoxButtons buttons = MessageBoxButtons.OK;
                DialogResult result;

                // Displays the MessageBox.
                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    // Closes the parent form.
                    // this.Close();
                    addressText.Focus();
                    return;
                }
            }

            try
            {

                ImpinjReader reader = new ImpinjReader();

                reader.ConnectionLost += OnConnectionLost;

                reader.ConnectionLost += new ImpinjReader.ConnectionLostHandler((ImpinjReader reader2) => {
                    Console.Write("Connection lost");
                });
                
                if (readers.ContainsKey(address))
                {
                    string message = "O leitor já está conectado.";
                    string caption = "Erro";
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    DialogResult result;

                    // Displays the MessageBox.
                    result = MessageBox.Show(message, caption, buttons);

                    if (result == System.Windows.Forms.DialogResult.OK)
                    {
                        // Closes the parent form.
                        // this.Close();
                        addressText.Focus();
                        return;
                    }
                }

                reader.Connect(address);
                readers.Add(address, reader);

                Settings settings = reader.QueryDefaultSettings();
                settings.ReaderMode = ReaderMode.AutoSetDenseReader;

                ReportConfig report = settings.Report;
                report.IncludeAntennaPortNumber = true;
                report.IncludeFirstSeenTime = true;
                report.IncludeLastSeenTime = true;
                report.IncludeSeenCount = true;
                report.Mode = ReportMode.Individual;

                AntennaConfigGroup antennas = settings.Antennas;
                antennas.DisableAll();

                antennas.EnableById(new ushort[] { 1 });
                AntennaConfig antennaConfig = antennas.GetAntenna(1);
                antennaConfig.MaxRxSensitivity = true;
                antennaConfig.RxSensitivityInDbm = -70;
                antennaConfig.MaxTxPower = true;
                antennaConfig.TxPowerInDbm = 20.0;

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                reader.TagsReported += OnTagsReported;

                // Apply the custom settings.
                reader.ApplySettings(settings);
                // Apply the default settings.
                reader.ApplyDefaultSettings();

                // Start reading.
                reader.Start();
            }
            catch (OctaneSdkException ex)
            {
                Console.Write(ex.ToString());
                return;
            }

            Control[] controls = this.Controls.Find("LogTextArea", true);
            RichTextBox logs = controls[0] as RichTextBox;
            logs.Invoke(new Action(() => {
                addressText.Clear();
                logs.AppendText("Leitor: " + address + " foi conectado com sucesso.\n");
            }));
 
        }

        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            // This event handler is called asynchronously 
            // when tag reports are available.
            // Loop through each tag in the report 
            // and print the data.
            Control[] controls = this.Controls.Find("LogTextArea", true);
            RichTextBox logs = controls[0] as RichTextBox;
            foreach (Tag tag in report)
            {
                logs.Invoke(new Action(() => {
                    logs.AppendText("EPC : " + tag.Epc.ToString() + " - " + sender.Address + ":" + tag.AntennaPortNumber + "\n");
                    logs.SelectionStart = logs.Text.Length;
                    logs.ScrollToCaret();
                }));
                
                Console.WriteLine("EPC : {0} ", tag.Epc);
            }
        }

        void OnConnectionLost(ImpinjReader sender)
        {
            Console.Write("Connetion Lost.");
        }
    }
}
