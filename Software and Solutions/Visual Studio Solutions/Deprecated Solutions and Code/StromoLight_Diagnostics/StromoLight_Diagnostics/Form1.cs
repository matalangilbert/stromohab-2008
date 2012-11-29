﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Stromohab_MCE;
using Stromohab_MCE_Connection;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZedGraph;
//using TreadMillController;
using System.Media;
using System.Messaging;
using StromoLight_Diagnostics;
using StromoLight_RemoteCalibration;
using System.Runtime.Remoting;
//using Microsoft.DirectX.DirectSound;
using System.IO;


namespace Stromohab_Diagnostics
{
    public partial class frmUI : Form
    {
        IPAddress ipAddress;
        int portNumber;
        ZedGraphPlotForm plotFeet;
        
        //AsymmetryAnalysis asymmetryAnalyser;1
        //delegate void displayDataCallback();2
        //Sonifier feetSound;3
        TightropeData dataCollection;
        Calibration markerHeightCal;
        StreamWriter file;

        //private delegate void DisplayDataCallback(StrideDiagnostics diagnosticData);

        public frmUI()
        {
            InitializeComponent();

            this.Text = "StromoLight Diagnostics";
            this.Size = new Size(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width, this.Height);


            //asymmetryAnalyser = new AsymmetryAnalysis();4
            //AsymmetryAnalysis.DataAvailable +=
                //new AsymmetryAnalysis.AsymmetryEventHandler(Asymmetry_DataAvailable);5
            //feetSound = new Sonifier();6
            //feetSound.Enabled = false;7
            //asymmetryAnalyser.Enabled = false;8

            SendStartupNotification();
            markerHeightCal = (Calibration)RemotingServices.Connect(typeof(Calibration), "tcp://144.32.137.126:8003/Calibration");
            
            //m_calibration = (Calibration)RemotingServices.Connect(typeof(Calibration),"tcp://144.32.137.126:8003/Calibration");
        }

        private void SendStartupNotification()
        {
            MessageQueue startupQueue = null;
            string startupQueueName = @".\Private$\StromoLight_Startup";

            if (MessageQueue.Exists(startupQueueName))
            {
                startupQueue = new System.Messaging.MessageQueue(@".\Private$\StromoLight_Startup");
                System.Messaging.Message message = new System.Messaging.Message();
                message.Body = "DSOK";
                message.Label = "Message from Diagnostics";
                startupQueue.Send(message);
            }
            else
            {
                startupQueue = MessageQueue.Create(@".\Private$\StromoLight_Startup");

                startupQueue = new System.Messaging.MessageQueue(@".\Private$\StromoLight_Startup");

                System.Messaging.Message message = new System.Messaging.Message();
                message.Body = "DSOK Started OK";
                message.Label = "Message from Diagnostics";
                startupQueue.Send(message);
            }
        }
        
        private void Asymmetry_DataAvailable(StrideDiagnostics diagnosticData)
        {
            //dataDoubleSupport.Invoke(new DisplayDataCallback(this.displayData), new object[]{diagnosticData});
        }

        private void displayData(StrideDiagnostics diagnosticData)
        {/*
            dataDoubleSupport.Text = String.Format("{0:0.00}", diagnosticData.Double_Support_SI);
            dataCycleDuration.Text = String.Format("{0:0.00}", diagnosticData.Cycle_Duration_SI);
            dataStepInterval.Text = String.Format("{0:0.00}", diagnosticData.Step_Interval_SI);
            dataSwingStance.Text = String.Format("{0:0.00}", diagnosticData.Swing_Stance_SI);
            dataStrideDuration.Text = String.Format("{0:0.00}", diagnosticData.Stride_Period);
            dataStrideLength.Text = String.Format("{0:0.00}", diagnosticData.Stride_Length);
        */}
        



        private void ConnectAndStartSession()
        {
            ipAddress = IPAddress.Parse("144.32.137.126");
            portNumber = int.Parse("8001");
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, portNumber);
            try
            {
                TCPProcessor.ConnectToServer(ipEndPoint);
            }
            catch
            {
                MessageBox.Show("Connection to server failed");
            }

            if (ipAddress != null)
            {
                SendMCECommand.StartCameras();
            }
            else
            {
                MessageBox.Show("Connection to server failed");
            }

            btnStartTreadmill.Enabled = true;
            //nudElevation.Enabled = true;
            nudSpeed.Enabled = true;
            //btnDisengage.Enabled = true;
            btnReset.Enabled = true;
            btnEndSession.Enabled = true;

        }

        private void frmUI_Load(object sender, EventArgs e)
        {
            btnStartTreadmill.Enabled = false;
            nudElevation.Enabled = false;
            nudSpeed.Enabled = false;
            btnDisengage.Enabled = false;
            btnReset.Enabled = false;
            btnEndSession.Enabled = false;
            btnStartData.Enabled = false;
            btnStopData.Enabled = false;
            chkLink.Checked = true;
            timer1.Enabled = false;

            ConnectAndStartSession();
        }

        private void btnStartTreadmill_Click(object sender, EventArgs e)
        {
            StartAll();
        }

