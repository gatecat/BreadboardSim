using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimGUI
{
    //This class represents the data needed to create a component - component type, component model and value. Label provides a user-friendly string to be displayed.
    public class ComponentData
    {
        public ComponentData(string _type, double _value, string _label, string _model = "")
        {
            ComponentType = _type;
            ComponentValue = _value;
            Label = _label;
            ComponentModel = _model;
        }
        public string ComponentType;
        public double ComponentValue;
        public string Label;
        public string ComponentModel;
        public override string ToString()
        {
            if (Label != null)
                return Label;
            else
                return "";
        }
    }
}
