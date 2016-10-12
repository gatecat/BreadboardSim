#include "DiscreteSemis.h"


std::string BJT::GetComponentType() {
	return "BJT";
}

int BJT::GetNumberOfPins() {
	return 3;
}

void BJT::SetParameters(ParameterSet params) {
	SaturationCurrent = params.getDouble("is", SaturationCurrent);
	ForwardGain = params.getDouble("bf", ForwardGain);
	ReverseGain = params.getDouble("br", ReverseGain);
	
	Rcollector = params.getDouble("rc", Rcollector);
	Rbase = params.getDouble("rb", Rbase);
	Remitter = params.getDouble("re", Remitter);

	if (strToLower(params.getString("type", "npn")) == "pnp") {
		MakePNP();
	}
}

//Vbe = (Vb - Ib*Rb) - (Ve - Ie * Re)
//Vbc = (Vb - Ib*Rb) - (Vc - Ic * Rc)


double BJT::DCFunction(DCSolver *solver, int f) {
	double Vbe = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[2]) - Remitter * solver->GetPinCurrent(this, 2));
	double Vbc = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[0]) - Rcollector * solver->GetPinCurrent(this, 0));


	if (f == 0) {
		double L = SaturationCurrent * ((Math::exp_safe(Vbe / GetVt()) - Math::exp_safe(Vbc / GetVt())) - (1 / ReverseGain) * (Math::exp_safe(Vbc / GetVt()) - 1));
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else if (f == 1) {
		double L = SaturationCurrent * ((1 / ForwardGain) * (Math::exp_safe(Vbe / GetVt()) - 1) + (1 / ReverseGain) * (Math::exp_safe(Vbc / GetVt()) - 1));
		double R = solver->GetPinCurrent(this, 1);
		return L - R;
	}
	else {
		return 0;
	}
}

double BJT::TransientFunction(TransientSolver *solver, int f) {
	double Vbe = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[2]) - Remitter * solver->GetPinCurrent(this, 2));
	double Vbc = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[0]) - Rcollector * solver->GetPinCurrent(this, 0));


	if (f == 0) {
		double L = SaturationCurrent * ((Math::exp_safe(Vbe / GetVt()) - Math::exp_safe(Vbc / GetVt())) - (1 / ReverseGain) * (Math::exp_safe(Vbc / GetVt()) - 1));
		double R = solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else if (f == 1) {
		double L = SaturationCurrent * ((1 / ForwardGain) * (Math::exp_safe(Vbe / GetVt()) - 1) + (1 / ReverseGain) * (Math::exp_safe(Vbc / GetVt()) - 1));
		double R = solver->GetPinCurrent(this, 1);
		return L - R;
	}
	else {
		return 0;
	}
}

double BJT::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	double Vbe = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[2]) - Remitter * solver->GetPinCurrent(this, 2));
	double Vbc = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[0]) - Rcollector * solver->GetPinCurrent(this, 0));

	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return SaturationCurrent * (-1 / GetVt()) * -1 * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (-1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[1]) {
				return SaturationCurrent * (1 / GetVt()) * Math::exp_deriv(Vbe / GetVt()) - SaturationCurrent * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[2]) {
				return SaturationCurrent * (-1 / GetVt()) * Math::exp_deriv(Vbe / GetVt());
			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return SaturationCurrent * (Rcollector / GetVt()) * -1 * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (Rcollector / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - 1;
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return SaturationCurrent * (-Rbase / GetVt()) * Math::exp_deriv(Vbe / GetVt()) - SaturationCurrent * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
		}
	}
	else if (f == 1) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return (SaturationCurrent / ReverseGain) * (-1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[1]) {
				return (SaturationCurrent / ForwardGain) * (1 / GetVt()) * Math::exp_deriv(Vbe / GetVt()) + (SaturationCurrent / ReverseGain) * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt()) ;
			}
			else if (var.net == PinConnections[2]){
				return (SaturationCurrent / ForwardGain) * (-1 / GetVt()) * Math::exp_deriv(Vbe / GetVt());
			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return (SaturationCurrent / ReverseGain) * (Rcollector / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return (SaturationCurrent / ForwardGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbe / GetVt()) + (SaturationCurrent / ReverseGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - 1;
			}
		}
	}
	return 0;
}

double BJT::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	double Vbe = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[2]) - Remitter * solver->GetPinCurrent(this, 2));
	double Vbc = (solver->GetNetVoltage(PinConnections[1]) - Rbase * solver->GetPinCurrent(this, 1)) - (solver->GetNetVoltage(PinConnections[0]) - Rcollector * solver->GetPinCurrent(this, 0));

	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return SaturationCurrent * (-1 / GetVt()) * -1 * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (-1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[1]) {
				return SaturationCurrent * (1 / GetVt()) * Math::exp_deriv(Vbe / GetVt()) - SaturationCurrent * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[2]) {
				return SaturationCurrent * (-1 / GetVt()) * Math::exp_deriv(Vbe / GetVt());
			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return SaturationCurrent * (Rcollector / GetVt()) * -1 * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (Rcollector / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - 1;
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return SaturationCurrent * (-Rbase / GetVt()) * Math::exp_deriv(Vbe / GetVt()) - SaturationCurrent * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - (SaturationCurrent / ReverseGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
		}
	}
	else if (f == 1) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return (SaturationCurrent / ReverseGain) * (-1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[1]) {
				return (SaturationCurrent / ForwardGain) * (1 / GetVt()) * Math::exp_deriv(Vbe / GetVt()) + (SaturationCurrent / ReverseGain) * (1 / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if (var.net == PinConnections[2]){
				return (SaturationCurrent / ForwardGain) * (-1 / GetVt()) * Math::exp_deriv(Vbe / GetVt());
			}
		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return (SaturationCurrent / ReverseGain) * (Rcollector / GetVt()) * Math::exp_deriv(Vbc / GetVt());
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return (SaturationCurrent / ForwardGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbe / GetVt()) + (SaturationCurrent / ReverseGain) * (-Rbase / GetVt()) * Math::exp_deriv(Vbc / GetVt()) - 1;
			}
		}
	}
	return 0;
}

void BJT::MakePNP() {
	IsPNP = true;
	SaturationCurrent = -SaturationCurrent; 
}

double BJT::GetVt() {
	if (IsPNP)
		return -Math::vTherm;
	else
		return Math::vTherm;
}