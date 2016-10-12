#include "LogicGates.h"
#include <algorithm> //For copy()
LogicGate::LogicGate(std::string type) {
	TypeName = type;
	ThisGate = gates[type];
	StateVars = new int[ThisGate.numberOfStateVars];
	OutputStates = new bool[ThisGate.numberOfOutputs];
	InputStates = new bool[ThisGate.numberOfInputs];
	LastInputStates = new bool[ThisGate.numberOfInputs];
	for (int i = 0; i < ThisGate.numberOfStateVars;i++) {
		StateVars[i] = 0;
	}
	for (int i = 0; i < ThisGate.numberOfInputs; i++) {
		InputStates[i] = false;
		LastInputStates[i] = false;
	}
}

std::string LogicGate::GetComponentType() {
	return std::string("LOGIC_").append(TypeName);
}

int LogicGate::GetNumberOfPins() {
	return ThisGate.numberOfInputs + ThisGate.numberOfOutputs + 2;
}

void LogicGate::SetParameters(ParameterSet params) {
	InputThreshold = params.getDouble("vth", InputThreshold);
	InputResistance = params.getDouble("rin", InputResistance);
	OutputResistance = params.getDouble("rout", OutputResistance);

}

double LogicGate::DCFunction(DCSolver *solver, int f) {

	int groundPin = ThisGate.numberOfInputs + ThisGate.numberOfOutputs;
	int supplyPin = groundPin + 1;

	for (int i = 0; i < ThisGate.numberOfInputs; i++) {
		if (LastInputStates[i]) {
			InputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) > (InputThreshold - Hysteresis));
		}
		else {
			InputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) > (InputThreshold + Hysteresis));

		}
	}

	(*ThisGate.function)(InputStates, OutputStates, StateVars, false);

	if (f < ThisGate.numberOfInputs) { //Input eqns
		return solver->GetPinCurrent(this, f) - (1/InputResistance * solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[groundPin]));
	}
	else if (f < groundPin) { //Output eqns
		if (OutputStates[f - ThisGate.numberOfInputs])
			return (solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[supplyPin]) - OutputResistance * solver->GetPinCurrent(this, f));
		else
			return (solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[groundPin]) + OutputResistance * solver->GetPinCurrent(this, f));
	}
	else { //Ground pin eqn
		double L = solver->GetPinCurrent(this, groundPin);
		double R = 0;
		for (int i = 0; i < ThisGate.numberOfInputs; i++) {
			R -= solver->GetPinCurrent(this, i);
		}
		for (int i = ThisGate.numberOfInputs; i < groundPin; i++) {
			if (!OutputStates[i - ThisGate.numberOfInputs])
				R -= solver->GetPinCurrent(this, i);
		}
		return L - R;
	}
}

double LogicGate::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	//bool *inputStates = new bool[ThisGate.numberOfInputs];
	//bool *outputStates = new bool[ThisGate.numberOfOutputs];

	int groundPin = ThisGate.numberOfInputs + ThisGate.numberOfOutputs;
	int supplyPin = groundPin + 1;

	/*for (int i = 0; i < ThisGate.numberOfInputs; i++) {
		inputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) > threshold);
	}

	(*ThisGate.function)(inputStates, outputStates, stateVars);*/

	if (f < ThisGate.numberOfInputs) { //Input eqns
		if (var.type == var.COMPONENT && var.component == this && var.pin == f)
			return 1;
		if (var.type == var.NET && var.net == PinConnections[f])
			return -1 / InputResistance;
		if (var.type == var.NET && var.net == PinConnections[groundPin])
			return 1 / InputResistance;
	}
	else if (f < groundPin) { //Output eqns
		if (OutputStates[f - ThisGate.numberOfInputs]) {
			if (var.type == var.NET && var.net == PinConnections[f])
				return 1;
			if (var.type == var.NET && var.net == PinConnections[supplyPin])
				return -1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == f) 
				return -OutputResistance;
			
		}
		else {
			if (var.type == var.NET && var.net == PinConnections[f])
				return 1;
			if (var.type == var.NET && var.net == PinConnections[groundPin])
				return -1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == f) 
				return OutputResistance;
			
		}
	}
	else { //Ground pin eqn
		if (var.type == var.COMPONENT && var.component == this && var.pin == groundPin)
			return 1;
		if (var.type == var.COMPONENT && var.component == this && var.pin < ThisGate.numberOfInputs)
			return 1;
		if (var.type == var.COMPONENT && var.component == this && var.pin < groundPin)
			if (!OutputStates[var.pin - ThisGate.numberOfInputs])
				return 1;
	}
	return 0;
}


