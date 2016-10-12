#include "DiscreteSemis.h"
#include "Math.h"
std::string Diode::GetComponentType() {
	return "D";
}

int Diode::GetNumberOfPins() {
	return 2;
}

void Diode::SetParameters(ParameterSet params) {
	SaturationCurrent = params.getDouble("is", SaturationCurrent);
	IdealityFactor = params.getDouble("n", IdealityFactor);
	SeriesResistance = params.getDouble("rser", SeriesResistance);
}

// f0: Is * (e ^ ((Vd - IRs)/(n*Vt)) - 1) - I

double Diode::DCFunction(DCSolver *solver, int f) {
	if (f == 0) {
		double L = SaturationCurrent *
				(Math::exp_safe(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1])) - SeriesResistance * solver->GetPinCurrent(this, 0)) / (IdealityFactor * Math::vTherm)) - 1);
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else {
		return 0;
	}
}

double Diode::TransientFunction(TransientSolver *solver, int f) {
	if (f == 0) {
		double L = SaturationCurrent *
			(Math::exp_safe(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1])) - SeriesResistance * solver->GetPinCurrent(this, 0)) / (IdealityFactor * Math::vTherm)) - 1);
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else {
		return 0;
	}
}

double Diode::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm));
			}
			else if (var.net == PinConnections[1]) {
				return -1 * SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm));

			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return -1 * SeriesResistance * SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm)) - 1;
			}
		}	
	}
	return 0;
}

double Diode::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm));
			}
			else if (var.net == PinConnections[1]) {
				return -1 * SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm));

			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return -1 * SeriesResistance * SaturationCurrent * (1 / (IdealityFactor*Math::vTherm)) * Math::exp_deriv(((solver->GetNetVoltage(PinConnections[0]) - solver->GetNetVoltage(PinConnections[1]) - SeriesResistance * solver->GetPinCurrent(this, 0))) / (IdealityFactor*Math::vTherm)) - 1;
			}
		}
	}
	return 0;
}
