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

                // NEW: Initialize output pins to known state
                InitializeOutputs();

                uiTimer.Start();
                UpdateStatus(true);
            }
            catch
            {
                UpdateStatus(false);
                uiTimer.Stop();
            }
        }


        private void InitializeOutputs()
        {
            if (!serialPort1.IsOpen) return;

            // Prevent feedback from packet updates
            updatingFromPacket = true;

            // Set all output pins to default state
            // Pin 19 (SHUTDOWN) = HIGH, all others LOW
            SendRawCommand("SET 13 0");   // ADDR3
            SendRawCommand("SET 16 0");   // PTT_IN
            SendRawCommand("SET 19 1");   // SHUTDOWN (HIGH)
            SendRawCommand("SET 20 0");   // GATE_IN
            SendRawCommand("SET 22 0");   // PSU_ADJUST
            SendRawCommand("SET 23 0");   // ADDR0
            SendRawCommand("SET 24 0");   // ADDR1
            SendRawCommand("SET 25 0");   // ADDR2

            // Update GUI checkboxes to match
            checkBox13.IsChecked = false;
            checkBox16.IsChecked = false;
            checkBox19.IsChecked = true;   // SHUTDOWN
            checkBox20.IsChecked = false;
            checkBox22.IsChecked = false;
            checkBox23.IsChecked = false;
            checkBox24.IsChecked = false;
            checkBox25.IsChecked = false;

            updatingFromPacket = false;
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
            if (v.Length != 15)  // Now 15 values
                return;

            updatingFromPacket = true;

            // Map according to new order
            SetPinState(checkBox1, 1, v[0]);   // DC_POWER (pin1)
            SetPinState(checkBox2, 2, v[1]);   // FWD_POWER (pin2)
            SetPinState(checkBox3, 3, v[2]);   // OVER_TEMP (pin3)
            SetPinState(checkBox4, 4, v[3]);   // OVER_DUTY (pin4)
            SetPinState(checkBox6, 6, v[4]);   // REFL_POWER_LIMIT (pin6)
            SetPinState(checkBox7, 7, v[5]);   // VFWD (pin7)
            SetPinState(checkBox8, 8, v[6]);   // VREFL (pin8)
            SetPinState(checkBox9, 9, v[7]);   // FAULT (pin9)
            SetPinState(checkBox10, 10, v[8]); // TRG_GT_SELECT (pin10)
            SetPinState(checkBox11, 11, v[9]); // SHUTDOWN_STATUS (pin11)
            SetPinState(checkBox12, 12, v[10]); // ADDRESS_SELECTED (pin12)
            SetPinState(checkBox14, 14, v[11]); // ENABLE_STATUS (pin14)
            SetPinState(checkBox15, 15, v[12]); // MISMATCH (pin15)
            SetPinState(checkBox17, 17, v[13]); // SYSLINK (pin17)
            SetPinState(checkBox18, 18, v[14]); // AUX (pin18)

            updatingFromPacket = false;

        }

        private void SetPinState(CheckBox cb, int pinNumber, string value)
        {
            bool faultActive = value == "1";

            cb.IsChecked = faultActive;
            cb.Content = faultActive ? "✗" : pinNumber.ToString();
            cb.Foreground = faultActive
                ? Avalonia.Media.Brushes.Red
                : Avalonia.Media.Brushes.Green;
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
