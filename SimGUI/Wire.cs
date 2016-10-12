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

using System.Drawing;
/*
 * --- Wire placement logic ---
 * Breadboard click [if wire tool selected]: get MainWindow to start wire at location
 * Breadboard mousemove: update wire endpoint
 * Breadboard click: finish wire
 * Update wire colour
 */
namespace SimGUI
{
    public class Wire : Canvas
    {
        //Is wire horizontal or vertical
        public Orientation WireOrientation;
        //Length of wire in terms of hole spacing, negative length means up or left rather than down or right
        public int Length;
        //Colour of wire body
        public Color WireColour;
        //Absolute position of first point on wire
        public Point WirePosition;
        //Net that the wire is connected to
        public string NetName = "";
        //Is wire currently selected
        public bool IsSelected = false;
        //Is wire a temporary wire currently being placed
        public bool IsTemporary = false;

        private Circuit ParentCircuit;

        public Wire(Circuit parent, Orientation newOrientation,
            int newLength, Color newColour, Point origin)
        {
            ParentCircuit = parent;
            WireOrientation = newOrientation;
            Length = newLength;
            WireColour = newColour;
            WirePosition = origin;
            //Connections should be towards the front
            SetZIndex(this, 5);
            MouseDown += Wire_MouseDown;
            KeyDown += Wire_KeyDown;
            MouseMove += Wire_MouseMove;
        }



