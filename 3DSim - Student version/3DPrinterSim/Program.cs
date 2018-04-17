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
            //System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            string[] Lines = File.ReadAllLines(fileName);
            Packet topPacket = Packet.ToTopCommand(false);//.ResetBuildPlatformCommand(false);
            string topResponse = CommunicationProtocol.SendPacket(simCtl, topPacket);

            Stopwatch swTimer = new Stopwatch();
            swTimer.Start();
            CommunicationProtocol.SendPacket(simCtl, Packet.ToBottom(false));

            double currentHeight = .5;
            double layerHeight = 0.5;

            float plateWidthX = 200;
            float plateWidthY = 200;

            float aimWidthX = 2.5F;
            float aimWidthY = 2.5F;

            //string line = file.ReadLine();
            int total = Lines.Length;
            //int currentLine = 0;

            Console.WriteLine("Press C to cancel");
            foreach(string line in Lines)//while (line != null)
            {
                if(Console.KeyAvailable)
                {
                    char ch = Console.ReadKey().KeyChar;
                    if(char.ToUpper(ch) == 'C')
                    {
                        break;
                    }
                }
                //Console.WriteLine(currentLine + "/" + total);
                //currentLine++;
                GCODECommand command = new GCODECommand(line);
                
                if (command.z > currentHeight)
                {
                    double layers = (command.z - currentHeight) / layerHeight;
                    for (double i = 0.5; i < layers; i++)
                    {
                        CommunicationProtocol.SendPacket(simCtl, Packet.RaiseBuildPlatformCommand(command.laser));
                        currentHeight += layerHeight;
                    }
                    //CommunicationProtocol.SendPacket(simCtl, Packet.RaiseBuildPlatformCommand((int)Math.Round(layers),command.laser));
                    //currentHeight += layerHeight*(int)Math.Round(layers);
                    //Console.WriteLine(layers);
                }
                else if (command.z < currentHeight && command.z!=0)
                {
                    double layers = (command.z - currentHeight) / layerHeight * -1;
                    for (double i = 0.5; i < layers; i++)
                    {
                        CommunicationProtocol.SendPacket(simCtl, Packet.LowerBuildPlatformCommand(command.laser));
                        currentHeight -= layerHeight;
                    }
                    //Console.WriteLine(layers);
                }
                if (command.x != 0 || command.y != 0)
                {
                    CommunicationProtocol.SendPacket(simCtl, Packet.AimLaserCommand((command.x / (plateWidthX / 2)) * aimWidthX, (command.y / (plateWidthY / 2) * aimWidthY), command.laser));
                }
                //Console.WriteLine("Laser: " + command.laser);
                //CommunicationProtocol.SendPacket(simCtl, Packet.LaserOnOffCommand(command.laser));
            }
            CommunicationProtocol.SendPacket(simCtl, Packet.ToTopCommand(false));
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
                Console.WriteLine("R - Remove Object");
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

                        PrintFile(printer.GetPrinterSim(), fileName);
                        break;

                    case 'T': // Test menu
                        bool done = false;
                        Console.Clear();
                        while (!done)
                        {
                            Console.WriteLine("Welcome to the test menu!");
                            Console.WriteLine("U - Step Up");
                            Console.WriteLine("D - Step Down");
                            Console.WriteLine("R - Reset Platform");
                            Console.WriteLine("T - Platform to top");
                            Console.WriteLine("L - Laser On/Off");
                            Console.WriteLine("A - Aim laser test");
                            Console.WriteLine("V - Get firmware version.");
                            Console.WriteLine("O - Remove Object");
                            Console.WriteLine("B - Back");
                            char ch2 = Char.ToUpper(Console.ReadKey(true).KeyChar);
                            Console.Clear();
                            switch (ch2)
                            {
                                case 'U':
                                    Packet upPacket = Packet.RaiseBuildPlatformCommand(false);
                                    string raiseResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), upPacket);
                                    if (raiseResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");

                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's Matthew's fault.");

                                    }
                                    break;
                                case 'D':
                                    Packet downPacket = Packet.LowerBuildPlatformCommand(false);
                                    string lowerResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), downPacket);
                                    if (lowerResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's potentially Jermaine's fault.");
                                    }
                                    break;

                                case 'R':
                                    Packet resetPlatform = Packet.ResetBuildPlatformCommand(false);
                                    string resetPlatformResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), resetPlatform);
                                    if (resetPlatformResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's John's fault.");
                                    }
                                    break;

                                case 'T':
                                    Packet toTopPlatform = Packet.ToTopCommand(false);
                                    string toTopResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), toTopPlatform);
                                    if (toTopResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's Kerstan's fault.");
                                    }
                                    break;

                                case 'L':
                                    Packet laserOnOff = Packet.LaserOnOffCommand(false);
                                    string laserCommandResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), laserOnOff);
                                    if (laserCommandResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's never Jordan's fault.");
                                    }
                                    break;
                                case 'A':
                                    Console.Write("Please input x coordinate: ");
                                    string xcoord = Console.ReadLine();

                                    Console.Write("Please input y coordinate: ");
                                    string ycoord = Console.ReadLine();
                                    Packet resetplatform = Packet.ResetBuildPlatformCommand(false);
                                    string resetCommand = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), resetplatform);
                                    if (resetCommand == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's never Jordan's fault.");
                                    }
                                    Console.ReadLine();

                                    Packet aimLaser = Packet.AimLaserCommand(float.Parse(xcoord), float.Parse(ycoord), true);
                                    string aimLaserResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), aimLaser);
                                    if (aimLaserResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's never Jordan's fault.");
                                    }
                                    break;

                                case 'V':
                                    Packet getVersion = Packet.GetFirmwareVersionCommand();
                                    string getVersionResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), getVersion);
                                    if (getVersionResponse.Contains("VERSION"))
                                    {
                                        Console.WriteLine("It worked! " + getVersionResponse);
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and Jordan is still the best.");
                                    }
                                    break;

                                case 'O':
                                    Packet objectRemove = Packet.RemoveObject(false);
                                    string objectRemoveResponse = CommunicationProtocol.SendPacket(printer.GetPrinterSim(), objectRemove);
                                    if (objectRemoveResponse == "SUCCESS")
                                    {
                                        Console.WriteLine("It worked!");
                                    }
                                    else
                                    {
                                        Console.WriteLine("It did not work and it's Jermaine's fault.");
                                    }
                                    break;
                                case 'B':
                                    done = true;
                                    break;


                            }
                        }
                        break;

                    case 'R':
                        CommunicationProtocol.SendPacket(printer.GetPrinterSim(), Packet.RemoveObject(false));
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