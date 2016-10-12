using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using System.Threading.Tasks;

using System.IO;
using System.IO.Pipes;

using System.Diagnostics;
using System.Windows;
namespace SimGUI
{
    public class Simulator
    {
        public Process SimProcess;
        private Thread LineReaderThread;
        //Size of FIFO data point buffer
        public const double BufferSize = 50000;

        //Names of variables - ordered in same order as values
        public List<string> VariableNames;

        //Variable 0 is always time

        //Buffer of variable values
        public List<List<double>> Results;

        private object LineBufferLock = new object();
        private List<string> LineBuffer = new List<string>();


        public bool SimRunning = false;

        //Updates pause during errors
        private bool UpdatesPaused = false;

        //Whether or not variable names array is populated
        public bool VarNamesPopulated = false;

        public Simulator()
        {


        }

        void SimulatorProcess_Exited(object sender, EventArgs e)
        {
            SimRunning = false;
            try
            {
                LineReaderThread.Abort();
            }
            catch
            {
                //Nothing
            }
        }

        public void LineReader()
        {
            while (true)
            {
                string line = SimProcess.StandardOutput.ReadLine();
                lock (LineBufferLock)
                {
                    if (line != null)
                        LineBuffer.Add(line);
                }
            }
        }

        public void Start(string netlist, double speed = 1)
        {
            SimProcess = new Process();
            SimProcess.StartInfo.UseShellExecute = false;
            SimProcess.StartInfo.FileName = "res/simbe.exe";
            SimProcess.StartInfo.CreateNoWindow = true;
            SimProcess.StartInfo.RedirectStandardInput = true;
            SimProcess.StartInfo.RedirectStandardOutput = true;
 //           SimProcess.StartInfo.RedirectStandardError = true;

            Results = new List<List<double>>();
            LineBuffer = new List<string>();
            VariableNames = new List<string>();
            LineReaderThread = new Thread(new ThreadStart(LineReader));
            VarNamesPopulated = false;
            SimProcess.Exited += SimulatorProcess_Exited;

            SimProcess.StartInfo.Arguments = speed.ToString();
            SimProcess.Start();
            SimProcess.PriorityBoostEnabled = true;
            SimProcess.PriorityClass = ProcessPriorityClass.AboveNormal;
            SimProcess.StandardInput.WriteLine(netlist);
            SimProcess.StandardInput.WriteLine("START " + speed.ToString());

            SimRunning = true;
            UpdatesPaused = false;

            LineReaderThread.Start();

        }


        //Read latest data from simulator
        //Call regularly
        //Returns number of new lines
        public int Update()
        {
            int numberOfLines = 0;
            
            if (!UpdatesPaused)
            {
                lock (LineBufferLock)
                {
                    foreach (string line in LineBuffer)
                    {
                        List<double> values = new List<double>();

                        string[] splitLine = line.Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (splitLine.Length >= 2)
                        {
                            string[] splitData = splitLine[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (splitLine[0] == "RESULT")
                            {
                                for (int i = 0; i < splitData.Length; i++)
                                {
                                    double val = 0;
                                    if (Double.TryParse(splitData[i], out val))
                                    {
                                        values.Add(val);
                                    }
                                    else
                                    {
                                        //Typically means sim returned a NAN or Infinity
                                        UpdatesPaused = true;
                                        MessageBox.Show("The simulator returned invalid data during simulation. The simulation will now stop. This error is normally caused by an impossible circuit, for example " +
                                                        "short circuits or invalid component values.", "Simulation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        Stop();
                                        return 0;
                                    };
                                }
                                Results.Add(values);
                                numberOfLines++;
                                if (Results.Count > BufferSize)
                                {
                                    Results.Remove(Results[0]);
                                }
                            }
                            else if (splitLine[0] == "VARS")
                            {
                                VariableNames = new List<string>(splitData);
                                VarNamesPopulated = true;
                            }
                            else if (splitLine[0] == "ERROR")
                            {
                                bool recoverable = (int.Parse(splitData[0])==1);
                                UpdatesPaused = true;
                                if (recoverable)
                                {
                                    MessageBoxResult result = MessageBoxResult.No;
                                    if (splitData[1] == "CONVERGENCE")
                                    {
                                        result = MessageBox.Show("The simulator encountered a convergence failure during simulation.\r\n" + 
                                            "This is normally caused by an invalid or unstable circuit. You can continue the simulation, but the results may be inaccurate.\r\n" + 
                                            "Would you like to continue?\r\n" + 
                                            "Please note: this message will not be shown again during this simulation, even if further convergence failures occur.", "Simulation Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    }
                                    else
                                    {
                                        result = MessageBox.Show("The simulator encountered an error during simulation. You can continue the simulation, but values may be inaccurate. Would you like to continue?", "Simulation Error", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    }
                                    if (result == MessageBoxResult.Yes)
                                    {
                                        SimProcess.StandardInput.WriteLine("CONTINUE");
                                        UpdatesPaused = false;
                                    }
                                    else
                                    {
                                        Stop();
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("The simulator encountered a fatal error during simulation. The simulation will now stop. This error is normally caused by an impossible circuit, for example " +
                                        "short circuits or invalid component values.", "Simulation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    Stop();
                                }
                            }


                        }
                        }
                       
                    LineBuffer.Clear();
                }
            }
            
            return numberOfLines;
        }

        //Procedures for getting data during simulation
        //Use 0 for last tick, -n for n ticks before now
        public double GetCurrentTime(int tick = 0)
        {
            if ((Results.Count - 1 + tick) > 0)
                return Results[Results.Count - 1 + tick][0];
            else
                return 0;
        }

        //Return number of ticks in buffer
        public int GetNumberOfTicks()
        {
            return Results.Count;
        }

        public int GetNetVoltageVarId(string netName)
        {
            string varName = "V(" + netName + ")";
            if (VariableNames.Contains(varName))
                return VariableNames.IndexOf(varName);
            else
                return -1;
        }

        //Pin numbers start from 1
        //A return value of -1 means the variable does not exists
        public int GetComponentPinCurrentVarId(string componentName, int pin)
        {
            string varName = "I(" + componentName + "." + (pin - 1) + ")";
            if (VariableNames.Contains(varName))
                return VariableNames.IndexOf(varName);
            else
                return -1;
        }

        //Use 0 for last tick, -n for n ticks before now
        public double GetValueOfVar(int varId, int tick)
        {

            if (((Results.Count - 1 + tick) > 0) && (varId >= 0))
            {
                if (Results[Results.Count - 1 + tick].Count > varId)
                    return Results[Results.Count - 1 + tick][varId];
                else
                    return 0;
            }
            else
            {
                return 0;
            }
        }

        public void SendChangeMessage(string message)
        {
            if (SimProcess != null)
            {
                if (!SimProcess.HasExited)
                {
                    SimProcess.StandardInput.WriteLine(message);
                }
            }
        }

        public void Stop()
        {
            if (LineReaderThread != null)
                if (LineReaderThread.IsAlive)
                    LineReaderThread.Abort();
            if (SimRunning)
            {
                if (!SimProcess.HasExited)
                    SimProcess.Kill();
                SimRunning = false;
            }
        }

        ~Simulator()
        {
            try
            {
                if(SimProcess != null)
                    if(!SimProcess.HasExited)
                        SimProcess.Kill();
            }
            catch
            {
                //Nothing
            }

            try
            {
                if (LineReaderThread != null)
                    if(LineReaderThread.IsAlive)
                        LineReaderThread.Abort();
            }
            catch
            {
                //Nothing
            }


        }
    }
}
