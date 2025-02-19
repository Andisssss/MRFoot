using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClientGUI
{
    public partial class Form1 : Form
    {
        private TcpListener _server;
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _listenerThread;
        private bool _isRunning = true;
        private List<string> selectedPorts = new List<string>();

        // File name for storing users.
        private string usersFilePath = Path.Combine(Application.StartupPath, "users.txt");

        public Form1()
        {
            InitializeComponent();

            // Initially hide the connect button and status label; they will show only after a user has been selected.
            connectButton.Visible = false;
            statusLabel.Visible = false;
            textBox1.Visible = false; // debug window

            // Start with the user-selection screen.
            ShowUserSelectionScreen();
            StartTcpServer();
        }

        /// <summary>
        /// Show two buttons at startup: "New User" and "Continue".
        /// </summary>
        private void ShowUserSelectionScreen()
        {
            // Clear any existing controls from the flow panel.
            flowLayoutPanel1.Controls.Clear();

            // Create "New User" button.
            Button newUserButton = new Button
            {
                Text = "Jauns lietotājs",
                Width = 150,
                Height = 30,
                Margin = new Padding(5),
                BackColor = System.Drawing.Color.LightBlue
            };
            newUserButton.Click += newUserButton_Click;
            flowLayoutPanel1.Controls.Add(newUserButton);

            // Create "Continue" button.
            Button contButton = new Button
            {
                Text = "Turpināt ar esošu lietotāju",
                Width = 200,
                Height = 30,
                Margin = new Padding(5),
                BackColor = System.Drawing.Color.LightGreen
            };
            contButton.Click += continueButton_Click;
            flowLayoutPanel1.Controls.Add(contButton);
        }

        /// <summary>
        /// Handler when the New User button is clicked.
        /// Opens a new form for data entry.
        /// </summary>
        private void newUserButton_Click(object sender, EventArgs e)
        {
            // Open the new user entry form as a dialog.
            NewUserForm newUserForm = new NewUserForm(usersFilePath);
            if (newUserForm.ShowDialog() == DialogResult.OK)
            {
                // Optionally, you might reload users in case the user now wants to “continue” with the new user.
                AppendText("New user saved.");
                // Proceed to the next step (show connect button etc.)
                ProceedAfterUserSelection();
            }
        }

        /// <summary>
        /// Handler when the Continue button is clicked.
        /// Displays a ComboBox for the operator to select an existing user.
        /// </summary>
        private void continueButton_Click(object sender, EventArgs e)
        {
            // Clear the flow panel and show a dropdown (ComboBox) with the list of users.
            flowLayoutPanel1.Controls.Clear();

            Label selectLabel = new Label
            {
                Text = "Select User:",
                AutoSize = true,
                Margin = new Padding(5)
            };
            flowLayoutPanel1.Controls.Add(selectLabel);

            ComboBox userComboBox = new ComboBox
            {
                Width = 200,
                Margin = new Padding(5)
            };

            // Load the user data from the file (if any)
            if (File.Exists(usersFilePath))
            {
                // Assuming each line is in CSV format: ID,Name,Age,Sex,Weight,Height,ShoeSize,LeadingLeg,Position,Injury
                string[] lines = File.ReadAllLines(usersFilePath);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Use the first two fields (ID and Name) for display.
                        string[] parts = line.Split(',');
                        if (parts.Length >= 2)
                        {
                            string display = $"ID: {parts[0]} - {parts[1]}";
                            userComboBox.Items.Add(display);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("No user data file found. Please create a new user.");
                ShowUserSelectionScreen();
                return;
            }

            if (userComboBox.Items.Count == 0)
            {
                MessageBox.Show("No users available. Please create a new user.");
                ShowUserSelectionScreen();
                return;
            }

            // Select the first item by default.
            userComboBox.SelectedIndex = 0;
            flowLayoutPanel1.Controls.Add(userComboBox);

            // Add a confirmation button.
            Button selectUserButton = new Button
            {
                Text = "OK",
                Width = 60,
                Height = 30,
                Margin = new Padding(5),
                BackColor = System.Drawing.Color.LightGreen
            };
            selectUserButton.Click += (s, ea) =>
            {
                AppendText($"User selected: {userComboBox.SelectedItem.ToString()}");
                ProceedAfterUserSelection();
            };
            flowLayoutPanel1.Controls.Add(selectUserButton);
        }

        /// <summary>
        /// Once a user has been selected or entered, hide the user-selection controls
        /// and show the Connect button and debug window.
        /// </summary>
        private void ProceedAfterUserSelection()
        {
            // Clear the flow panel and show the Connect button.
            flowLayoutPanel1.Controls.Clear();
            connectButton.Visible = true;
            statusLabel.Visible = true;
            textBox1.Visible = true;

            AppendText("User confirmed. Now you may connect to the client.");

            
        }

        // -------------------------
        // The remaining methods below are your existing code for TCP server,
        // port button creation, sending messages, etc.
        // -------------------------

        // Start the server thread and listen for client connections
        private void StartTcpServer()
        {
            _listenerThread = new Thread(() =>
            {
                try
                {
                    _server = new TcpListener(IPAddress.Loopback, 5555);
                    _server.Start();
                    AppendText("GUI Server started. Waiting for Client...");

                    _client = _server.AcceptTcpClient();  // Wait for the client to connect
                    AppendText("Client connected!");

                    _stream = _client.GetStream();

                    // Start listening for messages from the client
                    Thread messageListenerThread = new Thread(ListenForMessages);
                    messageListenerThread.IsBackground = true;
                    messageListenerThread.Start();
                }
                catch (Exception ex)
                {
                    AppendText($"Error: {ex.Message}");
                }
            });

            _listenerThread.IsBackground = true;
            _listenerThread.Start();
        }

        // Listen for incoming messages from the client
        private void ListenForMessages()
        {
            byte[] buffer = new byte[1024];

            while (_isRunning)
            {
                try
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    AppendText($"Client: {message}"); // Debugging log

                    // If the message contains COM port information, extract it
                    if (message.ToLower().Contains("com"))
                    {
                        string portsString = message.Replace("Client:", "").Trim();
                        string[] ports = portsString.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        AppendText($"Parsed ports: {string.Join(", ", ports)}");

                        CreatePortButtons(ports);
                    }
                }
                catch (Exception ex)
                {
                    AppendText($"Error reading message: {ex.Message}");
                    AppendText("Connection closed.");
                    break;
                }
            }
        }

        // Create buttons for each available port
        private void CreatePortButtons(string[] ports)
        {
            try
            {
                AppendText($"[DEBUG] CreatePortButtons called with {ports.Length} ports.");

                // Clear previous selections (dynamically created buttons)
                selectedPorts.Clear();

                if (flowLayoutPanel1.InvokeRequired)
                {
                    flowLayoutPanel1.Invoke((MethodInvoker)(() =>
                    {
                        flowLayoutPanel1.Controls.Clear();
                        AppendText("[DEBUG] Cleared FlowLayoutPanel (via Invoke).");
                    }));
                }
                else
                {
                    flowLayoutPanel1.Controls.Clear();
                    AppendText("[DEBUG] Cleared FlowLayoutPanel.");
                }

                if (ports == null || ports.Length == 0)
                {
                    AppendText("No COM ports received. Buttons not created.");
                    return;
                }

                foreach (var port in ports)
                {
                    AppendText($"[DEBUG] Creating button for: {port}");

                    var button = new Button
                    {
                        Text = port,
                        Width = 150,
                        Height = 40,
                        Margin = new Padding(5),
                        BackColor = System.Drawing.Color.LightBlue
                    };

                    button.Click += (sender, e) =>
                    {
                        if (selectedPorts.Count < 2)
                        {
                            selectedPorts.Add(port);
                            button.Enabled = false;
                            AppendText($"Selected: {port}");

                            // After selecting the second port, clear all buttons
                            if (selectedPorts.Count == 2)
                            {
                                string portsMessage = string.Join(",", selectedPorts);
                                AppendText($"Sent selected ports to client: {portsMessage}");

                                // Use Application.StartupPath to create the file in the same folder as the executable.
                                string filePath = Path.Combine(Application.StartupPath, "selected_ports.txt");
                                File.WriteAllText(filePath, portsMessage);
                                AppendText($"Written selected ports to file: {filePath}");

                                // Clear the buttons after both ports have been selected
                                if (flowLayoutPanel1.InvokeRequired)
                                {
                                    flowLayoutPanel1.Invoke((MethodInvoker)(() =>
                                    {
                                        flowLayoutPanel1.Controls.Clear();  // Remove buttons
                                        AppendText("[DEBUG] Cleared FlowLayoutPanel after selecting two ports.");
                                    }));
                                }
                                else
                                {
                                    flowLayoutPanel1.Controls.Clear();  // Remove buttons
                                    AppendText("[DEBUG] Cleared FlowLayoutPanel after selecting two ports.");
                                }

                                // Transition to the next screen
                                SwitchToNextScreen();
                            }
                        }
                    };

                    if (flowLayoutPanel1.InvokeRequired)
                    {
                        flowLayoutPanel1.Invoke((MethodInvoker)(() =>
                        {
                            flowLayoutPanel1.Controls.Add(button);
                            flowLayoutPanel1.PerformLayout();
                            flowLayoutPanel1.Refresh();
                            AppendText($"[DEBUG] Added button for {port} (via Invoke).");
                        }));
                    }
                    else
                    {
                        flowLayoutPanel1.Controls.Add(button);
                        flowLayoutPanel1.PerformLayout();
                        flowLayoutPanel1.Refresh();
                        AppendText($"[DEBUG] Added button for {port}.");
                    }
                }

                AppendText($"[DEBUG] Created {ports.Length} buttons.");
            }
            catch (Exception ex)
            {
                AppendText($"[ERROR] CreatePortButtons failed: {ex.Message}");
            }
        }

        // Transition to the next screen after selecting the ports
        private void SwitchToNextScreen()
        {
            // Create and show the 'Calibrate' button first
            Button calibrateButton = new Button
            {
                Text = "Calibrate",
                Width = 150,
                Height = 40,
                Margin = new Padding(5),
                BackColor = System.Drawing.Color.LightGreen
            };

            calibrateButton.Click += (sender, e) =>
            {
                // Send message to client for calibration
                SendMessageToClient("calibrate");
                calibrateButton.Enabled = false;
                AppendText("Calibrate command sent to client.");

                // Add Start, Stop, HMD, Exit buttons
                ShowControlButtons();
            };

            // Add the Calibrate button to the panel
            AddButtonToPanel(calibrateButton);

            // Method to add the other buttons after Calibrate is clicked
            void ShowControlButtons()
            {
                Button startButton = new Button
                {
                    Text = "Start",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(5),
                    BackColor = System.Drawing.Color.LightGreen
                };

                startButton.Click += (sender, e) =>
                {
                    SendMessageToClient("start");
                    startButton.Enabled = false;
                    AppendText("Start command sent to client.");
                };

                Button stopsButton = new Button
                {
                    Text = "Stop",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(5),
                    BackColor = System.Drawing.Color.IndianRed
                };

                stopsButton.Click += (sender, e) =>
                {
                    SendMessageToClient("stop");
                    AppendText("Stop command sent to client.");
                };

                Button exitButton = new Button
                {
                    Text = "Exit",
                    Width = 150,
                    Height = 40,
                    Margin = new Padding(5),
                    BackColor = System.Drawing.Color.DarkRed
                };

                exitButton.Click += (sender, e) =>
                {
                    SendMessageToClient("exit");
                    exitButton.Enabled = true;
                    AppendText("Exit command sent to client.");
                };

                // Add these buttons to the panel
                AddButtonToPanel(startButton);
                AddButtonToPanel(stopsButton);
                
                AddButtonToPanel(exitButton);
            }
           

            // Method to add buttons to the FlowLayoutPanel
            void AddButtonToPanel(Button button)
            {
                if (flowLayoutPanel1.InvokeRequired)
                {
                    flowLayoutPanel1.Invoke((MethodInvoker)(() =>
                    {
                        flowLayoutPanel1.Controls.Add(button);
                        flowLayoutPanel1.PerformLayout();
                        flowLayoutPanel1.Refresh();
                    }));
                }
                else
                {
                    flowLayoutPanel1.Controls.Add(button);
                    flowLayoutPanel1.PerformLayout();
                    flowLayoutPanel1.Refresh();
                }
            }
        }

        // Send messages to the client
        private void SendMessageToClient(string message)
        {
            if (_stream == null) return;

            byte[] responseBytes = Encoding.UTF8.GetBytes(message);
            _stream.Write(responseBytes, 0, responseBytes.Length);
            _stream.Flush();
        }

        // Append text to the TextBox (for debugging and feedback)
        private void AppendText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendText(text)));
                return;
            }
            textBox1.AppendText(text + Environment.NewLine); // Append to a TextBox for feedback
        }

        // Handle form closing
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _isRunning = false;
                _stream?.Close();
                _client?.Close();
                _server?.Stop();
                Console.WriteLine("[Server]: Connection closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }

        // Connect Button Click handler (now visible only after a user is selected)
        private void connectButton_Click(object sender, EventArgs e)
        {
            // Send a connection message to the client
            SendMessageToClient("connect");
            connectButton.Enabled = false;
            AppendText("Connect command sent to client. Waiting for port list...");

            // Disable the connect button and inform the user
            connectButton.Enabled = false;
            AppendText("Connect button disabled. Awaiting port information.");

            // After sending the message, wait for the client to send a response
            Task.Run(() => WaitForClientResponse());
        }

        private void WaitForClientResponse()
        {
            try
            {
                // Assuming _stream is the network stream used to communicate with the client
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (_isRunning && _stream != null)
                {
                    // Keep waiting for the client response (blocking call)
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        AppendText("Client connection lost. No data received.");
                        break; // Connection closed
                    }

                    string clientMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    AppendText($"Received message from client: {clientMessage}");

                    // Here, you can handle the specific response from the client
                    if (clientMessage.ToLower() == "ports")
                    {
                        AppendText("Client has sent COM ports information.");
                        // Do something with the COM ports data (e.g., display buttons, etc.)
                    }
                
                }
            }
            catch (Exception ex)
            {
                AppendText($"Error while waiting for client response: {ex.Message}");
            }
        }
    }
}
