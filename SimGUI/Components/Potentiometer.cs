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

using System.Diagnostics;

namespace SimGUI
{
    public class Potentiometer : VariableComponent
    {
        public Potentiometer(Circuit parent, Point origin)
            : base(parent, origin)
        {
            ComponentType = "Potentiometer";
            LoadFootprintFromXml("potentiometer");
            ComponentValue = new Quantity("res", "Resistance", "Ω");
            ComponentValue.AllowNegative = false;
            ComponentValue.AllowZero = false;
            ID = parent.GetNextComponentName("VR");
            PostInit();
        }

        public override string GenerateNetlist()
        {        
            //Lower half of pot. Resistance is limited to >=0.001 to prevent a divide by zero error in the simulator
            string netlist = "RES " + ID + ".1 " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + Math.Max(ComponentValue.Val * SetPoint, 0.00001) + "\r\n"; 
            //Upper half of pot. 
            netlist += "RES " + ID + ".2 " + ConnectedNets[2] + " " + ConnectedNets[3] + " res=" + Math.Max(ComponentValue.Val * (1.0 - SetPoint), 0.00001) + "\r\n";         

            return netlist;
        }

        protected override void UpdateSetpoint(double newValue)
        {
            base.UpdateSetpoint(newValue);
            foreach (Path p in Children.OfType<Path>())
            {
                if (p.Name == "pointer")
                {
                    p.RenderTransform = new RotateTransform(newValue * 150 - 75,0,1.5);
                }
            }
            ToolTip = (SetPoint * 100).ToString("F1") + "%";
            AdjustmentPanel.ToolTip = (SetPoint * 100).ToString("F1") + "%";
            ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".1" + " res=" + Math.Max(ComponentValue.Val * SetPoint, ComponentValue.Val * 0.0000001));
            ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + ".2" + " res=" + Math.Max(ComponentValue.Val * (1.0 - SetPoint), ComponentValue.Val * 0.00000001));

        }

        public override void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            base.UpdateFromSimulation(numberOfUpdates, sim, eventType);
            if (eventType == SimulationEvent.STARTED)
            {
                int pin1Var = sim.GetComponentPinCurrentVarId(ID + ".1", 1);
                if (pin1Var != -1)
                    ConnectedPinVariables[1].Add(pin1Var);

                int pin2VarA = sim.GetComponentPinCurrentVarId(ID + ".1", 2);
                if (pin2VarA != -1)
                    ConnectedPinVariables[2].Add(pin2VarA);

                int pin2VarB = sim.GetComponentPinCurrentVarId(ID + ".2", 1);
                if (pin2VarB != -1)
                    ConnectedPinVariables[2].Add(pin2VarB);

                int pin3Var = sim.GetComponentPinCurrentVarId(ID + ".2", 2);
                if (pin3Var != -1)
                    ConnectedPinVariables[3].Add(pin3Var);

            }
        }
    }
}
