using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Hardware;
using Firmware;
using System.Windows.Forms;

using System.IO;
namespace PrinterSimulator
{
    class GCODECommand
    {
        public GCODECommand(string cmd)
        {
            if (cmd.Length > 0)
            {
                char[] delimiters = { ' ' };
                string[] commandElements = cmd.Split(delimiters);
                if (commandElements[0].Equals("G1") || commandElements[0].Equals("G92"))
                {
                    for (int i = 1; i < commandElements.Length; i++)
                    {
                        string element = commandElements[i].ToUpper();
                        if (element[0] == ';')
                        {
                            break;
                        }
                        if (element.Length > 1)
                        {
                            string valueString = element.Substring(1, element.Length - 1);
                            float value = float.Parse(valueString, System.Globalization.CultureInfo.InvariantCulture);

                            if (element[0] == 'X')
                            {
                                this.x = value;
                            }
                            else if (element[0] == 'Y')
                            {
                                this.y = value;
                            }
                            else if (element[0] == 'Z')
                            {
                                this.z = value;
                            }
                            else if (element[0] == 'E')
                            {
                                if (value != 0)
                                {
                                    this.laser = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public float x = 0;
        public float y = 0;
        public float z = 0;
        public bool laser = false;
    }

    class PrintSim
    {

        static void PrintFile(PrinterControl simCtl, string fileName)
        {
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            Stopwatch swTimer = new Stopwatch();
            swTimer.Start();

            double currentHeight = .5;
            double layerHeight = 0.5;

            float plateWidthX = 200;
            float plateWidthY = 200;

            float aimWidthX = 2.5F;
            float aimWidthY = 2.5F;

            string line = file.ReadLine();
            string[] Lines = File.ReadAllLines(fileName);
            int total = Lines.Length;
            int iii = 0;
            //line = Lines[iii];

            Console.WriteLine("Press C to cancel");
            while (line != null)//iii < total)
            {
                if(Console.KeyAvailable)
                {
                    char ch = Console.ReadKey().KeyChar;
                    if(char.ToUpper(ch) == 'C')
                    {
                        CommunicationProtocol.SendPacket(simCtl, Packet.RemoveObject(false));
                        return;
                    }
                }
                Console.WriteLine(iii + "/" + total);
                iii++;
                GCODECommand command = new GCODECommand(line);
                //Console.WriteLine(line);
                
                if (command.z > currentHeight)
                {
                    double layers = (command.z - currentHeight) / layerHeight;
                    for (double i = 0.5; i < layers; i++)
                    {
                        CommunicationProtocol.SendPacket(simCtl, Packet.RaiseBuildPlatformCommand(command.laser));
                        currentHeight += layerHeight;
                    }
                    Console.WriteLine(layers);
                }
                if (command.z < currentHeight && command.z!=0)
                {
                    double layers = (command.z - currentHeight) / layerHeight * -1;
                    for (double i = 0.5; i < layers; i++)
                    {
                        CommunicationProtocol.SendPacket(simCtl, Packet.LowerBuildPlatformCommand(command.laser));
                        currentHeight -= layerHeight;
                    }
                    Console.WriteLine(layers);
                }
                if (command.x != 0 || command.y != 0)
                {
                    CommunicationProtocol.SendPacket(simCtl, Packet.AimLaserCommand((command.x / (plateWidthX / 2)) * aimWidthX, (command.y / (plateWidthY / 2) * aimWidthY), command.laser));
                }
                //Console.WriteLine("Laser: " + command.laser);
                //CommunicationProtocol.SendPacket(simCtl, Packet.LaserOnOffCommand(command.laser));
                
                //line = Lines[iii];
                line = file.ReadLine();
            }

            swTimer.Stop();
            long elapsedMS = swTimer.ElapsedMilliseconds;

            Console.WriteLine("Total Print Time: {0}", elapsedMS / 1000.0);
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        [STAThread]

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        static string getFile()
        {
            OpenFileDialog fd = new OpenFileDialog();
            fd.ShowDialog();
            return fd.FileName;
        }

        [STAThread]
        static void Main()
        {
            IntPtr ptr = GetConsoleWindow();
            MoveWindow(ptr, 0, 0, 1000, 400, true);

            // Start the printer - DO NOT CHANGE THESE LINES
            PrinterThread printer = new PrinterThread();
            Thread oThread = new Thread(new ThreadStart(printer.Run));
            oThread.Start();
            printer.WaitForInit();

            // Start the firmware thread - DO NOT CHANGE THESE LINES
            FirmwareController firmware = new FirmwareController(printer.GetPrinterSim());
            oThread = new Thread(new ThreadStart(firmware.Start));
            oThread.Start();
            firmware.WaitForInit();

            SetForegroundWindow(ptr);
            //Jordan - Creates packet and Send packet takes the packet as well as "GetPrinterSim"
            //Jordan - Creates packet and Send packet takes the packet as well as "GetPrinterSim"
            Packet p = Packet.GetFirmwareVersionCommand();//new Packet((byte)CommunicationCommand.GetFirmwareVersion, new byte[1]);
            //CommunicationProtocol.SendPacket(printer.GetPrinterSim(),Packet.ResetBuildPlatformCommand());
            string response = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), p);
            string versionNum = response.Split(' ')[1];

            bool fDone = false;
            while (!fDone)
            {
                Console.Clear();
                Console.WriteLine("Firmware Version: " + versionNum);
                Console.WriteLine("3D Printer Simulation - Control Menu\n");
                Console.WriteLine("P - Print");
                Console.WriteLine("T - Test");
                Console.WriteLine("Q - Quit");

                char ch = Char.ToUpper(Console.ReadKey().KeyChar);
                switch (ch)
                {
                    case 'P': // Print
                        string fileName = getFile();
                        if (fileName == "")
                        {
                            break;
                        }
                        Packet resetPacket = Packet.ResetBuildPlatformCommand(false);
                        string resetResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), resetPacket);

                        PrintFile(printer.GetPrinterSim(), fileName);
                        break;

                    case 'T': // Test menu
                        
                        break;

                    case 'Q' :  // Quite
                        printer.Stop();
                        firmware.Stop();
                        fDone = true;
                        break;
                }

            }

        }

        public void Tests()
        {

        }
    }
}