#include "DiscreteSemis.h"


std::string NMOS::GetComponentType() {
	return "NMOS";
}

int NMOS::GetNumberOfPins() {
	return 3;
}

void NMOS::SetParameters(ParameterSet params) {
	K = params.getDouble("k", K);
	lambda = params.getDouble("lambda", lambda);
	Vth = params.getDouble("vth", Vth);

	Rgs = params.getDouble("rgs", Rgs);
}


double NMOS::DCFunction(DCSolver *solver, int f) {
	double Vgs = solver->GetNetVoltage(PinConnections[1]) - solver->GetNetVoltage(PinConnections[0]);
	double Vds = solver->GetNetVoltage(PinConnections[2]) - solver->GetNetVoltage(PinConnections[0]);
	if (f == 0) {
		double L = 0;
		if (Vgs < Vth) {
			L = solver->GetPinCurrent(this, 1);
		}
		else {
			if (/*Vds > 0*/true) {
				if (Vds < (Vgs - Vth)) {
					L = K * ((Vgs - Vth) * Vds - (pow(Vds, 2) / 2)) * (1 + lambda * abs(Vds)) + solver->GetPinCurrent(this, 1);
				}
				else {
					L = (K / 2) * pow(Vgs - Vth, 2) * (1 + lambda * abs(Vds)) + solver->GetPinCurrent(this, 1);
				}
			}
			else {
				L = solver->GetPinCurrent(this, 1);
			}
		}
		double R = -solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else if (f == 1) {
		double L = solver->GetPinCurrent(this, 1);
		double R = (1.0 / Rgs) * Vgs;
		return L - R;
	}
	else {
		return 0;
	}
}

double NMOS::TransientFunction(TransientSolver *solver, int f) {
	double Vgs = solver->GetNetVoltage(PinConnections[1]) - solver->GetNetVoltage(PinConnections[0]);
	double Vds = solver->GetNetVoltage(PinConnections[2]) - solver->GetNetVoltage(PinConnections[0]);

	if (f == 0) {
		double L = 0;
		if (Vgs < Vth) {
			L = solver->GetPinCurrent(this, 1);
		}
		else {
			if (/*Vds > 0*/true) {
				if (Vds < (Vgs - Vth)) {
					L = K * ((Vgs - Vth) * Vds - (pow(Vds, 2) / 2)) * (1 + lambda * abs(Vds)) + solver->GetPinCurrent(this, 1);
				}
				else {
					L = (K / 2) * pow(Vgs - Vth, 2) * (1 + lambda * abs(Vds)) + solver->GetPinCurrent(this, 1);
				}
			}
			else {
				L = solver->GetPinCurrent(this, 1);
			}
		}
		double R = -solver->GetPinCurrent(this, 0);
		return L - R;
	}
	else if (f == 1) {
		double L = solver->GetPinCurrent(this, 1);
		double R = (1.0 / Rgs) * Vgs;
		return L - R;
	}
	else {
		return 0;
	}
}

double NMOS::DCDerivative(DCSolver *solver, int f, VariableIdentifier var) {
	double Vs = solver->GetNetVoltage(PinConnections[0]);
	double Vg = solver->GetNetVoltage(PinConnections[1]);
	double Vd = solver->GetNetVoltage(PinConnections[2]);

	double Vgs = Vg - Vs;
	double Vds = Vd - Vs;

	double lambda_c = lambda;
	//To compensate for the absolute term in the equation, lambda must be made negative is Vds < 0
	if (Vds < 0) {
		lambda_c = -lambda;
	}

	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if ((Vgs > Vth) && (/*Vds > 0*/true)) {
				if (Vds < (Vgs - Vth)) {
					if (var.net == PinConnections[0]) {
						return K * (-Vg + Vs + Vth - lambda_c * Vd * Vg + lambda_c * Vs * Vd + lambda_c * Vd * Vth - lambda_c * Vg * Vd + lambda_c * Vth * Vd + 2 * lambda_c * Vs * Vg
							- (3.0 / 2.0) * lambda_c * pow(Vs, 2) + lambda_c * Vth * Vd - 2 * lambda_c * Vs * Vth + (1.0 / 2.0) * lambda_c * pow(Vd, 2));
						 
					}
					else if (var.net == PinConnections[1]) {
						return K * (Vd - Vs + lambda_c * pow(Vd, 2) - lambda_c * Vd * Vs - lambda_c * Vs * Vd + lambda_c * pow(Vs, 2));
					}
					else if (var.net == PinConnections[2]) {
						return K * (Vg - Vth - Vd + 2 * lambda_c * Vg * Vd - 2 * lambda_c * Vth * Vd - lambda_c * Vs * Vg + (1.0 / 2.0) * lambda_c * pow(Vs, 2)
							+ lambda_c * Vs * Vth - (3.0 / 2.0) * lambda_c * pow(Vd, 2) + lambda_c * Vs * Vth + lambda_c * Vd * Vs);
					}

					//L = K * ((Vgs - Vth) * Vds - (pow(Vds, 2) / 2)) * (1 + lambda * Vds)
				}
				else {
					//L = (K / 2) * pow(Vgs - Vth, 2) * (1 + lambda * Vds) 
					if (var.net == PinConnections[0]) {
						return (K / 2.0) * (2 * Vs - 2 * Vg + 2 * Vth + 2 * lambda_c * Vd * Vs - 2 * lambda_c * Vd * Vg + 2 * lambda_c * Vth - lambda_c * Vg - 3 * lambda_c * pow(Vs, 2)
							- lambda_c * pow(Vth, 2) - 4 * lambda_c * Vs * Vg - 2 * lambda_c * Vg * Vth - 4 * lambda_c * Vth);

					}
					else if (var.net == PinConnections[1]) {
						return (K / 2.0) * (2 * Vg - 2 * Vs - 2 * Vth + 2 * lambda_c * Vd * Vg + 2 * lambda_c * Vs * Vd - 2 * lambda_c * Vd * Vth - lambda_c * Vs
							- 2 * lambda_c * pow(Vs, 2) - 2 * lambda_c * Vg * Vth);
					}
					else if (var.net == PinConnections[2]) {
						return (K / 2.0) * (lambda_c * pow(Vg, 2) + lambda_c * pow(Vth, 2) - 2 * lambda_c * Vs * Vg - 2 * lambda_c * Vg * Vth);
					}
				}
			}

		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return 1;
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return 1;
			}
		}
	}
	else if (f == 1) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return (1.0 / Rgs);
			}
			else if (var.net == PinConnections[1]) {
				return -(1.0 / Rgs);
			}
		} 
		else {
			if ((var.component == this) && (var.pin == 1)) {
				return 1;
			}
		}
	}
	return 0;
}

