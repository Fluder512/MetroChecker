namespace MR_Cleaner.Forms
{
    partial class FormRAM
    {
        private System.ComponentModel.IContainer components = null;
        private MetroFramework.Controls.MetroLabel ramLabel;
        private MetroFramework.Controls.MetroLabel usedMbLabel;
        private MetroFramework.Controls.MetroLabel totalMbLabel;
        private MetroFramework.Controls.MetroButton cleanButton;

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
            this.ramLabel = new MetroFramework.Controls.MetroLabel();
            this.usedMbLabel = new MetroFramework.Controls.MetroLabel();
            this.totalMbLabel = new MetroFramework.Controls.MetroLabel();
            this.cleanButton = new MetroFramework.Controls.MetroButton();
            this.SuspendLayout();
            // 
            // ramLabel
            // 
            this.ramLabel.AutoSize = true;
            this.ramLabel.FontSize = MetroFramework.MetroLabelSize.Tall;
            this.ramLabel.FontWeight = MetroFramework.MetroLabelWeight.Bold;
            this.ramLabel.ForeColor = System.Drawing.Color.White;
            this.ramLabel.Location = new System.Drawing.Point(23, 60);
            this.ramLabel.Name = "ramLabel";
            this.ramLabel.Size = new System.Drawing.Size(211, 25);
            this.ramLabel.TabIndex = 0;
            this.ramLabel.Text = "Общая нагрузка ОЗУ: 0%";
            this.ramLabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.ramLabel.UseCustomForeColor = true;
            // 
            // usedMbLabel
            // 
            this.usedMbLabel.AutoSize = true;
            this.usedMbLabel.ForeColor = System.Drawing.Color.White;
            this.usedMbLabel.Location = new System.Drawing.Point(23, 105);
            this.usedMbLabel.Name = "usedMbLabel";
            this.usedMbLabel.Size = new System.Drawing.Size(223, 19);
            this.usedMbLabel.TabIndex = 1;
            this.usedMbLabel.Text = "Используется: 0 MB | Свободно: 0 MB";
            this.usedMbLabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.usedMbLabel.UseCustomForeColor = true;
            // 
            // totalMbLabel
            // 
            this.totalMbLabel.AutoSize = true;
            this.totalMbLabel.ForeColor = System.Drawing.Color.White;
            this.totalMbLabel.Location = new System.Drawing.Point(23, 132);
            this.totalMbLabel.Name = "totalMbLabel";
            this.totalMbLabel.Size = new System.Drawing.Size(150, 19);
            this.totalMbLabel.TabIndex = 2;
            this.totalMbLabel.Text = "Всего памяти: 0 MB";
            this.totalMbLabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.totalMbLabel.UseCustomForeColor = true;
            // 
            // cleanButton
            // 
            this.cleanButton.Location = new System.Drawing.Point(23, 170);
            this.cleanButton.Name = "cleanButton";
            this.cleanButton.Size = new System.Drawing.Size(160, 32);
            this.cleanButton.Style = MetroFramework.MetroColorStyle.Red;
            this.cleanButton.TabIndex = 3;
            this.cleanButton.Text = "Очистить";
            this.cleanButton.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.cleanButton.UseSelectable = true;
            this.cleanButton.Click += new System.EventHandler(this.cleanButton_Click);
            // 
            // FormRAM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 225);
            this.Controls.Add(this.cleanButton);
            this.Controls.Add(this.totalMbLabel);
            this.Controls.Add(this.usedMbLabel);
            this.Controls.Add(this.ramLabel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormRAM";
            this.Opacity = 0.9D;
            this.Resizable = false;
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Проверка нагрузки ОЗУ";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.FormRAM_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}