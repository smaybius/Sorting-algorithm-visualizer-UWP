using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgoUWP
{
    internal static class InsertionWhatSort
    {
        public static void InsertionWhat(Arr MyArray, IComparer<int> cmp)
        {
            Debug.WriteLine("WeirdInsertion");
            bool sorted = false;
            // while is not sorted
            while (!sorted)
            {
                sorted = true;
                for (int i = 1; i < MyArray.Length; i++)
                {
                    int j = i;

                    // Insert MyArray[i] into list 0..i-1 
                    while (j > 0 && cmp.Compare(MyArray[j], MyArray[j - 1]) < 0)
                    {
                        // Swap MyArray[j] and MyArray[j-1] 
                        Sort.Swap(MyArray, i, j - 1, false);

                        // Decrement j by 1 
                        j--;
                        sorted = false;
                    }
                }
            }
        }
    }
}
