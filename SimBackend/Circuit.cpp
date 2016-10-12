#include "Circuit.h"
#include "PassiveComponents.h"
#include "DiscreteSemis.h"
#include "LogicGates.h"

#include "Opamp.h"
Circuit::Circuit()
{

}

void Circuit::ReadNetlist(std::string data)
{
	//Split string into lines
	std::stringstream ss1(data);
	std::string line;
	while (std::getline(ss1, line)) {
		std::stringstream ss2(line);
		std::string part;
		std::vector<std::string> parts;
		while (std::getline(ss2, part,' ')) {
			parts.push_back(part);
		}
		if (parts.size() >= 2) {
			if (parts[0] == "NET") {
				Net *n = new Net();
				n->NetName = parts[1];
				if (parts.size() >= 3) {
					n->IsFixedVoltage = true;
					n->NetVoltage = atof(parts[2].c_str());
				}
				Nets.push_back(n);
			}

			else if (parts[0] == "RES") {
				AddComponent(parts, new Resistor());
			}
			else if (parts[0] == "CAP") {
				AddComponent(parts, new Capacitor());
			}
			else if (parts[0] == "DIODE") {
				AddComponent(parts, new Diode());
			}
			else if (parts[0] == "BJT") {
				AddComponent(parts, new BJT());
			}
			else if (parts[0] == "NMOS") {
				AddComponent(parts, new NMOS());
			}
			else if (parts[0] == "OPAMP") {
				AddComponent(parts, new Opamp());
			}
			else if (parts[0].find("LOGIC_") == 0) {
				AddComponent(parts, new LogicGate(parts[0].substr(6)));
			}
			else {
				std::cerr << "WARNING : Unknown component type " << parts[0] << std::endl;
			}
		}
	}
}

void Circuit::AddComponent(std::vector<std::string> parts, Component *c) {
	c->ComponentID = parts[1];
	if (parts.size() >= (c->GetNumberOfPins() + 1)) {
		for (int i = 0; i < c->GetNumberOfPins(); i++) {
			std::string netName = parts[i + 2];
			Net *net;
			bool foundNet = false;
			for (auto a = Nets.begin(); a != Nets.end(); ++a) {
				if ((*a)->NetName == netName) {
					net = *a;
					foundNet = true;
					break;
				}
			}
			if (!foundNet) {
				net = new Net();
				net->NetName = netName;
				Nets.push_back(net);
			}
			c->PinConnections.push_back(net); 
			NetConnection conn;
			conn.component = c;
			conn.pin = i;
			net->connections.push_back(conn);
		}
	}
	c->SetParameters(ParameterSet(parts));
	Components.push_back(c);
}
bool reportedConvergenceFail = false;
void Circuit::ReportError(std::string desc, bool fatal) {
	//Only report a convergence failure once
	if (desc == "CONVERGENCE") {
		if (reportedConvergenceFail)
			return;
		reportedConvergenceFail = true;
	}

	std::cout << std::endl << "ERROR " << (fatal ? 0 : 1) << "," << desc << std::endl;
	if (fatal) {
		while (true);
	}
	else {
		ContinueFromError = false;
		std::string str;
		while (!ContinueFromError) {
			_sleep(100);
		}
	}
}