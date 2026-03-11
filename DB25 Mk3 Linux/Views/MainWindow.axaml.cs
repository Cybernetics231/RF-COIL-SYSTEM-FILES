using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.IO.Ports;

namespace DB25_Mk3_Linux.Views
{
    public partial class MainWindow : Window
    {
        private readonly SerialPort serialPort1 = new();

        private readonly DispatcherTimer uiTimer;

        private bool flashing = false;
        private bool updatingFromPacket = false;
        private int pingCounter = 0;

        private string latestInputPacket = "";
        private string latestOutputPacket = "";

        public MainWindow()
        {
            InitializeComponent();

            // TOP BAR EVENTS
            buttonRefresh.Click += ButtonRefresh_Click;
            buttonConnect.Click += ButtonConnect_Click;
            LightFlash.Click += LightFlash_Click;
            Gain_adjust_button.Click += Gain_adjust_button_Click;

            // PULSE BUTTONS
            buttonStartPulse.Click += buttonStartPulse_Click;
            buttonStopPulse.Click += buttonStopPulse_Click;

            // OPTIONAL: live pulse update when values change
            numericPulseOn.ValueChanged += (_, __) => UpdatePulseIfRunning();
            numericPulseOff.ValueChanged += (_, __) => UpdatePulseIfRunning();

            // OUTPUT PIN EVENTS (GUI-controlled only)
            checkBox13.IsCheckedChanged += (_, __) => SendPinCommand(13, checkBox13.IsChecked ?? false);
            checkBox16.IsCheckedChanged += (_, __) => SendPinCommand(16, checkBox16.IsChecked ?? false);
            checkBox19.IsCheckedChanged += (_, __) => SendPinCommand(19, checkBox19.IsChecked ?? false);
            checkBox20.IsCheckedChanged += (_, __) => SendPinCommand(20, checkBox20.IsChecked ?? false);
            checkBox22.IsCheckedChanged += (_, __) => SendPinCommand(22, checkBox22.IsChecked ?? false);
            checkBox23.IsCheckedChanged += (_, __) => SendPinCommand(23, checkBox23.IsChecked ?? false);
            checkBox24.IsCheckedChanged += (_, __) => SendPinCommand(24, checkBox24.IsChecked ?? false);
            checkBox25.IsCheckedChanged += (_, __) => SendPinCommand(25, checkBox25.IsChecked ?? false);

            // INPUT PINS (read-only)
            DisableInput(checkBox1);
            DisableInput(checkBox2);
            DisableInput(checkBox3);
            DisableInput(checkBox4);
            DisableInput(checkBox5);   // Not connected
            DisableInput(checkBox6);
            DisableInput(checkBox7);
            DisableInput(checkBox8);
            DisableInput(checkBox9);
            DisableInput(checkBox10);
            DisableInput(checkBox11);
            DisableInput(checkBox12);
            DisableInput(checkBox14);
            DisableInput(checkBox15);
            DisableInput(checkBox17);
            DisableInput(checkBox18);  // AUX 5V
            DisableInput(checkBox21);  // GND

            // DEFAULT OUTPUT STATES (GUI only, no SET)
            updatingFromPacket = true;
            checkBox13.IsChecked = false;
            checkBox16.IsChecked = false;
            checkBox19.IsChecked = true;   // SHUTDOWN default HIGH
            checkBox20.IsChecked = false;  // GATE default LOW
            checkBox22.IsChecked = false;
            checkBox23.IsChecked = false;
            checkBox24.IsChecked = false;
            checkBox25.IsChecked = false;
            updatingFromPacket = false;

            // DEFAULT GAIN
            numericUpDown1.Value = 50;

            // DEFAULT PULSE VALUES
            numericPulseOn.Value = 1;
            numericPulseOff.Value = 4;

            // SERIAL PORT SETUP
            serialPort1.BaudRate = 57600;
            serialPort1.DataReceived += SerialPort1_DataReceived;

            // UI TIMER
            uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            uiTimer.Tick += UiTimer_Tick;

            RefreshPorts();
            UpdateStatus(false);
        }

        private void DisableInput(CheckBox cb)
        {
            cb.IsEnabled = false;
        }

        // ============================
        // PORT LIST
        // ============================
        private void RefreshPorts()
        {
            var ports = SerialPort.GetPortNames();
            comboBoxPorts.ItemsSource = ports;

            if (ports.Length > 0)
                comboBoxPorts.SelectedIndex = 0;
        }

        private void ButtonRefresh_Click(object? sender, RoutedEventArgs e)
        {
            RefreshPorts();
        }

        // ============================
        // CONNECT / STATUS
        // ============================
        private void ButtonConnect_Click(object? sender, RoutedEventArgs e)
        {
            if (comboBoxPorts.SelectedItem is null)
            {
                labelStatus.Text = "No Port Selected";
                labelStatus.Foreground = Avalonia.Media.Brushes.Red;
                return;
            }

            try
            {
                if (serialPort1.IsOpen)
                    serialPort1.Close();

                serialPort1.PortName = comboBoxPorts.SelectedItem.ToString();
                serialPort1.Open();

                uiTimer.Start();
                UpdateStatus(true);

                // Re-assert GUI defaults for outputs (without sending SET)
                updatingFromPacket = true;
                checkBox13.IsChecked = false;
                checkBox16.IsChecked = false;
                checkBox19.IsChecked = true;
                checkBox20.IsChecked = false;
                checkBox22.IsChecked = false;
                checkBox23.IsChecked = false;
                checkBox24.IsChecked = false;
                checkBox25.IsChecked = false;
                updatingFromPacket = false;
            }
            catch
            {
                UpdateStatus(false);
                uiTimer.Stop();
            }
        }

