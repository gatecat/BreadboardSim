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
    class Transistor : Component
    {
        public Transistor(Circuit parent, Point origin, string model)
            : base(parent, origin)
        {
            ID = parent.GetNextComponentName("Q");
            ModelFile = "res/models/transistors.xml";
            LoadModel(model);
            switch (ModelCategory)
            {
                case "npn":
                    ComponentType = "NPN Transistor";
                    break;
                case "pnp":
                    ComponentType = "PNP Transistor";
                    break;
                case "nmos":
                    ComponentType = "N-channel MOSFET";
                    break;
                default:
                    ComponentType = "Transistor";
                    break;
            }
        }
    }
}
