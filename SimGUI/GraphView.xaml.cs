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
    /// <summary>
    /// Interaction logic for GraphView.xaml
    /// </summary>
    public struct Trace
    {
        public Trace(int _index, Brush _brush, int _varId, string _name) {
            TraceIndex = _index;
            TraceBrush = _brush;
            VariableId = _varId;
            TraceName = _name;
        }
        public readonly int TraceIndex;
        public readonly Brush TraceBrush;
        public int VariableId;
        public string TraceName;
    }

    public partial class GraphView : Window
    {
        //Vertical grid spacing in px
        private const double VerticalGridSpacing = 60;
        //Horizontal grid spacing in px
        private const double HorizontalGridSpacing = 60;

        private const int MaxNumberOfTraces = 8;

        public Quantity SecPerDiv, VoltsPerDiv;
        public double VoltsOffset;


        private Path GraphGrid;
        
        public Trace?[] Traces = new Trace?[MaxNumberOfTraces];

        private Simulator CurrentSim = null;

        //List of colours
        private readonly Brush[] TraceColours = new Brush[] {
            Brushes.Red,
            Brushes.Blue,
            Brushes.Green,
            Brushes.Yellow,
            Brushes.Cyan,
            Brushes.Magenta,
            Brushes.Orange,
            Brushes.Brown
        };

        public GraphView()
        {
            InitializeComponent();
            GraphGrid = new Path();
            GraphGrid.StrokeThickness = 0.5;
            GraphGrid.Stroke = Brushes.Gray;
            GraphArea.Children.Add(GraphGrid);

            SecPerDiv = new Quantity("spd", "Time per div", "s");
            SecPerDiv.AllowZero = false;
            SecPerDiv.AllowNegative = false;
            VoltsPerDiv = new Quantity("vpd", "Volts per div", "V");
            VoltsPerDiv.AllowZero = false;
            VoltsPerDiv.AllowNegative = false;
            VoltsOffset = -2;

            SecPerDiv.Val = 1;
            VoltsPerDiv.Val = 2;

            for (int i = 0; i < MaxNumberOfTraces; i++)
            {
                Traces[i] = null;
            }


        }

        public void UpdateGrid()
        {
            GeometryGroup gridLines = new GeometryGroup();
            double lineX = 0;
            Quantity currentTime = new Quantity();
            currentTime.Val = 0;
            HLabels.Children.Clear();
            while (lineX >= -GraphArea.ActualWidth)
            {
                gridLines.Children.Add(new LineGeometry(new Point(GraphArea.ActualWidth + lineX, 0), new Point(GraphArea.ActualWidth + lineX, GraphArea.ActualHeight)));
                TextBlock label = new TextBlock();
                label.HorizontalAlignment = HorizontalAlignment.Left;

                label.TextAlignment = TextAlignment.Center;
                label.Text = currentTime.ToString();
                label.RenderTransformOrigin = new Point(0.5, 0.5);
                label.RenderTransform = new TranslateTransform(lineX + GraphArea.ActualWidth + 50, 0);
                HLabels.Children.Add(label);
                lineX -= VerticalGridSpacing;

                currentTime.Val -= SecPerDiv.Val;
            }


            double lineY = GraphArea.ActualHeight / 2;
            Quantity currentVoltsA = new Quantity();
            currentVoltsA.Val = -VoltsOffset;
            Quantity currentVoltsB = new Quantity();
            currentVoltsB.Val = -VoltsOffset;

            VLabels.Children.Clear();

            while (lineY > 0)
            {
                gridLines.Children.Add(new LineGeometry(new Point(0, lineY), new Point(GraphArea.ActualWidth, lineY)));

                TextBlock label1 = new TextBlock();
                label1.HorizontalAlignment = HorizontalAlignment.Right;
                label1.Text = currentVoltsA.ToString();
                label1.RenderTransformOrigin = new Point(1, 0.5);
                label1.RenderTransform = new TranslateTransform(-5, lineY - 10);

                VLabels.Children.Add(label1);

               /* if (lineY != (GraphArea.ActualHeight - lineY))
                {*/
                    gridLines.Children.Add(new LineGeometry(new Point(0, GraphArea.ActualHeight - lineY), new Point(GraphArea.ActualWidth, GraphArea.ActualHeight - lineY)));

                    TextBlock label2 = new TextBlock();
                    label2.HorizontalAlignment = HorizontalAlignment.Right;
                    label2.Text = currentVoltsB.ToString();
                    label2.RenderTransformOrigin = new Point(1, 0.5);
                    label2.RenderTransform = new TranslateTransform(-5, GraphArea.ActualHeight - lineY - 10);

                    VLabels.Children.Add(label2);
                //}
                lineY -= HorizontalGridSpacing;
                currentVoltsA.Val += VoltsPerDiv.Val;
                currentVoltsB.Val -= VoltsPerDiv.Val;
            }
            GraphGrid.Data = gridLines;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {           
            UpdateGrid();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {          
            UpdateGrid();
            PlotAll();
        }

        //Add a trace given name and variable ID
        //Returns a bool indicating success or failure
        //If successful a Trace struct will be set containing the ID
        public bool AddTrace(string name, int varId, ref Trace traceData)
        {
            for (int i = 0; i < MaxNumberOfTraces; i++)
            {
                if (!Traces[i].HasValue)
                {
                    Trace t = new Trace(i, TraceColours[i], varId, name);
                    traceData = t;
                    Traces[i] = t;
                    TraceArea.AddTrace(TraceColours[i]);
                    return true;
                }
            }
            return false;
        }

        //Decode a set of (t, v) coordinates into a position to plot on the graph canvas
        private Point GetPoint(double t, double v)
        {
            double x = GraphArea.ActualWidth + VerticalGridSpacing * (t / SecPerDiv.Val);
            double y = GraphArea.ActualHeight / 2 - HorizontalGridSpacing * ((v + VoltsOffset) / VoltsPerDiv.Val);
            return new Point(x, y);
        }
        
        //Delete all traces
        public void ResetAll()
        {
            TraceArea.Reset();
            for (int i = 0; i < MaxNumberOfTraces; i++)
            {
                Traces[i] = null;
            }

        }


        //Plot a specific trace (specifiy by zero-based index)
        public void PlotTrace(int n)
        {
            double currentTime = CurrentSim.GetCurrentTime();
     

            StreamGeometry geo = new StreamGeometry();
            if (Traces[n].Value.VariableId != -1)
            {
                List<Point> points = new List<Point>();
                for (int i = 0; i > -CurrentSim.GetNumberOfTicks(); i--)
                {
                    Point point = GetPoint(CurrentSim.GetCurrentTime(i) - currentTime,
                        CurrentSim.GetValueOfVar(Traces[n].Value.VariableId, i));
                    if (point.Y < 0)
                        point.Y = 0.1;
                    if(point.Y > TraceArea.ActualHeight)
                       point.Y = TraceArea.ActualHeight;
                    points.Add(point);

                    if (point.X < -120) break;
                }
                TraceArea.SetTracePoints(n, points);
                
            }

            
        }

        //Called when a simulation is started
        public void StartSim(Simulator s)
        {
            CurrentSim = s;
        }

        //Replot all traces
        public void PlotAll()
        {
  
            if (CurrentSim != null)
            {
                if (CurrentSim.GetNumberOfTicks() >= 5)
                {
                    for (int i = 0; i < MaxNumberOfTraces; i++)
                    {
                        if (Traces[i].HasValue)
                        {
                            PlotTrace(i);
                        }
                    }
                }
            }
            GraphArea.InvalidateVisual();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {

            GraphSettings gs = new GraphSettings();
            gs.SetSettings(VoltsPerDiv.Val, VoltsOffset, SecPerDiv.Val);

            gs.ShowDialog();
            SecPerDiv = gs.GetSecPerDiv();
            VoltsPerDiv = gs.GetVoltsPerDiv();
            VoltsOffset = gs.GetVoltsOffset();
            UpdateGrid();
            PlotAll();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

    }

}
