namespace ClientGUI
{
    partial class NewUserForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox textBoxName;
        private System.Windows.Forms.TextBox textBoxAge;
        // Replace textBoxSex with comboBoxGender
        private System.Windows.Forms.ComboBox comboBoxGender;
        private System.Windows.Forms.TextBox textBoxWeight;
        private System.Windows.Forms.TextBox textBoxHeight;
        private System.Windows.Forms.TextBox textBoxShoeSize;
        private System.Windows.Forms.ComboBox comboBoxLeg;
        // Replace textBoxPosition with comboBoxPosition
        private System.Windows.Forms.ComboBox comboBoxPosition;
        private System.Windows.Forms.TextBox textBoxInjury;
        private System.Windows.Forms.Button btnSave;
        // Labels in Latvian
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblAge;
        private System.Windows.Forms.Label lblGender;
        private System.Windows.Forms.Label lblWeight;
        private System.Windows.Forms.Label lblHeight;
        private System.Windows.Forms.Label lblShoeSize;
        private System.Windows.Forms.Label lblLeadingLeg;
        private System.Windows.Forms.Label lblPosition;
        private System.Windows.Forms.Label lblInjury;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.textBoxName = new System.Windows.Forms.TextBox();
            this.lblAge = new System.Windows.Forms.Label();
            this.textBoxAge = new System.Windows.Forms.TextBox();
            this.lblGender = new System.Windows.Forms.Label();
            this.comboBoxGender = new System.Windows.Forms.ComboBox();
            this.lblWeight = new System.Windows.Forms.Label();
            this.textBoxWeight = new System.Windows.Forms.TextBox();
            this.lblHeight = new System.Windows.Forms.Label();
            this.textBoxHeight = new System.Windows.Forms.TextBox();
            this.lblShoeSize = new System.Windows.Forms.Label();
            this.textBoxShoeSize = new System.Windows.Forms.TextBox();
            this.lblLeadingLeg = new System.Windows.Forms.Label();
            this.comboBoxLeg = new System.Windows.Forms.ComboBox();
            this.lblPosition = new System.Windows.Forms.Label();
            this.comboBoxPosition = new System.Windows.Forms.ComboBox();
            this.lblInjury = new System.Windows.Forms.Label();
            this.textBoxInjury = new System.Windows.Forms.TextBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.Text = "Vārds:"; // Latvian for "Name"
            this.lblName.Location = new System.Drawing.Point(12, 15);
            this.lblName.AutoSize = true;
            // 
            // textBoxName
            // 
            this.textBoxName.Location = new System.Drawing.Point(120, 12);
            this.textBoxName.Width = 200;
            // 
            // lblAge
            // 
            this.lblAge.Text = "Vecums:"; // "Age"
            this.lblAge.Location = new System.Drawing.Point(12, 45);
            this.lblAge.AutoSize = true;
            // 
            // textBoxAge
            // 
            this.textBoxAge.Location = new System.Drawing.Point(120, 42);
            this.textBoxAge.Width = 200;
            // 
            // lblGender
            // 
            this.lblGender.Text = "Dzimums:"; // "Sex/Gender"
            this.lblGender.Location = new System.Drawing.Point(12, 75);
            this.lblGender.AutoSize = true;
            // 
            // comboBoxGender
            // 
            this.comboBoxGender.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxGender.FormattingEnabled = true;
            // Add items for gender.
            this.comboBoxGender.Items.AddRange(new object[] {
            "Vīrietis",  // Male
            "Sieviete"   // Female
            });
            this.comboBoxGender.Location = new System.Drawing.Point(120, 72);
            this.comboBoxGender.Width = 200;
            // 
            // lblWeight
            // 
            this.lblWeight.Text = "Svars:"; // "Weight"
            this.lblWeight.Location = new System.Drawing.Point(12, 105);
            this.lblWeight.AutoSize = true;
            // 
            // textBoxWeight
            // 
            this.textBoxWeight.Location = new System.Drawing.Point(120, 102);
            this.textBoxWeight.Width = 200;
            // 
            // lblHeight
            // 
            this.lblHeight.Text = "Augstums:"; // "Height"
            this.lblHeight.Location = new System.Drawing.Point(12, 135);
            this.lblHeight.AutoSize = true;
            // 
            // textBoxHeight
            // 
            this.textBoxHeight.Location = new System.Drawing.Point(120, 132);
            this.textBoxHeight.Width = 200;
            // 
            // lblShoeSize
            // 
            this.lblShoeSize.Text = "Kājas izmērs:"; // "Shoe Size"
            this.lblShoeSize.Location = new System.Drawing.Point(12, 165);
            this.lblShoeSize.AutoSize = true;
            // 
            // textBoxShoeSize
            // 
            this.textBoxShoeSize.Location = new System.Drawing.Point(120, 162);
            this.textBoxShoeSize.Width = 200;
            // 
            // lblLeadingLeg
            // 
           
