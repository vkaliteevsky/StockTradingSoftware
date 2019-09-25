namespace Stocks
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.connectButton = new System.Windows.Forms.Button();
            this.getBarsButton = new System.Windows.Forms.Button();
            this.infoTextBox = new System.Windows.Forms.TextBox();
            this.errorTextBox = new System.Windows.Forms.TextBox();
            this.testButton = new System.Windows.Forms.Button();
            this.clearPositionButton = new System.Windows.Forms.Button();
            this.getPositionButton = new System.Windows.Forms.Button();
            this.ClearTextButton = new System.Windows.Forms.Button();
            this.clearStratButton = new System.Windows.Forms.Button();
            this.stratListBox = new System.Windows.Forms.ComboBox();
            this.disconnectButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(9, 10);
            this.connectButton.Margin = new System.Windows.Forms.Padding(2);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(117, 34);
            this.connectButton.TabIndex = 0;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            // 
            // getBarsButton
            // 
            this.getBarsButton.Location = new System.Drawing.Point(9, 296);
            this.getBarsButton.Margin = new System.Windows.Forms.Padding(2);
            this.getBarsButton.Name = "getBarsButton";
            this.getBarsButton.Size = new System.Drawing.Size(117, 41);
            this.getBarsButton.TabIndex = 1;
            this.getBarsButton.Text = "Optimize Portfolio";
            this.getBarsButton.UseVisualStyleBackColor = true;
            this.getBarsButton.Click += new System.EventHandler(this.getBarsButton_Click);
            // 
            // infoTextBox
            // 
            this.infoTextBox.Location = new System.Drawing.Point(130, 9);
            this.infoTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.infoTextBox.Multiline = true;
            this.infoTextBox.Name = "infoTextBox";
            this.infoTextBox.Size = new System.Drawing.Size(568, 409);
            this.infoTextBox.TabIndex = 2;
            // 
            // errorTextBox
            // 
            this.errorTextBox.Location = new System.Drawing.Point(736, 10);
            this.errorTextBox.Margin = new System.Windows.Forms.Padding(2);
            this.errorTextBox.Multiline = true;
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.Size = new System.Drawing.Size(330, 409);
            this.errorTextBox.TabIndex = 3;
            // 
            // testButton
            // 
            this.testButton.Location = new System.Drawing.Point(9, 132);
            this.testButton.Margin = new System.Windows.Forms.Padding(2);
            this.testButton.Name = "testButton";
            this.testButton.Size = new System.Drawing.Size(117, 37);
            this.testButton.TabIndex = 4;
            this.testButton.Text = "Print Strat";
            this.testButton.UseVisualStyleBackColor = true;
            this.testButton.Click += new System.EventHandler(this.testButton_Click);
            // 
            // clearPositionButton
            // 
            this.clearPositionButton.Location = new System.Drawing.Point(9, 341);
            this.clearPositionButton.Margin = new System.Windows.Forms.Padding(2);
            this.clearPositionButton.Name = "clearPositionButton";
            this.clearPositionButton.Size = new System.Drawing.Size(117, 36);
            this.clearPositionButton.TabIndex = 5;
            this.clearPositionButton.Text = "Cancel All Orders";
            this.clearPositionButton.UseVisualStyleBackColor = true;
            this.clearPositionButton.Click += new System.EventHandler(this.clearPositionButton_Click);
            // 
            // getPositionButton
            // 
            this.getPositionButton.Location = new System.Drawing.Point(9, 382);
            this.getPositionButton.Margin = new System.Windows.Forms.Padding(2);
            this.getPositionButton.Name = "getPositionButton";
            this.getPositionButton.Size = new System.Drawing.Size(117, 36);
            this.getPositionButton.TabIndex = 6;
            this.getPositionButton.Text = "Update All";
            this.getPositionButton.UseVisualStyleBackColor = true;
            this.getPositionButton.Click += new System.EventHandler(this.getPositionButton_Click);
            // 
            // ClearTextButton
            // 
            this.ClearTextButton.Location = new System.Drawing.Point(9, 49);
            this.ClearTextButton.Margin = new System.Windows.Forms.Padding(2);
            this.ClearTextButton.Name = "ClearTextButton";
            this.ClearTextButton.Size = new System.Drawing.Size(117, 32);
            this.ClearTextButton.TabIndex = 8;
            this.ClearTextButton.Text = "Close All Market";
            this.ClearTextButton.UseVisualStyleBackColor = true;
            this.ClearTextButton.Click += new System.EventHandler(this.ClearTextButton_Click);
            // 
            // clearStratButton
            // 
            this.clearStratButton.Location = new System.Drawing.Point(9, 173);
            this.clearStratButton.Margin = new System.Windows.Forms.Padding(2);
            this.clearStratButton.Name = "clearStratButton";
            this.clearStratButton.Size = new System.Drawing.Size(117, 37);
            this.clearStratButton.TabIndex = 9;
            this.clearStratButton.Text = "Clear Position";
            this.clearStratButton.UseVisualStyleBackColor = true;
            this.clearStratButton.Click += new System.EventHandler(this.clearStratButton_Click);
            // 
            // stratListBox
            // 
            this.stratListBox.FormattingEnabled = true;
            this.stratListBox.Location = new System.Drawing.Point(9, 107);
            this.stratListBox.Margin = new System.Windows.Forms.Padding(2);
            this.stratListBox.Name = "stratListBox";
            this.stratListBox.Size = new System.Drawing.Size(118, 21);
            this.stratListBox.TabIndex = 11;
            // 
            // disconnectButton
            // 
            this.disconnectButton.Location = new System.Drawing.Point(9, 214);
            this.disconnectButton.Margin = new System.Windows.Forms.Padding(2);
            this.disconnectButton.Name = "disconnectButton";
            this.disconnectButton.Size = new System.Drawing.Size(117, 37);
            this.disconnectButton.TabIndex = 12;
            this.disconnectButton.Text = "Report All";
            this.disconnectButton.UseVisualStyleBackColor = true;
            this.disconnectButton.Click += new System.EventHandler(this.disconnectButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1090, 431);
            this.Controls.Add(this.disconnectButton);
            this.Controls.Add(this.stratListBox);
            this.Controls.Add(this.clearStratButton);
            this.Controls.Add(this.ClearTextButton);
            this.Controls.Add(this.getPositionButton);
            this.Controls.Add(this.clearPositionButton);
            this.Controls.Add(this.testButton);
            this.Controls.Add(this.errorTextBox);
            this.Controls.Add(this.infoTextBox);
            this.Controls.Add(this.getBarsButton);
            this.Controls.Add(this.connectButton);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Button getBarsButton;
        private System.Windows.Forms.TextBox infoTextBox;
        private System.Windows.Forms.TextBox errorTextBox;
        private System.Windows.Forms.Button testButton;
        private System.Windows.Forms.Button clearPositionButton;
        private System.Windows.Forms.Button getPositionButton;
        private System.Windows.Forms.Button ClearTextButton;
        private System.Windows.Forms.Button clearStratButton;
        private System.Windows.Forms.ComboBox stratListBox;
        private System.Windows.Forms.Button disconnectButton;
    }
}