        private void StartAll()
        {
            if (btnStartTreadmill.Text == "Start Treadmill")
            {
                if (plotFeet != null) plotFeet.Dispose();
                //SendMCECommand.Calibrate();
                //SendMCECommand.SetTreadmillSpeed((float)nudSpeed.Value);
                //feetSound.Enabled = true;
                btnStartTreadmill.Text = "Stop Treadmill";
                if (chkLink.Checked == true)
                {
                    if (chkDelayStart.Checked == true)
                    {
                        timer1.Enabled = true;
                    }
                    else
                    {
                        StartData();
                    }
                }
            }
            else
            {
                SendMCECommand.SetTreadmillSpeed(0.0F);
                btnStartTreadmill.Text = "Start Treadmill";
                StopData();
                timer1.Enabled = false;
                //asymmetryAnalyser.Enabled = false;
                //feetSound.Enabled = false;
            }
        }

        private void StartData()
        {
            SendMCECommand.Calibrate();
            file = new StreamWriter(@"D:/Test/tightrope.csv", false);
            dataCollection = new TightropeData(file, markerHeightCal);
            SendMCECommand.SetTreadmillSpeed((float)nudSpeed.Value);
            //feetSound.Enabled = true;
            plotFeet = new ZedGraphPlotForm();
            plotFeet.ContinuePlotting = true;
            //asymmetryAnalyser.Enabled = true;

        }

        private void StopData()
        {
            if (plotFeet != null)
                plotFeet.ContinuePlotting = false;
            file.Close();
            //asymmetryAnalyser.Enabled = false;
        }

        private void btnEndSession_Click(object sender, EventArgs e)
        {
            //disconnect from server, close serial port disable buttons etc
            if (plotFeet != null)
                plotFeet.Dispose();
            
        }

        private void nudSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (btnStartTreadmill.Text == "Stop Treadmill")
            {
                SendMCECommand.SetTreadmillSpeed((float)nudSpeed.Value);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            SendMCECommand.StopTreadmill();
            nudSpeed.Value = 0;
            nudElevation.Value = 0;
        }

        private void nudElevation_ValueChanged(object sender, EventArgs e)
        {
            
        }

        private void btnDisengage_Click(object sender, EventArgs e)
        {

        }

        private void chkLink_CheckedChanged(object sender, EventArgs e)
        {
            if (chkLink.Checked == true)
            {
                btnStartData.Enabled = false;
                btnStopData.Enabled = false;
                chkDelayStart.Enabled = true;
            }
            else
            {
                btnStartData.Enabled = true;
                btnStopData.Enabled = true;
                chkDelayStart.Enabled = false;
            }
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            //StartData();
        }

        private void frmUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            SendMCECommand.StopTreadmill();
            if (plotFeet != null)
                plotFeet.Dispose();
            Application.Exit();
        }

        

        private void btnStopData_Click(object sender, EventArgs e)
        {
            //StopData();
        }
        /*
        private void cbLeftToe_CheckedChanged(object sender, EventArgs e)
        {
            feetSound.LeftToe = cbLeftToe.Checked;
            feetSound.LeftToe_Sound = "droplet.wav";
        }

        private void cbLeftHeel_CheckedChanged(object sender, EventArgs e)
        {
            feetSound.LeftHeel = cbLeftHeel.Checked;
        }

        private void cbRightToe_CheckedChanged(object sender, EventArgs e)
        {
            feetSound.RightToe = cbRightToe.Checked;          
        }

        private void cbRightHeel_CheckedChanged(object sender, EventArgs e)
        {
            feetSound.RightHeel = cbRightHeel.Checked;
        }

        private void cbSonify_CheckedChanged(object sender, EventArgs e)
        {
            feetSound.Enabled = cbSonify.Checked;
            if (cbSonify.Checked)
            {
                cbSonify.Font = new Font(cbSonify.Font, FontStyle.Bold);
            }
            else
            {
                cbSonify.Font = new Font(cbSonify.Font, FontStyle.Regular);
            }
        }

        private void frmUI_MouseClick(object sender, MouseEventArgs e)
        {
            StartAll();
        }

        public double SwingStance
        {
            set { dataSwingStance.Text = String.Format("{0:0.00}", value); }
        }

        public double CycleDuration
        {
            set { dataCycleDuration.Text = String.Format("{0:0.00}",value); }
        }

        public double StepInterval
        {
            set { dataStepInterval.Text = String.Format("{0:0.00}",value); }
        }

        public double DoubleSupport
        {
            set { dataDoubleSupport.Text = String.Format("{0:0.00}",value); }
        }

        public double StrideLength
        {
            set { dataStrideLength.Text = String.Format("{0:0.00}",value); }
        }

        public double StrideDuration
        {
            set { dataStrideDuration.Text = String.Format("{0:00}", value); }
        }*/

        private void btnCalibrateGroundPlane_Click(object sender, EventArgs e)
        {
            //m_calibration.Store(Marker_Bin.LeftFoot,Marker_Bin.RightFoot);
            
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;
            StartData();
        }
    }
}
