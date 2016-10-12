#include <iostream>
#include <fstream>
#include <streambuf>
#include <thread>
#include <mutex>

#include "DCSolver.h"
#include "TransientSolver.h"
#include "Net.h"
#include "Component.h"
#include "PassiveComponents.h"
#include "DiscreteSemis.h"
#include "Circuit.h"

Circuit circuit;

std::vector<std::string> lineBuffer;
std::mutex lineBufferMutex;

void interactiveTick(TransientSolver *solver) {
	std::cout << "RESULT " << solver->GetTimeAtTick(solver->GetCurrentTick()) << ",";
	for (int i = 0; i < circuit.Nets.size(); i++) {
		std::cout << solver->GetNetVoltage(circuit.Nets[i]) << ",";
	}
	for (int i = 0; i < circuit.Components.size(); i++) {
		for (int j = 0; j < circuit.Components[i]->GetNumberOfPins(); j++) {
			std::cout << solver->GetPinCurrent(circuit.Components[i], j) << ",";
		}
	}
	std::cout << std::endl;
	lineBufferMutex.lock();
	for each(std::string line in lineBuffer) {
		std::stringstream ss(line);
		
		std::string part;
		std::vector<std::string> parts;
		while (std::getline(ss, part, ' ')) {
			parts.push_back(part);	
		}
		if (parts.size() > 2) {
			if (parts[0] == "CHANGE") {
				for each(Component *c in circuit.Components) {
					if (c->ComponentID == parts[1]) {
						c->SetParameters(ParameterSet(parts));
					}
				}
			}
		}
	}
	lineBuffer.clear();
	lineBufferMutex.unlock();
}



void iothread() {
	std::string line;
	while (true) {
		std::getline(std::cin, line);
		if (line == "CONTINUE") {
			circuit.ContinueFromError = true;
		}
		else {
			lineBufferMutex.lock();
			lineBuffer.push_back(line);
			lineBufferMutex.unlock();
		}

	}
}

int main(int argc, char* argv[])
{
	std::string line = "";
	std::string netlist = "";
	char buf[2048];
	double simSpeed = 0;
	while (1) {
		std::cin.getline(buf, 2048);
		line = std::string(buf);
		if (line.find("START") != std::string::npos) {
			simSpeed = atof(line.substr(6).c_str());
			break;
		}
		netlist.append(line);
		netlist.append("\n");
	}

	circuit.ReadNetlist(netlist);
	std::cout << "VARS t,";

	for (int i = 0; i < circuit.Nets.size(); i++) {
		std::cout << "V(" << circuit.Nets[i]->NetName << "),";
	}
	for (int i = 0; i < circuit.Components.size(); i++) {
		for (int j = 0; j < circuit.Components[i]->GetNumberOfPins(); j++) {
			std::cout << "I(" << circuit.Components[i]->ComponentID << "." << j << "),";
		}
	}
	std::cout << std::endl;

	DCSolver solver(&circuit);
	bool result = false;
	try {
		result = solver.Solve();
	}
	catch (void *e){
		std::cerr << "Failed to obtain initial operating point" << std::endl;
		circuit.ReportError("EXCEPTION", true);
	}
	if (!result) {
		circuit.ReportError("CONVERGENCE", false);
	}


	TransientSolver tranSolver(solver);
	tranSolver.InteractiveCallback = interactiveTick;
	std::thread updaterThread(iothread);
	tranSolver.RunInteractive(simSpeed);
	return 0;
}

