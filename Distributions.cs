using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoUWP
{
    public static class Distributions
    {
        public enum DistribChoices
        {
            LINEAR,
            FEW_UNIQUE,
            NO_UNIQUE,
            RANDOM,
            QUADRATIC,
            SQUARE_ROOT,
            CUBIC,
            QUINTIC,
            CUBE_ROOT,
            QUINTIC_ROOT,
            SINE_WAVE,
            SINE_WAVE_CONTINUOUS,
            COSINE_WAVE,
            COSINE_WAVE_CONTINUOUS,
            TANGENT_WAVE,
            TANGENT_WAVE_CONTINUOUS,
            COTANGENT_WAVE,
            COTANGENT_WAVE_CONTINUOUS,
            SECANT_WAVE,
            SECANT_WAVE_CONTINUOUS,
            COSECANT_WAVE,
            COSECANT_WAVE_CONTINUOUS,
            PERLIN_NOISE,
            PERLIN_NOISE_CURVE,
            BELL_CURVE,
            RULER,
            BLANCMANGE_CURVE,
            CANTOR_FUNCTION,
            SUM_OF_DIVISORS,
            OEIS_FLY_STRAIGHT,
            DECREASING_RANDOM,
            MODULO_FUNCTION,
            EULER_TOTIENT,
            PRIME_NUMBERS,
            PRODUCT_OF_DIGITS

        }
        public static void Linear(Arr array)
        {

            for (int i = 0; i < array.Length; i++)
            {
                Sort.Write(array, i, i + 1, false);
            }
        }
    }
}
