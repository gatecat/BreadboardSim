using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public class Breadboard : Canvas
    {
        private static List<Point> BreadBoardHolePositions;
        private static Dictionary<Point, string> BreadBoardNets;
        public int Number = 0;
        private Circuit ParentCircuit;  
        public Breadboard(Circuit parent)
        {
            //Obtain list of valid hole positions 
            ParentCircuit = parent;
            SetZIndex(this, -10); //Breadboards are always the backmost element
            if (BreadBoardHolePositions == null)
            {
                BreadBoardHolePositions = new List<Point>();
                BreadBoardNets = new Dictionary<Point, string>();
                System.IO.StreamReader file = new System.IO.StreamReader("res/breadboard/breadboard-holes.csv");
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    try
                    {
                        string[] splitLine = line.Split(new char[] { ',' });
                        BreadBoardHolePositions.Add(new Point(
                            Double.Parse(splitLine[0]),
                            Double.Parse(splitLine[1])));
                        BreadBoardNets.Add(new Point(
                            Double.Parse(splitLine[0]),
                            Double.Parse(splitLine[1])),
                            splitLine[2].Trim());
                    }
                    catch
                    {

                    }
                }
                file.Close();
            }
            
            Image breadboardImage = new Image();
            breadboardImage.Source = new BitmapImage(new Uri("res/breadboard/breadboard.png",UriKind.Relative));
            breadboardImage.Width = /*743.5*/742;
            Children.Add(breadboardImage);
            MouseUp += Breadboard_MouseUp;
            MouseMove += Breadboard_MouseMove;
            StartedWire = false;
            StartedLeadedComponent = false;
        }


        public static bool StartedWire; //Is a wire currently being placed
        public static Wire NewWire; //The wire currently being placed
        public static Point WirePointA; //First point of new wire

        public static bool StartedLeadedComponent;
        public static LeadedComponent NewLeadedComponent;
        public static Point ComponentPointA;

        private void Breadboard_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.LeftButton == MouseButtonState.Pressed) && (ParentCircuit.ParentWindow.SelectedTool == "SELECT"))
            {
                ParentCircuit.HandleComponentDrag(sender, e);
            }
            if (StartedWire)
            {
                Point actualCoord = GetAbsolutePosition(GetHoleCoord(e.GetPosition(this)));
                Orientation o = Util.getBestOrientation(WirePointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - WirePointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - WirePointA.Y) / Constants.ScaleFactor);
                }
                NewWire.Length = length;
                NewWire.WireOrientation = o;
                NewWire.Render();
            }
            else if (StartedLeadedComponent)
            {
                Point actualCoord = GetAbsolutePosition(GetHoleCoord(e.GetPosition(this)));
                Orientation o = Util.getBestOrientation(ComponentPointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - ComponentPointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - ComponentPointA.Y) / Constants.ScaleFactor);
                }

                NewLeadedComponent.Length = length;
                NewLeadedComponent.orientation = o;
                NewLeadedComponent.Render();

            }
        }

        private void Breadboard_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ParentCircuit.DeselectAll();
            if (ParentCircuit.ParentWindow.SelectedTool == "WIRE")
            {
                if (StartedWire)
                {
                    Point actualCoord = GetHoleCoord(e.GetPosition(this), true);
                    if (BreadBoardHolePositions.Contains(actualCoord))
                    {
                        actualCoord = GetAbsolutePosition(actualCoord);
                        Orientation o = Util.getBestOrientation(WirePointA, actualCoord);
                        int length = 0;
                        if (o == Orientation.Horizontal)
                        {
                            length = (int)((actualCoord.X - WirePointA.X) / Constants.ScaleFactor);
                        }
                        else
                        {
                            length = (int)((actualCoord.Y - WirePointA.Y) / Constants.ScaleFactor);
                        }
                        NewWire.Length = length;
                        NewWire.WireOrientation = o;
                        NewWire.MakePermanentWire();
                        StartedWire = false;
                        ParentCircuit.UpdateWireColours();
                        ParentCircuit.AddUndoAction(new AddAction(NewWire, ParentCircuit));
                    }
                }
                else
                {
                    Point rawCoord = GetHoleCoord(e.GetPosition(this), false);

                    Point actualCoord = new Point(0, 0);
                    bool isValid = false;

                    //We only want to start a new wire if the user clicks on or near a hole
                    foreach (Point p in BreadBoardHolePositions)
                    {
                        double distance = Math.Sqrt(Math.Pow((p.X - rawCoord.X), 2) + Math.Pow((p.Y - rawCoord.Y), 2));
                        if (distance < 0.4)
                        {
                            isValid = true;
                            actualCoord = p;
                            break;
                        }
                    }
                    if (isValid)
                    {
                        WirePointA = GetAbsolutePosition(actualCoord);
                        StartedWire = true;
                        NewWire = new Wire(ParentCircuit, Orientation.Horizontal, 0, 
                            Constants.RandomWireColours[new Random().Next(Constants.RandomWireColours.Length)], 
                            new Point(WirePointA.X + 1, WirePointA.Y + 1));
                        NewWire.MakeTemporaryWire();
                        ParentCircuit.AddWire(NewWire);
                    }
                }
            }
            else if (ParentCircuit.ParentWindow.SelectedTool == "COMPONENT")
            {
                if (StartedLeadedComponent)
                {
                    Point actualCoord = GetHoleCoord(e.GetPosition(this), true);
                    if (BreadBoardHolePositions.Contains(actualCoord))
                    {
                        actualCoord = GetAbsolutePosition(actualCoord);
                        Orientation o = Util.getBestOrientation(ComponentPointA, actualCoord);
                        int length = 0;
                        if (o == Orientation.Horizontal)
                        {
                            length = (int)((actualCoord.X - ComponentPointA.X) / Constants.ScaleFactor);
                        }
                        else
                        {
                            length = (int)((actualCoord.Y - ComponentPointA.Y) / Constants.ScaleFactor);
                        }
                        if (Math.Abs(length) >= NewLeadedComponent.MinLength)
                        {

                            NewLeadedComponent.Length = length;
                            NewLeadedComponent.orientation = o;
                            NewLeadedComponent.MakePermanent();
                            ParentCircuit.UpdateNetConnectivity();
                            StartedLeadedComponent = false;
                            ParentCircuit.AddUndoAction(new AddAction(NewLeadedComponent, ParentCircuit));
                        }
                    }
                }
                else
                {
                    ComponentData selectedComponentType = ParentCircuit.ParentWindow.GetSelectedComponent();
                    if (selectedComponentType != null)
                    {
                        Point rawCoord = GetHoleCoord(e.GetPosition(this), false);

                        Point actualCoord = new Point(0, 0);
                        bool isValid = false;

                        //We only want to start a new wire if the user clicks on or near a hole
                        foreach (Point p in BreadBoardHolePositions)
                        {
                            double distance = Math.Sqrt(Math.Pow((p.X - rawCoord.X), 2) + Math.Pow((p.Y - rawCoord.Y), 2));
                            if (distance < 0.4)
                            {
                                isValid = true;
                                actualCoord = p;
                                break;
                            }
                        }
                        if (isValid)
                        {
                            Component newComponent = Component.CreateComponent(ParentCircuit, GetAbsolutePosition(actualCoord), selectedComponentType);
                            if (newComponent != null)
                            {
                                if (newComponent is LeadedComponent)
                                {
                                    ComponentPointA = GetAbsolutePosition(actualCoord);
                                    StartedLeadedComponent = true;
                                    NewLeadedComponent = (LeadedComponent)newComponent;
                                    NewLeadedComponent.MakeTemporary();
                                }
                                else
                                {
                                    ParentCircuit.AddUndoAction(new AddAction(newComponent, ParentCircuit));
                                }
                                ParentCircuit.AddComponent(newComponent);
                                ParentCircuit.UpdateNetConnectivity();

                            }

                        }
                        
                    }
                }
            }
            ParentCircuit.ParentWindow.UpdatePrompt();
        } 



        //Gets the absolute position - relative to circuit area origin - of a set of hole coordinates
        private Point GetAbsolutePosition(Point hole)
        {
            double x, y;
            x = hole.X * Constants.ScaleFactor + Constants.OffsetX + Canvas.GetLeft(this);
            y = hole.Y * Constants.ScaleFactor + Constants.OffsetY + Canvas.GetTop(this);
            return new Point(x, y);
        }

        private static Point GetHoleCoord(Point positionRelToBreadboard, bool rounded = true)
        {
            Point holePos = new Point();

            holePos.X = (positionRelToBreadboard.X - (double)Constants.OffsetX) / Constants.ScaleFactor;
            holePos.Y = (positionRelToBreadboard.Y - (double)Constants.OffsetY) / Constants.ScaleFactor;
            if (rounded)
            {
                holePos.X = Math.Round(holePos.X);
                holePos.Y = Math.Round(holePos.Y);
            }
            return holePos;
        }

        public static string GetNetAtPoint(Point positionRelToBreadboard, string breadBoardReference = "")
        {
            Point holeCoord = GetHoleCoord(positionRelToBreadboard);
            if (BreadBoardNets.ContainsKey(holeCoord))
            {
                return BreadBoardNets[holeCoord].Replace("%B", breadBoardReference);
            }
            else
            {
                return "_invalid_" + breadBoardReference + "," + holeCoord.X + "," + holeCoord.Y;
            }
        }

        //Converts an absolute position to a position relative to the start of a breadboard, and returns the ID of that breadboard
        public static Point GetPositionOnBreadboard(Point absPosition, ref int breadboardId)
        {
            int breadBoardRefX = ((int)absPosition.X - Constants.BreadboardStartX) / Constants.BreadboardSpacingX;
            int breadBoardRefY = ((int)absPosition.Y - Constants.BreadboardStartY) / Constants.BreadboardSpacingY;
            int posRelBreadBoardX = ((int)absPosition.X - Constants.BreadboardStartX) % Constants.BreadboardSpacingX;
            int posRelBreadBoardY = ((int)absPosition.Y - Constants.BreadboardStartY) % Constants.BreadboardSpacingY;
            breadboardId = breadBoardRefX + breadBoardRefY * Constants.BreadboardsPerRow;
            return new Point(posRelBreadBoardX, posRelBreadBoardY);
        }
    }
}