double LogicGate::TransientFunction(TransientSolver *solver, int f) {
	//bool *inputStates = new bool[ThisGate.numberOfInputs];
	//bool *outputStates = new bool[ThisGate.numberOfOutputs];

	int groundPin = ThisGate.numberOfInputs + ThisGate.numberOfOutputs;
	int supplyPin = groundPin + 1;

	if (solver->GetTimeAtTick(solver->GetCurrentTick())  > LastTime) {
		(*ThisGate.function)(InputStates, OutputStates, StateVars, true);
		LastTime = solver->GetTimeAtTick(solver->GetCurrentTick());
		(*ThisGate.function)(InputStates, OutputStates, StateVars, false);

	}

	for (int i = 0; i < ThisGate.numberOfInputs; i++) {
		if (LastInputStates[i]) {
			InputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) >(InputThreshold - Hysteresis));
		}
		else {
			InputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) > (InputThreshold + Hysteresis));

		}
	}
	//std::copy(inputStates, inputStates + ThisGate.numberOfInputs, lastInputStates);

	(*ThisGate.function)(InputStates, OutputStates, StateVars, false);

	if (f < ThisGate.numberOfInputs) { //Input eqns
		return solver->GetPinCurrent(this, f) - (1/InputResistance * solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[groundPin]));
	}
	else if (f < groundPin) { //Output eqns
		if (OutputStates[f - ThisGate.numberOfInputs])
			return (solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[supplyPin]) - OutputResistance * solver->GetPinCurrent(this, f));
		else
			return (solver->GetNetVoltage(PinConnections[f]) - solver->GetNetVoltage(PinConnections[groundPin]) + OutputResistance * solver->GetPinCurrent(this, f));
	}
	else { //Ground pin eqn
		double L = solver->GetPinCurrent(this, groundPin);
		double R = 0;
		for (int i = 0; i < ThisGate.numberOfInputs; i++) {
			R -= solver->GetPinCurrent(this, i);
		}
		for (int i = ThisGate.numberOfInputs; i < groundPin; i++) {
			if (!OutputStates[i - ThisGate.numberOfInputs])
				R -= solver->GetPinCurrent(this, i);
		}
		return L - R;
	}
}

double LogicGate::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	//bool *inputStates = new bool[ThisGate.numberOfInputs];
	//bool *outputStates = new bool[ThisGate.numberOfOutputs];

	int groundPin = ThisGate.numberOfInputs + ThisGate.numberOfOutputs;
	int supplyPin = groundPin + 1;

	/*for (int i = 0; i < ThisGate.numberOfInputs; i++) {
		inputStates[i] = ((solver->GetNetVoltage(PinConnections[i]) - solver->GetNetVoltage(PinConnections[groundPin])) > threshold);
	}

	(*ThisGate.function)(inputStates, outputStates, stateVars);*/

	if (f < ThisGate.numberOfInputs) { //Input eqns
		if (var.type == var.COMPONENT && var.component == this && var.pin == f)
			return 1;
		if (var.type == var.NET && var.net == PinConnections[f])
			return -1/InputResistance;
		if (var.type == var.NET && var.net == PinConnections[groundPin])
			return 1 / InputResistance;
	}
	else if (f < groundPin) { //Output eqns
		if (OutputStates[f - ThisGate.numberOfInputs]) {
			if (var.type == var.NET && var.net == PinConnections[f])
				return 1;
			if (var.type == var.NET && var.net == PinConnections[supplyPin])
				return -1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == f)
				return -OutputResistance;

		}
		else {
			if (var.type == var.NET && var.net == PinConnections[f])
				return 1;
			if (var.type == var.NET && var.net == PinConnections[groundPin])
				return -1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == f)
				return OutputResistance;
		}
	}
	else { //Ground pin eqn
		if (var.type == var.COMPONENT && var.component == this && var.pin == groundPin)
			return 1;
		if (var.type == var.COMPONENT && var.component == this && var.pin < ThisGate.numberOfInputs)
			return 1;
		if (var.type == var.COMPONENT && var.component == this && var.pin < groundPin)
			if (!OutputStates[var.pin - ThisGate.numberOfInputs])
				return 1;
	}
	return 0;
}
void LogicFunctions::AND(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = inputs[0] && inputs[1];
}

void LogicFunctions::OR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = inputs[0] || inputs[1];
}

void LogicFunctions::XOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = (inputs[0] != inputs[1]);
}

void LogicFunctions::NOT(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = !inputs[0];
}

void LogicFunctions::NAND(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = !(inputs[0] && inputs[1]);
}

void LogicFunctions::NOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = !(inputs[0] || inputs[1]);
}

void LogicFunctions::XNOR(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	outputs[0] = (inputs[0] == inputs[1]);
}

/*
See report section 2.4.4.1
Inputs: D, CLK, S, R
Outputs: Q, Q'
State Vars: 2, state and last clock value
*/
void LogicFunctions::D_FLIP_FLOP(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	if (startOfTick) {
		if (inputs[2]) {
			stateVars[0] = true;
		}
		else if (inputs[3]) {
			stateVars[0] = false;
		}
		else if (inputs[1] && !stateVars[1]){
			stateVars[0] = inputs[0];
		}
		stateVars[1] = inputs[1];
	}
	else {
		outputs[0] = stateVars[0];
		outputs[1] = !stateVars[0];
	}
}

