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
    class LED : Component
    {
        private Color LEDColour;
        public LED(Circuit parent, Point origin, string model)
            : base(parent, origin)
        {
            ComponentType = "LED";
            ID = parent.GetNextComponentName("D");
            ModelFile = "res/models/leds.xml";
            Dictionary<string, string> meta = LoadModel(model);
            LEDColour = (Color)ColorConverter.ConvertFromString(meta["colour"]);
            SetBrightness(0);
        }

        //Calculates the value of a colour channel based on brightness
        private static byte GetColourChannelValue(byte maxValue, double bright)
        {
            int value = (int)(maxValue * 0.2 + maxValue * bright * 0.8);
            return (byte)Math.Max(Math.Min(value, 255), 0);
        }

        //Sets displayed brightness of LED, value should be between 0 (off) and 1 (on)
        private void SetBrightness(double bright)
        {
            Color ActualColor = Color.FromRgb(GetColourChannelValue(LEDColour.R, bright), GetColourChannelValue(LEDColour.G, bright), GetColourChannelValue(LEDColour.B, bright));
            foreach (Path p in Children.OfType<Path>())
            {
                if (p.Name == "LEDBody")
                {
                    p.Fill = new SolidColorBrush(ActualColor);
                    p.Stroke = new SolidColorBrush(ActualColor);
                }
            }
        }

        protected override void AfterPropertiesDialog(ComponentProperties dialog)
        {
            base.AfterPropertiesDialog(dialog);
            Dictionary<string, string> meta = LoadModel(ComponentModel);
            LEDColour = (Color)ColorConverter.ConvertFromString(meta["colour"]);
            SetBrightness(0);
        }

        public override void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            base.UpdateFromSimulation(numberOfUpdates, sim, eventType);
            if (eventType == SimulationEvent.TICK)
            {
                int varID = sim.GetComponentPinCurrentVarId(ID, 1);
                if (varID != -1)
                {
                    double LEDCurrent = sim.GetValueOfVar(varID, 0);

                    if (LEDCurrent > 0)
                    {
                        SetBrightness(Math.Min(LEDCurrent / 0.01, 1.0));
                    }
                    else
                    {
                        SetBrightness(0);
                    }
                }
                else
                {
                    SetBrightness(0);
                }
            }
            else if (eventType == SimulationEvent.STOPPED)
            {
                SetBrightness(0);
            }

        }
    }
}
