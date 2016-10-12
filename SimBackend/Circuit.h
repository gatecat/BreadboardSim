#pragma once

#include <vector>
#include <string>
#include <sstream>
#include <algorithm>

#include "ParameterSet.h"

#include "Net.h"


class Circuit;
class ParameterSet;
class Net;

class Circuit
{
public:
	Circuit();
	void ReadNetlist(std::string data);

	//DCSolver getSolver();
	void AddComponent(std::vector<std::string> nets, Component *c);
	std::vector<Net*> Nets;
	std::vector<Component*> Components;

	//Reports an error to the GUI. If fatal is set to true, then the program will subsequently hang until it is killed by the GUI.
	void ReportError(std::string desc, bool fatal);

	//Set true to continue after an error
	bool ContinueFromError = false;
};

