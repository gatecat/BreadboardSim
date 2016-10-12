using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimGUI
{
    //A interface that represents any possible action that can be undone or redone
    public interface UndoAction
    {
        void Undo();
        void Redo();
    }
    
    //For actions that can affect either a component or wire, this determines which it is
    enum ObjectType {Wire, Component};

    //Represents adding a component or wire
    public class AddAction : UndoAction
    {
        private Wire AddedWire = null;
        private Component AddedComponent = null;
        private Circuit ParentCircuit = null;

        private ObjectType AffectedObjectType;

        public AddAction(Wire wire, Circuit circuit)
        {
            AffectedObjectType = ObjectType.Wire;
            AddedWire = wire;
            ParentCircuit = circuit;
        }

        public AddAction(Component component, Circuit circuit)
        {
            AffectedObjectType = ObjectType.Component;
            AddedComponent = component;
            ParentCircuit = circuit;
        }

        public void Undo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ParentCircuit.RemoveWire(AddedWire);
            }
            else
            {
                ParentCircuit.RemoveComponent(AddedComponent);
            }
        }

        public void Redo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ParentCircuit.AddWire(AddedWire);
            }
            else
            {
                ParentCircuit.AddComponent(AddedComponent);
            }
        }
    }

    //Represents deleting a component or wire
    public class DeleteAction : UndoAction
    {
        private Wire AddedWire = null;
        private Component AddedComponent = null;
        private Circuit ParentCircuit = null;

        private ObjectType AffectedObjectType;

        public DeleteAction(Wire wire, Circuit circuit)
        {
            AffectedObjectType = ObjectType.Wire;
            AddedWire = wire;
            ParentCircuit = circuit;
        }

        public DeleteAction(Component component, Circuit circuit)
        {
            AffectedObjectType = ObjectType.Component;
            AddedComponent = component;
            ParentCircuit = circuit;
        }

        public void Undo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ParentCircuit.AddWire(AddedWire);
            }
            else
            {
                ParentCircuit.AddComponent(AddedComponent);
            }
        }

        public void Redo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ParentCircuit.RemoveWire(AddedWire);
            }
            else
            {
                ParentCircuit.RemoveComponent(AddedComponent);
            }
        }
    }

    //Represent a change of a component or wire
    public class ChangeAction : UndoAction
    {
        private Wire ChangedWire = null;
        private Component ChangedComponent = null;
        private Dictionary<string, string> PreviousParameters;
        private Dictionary<string, string> NewParameters;


        private ObjectType AffectedObjectType;

        public ChangeAction(Wire wire, Dictionary<string, string> prevParams, Dictionary<string, string> newParams)
        {
            AffectedObjectType = ObjectType.Wire;
            ChangedWire = wire;
            PreviousParameters = prevParams;
            NewParameters = newParams;
        }

        public ChangeAction(Component component, Dictionary<string, string> prevParams, Dictionary<string, string> newParams)
        {
            AffectedObjectType = ObjectType.Component;
            ChangedComponent = component;
            PreviousParameters = prevParams;
            NewParameters = newParams;
        }

        public void Undo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ChangedWire.ResetFromParameters(PreviousParameters);
            }
            else
            {
                ChangedComponent.ResetFromParameters(PreviousParameters);
            }
        }

        public void Redo()
        {
            if (AffectedObjectType == ObjectType.Wire)
            {
                ChangedWire.ResetFromParameters(NewParameters);
            }
            else
            {
                ChangedComponent.ResetFromParameters(NewParameters);
            }
        }
    }
}