        private void UpdateStatus(bool connected)
        {
            labelStatus.Text = connected ? "Connected" : "Disconnected";
            labelStatus.Foreground = connected
                ? Avalonia.Media.Brushes.LimeGreen
                : Avalonia.Media.Brushes.Red;
        }

        // ============================
        // FLASH CONTROL
        // ============================
        private void LightFlash_Click(object? sender, RoutedEventArgs e)
        {
            if (!serialPort1.IsOpen)
                return;

            flashing = !flashing;

            if (flashing)
            {
                SendRawCommand("F");
                LightFlash.Content = "Stop Flashing";
            }
            else
            {
                SendRawCommand("S");
                LightFlash.Content = "Test Flash";
            }
        }

        // ============================
        // GAIN CONTROL
        // ============================
        private void Gain_adjust_button_Click(object? sender, RoutedEventArgs e)
        {
            if (!serialPort1.IsOpen)
                return;

            int gainValue = (int)numericUpDown1.Value;
            SendRawCommand($"GAIN {gainValue}");
        }

        // ============================
        // PULSE CONTROL
        // ============================
        private void buttonStartPulse_Click(object? sender, RoutedEventArgs e)
        {
            if (!serialPort1.IsOpen)
                return;

            int onMs = (int)numericPulseOn.Value;
            int offMs = (int)numericPulseOff.Value;

            if (onMs < 1) onMs = 1;
            if (offMs < 1) offMs = 1;

            if (onMs > 100)
            {
                labelPulseStatus.Text = "Pulse ON > 100 ms";
                labelPulseStatus.Foreground = Avalonia.Media.Brushes.Red;
                return;
            }

            double duty = onMs / (double)(onMs + offMs);
            if (duty > 0.20)
            {
                labelPulseStatus.Text = "Duty > 20%";
                labelPulseStatus.Foreground = Avalonia.Media.Brushes.Red;
                return;
            }

            // Always send updated values, even if pulse is already running
            SendRawCommand($"PULSE {onMs} {offMs}");
            labelPulseStatus.Text = "Pulse: Running";
            labelPulseStatus.Foreground = Avalonia.Media.Brushes.LimeGreen;
        }

        private void buttonStopPulse_Click(object? sender, RoutedEventArgs e)
        {
            if (!serialPort1.IsOpen)
                return;

            SendRawCommand("PULSE STOP");
            labelPulseStatus.Text = "Pulse: Stopped";
            labelPulseStatus.Foreground = Avalonia.Media.Brushes.Red;
        }

        private void UpdatePulseIfRunning()
        {
            if (!serialPort1.IsOpen)
                return;

            if (!labelPulseStatus.Text.Contains("Running"))
                return;

            int onMs = (int)numericPulseOn.Value;
            int offMs = (int)numericPulseOff.Value;

            if (onMs < 1) onMs = 1;
            if (offMs < 1) offMs = 1;

            double duty = onMs / (double)(onMs + offMs);
            if (duty > 0.20)
                return; // silently ignore invalid live update

            SendRawCommand($"PULSE {onMs} {offMs}");
        }

        // ============================
        // SERIAL DATA RECEIVED
        // ============================
        private void SerialPort1_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string line = serialPort1.ReadLine().Trim();

                if (line.StartsWith("IN:"))
                    latestInputPacket = line.Substring(3);

                if (line.StartsWith("OUT:"))
                    latestOutputPacket = line.Substring(4);
            }
            catch
            {
                // ignore malformed packets
            }
        }

        // ============================
        // UI TIMER
        // ============================
        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            // Send PING every 1 second
            pingCounter++;
            if (pingCounter >= 10)
            {
                SendRawCommand("PING");
                pingCounter = 0;
            }

            labelInputPacket.Text = "IN: " + latestInputPacket;
            labelOutputPacket.Text = "OUT: " + latestOutputPacket;

            if (string.IsNullOrWhiteSpace(latestInputPacket))
                return;

            string[] v = latestInputPacket.Split(',');
            if (v.Length != 12)
                return;

            updatingFromPacket = true;

            // INPUT pins only (order matches Arduino IN packet)
            SetPinState(checkBox1, v[0]);   // DC_POWER
            SetPinState(checkBox2, v[1]);   // FWD_POWER
            SetPinState(checkBox6, v[2]);   // REFL_POWER_LIMIT
            SetPinState(checkBox7, v[3]);   // VFWD
            SetPinState(checkBox8, v[4]);   // VREFL
            SetPinState(checkBox9, v[5]);   // FAULT
            SetPinState(checkBox10, v[6]);  // TRG_GT_SELECT
            SetPinState(checkBox11, v[7]);  // SHUTDOWN_STATUS
            SetPinState(checkBox12, v[8]);  // ADDRESS_SELECTED
            SetPinState(checkBox14, v[9]);  // ENABLE_STATUS
            SetPinState(checkBox15, v[10]); // MISMATCH
            SetPinState(checkBox17, v[11]); // SYSLINK

            updatingFromPacket = false;
        }

        private void SetPinState(CheckBox cb, string value)
        {
            bool high = value == "1";
            cb.IsChecked = high;
            cb.Foreground = high
                ? Avalonia.Media.Brushes.LimeGreen
                : Avalonia.Media.Brushes.Red;
        }

        // ============================
        // OUTPUT PIN COMMAND
        // ============================
        private void SendPinCommand(int pin, bool state)
        {
            if (updatingFromPacket) return;
            if (!serialPort1.IsOpen) return;

            int val = state ? 1 : 0;
            SendRawCommand($"SET {pin} {val}");
        }

        private void SendRawCommand(string cmd)
        {
            try
            {
                if (serialPort1.IsOpen)
                    serialPort1.WriteLine(cmd);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Serial write error: " + ex.Message);
            }
        }
    }
}