namespace MR_Cleaner.Forms
{
    partial class FormGPU
    {
        private System.ComponentModel.IContainer components = null;
        private MetroFramework.Controls.MetroLabel gpuLabel;
        private System.Windows.Forms.Panel enginesPanel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.gpuLabel = new MetroFramework.Controls.MetroLabel();
            this.enginesPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // gpuLabel
            // 
            this.gpuLabel.AutoSize = true;
            this.gpuLabel.FontSize = MetroFramework.MetroLabelSize.Tall;
            this.gpuLabel.FontWeight = MetroFramework.MetroLabelWeight.Bold;
            this.gpuLabel.Location = new System.Drawing.Point(23, 60);
            this.gpuLabel.Name = "gpuLabel";
            this.gpuLabel.Size = new System.Drawing.Size(234, 25);
            this.gpuLabel.TabIndex = 0;
            this.gpuLabel.Text = "Общая нагрузка GPU: 0%";
            this.gpuLabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.gpuLabel.UseStyleColors = true;
            // 
            // enginesPanel
            // 
            this.enginesPanel.AutoScroll = true;
            this.enginesPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(24)))), ((int)(((byte)(24)))), ((int)(((byte)(24)))));
            this.enginesPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.enginesPanel.Location = new System.Drawing.Point(23, 100);
            this.enginesPanel.Name = "enginesPanel";
            this.enginesPanel.Size = new System.Drawing.Size(584, 547);
            this.enginesPanel.TabIndex = 1;
            // 
            // FormGPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(630, 670);
            this.Controls.Add(this.enginesPanel);
            this.Controls.Add(this.gpuLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormGPU";
            this.Opacity = 0.9D;
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Проверка нагрузки GPU";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.FormGPU_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}