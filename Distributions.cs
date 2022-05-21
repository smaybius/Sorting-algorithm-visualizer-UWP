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
            COSINE_WAVE,
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
