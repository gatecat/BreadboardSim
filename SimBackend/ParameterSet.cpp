#include "ParameterSet.h"
std::string strToLower(std::string str) {
	std::transform(str.begin(), str.end(), str.begin(), tolower);
	return str;
}

ParameterSet::ParameterSet(std::vector<std::string> parts) {
	for (auto a = parts.begin(); a != parts.end(); ++a) {
		int delpos = a->find('=');
		if (delpos != std::string::npos) {
			std::string before = strToLower(a->substr(0, delpos));
			std::string after = strToLower(a->substr(delpos + 1));
			params[before] = after;
		}
	}
}

std::string ParameterSet::getString(std::string key, std::string defaultValue) {
	auto a = params.find(strToLower(key));
	if (a != params.end()) {
		return a->second;
	}
	else {
		return defaultValue;
	}
}

double ParameterSet::getDouble(std::string key, double defaultValue) {
	auto a = params.find(strToLower(key));
	if (a != params.end()) {
		return atof(a->second.c_str());
	}
	else {
		return defaultValue;
	}
}