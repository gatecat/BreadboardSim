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
    class TraceDisplay : FrameworkElement
    {
        private List<Brush> TraceBrushes = new List<Brush>();
        private VisualCollection visualChildren;

        public TraceDisplay()
        {
            visualChildren = new VisualCollection(this);
        }
        //Removes all traces
        public void Reset()
        {
            visualChildren.Clear();
            TraceBrushes.Clear();
        }
        //Adds a trace to the end of the list of traces
        public void AddTrace(Brush brush)
        {
            visualChildren.Add(new DrawingVisual());
            TraceBrushes.Add(brush);
        }
        //Updates the set of points for a trace
        public void SetTracePoints(int traceNumber, List<Point> points)
        {
            if (traceNumber < visualChildren.Count)
            {
                DrawingVisual traceVisual = (DrawingVisual)visualChildren[traceNumber];
                DrawingContext traceContext = traceVisual.RenderOpen();
                Pen pen = new Pen(TraceBrushes[traceNumber], 2);
                pen.Freeze();
                StreamGeometry streamGeo = new StreamGeometry();
                StreamGeometryContext streamGeoCtx = streamGeo.Open();

                if (points.Count > 1)
                {
                    double lastX = points[0].X;
                    double lastY = points[0].Y;
                    streamGeoCtx.BeginFigure(points[0], false, false);
                    List<Point> linePoints = new List<Point>(); 
                    for (int i = 1; i < points.Count; i++)
                    {
                        if ((Math.Abs(lastX - points[i].X) > 0.5) || (Math.Abs(lastY - points[i].Y) > 100))
                        {
                            lastX = points[i].X;
                            lastY = points[i].Y;
                            if (points[i].X < 0)
                            {
                                Point endPoint = new Point();
                                endPoint.Y = points[i].Y;
                                endPoint.X = 0;
                                linePoints.Add(endPoint);
                                break;

                            }
                            else
                            {
                                linePoints.Add(points[i]);
                            }

                        }

                    }
                    streamGeoCtx.PolyLineTo(linePoints, true, false);
                }
                      

                streamGeoCtx.Close();
                streamGeo.Freeze();
                traceContext.DrawGeometry(null, pen, streamGeo);

                traceContext.Close();
            }


        }

        protected override int VisualChildrenCount
        {
            get { return visualChildren.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return visualChildren[index];
        }
    }
}
