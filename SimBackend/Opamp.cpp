#include "Opamp.h"
std::string Opamp::GetComponentType() {
	return "OPAMP";
}

int Opamp::GetNumberOfPins() {
	return 5;
}

void Opamp::SetParameters(ParameterSet params) {
	InputResistance = params.getDouble("rin", InputResistance);
	OpenLoopGain = params.getDouble("aol", OpenLoopGain);
	VosatP = params.getDouble("vosatp", VosatP);
	VosatN = params.getDouble("vosatn", VosatN);
	OutputResistance = params.getDouble("rout", OutputResistance);
}


double Opamp::DCFunction(DCSolver *solver, int f) {
	double Vsp = solver->GetNetVoltage(PinConnections[4]);
	double Vsm = solver->GetNetVoltage(PinConnections[3]);
	double NinvInp = solver->GetNetVoltage(PinConnections[0]);
	double InvInp = solver->GetNetVoltage(PinConnections[1]);
	double Vout = solver->GetNetVoltage(PinConnections[2]);
	double Iinp = solver->GetPinCurrent(this, 0);
	double Iinn = solver->GetPinCurrent(this, 1);
	double Iout = solver->GetPinCurrent(this, 2);
	double Ignd = solver->GetPinCurrent(this, 3);

	double Vo = ((Vsp - Vsm - VosatP - VosatN) / 2) * tanh(OpenLoopGain * (NinvInp - InvInp)) + ((Vsp + Vsm - VosatP + VosatN) / 2) + Iout * OutputResistance;

	if (f == 0) {
		double L = (NinvInp - InvInp) / InputResistance;
		double R = Iinp;
		return L - R;
	}
	else if (f == 1) {
		double L = -(NinvInp - InvInp) / InputResistance;
		double R = Iinn;
		return L - R;
	}
	else if (f == 2){
		double L = Vout;
		double R = Vo;
		return L - R;
	}
	else if (f == 3) {
		if (Iout > 0) {
			double L = Ignd;
			double R = -Iout-Iq;
			return L - R;
		}
		else {
			double L = Ignd;
			double R = -Iq;
			return L - R;
		}
	}
	else {
		return 0;
	}
}

double Opamp::TransientFunction(TransientSolver *solver, int f) {

	double Vsp = solver->GetNetVoltage(PinConnections[4]);
	double Vsm = solver->GetNetVoltage(PinConnections[3]);
	double NinvInp = solver->GetNetVoltage(PinConnections[0]);

	if (abs(NinvInp - LastVinp) > 0.1) {
		solver->SetNetVoltageGuess(PinConnections[1], solver->GetNetVoltage(PinConnections[0]));

	}
	LastVinp = NinvInp;
	double InvInp = solver->GetNetVoltage(PinConnections[1]);


	double Vout = solver->GetNetVoltage(PinConnections[2]);
	double Iinp = solver->GetPinCurrent(this, 0);
	double Iinn = solver->GetPinCurrent(this, 1);
	double Iout = solver->GetPinCurrent(this, 2);
	double Ignd = solver->GetPinCurrent(this, 3);

	double Vo = ((Vsp - Vsm - VosatP - VosatN) / 2) * tanh(OpenLoopGain * (NinvInp - InvInp)) + ((Vsp + Vsm - VosatP + VosatN) / 2) + Iout * OutputResistance;
	if (f == 0) {
		double L = (NinvInp - InvInp) / InputResistance;
		double R = Iinp;
		return L - R;
	}
	else if (f == 1) {
		double L = -(NinvInp - InvInp) / InputResistance;
		double R = Iinn;
		return L - R;
	}
	else if (f == 2){
		double L = Vout;
		double R = Vo;
		return L - R;
	}
	else if (f == 3) {
		if (Iout > 0) {
			double L = Ignd;
			double R = -Iout-Iq;
			return L - R;
		}
		else {
			double L = Ignd;
			double R =-Iq;
			return L - R;
		}
	}
	else {
		return 0;
	}
}

