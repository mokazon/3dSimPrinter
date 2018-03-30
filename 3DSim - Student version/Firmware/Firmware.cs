using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hardware;
namespace Firmware
{

    public class FirmwareController
    {
        PrinterControl printer;
        string VersionNumber = "1.0";
        bool fDone = false;
        bool fInitialized = false;

        public FirmwareController(PrinterControl printer)
        {
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
                printer.WriteSerialToHost(header, 4);
                //Console.WriteLine("Firmware - Waiting for ACK");
                byte[] ack = CommunicationProtocol.ReadBlocking(printer, 1);
                if (ack[0] == 0xA5)
                {
                    //Console.WriteLine("Firmware - ACKed");
                    //Console.WriteLine("Firmware - Waiting for data");
                    byte[] data = CommunicationProtocol.ReadWait(printer,dataLength,1000);

                    if (data.Length == 0)
                    {
                        byte[] result = Encoding.ASCII.GetBytes("TIMEOUT");
                        //Console.WriteLine("Firmware - Sending Timeout");
                        printer.WriteSerialToHost(result, result.Length);
                        //Console.WriteLine("Firmware - Sent Timeout");
                        continue;
                    }
                    //Console.WriteLine("Firmware - Got data : "+data[0]);
                    //foreach(byte x in data)
                    //{
                    //    Console.WriteLine("F: "+x);
                    //}
                    Packet p = new Packet(header[0], data);
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
                        byte[] result = Encoding.ASCII.GetBytes("CHECKSUM");
                        //Console.WriteLine("Firmware - Sending CHECKSUM");
                        printer.WriteSerialToHost(result, result.Length);
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
                SetLaser(BitConverter.ToBoolean(Data, 0));
            }
            else if(CmdByte == (byte) CommunicationCommand.ResetBuildPlatform)
            {
                ResetZRail();
            }
            else if(CmdByte == (byte)CommunicationCommand.RaiseBuildPlatform)
            {
                RaiseZRail();
            }
            else if(CmdByte == (byte)CommunicationCommand.LowerBuildPlatform)
            {
                LowerZRail();
            }
            else if(CmdByte == (byte)CommunicationCommand.ToTop)
            {
                ZRailToTop();
            }
            else if(CmdByte == (byte)CommunicationCommand.AimLaser)
            {
                PointLaser(BitConverter.ToSingle(Data, 0), BitConverter.ToSingle(Data, 4));
            }
            else if(CmdByte == (byte)CommunicationCommand.GetFirmwareVersion)
            {
                return Encoding.ASCII.GetBytes("VERSION "+VersionNumber);
            }
            return Encoding.ASCII.GetBytes("SUCCESS");
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

        public void ResetZRail()
        {
            while (!printer.LimitSwitchPressed())
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
            }
            for (int i = 0; i < 39800; i++)
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_DOWN);
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
                printer.StepStepper(PrinterControl.StepperDir.STEP_DOWN);
            }
        }

        public void ZRailToTop()
        {
            while(!printer.LimitSwitchPressed())
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
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
