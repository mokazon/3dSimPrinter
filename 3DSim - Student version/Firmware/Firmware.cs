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
