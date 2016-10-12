#pragma once
#include <cmath>
#include <exception>
/*
Additional helper functions providing various mathematix
*/
namespace Math {
	//Return the x-value at which f(x) is greatest, for x = start .. start + k*inc .. stop
	template <typename T, typename Func> T argmax(Func f, T start, T stop, T inc) {
		T max = f(start), maxPoint = start;
		for (T x = start; x <= stop; x += inc){
			T y = f(x);
			if (y > max) {
				max = y;
				maxPoint = x;
			}
		}
		return maxPoint;
	};
	
	//Same as argmax, but with different types for x and f(x)
	template <typename Tx, typename Tfx, typename Func> Tx argmax2(Func f, Tx start, Tx stop, Tx inc) {
		Tfx max = f(start);
		Tx maxPoint = start;
		for (Tx x = start; x <= stop; x += inc){
			Tfx y = f(x);
			if (y > max) {
				max = y;
				maxPoint = x;
			}
		}
		return maxPoint;
	};

	/*
	Perform a Newton-Raphson iteration for a system of n equations and n variables

	x is the n by 1 matrix containing initial x values, and set to the final values at the end
	
	There must then be a n rows by n + 1 columns matrix, the first n columns being the jacobian matrix
	of derivatives and the n+1th column being the value for -fn(x)

	The function returns the greatest magnitude L-R value
	*/

	void newtonIteration(int n, double *x, double **m);

	/*
	Using a Gaussian elimination, puts a n by n+1 matrix into 'row echelon form'
	*/
	void gaussianElimination(int n, double **m);

	//Thermal voltage at 300K
	const double vTherm = 25.85e-3;

	const double pi = 3.1415926535897932384626433832795;
	const double degreesToRadians = pi / 180;

	/*Safer exp function for use in a Newtonian solver - models as linear above a certain point*/
	double exp_safe(double x, double limit = 45);

	/*Derivative of above function*/
	double exp_deriv(double x, double limit = 45);

}