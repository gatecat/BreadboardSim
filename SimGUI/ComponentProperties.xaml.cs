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

    public partial class ComponentProperties : Window
    {
        private int nextRow = 0;
        //The list of all quantities that can be configured
        public List<Quantity> Parameters = new List<Quantity>();
        //The list, in the same order as the parameters list, of all the textboxes that values can be enterred into
        private List<TextBox> ValueBoxes = new List<TextBox>();
        //The list, in the same order as the parameters list, of all the unit selection boxes
        private List<ComboBox> UnitBoxes = new List<ComboBox>();
        //The currently selected model name
        public string SelectedModel;
        //Whether or not to show the model drop-down box
        public bool UsingModel = false;
        //Whether the cancel or OK button was pressed
        public bool WasCancelled = true;

        //Add a quantity to the properties dialog
        public void AddQuantity(Quantity quantity)
        {
            Parameters.Add(quantity);
            //Add another row to the parameters grid
            CustomGrid.RowDefinitions.Add(new RowDefinition());

            //Add the label for the parameter
            Label paramLabel = new Label();
            paramLabel.Content = quantity.Title;
            paramLabel.HorizontalAlignment = HorizontalAlignment.Left;
            paramLabel.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(paramLabel, nextRow);
            Grid.SetColumn(paramLabel, 0);
            CustomGrid.Children.Add(paramLabel);

            //Determine the current value with prefix
            double val = 0;
            Quantity.Prefix pref = Quantity.Prefix.None;
            quantity.GetValueWithPrefix(ref val, ref pref);

            //Add the textbox for the value itself
            TextBox valueBox = new TextBox();
            valueBox.Text = val.ToString();
            valueBox.HorizontalAlignment = HorizontalAlignment.Left;
            valueBox.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(valueBox, nextRow);
            Grid.SetColumn(valueBox, 1);
            ValueBoxes.Add(valueBox);
            valueBox.Width = 80;
            CustomGrid.Children.Add(valueBox);

            //Add the units dropdown box
            ComboBox unitBox = new ComboBox();
            unitBox.HorizontalAlignment = HorizontalAlignment.Left;
            unitBox.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(unitBox, nextRow);
            Grid.SetColumn(unitBox, 2);
            int index = 0;
            //Add all possible prefixes into the dropdown box
            for (Quantity.Prefix p = Quantity.Prefix.Pico; p <= Quantity.Prefix.Giga; p += 3)
            {
                unitBox.Items.Add(Quantity.PrependPrefix(quantity.Unit, p));
                if (p == pref)
                    unitBox.SelectedIndex = index;
                index++;
            }
            UnitBoxes.Add(unitBox);
            CustomGrid.Children.Add(unitBox);
            //Increment the counter specifiying the number of the next row
            nextRow++;
        }

        //Populates the list of models, and enables its display.
        public void AddModels(List<string> models)
        {
            UsingModel = true;
            //Make the row containing the list of models a non-zero height
            MainGrid.RowDefinitions[0].Height = new GridLength(30);
            models.ForEach((a) => ModelSelection.Items.Add(a));
        }

        //Set the currently selected model
        public void SelectModel(string modelName)
        {
            SelectedModel = modelName;
            ModelSelection.SelectedItem = modelName;
        }

        public ComponentProperties()
        {
            InitializeComponent();
            MainGrid.RowDefinitions[0].Height = new GridLength(0);

        }

        

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {

            if (UsingModel)
            {
                SelectedModel = (string)ModelSelection.SelectedItem;
            }

            for (int n = 0; n < Parameters.Count; n++)
            {
                double newValue = 0;
                if (!double.TryParse(ValueBoxes[n].Text, out newValue))
                {
                    MessageBox.Show(Parameters[n].Title + " must be numeric.");
                    return;
                };
                if ((newValue == 0) && (!Parameters[n].AllowZero))
                {
                    MessageBox.Show(Parameters[n].Title + " cannot be zero.");
                    return;
                }
                if ((newValue < 0) && (!Parameters[n].AllowNegative))
                {
                    MessageBox.Show(Parameters[n].Title + " cannot be negative.");
                    return;
                }
                Quantity.Prefix newPrefix = UnitBoxes[n].SelectedIndex * 3 + Quantity.Prefix.Pico;
                Parameters[n].SetValueWithPrefix(newValue, newPrefix);
            }
            WasCancelled = false;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
