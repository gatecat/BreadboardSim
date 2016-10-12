#include "TransientSolver.h"
#include <Windows.h>
TransientSolver::TransientSolver()
{
	VariableValues.push_back(std::vector<double>());
	times.push_back(0);
}

TransientSolver::TransientSolver(DCSolver init)
{
	NetVariables = init.NetVariables;
	ComponentVariables = init.ComponentVariables;
	VariableData = init.VariableData;
	VariableValues.push_back(init.VariableValues); 
	SolverCircuit = init.SolverCircuit;
	times.push_back(0);
}


void TransientSolver::AddComponent(Component *c) {
	//Due to Kirchoff's Laws, for a component with n pins we only need n-1 equations

	ComponentVariables[c] = nextFreeVariable;
	nextFreeVariable += c->GetNumberOfPins() - 1;

	for (int i = 0; i < c->GetNumberOfPins() - 1; i++) {
		VariableValues[0].push_back(0);
		VariableIdentifier id;
		id.type = VariableIdentifier::VariableType::COMPONENT;
		id.component = c;
		id.pin = i;
		VariableData[ComponentVariables[c] + i] = id;
	}
}

void TransientSolver::AddNet(Net *net) {
	NetVariables[net] = nextFreeVariable;
	VariableValues[0].push_back(0);
	VariableIdentifier id;
	id.type = VariableIdentifier::VariableType::NET;
	id.net = net;
	VariableData[nextFreeVariable] = id;
	nextFreeVariable++;
}

double TransientSolver::GetNetVoltage(Net *net, int n) {
	if (n == -1) n = currentTick;
	if (net->IsFixedVoltage) {
		return net->NetVoltage;
	}
	else {
		return VariableValues[n][NetVariables[net]];
	}
}

double TransientSolver::GetVarValue(int id, int tick) {
	if (tick == -1) tick = currentTick;
	return VariableValues[tick][id];
	
}

double TransientSolver::GetPinCurrent(Component *c, int pin, int n) {
	if (n == -1) n = currentTick;
	if (pin < c->GetNumberOfPins() - 1) {
		NetConnection conn;
		conn.component = c;
		conn.pin = pin;
		return VariableValues[n][ComponentVariables[c] + pin];
	}
	else {
		double sum = 0;
		for (int i = 0; i < c->GetNumberOfPins() - 1; i++) {
			sum += GetPinCurrent(c, i, n);
		}
		return -sum;
	}
}

int TransientSolver::GetCurrentTick() {
	return currentTick;
}

double TransientSolver::GetTimeAtTick(int n) {
	return times[n];
}

//This function is very similar to the function used to solve for a DC operating point.
//See report section 2.4.1
int TransientSolver::Tick(double tol, int maxIter, bool * convergenceFailureFlag) {
	clock_t startTime = clock();
	int n = VariableValues[currentTick].size();
	double **matrix = new double*[n];
	double worstTol = 0;
	int i;
	for (i = 0; i < n; i++) matrix[i] = new double[n+1];
	int worstVar = -1;
	bool convergenceFailure = false;

	for (i = 0; i < maxIter; i++) {
		
		for (int j = 0; j < n; j++) {
			for (int k = 0; k < n+1; k++) {
				matrix[j][k] = 0;
			}
		}
		//See report section 2.4.1.3
		for (int j = 0; j < n; j++) {
			VariableIdentifier varData = VariableData[j];
			if (varData.type == VariableIdentifier::VariableType::COMPONENT) {
				matrix[j][n] = -varData.component->TransientFunction(this, varData.pin);

				
				int k = ComponentVariables[varData.component];
				int npin = varData.component->GetNumberOfPins();
				for (int pin = 0; pin < npin; pin++) {
					//Components only have n-1 variables, but we must run the for loop up to n to check the net connection to the nth pin
					if (pin < (npin-1))
						matrix[j][k] = varData.component->TransientDerivative(this, varData.pin, VariableData[k]);
					Net *pinNet = varData.component->PinConnections[pin];
					if (!pinNet->IsFixedVoltage) {
						int netVar = NetVariables[pinNet];
						matrix[j][netVar] = varData.component->TransientDerivative(this, varData.pin, VariableData[netVar]);
					}
					k++;
				}
				

			}
			else {
				matrix[j][n] = -varData.net->TransientFunction(this);

				int ncon = varData.net->connections.size(); 
				for (int k = 0; k < ncon; k++) {
					NetConnection conn = varData.net->connections[k]; 
					if (conn.pin < (conn.component->GetNumberOfPins() - 1)) {
						int var = ComponentVariables[conn.component] + conn.pin;
						matrix[j][var] = varData.net->TransientDerivative(this, VariableData[var]);
					}
					else {
						int npin = conn.component->GetNumberOfPins();
						for (int l = 0; l < (npin - 1); l++) {
							int var = ComponentVariables[conn.component] + l;
							matrix[j][var] = varData.net->TransientDerivative(this, VariableData[var]);
						}
					}
				}
			}
		}
		worstTol = 0;
	    worstVar = -1;
		for (int i = 0; i < n; i++) {
			if (abs(matrix[i][n]) > worstTol) {
				worstTol = abs(matrix[i][n]);
				worstVar = i;
			}
		}
		if (worstTol < tol) break;
		Math::newtonIteration(n, &(VariableValues[currentTick][0]), matrix);
		if (((clock() - startTime) / ((double)CLOCKS_PER_SEC)) > maxTickTime) {
			std::cerr << "Tick timeout t=" << GetTimeAtTick(GetCurrentTick()) << " e=" << worstTol << std::endl;
		
			convergenceFailure = true;
			break;
		}
	}
	for (int j = 0; j < n; j++) delete[] matrix[j];
	delete[] matrix;
	if (i == maxIter) {
		std::cerr << "Interactive convergence failure t=" << GetTimeAtTick(GetCurrentTick()) << " e=" << worstTol << " var=" << worstVar << std::endl;
		convergenceFailure = true;
	}
	if (convergenceFailure) {
		//Only concerned by convergence failures where e>1
		if (worstTol > 1) {
			if (convergenceFailureFlag != nullptr)
				*convergenceFailureFlag = true;
		}
	}

	return i;
}


