using System;
using System.IO.Ports;
using System.Threading;


class Program {
    static readonly SerialPort serialPort = new SerialPort();
    static bool during_call = false;
    static bool activeMessage = false;
    static bool calling = false;

    static void Main() {
        string input;

        while(true) {
            if (activeMessage) {
                activeMessage = false;
            }

            if (calling) {
                continue;
            }

            Console.WriteLine("\n-- Wybierz opcje ['q' by wyjsc]: ");
            Console.WriteLine("1. Polacz z modemem (COM1)");
            Console.WriteLine("2. Rozlacz sie z modemem (COM1)");
            Console.WriteLine("3. Zadzwon");
            Console.WriteLine("4. Rozlacz sie");
            Console.WriteLine("5. Wyslij wiadomosc");
            Console.Write(">> ");

            input = Console.ReadLine();
            Console.WriteLine("\n");
            Console.Clear();

            switch (input) {
                case "1":
                    Connect();
                    break;
                case "2":
                    Disconnect();
                    break;
                case "3":
                    Call();
                    break;
                case "4":
                    HangUp();
                    break;
                case "5":
                    SendMessage();
                    break;
                case "q":
                    Environment.Exit(0);
                    break;
            }
        }
    }

    static void DataReceived(object sender, SerialDataReceivedEventArgs e) {
        var data = serialPort.ReadExisting();
        if (string.IsNullOrEmpty(data) || data.Contains("AT")) return;

        if (data.Contains("CON")) {
            during_call = true;
            Console.WriteLine("\n> Polaczono z drugim modemem");
            calling = false;
            return;
        }

        if(data.Contains("RING") && !during_call) {
            Console.Write("\n\n[!] Przychodzace polaczenie - odbieram...");
            serialPort.Write("ATA\r");
            during_call = true;
            return;
        }

        if(data.Contains("NO CARRIER")) {
            Console.Write("\n\n[!] Rozlaczono");
            during_call = false;
            return;
        }

        if (!activeMessage) {
            activeMessage = true;
            Console.WriteLine("\n\n< NOWA WIADOMOSC >");
        }
        Console.Write(data);
    }

    static void Call() {
        if (!IsConnected()) return;
        if (during_call) {
            Console.WriteLine("[!] Polaczenie trwa");
            return;
        }

        Console.WriteLine("> Dzwonienie...");
        calling = true;
        serialPort.Write("ATD\r");
    }

    static void Disconnect() {
        if (!IsConnected()) return;

        Console.WriteLine("> Zamykanie polaczenie...");
        HangUp();
        serialPort.Close();
    }

    static void HangUp() {
        if (!IsConnected()) return;
        if (!IsSecondConnected()) return;

        Console.WriteLine("> Rozlaczanie...");

        serialPort.Write("+");
        Thread.Sleep(100);
        serialPort.Write("+");
        Thread.Sleep(100);
        serialPort.Write("+");
        Thread.Sleep(100);
        Thread.Sleep(1000);
        serialPort.Write("ATH\r");
        Thread.Sleep(2000);
        serialPort.Write("ATH\r");
        during_call = false;
    }

    static void SendMessage() {
        if (!IsConnected()) return;
        if (!IsSecondConnected()) return;

        Console.Write("> Wiadomosc: ");
        string message = Console.ReadLine();

        Console.WriteLine("> Wysylanie...");
        serialPort.Write(message + "\r");
        Console.WriteLine("");
    }

    static bool IsSecondConnected(string message = "[!] Nie polaczono z drugim modemem") {
        if (!during_call) {
            Console.WriteLine(message);
            return false;
        }
        return true;
    }

    static bool IsConnected(string message = "[!] Nie polaczono z modemem") {
        if (!serialPort.IsOpen) {
            Console.WriteLine(message);
            return false;
        }
        return true;
    }

    static void Connect() {
        if (serialPort.IsOpen) {
            Console.WriteLine("[!] Juz polaczono");
            return;
        }

        serialPort.PortName = "COM1";
        serialPort.Parity = Parity.None;
        serialPort.DataBits = 8;
        serialPort.BaudRate = 9600;
        serialPort.StopBits = StopBits.One;
        serialPort.Handshake = Handshake.RequestToSend;
        serialPort.DataReceived += DataReceived;
        serialPort.RtsEnable = true;
        serialPort.DtrEnable = true;
        serialPort.WriteTimeout = 500;
        serialPort.ReadTimeout = 500;

        try {
            serialPort.Open();
            Console.WriteLine("> Nawiazano polaczenie");
        } catch (Exception ex) {
            Console.WriteLine("[ERROR]: " + ex.Message);
        }
    }
}
