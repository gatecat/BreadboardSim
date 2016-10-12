#include "PassiveComponents.h"

std::string Capacitor::GetComponentType() {
	return "C";
}

int Capacitor::GetNumberOfPins() {
	return 2;
}

void Capacitor::SetParameters(ParameterSet params) {
	Capacitance = params.getDouble("cap", Capacitance);
	SeriesResistance = params.getDouble("rser", SeriesResistance);
}

double Capacitor::DCFunction(DCSolver *solver, int f) {
	if (f == 0) {
		double L = (solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1])) / DCResistance;
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else {
		return 0;
	}
}

double Capacitor::TransientFunction(TransientSolver *solver, int f) {
	if (f == 0) {

		double I0 = solver->GetPinCurrent(this, 0, solver->GetCurrentTick() - 1);
		double I = solver->GetPinCurrent(this, 0);
		double V0 = solver->GetNetVoltage(PinConnections[0], solver->GetCurrentTick() - 1) - solver->GetNetVoltage(PinConnections[1], solver->GetCurrentTick() - 1) - SeriesResistance * I0;
		double V = solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * I;
		double DT = solver->GetTimeAtTick(solver->GetCurrentTick()) - solver->GetTimeAtTick(solver->GetCurrentTick() - 1);

		double L = V;
		double R;
		if (solver->GetTimeAtTick(solver->GetCurrentTick()) != lastT) {
			lastT = solver->GetTimeAtTick(solver->GetCurrentTick());
			usingBE = false;
		}
		if ((fabs(V - V0) > 0.5) || usingBE) {
			usingBE = true;
		}
		else {
			R = V0 + DT * (I0 + I) / (2 * Capacitance);
		}
		R = V0 + (DT / Capacitance) * I;

		solver->RequestTimestep(fmax(abs((0.05*Capacitance) / ((I + I0) * DT)), 1e-10));

		return L - R;


	}
	else {
		return 0;
	}
}

double Capacitor::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) return 1  / DCResistance;
			if (var.net == PinConnections[1]) return -1 / DCResistance;
		}
		else {
			if ((var.component == this) && (var.pin == 0)) return -1;
		}
	}
	return 0;
}

double Capacitor::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		double V0 = solver->GetNetVoltage(PinConnections[0], solver->GetCurrentTick() - 1) - solver->GetNetVoltage(PinConnections[1], solver->GetCurrentTick() - 1);
		double V = solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]);
		double I0 = solver->GetPinCurrent(this, 0, solver->GetCurrentTick() - 1);
		double I = solver->GetPinCurrent(this, 0);
		double DT = solver->GetTimeAtTick(solver->GetCurrentTick()) - solver->GetTimeAtTick(solver->GetCurrentTick() - 1);
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return 1;
			}
			else if (var.net == PinConnections[1]) {
				return -1;
			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				if (usingBE) {
					return -SeriesResistance - DT / Capacitance;
				}
				else {
					return -SeriesResistance - DT / (2 * Capacitance);
				}
				
			}
		}
	}
	return 0;
}
