#pragma once
class Component;
class DCSolver;
class TransientSolver;
struct VariableIdentifier;

#include <string>
#include "Component.h"
#include "DCSolver.h"
#include "TransientSolver.h"

/*
Structure to define a connection between a net and component
*/
struct NetConnection {
public:
	Component *component;
	int pin;
};

/*
A net is a connection going to one or more components
Nets can be fixed at a voltage (e.g. the GND net) or have the voltage determined by the components
connected to them.
*/
class Net
{
public:
	
	//Net name - must be unique throughout the circuit
	std::string NetName;

	//Whether or not net is fixed voltage
	bool IsFixedVoltage = false;

	//Voltage that the net is fixed at
	double NetVoltage = 0;

	//Pins the net is connected to
	std::vector<NetConnection> connections;

	/*Evaluate a Kirchoff-based function for the currents in the pins connected to the net,
	available for both DC and transient simulations
	(irrelevant for fixed-voltage nets)
	*/
    double DCFunction(DCSolver *solver);
    double TransientFunction(TransientSolver *solver);

	/*
	Evaluate the partial derivatives for the above functions
	*/
   double DCDerivative(DCSolver *solver, VariableIdentifier var);
   double TransientDerivative(TransientSolver *solver, VariableIdentifier var);

   VariableIdentifier GetNetVariableIdentifier();
};

