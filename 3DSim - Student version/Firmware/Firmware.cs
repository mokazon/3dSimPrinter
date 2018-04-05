using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hardware;
using System.Diagnostics;

namespace Firmware
{

    public class FirmwareController
    {
        PrinterControl printer;
        string VersionNumber = "1.0";
        bool fDone = false;
        bool fInitialized = false;
        double plateVelocity = 0;
        double plateAccel = 0;
        double plateZ = 0;
        Stopwatch stopwatch = new Stopwatch();

        byte[] TimeoutBytes = Encoding.ASCII.GetBytes("TIMEOUT");
        byte[] ChecksumBytes = Encoding.ASCII.GetBytes("CHECKSUM");
        byte[] SuccessBytes = Encoding.ASCII.GetBytes("SUCCESS");
        byte[] VersionBytes;

        public FirmwareController(PrinterControl printer)
        {
            VersionBytes = Encoding.ASCII.GetBytes("VERSION " + VersionNumber);
            this.printer = printer;
        }

        // Handle incoming commands from the serial link
        void Process()
        {
            //CommunicationProtocol communicationProtocol = new CommunicationProtocol();
            // Todo - receive incoming commands from the serial link and act on those commands by calling the low-level hardwarwe APIs, etc.
            while (!fDone)
            {
                byte[] header = CommunicationProtocol.ReadBlocking(printer, 4);
                //Console.WriteLine("Firmware - Received header: " + header[0] + "," + header[1] + "," + header[2] + "," + header[3]);
                byte dataLength = header[1];
                //Console.WriteLine("Firmware - Sending header: " + header[0] + "," + header[1] + "," + header[2] + "," + header[3]);
                byte[] headerCopy = new byte[header.Length];
                header.CopyTo(headerCopy, 0);
                printer.WriteSerialToHost(headerCopy, 4);
                //Console.WriteLine("Firmware - Waiting for ACK");
                byte[] ack = CommunicationProtocol.ReadBlocking(printer, 1);
                if (ack[0] == 0xA5)
                {
                    //Console.WriteLine("Firmware - ACKed");
                    //Console.WriteLine("Firmware - Waiting for data");
                    byte[] data = CommunicationProtocol.ReadWait(printer,dataLength,1000);

                    if (data.Length == 0)
                    {
                        //byte[] result = Encoding.ASCII.GetBytes("TIMEOUT");
                        //Console.WriteLine("Firmware - Sending Timeout");
                        printer.WriteSerialToHost(TimeoutBytes, TimeoutBytes.Length);//(result, result.Length);
                        //Console.WriteLine("Firmware - Sent Timeout");
                        continue;
                    }
                    /*foreach(byte x in data)
                    {
                        Console.WriteLine("F: "+x);
                    }*/
                    Packet p = new Packet(header[0], data);
                    //Console.WriteLine(BitConverter.GetBytes(p.Checksum)[0] + "," + BitConverter.GetBytes(p.Checksum)[1] + "?="+header[2]+","+header[3]);
                    if (p.Checksum == (ushort)BitConverter.ToInt16(header, 2))
                    {
                        //Console.WriteLine("Firmware - Correct Checksum");
                        byte[] result = ProcessCommand(header[0], data);
                        //Console.WriteLine("Firmware - Sending Response");
                        printer.WriteSerialToHost(result, result.Length);
                        //Console.WriteLine("Firmware - Sent Response");
                    }
                    else
                    {
                        //Console.WriteLine("Firmware - Incorrect Checksum");
                        //byte[] result = Encoding.ASCII.GetBytes("CHECKSUM");
                        //Console.WriteLine("Firmware - Sending CHECKSUM");
                        printer.WriteSerialToHost(ChecksumBytes, ChecksumBytes.Length);//(result, result.Length);
                        //Console.WriteLine("Firmware - Sent CHECKSUM");
                    }
                }
                else
                {
                    //Console.WriteLine("Firmware - Not ACK:" + ack[0]);
                }
            }
        }

