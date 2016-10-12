#pragma once
#include "Component.h"
class Component;
/*
Collection of prototypes for discrete semiconductors
Code for these components are in individual cpp files
*/

/*
A diode using the Shockley diode model, with an ideality factor and series resistance
Pin 0 is anode, pin 1 is cathode
*/
class Diode :
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
	double SaturationCurrent = 1e-14; //Saturation current
	double IdealityFactor = 1; //Ideality factor (1 for an ideal diode) 
	double SeriesResistance = 0; //Series resistance
};

/*
A NPN BJT transistor using the Ebers-Moll model with series resistance
Pin 0 is collector
Pin 1 is base
Pin 2 is emitter

*/
class BJT :
	public Component
{
public:
	std::string GetComponentType();
	int GetNumberOfPins();
	double DCFunction(DCSolver *solver, int f);
	double TransientFunction(TransientSolver *solver, int f);
	double DCDerivative(DCSolver *solver, int f, VariableIdentifier var);
	double TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var);

	/*Change parameters such that device model is PNP
	Set parameters before calling this*/
	void MakePNP();

	void SetParameters(ParameterSet params);

private:
	double ForwardGain = 100; //Forward current gain
	double ReverseGain = 1; //Reverse current gain
	double SaturationCurrent = 1e-14; //Saturation current

	double Rcollector = 0; //Series collector resistance
	double Rbase = 0; //Series base resistance
	double Remitter = 0; //Series emitter resistance

	bool IsPNP = false;
	double GetVt();

};


//N-channel MOSFET

//Pin 0 : source
//Pin 1 : gate
//Pin 2 : drain

class NMOS :
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
	double K = 100; //gain
	double lambda = 0; //channel modulation
	double Vth = 2; //threshold voltage

	double Rgs = 1e9; //Gate-source resistance
};