/*
See report section 2.4.4.2
Inputs: CLK, INHIBIT, RESET
Outputs: Q0-Q9, CARRY
State Vars: 2, counter value and last clock value
*/
void LogicFunctions::DCOUNTER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	if (startOfTick) {
		if (inputs[2]) {
			stateVars[0] = 0;
		}
		else if (inputs[0] && !inputs[1] && !stateVars[1]){
			stateVars[0] = (stateVars[0] + 1) % 10;
		}
		stateVars[1] = inputs[0];
	}
	else {
		for (int i = 0; i < 10; i++) {
			if (stateVars[0] == i) {
				outputs[i] = true;
			}
			else {
				outputs[i] = false;
			}
		}
		if (stateVars[0] < 5) {
			outputs[10] = true;
		}
		else {
			outputs[10] = false;
		}
	}

};
/*
See report section 2.4.4.3
Inputs: CLK, RESET
Outputs: Q1-Q7
State Vars: 1, counter value and last clock value
*/
void LogicFunctions::BRCOUNTER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	if (startOfTick) {
		if (inputs[1]) {
			stateVars[0] = 0;
		}
		else if (inputs[0] && !stateVars[1]){
			stateVars[0] = (stateVars[0] + 1) % 128;
		}
		stateVars[1] = inputs[0];
	}
	else {
		for (int i = 0; i < 7; i++) {
			if ((stateVars[0] & (1 << i)) != 0) {
				outputs[i] = true;
			}
			else {
				outputs[i] = false;
			}
		}
	}
}

/*
Inputs: R, S
Outputs Q, Q'
State vars: 1, set or reset
*/
void LogicFunctions::RS_FLIP_FLOP(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	if (startOfTick) {
		if (inputs[0]) {
			stateVars[0] = false;
		}
		else if (inputs[1]){
			stateVars[0] = true;
		}
	}
	else {
		outputs[0] = stateVars[0];
		outputs[1] = !stateVars[0];
	}
}


//7-segment display patterns
//NB: in order abcdefg
bool segments[10][7] = { { 1, 1, 1, 1, 1, 1, 0 },
{ 0, 1, 1, 0, 0, 0, 0 },
{ 1, 1, 0, 1, 1, 0, 1 },
{ 1, 1, 1, 1, 0, 0, 1 },
{ 0, 1, 1, 0, 0, 1, 1 },
{ 1, 0, 1, 1, 0, 1, 1 },
{ 0, 0, 1, 1, 1, 1, 1 },
{ 1, 1, 1, 0, 0, 0, 0 },
{ 1, 1, 1, 1, 1, 1, 1 },
{ 1, 1, 1, 0, 0, 1, 1 } };

//Used for the 7-segment decoder: gets the current BCD value
int GetInputPortValue(bool *inputs) {
	int val = 0; 
	for (int i = 0; i < 4; i++) {
		if (inputs[i])
			val += (1 << i);
	}
	return val;
}

/*
Inputs: BCD A-D, /Latch, /Blank, /Lamp Test
Outputs: 7-segment display outputs in order a-g
State vars: 0 - stored value
*/
void LogicFunctions::DISPDECODER(bool *inputs, bool *outputs, int* stateVars, bool startOfTick) {
	if (startOfTick) {
		if (!inputs[4]) {
			//std::cerr << "ip: " << GetInputPortValue(inputs) << std::endl;

			stateVars[0] = GetInputPortValue(inputs);
		}
	}
	else {
		int numberToDisplay;
		if (!inputs[6]) {
			numberToDisplay = 8;
		}
		else {
			numberToDisplay = stateVars[0];
		}
		//std::cerr << numberToDisplay << std::endl;
		if (inputs[5] && (numberToDisplay >= 0) && (numberToDisplay <= 9)) {
			std::copy(segments[numberToDisplay], segments[numberToDisplay] + 7, outputs);
		}
		else {
			for (int i = 0; i < 7; i++)
				outputs[i] = false;
		}
	}
}



std::map<std::string, LogicGateInfo> LogicGate::gates = {
	{ "AND", { 2, 1, 0, LogicFunctions::AND } },
	{ "OR", { 2, 1, 0, LogicFunctions::OR } },
	{ "XOR", { 2, 1, 0, LogicFunctions::XOR } },
	{ "NOT", { 1, 1, 0, LogicFunctions::NOT } },
	{ "NAND", { 2, 1, 0, LogicFunctions::NAND } },
	{ "NOR", { 2, 1, 0, LogicFunctions::NOR } },
	{ "XNOR", { 2, 1, 0, LogicFunctions::XNOR } },
	{ "DTYPE", { 4, 2, 2, LogicFunctions::D_FLIP_FLOP } },
	{ "DCOUNTER", { 3, 11, 2, LogicFunctions::DCOUNTER } },
	{ "BRCOUNTER", { 2, 7, 2, LogicFunctions::BRCOUNTER } },
	{ "RS_FLIP_FLOP", { 2, 2, 1, LogicFunctions::RS_FLIP_FLOP } },
	{ "DISPDECODER", { 7, 7, 1, LogicFunctions::DISPDECODER } }

};