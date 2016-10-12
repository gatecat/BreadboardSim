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
    class Resistor : LeadedComponent
    {
        private const double BodyLength = 2;
        //Resistor colour code
        private static Dictionary<int, Color> BandColours = new Dictionary<int,Color>{
                                                                  {0, Colors.Black},
                                                                  {1, Colors.SaddleBrown},
                                                                  {2, Colors.Red},
                                                                  {3, Colors.DarkOrange},
                                                                  {4, Colors.Yellow},
                                                                  {5, Colors.Green},
                                                                  {6, Colors.DarkBlue},
                                                                  {7, Colors.DarkViolet},
                                                                  {8, Colors.Gray},
                                                                  {9, Colors.White},
                                                                  {-1, Colors.DarkGoldenrod},
                                                                  {-2, Colors.LightGray}
                                                              };
        private readonly double[] bandPositions = { 0.3, 0.7, 1.1, 1.7 };

        public Resistor(Circuit parent, Point origin) : base(parent, origin)
        {
            ComponentType = "Resistor";
            MinLength = 3;
            ID = parent.GetNextComponentName("R");
            ComponentValue = new Quantity("res","Resistance","Ω");
            ComponentValue.AllowZero = false;
            ComponentValue.AllowNegative = false;
        }
        public override void SetComponentValue(double newValue)
        {
            Render();
            base.SetComponentValue(newValue);
        }
        public override void SetComponentValue(Quantity newValue)
        {
            Render();
            base.SetComponentValue(newValue);
            ComponentValue.AllowZero = false;
            ComponentValue.AllowNegative = false;
        }
        public override void Render()
        {
            base.Render();
            /*renderLength must be a double as otherwise premature rounding when dividing by 2 will make the component
            body off-center when the length is odd*/
            double renderLength = Math.Abs(Length);

            if (renderLength >= MinLength)
            {
                Path resBody = new Path();
                resBody.StrokeThickness = 0.02;
                resBody.Stroke = Brushes.Wheat;
                resBody.Fill = Brushes.Wheat;
                if (orientation == Orientation.Horizontal)
                    resBody.Data = new RectangleGeometry(new Rect((renderLength / 2) - (BodyLength / 2), -0.4, BodyLength, 0.8));
                else
                    resBody.Data = new RectangleGeometry(new Rect(-0.4, (renderLength / 2) - (BodyLength / 2), 0.8, BodyLength));
                resBody.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                Children.Add(resBody);
                SetZIndex(resBody, 0); //Resistor body in front of leads

                Color[] bands = GetResistorColourBands(ComponentValue.Val);

                for (int i = 0; i < 4; i++)
                {
                    Path band = new Path();
                    band.StrokeThickness = 0.02;
                    band.Stroke = new SolidColorBrush(bands[i]);
                    band.Fill = new SolidColorBrush(bands[i]);
                    if (orientation == Orientation.Horizontal)
                        band.Data = new RectangleGeometry(new Rect((renderLength / 2) - (BodyLength / 2) + bandPositions[i], -0.4, 0.25, 0.8));
                    else
                        band.Data = new RectangleGeometry(new Rect(-0.4, (renderLength / 2) - (BodyLength / 2) + bandPositions[i], 0.8, 0.25));
                    band.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                    SetZIndex(band, 1); //Resistor bands in front of body
                    Children.Add(band);
                }
            }
            PinPositions[1] = new Point(0, 0);
            if (orientation == Orientation.Horizontal)
            {
                PinPositions[2] = new Point(Length, 0);
            }
            else
            {
                PinPositions[2] = new Point(0, Length);
            }

        }
       
        //Returns the colour bands for a resistor of a given value at 5% tolerance
        //Always uses the 3-band + tolerance code
        public static Color[] GetResistorColourBands(double value)
        {
            Color[] bands = new Color[4] {Colors.Transparent, Colors.Transparent, Colors.Transparent, BandColours[-1]};
            int multiplier = (int)Math.Floor(Math.Log10(value)) - 1;
            if (BandColours.ContainsKey(multiplier))
            {
                bands[2] = BandColours[multiplier];
            }

            int normalisedValue = (int)Math.Round(value /  Math.Pow(10,multiplier));
            int digit1 = (normalisedValue / 10) % 10;
            if (BandColours.ContainsKey(digit1))
            {
                bands[0] = BandColours[digit1];
            }
            int digit2 = (normalisedValue) % 10;
            if (BandColours.ContainsKey(digit2))
            {
                bands[1] = BandColours[digit2];
            }
            return bands;
        }

        //We want to update colour bands after the properties dialog is displayed
        protected override void AfterPropertiesDialog(ComponentProperties dialog)
        {
            base.AfterPropertiesDialog(dialog);
            Render();
        }

        public override string GenerateNetlist()
        {
            return "RES " + ID + " " + ConnectedNets[1] + " " + ConnectedNets[2] + " res=" + ComponentValue.Val.ToString();
        }

        //Set up pin current vars
        public override void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            base.UpdateFromSimulation(numberOfUpdates, sim, eventType);
            if (eventType == SimulationEvent.STARTED)
            {
                int pinVar1 = sim.GetComponentPinCurrentVarId(ID, 1);
                if (pinVar1 != 0)
                {
                    ConnectedPinVariables[1].Add(pinVar1);
                }
                int pinVar2 = sim.GetComponentPinCurrentVarId(ID, 2);
                if (pinVar2 != 0)
                {
                    ConnectedPinVariables[2].Add(pinVar2);
                }
            }
        }
    }
}
