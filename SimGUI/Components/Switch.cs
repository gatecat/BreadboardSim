using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SimGUI
{
    class Switch : Component
    {
        private bool IsClosed = false;
        //Open and closed resistance must be finite 
        private const double RClosed = 1e-3;
        private const double ROpen = 1e12;

        public Switch(Circuit parent, Point origin, bool isSPDT) : base(parent, origin)
        {
            ID = parent.GetNextComponentName("SW");
            if (isSPDT)
            {
                ComponentType = "SPDT Switch";
                LoadFootprintFromXml("spdt_a");
            }
            else
            {
                ComponentType = "Push Switch";
                LoadFootprintFromXml("push_switch");
            }
        }

        protected void SetState(bool NewIsClosed)
        {
            IsClosed = NewIsClosed;

            if (ComponentType == "SPDT Switch")
            {
                if (IsClosed)
                {
                    LoadFootprintFromXml("spdt_b");
                    if (ParentCircuit.ParentWindow.CurrentSimulator.SimRunning)
                    {
                        ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".1 res=" + ROpen);
                        ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".2 res=" + RClosed);
                    }
                }
                else
                {
                    LoadFootprintFromXml("spdt_a");
                    if (ParentCircuit.ParentWindow.CurrentSimulator.SimRunning)
                    {
                        ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".1 res=" + RClosed);
                        ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".2 res=" + ROpen);
                    }
                }
            }
            else
            {
                if (IsClosed)
                {
                    ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".1 res=" + RClosed);
                }
                else
                {
                    ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".1 res=" + ROpen);
                }
                foreach (Path p in Children.OfType<Path>())
                {
                    if (p.Name == "button")
                    {
                        if (IsClosed)
                        {
                            p.Fill = Brushes.DarkBlue;
                        }
                        else
                        {
                            p.Fill = Brushes.DodgerBlue;
                        }
                    }
                }
            }


        }

        protected override void InteractiveClick(MouseButtonEventArgs e, bool isMouseDown)
        {
            if (ComponentType == "SPDT Switch")
            {
                if (isMouseDown)
                {
                    SetState(!IsClosed);
                }
            }
            else
            {
                if (isMouseDown)
                {
                    SetState(true);
                }
                else
                {
                    SetState(false);
                }
            }

        }

        public override string GenerateNetlist()
        {
            string netlist = "";
            if (ComponentType == "SPDT Switch")
            {
                if (IsClosed)
                {
                    netlist += "RES " + ID + ".1 " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + ROpen + "\r\n";
                    netlist += "RES " + ID + ".2 " + ConnectedNets[2] + " " + ConnectedNets[3] + " res=" + RClosed; 
                }
                else
                {
                    netlist += "RES " + ID + ".1 " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + RClosed + "\r\n";
                    netlist += "RES " + ID + ".2 " + ConnectedNets[2] + " " + ConnectedNets[3] + " res=" + ROpen;
                }
            }
            else
            {
                if (IsClosed)
                {
                    netlist += "RES " + ID + ".1 " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + RClosed + "\r\n";
                }
                else
                {
                    netlist += "RES " + ID + ".1 " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + ROpen + "\r\n";
                }

                //some pins are connected together internally
                netlist += "RES " + ID + ".2 " + ConnectedNets[1] + " " + ConnectedNets[3] + " res=" + RClosed + "\r\n";
                netlist += "RES " + ID + ".3 " + ConnectedNets[2] + " " + ConnectedNets[4] + " res=" + RClosed;

            }
            return netlist;
        }
        public override Dictionary<string, string> SaveParameters()
        {
            Dictionary<string, string> parameters = base.SaveParameters();
            parameters.Add("state", IsClosed.ToString());
            return parameters;
        }
        public override void LoadParameters(Dictionary<string, string> parameters)
        {
            base.LoadParameters(parameters);
            if (parameters.ContainsKey("state"))
                SetState(bool.Parse(parameters["state"]));
        }
    }
}
