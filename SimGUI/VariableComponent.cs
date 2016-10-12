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
    //This class is used for components that have some value that is changeable during the simulation such as potentiometers
    //and LDRs.
    public class VariableComponent : Component
    {
        protected double SetPoint; //Current setpoint, where 0=min and 1=max

        protected Border AdjustmentPanel;
        protected Slider AdjustmentSlider;

        protected const double PanelWidth = 6;
        //const double PanelHeight = 2;

        public VariableComponent(Circuit parent, Point origin)
            : base(parent, origin)
        {
            AdjustmentPanel = new Border();
            AdjustmentPanel.Width = PanelWidth * Constants.ScaleFactor;
            //AdjustmentPanel.Height = PanelHeight * Constants.ScaleFactor;
            AdjustmentPanel.RenderTransform = new ScaleTransform(1.0 / Constants.ScaleFactor, 1.0 / Constants.ScaleFactor);

            Canvas.SetLeft(AdjustmentPanel, -PanelWidth / 2);
            Canvas.SetTop(AdjustmentPanel, 3.8);
            AdjustmentPanel.Background = Brushes.Gray;
            AdjustmentPanel.BorderBrush = Brushes.Black;
            AdjustmentPanel.Visibility = System.Windows.Visibility.Collapsed;
            //Force adjustment panel to be on top
            Canvas.SetZIndex(AdjustmentPanel, 100);

            AdjustmentSlider = new Slider();
            AdjustmentSlider.Orientation = System.Windows.Controls.Orientation.Horizontal;
            AdjustmentSlider.Minimum = 0;
            AdjustmentSlider.Maximum = 1;
            AdjustmentSlider.LargeChange = 0.2;
            AdjustmentSlider.SmallChange = 0.1;

            AdjustmentSlider.IsSnapToTickEnabled = false;
            AdjustmentSlider.ValueChanged += AdjustmentSlider_ValueChanged;
            AdjustmentPanel.Child = AdjustmentSlider;
            AdjustmentSlider.Margin = new Thickness(0,0.2,0,0.2);

        }

        private void AdjustmentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateSetpoint(e.NewValue);
        }

        //Subclasses should call this after they have completed their initialisation
        protected virtual void PostInit()
        {
            Children.Add(AdjustmentPanel);
            UpdateSetpoint(0.5);
        }

        //Updates the current setting of the component, between 0 and 1
        protected virtual void UpdateSetpoint(double newValue)
        {
            SetPoint = newValue;
            AdjustmentSlider.Value = newValue;
        }

        //Hide the slider whenever a click outside the component occurs
        public override void Deselect()
        {
            base.Deselect();
            AdjustmentPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        //Show the slider when it is clicked on in interact mode
        protected override void InteractiveClick(MouseButtonEventArgs e, bool isMouseDown)
        {
            if (isMouseDown)
            {
                ParentCircuit.DeselectAll();
                AdjustmentPanel.Visibility = System.Windows.Visibility.Visible;
                //Bring this element to top
                Canvas.SetZIndex(this, 1000);
            }
        }

        public override Dictionary<string, string> SaveParameters()
        {
            Dictionary<string, string> parameters = base.SaveParameters();
            parameters.Add("setpoint", SetPoint.ToString());
            return parameters;
        }

        public override void LoadParameters(Dictionary<string, string> parameters)
        {
            base.LoadParameters(parameters);
            if (parameters.ContainsKey("setpoint"))
            {
                UpdateSetpoint(double.Parse(parameters["setpoint"]));
            }
        }
    }
}
