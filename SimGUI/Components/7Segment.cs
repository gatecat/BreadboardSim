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
    class SevenSegment : Component
    {
        private Color LEDColour;
        private readonly Color OffSegmentColour = Colors.LightGray;
        public SevenSegment(Circuit parent, Point origin, string model)
            : base(parent, origin)
        {
            ComponentType = "7-Segment Display";
            ID = parent.GetNextComponentName("D");
            ModelFile = "res/models/7seg.xml";

            Dictionary<string, string> meta = LoadModel(model);
            LEDColour = (Color)ColorConverter.ConvertFromString(meta["colour"]);
            for (int i = 0; i < 7;i++ )
                SetBrightness(i, 0);
        }


        //Sets displayed brightness of LED, value should be between 0 (off) and 1 (on)
        public void SetBrightness(int segment, double bright)
        {
            Color ActualColour = new Color();
            //Alpha blend LED colour with background colour
            double alpha = (Math.Min(Math.Max(bright,0),1));
            ActualColour.A = 255;
            ActualColour.R = (byte)(LEDColour.R * alpha + OffSegmentColour.R * (1 - alpha));
            ActualColour.G = (byte)(LEDColour.G * alpha + OffSegmentColour.G * (1 - alpha));
            ActualColour.B = (byte)(LEDColour.B * alpha + OffSegmentColour.B * (1 - alpha));

            foreach (Path p in Children.OfType<Path>())
            {
                if (p.Name == ("segment" + (char)('A' + segment)))
                {
                    p.Fill = new SolidColorBrush(ActualColour);
                    p.Stroke = new SolidColorBrush(ActualColour);
                    Debug.WriteLine(ActualColour.ToString());
                }
            }
        }
        public override void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            base.UpdateFromSimulation(numberOfUpdates, sim, eventType);

            if (eventType == SimulationEvent.TICK)
            {
                for (int i = 0; i < 7; i++)
                {
                    int varID = sim.GetComponentPinCurrentVarId(ID + ((char)('a' + i)), 1);
                    if (varID != -1)
                    {
                        double LEDCurrent = sim.GetValueOfVar(varID, 0);

                        if (LEDCurrent > 0)
                        {
                            SetBrightness(i, Math.Min(LEDCurrent / 0.01, 1.0));
                        }
                        else
                        {
                            SetBrightness(i, 0);
                        }

                    }
                    else
                    {
                        SetBrightness(i, 0);
                    }

                }

            }
            else if(eventType == SimulationEvent.STOPPED)
            {
                for (int i = 0; i < 7; i++)
                {
                    SetBrightness(i, 0);
                }
            }
  
        }
    }
}
