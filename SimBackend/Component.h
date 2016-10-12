#pragma once

#include <string>
#include <map>
class ParameterSet;
struct VariableIdentifier;
#include "Net.h"
#include "ParameterSet.h"
#include "DCSolver.h"
#include "TransientSolver.h"
/*
This class provides a minimum generic interface that all components derive from
As components may have more than two pins, there is the cabilitity for components to have multiple current variables, and thus multiple equations to solve.
For a generic component with n pins, there will be n-1 current variables and n-1 equations.
The current flowing in the remaining pin is equal to negative the sum of the currents through all other pins.

Pin numbers and function nunbers both start from 0.
*/
class Component
{
public:
	//Number of pins that the component has
	virtual int GetNumberOfPins() = 0;
	
	//Return a type string identifying the component (e.g. R for resistor, AC for AC source)
	virtual std::string GetComponentType() = 0;

	//Reference designator, e.g. R1 or D3
	std::string ComponentID; 

	//List of nets the component is connected to, identified by pin number
	std::vector<Net*> PinConnections;

	/*
	Returns a function that can be used by the non-linear DC operating point solver
	to solve as f(x) = 0
	For components with greater than 2 pins, f is the indentifier of the function 0 <= f < n-1
	*/
	virtual double DCFunction(DCSolver *solver, int f) = 0;

	/*
	Returns a function that can be used by the non-linear transient solver
	to solve as f(x) = 0
	For components with greater than 2 pins, f is the indentifier of the function 0 <= f < n-1
	*/
	virtual double TransientFunction(TransientSolver *solver, int f) = 0;

	/*
	Evaluate the partial derivatives for the above functions
	*/
	virtual double DCDerivative(DCSolver *solver, int f, VariableIdentifier var) = 0;
	virtual double TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) = 0;

	/*
	Get the identifier for the current variable for a pin
	*/
	VariableIdentifier getComponentVariableIdentifier(int pin);

	/*
	Initialise component parameters from a parameter set
	*/
	virtual void SetParameters(ParameterSet params);
};

