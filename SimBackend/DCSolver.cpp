#include "DCSolver.h"

bool VariableIdentifier::operator==(VariableIdentifier& other)const {
	if (type == other.type) {
		if (type == VariableType::COMPONENT) {
			return ((component == other.component) && (pin == other.pin));
		}
		else {
			return (net == other.net);
		}
	}
	return false;
}

DCSolver::DCSolver(Circuit *circuit) {
	SolverCircuit = circuit;
	for (auto n = circuit->Nets.begin(); n != circuit->Nets.end(); ++n) {
		AddNet(*n);
	}
	for (auto c = circuit->Components.begin(); c != circuit->Components.end(); ++c) {
		AddComponent(*c);
	}
}

void DCSolver::AddComponent(Component *c) {
	//Due to Kirchoff's Laws, for a component with n pins we only need n-1 equations

	ComponentVariables[c] = nextFreeVariable;
	nextFreeVariable += c->GetNumberOfPins() - 1;

	for (int i = 0; i < c->GetNumberOfPins() - 1; i++) {
		VariableValues.push_back(0.1);
		VariableIdentifier id;
		id.type = VariableIdentifier::VariableType::COMPONENT;
		id.component = c;
		id.pin = i;
		VariableData[ComponentVariables[c] + i] = id;
	}
}

void DCSolver::AddNet(Net *net) {
	if (!net->IsFixedVoltage) {
		NetVariables[net] = nextFreeVariable;
		VariableValues.push_back(0.1);
		VariableIdentifier id;
		id.type = VariableIdentifier::VariableType::NET;
		id.net = net;
		VariableData[nextFreeVariable] = id;
		nextFreeVariable++;
	}
}

bool DCSolver::Solve(double tol, int maxIter, bool attemptRamp) {
	int n = VariableValues.size();
	double worstTol = 0;
	//The matrix to solve by Gaussian elimination for the next Newton-Raphson iteration, the rows representing functions. The first n-1 columns are
	//the Jacobian matrix of partial derivatives (each column representing a variable), and the final column is the value of -f(x) for that function
	//This is solved to find the values of (x_n+1 - x_n)
	double **matrix = new double*[n];
	int i;
	for ( i = 0; i < n; i++) matrix[i] = new double[n+1];

	for ( i = 0; i < maxIter; i++) {
		for (int j = 0; j < n; j++) {
			VariableIdentifier varData = VariableData[j];
			if (varData.type == VariableIdentifier::VariableType::COMPONENT) {
				//Set the value of -f(x)
				matrix[j][n] = -varData.component->DCFunction(this, varData.pin);
				//Populate the matrix of derivatives
				for (int k = 0; k < n; k++) {
					matrix[j][k] = varData.component->DCDerivative(this, varData.pin, VariableData[k]);
				}
			}
			else {
				//Set the value of -f(x)
				matrix[j][n] = -varData.net->DCFunction(this);
				//Populate the matrix of derivatives
				for (int k = 0; k < n; k++) {
					matrix[j][k] = varData.net->DCDerivative(this, VariableData[k]);

				}

			}
		}


		worstTol = 0;
		for (int j = 0; j < n; j++) {
			if (abs(matrix[j][n]) > worstTol)
				worstTol = abs(matrix[j][n]);
		}
		if (worstTol < tol) break;
		//Call the Newton-Raphson solver, which updates VariableValues with their new values
		Math::newtonIteration(n, &(VariableValues[0]), matrix);

	}
	for (int j = 0; j < n; j++) delete matrix[j];
	delete[] matrix;
	//If conventional Newton's method solution to find the operating point fails
	//Fixed nets are ramped up from zero volts to full in 10% steps in an attempt to find the operating point
	//This works to prevent convergence failures in unstable circuits such as oscillators
	if ((i == maxIter) && (worstTol > 1)) {
		if (attemptRamp) {

			std::cerr << "WARNING: DC simulation failed to converge (error=" << worstTol << ")" << std::endl;
			std::map<Net *, double> netVoltages;
			for each(auto net in SolverCircuit->Nets) {
				if ((net->IsFixedVoltage) && (net->NetVoltage != 0)) {
					netVoltages[net] = net->NetVoltage;
					net->NetVoltage = 0;
				}
			}
			for (int j = 0; j < VariableValues.size(); j++) {
				VariableValues[j] = 0;
			}
			Solve(tol, maxIter, false);
			TransientSolver rampSolver(*this);
			int ticks;
			ticks = rampSolver.RampUp(netVoltages);
			for (int j = 0; j < VariableValues.size(); j++) {
				VariableValues[j] = rampSolver.GetVarValue(j, ticks - 1);
			}
		}
		else {
			std::cerr << "WARNING: DC Ramp analysis OP failed to converge (error=" << worstTol << ")" << std::endl;
			return false;
		}
	}
	return true;
}

double DCSolver::GetNetVoltage(Net *n) {
	if (n->IsFixedVoltage) {
		return n->NetVoltage;
	}
	else {
		return VariableValues[NetVariables[n]];
	}
}


double DCSolver::GetPinCurrent(Component *c, int pin) {
	if (pin < c->GetNumberOfPins() - 1) {
		NetConnection conn;
		conn.component = c;
		conn.pin = pin;
		return VariableValues[ComponentVariables[c] + pin];
	}
	else {
		double sum = 0;
		for (int i = 0; i < c->GetNumberOfPins() - 1; i++) {
			sum += GetPinCurrent(c, i);
		}
		return -sum;
	}
}

