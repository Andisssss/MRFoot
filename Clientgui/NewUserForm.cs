using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClientGUI
{
    public partial class NewUserForm : Form
    {
        private string usersFilePath;
        public string SelectedUser { get; private set; }  // Will hold "ID: 1 - Andis"

        public NewUserForm(string filePath)
        {
            usersFilePath = filePath;
            InitializeComponent();

            // Disable the Save button initially.
            btnSave.Enabled = false;

            // Attach event handlers to all input controls for validation.
            textBoxName.TextChanged += (s, e) => ValidateFields();
            textBoxAge.TextChanged += (s, e) => ValidateFields();
            comboBoxGender.SelectedIndexChanged += (s, e) => ValidateFields();
            textBoxWeight.TextChanged += (s, e) => ValidateFields();
            textBoxHeight.TextChanged += (s, e) => ValidateFields();
            textBoxShoeSize.TextChanged += (s, e) => ValidateFields();
            comboBoxLeg.SelectedIndexChanged += (s, e) => ValidateFields();
            comboBoxPosition.SelectedIndexChanged += (s, e) => ValidateFields();
            textBoxInjury.TextChanged += (s, e) => ValidateFields();
        }

        /// <summary>
        /// Checks if all required fields have been filled in.
        /// Enables the Save button only if every field is non-empty or has a selection.
        /// </summary>
        private void ValidateFields()
        {
            bool allFilled =
                !string.IsNullOrWhiteSpace(textBoxName.Text) &&
                !string.IsNullOrWhiteSpace(textBoxAge.Text) &&
                comboBoxGender.SelectedIndex != -1 &&
                !string.IsNullOrWhiteSpace(textBoxWeight.Text) &&
                !string.IsNullOrWhiteSpace(textBoxHeight.Text) &&
                !string.IsNullOrWhiteSpace(textBoxShoeSize.Text) &&
                comboBoxLeg.SelectedIndex != -1 &&
                comboBoxPosition.SelectedIndex != -1 &&
                !string.IsNullOrWhiteSpace(textBoxInjury.Text);

            btnSave.Enabled = allFilled;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            // Generate an automatic ID.
            int newId = 1;
            if (File.Exists(usersFilePath))
            {
                var lines = File.ReadAllLines(usersFilePath);
                if (lines.Length > 0)
                {
                    int maxId = lines.Select(line => int.Parse(line.Split(',')[0])).Max();
                    newId = maxId + 1;
                }
            }

            // Gather data from controls.
            string name = textBoxName.Text.Trim();
            string age = textBoxAge.Text.Trim();
            string gender = comboBoxGender.SelectedItem?.ToString() ?? "";
            string weight = textBoxWeight.Text.Trim();
            string height = textBoxHeight.Text.Trim();
            string shoeSize = textBoxShoeSize.Text.Trim();
            string leadingLeg = comboBoxLeg.SelectedItem?.ToString() ?? "";
            string position = comboBoxPosition.SelectedItem?.ToString() ?? "";
            string injury = textBoxInjury.Text.Trim();

            // Create a CSV line.
            string newLine = $"{newId},{name},{age},{gender},{weight},{height},{shoeSize},{leadingLeg},{position},{injury}";

            // Append to the file.
            File.AppendAllLines(usersFilePath, new string[] { newLine });

            // Set the SelectedUser property for Form1 display.
            SelectedUser = $"ID: {newId} - {name}";

            MessageBox.Show("Lietotājs veiksmīgi saglabāts.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
