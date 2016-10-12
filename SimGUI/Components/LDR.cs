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
    public class LDR : VariableComponent
    {
        //Resistance at 10 lux
        private const double R10lux = 10e3;
        //Resistance at 0.1 lux
        private const double R01lux = 1e6;

        //Lux at beginning of setpoint
        private const double LuxStart = 0.001;
        private const double LuxEnd = 500;

        /*
         * log R = klogL + c
         * R = A*L^k 
         * k = (log R1 - log R2) / (log L1 - log L2)
         * L1 = 10, L2 = 0.1
         * k = (log R1 - log R2) / 2
         * A = R1 / (L1^k)
         */

        //Current resistance
        private double CurrentResistance = 1000;

        public LDR(Circuit parent, Point origin)
            : base(parent, origin)
        {
            ComponentType = "LDR";
            LoadFootprintFromXml("LDR");
            ID = parent.GetNextComponentName("VR");
            Canvas.SetLeft(AdjustmentPanel, -PanelWidth / 2 + 0.5);
            Canvas.SetTop(AdjustmentPanel, 1.4);
            PostInit();
        }

        public override string GenerateNetlist()
        {
            //Lower half of pot. Resistance is limited to >=0.001 to prevent a divide by zero error in the simulator
            string netlist = "RES " + ID + " " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + CurrentResistance + "\r\n";
            return netlist;
        }

        private double GetResistanceFromLux(double lux)
        {
            double k = (Math.Log10(R10lux) - Math.Log10(R01lux)) / 2;
            double A = R10lux / (Math.Pow(10, k));
            return A * Math.Pow(lux, k);
        }


        protected override void UpdateSetpoint(double newValue)
        {
            base.UpdateSetpoint(newValue);
            double lux = LuxStart + SetPoint * (LuxEnd - LuxStart);
            ToolTip = lux.ToString("F1") + " lux";
            AdjustmentPanel.ToolTip = lux.ToString("F1") + " lux";
            CurrentResistance = GetResistanceFromLux(lux);
            ParentCircuit.ParentWindow.CurrentSimulator.SendChangeMessage("CHANGE " + ID + " res=" + CurrentResistance);

        }
        public override void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            base.UpdateFromSimulation(numberOfUpdates, sim, eventType);
            /*
              * To prevent updates from being sent so quickly that they break the simulator
              * when the slider is dragged, updates are queued and change messages actually sent here.
             */
            if (eventType == SimulationEvent.STARTED)
            {
                int pin1Var = sim.GetComponentPinCurrentVarId(ID, 1);
                if (pin1Var != -1)
                    ConnectedPinVariables[1].Add(pin1Var);

                int pin2Var = sim.GetComponentPinCurrentVarId(ID, 2);
                if (pin2Var != -1)
                    ConnectedPinVariables[2].Add(pin2Var);

            }
        }
    }
}
