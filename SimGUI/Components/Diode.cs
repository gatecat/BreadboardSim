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
    class Diode : LeadedComponent
    {
        private const double BodyLength = 1.5;
        private readonly Color BodyColour = Color.FromRgb(60, 60, 60);

        public Diode(Circuit parent, Point origin, string model)
            : base(parent, origin)
        {
            ComponentType = "Diode";
            MinLength = 3;
            ID = parent.GetNextComponentName("D");
            ModelFile = "res/models/diodes.xml";
            LoadModel(model);
            PinNames.Add(1, "Anode");
            PinNames.Add(2, "Cathode");
        }

        public override void Render()
        {
            base.Render();
            /*renderLength must be a double as otherwise premature rounding when dividing by 2 will make the component
            body off-center when the length is odd*/
            double renderLength = Math.Abs(Length);

            if (renderLength >= MinLength)
            {
                Path diodeBody = new Path();
                diodeBody.StrokeThickness = 0.02;
                diodeBody.Stroke = new SolidColorBrush(BodyColour);
                diodeBody.Fill = new SolidColorBrush(BodyColour);
                if (orientation == Orientation.Horizontal)
                    diodeBody.Data = new RectangleGeometry(new Rect((renderLength / 2) - (BodyLength / 2), -0.4, BodyLength, 0.8));
                else
                    diodeBody.Data = new RectangleGeometry(new Rect(-0.4, (renderLength / 2) - (BodyLength / 2), 0.8, BodyLength));
                diodeBody.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                Children.Add(diodeBody);
                SetZIndex(diodeBody, 0); //Diode body in front of leads

                Path polarityBand = new Path();
                polarityBand.StrokeThickness = 0.02;
                polarityBand.Stroke = Brushes.LightGray;
                polarityBand.Fill = Brushes.LightGray;
                if (orientation == Orientation.Horizontal)
                    polarityBand.Data = new RectangleGeometry(new Rect((renderLength / 2) - (BodyLength / 2) + 1, -0.4, 0.25, 0.8));
                else
                    polarityBand.Data = new RectangleGeometry(new Rect(-0.4, (renderLength / 2) - (BodyLength / 2) + 1, 0.8, 0.25));
                polarityBand.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                SetZIndex(polarityBand, 1); //Polarity band in front of body
                Children.Add(polarityBand);
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
    }
}