double NMOS::TransientDerivative(TransientSolver *solver, int f, VariableIdentifier var) {
	double Vs = solver->GetNetVoltage(PinConnections[0]);
	double Vg = solver->GetNetVoltage(PinConnections[1]);
	double Vd = solver->GetNetVoltage(PinConnections[2]);

	double Vgs = Vg - Vs;
	double Vds = Vd - Vs;

	double lambda_c = lambda;
	//To compensate for the absolute term in the equation, lambda must be made negative is Vds < 0
	if (Vds < 0) {
		lambda_c = -lambda;
	}

	if (f == 0) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if ((Vgs > Vth) && (/*Vds > 0*/true)) {
				if (Vds < (Vgs - Vth)) {
					if (var.net == PinConnections[0]) {
						return K * (-Vg + Vs + Vth - lambda_c * Vd * Vg + lambda_c * Vs * Vd + lambda_c * Vd * Vth - lambda_c * Vg * Vd + lambda_c * Vth * Vd + 2 * lambda_c * Vs * Vg
							- (3.0 / 2.0) * lambda_c * pow(Vs, 2) + lambda_c * Vth * Vd - 2 * lambda_c * Vs * Vth + (1.0 / 2.0) * lambda_c * pow(Vd, 2));

					}
					else if (var.net == PinConnections[1]) {
						return K * (Vd - Vs + lambda_c * pow(Vd, 2) - lambda_c * Vd * Vs - lambda_c * Vs * Vd + lambda_c * pow(Vs, 2));
					}
					else if (var.net == PinConnections[2]) {
						return K * (Vg - Vth - Vd + 2 * lambda_c * Vg * Vd - 2 * lambda_c * Vth * Vd - lambda_c * Vs * Vg + (1.0 / 2.0) * lambda_c * pow(Vs, 2)
							+ lambda_c * Vs * Vth - (3.0 / 2.0) * lambda_c * pow(Vd, 2) + lambda_c * Vs * Vth + lambda_c * Vd * Vs);
					}

					//L = K * ((Vgs - Vth) * Vds - (pow(Vds, 2) / 2)) * (1 + lambda * Vds)
				}
				else {
					//L = (K / 2) * pow(Vgs - Vth, 2) * (1 + lambda * Vds) 
					if (var.net == PinConnections[0]) {
						return (K / 2.0) * (2 * Vs - 2 * Vg + 2 * Vth + 2 * lambda_c * Vd * Vs - 2 * lambda_c * Vd * Vg + 2 * lambda_c * Vth - lambda_c * Vg - 3 * lambda_c * pow(Vs, 2)
							- lambda_c * pow(Vth, 2) - 4 * lambda_c * Vs * Vg - 2 * lambda_c * Vg * Vth - 4 * lambda_c * Vth);

					}
					else if (var.net == PinConnections[1]) {
						return (K / 2.0) * (2 * Vg - 2 * Vs - 2 * Vth + 2 * lambda_c * Vd * Vg + 2 * lambda_c * Vs * Vd - 2 * lambda_c * Vd * Vth - lambda_c * Vs
							- 2 * lambda_c * pow(Vs, 2) - 2 * lambda_c * Vg * Vth);
					}
					else if (var.net == PinConnections[2]) {
						return (K / 2.0) * (lambda_c * pow(Vg, 2) + lambda_c * pow(Vth, 2) - 2 * lambda_c * Vs * Vg - 2 * lambda_c * Vg * Vth);
					}
				}
			}

		}
		else {
			if ((var.component == this) && (var.pin == 0)) {
				return 1;
			}
			else if ((var.component == this) && (var.pin == 1)) {
				return 1;
			}
		}
	}
	else if (f == 1) {
		if (var.type == VariableIdentifier::VariableType::NET) {
			if (var.net == PinConnections[0]) {
				return (1.0 / Rgs);
			}
			else if (var.net == PinConnections[1]) {
				return -(1.0 / Rgs);
			}
		}
		else {
			if ((var.component == this) && (var.pin == 1)) {
				return 1;
			}
		}
	}
	return 0;
}
