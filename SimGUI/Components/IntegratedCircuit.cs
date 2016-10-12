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
    class IntegratedCircuit : Component
    {
        public IntegratedCircuit(Circuit parent, Point origin, string model)
            : base(parent, origin)
        {
            ComponentType = "Integrated Circuit";
            ID = parent.GetNextComponentName("IC");
            ModelFile = "res/models/ics.xml";
            LoadModel(model);
        }

        //A slight variation : we want ICs to only display their part number and not their full name
        public override void UpdateText()
        {
            base.UpdateText();
            foreach (var textObject in Children.OfType<TextBlock>())
            {
                if (textObject.Name == "_Model")
                {
                    if (ComponentModel.IndexOf(' ') != -1)
                    {
                        textObject.Text = ComponentModel.Substring(0, ComponentModel.IndexOf(' '));
                    }
                    else
                    {
                        textObject.Text = ComponentModel;
                    }
                    Util.DoEvents();

                }
            }
        }
    }
}
