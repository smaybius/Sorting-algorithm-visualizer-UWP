using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace AlgoUWP
{
    public class Comparer : IComparer<int>
    {

        public int Compare(int x, int y)
        {
            Sort.comparisons++;
            return x < y ? -1 : x > y ? 1 : 0;
        }
    }

    public class Arr
    {
        
        // Array of values
        int[] arr;
        public bool ismain;
        private Canvas MyCanvas;
        private Slider Speed;

        public Arr(int length, Canvas canvas, Slider speed)
        {
            arr = new int[length];
            this.MyCanvas = canvas;
            this.Speed = speed;
            ismain = true;
        }
        public Arr(int length)
        {
            arr = new int[length];
            ismain = false;
        }


        // To enable client code to validate input
        // when accessing your indexer.
        public int Length => arr.Length;

        // Indexer declaration.
        // If index is out of range, the temps array will throw the exception.
        public int this[int index]
        {
            get
            {
                Sort.reads++;
                DrawNumbers(arr, index);
                Thread.Sleep((int)Speed.Value);
                return arr[index];
            }
            set
            {
                arr[index] = value;
                DrawNumbers(arr, index);
                if (ismain)
                {
                    Sort.mainwrites++;
                }
                else
                {
                    Sort.auxwrites++;
                }
                if (Speed != null) Thread.Sleep((int)Speed.Value);
            }
        }
        public void DrawNumbers(int[] arr, int mark)
        {
            MyCanvas.Children.Clear();

            int howMany = arr.Length;
            double size = MyCanvas.ActualWidth / howMany;

            for (int i = 0; i < howMany; i++)
            {
                Rectangle rect = new();
                Canvas.SetLeft(rect, size * i);
                Canvas.SetTop(rect, 0);
                rect.Width = size;
                rect.Height = (MyCanvas.ActualHeight - 5) / howMany * arr[i];
                rect.Fill = Array.IndexOf(arr, i) == mark ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                MyCanvas.Children.Add(rect);
            }
        }
        public void DrawNumbers()
        {
            
                MyCanvas.Children.Clear();

                int howMany = arr.Length;
                double size = MyCanvas.ActualWidth / howMany;

                for (int i = 0; i < howMany; i++)
                {
                    Rectangle rect = new();
                    Canvas.SetLeft(rect, size * i);
                    Canvas.SetTop(rect, 0);
                    rect.Width = size;
                    rect.Height = (MyCanvas.ActualHeight - 5) / howMany * arr[i];
                    rect.Fill = new SolidColorBrush(Colors.Black);
                    MyCanvas.Children.Add(rect);
                }
            
                
        }

        public static implicit operator int(Arr v)
        {
            Convert.ToInt32(v);
            return v;
        }
    }
    public static class Sort
    {

        //for custom arrays
        public static bool isPremade;

        //how many comparisons were needed for the sort in total
        public static long comparisons;

        //how many array accesses were needed for the sort
        public static long reads;

        public static long swaps;

        public static long mainwrites;

        public static long auxwrites;

        public static long reversals;

        //bool for the pause button && extra functionallity
        public static bool isPaused;
        
        public static void Swap(Arr array, int a, int b, bool auxwr)
        {

            (array[b], array[a]) = (array[a], array[b]);
            swaps++;
            if (auxwr == true)
            {
                auxwrites += 2;
            }
            else
            {
                mainwrites += 2;

            }

        }

        public static void Write(Arr array, int at, int equals, bool auxwr)
        {
            array[at] = equals;
            if (auxwr == true)
            {
                auxwrites++;
            }
            else
            {
                mainwrites++;
            }
        }

        public static void Reversal(Arr array, int start, int length, bool aux)
        {
            reversals++;

            for (int i = start; i < start + ((length - start + 1) / 2); i++)
            {
                Swap(array, i, start + length - i, aux);
            }
        }

        public static void ChangeReversals(int value)
        {
            reversals += value;
        }

        public static void VisualClear(Arr array, int index)
        {
            array[index] = -1;
        }

    }
}
