#pragma once
#include "Component.h"
#include <string>
#include <map>

/*
There is one class called LogicGate which serves all logic gate types
Each logic gate type has an instance of LogicGateInfo defining the number of inputs, number of outputs, 
number of internal state variables and function to call to calculate outputs
*/

//inputs, outputs, state variables, whether or not start of tick
typedef void (*LogicFunction)(bool*, bool*, int*, bool);

struct LogicGateInfo {
public:
	int numberOfInputs;
	int numberOfOutputs;
	int numberOfStateVars;
	LogicFunction function;
};

class LogicGate :
	public Component
{
public:

	LogicGate(std::string type); //Constructor given type

	std::string GetComponentType();
	int GetNumberOfPins();

	/*
	Pin connections
	
	nI = number of inputs, nO = number of outputs

	0 .. nI-1 inputs
	nI .. nI + nO-1 outputs
	nI + nO negative supply
	nI + nO + 1 positive supply

	*/

	double DCFunction(DCSolver *solver, int f);
	double TransientFunction(TransientSolver *solver, int f);
	double DCDerivative(DCSolver *solver, int f, VariableIdentifier var);
	double TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var);

	void SetParameters(ParameterSet params);

	static std::map<std::string, LogicGateInfo> gates;
private:
	LogicGateInfo ThisGate;
	std::string TypeName = "";
	int *StateVars;
	bool *OutputStates;
	bool *InputStates;
	bool *LastInputStates;
	double LastTime = -1;

	double InputThreshold = 1.4; //Input threshold voltage between 0 and 1
	double Hysteresis = 0.1; //Input hysteresis
	double InputResistance = 1e6; //input resistance
	double OutputResistance = 20; //Output resistance
};

namespace LogicFunctions {
	void AND(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void OR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void XOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void NOT(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void NAND(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void NOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void XNOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void RS_FLIP_FLOP(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);

	void D_FLIP_FLOP(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void DCOUNTER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void BRCOUNTER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);
	void DISPDECODER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick);

}

