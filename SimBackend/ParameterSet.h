#pragma once
#include <map>
#include <string>
#include <vector>
#include <algorithm>
std::string strToLower(std::string in);

/*
A ParameterSet is a set of key/value pairs, readable as strings or doubles, representing component parameters read from a netlist
Keys are not case sensitive
*/
class ParameterSet {
public:
	std::map<std::string, std::string> params;
	ParameterSet(std::vector<std::string> parts);
	std::string getString(std::string key, std::string defaultValue);
	double getDouble(std::string key, double defaultValue = 0);

};
