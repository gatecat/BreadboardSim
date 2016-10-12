#include "PassiveComponents.h"

std::string Resistor::GetComponentType() {
	return "R";
}

int Resistor::GetNumberOfPins() {
	return 2;
}

void Resistor::SetParameters(ParameterSet params) {
	Resistance = params.getDouble("res", Resistance);
	//std::cerr << "res of " << ComponentID << " is " << resistance << std::endl;
}

// f0: (V1 - V2) / R - I

double Resistor::DCFunction(DCSolver *solver, int f) {
	if (f == 0) {
		double L = (solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1])) / Resistance;
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else {
		return 0;
	}	
}

double Resistor::TransientFunction(TransientSolver *solver, int f) {
	if (f == 0) {
		double L = (solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1])) / Resistance;
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else {
		return 0;
	}
}

double Resistor::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) return 1 / Resistance;
			if (var.net == PinConnections[1]) return -1 / Resistance;
		}
		else {
			if ((var.component == this) && (var.pin == 0)) return -1;
		}
	}
	return 0;
}

double Resistor::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) return 1 / Resistance;
			if (var.net == PinConnections[1]) return -1 / Resistance;
		}
		else {
			if ((var.component == this) && (var.pin == 0)) return -1;
		}
	}
	return 0;
}
