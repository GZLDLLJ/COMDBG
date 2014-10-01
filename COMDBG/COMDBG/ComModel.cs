﻿/**
 
 * Copyright (c) 2014, Wenhuix, All rights reserved.

 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, 
 * this list of conditions and the following disclaimer.

 * Redistributions in binary form must reproduce the above copyright notice, 
 * this list of conditions and the following disclaimer in the documentation 
 * and/or other materials provided with the distribution.

 * Neither the name of COMDBG nor the names of its contributors may 
 * be used to endorse or promote products derived from this software without 
 * specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF 
 * THE POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.ComponentModel;
using System.Threading;

namespace COMDBG
{
    public delegate void SerialPortEventHandler(Object sender, SerialPortEventArgs e);

    public class SerialPortEventArgs : EventArgs
    {
        public bool isOpend = false;
        public String receivedString = "";
    }

    public class ComModel
    {
        private SerialPort sp = new SerialPort();

        public event SerialPortEventHandler comReceiveDataEvent = null;
        public event SerialPortEventHandler comOpenEvent = null;
        public event SerialPortEventHandler comCloseEvent = null;

        /// <summary>
        /// When serial received data, will call this method
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            while (sp.IsOpen && sp.BytesToRead > 0)
            {
                string str = "";
                try
                {
                    str = sp.ReadExisting();
                }
                catch (System.Exception)
                {
                	//catch read exception
                }
                SerialPortEventArgs args = new SerialPortEventArgs();
                args.receivedString = str;
                comReceiveDataEvent.Invoke(this, args);
            }
        }

        public void Send(Byte[] bytes)
        {
            if (sp.IsOpen)
            {
                sp.Write(bytes, 0, bytes.Length);
            }
        }

        /// <summary>
        /// Open Serial port
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <param name="dataBits"></param>
        /// <param name="stopBits"></param>
        /// <param name="parity"></param>
        public void Open(string portName, String baudRate,
            string dataBits, string stopBits, string parity)
        {
            if (sp.IsOpen)
            {
                sp.Close();
            }
            sp.PortName = portName;
            sp.BaudRate = Convert.ToInt32(baudRate);
            sp.DataBits = Convert.ToInt16(dataBits);
            sp.StopBits = (StopBits)Enum.Parse(typeof(StopBits), stopBits);
            sp.Parity = (Parity)Enum.Parse(typeof(Parity), parity);

            SerialPortEventArgs args = new SerialPortEventArgs();
            try
            {
                sp.Open();
                sp.DataReceived += new SerialDataReceivedEventHandler(DataReceived);
                args.isOpend = true;
            }
            catch (System.Exception)
            {
                args.isOpend = false;
            }
            comOpenEvent.Invoke(this, args);
        }

        //Take care to avoid deadlock when calling Close on the SerialPort 
        //in response to a GUI event.
        // An app involving the UI and the SerialPort freezes up when closing the SerialPort
        // Deadlock can occur if Control.Invoke() is used in serial port event handlers

        //The typical scenario we encounter is occasional deadlock in an app 
        //that has a data received handler trying to update the GUI at the 
        //same time the GUI thread is trying to close the SerialPort (for 
        //example, in response to the user clicking a Close button).

        //The reason deadlock happens is that Close() waits for events to 
        //finish executing before it closes the port. You can address this 
        //problem in your apps in two ways:

        //(1)In your event handlers, replace every Control.Invoke call with 
        //Control.BeginInvoke, which executes asynchronously and avoids 
        //the deadlock condition. This is commonly used for deadlock avoidance 
        //when working with GUIs.

        //(2)Call serialPort.Close() on a separate thread. You may prefer this
        //because this is less invasive than updating your Invoke calls.

        /// <summary>
        /// Close serial port
        /// </summary>
        public void Close()
        {
            Thread closeThread = new Thread(new ThreadStart(CloseSpThread));
            closeThread.Start();
        }

        private void CloseSpThread()
        {
            SerialPortEventArgs args = new SerialPortEventArgs();
            args.isOpend = false;
            try
            {
                sp.Close(); //close the serial port
            }
            catch (Exception)
            {
                args.isOpend = true;
            }

            comCloseEvent.Invoke(this, args);
        }

    }
}