        //Wire_KeyDown must be public as it may be called if there is a MainWindow keypress that is not otherwise handled
        public void Wire_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    ParentCircuit.RemoveWire(this);
                    ParentCircuit.AddUndoAction(new DeleteAction(this, ParentCircuit));
                    break;
            }
        }

        private void Wire_MouseDown(object sender, MouseButtonEventArgs e)
        {
           /* string message = "Connected to: ";
            foreach (string net in getConnectedBusses())
            {
                message += "\r\n" + net;
            }
            MessageBox.Show(message);*/
            ParentCircuit.DeselectAll();
            if (e.ChangedButton == MouseButton.Left)
            {
                if (ParentCircuit.ParentWindow.SelectedTool == "SELECT")
                {
                    Select();
                }
                else if (ParentCircuit.ParentWindow.SelectedTool == "DELETE")
                {
                    ParentCircuit.RemoveWire(this);
                }
            }
            ParentCircuit.ParentWindow.UpdatePrompt();

        }
        private Color ActualColour;
        public void MakeTemporaryWire()
        {
            IsTemporary = true;
            IsHitTestVisible = false;
            ActualColour = WireColour;
            WireColour = Color.FromArgb(127, 100, 100, 100);
            Render();
        }

        public void MakePermanentWire()
        {
            IsTemporary = false;
            IsHitTestVisible = true;
            WireColour = ActualColour;

            Render();
        }

        public string[] GetConnectedBreadboardNets() {
            int startBbId = 0, endBbId = 0;
            Point startPtOnBb, endPtOnBb;
            Point endPoint = WirePosition;
            if (WireOrientation == Orientation.Horizontal)
            {
                endPoint.X += Length * Constants.ScaleFactor;
            }
            else
            {
                endPoint.Y += Length * Constants.ScaleFactor;
            }
            startPtOnBb = Breadboard.GetPositionOnBreadboard(WirePosition, ref startBbId);
            endPtOnBb = Breadboard.GetPositionOnBreadboard(endPoint, ref endBbId);
            return new String[] {Breadboard.GetNetAtPoint(startPtOnBb, startBbId.ToString()),
                                 Breadboard.GetNetAtPoint(endPtOnBb, endBbId.ToString())};
        }

        public void Select()
        {
            ParentCircuit.DeselectAll();
            IsSelected = true;
            Opacity = 0.5;
        }

        public void Deselect()
        {
            IsSelected = false;
            Opacity = 1;
        }


        public void Render()
        {
            Children.Clear();
            Canvas.SetLeft(this, WirePosition.X);
            Canvas.SetTop(this, WirePosition.Y);

            int renderLength = Math.Abs(Length);
            //Wire starting tip
            Path startOfWire = new Path();
            startOfWire.StrokeThickness = 0.02;
            startOfWire.Stroke = Brushes.Gray;
            startOfWire.Fill = Brushes.Gray;
            if (WireOrientation == Orientation.Horizontal)
                startOfWire.Data = new RectangleGeometry(new Rect(-0.2, -0.3, 0.6, 0.4));
            else
                startOfWire.Data = new RectangleGeometry(new Rect(-0.3, -0.2, 0.4, 0.6));
            startOfWire.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
            Children.Add(startOfWire);
            if (renderLength > 0)
            {
                //Main body of wire
                Path wireBody = new Path();
                wireBody.StrokeThickness = 0.02;
                wireBody.Stroke = new SolidColorBrush(WireColour);
                wireBody.Fill = new SolidColorBrush(WireColour);
                if (WireOrientation == Orientation.Horizontal)
                    wireBody.Data = new RectangleGeometry(new Rect(0.4, -0.3, renderLength - 0.8, 0.4));
                else
                    wireBody.Data = new RectangleGeometry(new Rect(-0.3, 0.4, 0.4, renderLength - 0.8));
                wireBody.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                Children.Add(wireBody);

                //Wire ending tip
                Path endOfWire = new Path();
                endOfWire.Stroke = Brushes.Gray;
                endOfWire.StrokeThickness = 0.02;
                endOfWire.Fill = Brushes.Gray;
                if (WireOrientation == Orientation.Horizontal)
                    endOfWire.Data = new RectangleGeometry(new Rect(renderLength - 0.4, -0.3, 0.6, 0.4));
                else
                    endOfWire.Data = new RectangleGeometry(new Rect(-0.3, renderLength - 0.4, 0.4, 0.6));
                endOfWire.RenderTransform = new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor);
                Children.Add(endOfWire);
            }
            //If wire goes in opposite direction, rotate by 180deg
            if (Length < 0)
            {
                if (WireOrientation == Orientation.Horizontal)
                {
                    RenderTransform = new ScaleTransform(-1, 1);
                }
                else
                {
                    RenderTransform = new ScaleTransform(1, -1);
                }
            }
            else
            {
                RenderTransform = new ScaleTransform(1,1);
            }
        }

        void Wire_MouseMove(object sender, MouseEventArgs e)
        {
            //The below statements handle the case that the mouse is moved over a wire
            //while a wire or leaded component is beign placed
            if (Breadboard.StartedWire)
            {
                Point actualCoord = e.GetPosition(ParentCircuit.ParentWindow.DrawArea);
                Orientation o = Util.getBestOrientation(Breadboard.WirePointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - Breadboard.WirePointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - Breadboard.WirePointA.Y) / Constants.ScaleFactor);
                }
                Breadboard.NewWire.Length = length;
                Breadboard.NewWire.WireOrientation = o;
                Breadboard.NewWire.Render();
            }
            else if (Breadboard.StartedLeadedComponent)
            {
                Point actualCoord = e.GetPosition(ParentCircuit.ParentWindow.DrawArea);
                Orientation o = Util.getBestOrientation(Breadboard.ComponentPointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - Breadboard.ComponentPointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - Breadboard.ComponentPointA.Y) / Constants.ScaleFactor);
                }

                Breadboard.NewLeadedComponent.Length = length;
                Breadboard.NewLeadedComponent.orientation = o;
                Breadboard.NewLeadedComponent.Render();

            }
        }

        //This function generates a set of key-value pairs that represent the wire's parameters.
        public virtual Dictionary<string, string> SaveParameters()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["startX"] = WirePosition.X.ToString();
            parameters["startY"] = WirePosition.Y.ToString();
            parameters["length"] = Length.ToString();
            parameters["orientation"] = (WireOrientation == Orientation.Horizontal) ? "horiz" : "vert";
            parameters["colour"] = WireColour.ToString();
            return parameters;
        }

        //Creates a new wire object from the set of parameters generated by SaveParameters()
        public static Wire CreateFromParameters(Circuit parent, Dictionary<string, string> parameters)
        {
            Point origin = new Point(double.Parse(parameters["startX"]), double.Parse(parameters["startY"]));
            Orientation orientation = (parameters["orientation"] == "horiz") ? Orientation.Horizontal : Orientation.Vertical;
            int length = int.Parse(parameters["length"]);
            Color colour = (Color)ColorConverter.ConvertFromString(parameters["colour"]);
            Wire newWire = new Wire(parent, orientation, length, colour, origin);
            newWire.Render();
            return newWire;
        }

        //Loads the entire wire state from a set of parameters. Used for undo/redo functionality
        public void ResetFromParameters(Dictionary<string, string> parameters)
        {
            Point origin = new Point(double.Parse(parameters["startX"]), double.Parse(parameters["startY"]));
            WireOrientation = (parameters["orientation"] == "horiz") ? Orientation.Horizontal : Orientation.Vertical;
            Length = int.Parse(parameters["length"]);
            WireColour = (Color)ColorConverter.ConvertFromString(parameters["colour"]);
            Render();
        }
    }
}
