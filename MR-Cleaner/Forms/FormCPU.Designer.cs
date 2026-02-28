namespace MR_Cleaner.Forms
{
    partial class FormCPU
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormCPU));
            this.totalLabel = new MetroFramework.Controls.MetroLabel();
            this.SuspendLayout();
            // 
            // totalLabel
            // 
            this.totalLabel.AutoSize = true;
            this.totalLabel.FontSize = MetroFramework.MetroLabelSize.Tall;
            this.totalLabel.FontWeight = MetroFramework.MetroLabelWeight.Bold;
            this.totalLabel.Location = new System.Drawing.Point(23, 60);
            this.totalLabel.Name = "totalLabel";
            this.totalLabel.Size = new System.Drawing.Size(192, 25);
            this.totalLabel.TabIndex = 0;
            this.totalLabel.Text = "Общая нагрузка: 0%";
            this.totalLabel.Theme = MetroFramework.MetroThemeStyle.Dark;
            // 
            // FormCPU
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 200);
            this.Controls.Add(this.totalLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormCPU";
            this.Style = MetroFramework.MetroColorStyle.Red;
            this.Text = "Проверка нагрузки ЦП";
            this.Theme = MetroFramework.MetroThemeStyle.Dark;
            this.Load += new System.EventHandler(this.FormCPU_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MetroFramework.Controls.MetroLabel totalLabel;
    }
}