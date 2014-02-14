using System;
using System.Windows.Forms;

using Ardex.TestClient.Tests.ChangeHistoryBased;
using Ardex.TestClient.Tests.TimestampBased;

namespace Ardex.TestClient
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.TestAsync();
        }

        private async void TestAsync()
        {
            this.button1.Enabled = false;

            try
            {
                await new ChangeHistoryTest().RunAsync();
            }
            finally
            {
                this.button1.Enabled = true;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;

            try
            {
                await new TimestampTest().RunAsync();
            }
            finally
            {
                this.button2.Enabled = true;
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }
    }
}