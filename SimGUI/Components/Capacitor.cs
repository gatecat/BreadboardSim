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
    public class Capacitor : Component
    {
        public Capacitor(Circuit parent, Point origin, bool electrolytic = false)
            : base(parent, origin)
        {
            if(electrolytic) {
                ComponentType = "Electrolytic Capacitor";
                LoadFootprintFromXml("capacitor_elec");
            } else {
                ComponentType = "Capacitor";
                LoadFootprintFromXml("capacitor_np");
            }
            ComponentValue = new Quantity("cap", "Capacitance", "F");
            ComponentValue.AllowZero = false;
            ComponentValue.AllowNegative = false;
            ID = parent.GetNextComponentName("C");
        }

        public override string GenerateNetlist()
        {
            return "CAP " + ID + " " + ConnectedNets[1] + " " + ConnectedNets[2] + " rser=0.001 cap=" + ComponentValue.Val.ToString();
        }
    }
}
