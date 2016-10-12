using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Diagnostics;
using System.Xml;
using System.Windows;
using System.Windows.Input;
namespace SimGUI
{
    public class Circuit
    {
        public List<Wire> Wires;
        public List<Component> Components;
        public MainWindow ParentWindow;

        //The speed at which the simulation runs - the simulation seconds that occur in one real second
        public Quantity SimulationSpeed;
        //The voltage of the positive (red) rail
        public double PositiveRailVoltage = 5;
        //The voltage of the negative (blue) rail
        public double NegativeRailVoltage = -5;
        
        //A 'queue' of actions used for undo/redo
        private List<UndoAction> UndoQueue = new List<UndoAction>();
        //Current position in the UndoQueue
        private int UndoQueuePointer = -1;
        //A flag that represents whether the undo queue has been since it was last cleared
        //Used to detect whether there are any changes to be saved.
        public bool UndoQueueChangedFlag = false;

        public Circuit(MainWindow parent)
        {
            Wires = new List<Wire>();
            Components = new List<Component>();
            ParentWindow = parent;
            SimulationSpeed = new Quantity("simspeed", "Simulation Speed", "s");
            SimulationSpeed.Val = 1;
        }

        //Adds a wire to the circuit
        public void AddWire(Wire newWire)
        {
            Wires.Add(newWire);
            ParentWindow.DrawArea.Children.Add(newWire);
        }

        //Adds a component to the circuit
        public void AddComponent(Component newComponent)
        {
            Components.Add(newComponent);
            ParentWindow.DrawArea.Children.Add(newComponent);
        }

        //Removes a wire from the circuit, if it is in the circuit
        public void RemoveWire(Wire wireToRemove)
        {
            if (Wires.Contains(wireToRemove))
            {
                Wires.Remove(wireToRemove);
                ParentWindow.DrawArea.Children.Remove(wireToRemove);
                if (!wireToRemove.IsTemporary)
                {
                    UpdateWireColours();
                }
            }
        }

        //Removes a component from the circuit, if it is in the circuit
        public void RemoveComponent(Component componentToRemove)
        {
            if (Components.Contains(componentToRemove))
            {
                Components.Remove(componentToRemove);
                ParentWindow.DrawArea.Children.Remove(componentToRemove);
            }
        }

        //Updates the netName field of all connections, and ConnectedNets fields of all components
        public void UpdateNetConnectivity()
        {
            //Map breadboard segments to net names
            Dictionary<string, string> netMappings = new Dictionary<string, string>();
            foreach (var wire in Wires)
            {
                string newNetName, removedNet;
                string[] wireConnections = wire.GetConnectedBreadboardNets();
                string[] connectedNetNames = new string[2];
                Debug.Assert(wireConnections.Length == 2);
                for (int i = 0; i < 2; i++)
                {
                    if (netMappings.ContainsKey(wireConnections[i]))
                    {
                        connectedNetNames[i] = netMappings[wireConnections[i]];
                    }
                    else
                    {
                        connectedNetNames[i] = wireConnections[i];
                    }
                }
                if (connectedNetNames[1].StartsWith("_power_"))
                {
                    newNetName = connectedNetNames[1];
                    removedNet = connectedNetNames[0];
                }
                else
                {
                    newNetName = connectedNetNames[0];
                    removedNet = connectedNetNames[1];
                }
                //We must iterate through keys as we need to update collection
                foreach (var mappingBus in netMappings.Keys.ToList())
                {
                    if (netMappings[mappingBus] == removedNet)
                        netMappings[mappingBus] = newNetName;
                }
                netMappings[wireConnections[0]] = newNetName;
                netMappings[wireConnections[1]] = newNetName;
            }
            foreach (var wire in Wires)
            {
                wire.NetName = netMappings[wire.GetConnectedBreadboardNets()[0]];
            }
            foreach (var component in Components)
            {
                component.ConnectedNets = new Dictionary<int, string>();
                Dictionary<int, string> connectedBusses = component.GetConnectedBreadboardNets();
                foreach (var busConnection in connectedBusses)
                {
                    if (netMappings.ContainsKey(busConnection.Value))
                    {
                        component.ConnectedNets[busConnection.Key] = netMappings[busConnection.Value];
                    }
                    else
                    {
                        component.ConnectedNets[busConnection.Key] = busConnection.Value;
                    }
                }
            }
        }

        //Updates colours of wires so that interconnected wires have the same colour
        public void UpdateWireColours()
        {
            UpdateNetConnectivity();
            Dictionary<string, Color> netColours = new Dictionary<string, Color>();
            foreach (var wire in Wires)
            {
                if (Constants.FixedNetColours.ContainsKey(wire.NetName))
                {
                    wire.WireColour = Constants.FixedNetColours[wire.NetName];
                }                
                else if(netColours.ContainsKey(wire.NetName))
                {
                    wire.WireColour = netColours[wire.NetName];
                }
                else
                {
                    /*
                     * This if statement is to handle the special case where a net is connected to a power rail
                     * and then disconnected, as then it should not keep the same wire colour
                     */
                    if (Constants.FixedNetColours.Values.Contains(wire.WireColour))
                    {
                        wire.WireColour = Constants.RandomWireColours[new Random().Next(Constants.RandomWireColours.Length)];
                    }
                    netColours[wire.NetName] = wire.WireColour;
                }
                wire.Render();
            }
        }

