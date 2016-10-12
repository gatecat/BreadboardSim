using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimGUI
{

    public class Quantity
    {
        //Identifier for the quantity (i.e. res for resistance)
        public string ID;
        //User-friendly name for the quantity, displayed in the ComponentProperties dialog
        public string Title;
        //A symbol for the quantity's unit, i.e. Ω, F, etc
        public string Unit;

        public double Val = 0;

        //Used for validation in the ComponentProperties dialog - specify whether or not the quantity can be
        //zero or negative
        public bool AllowZero = true;
        public bool AllowNegative = false;

        public Quantity()
        {
            ID = "";
            Title = "";
            Unit = "";
            Val = 0;
        }

        public Quantity(string _id, string _title, string _unit)
        {
            ID = _id;
            Title = _title;
            Unit = _unit;
            Val = 0;
        }

        public enum Prefix
        {
            Pico = -12,
            Nano = -9,
            Micro = -6,
            Milli = -3,
            None = 0,
            Kilo = 3,
            Mega = 6,
            Giga = 9
        }
        //Returns the value divided by the most suitable prefix, with the prefix
        //E.g. 1.5 and Prefix.Kilo returned means 1.5k/1.5x10^3/1500
        public void GetValueWithPrefix(ref double value, ref Prefix prefix)
        {
            if (Val == 0)
            {
                value = 0;
                prefix = Prefix.None;
            }
            else {
                for (prefix = Prefix.Pico; prefix <= Prefix.Giga; prefix+=3)
                {
                    value = Val / Math.Pow(10, (int)prefix);
                    if (Math.Abs(value) < (1000 - 1e-10)) break; //1e-10 needed to avoid rounding errors
                }
            }

        }
        //Sets the value with a prefix
        //E.g. passing 12 and micro sets the value to 12^-6
        public void SetValueWithPrefix(double value, Prefix prefix)
        {
            Val = value * Math.Pow(10, (int)prefix);
        }

        //Returns the character that represents a prefix, or \0 for no prefix
        public static char PrefixToChar(Prefix p)
        {
            switch (p)
            {
                case Prefix.Pico:
                    return 'p';
                case Prefix.Nano:
                    return 'n';
                case Prefix.Micro:
                    return 'µ';
                case Prefix.Milli:
                    return 'm';
                case Prefix.None:
                    return '\0';
                case Prefix.Kilo:
                    return 'k';
                case Prefix.Mega:
                    return 'M';
                case Prefix.Giga:
                    return 'G';
                default:
                    return '\0';
            }
        }

        //Places the prefix at the start of a string, handling a null prefix correctly
        public static string PrependPrefix(string s, Prefix p)
        {
            char c = PrefixToChar(p);
            if (c != '\0')
            {
                return new string(new char[] {c}) + s;
            }
            else
            {
                return s;
            }
        }

        //Returns a nice formatted string, with the number displayed with the best fitting prefix
        public override string ToString()
        {
            double v = 0;
            Prefix p = Prefix.None;
            GetValueWithPrefix(ref v, ref p);
            return v.ToString() + PrependPrefix(Unit, p);
        }
        //Returns a nice formatted string to 2dp, with the number displayed with the best fitting prefix
        public string ToFixedString()
        {
            double v = 0;
            Prefix p = Prefix.None;
            GetValueWithPrefix(ref v, ref p);
            return String.Format("{0:0.##}",v) + PrependPrefix(Unit, p);
        }
    }
}
