#include "Math.h"
#include <stdexcept>
#include <set>

namespace Math {
	void newtonIteration(int n, double *x, double **m) {
		bool atConvergence = true;

		gaussianElimination(n, m);
		double *delta = new double[n];
		//Back substitution: see report section 2.4.1.5
		for (int i = n - 1; i >= 0; i--) {
			double sum = m[i][n];
			for (int j = i + 1; j < n; j++) {
				sum -= delta[j] * m[i][j];
			}
			delta[i] = sum / m[i][i];
		}

		for (int i = 0; i < n; i++) {
			x[i] += delta[i];
		}
		delete[] delta;

	}
	
	//See report section 2.4.1.4
	void gaussianElimination(int n, double **m) {
		//One for each row: a list of non-zeros for each row

		for (int r = 0; r < n; r++) {
			int i_max = argmax2<int, double>([&](int x) -> double {return std::abs(m[x][r]); }, r, n - 1, 1);
			if (m[i_max][r] == 0)
				throw new std::runtime_error("Matrix is singular");

			//Swap rows
			double *tmpRow;
			tmpRow = m[r];
			m[r] = m[i_max];
			m[i_max] = tmpRow;

			for (int i = r + 1; i < n; i++) {
				if (m[i][r] != 0) {
					for (int j = r + 1; j < n + 1; j++) {
						if (m[r][j] != 0) {
							m[i][j] -= m[r][j] * (m[i][r] / m[r][r]);
						}
					}
					m[i][r] = 0;
				}

			}
		}
	}



	double exp_safe(double x, double limit) {
		if (x > limit) {
			return exp(limit)*(x - limit + 1);
		}
		else if (x < -limit) {
			return exp(-limit)*(x + limit + 1);
		}
		else {
			return exp(x);
		}
	}

	double exp_deriv(double x, double limit) {
		if (x > limit) {
			return exp(limit);
		}
		else if (x < -limit) {
			return exp(-limit);
		}
		else {
			return exp(x);
		}
	}
}