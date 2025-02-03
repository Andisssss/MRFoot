using System;
using System.Windows.Forms;

namespace ClientGUI
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private FlowLayoutPanel flowLayoutPanel1;
        private TextBox textBox1;
        private Button connectButton;
        private Button continueButton; // Dynamically added button
        private Label statusLabel; // Label for status updates

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
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.connectButton = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label(); // Label for status message
            this.SuspendLayout();

            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true; // Enable scrolling if there are more buttons
            this.flowLayoutPanel1.Location = new System.Drawing.Point(12, 12);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(500, 200); // Adjust size
            this.flowLayoutPanel1.TabIndex = 0;
            this.flowLayoutPanel1.BackColor = System.Drawing.Color.WhiteSmoke; // Optional background color for better visibility

            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(12, 220);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(500, 100); // Adjusted size for visibility
            this.textBox1.TabIndex = 1;

            // 
            // connectButton
            // 
            this.connectButton.Location = new System.Drawing.Point(12, 330);
            this.connectButton.Name = "connectButton";
            this.connectButton.Size = new System.Drawing.Size(100, 30); // Adjusted size
            this.connectButton.TabIndex = 2;
            this.connectButton.Text = "Connect";
            this.connectButton.UseVisualStyleBackColor = true;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);

            // 
            // statusLabel
            // 
            this.statusLabel.Location = new System.Drawing.Point(12, 370); // Below the Connect button
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(500, 30);
            this.statusLabel.TabIndex = 3;
            this.statusLabel.Text = "Status: Waiting for action...";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(530, 420); // Increased form size to fit everything
            this.Controls.Add(this.statusLabel); // Add the status label to the form
            this.Controls.Add(this.connectButton);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "Form1";
            this.Text = "Client GUI";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // Method to handle button click events (for connection and further actions)


        private void GeneratePortButtons()
        {
            // Simulating port generation logic
            // Replace this with actual port fetching logic
            for (int i = 1; i <= 5; i++) // Example: Adding 5 buttons
            {
                Button portButton = new Button();
                portButton.Text = "COM" + i;
                portButton.Size = new System.Drawing.Size(100, 30);
                portButton.Click += PortButton_Click;
                this.flowLayoutPanel1.Controls.Add(portButton);
            }
        }

        private void PortButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            textBox1.AppendText("Port selected: " + clickedButton.Text + Environment.NewLine);
        }

        private void ContinueButton_Click(object sender, EventArgs e)
        {
            // Logic to continue after selecting ports
            this.statusLabel.Text = "Status: Continuing with the selected port(s).";
            // Optionally, disable further actions or close the form
            connectButton.Enabled = false;
            continueButton.Enabled = false;
        }

     
    }
}
