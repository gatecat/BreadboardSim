#include "Component.h"

VariableIdentifier Component::getComponentVariableIdentifier(int pin) {
	VariableIdentifier id;
	id.type = VariableIdentifier::VariableType::COMPONENT;
	id.component = this;
	id.pin = pin;
	return id;
}

void Component::SetParameters(ParameterSet params) {

}