void TransientSolver::RunInteractive(double simSpeed, double tol, int maxIter) {

	double currentTime = 0;
	//In order to see timestep recommendations and initialise stateful components, run a timestep at 0s - but discard it, as the steady state represents the initial conditions
	bool firstRun = true;
	bool running = true;
	int ticktimestoAvg = 1200;
	std::deque<double> ticktimes;
	std::clock_t lastUpdateTime = 0;
	while (running) {
		currentTick++;
		nextTimestep = simSpeed / 10;
		if (!firstRun) {
			nextTimestep = simSpeed * averageTickTime;
		}
		VariableValues.push_back(VariableValues[currentTick - 1]);
		times.push_back(currentTime);

		if (VariableValues.size() > bufferSize) {
			VariableValues.pop_front();
			times.pop_front();
			currentTick--;
		}

		LARGE_INTEGER startT, endT, elapseduS;
		LARGE_INTEGER freq;

		QueryPerformanceFrequency(&freq);
		QueryPerformanceCounter(&startT);

		bool convergenceFailure = false;
		if (firstRun) {
			try {
				Tick(tol, maxIter);
			}
			catch (std::runtime_error *e) {

			}
		}
		else {
			try {
				Tick(tol, maxIter, &convergenceFailure);
			}
			catch (std::runtime_error *e) {
				std::cerr << "RUNTIME ERROR AT T=" << currentTime << " : " << e->what() << std::endl;
				SolverCircuit->ReportError("EXCEPTION", true);
				running = false;
			}
		}
		

		if (!firstRun) {
			if (((clock() - lastUpdateTime) / ((double)CLOCKS_PER_SEC)) > 2e-3) {
				if (InteractiveCallback != nullptr) {
					(*InteractiveCallback)(this);
				}
				lastUpdateTime = clock(); 
			}
		}

		QueryPerformanceCounter(&endT);
		while (((endT.QuadPart - startT.QuadPart) / ((double)freq.QuadPart)) < 1e-4) QueryPerformanceCounter(&endT);
		//std::cerr << ((endT.QuadPart - startT.QuadPart) / ((double)freq.QuadPart)) << std::endl;
		elapseduS.QuadPart = endT.QuadPart - startT.QuadPart;
		elapseduS.QuadPart *= 1000000;
		elapseduS.QuadPart /= freq.QuadPart;

		//Recalculate tick time 
		double timeForTick = elapseduS.QuadPart / 1000000.0;
		ticktimes.push_back(timeForTick);
		if (ticktimes.size() > ticktimestoAvg) {
			ticktimes.pop_front();
		}
		double ttsum = 0;
		for (auto iter = ticktimes.begin(); iter != ticktimes.end(); iter++)
			ttsum += *iter;
		averageTickTime = ttsum / ticktimes.size();

		totalNumberOfTicks++;
		if ((totalNumberOfTicks % 30) == 0) {
			std::cerr << averageTickTime << std::endl;
		}
		currentTime += nextTimestep;
		if (convergenceFailure)
			SolverCircuit->ReportError("CONVERGENCE", false);

		if (firstRun) {
			VariableValues.erase(VariableValues.end() - 1);
			times.erase(times.end() - 1);
			currentTick--;
			firstRun = false;
		}
	}

};

bool TransientSolver::RampUp(std::map<Net *, double> originalVoltages, double tol, int maxIter) {
	double currentTime = 0;
	bool firstRun = true;
	bool status = true;
	for(int i = 0; i < 10; i++) {
		currentTick++;
		nextTimestep = 1e-3;
		VariableValues.push_back(VariableValues[currentTick - 1]);
		times.push_back(currentTime);

		 
		if (firstRun) {
			try {
				Tick(tol, maxIter);
			}
			catch (std::runtime_error *e) {
				//Runtime errors are common on the first tick and can safely be ignored.
			}
		}
		else {
			int iter = Tick(tol, maxIter);
			if (iter == maxIter) {
				std::cerr << "Source ramp convergence failure at t=" << currentTime << std::endl;
				status = false;
			}
				
		}

		currentTime += nextTimestep;

		//Ramp up fixed voltage nets
		for each(auto net in originalVoltages) {
			Net *vNet = net.first;
			if (abs(vNet->NetVoltage) < abs(net.second))
				vNet->NetVoltage += net.second * 0.1;
		}

		if (firstRun) {
			VariableValues.erase(VariableValues.end() - 1);
			times.erase(times.end() - 1);
			currentTick--;
			firstRun = false;
		}
	}
	return status;
}

void TransientSolver::Reset() {
	VariableValues.clear();
	nextTimestep = 0;
	currentTick = 0;
	VariableValues.push_back(std::vector<double>());
	times.push_back(0);
}

void TransientSolver::RequestTimestep(double deltaT) {
	if (deltaT < nextTimestep)
		nextTimestep = deltaT;
}

void TransientSolver::SetNetVoltageGuess(Net *net, double value) {
	VariableValues[currentTick][NetVariables[net]] = value;
}