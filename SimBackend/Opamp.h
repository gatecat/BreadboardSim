#pragma once
#include "Component.h"
#include <string>
#include <cmath>

/*
A basic opamp model
Pin 0 is non-inverting input
Pin 1 is inverting input
Pin 2 is output
Pin 3 is -Vs
Pin 4 is +Vs

*/
class Opamp :
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
	double InputResistance = 1e6; //Input resistance
	double OpenLoopGain = 1e3; //Open loop gain

	double OutputResistance = 0.001; //Output resistance
	double VosatP = 0; //Positive output saturation voltage
	double VosatN = 0; //Negative output saturation voltage
	double LastVinp = 0;

	double Iq = 0.01;
};