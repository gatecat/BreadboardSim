#pragma once
class Component;
#include "Component.h"
/*
Collection of prototypes for passive components
Code for these components are in individual cpp files
*/

/*
An ideal resistor
*/
class Resistor :
	public Component
{
public:
	std::string GetComponentType();
	int GetNumberOfPins();
	double DCFunction(DCSolver *solver, int f);
	double TransientFunction(TransientSolver *solver, int f);
	double DCDerivative(DCSolver *solver, int f, VariableIdentifier var);
	double TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var);

	void SetParameters(ParameterSet params);
private:
	double Resistance = 0; //Resistance in ohms
};

/*
An capacitor, approximated trapezoidially, with optional series resistances 
*/
class Capacitor :
	public Component
{
public:
	std::string GetComponentType();
	int GetNumberOfPins();
	double DCFunction(DCSolver *solver, int f);
	double TransientFunction(TransientSolver *solver, int f);
	double DCDerivative(DCSolver *solver, int f, VariableIdentifier var);
	double TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var);

	void SetParameters(ParameterSet params);

private:
	double Capacitance = 1e-9;
	double SeriesResistance = 1e-3;
	double DCResistance = 1e12; 
	double lastT = 0;
	double usingBE = false;
};