double Opamp::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	double Vsp = solver->GetNetVoltage(PinConnections[4]);
	double Vsm = solver->GetNetVoltage(PinConnections[3]);
	double NinvInp = solver->GetNetVoltage(PinConnections[0]);
	double InvInp = solver->GetNetVoltage(PinConnections[1]);
	double Vout = solver->GetNetVoltage(PinConnections[2]);
	double Iinp = solver->GetPinCurrent(this, 0);
	double Iinn = solver->GetPinCurrent(this, 1);
	double Iout = solver->GetPinCurrent(this, 2);
	double Ignd = solver->GetPinCurrent(this, 3);


	if (f == 0) {
		if (var.type == var.NET && var.net == PinConnections[0])
			return 1 / InputResistance;
		if (var.type == var.NET && var.net == PinConnections[1])
			return -1 / InputResistance;
		if (var.type == var.COMPONENT && var.component == this && var.pin == 0)
			return -1;
	}
	else if (f == 1) {
		if (var.type == var.NET && var.net == PinConnections[0])
			return -1 / InputResistance;
		if (var.type == var.NET && var.net == PinConnections[1])
			return 1 / InputResistance;
		if (var.type == var.COMPONENT && var.component == this && var.pin == 1)
			return -1;
	}
	else if (f == 2){
		if (var.type == var.NET && var.net == PinConnections[2])
			return 1;
		if (var.type == var.NET && var.net == PinConnections[0])
			return -(((Vsp - Vsm - VosatP - VosatN) / 2) * (OpenLoopGain)* (1 - pow(tanh(OpenLoopGain*(NinvInp - InvInp)), 2)));
		if (var.type == var.NET && var.net == PinConnections[1])
			return (((Vsp - Vsm - VosatP - VosatN) / 2) * (OpenLoopGain)* (1 - pow(tanh(OpenLoopGain*(NinvInp - InvInp)), 2)));

		if (var.type == var.NET && var.net == PinConnections[3])
			return (1.0 / 2.0) * tanh(OpenLoopGain * (NinvInp - InvInp)) - (1.0 / 2.0);
		if (var.type == var.NET && var.net == PinConnections[4])
			return (-1.0 / 2.0) * tanh(OpenLoopGain * (NinvInp - InvInp)) - (1.0 / 2.0);
		if (var.type == var.COMPONENT && var.component == this && var.pin == 2)
			return -OutputResistance;

	}
	else if (f == 3) {
		if (solver->GetPinCurrent(this, 2) > 0) {
			if (var.type == var.COMPONENT && var.component == this && var.pin == 3)
				return 1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == 2)
				return 1;
		}
		else {
			if (var.type == var.COMPONENT && var.component == this && var.pin == 3)
				return 1;
		}
	}

	return 0;
}

double Opamp::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	double Vsp = solver->GetNetVoltage(PinConnections[4]);
	double Vsm = solver->GetNetVoltage(PinConnections[3]);
	double NinvInp = solver->GetNetVoltage(PinConnections[0]);
	double InvInp = solver->GetNetVoltage(PinConnections[1]);
	double Vout = solver->GetNetVoltage(PinConnections[2]);
	double Iinp = solver->GetPinCurrent(this, 0);
	double Iinn = solver->GetPinCurrent(this, 1);
	double Iout = solver->GetPinCurrent(this, 2);
	double Ignd = solver->GetPinCurrent(this, 3);


	if (f == 0) {
		if (var.type == var.NET && var.net == PinConnections[0])
			return 1 / InputResistance;
		if (var.type == var.NET && var.net == PinConnections[1])
			return -1 / InputResistance;
		if (var.type == var.COMPONENT && var.component == this && var.pin == 0)
			return -1;
	}
	else if (f == 1) {
		if (var.type == var.NET && var.net == PinConnections[0])
			return -1 / InputResistance;
		if (var.type == var.NET && var.net == PinConnections[1])
			return 1 / InputResistance;
		if (var.type == var.COMPONENT && var.component == this && var.pin == 1)
			return -1;
	}
	else if (f == 2){
		if (var.type == var.NET && var.net == PinConnections[2])
			return 1;
		if (var.type == var.NET && var.net == PinConnections[0])
			return -(((Vsp - Vsm - VosatP - VosatN) / 2) * (OpenLoopGain)* (1 - pow(tanh(OpenLoopGain*(NinvInp - InvInp)), 2)));
		if (var.type == var.NET && var.net == PinConnections[1])
			return (((Vsp - Vsm - VosatP - VosatN) / 2) * (OpenLoopGain)* (1 - pow(tanh(OpenLoopGain*(NinvInp - InvInp)), 2)));

		if (var.type == var.NET && var.net == PinConnections[3])
			return (1.0 / 2.0) * tanh(OpenLoopGain * (NinvInp - InvInp)) - (1.0 / 2.0);
		if (var.type == var.NET && var.net == PinConnections[4])
			return (-1.0 / 2.0) * tanh(OpenLoopGain * (NinvInp - InvInp)) - (1.0 / 2.0);
		if (var.type == var.COMPONENT && var.component == this && var.pin == 2)
			return -OutputResistance;

	}
	else if (f == 3) {
		if (solver->GetPinCurrent(this, 2) > 0) {
			if (var.type == var.COMPONENT && var.component == this && var.pin == 3)
				return 1;
			if (var.type == var.COMPONENT && var.component == this && var.pin == 2)
				return 1;
		}
		else {
			if (var.type == var.COMPONENT && var.component == this && var.pin == 3)
				return 1;
		}
	}

	return 0;
}