using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace MR_Cleaner.Forms
{
    public partial class FormCPU : MetroFramework.Forms.MetroForm
    {
        private Timer updateTimer;
        private PerformanceCounter totalCounter;
        private List<PerformanceCounter> coreCounters;
        private List<MetroFramework.Controls.MetroLabel> coreLabels;
        private int coreCount;

        public FormCPU()
        {
            InitializeComponent();
            this.ActiveControl = null;
            coreLabels = new List<MetroFramework.Controls.MetroLabel>();
            coreCounters = new List<PerformanceCounter>();
        }

        private void FormCPU_Load(object sender, EventArgs e)
        {
            try
            {
                totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                totalCounter.NextValue();

                coreCount = Environment.ProcessorCount;

                for (int i = 0; i < coreCount; i++)
                {
                    var pc = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                    pc.NextValue();
                    coreCounters.Add(pc);

                    var label = new MetroFramework.Controls.MetroLabel();
                    label.AutoSize = true;
                    label.Location = new Point(23, 100 + i * 30);
                    label.Text = $"Ядро {i}: 0%";
                    label.Theme = MetroFramework.MetroThemeStyle.Dark;
                    this.Controls.Add(label);
                    coreLabels.Add(label);
                }

                this.ClientSize = new Size(400, 100 + coreCount * 30 + 30);

                updateTimer = new Timer();
                updateTimer.Interval = 1000;
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Start();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(this, "Ошибка: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            this.ActiveControl = null;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                float total = totalCounter.NextValue();
                totalLabel.Text = $"Общая нагрузка: {total:F0}%";

                for (int i = 0; i < coreCount; i++)
                {
                    float coreValue = coreCounters[i].NextValue();
                    coreLabels[i].Text = $"Ядро {i}: {coreValue:F0}%";
                }
            }
            catch { }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            this.ActiveControl = null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Dispose();
            }
            if (totalCounter != null)
            {
                totalCounter.Dispose();
            }
            foreach (var pc in coreCounters)
            {
                pc.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}