        public byte[] ProcessCommand(byte CmdByte, byte[] Data)
        {
            if(CmdByte == (byte) CommunicationCommand.Laser)
            {
                //Console.WriteLine("F: Laser");
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte) CommunicationCommand.ResetBuildPlatform)
            {
                //Console.WriteLine("F: Reset Build Platform");
                ResetZRail();
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte)CommunicationCommand.RaiseBuildPlatform)
            {
                //Console.WriteLine("F: Raise Build Platform");
                /*int layers = BitConverter.ToInt32(Data, 0);
                for (int i = 0; i < layers; i++)
                {
                    RaiseZRail();
                }
                SetLaser(BitConverter.ToBoolean(Data, 4));*/
                RaiseZRail();
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte)CommunicationCommand.LowerBuildPlatform)
            {
                //Console.WriteLine("F: Lower BuildPlatform");
                LowerZRail();
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte)CommunicationCommand.ToTop)
            {
                //Console.WriteLine("F: ToTop");
                ZRailToTop();
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte)CommunicationCommand.AimLaser)
            {
                //Console.WriteLine("F: AimLaser");
                PointLaser(BitConverter.ToSingle(Data, 0), BitConverter.ToSingle(Data, 4));
                SetLaser(BitConverter.ToBoolean(Data, 8));
            }
            else if(CmdByte == (byte)CommunicationCommand.GetFirmwareVersion)
            {
                //Console.WriteLine("F: GetFrimware");
                return VersionBytes;//Encoding.ASCII.GetBytes("VERSION "+VersionNumber);
            }
            else if(CmdByte == (byte)CommunicationCommand.RemoveObject)
            {
                //printer.RemoveModelFromPrinter();
            }
            else if(CmdByte == (byte)CommunicationCommand.ToBottom)
            {
                ZRailToBottom();
            }
            return SuccessBytes;//Encoding.ASCII.GetBytes("SUCCESS");
        }

        public void Start()
        {
            fInitialized = true;

            Process(); // this is a blocking call
        }

        public void Stop()
        {
            fDone = true;
        }

        public void WaitForInit()
        {
            while (!fInitialized)
                Thread.Sleep(500);
        }

        public void accelPlate(PrinterControl.StepperDir dir)
        {
            double deltaT = (double)stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;
            double stepHeight = 0.0025;
            //double deltaT = stopwatch.ElapsedMilliseconds;
            if (plateVelocity < 2 && plateVelocity > -2)
            {
                plateVelocity = 2.0;
            }
            double targetTime = (stepHeight) / plateVelocity;
            if (deltaT > targetTime)
            {
                stopwatch.Reset();
                stopwatch.Start();
                printer.StepStepper(dir);
                plateZ -= 1;

                plateVelocity += 4.0 * (deltaT);

                if (plateVelocity > 40.0)
                {
                    plateVelocity = 40.0;
                }

            }
        }

        public void ResetZRail()
        {
            Console.WriteLine("reset");
            stopwatch.Reset();
            stopwatch.Start();
            while (!printer.LimitSwitchPressed())
            {
                accelPlate(PrinterControl.StepperDir.STEP_UP);
            }
            Console.WriteLine("reset2");
            plateZ = 39800;
            stopwatch.Reset();
            stopwatch.Start();
            plateVelocity = 0;
            while (plateZ > 0)
            {
                accelPlate(PrinterControl.StepperDir.STEP_DOWN);
            }
        }

        public void RaiseZRail()
        {
            for (int i = 0; i < 200; i++)
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
            }
        }

        public void LowerZRail()
        {
            for (int i = 0; i < 200; i++)
            {
                bool b = printer.StepStepper(PrinterControl.StepperDir.STEP_DOWN);
            }
        }

        public void ZRailToTop()
        {
            stopwatch.Reset();
            stopwatch.Start();
            while (!printer.LimitSwitchPressed())
            {
                accelPlate(PrinterControl.StepperDir.STEP_UP);
            }
            Console.WriteLine("reset2");
            plateZ = 39800;
            /*while(!printer.LimitSwitchPressed())
            {
                bool b = printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
            }*/
        }

        public void ZRailToBottom()
        {
            stopwatch.Reset();
            stopwatch.Start();
            plateVelocity = 0;
            while (plateZ > 0)
            {
                accelPlate(PrinterControl.StepperDir.STEP_DOWN);
            }
        }

        public void PointLaser(float x, float y)
        {
            printer.MoveGalvos(x, y);
        }

        public void SetLaser(bool laserOn)
        {
            printer.SetLaser(laserOn);
        }
    }
}
