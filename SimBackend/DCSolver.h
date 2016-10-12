#pragma once
class Net;
struct netConnection;
class Component;
class Circuit;

class TransientSolver;

#include <string>
#include <vector>

#include "TransientSolver.h"

#include "Math.h"

#include "Component.h"
#include "Net.h"
/*
DC Operating point solver

This solver, given a set of components and nets, returns a solution for the steady-state conditions of a circuit

ALL PIN NUMBERS START FROM 0
*/

//This class is used to identify the type of a variable
struct VariableIdentifier {
public:
	enum VariableType {
		COMPONENT,
		NET
	} type;
	Component *component;
	int pin;
	Net *net;

	bool operator==(VariableIdentifier& other)const;
};

class DCSolver
{
	friend TransientSolver;
public:
	DCSolver(Circuit *circuit);


	//Run a solve routine, returning whether or not successful
	bool Solve(double tol = 1e-8, int maxIter = 200, bool attemptRamp = true);

	//Get value of a net voltage at current point in solve routine
	double GetNetVoltage(Net *net);

	//Get value of current going INTO a pin at current point in solve routine
	double GetPinCurrent(Component *c, int pin);
	Circuit *SolverCircuit;

private:
	int nextFreeVariable = 0;

	//Add components and nets to be included in the solver
	void AddComponent(Component *c);
	void AddNet(Net *net);


	std::map<Net *, int> NetVariables; //Map pointers to nets to net voltage variable IDs

	//Note that there are n-1 (where n=number of pins) variables allocated to each component
	std::map<Component *, int> ComponentVariables; //Map component pins to pin current variable IDs

	std::map<int, VariableIdentifier> VariableData; //Allow variables to be looked up

	std::vector<double> VariableValues; //Map variable IDs to values

};

#include "Circuit.h"