            this.lblLeadingLeg.Text = "Dominējošā kāja:"; // "Leading Leg"
            this.lblLeadingLeg.Location = new System.Drawing.Point(12, 195);
            this.lblLeadingLeg.AutoSize = true;
            // 
            // textBoxLeadingLeg
            // 
            this.comboBoxLeg.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLeg.FormattingEnabled = true;
            // Add items for gender.
            this.comboBoxLeg.Items.AddRange(new object[] {
            "Labā",
            "Kreisā"
            });
            this.comboBoxLeg.Location = new System.Drawing.Point(120, 192);
            this.comboBoxLeg.Width = 200;
            // 
            // lblPosition
            // 
            this.lblPosition.Text = "Pozīcija laukumā:"; // "Position in Field"
            this.lblPosition.Location = new System.Drawing.Point(12, 225);
            this.lblPosition.AutoSize = true;
            // 
            // comboBoxPosition
            // 
            this.comboBoxPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxPosition.FormattingEnabled = true;
            // Add options (you can adjust or group these as needed)
            this.comboBoxPosition.Items.AddRange(new object[] {
                "Vārtsargs (GK)",
                "Aizsargu (CB)",
                "Mala aizsargs (LB / RB)",
                "Spārnains aizsargs (LWB / RWB)",
                "Sweeper (SW)",
                "Defensīvais puslodzis (CDM)",
                "Centrālais puslodzis (CM)",
                "Uzbrukuma puslodzis (CAM)",
                "Kreisais puslodzis (LM)",
                "Labais puslodzis (RM)",
                "Box-to-Box puslodzis",
                "Uzbrucējs (ST)",
                "Centra uzbrucējs (CF)",
                "Otrais uzbrucējs (SS)",
                "Spārnains uzbrucējs (LW / RW)"
            });
            this.comboBoxPosition.Location = new System.Drawing.Point(120, 222);
            this.comboBoxPosition.Width = 200;
            // 
            // lblInjury
            // 
            this.lblInjury.Text = "Traumas (6 mēn.):"; // "Injury in Last 6Mths"
            this.lblInjury.Location = new System.Drawing.Point(12, 255);
            this.lblInjury.AutoSize = true;
            // 
            // textBoxInjury
            // 
            this.textBoxInjury.Location = new System.Drawing.Point(120, 252);
            this.textBoxInjury.Width = 200;
            // 
            // btnSave
            // 
            this.btnSave.Text = "Saglabāt"; // "Save"
            this.btnSave.Location = new System.Drawing.Point(120, 290);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // NewUserForm
            // 
            this.ClientSize = new System.Drawing.Size(350, 340);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.textBoxName);
            this.Controls.Add(this.lblAge);
            this.Controls.Add(this.textBoxAge);
            this.Controls.Add(this.lblGender);
            this.Controls.Add(this.comboBoxGender);
            this.Controls.Add(this.lblWeight);
            this.Controls.Add(this.textBoxWeight);
            this.Controls.Add(this.lblHeight);
            this.Controls.Add(this.textBoxHeight);
            this.Controls.Add(this.lblShoeSize);
            this.Controls.Add(this.textBoxShoeSize);
            this.Controls.Add(this.lblLeadingLeg);
            this.Controls.Add(this.comboBoxLeg);
            this.Controls.Add(this.lblPosition);
            this.Controls.Add(this.comboBoxPosition);
            this.Controls.Add(this.lblInjury);
            this.Controls.Add(this.textBoxInjury);
            this.Controls.Add(this.btnSave);
            this.Text = "Jauna lietotāja ievade"; // "New User Entry" in Latvian
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
