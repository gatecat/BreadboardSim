using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace SimGUI
{
    public class Component : Canvas
    {
        //Unique reference for component e.g. R1, D12, U3 etc
        public string ID = "";
        //Absolute position of origin of component
        protected Point ComponentPosition;
        //Map between pin number and relative position on component (scaled such that 1unit = spacing between two adjacent holes)
        protected Dictionary<int, Point> PinPositions;
        //The context menu displayed when the component is right clicked
        protected ContextMenu ComponentContextMenu;
        //Rotation angle of component in degrees
        protected int Angle = 0;
        //The circuit which contains the component
        protected Circuit ParentCircuit;
        //Whether or not the component has been selected
        public bool IsSelected = false;
        //The value of the component, e.g. the resistance of a resistor. Keep at 0 if irrelevant (e.g. for ICs)
        public Quantity ComponentValue = new Quantity("","","");
        //A mapping between pin number, and the name of the net connected to that pin
        public Dictionary<int, string> ConnectedNets;
        //A mapping between pin number, and component pin current variables related to that pin in the simulator
        protected Dictionary<int, List<int>> ConnectedPinVariables;
        //A mapping betwen pin number and user-friendly pin name
        protected Dictionary<int, string> PinNames;

        //Type of component e.g. Resistor, Capacitor, Variable Resistor, Integrated Circuit
        public string ComponentType = "";
        //Model of component, e.g. 4001, 555, 1N4001, etc
        public string ComponentModel = "";
        /*
         * Netlist in unmerged form, which will have $n (where n is a positive integer) 
         * replaced with the net connected to pin n and $r replaced with the component name
         */
        public string UnmergedNetlist = "";
        //The model filename for a given component type
        public string ModelFile = "";
        //The modle category that the component is
        public string ModelCategory = null;
        //Whether or not the component is currently being dragged
        public bool DragStarted = false;
        public Component(Circuit parent, Point origin)
        {
            ParentCircuit = parent;
            PinPositions = new Dictionary<int, Point>();
            ConnectedNets = new Dictionary<int, string>();
            ConnectedPinVariables = new Dictionary<int, List<int>>();
            PinNames = new Dictionary<int, string>();
            ComponentPosition = origin;
            Canvas.SetLeft(this, origin.X);
            Canvas.SetTop(this, origin.Y);

            ComponentValue.AllowZero = false;

           // RenderTransform = new ScaleTransform(Constants.scaleFactor, Constants.scaleFactor);
            UpdateTransform();
            MouseDown += Component_MouseDown;
            MouseMove += Component_MouseMove;
            MouseUp += Component_MouseUp;
            KeyDown += Component_KeyDown;
            ComponentContextMenu = new ContextMenu();
            if (!(this is LeadedComponent))
            {
                MenuItem rotateMenuItem = new MenuItem();
                rotateMenuItem.Header = "Rotate";
                rotateMenuItem.Click += rotateMenuItem_Click;
                ComponentContextMenu.Items.Add(rotateMenuItem);
            };
            MenuItem deleteMenuItem = new MenuItem();
            deleteMenuItem.Header = "Delete";
            ComponentContextMenu.Items.Add(deleteMenuItem);
            deleteMenuItem.Click += deleteMenuItem_Click;

            MenuItem propertiesMenuItem = new MenuItem();
            propertiesMenuItem.Header = "Properties";
            ComponentContextMenu.Items.Add(propertiesMenuItem);
            propertiesMenuItem.Click += propertiesMenuItem_Click;

            ContextMenu = ComponentContextMenu;
        }

        protected void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ParentCircuit.RemoveComponent(this);
            ParentCircuit.AddUndoAction(new DeleteAction(this, ParentCircuit));
        }

        protected void propertiesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowComponentProperties();
        }

        //Component_KeyDown must be public as it may be called if there is a MainWindow keypress that is not otherwise handled
        public void Component_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    ParentCircuit.RemoveComponent(this);
                    ParentCircuit.AddUndoAction(new DeleteAction(this, ParentCircuit));
                    break;
                case Key.P:
                    ShowComponentProperties();
                    break;
                case Key.R:
                    if (!(this is LeadedComponent))
                    {
                        Rotate();
                    };
                    break;
            }
        }

        protected void rotateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Rotate();
        }

        public virtual void SetComponentValue(double newValue)
        {
            ComponentValue.Val = newValue;
            UpdateText();
        }

        public virtual void SetComponentValue(Quantity newValue)
        {
            ComponentValue = newValue;
            UpdateText();
        }

        //Updates the component's rendertransform
        protected void UpdateTransform()
        {
            TransformGroup g = new TransformGroup();
            //Scale according to our standard scheme such that '1 unit' = the space between two adjacent holes
            g.Children.Add(new ScaleTransform(Constants.ScaleFactor, Constants.ScaleFactor));
            g.Children.Add(new RotateTransform(Angle));
            RenderTransform = g;
        }

        private void Rotate()
        {
            if (!(this is LeadedComponent))
            {
                Dictionary<string, string> oldParams = SaveParameters();
                Angle = (Angle + 90) % 360;
                UpdateTransform();
                Dictionary<string, string> newParams = SaveParameters();
                ParentCircuit.AddUndoAction(new ChangeAction(this, oldParams, newParams));
            }
        }

        public void SetAngle(int newAngle)
        {
            if (!(this is LeadedComponent))
            {
                Angle = newAngle;
                UpdateTransform();
            }
        }

        protected Point moveOrigin;
        public void Component_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (ParentCircuit.ParentWindow.SelectedTool == "SELECT")
                {
                    if (DragStarted)
                    {
                        Point newPosition = new Point(e.GetPosition(this).X - moveOrigin.X, e.GetPosition(this).Y - moveOrigin.Y);
                        newPosition = RenderTransform.Transform(newPosition);
                        newPosition = new Point(newPosition.X + ComponentPosition.X, newPosition.Y + ComponentPosition.Y);
                        Point snappedPosition = Util.snap(newPosition, Constants.ScaleFactor);
                        ComponentPosition = snappedPosition;
                        Canvas.SetLeft(this, ComponentPosition.X);
                        Canvas.SetTop(this, ComponentPosition.Y);
                    }

                }

            }
            //The below statements handle the case that the mouse is moved over a component
            //while a wire or leaded component is beign placed
            if (Breadboard.StartedWire)
            {
                Point actualCoord = e.GetPosition(ParentCircuit.ParentWindow.DrawArea);
                Orientation o = Util.getBestOrientation(Breadboard.WirePointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - Breadboard.WirePointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - Breadboard.WirePointA.Y) / Constants.ScaleFactor);
                }
                Breadboard.NewWire.Length = length;
                Breadboard.NewWire.WireOrientation = o;
                Breadboard.NewWire.Render();
            }
            else if (Breadboard.StartedLeadedComponent)
            {
                Point actualCoord = e.GetPosition(ParentCircuit.ParentWindow.DrawArea);
                Orientation o = Util.getBestOrientation(Breadboard.ComponentPointA, actualCoord);
                int length = 0;
                if (o == Orientation.Horizontal)
                {
                    length = (int)((actualCoord.X - Breadboard.ComponentPointA.X) / Constants.ScaleFactor);

                }
                else
                {
                    length = (int)((actualCoord.Y - Breadboard.ComponentPointA.Y) / Constants.ScaleFactor);
                }

                Breadboard.NewLeadedComponent.Length = length;
                Breadboard.NewLeadedComponent.orientation = o;
                Breadboard.NewLeadedComponent.Render();

            }
        }

        //Finishes moving the component about
        public void FinishDrag()
        {
            if (DragStarted)
            {
                DragStarted = false;
                if (PreMoveParameters != null)
                {
                    Dictionary<string, string> newParameters = SaveParameters();
                    if ((newParameters["startX"] != PreMoveParameters["startX"]) || (newParameters["startY"] != PreMoveParameters["startY"]))
                    {
                        ParentCircuit.AddUndoAction(new ChangeAction(this, PreMoveParameters, newParameters));
                    }
                }
            }

        }

        public virtual void Select()
        {
            ParentCircuit.DeselectAll();
            IsSelected = true;
            Opacity = 0.5;
        }

        public virtual void Deselect()
        {
            IsSelected = false;
            Opacity = 1;
        }
        //A snapshot of component parameters before the move is started
        private Dictionary<string, string> PreMoveParameters;
        protected void Component_MouseDown(object sender, MouseButtonEventArgs e)
        {
            moveOrigin = e.GetPosition(this);
            if (e.ChangedButton == MouseButton.Left) {
                if(ParentCircuit.ParentWindow.SelectedTool == "SELECT") {
                    ParentCircuit.DeselectAll();
                    Select();
                    PreMoveParameters = SaveParameters();
                    DragStarted = true;
                    foreach (var netConnection in ConnectedNets)
                    {
                        Debug.WriteLine(netConnection.Key.ToString() + " => " + netConnection.Value);
                    }
                }
                else if (ParentCircuit.ParentWindow.SelectedTool == "DELETE")
                {
                    ParentCircuit.RemoveComponent(this);
                }
            }
            if (ParentCircuit.ParentWindow.SelectedTool == "INTERACT")
            {
                InteractiveClick(e , true);
            }
            ParentCircuit.ParentWindow.UpdatePrompt();

        }

        void Component_MouseUp(object sender, MouseButtonEventArgs e)
        {
            InteractiveClick(e, false);
            ParentCircuit.FinishDraggingAll();
        }

        //This function is called on when a component is clicked on in interact mode. It is passed the MouseEventArgs from the original MouseDown event, and a bool stating whether it is a mousedown or a mouseup
        protected virtual void InteractiveClick(MouseButtonEventArgs e, bool isMouseDown)
        {
            //To be overriden if necessary
        }

        /*
         * Loads component footprint data from an XML file
         */
        public void LoadFootprintFromXml(string footprint, string filename = "res/footprints.xml")
        {
            Children.Clear();
            XmlDocument footprintDb = new XmlDocument();
            footprintDb.Load(filename);
            XmlElement footprintElement = null;

            //Find footprint in XML file
            foreach (var footprintCandidate in footprintDb.DocumentElement.GetElementsByTagName("footprint"))
            {
                //Object might not be an element, must check before we cast
                if (footprintCandidate is XmlElement)
                {
                    XmlElement elem = (XmlElement)footprintCandidate;
                    //We want a case-insensitive match, hence the ToLower() calls
                    if (elem.GetAttribute("name").ToLower() == footprint.ToLower())
                    {
                        footprintElement = elem;
                        break;
                    }
                }
            }

            if (footprintElement == null)
            {
                //Footprint not found, throw an exception.
                throw new Exception("Component footprint " + footprint + " not in database.");
            }

            //Add paths
            foreach (var pathCandidate in footprintElement.GetElementsByTagName("path"))
            {
                if (pathCandidate is XmlElement)
                {
                    XmlElement pathElem = (XmlElement)pathCandidate;
                    Path newWpfPath = new Path();
                    if (pathElem.HasAttribute("name"))
                    {
                        newWpfPath.Name = pathElem.GetAttribute("name");
                    }
                    if (pathElem.HasAttribute("linecolour"))
                    {
                        Color lineColour = (Color)ColorConverter.ConvertFromString(pathElem.GetAttribute("linecolour"));
                        newWpfPath.Stroke = new SolidColorBrush(lineColour);
                    }
                    if (pathElem.HasAttribute("fillcolour"))
                    {
                        Color fillColour = (Color)ColorConverter.ConvertFromString(pathElem.GetAttribute("fillcolour"));
                        newWpfPath.Fill = new SolidColorBrush(fillColour);
                    }
                    if (pathElem.HasAttribute("thickness"))
                    {
                        newWpfPath.StrokeThickness = double.Parse(pathElem.GetAttribute("thickness"));
                    } else {
                        newWpfPath.StrokeThickness = 0.02;
                    }
                    //Path element contains path data in WPF path format
                    newWpfPath.Data = Geometry.Parse(pathElem.InnerText);
                    Children.Add(newWpfPath);
                }
            }

            //Add text objects
            foreach (var textCandidate in footprintElement.GetElementsByTagName("text"))
            {
                if (textCandidate is XmlElement)
                {
                    XmlElement textElem = (XmlElement)textCandidate;
                    TextBlock newTextBlock = new TextBlock();
                    if (textElem.HasAttribute("name"))
                    {
                        newTextBlock.Name = textElem.GetAttribute("name");
                    }
                    if (textElem.HasAttribute("X"))
                    {
                        Canvas.SetLeft(newTextBlock, double.Parse(textElem.GetAttribute("X")));
                    }
                    if (textElem.HasAttribute("Y"))
                    {
                        Canvas.SetTop(newTextBlock, double.Parse(textElem.GetAttribute("Y")));
                    }

                    if (textElem.HasAttribute("size"))
                    {
                        newTextBlock.FontSize =  double.Parse(textElem.GetAttribute("size"));
                    }
                    else
                    {
                        newTextBlock.FontSize = 1;
                    }

                    if (textElem.HasAttribute("font"))
                    {
                        newTextBlock.FontFamily = new FontFamily(textElem.GetAttribute("font"));
                    }
                    else
                    {
                        newTextBlock.FontFamily = new FontFamily("Segeo UI");
                    }
                    
                    if (textElem.HasAttribute("colour"))
                    {
                        Color textColour = (Color)ColorConverter.ConvertFromString(textElem.GetAttribute("colour"));
                        newTextBlock.Foreground = new SolidColorBrush(textColour);
                    }
                    else
                    {
                        newTextBlock.Foreground = Brushes.Black;
                    }
                    newTextBlock.Text = textElem.InnerText;
                    Children.Add(newTextBlock);
                }
            }

            //Load pin data
            PinPositions = new Dictionary<int, Point>();
            foreach (var pinCandidate in footprintElement.GetElementsByTagName("pin"))
            {
                if (pinCandidate is XmlElement)
                {
                    XmlElement pinElem = (XmlElement)pinCandidate;
                    if (pinElem.HasAttribute("x") && pinElem.HasAttribute("y") && pinElem.HasAttribute("number"))
                    {
                        PinPositions[int.Parse(pinElem.GetAttribute("number"))] = new Point(
                            double.Parse(pinElem.GetAttribute("x")),
                            double.Parse(pinElem.GetAttribute("y")));
                    }                  
                }
            }
        }

        /*
         * Returns a list of model names from an XML format file, optionally only including models from a certain category
         */
        public static List<string> GetModelNames(string filename, string category = null)
        {
            List<string> modelNames = new List<string>();
            XmlDocument modelDb = new XmlDocument();
            modelDb.Load(filename);
            //Iterate through models
            foreach (var modelObject in modelDb.DocumentElement.GetElementsByTagName("model"))
            {
                //Object might not be an element, must check before we cast
                if (modelObject is XmlElement)
                {
                    XmlElement modelElement = (XmlElement)modelObject;
                    if (category == null) //If category is set to null, then include all categories or models with no category
                    {
                        if (modelElement.HasAttribute("name"))
                        {
                            modelNames.Add(modelElement.GetAttribute("name"));
                        }
                    }
                    else
                    {
                        if (modelElement.HasAttribute("category"))
                        {
                            if (modelElement.GetAttribute("category").ToLower() == category.ToLower())
                            {
                                if (modelElement.HasAttribute("name"))
                                {
                                    modelNames.Add(modelElement.GetAttribute("name"));
                                }
                            }
                        }
                    }
                }
            }
            return modelNames;
        }

        /*
         * Loads the data for a given model from a given XML file, including loading the footprint if relevant, 
         * and returns a dictionary mapping metadata keys to values
         * fileName defaults to ModelFile
         */
        public Dictionary<string, string> LoadModel(string modelName, string fileName = null)
        {
            if (fileName == null)
                fileName = ModelFile;
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            XmlDocument modelDb = new XmlDocument();
            modelDb.Load(fileName);
            XmlElement modelElement = null;
            
            //Find footprint in XML file
            foreach (var modelCandidate in modelDb.DocumentElement.GetElementsByTagName("model"))
            {
                //Object might not be an element, must check before we cast
                if (modelCandidate is XmlElement)
                {
                    XmlElement elem = (XmlElement)modelCandidate;
                    //We want a case-insensitive match, hence the ToLower() calls
                    if (elem.GetAttribute("name").ToLower() == modelName.ToLower())
                    {
                        modelElement = elem;
                        break;
                    }
                }
            }

            if (modelElement == null)
            {
                //Footprint not found, throw an exception.
                throw new Exception("Component model " + modelName + " not in database.");
            }
            ComponentModel = modelName;
            if (modelElement.GetElementsByTagName("data").Count >= 1)
            {
                if (modelElement.GetElementsByTagName("data")[0] is XmlElement) {
                    UnmergedNetlist = ((XmlElement)modelElement.GetElementsByTagName("data")[0]).InnerText;
                }
            }
            //UnmergedNetlist = modelElement.InnerText;
            foreach (var attrObject in modelElement.Attributes)
            {
                if (attrObject is XmlAttribute)
                {
                    XmlAttribute attr = (XmlAttribute)attrObject;
                    metadata[attr.Name] = attr.Value;
                }
            }
            if (modelElement.HasAttribute("footprint"))
            {
                LoadFootprintFromXml(modelElement.GetAttribute("footprint"));
                UpdateText();
            }

            PinNames.Clear();
            //Read pin labels - models can set labels for any path, intended to give user-friendly pin names
            //Paths must exist when this happens.
            foreach (var labelElement in modelElement.GetElementsByTagName("label").OfType<XmlElement>())
            {
                if (labelElement.HasAttribute("pin") && (labelElement.HasAttribute("name")))
                {
                    foreach (Path p in Children.OfType<Path>())
                    {
                        if (p.Name == ("pin" + labelElement.GetAttribute("pin")))
                        {
                            int pin = int.Parse(labelElement.GetAttribute("pin"));
                            string label = pin.ToString() + ". " + labelElement.GetAttribute("name");
                            PinNames.Add(pin, label);

                            p.ToolTip = label;
                        }
                    }
                }
            }

            if (modelElement.HasAttribute("category"))
            {
                ModelCategory = modelElement.GetAttribute("category");
            }
            else {
                ModelCategory = null;
            }
            return metadata;
        }

        /*
         * Updates any 'special' text elements
         */
        public virtual void UpdateText()
        {
            foreach (var textObject in Children.OfType<TextBlock>())
            {
                if (textObject.Name == "_Model")
                {
                    textObject.Text = ComponentModel;
                }
                else if (textObject.Name == "_Value")
                {
                    textObject.Text = ComponentValue.ToString();
                }
            }
        }
 

        /*
         * Converts a position relative to the component origin with units such that 1 unit = the spacing between two holes
         * to an absolute position using px as units relative to the top left corner of the circuit area
         */
        protected Point GetAbsolutePosition(Point relativePosition)
        {
            double x = Canvas.GetLeft(this) + relativePosition.X * Constants.ScaleFactor;
            double y = Canvas.GetTop(this) + relativePosition.Y * Constants.ScaleFactor;
            return new Point(x, y);
        }

        /*
         * Returns a map between pin numbers and connected breadboard bus
         */
        public Dictionary<int, string> GetConnectedBreadboardNets()
        {
            Dictionary<int, string> connectedBusses = new Dictionary<int,string>();
            foreach (var pinPosition in GetPinPositions())
            {
                int breadboardID = 0;
                Point breadboardPosition = Breadboard.GetPositionOnBreadboard(
                    GetAbsolutePosition(pinPosition.Value), ref breadboardID);
                connectedBusses[pinPosition.Key] = Breadboard.GetNetAtPoint(breadboardPosition,
                    breadboardID.ToString());
            }
            return connectedBusses;
        }

        /*
         * Generates zero or more netlist lines as a string, using ConnectedNets 
         * to determine which pin each net is connected to
         */
        public virtual string GenerateNetlist()
        {
            //Default netlist handler just generates the netlist based on the unmerged netlist
            //Which is suitable for components that use models, e.g. ICs, diodes, transistors
            string netlist = UnmergedNetlist;
            foreach (var pinConnection in ConnectedNets)
            {
                netlist = netlist.Replace("{" + pinConnection.Key.ToString() + "}", pinConnection.Value);
            }
            netlist = netlist.Replace("{r}", ID);
            return netlist;
        }

        /*
         * Creates a new Component instance based on a ComponentData object
         */
        public static Component CreateComponent(Circuit parent, Point origin, ComponentData data)
        {
            Component newComponent = null;
            switch (data.ComponentType)
            {
                case "Resistor":
                    newComponent = new Resistor(parent, origin);
                    break;
                case "SPDT Switch":
                    newComponent = new Switch(parent, origin, true);
                    break;
                case "Push Switch":
                    newComponent = new Switch(parent, origin, false);
                    break;
                case "Integrated Circuit":
                    newComponent = new IntegratedCircuit(parent, origin, data.ComponentModel);
                    break;
                case "Capacitor":
                    newComponent = new Capacitor(parent, origin, false);
                    break;
                case "Electrolytic Capacitor":
                    newComponent = new Capacitor(parent, origin, true);
                    break;
                case "Potentiometer":
                    newComponent = new Potentiometer(parent, origin);
                    break;
                case "LDR":
                    newComponent = new LDR(parent, origin);
                    break;
                case "Diode":
                    newComponent = new Diode(parent, origin, data.ComponentModel);
                    break;
                case "Transistor":
                case "NPN Transistor":
                case "PNP Transistor":
                case "N-channel MOSFET":
                    newComponent = new Transistor(parent, origin, data.ComponentModel);
                    break;
                case "LED":
                    newComponent = new LED(parent, origin, data.ComponentModel);
                    break;
                case "7-Segment Display":
                    newComponent = new SevenSegment(parent, origin, data.ComponentModel);
                    break;
                case "Probe":
                    newComponent = new Probe(parent, origin);
                    break;
            }
            if (newComponent != null)
            {
                newComponent.SetComponentValue(data.ComponentValue);
            }
            return newComponent;
         }

        //Shows the component properties dialog
        public void ShowComponentProperties()
        {
            ComponentProperties dialog = new ComponentProperties();
            Dictionary<string, string> paramsBefore = SaveParameters();
            if (SetupPropertiesDialog(dialog))
            {
                dialog.ShowDialog();
                if (!dialog.WasCancelled)
                {
                    AfterPropertiesDialog(dialog);
                    Dictionary<string, string> paramsNow = SaveParameters();
                    if (paramsNow.Any(element => element.Value != paramsBefore[element.Key]))
                    {
                        ParentCircuit.AddUndoAction(new ChangeAction(this, paramsBefore, paramsNow));
                    }
                }
            }
        }

         /*
          * This function is called in order to populate the fields in a component properties dialog
          * It can be overriden to add extra fields. Return false to cancel display.
          */
        protected virtual bool SetupPropertiesDialog(ComponentProperties dialog)
        {
            if ((ModelFile == "") && (ComponentValue.ID == ""))
            {
                MessageBox.Show("There are no user changeable properties for this component.");
                return false;
            }
            if (ModelFile != "")
            {
                dialog.AddModels(GetModelNames(ModelFile, ModelCategory));
                dialog.SelectModel(ComponentModel);
            }
            if (ComponentValue.ID != "")
            {
                dialog.AddQuantity(ComponentValue);
            }
            return true;
        }

        /*
         * This function is called after displaying a component properties dialog
         * so that the component can configure its parameters based on the values entered by the user.
         */
        protected virtual void AfterPropertiesDialog(ComponentProperties dialog)
        {
            if (ComponentValue.ID != "")
            {
                ComponentValue.Val = dialog.Parameters[0].Val;
            }
            if (ModelFile != "")
            {
                if (dialog.SelectedModel != ComponentModel)
                {
                    LoadModel(dialog.SelectedModel);
                }
            }
            UpdateText();
            ParentCircuit.ParentWindow.UpdatePrompt();

        }

        /*
         * This function generates a set of key-value pairs that represent the component's parameters.
         * It can be overriden if extra parameters need to be loaded.
         */
        public virtual Dictionary<string, string> SaveParameters()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["ref"] = ID;
            parameters["type"] = ComponentType;
            parameters["startX"] = ComponentPosition.X.ToString();
            parameters["startY"] = ComponentPosition.Y.ToString();
            parameters["angle"] = Angle.ToString();
            if (ComponentValue.ID != "")
            {
                parameters["value"] = ComponentValue.Val.ToString();
            }
            if (ModelFile != "")
            {
                parameters["model"] = ComponentModel;
            }
            return parameters;
        }

        //Creates a new component object from the set of parameters generated by SaveParameters()
        public static Component CreateFromParameters(Circuit parent, Dictionary<string, string> parameters)
        {
            ComponentData data = new ComponentData(parameters["type"], 0, "", "");
            if (parameters.ContainsKey("value"))
                data.ComponentValue = double.Parse(parameters["value"]);
            if (parameters.ContainsKey("model"))
                data.ComponentModel = parameters["model"];
            Point origin = new Point(double.Parse(parameters["startX"]), double.Parse(parameters["startY"]));
            Component newComponent = Component.CreateComponent(parent, origin, data);
            if (parameters.ContainsKey("angle"))
                newComponent.SetAngle(int.Parse(parameters["angle"]));
            newComponent.LoadParameters(parameters);
            return newComponent;
        }

        //Loads the entire component state from a set of parameters. Used for undo/redo functionality
        public void ResetFromParameters(Dictionary<string, string> parameters)
        {
            if (parameters.ContainsKey("model"))
                if(ComponentModel != parameters["model"])
                    LoadModel(parameters["model"]);
            if (parameters.ContainsKey("value"))
                if (double.Parse(parameters["value"]) != ComponentValue.Val)
                    SetComponentValue(double.Parse(parameters["value"]));

            ComponentPosition = new Point(double.Parse(parameters["startX"]), double.Parse(parameters["startY"]));
            Canvas.SetLeft(this, ComponentPosition.X);
            Canvas.SetTop(this, ComponentPosition.Y);

            if (parameters.ContainsKey("angle"))
                SetAngle(int.Parse(parameters["angle"]));
            LoadParameters(parameters);
            ParentCircuit.ParentWindow.UpdatePrompt();
        }


        /*
         * This function sets up the component's parameters from a set of key-value pairs - it is called by CreateFromParameters. Component value and model are handled automatically by the load routine
         * so by default, this function does nothing. It can be overriden if extra parameters need to be loaded.
         */
        public virtual void LoadParameters(Dictionary<string, string> parameters)
        {
            
        }
        
        //Called so that a component can update its appearance based on the simulation. Used, for example, by LEDs.
        //Passes what type of event has happened, and the total number of updates that have occurred if it is a tick event
        public virtual void UpdateFromSimulation(int numberOfUpdates, Simulator sim, SimulationEvent eventType)
        {
            if (eventType == SimulationEvent.STARTED)
            {
                ConnectedPinVariables.Clear();
                //Generate list of pin variables
                //Using ConnectedNets as a source of pin numbers
                foreach (int pin in ConnectedNets.Keys)
                {
                    ConnectedPinVariables.Add(pin, new List<int>());
                
                }
                Regex regex = new Regex(@"\{([0-9]+)\}");
                foreach (string netlistLine in UnmergedNetlist.Split(new char[] {'\n'}))
                {
                    string[] parts = netlistLine.Trim().Split(new char[] {' '});
                    if (parts.Length >= 3)
                    {
                        //Ignore fixed voltage nets
                        if (parts[0] != "NET")
                        {
                            for (int i = 2; i < parts.Length; i++)
                            {
                                //Look for connections to pin
                                if (regex.IsMatch(parts[i]))
                                {
                                    int thisPinNum = int.Parse(regex.Matches(parts[i])[0].Groups[1].Value);
                                    //Determine pin number of the subcircuit component based on position
                                    int simPinNum = i - 1;
                                    string simCompName = parts[1].Replace("{r}", ID);
                                    int varId = sim.GetComponentPinCurrentVarId(simCompName, simPinNum);
                                    if (varId != -1)
                                    {
                                        ConnectedPinVariables[thisPinNum].Add(varId);
                                    }
                                }                                
                            }
                        }
                    }
                }
            }
            else if (eventType == SimulationEvent.TICK)
            {
                if ((numberOfUpdates % 2) == 0)
                {
                    Quantity current = new Quantity("I", "Current", "A");
                    Quantity voltage = new Quantity("V", "Voltage", "V");
                    foreach (var pin in ConnectedNets.Keys)
                    {
                        string label = "";
                        if (PinNames.ContainsKey(pin))
                        {
                            label = PinNames[pin] + "\r\n";
                        }
                        else
                        {
                            label = "Pin " + pin + "\r\n";
                        }
                        int netVarId = sim.GetNetVoltageVarId(ConnectedNets[pin]);
                        if (netVarId != -1)
                        {
                            voltage.Val = sim.GetValueOfVar(netVarId, 0);
                            label += voltage.ToFixedString();
                        }
                        if (ConnectedPinVariables.ContainsKey(pin))
                        {
                            if (ConnectedPinVariables[pin].Count > 0)
                            {
                                current.Val = 0;
                                foreach (int pinVar in ConnectedPinVariables[pin])
                                {
                                    current.Val += sim.GetValueOfVar(pinVar, 0);
                                }

                                string dir = "";
                                if (current.Val < 0)
                                    dir = "out";
                                if (current.Val > 0)
                                    dir = "in";
                                current.Val = Math.Abs(current.Val);
                                label += "\r\n" + current.ToFixedString() + " " + dir;

                            }
                        }

                        foreach (Path p in Children.OfType<Path>())
                        {
                            if (p.Name == ("pin" + pin))
                            {
                                p.ToolTip = label;
                            }
                        }
                    }
                }

            }
            else if (eventType == SimulationEvent.STOPPED)
            {
                foreach (int pin in ConnectedNets.Keys)
                {
                    if (PinNames.ContainsKey(pin))
                    {
                        foreach (Path p in Children.OfType<Path>())
                        {
                            if (p.Name == ("pin" + pin))
                            {
                                p.ToolTip = PinNames[pin];
                            }
                        }
                    }
                    else
                    {
                        foreach (Path p in Children.OfType<Path>())
                        {
                            if (p.Name == ("pin" + pin))
                            {
                                p.ToolTip = "Pin " + pin;
                            }
                        }
                    }
                }
            }
        }

        //Returns a map between pin number and relative position on component (scaled such that 1unit = spacing between two adjacent holes)
        //Adjusted for rotation
        public virtual Dictionary<int, Point> GetPinPositions()
        {
            Dictionary<int, Point> realPinPositions = new Dictionary<int, Point>();
            foreach(var pin in PinPositions) {
                realPinPositions[pin.Key] = new RotateTransform(Angle).Transform(pin.Value);
            }
            return realPinPositions;
        }
    }

    //The possible events that could be the reason for UpdateFromSimulation being called
    public enum SimulationEvent {
        STARTED, //The simulation was started
        TICK, //An update tick has occurred
        STOPPED //The simulation has been stopped
    }
}
