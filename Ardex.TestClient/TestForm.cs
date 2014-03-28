using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

using Ardex.TestClient.Tests.ChangeHistoryBased;
using Ardex.TestClient.Tests.Filtered;
//using Ardex.TestClient.Tests.TimestampBased;

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
                using (var test = new ChangeHistoryTest())
                {
                    var sw = Stopwatch.StartNew();

                    await test.RunAsync();

                    sw.Stop();

                    MessageBox.Show(string.Format("Done. Seconds elapsed: {0:0.#}.", sw.Elapsed.TotalSeconds));

                    MessageBox.Show(string.Format(
                        "Sync complete. Repo 1 and 2 equal = {0}, Repo 2 and 3 equal = {1}. Server count = {2}.",
                        test.Server.Repository
                            .OrderBy(p => p.EntityGuid)
                            .SequenceEqual(test.Client1.Repository.OrderBy(p => p.EntityGuid), test.EntityMapping.EqualityComparer),
                        test.Server.Repository
                            .OrderBy(p => p.EntityGuid)
                            .SequenceEqual(test.Client2.Repository.OrderBy(p => p.EntityGuid), test.EntityMapping.EqualityComparer),
                        test.Server.Repository.Count)
                    );
                }
            }
            finally
            {
                this.button1.Enabled = true;
            }
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //this.button2.Enabled = false;

            //try
            //{
            //    await new TimestampTest().RunAsync();
            //}
            //finally
            //{
            //    this.button2.Enabled = true;
            //}
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            //this.button4.Enabled = false;

            //try
            //{
            //    using (var test = new FilteredTest())
            //    {
            //        await test.RunAsync();
            //    }
            //}
            //finally
            //{
            //    this.button4.Enabled = true;
            //}
        }
    }
}