using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MR_Cleaner.Forms
{
    public partial class FormGPU : MetroFramework.Forms.MetroForm
    {
        private sealed class GpuEngineItem
        {
            public string InstanceName;
            public string DisplayName;
            public PerformanceCounter Counter;
            public MetroFramework.Controls.MetroLabel Label;
        }

        private Timer updateTimer;
        private List<GpuEngineItem> gpuEngines;

        public FormGPU()
        {
            InitializeComponent();
            this.ActiveControl = null;
            gpuEngines = new List<GpuEngineItem>();
        }

        private void FormGPU_Load(object sender, EventArgs e)
        {
            gpuLabel.ForeColor = Color.White;
            gpuLabel.UseCustomForeColor = true;

            try
            {
                gpuEngines.Clear();
                enginesPanel.Controls.Clear();

                PerformanceCounterCategory category;
                try
                {
                    category = new PerformanceCounterCategory("GPU Engine");
                }
                catch
                {
                    gpuLabel.Text = "Общая нагрузка GPU: недоступно";
                    return;
                }

                string[] instances;
                try
                {
                    instances = category.GetInstanceNames();
                }
                catch
                {
                    gpuLabel.Text = "Общая нагрузка GPU: недоступно";
                    return;
                }

                var filtered = instances
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Where(x => x.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(x => x)
                    .ToList();

                if (filtered.Count == 0)
                {
                    gpuLabel.Text = "Общая нагрузка GPU: недоступно";
                    return;
                }

                int y = 10;
                int index = 0;

                foreach (string instance in filtered)
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        counter.NextValue();

                        var label = new MetroFramework.Controls.MetroLabel();
                        label.ForeColor = Color.White;
                        label.UseCustomForeColor = true;
                        label.AutoSize = false;
                        label.Width = enginesPanel.ClientSize.Width - 25;
                        label.Height = 24;
                        label.Location = new Point(10, y);
                        label.Theme = MetroFramework.MetroThemeStyle.Dark;
                        label.Style = MetroFramework.MetroColorStyle.Red;
                        label.Text = $"{BuildDisplayName(instance, index)}: 0%";
                        label.UseStyleColors = true;

                        enginesPanel.Controls.Add(label);

                        gpuEngines.Add(new GpuEngineItem
                        {
                            InstanceName = instance,
                            DisplayName = BuildDisplayName(instance, index),
                            Counter = counter,
                            Label = label
                        });

                        y += 26;
                        index++;
                    }
                    catch
                    {
                    }
                }

                updateTimer = new Timer();
                updateTimer.Interval = 1000;
                updateTimer.Tick += UpdateTimer_Tick;
                updateTimer.Start();
            }
            catch (Exception ex)
            {
                MetroFramework.MetroMessageBox.Show(
                    this,
                    "Ошибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            this.ActiveControl = null;
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (gpuEngines == null || gpuEngines.Count == 0)
                {
                    gpuLabel.Text = "Общая нагрузка GPU: недоступно";
                    return;
                }

                float total = 0f;

                for (int i = 0; i < gpuEngines.Count; i++)
                {
                    float value = 0f;

                    try
                    {
                        value = gpuEngines[i].Counter.NextValue();
                    }
                    catch
                    {
                    }

                    if (value < 0f)
                        value = 0f;

                    if (value > 100f)
                        value = 100f;

                    gpuEngines[i].Label.Text = $"{gpuEngines[i].DisplayName}: {value:F0}%";
                    total += value;
                }

                if (total > 100f)
                    total = 100f;

                gpuLabel.Text = $"Общая нагрузка GPU: {total:F0}%";
            }
            catch
            {
            }
        }

        private static string BuildDisplayName(string instanceName, int index)
        {
            try
            {
                int pos = instanceName.IndexOf("engtype_", StringComparison.OrdinalIgnoreCase);
                if (pos >= 0)
                {
                    string type = instanceName.Substring(pos + "engtype_".Length);

                    int underscore = type.IndexOf('_');
                    if (underscore > 0)
                        type = type.Substring(0, underscore);

                    return $"ГП движок {index} ({type})";
                }
            }
            catch
            {
            }

            return $"ГП движок {index}";
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
                updateTimer.Tick -= UpdateTimer_Tick;
                updateTimer.Dispose();
                updateTimer = null;
            }

            if (gpuEngines != null)
            {
                foreach (var item in gpuEngines)
                {
                    try
                    {
                        if (item.Counter != null)
                            item.Counter.Dispose();
                    }
                    catch
                    {
                    }
                }

                gpuEngines.Clear();
            }

            base.OnFormClosing(e);
        }
    }
}