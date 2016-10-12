using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SimGUI
{
    /*
     * This class contains constants, mostly related to the graphical layout of the program, that are used by various
     * classes throughout the program.
     */
    public static class Constants
    {
        //hole spacing in px (i.e. px per 2mm)
        //components and connections use units such that 1 unit = the spacing between two adjacent holes = scalefactor px
        public const double ScaleFactor = 15;

        //X-coordinate of centre of upper left hole, relative to start of bb
        public const int OffsetX = 30;
        //Y-coordinate of centre of upper left hole, relative to start of bb
        public const int OffsetY = 30;

        //X-coordinate of upper left of first breadboard
        public const int BreadboardStartX = 30;
        //Y-coordinate of upper left of first breadboard
        public const int BreadboardStartY = 30;

        //Distance horizontally between upper-left corners of two breadboards
        public const int BreadboardSpacingX = 780;
        //Distance vertically between upper-left corners of two breadboards
        public const int BreadboardSpacingY = 330;

        //Max number of breadboard columns
        public const int BreadboardsPerRow = 2;

        //The possible wire colours for a signal wire. The colour will be chosen at random when the wire is placed
        public static readonly Color[] RandomWireColours = {
                                                       Colors.DarkGreen,
                                                       Colors.Yellow,
                                                       Colors.Orange,
                                                       Colors.DarkMagenta,
                                                       Colors.SteelBlue,
                                                       Colors.LawnGreen,
                                                       Colors.Coral
                                                           };
        //A map betwen net name and colour for nets that have a fixed colour (typically power nets)
        public static readonly Dictionary<string, Color> FixedNetColours = new Dictionary<string, Color>
        {
            {"_power_V+",Colors.Red},
            {"_power_GND",Colors.Black},
            {"_power_V-",Colors.Blue}
        };

        //Preferred values for simulation speed and graph settings
        public static readonly int[] PreferredValues = { 5, 2, 1 };

        //E3 and E6 preferred component value series
        public static readonly double[] E3 = { 1, 2.2, 4.7 };
        public static readonly double[] E6 = { 1, 1.5, 2.2, 3.3, 4.7, 6.8 };
    }
}
