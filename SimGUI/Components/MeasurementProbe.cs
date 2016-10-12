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
    class Probe : Component
    {
        public Probe(Circuit parent, Point origin)
            : base(parent, origin)
        {
            ComponentType = "Probe";
            LoadFootprintFromXml("probe");
            ID = parent.GetNextComponentName("P");
        }

        public override string GenerateNetlist()
        {
            return "";
        }

        public void SetProbeColour(Brush b)
        {
            foreach (Path p in Children.OfType<Path>())
            {
                if (p.Name == "ProbeBody")
                {
                    p.Fill = b;
                }
            }
        }
    }
}
