using System;
using Microsoft.SPOT;

namespace Light_and_Magic {
	class Calc {
		double[] intensity = new double[100];
		double[] luminosity = new double[100];

		String name;

		public Calc(double[] xin, double[] yin, string namein) {

			name = namein;

			double[] x = xin;
			double[] y = yin;

			double[] X = new double[x.Length];
			double[] Y = new double[y.Length];

			for (int i = 0; i < y.Length; i++) {
				Y[i] = System.Math.Log(y[i]);
			}

			X = x;

			double sum_x = 0;
			foreach (double val in X) {
				sum_x = sum_x + val;
			}

			double sum_y = 0;
			foreach (double val in Y) {
				sum_y = sum_y + val;
			}

			double sum_x_2 = 0;
			foreach (double val in X) {
				sum_x_2 = sum_x_2 + System.Math.Pow(val, 2);
			}

			double sum_XY = 0;
			for (int i = 0; i < x.Length; i++) {
				sum_XY = sum_XY + (X[i] * Y[i]);
			}

			double N = X.Length;
			double B = (sum_XY - (sum_x_2 * (sum_y / sum_x))) / (sum_x - (sum_x_2 * N / sum_x));

			double A = (sum_y - N * B) / sum_x;

			double C = System.Math.Exp(B);

			double[] xx = new double[100];
			double[] yy = new double[100];

			for (int i = 0; i < 100; i++) {
				intensity[i] = i + 1;
				luminosity[i] = C * System.Math.Exp(A * (i + 1));
			}
		}

		public double getLuminosity(int i) {
			return luminosity[i];
		}
	}
}
