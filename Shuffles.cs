using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoUWP
{
    public static class Shuffles
    {
        static readonly Random random = new();

        public enum ShuffleChoices
        {
            RANDOM,
            REVERSE,
            SLIGHT_SHUFFLE,
            SORTED,
            NAIVE_RANDOM,
            SHUFFLED_TAIL,
            SHUFFLED_HEAD,
            SHUFFLED_BOTHSIDES,
            SHIFTED_ELEMENT,
            RANDOM_SWAP,
            SWAPPED_ENDS,
            INVERTED_PAIRS,
            RANDOM_REVERSAL,
            NOISY,
            SHUFFLED_ODDS,
            FINAL_MERGE,
            REAL_FINAL_MERGE,
            SHUFFLED_HALF,
            PARTITIONED,
            QUICK_PARTITIONED,
            SAWTOOTH,
            PIPE_ORGAN,
            INVERTED_PIPE_ORGAN,
            TRIANGLE_WAVE,
            INTERLACED,
            DOUBLE_LAYERED,
            FINAL_RADIX,
            REAL_FINAL_RADIX,
            RECURSIVE_RADIX,
            BACKWARDS_WEAVE,
            HALF_ROTATION,
            HALF_REVERSED,
            BINARY_SEARCH_TREE,
            INVERTED_BST,
            LOGPILE,
            MAX_HEAP,
            MIN_HEAP,
            FLIPPED_MIN_HEAP,
            SMOOTH_HEAP,
            POPLAR_HEAP,
            TRIANGULAR_HEAP,
            CIRCLE_PASS,
            FINAL_PAIRWISE,
            RECURSIVE_REVERSE,
            GRAY_CODE_FRACTAL,
            SIERPINSKI_TRIANGLE,
            TRIANGULAR,
            QUICKSORT_ADVERSARY,
            PDQUICKSORT_ADVERSARY,
            GRAILSORT_ADVERSARY,
            SHUFFLE_MERGE_ADVERSARY,
            BIT_REVERSAL,
            BLOCK_RANDOM,
            BLOCK_REVERSE,
        }
        public static void RandomShuffle(Arr array, int start, int end)
        {
            Debug.WriteLine("Random shuffle");
            for (int i = start; i < end; i++)
            {
                int randomIndex = random.Next(end - i) + i;
                Sort.Swap(array, i, randomIndex, false);
            }
        }
        public static void Sorted(Arr array, int start, int end)
        {
            Debug.WriteLine("Sorting");
            int min = array[start], max = min;
            for (int i = start + 1; i < end; i++)
            {
                if (array[i] < min) min = array[i];
                else if (array[i] > max) max = array[i];
            }

            int size = max - min + 1;
            int[] holes = new int[size];

            for (int i = start; i < end; i++)
            {
                holes[array[i] - min] = holes[array[i] - min] + 1;
            }

            for (int i = 0, j = start; i < size; i++)
            {
                while (holes[i] > 0)
                {
                    holes[i] = holes[i] - 1;
                    Sort.Write(array, j, i + min, false);
                    j++;
                }
            }
        }
    }
}
