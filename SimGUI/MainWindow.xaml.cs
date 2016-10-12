using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.Reflection;
namespace SimGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private double CurrentZoomFactor = 1;
        private const double ZoomFactorDelta = 0.25;

        private System.Windows.Threading.DispatcherTimer UpdateTimer = new System.Windows.Threading.DispatcherTimer();
        private int NumberOfUpdates = 0;

        public string SelectedTool = "SELECT";

        public Circuit circuit;

        //Map breadboard IDs to the breadboard objects
        //Breadboard IDs are such that top left breadboard = 0, top right breadboard = 1, one below the top left = 2, etc
        public Dictionary<int, Breadboard> Breadboards = new Dictionary<int, Breadboard>();

        //Path to last opened file
        private string LastOpenedFile = null;

        public Simulator CurrentSimulator = new Simulator();
        
        //List of required files
        private readonly string[] ApplicationResources = {
                                               "res/simbe.exe",
                                               "res/app.ico",
                                               "res/about.txt",
                                               "res/actions/graph.png",
                                               "res/actions/open.png",
                                               "res/actions/run.png",
                                               "res/actions/redo.png",
                                               "res/actions/save.png",
                                               "res/actions/settings.png",
                                               "res/actions/stop.png",
                                               "res/actions/undo.png",
                                               "res/actions/zoomin.png",
                                               "res/actions/zoomout.png",
                                               "res/models/7seg.xml",
                                               "res/models/diodes.xml",
                                               "res/models/ics.xml",
                                               "res/models/leds.xml",
                                               "res/models/transistors.xml",
                                               "res/breadboard/breadboard.png",
                                               "res/breadboard/breadboard-holes.csv",
                                               "res/tools/component.cur",
                                               "res/tools/component.png",
                                               "res/tools/delete.cur",
                                               "res/tools/delete.png",
                                               "res/tools/interact.cur",
                                               "res/tools/interact.png",
                                               "res/tools/select.png",
                                               "res/tools/wire.cur",
                                               "res/tools/wire.png"
                                           };
        private GraphView CurrentGraph = null;

        public MainWindow()
        {

            System.Diagnostics.Trace.WriteLine(Environment.Version);

            if (!System.IO.Directory.Exists("res/"))
            {
                System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                if (!System.IO.Directory.Exists("res/"))
                {
                    MessageBox.Show("The directory containing the resources needed by this application is missing. Please check your installation.", "Application Error", MessageBoxButton.OK,MessageBoxImage.Error);
                    Application.Current.Shutdown();
                }
            }

            string missingResource = "";
            if (CheckForMissingResources(ref missingResource))
            {
                MessageBox.Show("The resource \"" + missingResource + "\" is missing. Please check your installation.", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            circuit = new Circuit(this);
           // AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            App.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            InitializeComponent();

            TreeViewItem customResistor = new TreeViewItem();

            PopulateMenu(Devices_Resistors, Constants.E6, 0, 7, "Ω Resistor", "Resistor");

            TreeViewItem customCapacitor = new TreeViewItem();

            PopulateMenu(Devices_Capacitors, Constants.E3, -10, -7, "F Capacitor", "Capacitor");
            PopulateMenu(Devices_ElectroCapacitors, Constants.E3, -6, -3, "F Capacitor", "Electrolytic Capacitor");

            PopulateMenuWithModels(Devices_DigitalIC, "Integrated Circuit", "res/models/ics.xml", "Digital");
            PopulateMenuWithModels(Devices_AnalogIC, "Integrated Circuit", "res/models/ics.xml", "Analog");

            PopulateMenuWithModels(Devices_Diodes, "Diode", "res/models/diodes.xml");
            PopulateMenuWithModels(Devices_NPN, "NPN Transistor", "res/models/transistors.xml","npn");
            PopulateMenuWithModels(Devices_PNP, "PNP Transistor", "res/models/transistors.xml", "pnp");
            PopulateMenuWithModels(Devices_NMOS, "N-channel MOSFET", "res/models/transistors.xml", "nmos");


            PopulateMenuWithModels(Devices_Output, "LED", "res/models/leds.xml", null, " LED");
            PopulateMenuWithModels(Devices_Output, "7-Segment Display", "res/models/7seg.xml", null);

            ComponentData pot_10k = new ComponentData("Potentiometer", 10000, "10k Potentiometer");

            TreeViewItem pot_10k_item = new TreeViewItem();
            pot_10k_item.Header = pot_10k;
            Devices_Input.Items.Add(pot_10k_item);

            ComponentData ldr = new ComponentData("LDR", 0, "LDR");

            TreeViewItem ldr_item = new TreeViewItem();
            ldr_item.Header = ldr;
            Devices_Input.Items.Add(ldr_item);


            ComponentData spdt_switch = new ComponentData("SPDT Switch", 0, "SPDT Switch");

            TreeViewItem spdt_switch_item = new TreeViewItem();
            spdt_switch_item.Header = spdt_switch;
            Devices_Input.Items.Add(spdt_switch_item);


            ComponentData push_switch = new ComponentData("Push Switch", 0, "Push Switch");

            TreeViewItem push_switch_item = new TreeViewItem();
            push_switch_item.Header = push_switch;
            Devices_Input.Items.Add(push_switch_item);

            ComponentData probe = new ComponentData("Probe", 0, "Oscilloscope Probe");

            TreeViewItem probe_item = new TreeViewItem();
            probe_item.Header = probe;

            DevicePicker.Items.Add(probe_item);

            SetNumberOfBreadboards(1);

            UpdateTimer.Interval = TimeSpan.FromMilliseconds(50);
            UpdateTimer.Tick += SimUpdate;

            SelectTool("SELECT");

            string[] args = Environment.GetCommandLineArgs();
            
            if (args.Length > 1)
            {
                if (System.IO.File.Exists(args[1]))
                {
                    circuit.ClearUndoQueue();
                    circuit.LoadCircuit(args[1]);
                    LastOpenedFile = args[1];
                    Title = "Breadboard Simulator - " + System.IO.Path.GetFileNameWithoutExtension(args[1]);
                }
            }
            PopulateSamplesMenu(File_Samples, "res/samples");
        }

        private void PopulateSamplesMenu(MenuItem rootItem, string rootPath)
        {
            foreach (string dir in System.IO.Directory.EnumerateDirectories(rootPath))
            {
                string dirName = System.IO.Path.GetFileName(dir);

                MenuItem subMenu = new MenuItem();
                subMenu.Header = dirName;
                PopulateSamplesMenu(subMenu, rootPath + "/" + dirName);
                rootItem.Items.Add(subMenu);
            }
            foreach (string file in System.IO.Directory.EnumerateFiles(rootPath,"*.bbrd"))
            {
                string fileName = System.IO.Path.GetFileName(file);

                MenuItem menuItem = new MenuItem();
                menuItem.Header = System.IO.Path.GetFileNameWithoutExtension(file);
                menuItem.Click += sampleMenuItem_Click;
                rootItem.Items.Add(menuItem);

            }
        }

        private void sampleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem) {
                StopSimulation.Command.Execute(null);
                if (DisplaySaveChangesDialog())
                {
                    MenuItem item = (MenuItem) sender;
                    string fileName = item.Header.ToString() + ".bbrd";
                    item = (MenuItem)item.Parent;

                    while (item != File_Samples)
                    {
                        fileName = item.Header.ToString() + "/" + fileName;
                        if (!(item.Parent is MenuItem))
                            break;
                        item = (MenuItem)item.Parent;
                    }
                    fileName = "res/samples/" + fileName;
                    LastOpenedFile = null;
                    circuit.ClearUndoQueue();
                    circuit.LoadCircuit(fileName);
                    Title = "Breadboard Simulator - " + System.IO.Path.GetFileNameWithoutExtension(fileName);
                }
            }

        }

        private bool CheckForMissingResources(ref string missingResource)
        {
            foreach (string resource in ApplicationResources)
            {
                if (!System.IO.File.Exists(resource))
                {
                    missingResource = resource;
                    return true;
                }
            }
            return false;
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {

            try
            {
                string debugMessage = e.Exception.Message;
                e.Handled = true;

                MessageBoxResult r = MessageBox.Show("An internal error has occurred. It may be possible to continue running, but the internal state may be corrupted so it is reccommended that you save your work and restart"
                    + " the application.\r\nWould you like to continue running? (pressing No will exit.)\r\n\r\nDebug information: "
                    + debugMessage + "\r\n\r\nPlease report this error, together with a brief description of what you were doing at the time it occurred" +
                    " to David Shah <dave@ds0.me>", "Internal Error", MessageBoxButton.YesNo, MessageBoxImage.Error);

                if (r == MessageBoxResult.No)
                {
                    if (CurrentSimulator != null)
                    {
                        if (CurrentSimulator.SimRunning)
                        {
                            try
                            {
                                CurrentSimulator.SimProcess.Kill();
                            }
                            catch
                            {

                            }
                        }
                    }
                    Application.Current.Shutdown();
                }

            }
            catch
            {
                if (CurrentSimulator != null)
                {
                    if (CurrentSimulator.SimRunning)
                    {
                        try
                        {
                            CurrentSimulator.SimProcess.Kill();
                        }
                        catch
                        {

                        }
                    }
                }
                try
                {
                    Application.Current.Shutdown();

                }
                catch
                {

                }
            }
        }


        private void SimUpdate(object sender, EventArgs e)
        {

            if (CurrentSimulator.SimRunning)
            {
                CurrentSimulator.Update();
                //Graphing may fail with too few points
                if (CurrentSimulator.GetNumberOfTicks() >= 5)
                {
                    if (CurrentGraph != null)
                    {
                        CurrentGraph.PlotAll();
                    }
                }

                NumberOfUpdates++;

                foreach (var component in circuit.Components)
                    component.UpdateFromSimulation(NumberOfUpdates, CurrentSimulator,SimulationEvent.TICK);
                Quantity netVolt = new Quantity("v", "v", "V");

                foreach (var wire in circuit.Wires)
                {
                    int varId = CurrentSimulator.GetNetVoltageVarId(wire.NetName);
                    if (varId != -1)
                    {
                        netVolt.Val = CurrentSimulator.GetValueOfVar(varId, 0);
                        wire.ToolTip = netVolt.ToFixedString();
                    }
                    else {
                        wire.ToolTip = "";
                    }
                }
                //Run every second
                if ((NumberOfUpdates % 20) == 0)
                {
                    Quantity simulationTime = new Quantity("t", "Simulation Time", "s");
                    simulationTime.Val = CurrentSimulator.GetCurrentTime();
                    StatusText.Text = "Interactive Simulation Running | t=" + simulationTime.ToFixedString();
                }
            }
            else
            {
                StatusText.Text = "Ready";
                NumberOfUpdates = 0;
                UpdateTimer.Stop();

            }

            Util.DoEvents();
        }
        
        //Starts the simulator and initialises the GraphView
        private void StartSimulation()
        {
            if (CurrentSimulator.SimRunning)
                CurrentSimulator.Stop();
            if (UpdateTimer.IsEnabled)
                UpdateTimer.Stop();
            string netlist = circuit.GetNetlist();
            NumberOfUpdates = 0;
            CurrentSimulator.Start(netlist, circuit.SimulationSpeed.Val);
            if (CurrentSimulator.SimRunning)
            {
                while (!CurrentSimulator.VarNamesPopulated)
                {
                    CurrentSimulator.Update();
                    if (!CurrentSimulator.SimRunning)
                        return;
                }
                StatusText.Text = "Interactive Simulation Running";
                if (CurrentGraph != null)
                {
                    CurrentGraph.ResetAll();
                }
                else
                {
                    CurrentGraph = new GraphView();
                    CurrentGraph.SecPerDiv.Val = circuit.SimulationSpeed.Val;
                }

                CurrentGraph.StartSim(CurrentSimulator);
                foreach (Component c in circuit.Components)
                    c.UpdateFromSimulation(0, CurrentSimulator, SimulationEvent.STARTED);

                int numberOfTraces = 0;
                foreach (Probe p in circuit.Components.OfType<Probe>())
                {
                    Trace t = new Trace();
                    if (CurrentGraph.AddTrace(p.ID, CurrentSimulator.GetNetVoltageVarId(p.ConnectedNets[1]), ref t))
                    {
                        numberOfTraces++;
                        p.SetProbeColour(t.TraceBrush);
                    }
                }
                if(numberOfTraces > 0)
                    CurrentGraph.Show();
                UpdateTimer.Start();

            }
        }

        //Populate a TreeViewItem with items following a preferred value series that represent a given component; between the magnitudes specified by magBegin and magEnd
        private void PopulateMenu(TreeViewItem root, double[] series, int magBegin, int magEnd, string suffix, string componentType)
        {
            for (int mag = magBegin; mag <= magEnd; mag++)
            {
                foreach (double seriesValue in series)
                {
                    double val = seriesValue * Math.Pow(10, mag);
                    Quantity q = new Quantity();
                    q.Val = val;
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Header = new ComponentData(componentType, val, q.ToString() + suffix);
                    root.Items.Add(newItem);
                }
            }
        }

        //Populate a TreeViewItem with items from a model database
        private void PopulateMenuWithModels(TreeViewItem root, string componentType, string modelFile, string category = null, string suffix = "")
        {
            List<string> modelNames = Component.GetModelNames(modelFile, category);
            foreach(string model in modelNames) {
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Header = new ComponentData(componentType, 0, model + suffix, model);
                    root.Items.Add(newItem);
            }
        }

        //Adds a given breadboard, identified by ID
        private void AddBreadboard(int breadboardNumber)
        {
            int col = breadboardNumber % Constants.BreadboardsPerRow;
            int row = breadboardNumber / Constants.BreadboardsPerRow;
            Breadboard newBb = new Breadboard(circuit);
            Canvas.SetLeft(newBb, col * Constants.BreadboardSpacingX + Constants.BreadboardStartX);
            Canvas.SetTop(newBb, row * Constants.BreadboardSpacingY + Constants.BreadboardStartY);
            DrawArea.Children.Add(newBb);
            Breadboards[breadboardNumber] = newBb;
        }

        //Removes a given breadboard, identified by ID
        private void RemoveBreadboard(int breadboardNumber)
        {
            if (Breadboards.ContainsKey(breadboardNumber))
            {
                DrawArea.Children.Remove(Breadboards[breadboardNumber]);
                Breadboards.Remove(breadboardNumber);
            }
        }

        //Modifies the set of breadboards such that there are a certain number of breadboards
        public void SetNumberOfBreadboards(int newNumberOfBreadboards)
        {
            int currentNumberOfBreadboards = Breadboards.Count;
            if (newNumberOfBreadboards > currentNumberOfBreadboards)
            {
                for (int i = currentNumberOfBreadboards; i < newNumberOfBreadboards; i++)
                {
                    AddBreadboard(i);
                }
            }
            else if (newNumberOfBreadboards < currentNumberOfBreadboards)
            {
                for (int i = currentNumberOfBreadboards; i >= newNumberOfBreadboards; i--)
                {
                    RemoveBreadboard(i);
                }
            }
        }

        private void ZoomIn_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentZoomFactor *= 1 + ZoomFactorDelta;
            DrawArea.LayoutTransform = new ScaleTransform(CurrentZoomFactor, CurrentZoomFactor);
            CScroll.ScrollToHorizontalOffset(CScroll.HorizontalOffset * (1 + ZoomFactorDelta));
            CScroll.ScrollToVerticalOffset(CScroll.VerticalOffset * (1 + ZoomFactorDelta));

        }

        private void ZoomOut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentZoomFactor /= (1 + ZoomFactorDelta);
            DrawArea.LayoutTransform = new ScaleTransform(CurrentZoomFactor, CurrentZoomFactor);
            CScroll.ScrollToHorizontalOffset(CScroll.HorizontalOffset * (1 - ZoomFactorDelta));
            CScroll.ScrollToVerticalOffset(CScroll.VerticalOffset * (1 - ZoomFactorDelta));
        }
        private void ShowHideGraph_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentGraph == null)
            {
                CurrentGraph = new GraphView();
                CurrentGraph.Show();
            }
            else
            {
                if (CurrentGraph.IsVisible)
                {
                    CurrentGraph.Hide();
                }
                else
                {
                    CurrentGraph.Show();
                }
            }
        }

        private void ShowSettings_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CircuitProperties p = new CircuitProperties();
            p.SetProperties(GetNumberOfBreadboards(), circuit.SimulationSpeed.Val, circuit.PositiveRailVoltage, circuit.NegativeRailVoltage);
            p.ShowDialog();
            SetNumberOfBreadboards(p.GetSelectedNumberOfBreadboards());
            circuit.SimulationSpeed.Val = p.GetSelectedSimulationSpeed();
            circuit.PositiveRailVoltage = p.GetPositiveRailVoltage();
            circuit.NegativeRailVoltage = p.GetNegativeRailVoltage();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DisplaySaveChangesDialog())
            {
                CurrentSimulator.Stop();
                if (CurrentGraph != null)
                    CurrentGraph.Close();
            }
            else
            {
                e.Cancel = true;
            }

        }


        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    circuit.PurgeUnfinished();
                    break;
                default:
                    if (circuit.GetSelectedComponent() != null)
                    {
                        circuit.GetSelectedComponent().Component_KeyDown(sender, e);
                    }
                    else if (circuit.GetSelectedWire() != null)
                    {
                        circuit.GetSelectedWire().Wire_KeyDown(sender, e);
                    }
                    break;
            }
        }

        public ComponentData GetSelectedComponent()
        {
            if (DevicePicker.SelectedItem is TreeViewItem)
            {
                if(((TreeViewItem)DevicePicker.SelectedItem).Header is ComponentData) {
                    return (ComponentData)((TreeViewItem)DevicePicker.SelectedItem).Header;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }


        //Called when any of the tool items is clicked
        private void ToolChange_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string CommandName = ((RoutedCommand)e.Command).Name;
            //Command name is of the form SelectToolTOOL
            SelectTool(CommandName.Substring(10));
            e.Handled = true;
        }

        public void UpdatePrompt()
        {
            switch (SelectedTool)
            {
                case "SELECT":
                    if (circuit.GetSelectedWire() != null)
                    {
                        Prompt.Text = "Wire selected";
                    }
                    else if (circuit.GetSelectedComponent() != null)
                    {
                        Component c = circuit.GetSelectedComponent();
                        Prompt.Text = c.ComponentType + " " + c.ID + " selected";
                        if (c.ComponentModel != "")
                            Prompt.Text += ", model " + c.ComponentModel;
                        if (c.ComponentValue.ID != "")
                            Prompt.Text += ", value " + c.ComponentValue.ToString();
                    }
                    else
                    {
                        Prompt.Text = "Select an object";
                    }
                    break;
                case "INTERACT":
                    Prompt.Text = "Click on a component to interact with it";
                    break;
                case "WIRE":
                    if(Breadboard.StartedWire) {
                        Prompt.Text = "Click to finish placing wire";
                    } else {
                        Prompt.Text = "Click to start placing wire";
                    }
                    break;
                case "COMPONENT":
                    ComponentData selectedComponent = GetSelectedComponent();
                    if (selectedComponent != null)
                    {
                        if (Breadboard.StartedLeadedComponent)
                        {
                            Prompt.Text = "Click to finish placing " + selectedComponent.Label;
                        }
                        else
                        {
                            Prompt.Text = "Click to place " + selectedComponent.Label;
                        }
                    }
                    else
                    {
                        Prompt.Text = "Select a component type from the panel on the left";
                    }
                    break;
                case "DELETE":
                    Prompt.Text = "Click on a component or wire to delete it";
                    break;
            }
        }

        //Selects a new tool
        public void SelectTool(string toolName)
        {
            circuit.PurgeUnfinished();
            circuit.DeselectAll();
            foreach (ToggleButton t in Toolbox.Items.OfType<ToggleButton>())
            {
                t.IsChecked = false;
            }
            SelectedTool = toolName.ToUpper();
            switch (SelectedTool)
            {
                case "SELECT":
                    Tool_SELECT.IsChecked = true;
                    DrawArea.Cursor = Cursors.Arrow;
                    break;
                case "INTERACT":
                    Tool_INTERACT.IsChecked = true;
                    DrawArea.Cursor = new Cursor(Environment.CurrentDirectory + "/res/tools/interact.cur");
                    break;
                case "COMPONENT":
                    Tool_COMPONENT.IsChecked = true;
                    DrawArea.Cursor = new Cursor(Environment.CurrentDirectory + "/res/tools/component.cur");
                    break;
                case "WIRE":
                    Tool_WIRE.IsChecked = true;
                    DrawArea.Cursor = new Cursor(Environment.CurrentDirectory + "/res/tools/wire.cur");
                    break;
                case "DELETE":
                    Tool_DELETE.IsChecked = true;
                    DrawArea.Cursor = new Cursor(Environment.CurrentDirectory + "/res/tools/delete.cur");
                    break;
            }
            UpdatePrompt();
        }

        private void DevicePicker_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (GetSelectedComponent() != null)
            {
                SelectTool("COMPONENT");
            }
        }

        private void RunSimulation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StartSimulation();
        }

        public int GetNumberOfBreadboards()
        {
            return Breadboards.Count;
        }

        //If necessary, displays a warning about unsaved changes. Returns false when cancelled.
        private bool DisplaySaveChangesDialog()
        {
            if (circuit.UndoQueueChangedFlag)
            {
                MessageBoxResult result = MessageBox.Show("There are unsaved changes to the breadboard. Would you like to save them now?",
                    "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    File_Save.Command.Execute(null);
                    return true;
                }
                else if (result == MessageBoxResult.No)
                {
                    return true;
                }
                else {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }

        private void NewFile_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StopSimulation.Command.Execute(null);
            if (DisplaySaveChangesDialog())
            {
                LastOpenedFile = null;
                Title = "Breadboard Simulator - Untitled";
                circuit.ClearUndoQueue();
                circuit.ClearCircuit();
            }
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            StopSimulation.Command.Execute(null);
            if (DisplaySaveChangesDialog())
            {
                Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
                openDialog.CheckFileExists = true;
                openDialog.Filter = "Breadboards (.bbrd)|*.bbrd";
                openDialog.DefaultExt = ".bbrd";
                bool? result = openDialog.ShowDialog();
                //We want to check that file selection is successful and the user did not click Cancel
                if (result == true)
                {
                    string filename = openDialog.FileName;
                    circuit.ClearUndoQueue();
                    circuit.LoadCircuit(filename);
                    LastOpenedFile = filename;
                    Title = "Breadboard Simulator - " + System.IO.Path.GetFileNameWithoutExtension(filename);
                }
            }

        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            bool? result = false;
            string filename;

            bool saveAsCommand = false;
            if (e != null)
            {
                if (((RoutedCommand)e.Command).Name == "SaveAs")
                {
                    saveAsCommand = true;
                }
            }

            if (!saveAsCommand && (LastOpenedFile != null))
            {
                result = true;
                filename = LastOpenedFile;
            }
            else
            {
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "Breadboards (.bbrd)|*.bbrd";
                saveDialog.DefaultExt = ".bbrd";
                if (LastOpenedFile != null)
                {
                    saveDialog.InitialDirectory = System.IO.Path.GetDirectoryName(LastOpenedFile);
                    saveDialog.FileName = System.IO.Path.GetFileName(LastOpenedFile);
                }
                else
                {
                    saveDialog.FileName = "Untitled";
                }

                result = saveDialog.ShowDialog();
                filename = saveDialog.FileName;
            }
           
            

            if (result == true)
            {
                circuit.SaveCircuit(filename);
                circuit.UndoQueueChangedFlag = false;
                LastOpenedFile = filename;
                Title = "Breadboard Simulator - " + System.IO.Path.GetFileNameWithoutExtension(filename);
            }
        }


        private void StopSimulation_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            CurrentSimulator.Stop();
            UpdateTimer.Stop();
            NumberOfUpdates = 0;
            StatusText.Text = "Ready";

            foreach (Component c in circuit.Components)
            {
                c.UpdateFromSimulation(0, CurrentSimulator, SimulationEvent.STOPPED);
            }
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            circuit.UndoLast();
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            circuit.RedoLast();
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = circuit.CanRedoLast();
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = circuit.CanUndoLast();
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {

        }

        private void HelpAbout_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            MessageBox.Show(System.IO.File.ReadAllText("res/about.txt").Replace("{version}",Assembly.GetExecutingAssembly().GetName().Version.ToString()),"About Breadboard Simulator");
        }

        private void HelpContents_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("res\\doc\\index.html");
        }

        private void HelpGS_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("res\\doc\\getting-started.html");
        }

    }

    //Commands that are not built in to WPF, but that are needed for this application
    public static class CustomCommands
    {
        public static readonly RoutedUICommand CircuitSettings = new RoutedUICommand("Settings","CircuitSettings",typeof(MainWindow),
          new InputGestureCollection { new KeyGesture(Key.F4) });
        public static readonly RoutedUICommand RunSimulation = new RoutedUICommand("Run", "RunSimulation", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F5) });
        public static readonly RoutedUICommand StopSimulation = new RoutedUICommand("Stop", "StopSimulation", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.F6) });
        public static readonly RoutedUICommand ShowHideGraph = new RoutedUICommand("Show/Hide Graph", "ShowHideGraph", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.G, ModifierKeys.Control) });

        //Tool selection commands
        public static readonly RoutedUICommand SelectToolSELECT = new RoutedUICommand("Select", "SelectToolSELECT", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.Escape, ModifierKeys.Shift) });
        public static readonly RoutedUICommand SelectToolINTERACT = new RoutedUICommand("Interact", "SelectToolINTERACT", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.I, ModifierKeys.Control) });
        public static readonly RoutedUICommand SelectToolCOMPONENT = new RoutedUICommand("Place Components", "SelectToolCOMPONENT", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.A, ModifierKeys.Control) });
        public static readonly RoutedUICommand SelectToolWIRE = new RoutedUICommand("Place Wires", "SelectToolWIRE", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.W, ModifierKeys.Control) });
        public static readonly RoutedUICommand SelectToolDELETE = new RoutedUICommand("Delete", "SelectToolDELETE", typeof(MainWindow),
            new InputGestureCollection { new KeyGesture(Key.Delete, ModifierKeys.Shift) });

        public static readonly RoutedUICommand HelpContents = new RoutedUICommand("Contents", "HelpContents", typeof(MainWindow), new InputGestureCollection { new KeyGesture(Key.F1) });
        public static readonly RoutedUICommand HelpGS = new RoutedUICommand("Getting Started", "HelpGS", typeof(MainWindow));
        public static readonly RoutedUICommand HelpAbout = new RoutedUICommand("About", "HelpAbout", typeof(MainWindow));
    }
}
