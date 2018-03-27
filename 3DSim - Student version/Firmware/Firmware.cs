﻿using System;
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
            // Todo - receive incoming commands from the serial link and act on those commands by calling the low-level hardwarwe APIs, etc.
            while (!fDone)
            {
                byte[] header = CommunicationProtocol.ReadBlocking(printer, 4);
                byte dataLength = header[1];
                printer.WriteSerialToHost(header, header.Length);
                byte[] ack = CommunicationProtocol.ReadBlocking(printer, 1);
                if (ack[0] == 0xA5)
                {
                    byte[] data = CommunicationProtocol.ReadWait(printer,dataLength,10000);
                    if(data.Length == 0)
                    {
                        byte[] result = Encoding.ASCII.GetBytes("TIMEOUT");
                        printer.WriteSerialToHost(result, result.Length);
                        continue;
                    }
                    Packet p = new Packet(header[0], data);
                    if (p.Checksum == (ushort)BitConverter.ToInt16(header, 2))
                    {
                        byte[] result = ProcessCommand(header[0], data);

                        printer.WriteSerialToHost(result, result.Length);
                    }
                    else
                    {
                        byte[] result = Encoding.ASCII.GetBytes("CHECKSUM");
                        printer.WriteSerialToHost(result, result.Length);
                    }
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
                Thread.Sleep(100);
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