        //Generates the entire netlist as a string
        public string GetNetlist()
        {
            UpdateNetConnectivity();
            string netlist = "";
            netlist += "NET _power_V+ " + PositiveRailVoltage.ToString() + "\r\n";
            netlist += "NET _power_GND 0" + "\r\n";
            netlist += "NET _power_V- " + NegativeRailVoltage.ToString() + "\r\n";
            foreach (Component c in Components)
            {
                netlist += c.GenerateNetlist() + "\r\n";
            }
            return netlist;
        }

        //Deselect all items (call before selecting another item to ensure only one item is selected at a time)
        public void DeselectAll()
        {
            Wires.ForEach((wire) => { wire.Deselect(); });
            Components.ForEach((component) => { component.Deselect(); });
            FinishDraggingAll();
        }

        //Sets all components to not being dragged
        public void FinishDraggingAll()
        {
            foreach (Component c in Components)
            {
                c.FinishDrag();
            }
        }


        //Routes a MouseMove event (parse args to this function) to the component currently being dragged
        public void HandleComponentDrag(object sender, MouseEventArgs e)
        {
            foreach (Component c in Components)
            {
                if (c.DragStarted)
                {
                    c.Component_MouseMove(sender, e);
                    break;
                }
            }
        }

        //Removes any unfinished 'temporary' components or wires
        public void PurgeUnfinished()
        {
            
            List<Wire> wiresToDelete = new List<Wire>();
            /*
             * We have to build up a list of items to delete first
             * as C# does not allow you to modify a collection that you are currently iterating through
             */
            foreach (var wire in Wires)
            {
                if (wire.IsTemporary)
                    wiresToDelete.Add(wire);
            }
            foreach (var wire in wiresToDelete)
            {
                RemoveWire(wire);
            }
            Breadboard.StartedWire = false;
            Breadboard.NewWire = null;

            List<Component> componentsToDelete = new List<Component>();
            foreach (var leadedComponent in Components.OfType<LeadedComponent>())
            {
                if (leadedComponent.IsTemporary)
                    componentsToDelete.Add(leadedComponent);
            }
            foreach (var component in componentsToDelete)
            {
                RemoveComponent(component);
            }
            Breadboard.StartedLeadedComponent = false;
            Breadboard.NewLeadedComponent = null;
            ParentWindow.UpdatePrompt();
        }

        //If a component is selected, returns it, otherwise returns null
        public Component GetSelectedComponent()
        {
            foreach (var component in Components)
            {
                if (component.IsSelected)
                {
                    return component;
                }
            }
            return null;
        }


        //If a wire is selected, returns it, otherwise returns null
        public Wire GetSelectedWire()
        {
            foreach (var wire in Wires)
            {
                if (wire.IsSelected)
                {
                    return wire;
                }
            }
            return null;
        }

        //Returns the next free component name with a given prefix
        public string GetNextComponentName(string prefix)
        {
            string nextName = "";
            int id = 0;
            do {
                id++;
                nextName = prefix  + id;
            } while(Components.Any((c) => (c.ID == nextName)));
            return nextName;
        }

        //Save a key-value pair to an element with a given name (a helper function for SaveCircuit)
        private XmlElement KeyValuePairToElement(XmlDocument doc, string elementName, string key, string value)
        {
            XmlElement kvElement = doc.CreateElement(elementName);
            kvElement.SetAttribute("key", key);
            kvElement.SetAttribute("value", value);
            return kvElement;
        }

        //Save circuit to an XML file, see report section 2.3.3.1 for format
        public void SaveCircuit(string filename)
        {
            //Remove unfinished objects
            PurgeUnfinished();

            XmlDocument circuitXml = new XmlDocument();
            //Root element
            XmlElement circuitRoot = circuitXml.CreateElement("breadboard");
            circuitXml.AppendChild(circuitRoot);

            //Save settings
            circuitRoot.AppendChild(KeyValuePairToElement(circuitXml, "setting", "numberOfBreadboards", ParentWindow.GetNumberOfBreadboards().ToString()));
            circuitRoot.AppendChild(KeyValuePairToElement(circuitXml, "setting", "simulationSpeed", SimulationSpeed.Val.ToString()));
            circuitRoot.AppendChild(KeyValuePairToElement(circuitXml, "setting", "positiveRailVoltage", PositiveRailVoltage.ToString()));
            circuitRoot.AppendChild(KeyValuePairToElement(circuitXml, "setting", "negativeRailVoltage", NegativeRailVoltage.ToString()));

            //Save components
            foreach (Component component in Components)
            {
                XmlElement componentElement = circuitXml.CreateElement("component");
                Dictionary<string, string> parameters = component.SaveParameters();
                foreach (var paramPair in parameters)
                    componentElement.SetAttribute(paramPair.Key, paramPair.Value);
                circuitRoot.AppendChild(componentElement);
            }

            //Save wires
            foreach (Wire wire in Wires)
            {
                XmlElement wireElement = circuitXml.CreateElement("wire");
                Dictionary<string, string> parameters = wire.SaveParameters();
                foreach (var paramPair in parameters)
                    wireElement.SetAttribute(paramPair.Key, paramPair.Value);
                circuitRoot.AppendChild(wireElement);
            }

            circuitXml.Save(filename);
        }

