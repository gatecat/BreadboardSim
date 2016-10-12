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
    public class LeadedComponent : Component
    {
        public int Length = 0;
        public int MinLength = 3;
        public Orientation orientation;
        public bool IsTemporary = false;

        public LeadedComponent(Circuit parent, Point origin) : base(parent, origin)
        {

        }

        public virtual void Render()
        {
            Children.Clear();
            int renderLength = Math.Abs(Length);
            //Wire starting tip
           /* Path leads = new Path();
            leads.StrokeThickness = 0.02;
            leads.Stroke = Brushes.Gray;
            leads.Fill = Brushes.Gray;
            if (orientation == Orientation.Horizontal)
                leads.Data = new RectangleGeometry(new Rect(-0.2, -0.15, renderLength + 0.4, 0.3));
            else
                leads.Data = new RectangleGeometry(new Rect(-0.15, -0.2, 0.3, renderLength + 0.4));
            leads.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
            Canvas.SetZIndex(leads, -1); //Make leads appear behind component body
            Children.Add(leads);*/

            Path lead1 = new Path();
            lead1.Name = "pin1";
            lead1.StrokeThickness = 0.02;
            lead1.Stroke = Brushes.Gray;
            lead1.Fill = Brushes.Gray;
            if (orientation == Orientation.Horizontal)
                lead1.Data = new RectangleGeometry(new Rect(-0.2, -0.15, renderLength / 2.0 + 0.2, 0.3));
            else
                lead1.Data = new RectangleGeometry(new Rect(-0.15, -0.2, 0.3, renderLength / 2.0 + 0.2));
            lead1.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
            Canvas.SetZIndex(lead1, -1); //Make leads appear behind component body
            Children.Add(lead1);

            Path lead2 = new Path();
            lead2.Name = "pin2";
            lead2.StrokeThickness = 0.02;
            lead2.Stroke = Brushes.Gray;
            lead2.Fill = Brushes.Gray;
            if (orientation == Orientation.Horizontal)
                lead2.Data = new RectangleGeometry(new Rect(renderLength / 2.0, -0.15, renderLength / 2.0 + 0.2, 0.3));
            else
                lead2.Data = new RectangleGeometry(new Rect(-0.15, renderLength / 2.0, 0.3, renderLength / 2.0 + 0.2));
            lead2.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
            Canvas.SetZIndex(lead2, -1); //Make leads appear behind component body
            Children.Add(lead2);

            if (Length < 0)
            {
                RenderTransform = new RotateTransform(180);
            }
            else
            {
                RenderTransform = new RotateTransform(0);
            }
        }

        public void MakeTemporary()
        {
            IsTemporary = true;
            IsHitTestVisible = false;
            Opacity = 0.5;
            Render();
        }

        public void MakePermanent()
        {
            IsTemporary = false;
            IsHitTestVisible = true;
            Opacity = 1;
            Render();
        }
        public override Dictionary<string, string> SaveParameters()
        {
            //We also need to save component length
            Dictionary<string, string> parameters = base.SaveParameters();
            parameters["orientation"] = (orientation == Orientation.Horizontal) ? "horiz" : "vert";
            parameters["length"] = Length.ToString();
            return parameters;
        }
        public override void LoadParameters(Dictionary<string, string> parameters)
        {
            base.LoadParameters(parameters);
            orientation = (parameters["orientation"] == "horiz") ? Orientation.Horizontal : Orientation.Vertical;
            Length = int.Parse(parameters["length"]);
            Render();
        }
    }
}
