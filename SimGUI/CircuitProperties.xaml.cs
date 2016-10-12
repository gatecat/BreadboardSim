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
    /// <summary>
    /// Interaction logic for CircuitProperties.xaml
    /// </summary>
    public partial class CircuitProperties : Window
    {
        private List<Quantity> SimulationSpeeds;
        public CircuitProperties()
        {
            InitializeComponent();
            SimulationSpeeds = new List<Quantity>();
            for (int magnitude = 0; magnitude >= -9; magnitude--)
            {
                for (int i = 0; i < Constants.PreferredValues.Length; i++)
                {
                    double val = Constants.PreferredValues[i] * Math.Pow(10, magnitude);
                    Quantity q = new Quantity("simspeed", "Simulation Speed", "s");
                    q.Val = val;
                    SimulationSpeeds.Add(q);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            double tmp = 0;
            //Validate numerical input
            if (!double.TryParse(PositiveRailVoltage.Text, out tmp))
            {
                MessageBox.Show("Positive rail voltage must be a valid number.");
                return;
            }
            if (!double.TryParse(NegativeRailVoltage.Text, out tmp))
            {
                MessageBox.Show("Negative rail voltage must be a valid number.");
                return;
            }
            Close();
        }
        private int SelectedSimIndex = 2;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (Quantity q in SimulationSpeeds)
            {
                SimulationSpeed.Items.Add(q);
            }
            SimulationSpeed.SelectedIndex = SelectedSimIndex;
        }

        public void SetProperties(int numberOfBreadboards, double simulationSpeed, double positiveRailVoltage, double negativeRailVoltage)
        {
            BreadboardCount.SelectedIndex = numberOfBreadboards - 1;
            for (int i = 0; i < SimulationSpeeds.Count; i++)
            {
                if (SimulationSpeeds[i].Val == simulationSpeed)
                {
                    SelectedSimIndex = i;
                    break;
                }
            }
            PositiveRailVoltage.Text = positiveRailVoltage.ToString();
            NegativeRailVoltage.Text = negativeRailVoltage.ToString();
        }

        public int GetSelectedNumberOfBreadboards()
        {
            return BreadboardCount.SelectedIndex + 1;
        }
        public double GetSelectedSimulationSpeed()
        {
            if (SimulationSpeed.SelectedItem is Quantity)
            {
                return ((Quantity)SimulationSpeed.SelectedItem).Val;
            }
            else
            {
                return 1;
            }
        }
        public double GetPositiveRailVoltage()
        {
            return double.Parse(PositiveRailVoltage.Text);
        }
        public double GetNegativeRailVoltage()
        {
            return double.Parse(NegativeRailVoltage.Text);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            double tmp = 0;
            //Validate numerical input
            if (!double.TryParse(PositiveRailVoltage.Text, out tmp))
            {
                MessageBox.Show("Positive rail voltage must be a valid number.");
                e.Cancel = true;
                return;
            }
            if (!double.TryParse(NegativeRailVoltage.Text, out tmp))
            {
                MessageBox.Show("Negative rail voltage must be a valid number.");
                e.Cancel = true;
                return;
            }
        }
        
    }
}
