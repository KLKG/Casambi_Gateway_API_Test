using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace UDP_WPF
{
    public class ViewModel : INotifyPropertyChanged
    {
        public const string
            ConnectText = "Connect",
            ConnectingText = "Connecting...",
            DisconnectText = "Disconnect",
            DisconnectingText = "Disconnecting...",
            FailedText = "Try Again",
            TestText = "Test",
            TestingText = "Testing ...";

        public event PropertyChangedEventHandler PropertyChanged;
        UdpClient listener;
        Socket sender;
        IPAddress localIP;
        internal ScrollViewer scrollViewer;

        #region udp-config and connect
        private bool _IsConfigEnabled = true;
        public bool IsConfigEnabled { get => _IsConfigEnabled; set => Set(ref _IsConfigEnabled, value); }

        private IPAddress _IP = IPAddress.Parse("192.168.20.255");
        public IPAddress IP { get => _IP; set => Set(ref _IP, value); }

        private ushort _Port = 10009;
        public ushort Port { get => _Port; set => Set(ref _Port, value); }

        private byte _NetID = 0;
        public byte NetID { get => _NetID; set => Set(ref _NetID, value); }

        private bool _IsConnected = false;
        public bool IsConnected { get => _IsConnected; set => Set(ref _IsConnected, value); }

        private string _ConnectButtonText = ConnectText;
        public string ConnectButtonText { get => _ConnectButtonText; set => Set(ref _ConnectButtonText, value); }

        private bool _ConnectButtonState = false;
        public bool ConnectButtonState 
        { 
            get => _ConnectButtonState; 
            set {
                if (Set(ref _ConnectButtonState, value))
                    ChangeConnectButtonState(value);
            } 
        }

        private bool _IsConnectButtonEnabled = true;
        public bool IsConnectButtonEnabled { get => _IsConnectButtonEnabled; set => Set(ref _IsConnectButtonEnabled, value); }

        private async void ChangeConnectButtonState(bool value)
        {
            if (!IsConnectButtonEnabled) return;
            IsConnectButtonEnabled = false;
            { 
                // connect
                if (value)
                {
                    AddLogLines("connecting...");
                    ConnectButtonText = ConnectingText;
                    IsConfigEnabled = false;
                    try
                    {

                        listener = new UdpClient(Port);
                        await Task.Delay(100);
                        IsConnected = true;
                        Listen();
                        AddLogLines($"connected to {IP}:{Port}");
                        ConnectButtonText = DisconnectText;
                    }
                    catch (Exception ex)
                    {
                        AddLogLines($"connection to {IP}:{Port} failed");
                        AddLogLines(ex.Message.Replace("\r", "n").Split('\n'));
                        ConnectButtonText = FailedText;
                        IsConfigEnabled = true;
                    }
                }

                // disconnect
                else
                {
                    AddLogLines($"disconnecting...");
                    ConnectButtonText = DisconnectingText;
                    IsTesting = false;
                    IsConnected = false;
                    try
                    {
                        await Task.Delay(100);
                        listener.Close();
                        listener = null;
                        AddLogLines($"disconnected from {IP}:{Port}");
                        ConnectButtonText = ConnectText;
                        IsConfigEnabled = true;
                    }
                    catch (Exception ex)
                    {
                        AddLogLines("Error Occured");
                        AddLogLines($"disconnection from {IP}:{Port} failed");
                        AddLogLines(ex.Message.Replace("\r", "n").Split('\n'));
                        ConnectButtonText = FailedText;
                        IsConfigEnabled = false;
                        IsConnected = true;
                    }

                }
            }
            IsConnectButtonEnabled = true;
        }
        #endregion

        #region test
        private bool _IsTesting = false;
        public bool IsTesting { get => _IsTesting; set => Set(ref _IsTesting, value); }

        private string _TestButtonText = TestText;
        public string TestButtonText { get => _TestButtonText; set => Set(ref _TestButtonText, value); }

        private bool _TestButtonState = false;
        public bool TestButtonState
        {
            get => _TestButtonState;
            set
            {
                if (Set(ref _TestButtonState, value))
                {
                    ChangeTestButtonState(value);
                    TestVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private int _TestProgress = 0;
        public int TestProgress { get => _TestProgress; set => Set(ref _TestProgress, value); }

        private Visibility _TestVisibility = Visibility.Collapsed;
        public Visibility TestVisibility { get => _TestVisibility; set => Set(ref _TestVisibility, value); }

        // start/stop test button
        private void ChangeTestButtonState(bool value)
        {
            if (!IsConnected) return;
            {
                // connect
                if (value)
                {
                    AddLogLines($"start test");
                    IsTesting = true;
                    TestButtonText = TestingText;
                    Test();
                }

                // disconnect
                else
                {
                    IsTesting = false;
                    TestButtonText = TestText;
                }
            }
        }

        private async void Test()
        {
            var casambiID = 1;

            while (IsTesting)
            {
                AddLogLines($"test: {casambiID}/251");

                // send signal
                var signal = $"{NetID}.72.2.39.{casambiID:X2}";
                if (!Send(signal)) break;

                // inc casambi id
                await Task.Delay(1000);
                if (casambiID < 0xFB)
                    casambiID++;
                else
                {
                    casambiID = 1;
                    AddLogLines(new string('-', 32));
                }
                TestProgress = casambiID - 1;
            }
            AddLogLines($"end test");
        }
        #endregion

        #region log
        private ObservableCollection<string> _Log = new ObservableCollection<string>();
        public ObservableCollection<string> Log { get => _Log; set => Set(ref _Log, value); }
        // clear log button

        public void AddLogLines(params string[] lines)
        {
            var scroll = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight;
            var d = DateTime.Now;
            foreach (var line in lines)
                Log.Add($"{d.Hour:00}:{d.Minute:00}:{d.Second:00}.{d.Millisecond:000} \t{line}");
            if (scroll) scrollViewer.ScrollToEnd();
        }

        private void AddLogException(string message, Exception ex)
        {
            AddLogLines(message, $"Exception: " + ex.Message);
        }

        private bool _ClearLogButtonState = false;
        public bool ClearLogButtonState
        {
            get => _ClearLogButtonState;
            set
            {
                if (Set(ref _ClearLogButtonState, value) & value)
                {
                    Log.Clear();
                    ClearLogButtonState = false;
                }
            }
        }
        #endregion

        #region signal
        private string _Signal = string.Empty;
        public string Signal { get => _Signal; set => Set(ref _Signal, value); }
        // send signal button

        private bool _SignalButtonState = false;
        public bool SignalButtonState
        { 
            get => _SignalButtonState;
            set
            {
                if (Set(ref _SignalButtonState, value) && value)
                {
                    Send(Signal);
                    SignalButtonState = false;
                }
            }
        }
        #endregion


        private async void Listen()
        {
            // get local ip
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(IP, 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address;
            }

            do
            {
                if (listener != null && listener.Available > 0)
                    try
                    {
                        var r = await listener.ReceiveAsync();
                        if (!r.RemoteEndPoint.Address.Equals(localIP))
                        {
                            var signal = Encoding.ASCII.GetString(r.Buffer);
                            var txt = GetPrintableSignal(signal);
                            AddLogLines($"received from {r.RemoteEndPoint}: {txt}");
                            var lines = signal.Replace("\r", "").Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines) Received(line);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLogException("Error while listening", ex);
                    }
                await Task.Delay(25);
            } while (IsConnected);
        }

        private void Received(string signal)
        {
            // split
            var parts = signal.Split('.');
            if (parts.Length > 3 && int.TryParse(parts[0], out var netID) && netID == NetID)
            {
                var commandDirection = parts[1];
                var length = parts[2];
                var commandID = parts[3];

                AddLogLines(
                    $"Message:    {signal}", 
                    $"NetID:      {netID}",
                    $"Direction:  {commandDirection}",
                    $"Length:     {length}",
                    $"CommandID:  {commandID}");

                switch (commandID)
                {
                    case "39":
                        {
                            if (parts.Length > 6
                                && byte.TryParse(parts[6], NumberStyles.HexNumber, null, out var priorityNodeType)
                                && byte.TryParse(parts[8], NumberStyles.HexNumber, null, out var condition2))
                            {
                                var unitID = parts[4];
                                var scene = parts[5];
                                var priority = priorityNodeType & 0b00111111;
                                var nodeType = (priorityNodeType & 0b11000000) >> 6;
                                var condition = parts[7];
                                var online = condition2 & 0b0000001;
                                AddLogLines(
                                    $"UnitID:     {unitID}",
                                    $"Scene:      {scene}",
                                    $"Priority:   {priority}",
                                    $"NodeType:   {nodeType}",
                                    $"Condition:  {condition}",
                                    $"Online:     {online}");
                            }
                            else
                            {
                                AddLogLines("corrupt signal");
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
        }

        private bool Send(string signal)
        {
            if (!signal.EndsWith("\r\n")) signal += "\r\n";

            sender = sender ?? new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var buffer = Encoding.ASCII.GetBytes(signal);
            var ep = new IPEndPoint(IP, Port);
            var txt = GetPrintableSignal(signal);

            try
            {
                sender.SendTo(buffer, ep);
                AddLogLines($"sended to {ep}: {txt}");
                return true;
            }
            catch (Exception ex)
            {
                AddLogException("Error while sending", ex);
                return false;
            }
        }

        internal void Close()
        {
            IsConnected = false;
            sender?.Close();
            sender = null;
            listener?.Close();
            listener = null;
        }

        private string GetPrintableSignal(string signal)
            => signal.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

        private bool Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = "")
        {
            if (!Equals(storage, value))
            {
                storage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

    }
}