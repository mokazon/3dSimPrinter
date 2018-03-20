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
        string VersionNumber = "1";
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
                    byte[] data = CommunicationProtocol.ReadWait(printer,dataLength,1000);
                    if(data.Length == 0)
                    {
                        byte[] result = Encoding.ASCII.GetBytes("TIMEOUT");
                        printer.WriteSerialToHost(result, result.Length);
                        continue;
                    }
                    Packet p = new Packet(header[0], data);
                    if (p.Checksum == (ushort)BitConverter.ToInt16(header, 2))
                    {
                        ProcessCommand(header[0], data);
                        byte[] result = Encoding.ASCII.GetBytes("SUCCESS");
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

        public void ProcessCommand(byte CmdByte, byte[] Data)
        {
            if(CmdByte == (byte)Command.GetFirmwareVersion)
            {
                printer.WriteSerialToHost(Encoding.ASCII.GetBytes(VersionNumber), Encoding.ASCII.GetBytes(VersionNumber).Length);
            }
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

        public void RaiseZRail(int increments)
        {
            for (int i = 0; i < increments; i++)
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
            }
        }

        public void LowerZRail(int increments)
        {
            for (int i = 0; i < increments; i++)
            {
                printer.StepStepper(PrinterControl.StepperDir.STEP_UP);
            }
        }
    }
}
