#pragma once
class Net;
struct netConnection;
class Component;
#include <string>
#include <vector>
#include <iostream>
#include <ctime>
#include <deque>
#include <cstdlib> 
#include <thread>

#include "Math.h"

#include "Component.h"
#include "Net.h"
#include "Circuit.h"

#include "DCSolver.h"


typedef void (*fnTickCallback) (TransientSolver *t);

class TransientSolver
{
public:
	TransientSolver();
	TransientSolver(DCSolver init);

	//Add components and nets to be included in the solver
	void AddComponent(Component *c);
	void AddNet(Net *net);

	//Run a single Newton-Raphson solve 'tick'
	int Tick(double tol = 1e-6, int maxIter = 50, bool *convergenceFailureFlag = nullptr);

	//Run the solver in interactive mode
	void RunInteractive(double simSpeed, double tol = 1e-6, int maxIter = 100);

	/*
	Performs a DC 'ramp-up' simulation. Initial operating point must have all fixed voltage nets at 0V
	Returns whether or not successful
	*/
	bool RampUp(std::map<Net *, double> originalVoltages, double tol = 1e-12, int maxIter = 400);

	//Get value of a net voltage at current point in solve routine, given the tick number (-1 for current time)
	double GetNetVoltage(Net *net, int n = -1);

	//Get value of current going INTO a pin at current point in solve routine given the tick number
	double GetPinCurrent(Component *c, int pin, int n = -1);

	//Get current tick number
	int GetCurrentTick();

	//Get time that a given tick occurred
	double GetTimeAtTick(int n);

	//To be called by components, to recommend the next timestep
	void RequestTimestep(double deltaT);

	//Clears all results
	void Reset();

	//Get variable value given ID and tick
	double GetVarValue(int id, int tick = -1);

	//This function is called after an interactive simulation tick
	fnTickCallback InteractiveCallback = nullptr;

	//Sets the guess value for a net voltage
	void SetNetVoltageGuess(Net *net, double vale);

private:
	int nextFreeVariable = 0;
	double nextTimestep = 0;
	int currentTick = 0;
	int totalNumberOfTicks = 0;
	double averageTickTime = 0;

	std::map<Net *, int> NetVariables; //Map pointers to nets to net voltage variable IDs

	//Note that there are n-1 (where n=number of pins) variables allocated to each component
	std::map<Component *, int> ComponentVariables; //Map component pins to pin current variable IDs

	std::map<int, VariableIdentifier> VariableData; //Allow variables to be looked up

	std::deque<std::vector<double>> VariableValues; //Map variable IDs to values at a given tick

	std::deque<double> times; //Time at each tick

	const int bufferSize = 10000; //During interactive simulations, number of components to store

	//Max time for single tick
	const double maxTickTime = 0.4;
	
	Circuit *SolverCircuit;
};

