#include "Net.h"

double Net::DCFunction(DCSolver *solver) {
	double sum = 0;
	for (auto iter = connections.begin(); iter != connections.end(); ++iter) {
		sum -= solver->GetPinCurrent(iter->component, iter->pin);
	}
	return sum;
}

double Net::DCDerivative(DCSolver *solver, VariableIdentifier var) {
	if (var.type == VariableIdentifier::VariableType::NET) return 0;
	double deriv = 0;

	for (auto iter = connections.begin(); iter != connections.end(); ++iter) {
		if (var == iter->component->getComponentVariableIdentifier(iter->pin))
			deriv += -1;
		if (iter->component == var.component) {
			if (iter->pin == iter->component->GetNumberOfPins() - 1) {
				if (var.pin < iter->component->GetNumberOfPins() - 1)
					deriv += 1;
			}
		}
	}

	return deriv;
}

double Net::TransientFunction(TransientSolver *solver) {
	double sum = 0;
	for (auto iter = connections.begin(); iter != connections.end(); ++iter) {
		sum -= solver->GetPinCurrent(iter->component, iter->pin);
	}
	return sum;
}

double Net::TransientDerivative(TransientSolver *solver, VariableIdentifier var) {
	if (var.type == VariableIdentifier::VariableType::NET) return 0;
	double deriv = 0;

	for (auto iter = connections.begin(); iter != connections.end(); ++iter) {
		if (var == iter->component->getComponentVariableIdentifier(iter->pin))
			deriv += -1;
		if (iter->component == var.component) {
			if (iter->pin == iter->component->GetNumberOfPins() - 1) {
				if (var.pin < iter->component->GetNumberOfPins() - 1)
					deriv += 1;
			}
		}
	}

	return deriv;
}

VariableIdentifier Net::GetNetVariableIdentifier() {
	VariableIdentifier id;
	id.type = VariableIdentifier::VariableType::NET;
	id.net = this;
	return id;
}