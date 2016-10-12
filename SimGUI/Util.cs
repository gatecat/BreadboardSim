using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SimGUI
{
    public enum Orientation
    {
        Horizontal,
        Vertical
    }
    public static class Util
    {
        //Snap either a single value (first overload) or a point coordinate (second overload) to a grid of a given spacing
        public static double snap(double coord, double gridSpacing = 10)
        {
            return (Math.Round(coord / gridSpacing) * gridSpacing);
        }

        public static Point snap(Point coord, double gridSpacing = 10)
        {
            Point p = new Point();
            p.X = (Math.Round(coord.X / gridSpacing) * gridSpacing);
            p.Y = (Math.Round(coord.Y / gridSpacing) * gridSpacing);
            return p;
        }

        /*
         * Returns the orientation of the closest line between two points
         */
        public static Orientation getBestOrientation(Point start, Point end)
        {
            if (Math.Abs(end.Y - start.Y) >= Math.Abs(end.X - start.X)) return Orientation.Vertical;
            else return Orientation.Horizontal;
        }

        public static void DoEvents()
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate { }));
        }
    }
}