        //Remove all components and wires
        public void ClearCircuit()
        {
            ParentWindow.SelectTool("SELECT");
            //We cannot use a foreach here as we are modifying the collection as we iterate through it.
            while(Components.Count > 0) {
                RemoveComponent(Components[0]);
            }
            while (Wires.Count > 0)
            {
                RemoveWire(Wires[0]);
            }
            ParentWindow.SetNumberOfBreadboards(1);
            SimulationSpeed.Val = 1;
            PositiveRailVoltage = 5;
            NegativeRailVoltage = -5;
        }

        //Load circuit from an XML file, see section 2.3.3.1 for format
        public void LoadCircuit(string filename)
        {
            ClearCircuit();
            bool wasError = false;
            try
            {
                XmlDocument circuitXml = new XmlDocument();
                circuitXml.Load(filename);
                //Load settings

                foreach (var settingElement in circuitXml.GetElementsByTagName("setting").OfType<XmlElement>())
                {
                    try
                    {
                        if (settingElement.HasAttribute("key") && settingElement.HasAttribute("value"))
                        {
                            switch (settingElement.GetAttribute("key"))
                            {
                                case "numberOfBreadboards":
                                    ParentWindow.SetNumberOfBreadboards(int.Parse(settingElement.GetAttribute("value")));
                                    break;
                                case "simulationSpeed":
                                    SimulationSpeed.Val = double.Parse(settingElement.GetAttribute("value"));
                                    break;
                                case "positiveRailVoltage":
                                    PositiveRailVoltage = double.Parse(settingElement.GetAttribute("value"));
                                    break;
                                case "negativeRailVoltage":
                                    NegativeRailVoltage = double.Parse(settingElement.GetAttribute("value"));
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        wasError = true;
                    }

                }
                //Load components
                foreach (var componentElement in circuitXml.GetElementsByTagName("component").OfType<XmlElement>())
                {
                    try
                    {
                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        foreach (var attribute in componentElement.Attributes.OfType<XmlAttribute>())
                        {
                            parameters[attribute.Name] = attribute.Value;
                        }
                        AddComponent(Component.CreateFromParameters(this, parameters));
                    }
                    catch
                    {
                        wasError = true;
                    }

                }
                //Load wires
                foreach (var wireElement in circuitXml.GetElementsByTagName("wire").OfType<XmlElement>())
                {
                    try
                    {
                        Dictionary<string, string> parameters = new Dictionary<string, string>();
                        foreach (var attribute in wireElement.Attributes.OfType<XmlAttribute>())
                        {
                            parameters[attribute.Name] = attribute.Value;
                        }
                        AddWire(Wire.CreateFromParameters(this, parameters));
                    }
                    catch
                    {
                        wasError = true;
                    }

                }
            }
            catch
            {
                wasError = true;
            }

            if (wasError)
            {
                MessageBox.Show("An error occurred while loading the file. The file you selected may not be compatible with this version of the Breadboard Simulator," +
                " or it may have become corrupted.\r\nAs a result, the circuit may be missing entirely, incomplete or have broken components.", "Failure to open file", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            //Rebuild circuit
            UpdateWireColours();
            
        }

        //Undoes the last action
        public void UndoLast()
        {
            if (UndoQueuePointer >= 0)
            {
                UndoQueue[UndoQueuePointer].Undo();
                UndoQueuePointer--;
                UpdateWireColours();
                UndoQueueChangedFlag = true;
            }
        }

        //Redoes the last undone actione
        public void RedoLast()
        {
            if (UndoQueuePointer < (UndoQueue.Count - 1))
            {
                UndoQueuePointer++;
                UndoQueue[UndoQueuePointer].Redo();
                UpdateWireColours();
                UndoQueueChangedFlag = true;
            }
                
        }

        //Returns whether or not there is anything to undo
        public bool CanUndoLast()
        {
            return (UndoQueuePointer >= 0);
        }

        //Returns whether or not there is anything to redo
        public bool CanRedoLast()
        {
            return (UndoQueuePointer < (UndoQueue.Count - 1));
        }

        //Adds an action to the undo queue
        public void AddUndoAction(UndoAction action)
        {
            while (UndoQueue.Count > (UndoQueuePointer + 1))
            {
                UndoQueue.RemoveAt(UndoQueue.Count - 1);
            }
            UndoQueuePointer++;
            UndoQueue.Add(action);
            UndoQueueChangedFlag = true;
        }

        //Resets the undo queue - used when opening a new file
        public void ClearUndoQueue()
        {
            UndoQueuePointer = -1;
            UndoQueue.Clear();
            UndoQueueChangedFlag = false;
        }
    }
}
