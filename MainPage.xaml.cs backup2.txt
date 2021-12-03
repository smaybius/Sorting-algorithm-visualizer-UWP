using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AlgoUWP
{
    // We are initializing a COM interface for use within the namespace
    // This interface allows access to memory at the byte level which we need to populate audio data that is generated
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 
    internal class Subarray
    {
        public int start;
        public int end;
        public Subarray(int start, int end)
        {
            this.start = start;
            this.end = end;
        }
    }

    internal class PDQPair
    {
        public int pivotPosition;
        public bool alreadyPartitioned;

        public PDQPair(int pivotPos, bool presorted)
        {
            pivotPosition = pivotPos;
            alreadyPartitioned = presorted;
        }

        public int GetPivotPosition()
        {
            return pivotPosition;
        }

        public bool GetPresortBool()
        {
            return alreadyPartitioned;
        }
    }

    public class MatrixShape
    {
        public int width, height;
        public bool unbalanced, insertLast;

        public MatrixShape(int width, int height, bool insertLast)
        {
            this.width = width;
            this.height = height;
            unbalanced = (width == 1) ^ (height == 1);
            this.insertLast = unbalanced || insertLast;
        }
    }

    public sealed partial class MainPage : Page
    {
        //bool for the pause button && extra functionallity
        private bool isPaused;

        //current array of numbers (the one being shown)
        public int[] arr;

        //currently highlighted indexes
        public int[] selectedArr;

        //all sorting steps (arrays of numbers)
        private List<int[]> sortHistory;

        //all highlighted indexes during the sorting steps
        private List<int[]> selectedHistory;

        //timer that we'll use when drawing the array
        private DispatcherTimer timer;

        //for custom arrays
        private bool isPremade;

        //how many comparisons were needed for the sort in total
        public long comparisons;

        //how many array accesses were needed for the sort
        public long arrAccesses;

        public long swaps;

        public long writes;

        public long reversals;

        public bool shuffling;

        //Audio variables
        private AudioGraph graph;
        private AudioDeviceOutputNode deviceOutputNode;
        private AudioFrameInputNode frameInputNode;
        public double theta = 0;

        public MainPage()
        {
            InitializeComponent();
            if (timer != null) { timer.Stop(); }
            isPaused = false;
            selectedHistory = new List<int[]>();
            sortHistory = new List<int[]>();
            arr = new int[(int)ArraySize.Value];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = i + 1;
                selectedArr = new int[] { i };
            }
            Array.Sort(arr);
            AddHistorySnap();
            DrawHistory();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await CreateAudioGraph();
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            //disable the shuffle button if the shuffle is set to "none" (option 4)
            //if (shufflecomboBox.SelectedIndex == 4)
            //{
            ShuffleArray();
            //}

            //frameInputNode.Start();
        }

        public void RandomShuffle(int[] array, int start, int end)
        {
            Random random = new();
            for (int i = start; i < end; i++)
            {
                int randomIndex = random.Next(end - i) + i;
                Swap(array, i, randomIndex);
            }
        }
        public void Reversearraycopy(int[] src, int srcPos, int[] dest, int destPos, int length)
        {
            for (int i = length - 1; i >= 0; i--)
            {
                Write(dest, destPos + i, src[srcPos + i]);
            }
        }

        private void SwapBlocksBackwards(int[] array, int a, int b, int len)
        {
            for (int i = 0; i < len; i++)
            {
                Swap(array, a + len - i - 1, b + len - i - 1);
            }
        }

        private void BlockSwap(int[] array, int a, int b, int len)
        {
            for (int i = 0; i < len; i++)
            {
                Swap(array, a + i, b + i);
            }
        }

        private void ShiftForwards(int[] array, int start, int length)
        {
            int temp = array[start];
            for (int i = 0; i < length; i++)
            {
                Write(array, start + i, array[start + i + 1]);
            }
            Write(array, start + length, temp);
        }

        private void ShiftBackwards(int[] array, int start, int length)
        {
            int temp = array[start + length];
            for (int i = length; i > 0; i--)
            {
                Write(array, start + i, array[start + i - 1]);
            }
            Write(array, start, temp);
        }

        public void HolyGriesMills(int[] array, int pos, int lenA, int lenB)
        {
            while (lenA > 1 && lenB > 1)
            {
                while (lenA <= lenB)
                {
                    BlockSwap(array, pos, pos + lenA, lenA);
                    pos += lenA;
                    lenB -= lenA;
                }

                if (lenA <= 1 || lenB <= 1)
                {
                    break;
                }

                while (lenA > lenB)
                {
                    SwapBlocksBackwards(array, pos + lenA - lenB, pos + lenA, lenB);
                    lenA -= lenB;
                }
            }

            if (lenA == 1)
            {
                ShiftForwards(array, pos, lenB);
            }
            else if (lenB == 1)
            {
                ShiftBackwards(array, pos, lenA);
            }
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem b = sender as MenuFlyoutItem;
            comboBox.Content = b.Text;
        }

        public void ResetDistribution(int index)
        {

        }

        private void ArraySize_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            ResetDistribution(distribcomboBox.SelectedIndex);
        }

        private void ShuffleArray()
        {
            shuffling = true;
            //create a random starting array, if its not already premade
            if (!isPremade)
            {
                Random random = new();
                if (timer != null) { timer.Stop(); }
                isPaused = false;
                selectedHistory = new List<int[]>();
                sortHistory = new List<int[]>();
                switch (shufflecomboBox.SelectedIndex)
                {
                    case 0: //reset
                        if (timer != null) { timer.Stop(); }
                        isPaused = false;
                        selectedHistory = new List<int[]>();
                        sortHistory = new List<int[]>();
                        int n = arr.Length - 1;
                        double c = 2 * Math.PI / n;
                        arr = new int[(int)ArraySize.Value];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = i + 1;
                            selectedArr = new int[] { i };
                        }
                        Array.Sort(arr);
                        AddHistorySnap();
                        switch (distribcomboBox.SelectedIndex)
                        {
                            //case 0: linear, default
                            case 1: //few unique
                                int l = 0, r, t = Math.Min(arr.Length, 8);
                                for (int i = 0; i < t; i++)
                                {
                                    if (random.NextDouble() < 0.5)
                                    {
                                        l++;
                                    }
                                    selectedArr = new int[] { i };
                                }

                                r = arr.Length - (t - l);
                                for (int i = 0; i < l; i++)
                                {
                                    arr[i] = (int)(arr.Length * 0.25);
                                    selectedArr = new int[] { i };
                                }

                                for (int i = 0; i < r; i++)
                                {
                                    arr[i] = arr.Length / 2;
                                    selectedArr = new int[] { i };
                                }

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(arr.Length * 0.75);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 2: //no unique
                                int val = arr.Length / 2;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = val;
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 3: //noise
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = random.Next(arr.Length);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 4: //quadratic curve
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(Math.Pow(i, 2) / arr.Length);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 5: //square root curve
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(Math.Sqrt(i) * Math.Sqrt(arr.Length));
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 6: //cubic curve
                                double midl = (arr.Length - 1) / 2d;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)((Math.Pow(i - midl, 3) / Math.Pow(midl, 3 - 1)) + midl);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 7: //quintic curve
                                double midd = (arr.Length - 1) / 2d;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)((Math.Pow(i - midd, 5) / Math.Pow(midd, 5 - 1)) + midd);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 8: //cube root curve
                                double h = arr.Length / 2d;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    double vall = i / h - 1,
                                           root = vall < 0 ? -Math.Pow(-vall, 1d / 3) : Math.Pow(vall, 1d / 3);

                                    arr[i] = (int)(h * (root + 1));
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 9: //fifth root curve
                                double hh = arr.Length / 2d;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    double vall = i / hh - 1,
                                           root = vall < 0 ? -Math.Pow(-vall, 1d / 5) : Math.Pow(vall, 1d / 5);

                                    arr[i] = (int)(hh * (root + 1));
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 10: //sine

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(n * (Math.Sin(c * i) + 1) / 2);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 11: //cosine

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(n * (Math.Cos(c * i) + 1) / 2);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 12: //tangent

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(n * (Math.Tan(c * i) + 1) / 2);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 13: //perlin noise
                                int[] perlinNoise = new int[arr.Length];

                                float step = 1f / arr.Length;
                                float randomStart = random.Next(arr.Length);
                                int octave = (int)(Math.Log(arr.Length) / Math.Log(2));

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    int value = (int)(PerlinNoise.ReturnFracBrownNoise(randomStart, octave) * arr.Length);
                                    perlinNoise[i] = value;
                                    randomStart += step;
                                    selectedArr = new int[] { i };
                                }

                                int minimum = int.MaxValue;
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    if (perlinNoise[i] < minimum)
                                    {
                                        minimum = perlinNoise[i];
                                    }
                                    selectedArr = new int[] { i };
                                }
                                minimum = Math.Abs(minimum);
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    perlinNoise[i] += minimum;
                                    selectedArr = new int[] { i };
                                }

                                double maximum = double.MinValue;
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    if (perlinNoise[i] > maximum)
                                    {
                                        maximum = perlinNoise[i];
                                    }
                                    selectedArr = new int[] { i };
                                }
                                double scale = arr.Length / maximum;
                                if (scale is < 1.0 or > 1.8)
                                {
                                    for (int i = 0; i < arr.Length; i++)
                                    {
                                        perlinNoise[i] = (int)(perlinNoise[i] * scale);
                                        selectedArr = new int[] { i };
                                    }
                                }

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = Math.Min(perlinNoise[i], arr.Length - 1);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 14: //perlin noise curve
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    int value = 0 - (int)(PerlinNoise.ReturnNoise((float)i / arr.Length) * arr.Length);
                                    arr[i] = Math.Min(value, arr.Length - 1);
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 15: //bell curve
                                double stepp = 8d / arr.Length;
                                double position = -4;
                                int constant = 1264;
                                double factor = arr.Length / 512d;

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    double square = Math.Pow(position, 2);
                                    double negativeSquare = 0 - square;
                                    double halfNegSquare = negativeSquare / 2d;
                                    double numerator = constant * factor * Math.Pow(Math.E, halfNegSquare);

                                    double doublePi = 2 * Math.PI;
                                    double denominator = Math.Sqrt(doublePi);

                                    arr[i] = (int)(numerator / denominator);
                                    position += stepp;
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 16: //ruler
                                int steppp = Math.Max(1, arr.Length / 256);
                                int floorLog2 = (int)(Math.Log(arr.Length / steppp) / Math.Log(2));
                                int lowest;
                                for (lowest = steppp; 2 * lowest <= arr.Length / 4; lowest *= 2)
                                {
                                    ;
                                }

                                bool[] digits = new bool[floorLog2 + 2];

                                int iii, jj;
                                for (iii = 0; iii + steppp <= arr.Length; iii += steppp)
                                {
                                    for (jj = 0; digits[jj]; jj++)
                                    {
                                        ;
                                    }

                                    digits[jj] = true;

                                    for (int kk = 0; kk < steppp; kk++)
                                    {
                                        int value = arr.Length / 2 - Math.Min((1 << jj) * steppp, lowest);
                                        arr[iii + kk] = value;
                                    }

                                    for (int kk = 0; kk < jj; kk++)
                                    {
                                        digits[kk] = false;
                                    }

                                    selectedArr = new int[] { iii };
                                }

                                for (jj = 0; digits[jj]; jj++)
                                {
                                    ;
                                }

                                digits[jj] = true;
                                while (iii < arr.Length)
                                {
                                    int value = Math.Max(arr.Length / 2 - ((1 << jj) * steppp), arr.Length / 4);
                                    arr[iii++] = value;
                                    selectedArr = new int[] { iii };
                                }
                                AddHistorySnap();
                                break;
                            case 17: //blancmange curve
                                int floorLog22 = (int)(Math.Log(arr.Length) / Math.Log(2));

                                for (int i = 0; i < arr.Length; i++)
                                {
                                    int value = (int)(arr.Length * curveSum(floorLog22, (double)i / arr.Length));
                                    arr[i] = value;
                                    selectedArr = new int[] { i };
                                }
                                double curveSum(int n, double x)
                                {
                                    double sum = 0;
                                    while (n >= 0)
                                    {
                                        sum += curve(n--, x);
                                    }

                                    return sum;
                                }

                                double curve(int n, double x)
                                {
                                    return triangleWave((1 << n) * x) / (1 << n);
                                }

                                double triangleWave(double x)
                                {
                                    return Math.Abs(x - (int)(x + 0.5));
                                }
                                AddHistorySnap();
                                break;
                            case 18: //cantor function
                                cantor(arr, 0, arr.Length, 0, arr.Length - 1);
                                void cantor(int[] array, int a, int b, int min, int max)
                                {
                                    if (b - a < 1 || max == min)
                                    {
                                        return;
                                    }

                                    int mid = (min + max) / 2;
                                    if (b - a == 1)
                                    {
                                        array[a] = mid;
                                        return;
                                    }

                                    int t1 = (a + a + b) / 3, t2 = (a + b + b + 2) / 3;

                                    for (int i = t1; i < t2; i++)
                                    {
                                        array[i] = mid;
                                        selectedArr = new int[] { i };
                                    }

                                    cantor(array, a, t1, min, mid);
                                    cantor(array, t2, b, mid + 1, max);
                                }
                                AddHistorySnap();
                                break;
                            case 19: //sum of divisors
                                int[] nn = new int[arr.Length];

                                nn[0] = 0;
                                nn[1] = 1;
                                double maxq = 1;

                                for (int i = 2; i < arr.Length; i++)
                                {
                                    nn[i] = sumDivisors(i);
                                    if (nn[i] > maxq)
                                    {
                                        maxq = nn[i];
                                    }

                                    selectedArr = new int[] { i };
                                }

                                double scalee = Math.Min((arr.Length - 1) / maxq, 1);
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(nn[i] * scalee);
                                    selectedArr = new int[] { i };
                                }
                                int sumDivisors(int n)
                                {
                                    int sum = n + 1;
                                    for (int i = 2; i <= (int)Math.Sqrt(n); i++)
                                    {
                                        if (n % i == 0)
                                        {
                                            if (i == n / i)
                                            {
                                                sum += i;
                                            }
                                            else
                                            {
                                                sum += i + n / i;
                                            }
                                        }
                                        selectedArr = new int[] { i };
                                    }
                                    return sum;
                                }
                                AddHistorySnap();
                                break;
                            case 20: //oeis fly straight
                                int[] fsd = new int[arr.Length];

                                double maxx;
                                maxx = fsd[0] = fsd[1] = 1;
                                for (int i = 2; i < arr.Length; i++)
                                {
                                    int g = gcd(fsd[i - 1], i);
                                    fsd[i] = fsd[i - 1] / g + (g == 1 ? i + 1 : 0);
                                    if (fsd[i] > maxx)
                                    {
                                        maxx = fsd[i];
                                    }
                                    selectedArr = new int[] { i };
                                }

                                double scalew = Math.Min((arr.Length - 1) / maxx, 1);
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = (int)(fsd[i] * scalew);
                                    selectedArr = new int[] { i };
                                }
                                int gcd(int a, int b)
                                {
                                    return b == 0 ? a : gcd(b, a % b);
                                }
                                AddHistorySnap();
                                break;
                            case 21: //decreasing random
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    int rr = random.Next(arr.Length - i) + i;
                                    arr[i] = rr;
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            case 22: //modulo function
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = 2 * (arr.Length % (i + 1));
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                            default: //case 0
                                for (int i = 0; i < arr.Length; i++)
                                {
                                    arr[i] = i + 1;
                                    selectedArr = new int[] { i };
                                }
                                AddHistorySnap();
                                break;
                        }
                        break;
                    case 1: //random
                        for (int i = 0; i < arr.Length; i++)
                        {
                            int oldArrItem = arr[i];
                            int switchIndex = random.Next(i, arr.Length);
                            arr[i] = arr[switchIndex];
                            arr[switchIndex] = oldArrItem;
                            selectedArr = new int[] { i };
                        }
                        AddHistorySnap();
                        break;
                    case 2: //reversed
                        Reversal(arr, 0, arr.Length);
                        AddHistorySnap();
                        break;
                    case 3: //slight shuffle
                        for (int i = 0; i < Math.Max(arr.Length / 20, 1); i++)
                        {
                            Swap(arr, random.Next(arr.Length), random.Next(arr.Length));
                        }
                        AddHistorySnap();
                        break;
                    case 4: //sorted
                        Sort(arr, 0, arr.Length);
                        AddHistorySnap();
                        break;
                    case 5: //reverse sorted
                        Sort(arr, 0, arr.Length);
                        Reversal(arr, 0, arr.Length);
                        AddHistorySnap();
                        break;
                    case 6: //scrambled tail
                        int[] aux = new int[arr.Length];
                        int ii = 0, jk = 0, k = 0;
                        while (ii < arr.Length)
                        {
                            if (random.NextDouble() < 1 / 7d)
                            {
                                Write(aux, k++, arr[ii++]);
                            }
                            else
                            {
                                Write(arr, jk++, arr[ii++]);
                            }
                        }
                        Array.Copy(aux, 0, arr, jk, k);
                        RandomShuffle(arr, jk, arr.Length);
                        AddHistorySnap();
                        break;
                    case 7: //scrambled head
                        int[] auxx = new int[arr.Length];
                        int il = arr.Length - 1, jl = arr.Length - 1, kl = 0;
                        while (il >= 0)
                        {
                            if (random.NextDouble() < 1 / 7d)
                            {
                                Write(auxx, kl++, arr[il--]);
                            }
                            else
                            {
                                Write(arr, jl--, arr[il--]);
                            }
                        }
                        Reversearraycopy(auxx, 0, arr, 0, kl);
                        RandomShuffle(arr, 0, jl);
                        AddHistorySnap();
                        break;
                    case 8: //shifted element
                        int start = random.Next(arr.Length);
                        int dest = random.Next(arr.Length);
                        if (dest < start)
                        {
                            HolyGriesMills(arr, dest, start, start + 1);
                        }
                        else
                        {
                            HolyGriesMills(arr, start, start + 1, dest);
                        }
                        AddHistorySnap();
                        break;
                    case 9: //noisy
                        int ik, size = Math.Max(4, (int)(Math.Sqrt(arr.Length) / 2));
                        for (ik = 0; ik + size <= arr.Length; ik += random.Next(size - 1) + 1)
                        {
                            RandomShuffle(arr, ik, ik + size);
                        }

                        RandomShuffle(arr, ik, arr.Length);
                        AddHistorySnap();
                        break;
                    case 10: //scrambled odds
                        for (int i = 1; i < arr.Length; i += 2)
                        {
                            int randomIndex = (((random.Next(arr.Length - i) / 2)) * 2) + i;
                            Swap(arr, i, randomIndex);
                        }
                        AddHistorySnap();
                        break;
                    case 11: //final merge pass
                        int count = 2;

                        int kp = 0;
                        int[] temp = new int[arr.Length];

                        for (int jp = 0; jp < count; jp++)
                        {
                            for (int i = jp; i < arr.Length; i += count)
                            {
                                Write(temp, kp++, arr[i]);
                            }
                        }

                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, temp[i]);
                        }

                        AddHistorySnap();
                        break;
                    case 12: //Shuffled final merge pass
                        RandomShuffle(arr, 0, arr.Length);
                        Sort(arr, 0, arr.Length / 2);
                        Sort(arr, arr.Length / 2, arr.Length);
                        AddHistorySnap();
                        break;
                    case 13: //shuffled second half
                        RandomShuffle(arr, 0, arr.Length);
                        Sort(arr, 0, arr.Length / 2);
                        AddHistorySnap();
                        break;
                    case 14: //partitioned
                        Sort(arr, 0, arr.Length);
                        RandomShuffle(arr, 0, arr.Length / 2);
                        RandomShuffle(arr, arr.Length / 2, arr.Length);
                        AddHistorySnap();
                        break;
                    case 15: //sawtooth
                        int countt = 4;

                        int kr = 0;
                        int[] tempp = new int[arr.Length];

                        for (int j = 0; j < countt; j++)
                        {
                            for (int i = j; i < arr.Length; i += countt)
                            {
                                Write(tempp, kr++, arr[i]);
                            }
                        }

                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, tempp[i]);
                        }

                        AddHistorySnap();
                        break;
                    case 16: //pipe organ
                        int[] tempr = new int[arr.Length];

                        for (int i = 0, j = 0; i < arr.Length; i += 2)
                        {
                            tempr[j++] = arr[i];
                        }
                        for (int i = 1, j = arr.Length; i < arr.Length; i += 2)
                        {
                            tempr[--j] = arr[i];
                        }
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, tempr[i]);
                        }
                        AddHistorySnap();
                        break;
                    case 17: //inverted pipe organ
                        int[] tempe = new int[arr.Length];

                        Reversal (arr, 0, arr.Length - 1);
                        for (int i = 0, j = 0; i < arr.Length; i += 2)
                        {
                            tempe[j++] = arr[i];
                        }
                        for (int i = 1, j = arr.Length; i < arr.Length; i += 2)
                        {
                            tempe[--j] = arr[i];
                        }
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, tempe[i]);
                        }
                        AddHistorySnap();
                        break;
                    case 18: //interlaced
                        int[] referenceArray = new int[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            referenceArray[i] = arr[i];
                        }

                        int leftIndex = 1;
                        int rightIndex = arr.Length - 1;

                        for (int i = 1; i < arr.Length; i++)
                        {
                            if (i % 2 == 0)
                            {
                                Write(arr, i, referenceArray[leftIndex++]);
                            }
                            else
                            {
                                Write(arr, i, referenceArray[rightIndex--]);
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 19: //double layered
                        for (int i = 0; i < arr.Length / 2; i += 2)
                        {
                            Swap(arr, i, arr.Length - i - 1);
                        }
                        AddHistorySnap();
                        break;
                    case 20: //final radix pass
                        int currentL = arr.Length;

                        currentL -= currentL % 2;
                        int mid = currentL / 2;
                        int[] tempw = new int[mid];

                        for (int i = 0; i < mid; i++)
                        {
                            Write(tempw, i, arr[i]);
                        }

                        for (int i = mid, j = 0; i < currentL; i++, j += 2)
                        {
                            Write(arr, j, arr[i]);
                            Write(arr, j + 1, tempw[i - mid]);
                        }
                        AddHistorySnap();
                        break;
                    case 21: //recursive final radix pass
                        weaveRec(arr, 0, arr.Length, 1);

                        void weaveRec(int[] array, int pos, int length, int gap)
                        {
                            if (length < 2)
                            {
                                return;
                            }

                            int mod2 = length % 2;
                            length -= mod2;
                            int mid = length / 2;
                            int[] temp = new int[mid];

                            for (int i = pos, j = 0; i < pos + gap * mid; i += gap, j++)
                            {
                                Write(temp, j, array[i]);
                            }

                            for (int i = pos + gap * mid, j = pos, k = 0; i < pos + gap * length; i += gap, j += 2 * gap, k++)
                            {
                                Write(array, j, array[i]);
                                Write(array, j + gap, temp[k]);
                            }

                            weaveRec(array, pos, mid + mod2, 2 * gap);
                            weaveRec(array, pos + gap, mid, 2 * gap);
                        }
                        AddHistorySnap();
                        break;
                    case 22: //half rotation
                        int a = 0, m = (arr.Length + 1) / 2;

                        if (arr.Length % 2 == 0)
                        {
                            while (m < arr.Length)
                            {
                                Swap(arr, a++, m++);
                            }
                        }
                        else
                        {
                            int tempf = arr[a];
                            while (m < arr.Length)
                            {
                                Write(arr, a++, arr[m]);
                                Write(arr, m++, arr[a]);
                            }
                            Write(arr, a, tempf);
                        }
                        AddHistorySnap();
                        break;
                    case 23: //half reverse
                        Reversal(arr, 0, arr.Length - 1);
                        Reversal(arr, arr.Length / 4, (3 * arr.Length + 3) / 4 - 1);
                        AddHistorySnap();
                        break;
                    case 24: //binary search tree traversal
                        int len = arr.Length;
                        int[] tempt = new int[len];
                        Array.Copy(arr, tempt, arr.Length);

                        // credit to sam walko/anon

                        Queue<Subarray> q = new();
                        q.Enqueue(new Subarray(0, arr.Length));
                        int iw = 0;

                        while (q.Count != 0)
                        {
                            Subarray sub = q.Peek();
                            if (sub.start != sub.end)
                            {
                                int midl = (sub.start + sub.end) / 2;
                                Write(arr, iw, tempt[midl]);
                                iw++;
                                q.Enqueue(new Subarray(sub.start, midl));
                                q.Enqueue(new Subarray(midl + 1, sub.end));
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 25: //inverted binary search tree
                        int[] tempq = new int[arr.Length];

                        // credit to sam walko/anon
                        Queue<Subarray> qq = new();
                        qq.Enqueue(new Subarray(0, arr.Length));
                        int ie = 0;

                        while (qq.Count != 0)
                        {
                            Subarray sub = qq.Peek();
                            if (sub.start != sub.end)
                            {
                                int midw = (sub.start + sub.end) / 2;
                                Write(tempq, ie, midw);
                                ie++;
                                qq.Enqueue(new Subarray(sub.start, midw));
                                qq.Enqueue(new Subarray(midw + 1, sub.end));
                            }
                        }
                        int lenn = arr.Length;
                        int[] temp2 = new int[lenn];
                        Array.Copy(arr, temp2, arr.Length);
                        for (ie = 0; ie < arr.Length; ie++)
                        {
                            Write(arr, tempq[ie], temp2[ie]);
                        }

                        AddHistorySnap();
                        break;
                    case 26: //logarithmic slopes
                        int[] tempg = new int[arr.Length];
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(tempg, i, arr[i]);
                        }

                        Write(arr, 0, 0);
                        for (int i = 1; i < arr.Length; i++)
                        {
                            int log = (int)(Math.Log(i) / Math.Log(2));
                            int power = (int)Math.Pow(2, log);
                            int value = tempg[2 * (i - power) + 1];
                            Write(arr, i, value);
                        }
                        AddHistorySnap();
                        break;
                    case 27: //max heapified
                        for (int i = arr.Length / 2 - 1; i >= 0; i--)
                        {
                            Heapify(i, arr.Length);
                        }
                        selectedArr = new int[] { arr.Length - 1 };

                        void Heapify(int i, int topI)
                        {
                            int maxI = i;
                            int leftChildI = i * 2 + 1;
                            int rightChildI = i * 2 + 2;

                            comparisons++;
                            if (leftChildI < topI)
                            {
                                arrAccesses += 2;
                                comparisons++;
                                if (arr[leftChildI] > arr[maxI])
                                {
                                    maxI = leftChildI;
                                }
                            }

                            comparisons++;
                            if (rightChildI < topI)
                            {
                                arrAccesses += 2;
                                comparisons++;
                                if (arr[rightChildI] > arr[maxI])
                                {
                                    maxI = rightChildI;
                                }
                            }

                            comparisons++;
                            if (maxI != i)
                            {
                                int oldI = arr[i];
                                arr[i] = arr[maxI];
                                arr[maxI] = oldI;

                                arrAccesses += 4;

                                selectedArr = new int[] { i };
                                Heapify(maxI, topI);
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 28: //min heapified
                        break;
                    case 29: //smooth heapified
                        break;
                    case 30: //poplar heapified
                        break;
                    case 31: //triangular heapified
                        break;
                    case 32: //first circle run
                        RandomShuffle(arr, 0, arr.Length);

                        int nw = 1;
                        for (; nw < arr.Length; nw *= 2)
                        {
                            ;
                        }

                        circleSortRoutine(arr, 0, nw - 1, arr.Length);
                        void circleSortRoutine(int[] array, int lo, int hi, int end)
                        {
                            if (lo == hi)
                            {
                                return;
                            }

                            int high = hi;
                            int low = lo;
                            int mid = (hi - lo) / 2;

                            while (lo < hi)
                            {
                                if (hi < end && lo > hi)
                                {
                                    Swap(array, lo, hi);
                                }

                                lo++;
                                hi--;
                            }

                            circleSortRoutine(array, low, low + mid, end);
                            if (low + mid + 1 < end)
                            {
                                circleSortRoutine(array, low + mid + 1, high, end);
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 33: //final pairwise pass
                        RandomShuffle(arr, 0, arr.Length);

                        //create pairs
                        for (int i = 1; i < arr.Length; i += 2)
                        {
                            if (CompareValues(i - 1, i) > 0)
                            {
                                Swap(arr, i - 1, i);
                            }
                        }

                        int[] temps = new int[arr.Length];

                        //sort the smaller and larger of the pairs separately with pigeonhole sort
                        for (int mq = 0; mq < 2; mq++)
                        {
                            for (int kd = mq; kd < arr.Length; kd += 2)
                            {
                                Write(temps, arr[kd], temps[arr[kd]] + 1);
                            }

                            int i = 0, j = mq;
                            while (true)
                            {
                                while (i < arr.Length && temps[i] == 0)
                                {
                                    i++;
                                }

                                if (i >= arr.Length)
                                {
                                    break;
                                }

                                Write(arr, j, i);

                                j += 2;
                                Write(temps, i, temps[i] - 1);
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 34: //recursive reversal
                        reversalRec(arr, 0, arr.Length);
                        void reversalRec(int[] array, int a, int b)
                        {
                            if (b - a < 2)
                            {
                                return;
                            }

                            Reversal(array, a, b - 1);

                            int m = (a + b) / 2;
                            reversalRec(array, a, m);
                            reversalRec(array, m, b);
                        }
                        AddHistorySnap();
                        break;
                    case 35: //gray code fractal
                        reversalRec2(arr, 0, arr.Length, false);
                        void reversalRec2(int[] array, int a, int b, bool bw)
                        {
                            if (b - a < 3)
                            {
                                return;
                            }

                            int m = (a + b) / 2;

                            if (bw)
                            {
                                Reversal(array, a, m - 1);
                            }
                            else
                            {
                                Reversal(array, m, b - 1);
                            }

                            reversalRec2(array, a, m, false);
                            reversalRec2(array, m, b, true);
                        }
                        AddHistorySnap();
                        break;
                    case 36: //sierpinski triangle
                        int[] triangle = new int[arr.Length];
                        triangleRec(triangle, 0, arr.Length);

                        int[] temph = new int[arr.Length];
                        Array.Copy(arr, temph, arr.Length);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, temph[triangle[i]]);
                        }

                        void triangleRec(int[] array, int a, int b)
                        {
                            if (b - a < 2)
                            {
                                return;
                            }

                            if (b - a == 2)
                            {
                                array[a + 1]++;
                                return;
                            }

                            int h = (b - a) / 3, t1 = (a + a + b) / 3, t2 = (a + b + b + 2) / 3;
                            for (int i = a; i < t1; i++)
                            {
                                array[i] += h;
                            }

                            for (int i = t1; i < t2; i++)
                            {
                                array[i] += 2 * h;
                            }

                            triangleRec(array, a, t1);
                            triangleRec(array, t1, t2);
                            triangleRec(array, t2, b);
                        }
                        AddHistorySnap();
                        break;
                    case 37: //triangular input
                        int[] triangleq = new int[arr.Length];

                        int jq = 0, kq = 2;
                        int max = 0;

                        for (int i = 1; i < arr.Length; i++, jq++)
                        {
                            if (i == kq)
                            {
                                jq = 0;
                                kq *= 2;
                            }
                            triangleq[i] = triangleq[jq] + 1;
                            if (triangleq[i] > max)
                            {
                                max = triangleq[i];
                            }
                        }
                        int[] cnt = new int[max + 1];

                        for (int i = 0; i < arr.Length; i++)
                        {
                            cnt[triangleq[i]]++;
                        }

                        for (int i = 1; i < cnt.Length; i++)
                        {
                            cnt[i] += cnt[i - 1];
                        }

                        for (int i = arr.Length - 1; i >= 0; i--)
                        {
                            triangleq[i] = --cnt[triangleq[i]];
                        }

                        int[] tempc = new int[arr.Length];
                        Array.Copy(arr, tempc, arr.Length);
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, tempc[triangleq[i]]);
                        }

                        AddHistorySnap();
                        break;
                    case 38: //quicksort adversary
                        for (int j = arr.Length - arr.Length % 2 - 2, i = j - 1; i >= 0; i -= 2, j--)
                        {
                            Swap(arr, i, j);
                        }

                        AddHistorySnap();
                        break;
                    case 39: //pdq adversary

                        break;
                    case 40: //grailsort adversary
                        if (arr.Length <= 16)
                        {
                            Reversal(arr, 0, arr.Length - 1);
                        }
                        else
                        {
                            int blockLen = 1;
                            while (blockLen * blockLen < arr.Length)
                            {
                                blockLen *= 2;
                            }

                            int numKeys = (arr.Length - 1) / blockLen + 1;
                            int keys = blockLen + numKeys;

                            RandomShuffle(arr, 0, arr.Length);
                            Sort(arr, 0, keys);
                            Reversal(arr, 0, keys - 1);
                            Sort(arr, keys, arr.Length);

                            push(arr, keys, arr.Length, blockLen);
                        }
                        void rotate(int[] array, int a, int m, int b)
                        {
                            Reversal(array, a, m - 1);
                            Reversal(array, m, b - 1);
                            Reversal(array, a, b - 1);
                        }

                        void push(int[] array, int a, int b, int bLen)
                        {
                            int len = b - a,
                                b1 = b - len % bLen, len1 = b1 - a;
                            if (len1 <= 2 * bLen)
                            {
                                return;
                            }

                            int m = bLen;
                            while (2 * m < len)
                            {
                                m *= 2;
                            }

                            m += a;

                            if (b1 - m < bLen)
                            {
                                push(array, a, m, bLen);
                            }
                            else
                            {
                                m = a + b1 - m;
                                rotate(array, m - (bLen - 2), b1 - (bLen - 1), b1);
                                MultiSwap(array, a, m);
                                rotate(array, a, m, b1);
                                m = a + b1 - m;

                                push(array, a, m, bLen);
                                push(array, m, b, bLen);
                            }
                        }
                        AddHistorySnap();
                        break;
                    case 41: //shuffle merge adversary
                        int nq = arr.Length;

                        int[] tmp = new int[nq];
                        int d = 2, end = 1 << (int)(Math.Log(nq - 1) / Math.Log(2) + 1);

                        while (d <= end)
                        {
                            int i = 0, dec = 0;

                            while (i < nq)
                            {
                                int j = i;
                                dec += nq;
                                while (dec >= d)
                                {
                                    dec -= d;
                                    j++;
                                }
                                int kf = j;
                                dec += nq;
                                while (dec >= d)
                                {
                                    dec -= d;
                                    kf++;
                                }
                                shuffleMergeBad(arr, tmp, i, j, kf);
                                i = kf;
                            }
                            d *= 2;
                        }
                        void shuffleMergeBad(int[] array, int[] tmp, int a, int m, int b)
                        {
                            if ((b - a) % 2 == 1)
                            {
                                if (m - a > b - m)
                                {
                                    a++;
                                }
                                else
                                {
                                    b--;
                                }
                            }
                            shuffleBad(array, tmp, a, b);
                        }

                        //length is always even
                        void shuffleBad(int[] array, int[] tmp, int a, int b)
                        {
                            if (b - a < 2)
                            {
                                return;
                            }

                            int m = (a + b) / 2;
                            int s = (b - a - 1) / 4 + 1;

                            a = m - s;
                            b = m + s;
                            int j = a;

                            for (int i = a + 1; i < b; i += 2)
                            {
                                Write(tmp, j++, array[i]);
                            }

                            for (int i = a; i < b; i += 2)
                            {
                                Write(tmp, j++, array[i]);
                            }

                            Array.Copy(tmp, a, array, a, b - a);
                        }
                        AddHistorySnap();
                        break;
                    case 42: //bit reversal
                        int leng = 1 << (int)(Math.Log(arr.Length) / Math.Log(2));
                        bool pow2 = leng == arr.Length;

                        int[] tempb = new int[arr.Length];
                        Array.Copy(arr, tempb, arr.Length);
                        for (int i = 0; i < leng; i++)
                        {
                            arr[i] = i;
                        }

                        int mb = 0;
                        int d1 = leng >> 1, d2 = d1 + (d1 >> 1);

                        for (int i = 1; i < leng - 1; i++)
                        {
                            int j = d1;

                            for (
                                int kb = i, nb = d2;
                                (kb & 1) == 0;
                                j -= nb, kb >>= 1, nb >>= 1
                            )
                            {
                                ;
                            }

                            mb += j;
                            if (mb > i)
                            {
                                Swap(arr, i, mb);
                            }
                        }

                        if (!pow2)
                        {
                            for (int i = leng; i < arr.Length; i++)
                            {
                                Write(arr, i, arr[i - leng]);
                            }

                            int[] cntq = new int[leng];

                            for (int i = 0; i < arr.Length; i++)
                            {
                                cntq[arr[i]]++;
                            }

                            for (int i = 1; i < cntq.Length; i++)
                            {
                                cntq[i] += cntq[i - 1];
                            }

                            for (int i = arr.Length - 1; i >= 0; i--)
                            {
                                Write(arr, i, --cntq[arr[i]]);
                            }
                        }
                        int[] bits = new int[arr.Length];
                        Array.Copy(arr, bits, arr.Length);

                        for (int i = 0; i < arr.Length; i++)
                        {
                            Write(arr, i, tempb[bits[i]]);
                        }

                        AddHistorySnap();
                        break;
                    case 43: //block random
                        int cl = arr.Length;
                        int blockSize = pow2lte((int)Math.Sqrt(cl));
                        _ = cl % blockSize;
                        for (int i = 0; i < arr.Length; i += blockSize)
                        {
                            int randomIndex = random.Next((arr.Length - i) / blockSize) * blockSize + i;
                            blockSwap(arr, i, randomIndex, blockSize);
                        }
                        void blockSwap(int[] array, int a, int b, int len)
                        {
                            for (int i = 0; i < len; i++)
                            {
                                Swap(array, a + i, b + i);
                            }
                        }

                        int pow2lte(int value)
                        {
                            int val;
                            for (val = 1; val <= value; val <<= 1)
                            {
                                ;
                            }

                            return val >> 1;
                        }
                        AddHistorySnap();
                        break;
                    case 44: //block reverse
                        int cl2 = arr.Length;
                        int blockSizez = pow2lte2((int)Math.Sqrt(cl2));
                        cl2 -= cl2 % blockSizez;

                        int i1 = 0, j1 = cl2 - blockSizez;
                        while (i1 < j1)
                        {
                            blockSwap2(arr, i1, j1, blockSizez);
                            i1 += blockSizez;
                            j1 -= blockSizez;
                        }
                        void blockSwap2(int[] array, int a, int b, int len)
                        {
                            for (int i = 0; i < len; i++)
                            {
                                Swap(array, a + i, b + i);
                            }
                        }

                        int pow2lte2(int value)
                        {
                            int val;
                            for (val = 1; val <= value; val <<= 1)
                            {
                                ;
                            }

                            return val >> 1;
                        }
                        AddHistorySnap();
                        break;
                    default: //none
                        AddHistorySnap();
                        break;

                }
                DrawHistory();
            }
            isPremade = false;
            shuffling = false;

        }

        // Returns floor(log2(n)), assumes n > 0.
        public int PdqLog(int n)
        {
            int log = 0;
            while ((n >>= 1) != 0)
            {
                ++log;
            }

            return log;
        }

        public void Sort(int[] array, int start, int end)
        {
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
                Write(holes, array[i] - min, holes[array[i] - min] + 1);
            }

            for (int i = 0, j = start; i < size; i++)
            {
                while (holes[i] > 0)
                {
                    Write(holes, i, holes[i] - 1);
                    Write(array, j, i + min);
                    j++;
                }
            }
        }

        public void MultiSwap(int[] array, int pos, int to)
        {
            if (to - pos > 0)
            {
                for (int i = pos; i < to; i++)
                {
                    Swap(array, i, i + 1);
                }
            }
            else
            {
                for (int i = pos; i > to; i--)
                {
                    Swap(array, i, i - 1);
                }
            }
        }

        private void Visualize_Click(object sender, RoutedEventArgs e)
        {
            //initialize everything
            if (timer != null) { timer.Stop(); }
            isPaused = false;
            comparisons = 0;
            arrAccesses = 0;
            swaps = 0;
            writes = 0;
            reversals = 0;
            CompsText.Text = "Comparisons: 0";
            SwapsText.Text = "Swaps: 0";
            WritesText.Text = "Writes: 0";
            ReversalsText.Text = "Reversals: 0";

            selectedHistory = new List<int[]>();
            sortHistory = new List<int[]>();
            ResetPauseButtonText();


            switch (comboBox.Content)
            {
                case "Merge sort":
                    MergeSort(arr, arr.Length, false);
                    DrawHistory();
                    break;
                case "Insertion sort":
                    InsertionSort(arr, 0, arr.Length);
                    DrawHistory();
                    break;
                case "Quick (left/left) sort":
                    QuickSortLL(arr, 0, arr.Length - 1);
                    DrawHistory();
                    break;
                case "Bubble sort":
                    BubbleSort(arr, arr.Length);
                    DrawHistory();
                    break;
                case "Selection sort":
                    SelectionSort(arr, arr.Length);
                    DrawHistory();
                    break;
                case "Max heap sort":
                    HeapSort(arr, 0, arr.Length, true);
                    DrawHistory();
                    break;
                case "Odd-even sort":
                    OddEvenSort(arr, arr.Length); //first custom sort, also the first to not be hardcoded to the program's system.
                    DrawHistory();
                    break;
                case "LSD radix sort":
                    RadixSort(arr);
                    DrawHistory();
                    break;
                case "Shell sort":
                    ShellSort(arr, arr.Length);
                    DrawHistory();
                    break;
                case "Cocktail shaker sort":
                    CocktailShakerSort(arr, arr.Length);
                    DrawHistory();
                    break;
                case "Move to back sort":
                    MoveToBackSort();
                    DrawHistory();
                    break;
                case "Weird insertion sort":
                    InsertionWhat(arr);
                    DrawHistory();
                    break;
                case "Grail sort":
                    GrailCommonSort(arr, 0, arr.Length, null, 0, 0);
                    DrawHistory();
                    break;
                case "Lazy stable sort":
                    GrailLazyStableSort(arr, 0, arr.Length);
                    DrawHistory();
                    break;
                case "Intro sort (Array.Sort on C#)":
                    IntroSortCS(arr, 0, arr.Length);
                    DrawHistory();
                    break;
                default:
                    MessageDialog dialog = new("You need to select an algorithm.", "Alert");
                    //dialog.ShowAsync();
                    break;
            }

        }

        private bool isLeftButtonDown;
        private void Canv_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            isLeftButtonDown = true;
        }

        private void Canv_MouseMove(object sender, PointerRoutedEventArgs e)
        {
            if (isLeftButtonDown && isPaused && e.GetCurrentPoint(canv).Position.X > 0 && e.GetCurrentPoint(canv).Position.X < canv.ActualWidth && e.GetCurrentPoint(canv).Position.Y > 0 && e.GetCurrentPoint(canv).Position.Y < canv.ActualHeight)
            {
                isPremade = true;
                PointerPoint a = e.GetCurrentPoint(canv);
                arr[(int)Math.Ceiling(a.Position.X / (canv.ActualWidth / arr.Length)) - 1] = arr.Length - (int)Math.Ceiling(a.Position.Y / (canv.ActualHeight / arr.Length));
                DrawNumbers(arr, null);
            }
            else
            {
                isLeftButtonDown = false;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, PointerRoutedEventArgs e)
        {
            isLeftButtonDown = false;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPremade)
            {
                MessageDialog dialog = new("To visualize a premade array click \"Visualize\"", "Alert");
                //dialog.ShowAsync();
                return;
            }
            if (timer == null)
            {
                MessageDialog dialog = new("There is no running preview.", "Alert");
                //dialog.ShowAsync();
                return;
            }
            if (isPaused)
            {
                PlayPreview();
            }
            else
            {
                PausePreview();
            }
        }

        private void PausePreview()
        {
            if (timer != null)
            {
                timer.Stop();
            }
            pauseButton.Content = "Play";
            isPaused = true;
        }

        private void PlayPreview()
        {
            if (timer != null)
            {
                timer.Start();
            }
            pauseButton.Content = "Pause";
            isPaused = false;
        }

        private void ResetPauseButtonText()
        {
            pauseButton.Content = "Pause";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (arr != null)
            {
                DrawNumbers(arr, selectedArr);
            }
        }

        public void AddHistorySnap()
        {
            int[] historySnap = new int[arr.Length];
            arr.CopyTo(historySnap, 0);
            sortHistory.Add(historySnap);
            selectedHistory.Add(selectedArr);
        }

        private void DrawNumbers(int[] arr, int[] selectedHistory)
        {
            canv.Children.Clear();

            int howMany = arr.Length;
            double size = canv.ActualWidth / howMany;

            for (int i = 0; i < howMany; i++)
            {
                Rectangle rect = new();
                Canvas.SetLeft(rect, size * i);
                Canvas.SetTop(rect, 0);
                rect.Width = size;
                rect.Height = (canv.ActualHeight - 5) / howMany * arr[i];
                rect.Fill = selectedHistory != null && selectedHistory.Contains(i) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Black);
                canv.Children.Add(rect);
            }
        }

        private void DrawHistory()
        {
            int counter = 0;
            timer = new DispatcherTimer();
            timer.Tick += (sender, e) =>
            {
                timer.Stop();
                arr = sortHistory[counter];
                selectedArr = selectedHistory[counter];
                DrawNumbers(sortHistory[counter], selectedHistory[counter]);
                counter++;
                if (counter < sortHistory.Count)
                {
                    timer.Interval = TimeSpan.FromMilliseconds(speedSlider.Value);
                    timer.Start();
                }
                else
                {
                    isPaused = true;
                    timer = null;
                    ResetPauseButtonText();
                }
            };
            timer.Start();
        }

        private void UpdateSwap()
        {
            swaps++;
            writes += 2;
            SwapsText.Text = "Swaps: " + swaps;
            WritesText.Text = "Writes: " + writes;
        }
        public void Swap(int[] array, int a, int b)
        {

            int temp = array[a];
            array[a] = array[b];
            array[b] = temp;
            selectedArr = new int[] { a, b };
            AddHistorySnap();
            UpdateSwap();
        }

        public void Write(int[] array, int at, int equals)
        {
            array[at] = equals;
            writes++;
            WritesText.Text = "Writes: " + writes;
            selectedArr = new int[] { at, equals };
            AddHistorySnap();
        }

        public int CompareValues(int left, int right)
        {
            comparisons++;
            CompsText.Text = "Comparisons: " + comparisons;
            //selectedArr = new int[] { arr[left], arr[right] };
            AddHistorySnap();
            if (left > right)
            {
                return 1;
            }
            else if (left < right)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public int CompareIndices(int[] array, int left, int right)
        {
            return CompareValues(array[left], array[right]);
        }

        public void Reversal(int[] array, int start, int length)
        {
            reversals++;
            ReversalsText.Text = "Reversals: " + reversals;

            for (int i = start; i < start + ((length - start + 1) / 2); i++)
            {
                Swap(array, i, start + length - i);
            }
        }

        // Taken from https://en.wikipedia.org/wiki/Gnome_sort
        private void SmartGnomeSort(int[] array, int lowerBound, int upperBound)
        {
            int pos = upperBound;

            while (pos > lowerBound && CompareValues(array[pos - 1], array[pos]) == 1)
            {
                Swap(array, pos - 1, pos);
                pos--;
                selectedArr = new int[] { pos };
                AddHistorySnap();
            }
        }

        public void GnomeCustomSort(int[] array, int low, int high)
        {
            for (int i = low + 1; i < high; i++)
            {
                SmartGnomeSort(array, low, i);
            }
        }
        private long allocAmount;
        public int[] CreateExternalArray(int length)
        {
            allocAmount += length;
            int[] result = new int[length];
            return result;
        }

        public void DeleteExternalArray(int[] array)
        {
            allocAmount -= array.Length;
        }

        public void ChangeReversals(int value)
        {
            reversals += value;
        }

        public void ChangeAllocAmount(int value)
        {
            allocAmount += value;
        }

        public void ClearAllocAmount()
        {
            allocAmount = 0;
        }

        public void VisualClear(int[] array, int index)
        {
            array[index] = -1;
        }

        //sorts----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------


        public void CustomBinaryInsert(int[] array, int start, int end)
        {
            BinaryInsertSort(array, start, end);
        }

        private void BinaryInsertSort(int[] array, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                int num = array[i];
                int lo = start, hi = i;

                while (lo < hi)
                {
                    int mid = lo + ((hi - lo) / 2); // avoid int overflow!

                    if (CompareValues(num, array[mid]) < 0)
                    { // do NOT move equal elements to right of inserted element; this maintains stability!
                        hi = mid;
                    }
                    else
                    {
                        lo = mid + 1;
                    }
                }

                // item has to go into position lo

                int j = i - 1;

                while (j >= lo)
                {
                    Write(array, j + 1, array[j]);
                    j--;
                }
                Write(array, lo, num);
            }
        }

        private void Merge(int[] array, int[] tmp, int start, int mid, int end, bool binary)
        {
            if (start == mid)
            {
                return;
            }

            Merge(array, tmp, start, (mid + start) / 2, mid, binary);
            Merge(array, tmp, mid, (mid + end) / 2, end, binary);

            if (end - start < 32 && binary)
            {
                return;
            }
            else if (end - start < 64 && binary)
            {
                CustomBinaryInsert(array, start, end);
            }
            else
            {
                int low = start;
                int high = mid;

                for (int nxt = 0; nxt < end - start; nxt++)
                {
                    if (low >= mid && high >= end)
                    {
                        break;
                    }

                    if (low < mid && high >= end)
                    {
                        Write(tmp, nxt, array[low++]);
                    }
                    else if (low >= mid && high < end)
                    {
                        Write(tmp, nxt, array[high++]);
                    }
                    else if (CompareValues(array[low], array[high]) <= 0)
                    {
                        Write(tmp, nxt, array[low++]);
                    }
                    else
                    {
                        Write(tmp, nxt, array[high++]);
                    }
                }
                for (int i = 0; i < end - start; i++)
                {
                    Write(array, start + i, tmp[i]);
                }
            }
        }

        private void MergeSort(int[] array, int length, bool binary)
        {

            if (length < 32 && binary)
            {
                CustomBinaryInsert(array, 0, length);
                return;
            }

            int[] tmp = CreateExternalArray(length);

            int start = 0;
            int end = length;
            int mid = start + ((end - start) / 2);

            Merge(array, tmp, start, mid, end, binary);

            DeleteExternalArray(tmp);
        }

        private void InsertionSort(int[] array, int start, int end)
        {
            int pos;
            int current;

            for (int i = start; i < end; i++)
            {
                current = array[i];
                pos = i - 1;

                while (pos >= start && CompareValues(array[pos], current) > 0)
                {
                    Write(array, pos + 1, array[pos]);
                    pos--;
                }
                Write(array, pos + 1, current);
            }
        }

        private int PartitionLL(int[] array, int lo, int hi)
        {
            int pivot = array[hi];
            int i = lo;

            for (int j = lo; j < hi; j++)
            {
                if (CompareValues(array[j], pivot) < 0)
                {
                    Swap(array, i, j);
                    i++;
                }
            }
            Swap(array, i, hi);
            return i;
        }

        private void QuickSortLL(int[] array, int lo, int hi)
        {
            if (lo < hi)
            {
                int p = PartitionLL(array, lo, hi);
                QuickSortLL(array, lo, p - 1);
                QuickSortLL(array, p + 1, hi);
            }
        }


        private void BubbleSort(int[] array, int length)
        {
            int consecSorted;
            for (int i = length - 1; i > 0; i -= consecSorted)
            {
                consecSorted = 1;
                for (int j = 0; j < i; j++)
                {
                    if (CompareIndices(array, j, j + 1) > 0)
                    {
                        Swap(array, j, j + 1);
                        consecSorted = 1;
                    }
                    else
                    {
                        consecSorted++;
                    }
                }
            }
        }

        private void SelectionSort(int[] array, int length)
        {
            for (int i = 0; i < length - 1; i++)
            {
                int lowestindex = i;

                for (int j = i + 1; j < length; j++)
                {
                    if (CompareValues(array[j], array[lowestindex]) == -1)
                    {
                        lowestindex = j;
                    }
                }
                Swap(array, i, lowestindex);
            }
        }

        private void SiftDown(int[] array, int root, int dist, int start, bool isMax)
        {
            int compareVal;
            if (isMax)
            {
                compareVal = -1;
            }
            else
            {
                compareVal = 1;
            }

            while (root <= dist / 2)
            {
                int leaf = 2 * root;
                if (leaf < dist && CompareValues(array[start + leaf - 1], array[start + leaf]) == compareVal)
                {
                    leaf++;
                }
                if (CompareValues(array[start + root - 1], array[start + leaf - 1]) == compareVal)
                {
                    Swap(array, start + root - 1, start + leaf - 1);
                    root = leaf;
                }
                else
                {
                    break;
                }
            }
        }

        private void Heapify(int[] arr, int low, int high, bool isMax)
        {
            int length = high - low;
            for (int i = length / 2; i >= 1; i--)
            {
                SiftDown(arr, i, length, low, isMax);
            }
        }

        // This version of heap sort works for max and min variants, alongside sorting 
        // partial ranges of an array.
        private void HeapSort(int[] arr, int start, int length, bool isMax)
        {
            Heapify(arr, start, length, isMax);

            for (int i = length - start; i > 1; i--)
            {
                Swap(arr, start, start + i - 1);
                SiftDown(arr, 1, i - 1, start, isMax);
            }

            if (!isMax)
            {
                Reversal(arr, start, start + length - 1);
            }
        }

        public void OddEvenSort(int[] array, int length)
        {
            bool sorted = false;

            while (!sorted)
            {
                sorted = true;

                for (int i = 1; i < length - 1; i += 2)
                {
                    if (CompareValues(array[i], array[i + 1]) == 1)
                    {
                        Swap(array, i, i + 1);
                        sorted = false;
                    }
                }

                for (int i = 0; i < length - 1; i += 2)
                {
                    if (CompareValues(array[i], array[i + 1]) == 1)
                    {
                        Swap(array, i, i + 1);
                        sorted = false;
                    }
                }
            }
        }

        public int[] RadixSort(int[] array)
        {
            bool isFinished = false;
            int digitPosition = 0;

            List<Queue<int>> buckets = new();
            InitializeBuckets(buckets);

            while (!isFinished)
            {
                isFinished = true;

                foreach (int value in array)
                {
                    int bucketNumber = GetBucketNumber(value, digitPosition);
                    if (bucketNumber > 0)
                    {
                        isFinished = false;
                    }

                    buckets[bucketNumber].Enqueue(value);
                    writes++;
                    selectedArr = new int[] { value };
                    AddHistorySnap();
                }

                int i = 0;
                foreach (Queue<int> bucket in buckets)
                {
                    while (bucket.Count > 0)
                    {
                        array[i] = bucket.Dequeue();
                        i++;
                        writes++;
                        selectedArr = new int[] { i };
                        AddHistorySnap();
                    }

                }

                digitPosition++;
            }

            return array;
        }

        private int GetBucketNumber(int value, int digitPosition)
        {
            int bucketNumber = value / (int)Math.Pow(10, digitPosition) % 10;
            return bucketNumber;
        }

        private void InitializeBuckets(List<Queue<int>> buckets)
        {
            for (int i = 0; i < 10; i++)
            {
                Queue<int> q = new();
                buckets.Add(q);
                writes++;
                selectedArr = new int[] { i };
                AddHistorySnap();
            }
        }
        private readonly int[] OriginalGaps = { 2048, 1024, 512, 256, 128, 64, 32, 16, 8, 4, 2, 1 };
        private readonly int[] PowTwoPlusOneGaps = { 2049, 1025, 513, 257, 129, 65, 33, 17, 9, 5, 3, 1 };
        private readonly int[] PowTwoMinusOneGaps = { 4095, 2047, 1023, 511, 255, 127, 63, 31, 15, 7, 3, 1 };
        private readonly int[] ThreeSmoothGaps = {3888, 3456, 3072, 2916, 2592, 2304, 2187, 2048, 1944, 1728,
                                                  1536, 1458, 1296, 1152, 1024, 972, 864, 768, 729, 648, 576,
                                                  512, 486, 432, 384, 324, 288, 256, 243, 216, 192, 162, 144,
                                                  128, 108, 96, 81, 72, 64, 54, 48, 36, 32, 27, 24, 18, 16, 12,
                                                  9, 8, 6, 4, 3, 2, 1};
        private readonly int[] PowersOfThreeGaps = { 3280, 1093, 364, 121, 40, 13, 4, 1 };
        private readonly int[] SedgewickIncerpiGaps = { 1968, 861, 336, 112, 48, 21, 7, 3, 1 };
        private readonly int[] SedgewickGaps = { 1073, 281, 77, 23, 8, 1 };
        private readonly int[] OddEvenSedgewickGaps = { 3905, 2161, 929, 505, 209, 109, 41, 19, 5, 1 };
        private readonly int[] GonnetBaezaYatesGaps = { 1861, 846, 384, 174, 79, 36, 16, 7, 3, 1 };
        private readonly int[] TokudaGaps = { 2660, 1182, 525, 233, 103, 46, 20, 9, 4, 1 };
        private readonly int[] CiuraGaps = { 1750, 701, 301, 132, 57, 23, 10, 4, 1 };
        private readonly int[] ExtendedCiuraGaps = { 8861, 3938, 1750, 701, 301, 132, 57, 23, 10, 4, 1 };
        private void ShellSort(int[] array, int length)
        {
            int[] incs = ExtendedCiuraGaps;

            for (int k = 0; k < incs.Length; k++)
            {
                if (incs == PowersOfThreeGaps)
                {
                    if (incs[k] < length / 3)
                    {
                        for (int h = incs[k], i = h; i < length; i++)
                        {

                            int v = array[i];
                            int j = i;

                            while (j >= h && CompareValues(array[j - h], v) == 1)
                            {
                                Write(array, j, array[j - h]);
                                j -= h;
                            }
                            Write(array, j, v);
                        }
                    }
                }
                else
                {
                    if (incs[k] < length)
                    {
                        for (int h = incs[k], i = h; i < length; i++)
                        {
                            //ArrayVisualizer.setCurrentGap(incs[k]);

                            int v = array[i];
                            int j = i;

                            while (j >= h && CompareValues(array[j - h], v) == 1)
                            {
                                Write(array, j, array[j - h]);
                                j -= h;
                            }
                            Write(array, j, v);
                        }
                    }
                }
            }
        }

        private void QuickShellSort(int[] array, int lo, int hi)
        {
            int[] incs = { 48, 21, 7, 3, 1 };

            for (int k = 0; k < incs.Length; k++)
            {
                for (int h = incs[k], i = h + lo; i < hi; i++)
                {
                    int v = array[i];
                    int j = i;

                    while (j >= h && CompareValues(array[j - h], v) == 1)
                    {

                        Write(array, j, array[j - h]);
                        j -= h;
                    }
                    Write(array, j, v);
                }
            }
        }

        private int Cur_List_Ptr = 0;
        public void MoveToBackSort()
        {
            if (Cur_List_Ptr >= arr.Length - 1)
            {
                Cur_List_Ptr = 0;
            }

            if (arr[Cur_List_Ptr] > arr[Cur_List_Ptr + 1])
            {
                Turn_Around(Cur_List_Ptr);
            }
            Cur_List_Ptr++;
        }

        private void Turn_Around(int Cur_Ptr)
        {
            int Temp_Value = arr[Cur_List_Ptr];
            int Ending = arr.Length - 1;
            for (int i = Cur_List_Ptr; i < Ending; i++)
            {
                arr[i] = arr[i + 1];
                selectedArr = new int[] { i };
                AddHistorySnap();
            }
            arr[Ending] = Temp_Value;
            selectedArr = new int[] { Ending };
        }

        public void InsertionWhat(int[] MyArray)
        {
            bool sorted = false;
            // while is not sorted
            while (!sorted)
            {
                sorted = true;
                for (int i = 1; i < MyArray.Length; i++)
                {
                    int j = i;

                    // Insert MyArray[i] into list 0..i-1 
                    while (j > 0 && CompareValues(MyArray[j], MyArray[j - 1]) < 0)
                    {
                        // Swap MyArray[j] and MyArray[j-1] 
                        Swap(arr, i, j - 1);
                        selectedArr = new int[] { i, j };
                        AddHistorySnap();

                        // Decrement j by 1 
                        j--;
                        sorted = false;
                    }
                }
            }
        }
        public void CocktailShakerSort(int[] array, int length)
        {
            for (int start = 0, end = length - 1; start < end;)
            {
                int consecSorted = 1;
                for (int i = start; i < end; i++)
                {
                    if (CompareIndices(array, i, i + 1) > 0)
                    {
                        Swap(array, i, i + 1);
                        consecSorted = 1;
                    }
                    else
                    {
                        consecSorted++;
                    }
                }
                end -= consecSorted;

                consecSorted = 1;
                for (int i = end; i > start; i--)
                {
                    if (CompareIndices(array, i - 1, i) > 0)
                    {
                        Swap(array, i - 1, i);
                        consecSorted = 1;
                    }
                    else
                    {
                        consecSorted++;
                    }
                }
                start += consecSorted;
            }
        }
        public void IntroSortCS(Array keys, int index, int length)
        {

            if (length <= 1)
            {
                return;
            }

            SortCS(index, length);
            return;
        }
        public int MaxLength =>
            // Keep in sync with `inline SIZE_T MaxArrayLength()` from gchelpers and HashHelpers.MaxPrimeArrayLength.
            0X7FFFFFC7;
        internal const int IntrosortSizeThreshold = 16;

        internal void SwapIfGreater(int a, int b)
        {
            if (a != b)
            {
                if (CompareValues(arr[a], arr[b]) > 0)
                {
                    Swap(arr, arr[a], arr[b]);
                }
            }
        }

        internal void SortCS(int left, int length)
        {
            IntrospectiveSort(left, length);
        }

        private void IntrospectiveSort(int left, int length)
        {
            if (length < 2)
                return;
            int logarithm = (int)Math.Log((uint)length, 2);
            IntroSort(left, length + left - 1, 2 * (logarithm + 1));

        }

        private void IntroSort(int lo, int hi, int depthLimit)
        {
            Debug.Assert(hi >= lo);
            Debug.Assert(depthLimit >= 0);

            while (hi > lo)
            {
                int partitionSize = hi - lo + 1;
                if (partitionSize <= IntrosortSizeThreshold)
                {
                    Debug.Assert(partitionSize >= 2);

                    if (partitionSize == 2)
                    {
                        SwapIfGreater(lo, hi);
                        return;
                    }

                    if (partitionSize == 3)
                    {
                        SwapIfGreater(lo, hi - 1);
                        SwapIfGreater(lo, hi);
                        SwapIfGreater(hi - 1, hi);
                        return;
                    }

                    InsertionSort(lo, hi);
                    return;
                }

                if (depthLimit == 0)
                {
                    Heapsort(lo, hi);
                    return;
                }
                depthLimit--;

                int p = PickPivotAndPartition(lo, hi);
                IntroSort(p + 1, hi, depthLimit);
                hi = p - 1;
            }
        }

        private int PickPivotAndPartition(int lo, int hi)
        {
            Debug.Assert(hi - lo >= IntrosortSizeThreshold);

            // Compute median-of-three.  But also partition them, since we've done the comparison.
            int mid = lo + (hi - lo) / 2;

            // Sort lo, mid and hi appropriately, then pick mid as the pivot.
            SwapIfGreater(lo, mid);
            SwapIfGreater(lo, hi);
            SwapIfGreater(mid, hi);

            object pivot = arr[mid];
            Swap(arr, mid, hi - 1);
            int left = lo, right = hi - 1;  // We already partitioned lo and hi and put the pivot in hi - 1.  And we pre-increment & decrement below.

            while (left < right)
            {
                while (CompareValues(arr[++left], (int)pivot) < 0) ;
                while (CompareValues((int)pivot, arr[--right]) < 0) ;

                if (left >= right)
                    break;

                Swap(arr, left, right);
            }

            // Put pivot in the right location.
            if (left != hi - 1)
            {
                Swap(arr, left, hi - 1);
            }
            return left;
        }

        private void Heapsort(int lo, int hi)
        {
            int n = hi - lo + 1;
            for (int i = n / 2; i >= 1; i--)
            {
                DownHeap(i, n, lo);
            }
            for (int i = n; i > 1; i--)
            {
                Swap(arr, lo, lo + i - 1);

                DownHeap(1, i - 1, lo);
            }
        }

        private void DownHeap(int i, int n, int lo)
        {
            object d = arr[lo + i - 1];
            int child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && CompareValues(arr[lo + child - 1], arr[lo + child]) < 0)
                {
                    child++;
                }
                if (!(CompareValues((int)d, arr[lo + child - 1]) < 0))
                    break;
                arr[lo + i - 1] = arr[lo + child - 1];
                i = child;
            }
            arr[lo + i - 1] = (int)d;
        }

        private void InsertionSort(int lo, int hi)
        {
            int i, j;
            object t;
            for (i = lo; i < hi; i++)
            {
                j = i;
                t = arr[i + 1];
                while (j >= lo && CompareValues((int)t, arr[j]) < 0)
                {
                    arr[j + 1] = arr[j];
                    j--;
                }
                arr[j + 1] = (int)t;
            }
        }


    //Grailsort-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------



    private readonly int grailStaticBufferLen = 32; //Buffer length changed due to less numbers in this program being sorted than what Mr. Astrelin used for testing.
        public int GetStaticBuffer()
        {
            return grailStaticBufferLen;
        }

        private void GrailSwap(int[] arr, int a, int b)
        {
            Swap(arr, a, b);
        }

        private void GrailMultiSwap(int[] arr, int a, int b, int swapsLeft)
        {
            while (swapsLeft != 0)
            {
                GrailSwap(arr, a++, b++);
                swapsLeft--;
                AddHistorySnap();
            }
        }

        private void GrailRotate(int[] array, int pos, int lenA, int lenB)
        {
            while (lenA != 0 && lenB != 0)
            {
                if (lenA <= lenB)
                {
                    GrailMultiSwap(array, pos, pos + lenA, lenA);
                    pos += lenA;
                    lenB -= lenA;
                }
                else
                {
                    GrailMultiSwap(array, pos + (lenA - lenB), pos + lenA, lenB);
                    lenA -= lenB;
                }
            }
        }

        private void GrailInsertSort(int[] arr, int pos, int len)
        {
            GnomeCustomSort(arr, pos, pos + len);
        }

        //boolean argument determines direction
        private int GrailBinSearch(int[] arr, int pos, int len, int keyPos, bool isLeft)
        {
            int left = -1, right = len;
            while (left < right - 1)
            {
                int mid = left + ((right - left) >> 1);
                if (isLeft)
                {
                    if (CompareValues(arr[pos + mid], arr[keyPos]) >= 0)
                    {
                        right = mid;
                    }
                    else
                    {
                        left = mid;
                    }
                }
                else
                {
                    if (CompareValues(arr[pos + mid], arr[keyPos]) > 0)
                    {
                        right = mid;
                    }
                    else
                    {
                        left = mid;
                    }
                }
            }
            return right;
        }

        // cost: 2 * len + numKeys^2 / 2
        private int GrailFindKeys(int[] arr, int pos, int len, int numKeys)
        {
            int dist = 1, foundKeys = 1, firstKey = 0;  // first key is always here

            while (dist < len && foundKeys < numKeys)
            {

                //Binary Search left
                int loc = GrailBinSearch(arr, pos + firstKey, foundKeys, pos + dist, true);
                if (loc == foundKeys || CompareValues(arr[pos + dist], arr[pos + (firstKey + loc)]) != 0)
                {
                    GrailRotate(arr, pos + firstKey, foundKeys, dist - (firstKey + foundKeys));
                    firstKey = dist - foundKeys;
                    GrailRotate(arr, pos + (firstKey + loc), foundKeys - loc, 1);
                    foundKeys++;
                }
                else
                {
                }

                dist++;
            }
            GrailRotate(arr, pos, firstKey, foundKeys);

            return foundKeys;
        }

        // cost: min(len1, len2)^2 + max(len1, len2)
        public void GrailMergeWithoutBuffer(int[] arr, int pos, int len1, int len2)
        {
            if (len1 < len2)
            {
                while (len1 != 0)
                {
                    //Binary Search left
                    int loc = GrailBinSearch(arr, pos + len1, len2, pos, true);
                    if (loc != 0)
                    {
                        GrailRotate(arr, pos, len1, loc);
                        pos += loc;
                        len2 -= loc;
                    }
                    if (len2 == 0)
                    {
                        break;
                    }

                    do
                    {
                        pos++;
                        len1--;
                    } while (len1 != 0 && CompareValues(arr[pos], arr[pos + len1]) <= 0);
                }
            }
            else
            {
                while (len2 != 0)
                {
                    //Binary Search right
                    int loc = GrailBinSearch(arr, pos, len1, pos + (len1 + len2 - 1), false);
                    if (loc != len1)
                    {
                        GrailRotate(arr, pos + loc, len1 - loc, len2);
                        len1 = loc;
                    }
                    if (len1 == 0)
                    {
                        break;
                    }

                    do
                    {
                        len2--;
                    } while (len2 != 0 && CompareValues(arr[pos + len1 - 1], arr[pos + len1 + len2 - 1]) <= 0);
                }
            }
        }

        // arr - starting array. arr[0 - regBlockLen..-1] - buffer (if havebuf).
        // regBlockLen - length of regular blocks. First blockCount blocks are stable sorted by 1st elements and key-coded
        // keysPos - arrays of keys, in same order as blocks. keysPos < midkey means stream A
        // aBlockCount are regular blocks from stream A.
        // lastLen is length of last (irregular) block from stream B, that should go before nblock2 blocks.
        // lastLen = 0 requires aBlockCount = 0 (no irregular blocks). lastLen > 0, aBlockCount = 0 is possible.
        private void GrailMergeBuffersLeft(int[] arr, int keysPos, int midkey, int pos,
                int blockCount, int blockLen, bool havebuf, int aBlockCount,
                int lastLen)
        {

            if (blockCount == 0)
            {
                int aBlocksLen = aBlockCount * blockLen;
                if (havebuf)
                {
                    GrailMergeLeft(arr, pos, aBlocksLen, lastLen, 0 - blockLen);
                }
                else
                {
                    GrailMergeWithoutBuffer(arr, pos, aBlocksLen, lastLen);
                }

                return;
            }

            int leftOverLen = blockLen;
            int leftOverFrag = CompareValues(arr[keysPos], arr[midkey]) < 0 ? 0 : 1;
            int processIndex = blockLen;
            int restToProcess;

            for (int keyIndex = 1; keyIndex < blockCount; keyIndex++, processIndex += blockLen)
            {
                restToProcess = processIndex - leftOverLen;
                int nextFrag = CompareValues(arr[keysPos + keyIndex], arr[midkey]) < 0 ? 0 : 1;

                if (nextFrag == leftOverFrag)
                {
                    if (havebuf)
                    {
                        GrailMultiSwap(arr, pos + restToProcess - blockLen, pos + restToProcess, leftOverLen);
                    }

                    leftOverLen = blockLen;
                }
                else
                {
                    if (havebuf)
                    {
                        GrailPair results = GrailSmartMergeWithBuffer(arr, pos + restToProcess, leftOverLen, leftOverFrag, blockLen);
                        leftOverLen = results.GetLeftOverLen();
                        leftOverFrag = results.GetLeftOverFrag();
                    }
                    else
                    {
                        GrailPair results = GrailSmartMergeWithoutBuffer(arr, pos + restToProcess, leftOverLen, leftOverFrag, blockLen);
                        leftOverLen = results.GetLeftOverLen();
                        leftOverFrag = results.GetLeftOverFrag();
                    }
                }
            }
            restToProcess = processIndex - leftOverLen;

            if (lastLen != 0)
            {
                if (leftOverFrag != 0)
                {
                    if (havebuf)
                    {
                        GrailMultiSwap(arr, pos + restToProcess - blockLen, pos + restToProcess, leftOverLen);
                    }
                    restToProcess = processIndex;
                    leftOverLen = blockLen * aBlockCount;
                }
                else
                {
                    leftOverLen += blockLen * aBlockCount;
                }
                if (havebuf)
                {
                    GrailMergeLeft(arr, pos + restToProcess, leftOverLen, lastLen, -blockLen);
                }
                else
                {
                    GrailMergeWithoutBuffer(arr, pos + restToProcess, leftOverLen, lastLen);
                }
            }
            else
            {
                if (havebuf)
                {
                    GrailMultiSwap(arr, pos + restToProcess, pos + (restToProcess - blockLen), leftOverLen);
                }
            }
        }

        // arr[dist..-1] - buffer, arr[0, leftLen - 1] ++ arr[leftLen, leftLen + rightLen - 1]
        // -> arr[dist, dist + leftLen + rightLen - 1]
        private void GrailMergeLeft(int[] arr, int pos, int leftLen, int rightLen, int dist)
        {
            int left = 0;
            int right = leftLen;

            rightLen += leftLen;

            while (right < rightLen)
            {
                if (left == leftLen || CompareValues(arr[pos + left], arr[pos + right]) > 0)
                {
                    GrailSwap(arr, pos + (dist++), pos + (right++));
                }
                else
                {
                    GrailSwap(arr, pos + (dist++), pos + (left++));
                }

                AddHistorySnap();
            }

            if (dist != left)
            {
                GrailMultiSwap(arr, pos + dist, pos + left, leftLen - left);
            }
        }
        private void GrailMergeRight(int[] arr, int pos, int leftLen, int rightLen, int dist)
        {
            int mergedPos = leftLen + rightLen + dist - 1;
            int right = leftLen + rightLen - 1;
            int left = leftLen - 1;

            while (left >= 0)
            {
                if (right < leftLen || CompareValues(arr[pos + left], arr[pos + right]) > 0)
                {
                    GrailSwap(arr, pos + (mergedPos--), pos + (left--));
                }
                else
                {
                    GrailSwap(arr, pos + (mergedPos--), pos + (right--));
                }
            }

            if (right != mergedPos)
            {
                while (right >= leftLen)
                {
                    GrailSwap(arr, pos + (mergedPos--), pos + (right--));
                }
            }
        }

        //returns the leftover length, then the leftover fragment
        private GrailPair GrailSmartMergeWithoutBuffer(int[] arr, int pos, int leftOverLen, int leftOverFrag, int regBlockLen)
        {
            if (regBlockLen == 0)
            {
                return new GrailPair(leftOverLen, leftOverFrag);
            }

            int len1 = leftOverLen;
            int len2 = regBlockLen;
            int typeFrag = 1 - leftOverFrag; //1 if inverted

            if (len1 != 0 && CompareValues(arr[pos + (len1 - 1)], arr[pos + len1]) - typeFrag >= 0)
            {

                while (len1 != 0)
                {
                    int foundLen;
                    if (typeFrag != 0)
                    {
                        //Binary Search left
                        foundLen = GrailBinSearch(arr, pos + len1, len2, pos, true);
                    }
                    else
                    {
                        //Binary Search right
                        foundLen = GrailBinSearch(arr, pos + len1, len2, pos, false);
                    }
                    if (foundLen != 0)
                    {
                        GrailRotate(arr, pos, len1, foundLen);
                        pos += foundLen;
                        len2 -= foundLen;
                    }
                    if (len2 == 0)
                    {
                        return new GrailPair(len1, leftOverFrag);
                    }
                    do
                    {
                        pos++;
                        len1--;
                    } while (len1 != 0 && CompareValues(arr[pos], arr[pos + len1]) - typeFrag < 0);
                }
            }
            return new GrailPair(len2, typeFrag);
        }

        //returns the leftover length, then the leftover fragment
        private GrailPair GrailSmartMergeWithBuffer(int[] arr, int pos, int leftOverLen, int leftOverFrag, int blockLen)
        {
            int dist = 0 - blockLen, left = 0, right = leftOverLen, leftEnd = right, rightEnd = right + blockLen;
            int typeFrag = 1 - leftOverFrag;  // 1 if inverted

            while (left < leftEnd && right < rightEnd)
            {
                if (CompareValues(arr[pos + left], arr[pos + right]) - typeFrag < 0)
                {
                    GrailSwap(arr, pos + (dist++), pos + (left++));
                }
                else
                {
                    GrailSwap(arr, pos + (dist++), pos + (right++));
                }
            }

            int length, fragment = leftOverFrag;
            if (left < leftEnd)
            {
                length = leftEnd - left;
                while (left < leftEnd)
                {
                    GrailSwap(arr, pos + (--leftEnd), pos + (--rightEnd));
                }
            }
            else
            {
                length = rightEnd - right;
                fragment = typeFrag;
            }
            return new GrailPair(length, fragment);
        }


        /***** Sort With Extra Buffer *****/

        //returns the leftover length, then the leftover fragment
        private GrailPair GrailSmartMergeWithXBuf(int[] arr, int pos, int leftOverLen, int leftOverFrag, int blockLen)
        {
            int dist = 0 - blockLen, left = 0, right = leftOverLen, leftEnd = right, rightEnd = right + blockLen;
            int typeFrag = 1 - leftOverFrag;  // 1 if inverted

            while (left < leftEnd && right < rightEnd)
            {
                if (CompareValues(arr[pos + left], arr[pos + right]) - typeFrag < 0)
                {
                    Write(arr, pos + dist++, arr[pos + left++]);
                }
                else
                {
                    Write(arr, pos + dist++, arr[pos + right++]);
                }
            }

            int length, fragment = leftOverFrag;
            if (left < leftEnd)
            {
                length = leftEnd - left;
                while (left < leftEnd)
                {
                    Write(arr, pos + --rightEnd, arr[pos + --leftEnd]);
                }
            }
            else
            {
                length = rightEnd - right;
                fragment = typeFrag;
            }
            return new GrailPair(length, fragment);
        }

        // arr[dist..-1] - free, arr[0, leftEnd - 1] ++ arr[leftEnd, leftEnd + rightEnd - 1]
        // -> arr[dist, dist + leftEnd + rightEnd - 1]
        private void GrailMergeLeftWithXBuf(int[] arr, int pos, int leftEnd, int rightEnd, int dist)
        {
            int left = 0;
            int right = leftEnd;
            rightEnd += leftEnd;


            while (right < rightEnd)
            {
                if (left == leftEnd || CompareValues(arr[pos + left], arr[pos + right]) > 0)
                {
                    Write(arr, pos + dist++, arr[pos + right++]);
                }
                else
                {
                    Write(arr, pos + dist++, arr[pos + left++]);
                }
            }

            if (dist != left)
            {
                while (left < leftEnd)
                {
                    Write(arr, pos + dist++, arr[pos + left++]);
                }

            }
        }

        // arr - starting array. arr[0 - regBlockLen..-1] - buffer (if havebuf).
        // regBlockLen - length of regular blocks. First blockCount blocks are stable sorted by 1st elements and key-coded
        // keysPos - where keys are in array, in same order as blocks. keysPos < midkey means stream A
        // aBlockCount are regular blocks from stream A.
        // lastLen is length of last (irregular) block from stream B, that should go before aCountBlock blocks.
        // lastLen = 0 requires aBlockCount = 0 (no irregular blocks). lastLen > 0, aBlockCount = 0 is possible.
        private void GrailMergeBuffersLeftWithXBuf(int[] arr, int keysPos, int midkey, int pos,
                int blockCount, int regBlockLen, int aBlockCount, int lastLen)
        {

            if (blockCount == 0)
            {
                int aBlocksLen = aBlockCount * regBlockLen;
                GrailMergeLeftWithXBuf(arr, pos, aBlocksLen, lastLen, 0 - regBlockLen);
                return;
            }

            int leftOverLen = regBlockLen;
            int leftOverFrag = CompareValues(arr[keysPos], arr[midkey]) < 0 ? 0 : 1;
            int processIndex = regBlockLen;

            int restToProcess;
            for (int keyIndex = 1; keyIndex < blockCount; keyIndex++, processIndex += regBlockLen)
            {
                restToProcess = processIndex - leftOverLen;
                int nextFrag = CompareValues(arr[keysPos + keyIndex], arr[midkey]) < 0 ? 0 : 1;

                if (nextFrag == leftOverFrag)
                {
                    Array.Copy(arr, pos + restToProcess, arr, pos + restToProcess - regBlockLen, leftOverLen);
                    leftOverLen = regBlockLen;
                }
                else
                {
                    GrailPair results = GrailSmartMergeWithXBuf(arr, pos + restToProcess, leftOverLen, leftOverFrag, regBlockLen);
                    leftOverLen = results.GetLeftOverLen();
                    leftOverFrag = results.GetLeftOverFrag();
                }
            }
            restToProcess = processIndex - leftOverLen;

            if (lastLen != 0)
            {
                if (leftOverFrag != 0)
                {
                    Array.Copy(arr, pos + restToProcess, arr, pos + restToProcess - regBlockLen, leftOverLen);

                    restToProcess = processIndex;
                    leftOverLen = regBlockLen * aBlockCount;
                }
                else
                {
                    leftOverLen += regBlockLen * aBlockCount;
                }
                GrailMergeLeftWithXBuf(arr, pos + restToProcess, leftOverLen, lastLen, 0 - regBlockLen);
            }
            else
            {
                Array.Copy(arr, pos + restToProcess, arr, pos + restToProcess - regBlockLen, leftOverLen);
            }
        }

        /***** End Sort With Extra Buffer *****/

        // build blocks of length buildLen
        // input: [-buildLen, -1] elements are buffer
        // output: first buildLen elements are buffer, blocks 2 * buildLen and last subblock sorted
        private void GrailBuildBlocks(int[] arr, int pos, int len, int buildLen,
                int[] extbuf, int bufferPos, int extBufLen)
        {

            int buildBuf = buildLen < extBufLen ? buildLen : extBufLen;
            while ((buildBuf & (buildBuf - 1)) != 0)
            {
                buildBuf &= buildBuf - 1;  // max power or 2 - just in case
            }

            int extraDist, part;
            if (buildBuf != 0)
            {
                Array.Copy(arr, pos - buildBuf, extbuf, bufferPos, buildBuf);

                for (int dist = 1; dist < len; dist += 2)
                {
                    extraDist = 0;
                    if (CompareValues(arr[pos + (dist - 1)], arr[pos + dist]) > 0)
                    {
                        extraDist = 1;
                    }

                    Write(arr, pos + dist - 3, arr[pos + dist - 1 + extraDist]);
                    Write(arr, pos + dist - 2, arr[pos + dist - extraDist]);
                }
                if (len % 2 != 0)
                {
                    Write(arr, pos + len - 3, arr[pos + len - 1]);
                }

                pos -= 2;

                for (part = 2; part < buildBuf; part *= 2)
                {
                    int left = 0;
                    int right = len - 2 * part;
                    while (left <= right)
                    {
                        GrailMergeLeftWithXBuf(arr, pos + left, part, part, 0 - part);
                        left += 2 * part;
                    }
                    int rest = len - left;

                    if (rest > part)
                    {
                        GrailMergeLeftWithXBuf(arr, pos + left, part, rest - part, 0 - part);
                    }
                    else
                    {
                        for (; left < len; left++)
                        {
                            Write(arr, pos + left - part, arr[pos + left]);
                        }
                    }
                    pos -= part;
                }
                Array.Copy(extbuf, bufferPos, arr, pos + len, buildBuf);
            }
            else
            {
                for (int dist = 1; dist < len; dist += 2)
                {
                    extraDist = 0;
                    if (CompareValues(arr[pos + (dist - 1)], arr[pos + dist]) > 0)
                    {
                        extraDist = 1;
                    }

                    GrailSwap(arr, pos + (dist - 3), pos + (dist - 1 + extraDist));
                    GrailSwap(arr, pos + (dist - 2), pos + (dist - extraDist));
                }
                if (len % 2 != 0)
                {
                    GrailSwap(arr, pos + (len - 1), pos + (len - 3));
                }

                pos -= 2;
                part = 2;
            }

            for (; part < buildLen; part *= 2)
            {
                int left = 0;
                int right = len - 2 * part;
                while (left <= right)
                {
                    GrailMergeLeft(arr, pos + left, part, part, 0 - part);
                    left += 2 * part;
                }
                int rest = len - left;
                if (rest > part)
                {
                    GrailMergeLeft(arr, pos + left, part, rest - part, 0 - part);
                }
                else
                {
                    GrailRotate(arr, pos + left - part, part, rest);
                }
                pos -= part;
            }
            int restToBuild = len % (2 * buildLen);
            int leftOverPos = len - restToBuild;

            if (restToBuild <= buildLen)
            {
                GrailRotate(arr, pos + leftOverPos, restToBuild, buildLen);
            }
            else
            {
                GrailMergeRight(arr, pos + leftOverPos, buildLen, restToBuild - buildLen, buildLen);
            }

            while (leftOverPos > 0)
            {
                leftOverPos -= 2 * buildLen;
                GrailMergeRight(arr, pos + leftOverPos, buildLen, buildLen, buildLen);
            }
        }

        // keys are on the left of arr. Blocks of length buildLen combined. We'll combine them in pairs
        // buildLen and nkeys are powers of 2. (2 * buildLen / regBlockLen) keys are guaranteed
        private void GrailCombineBlocks(int[] arr, int keyPos, int pos, int len, int buildLen,
                int regBlockLen, bool havebuf, int[] buffer, int bufferPos)
        {

            int combineLen = len / (2 * buildLen);
            int leftOver = len % (2 * buildLen);
            if (leftOver <= buildLen)
            {
                len -= leftOver;
                leftOver = 0;
            }

            if (buffer != null)
            {
                Array.Copy(arr, pos - regBlockLen, buffer, bufferPos, regBlockLen);
            }

            for (int i = 0; i <= combineLen; i++)
            {
                if (i == combineLen && leftOver == 0)
                {
                    break;
                }

                int blockPos = pos + i * 2 * buildLen;
                int blockCount = (i == combineLen ? leftOver : 2 * buildLen) / regBlockLen;

                GrailInsertSort(arr, keyPos, blockCount + (i == combineLen ? 1 : 0));

                int midkey = buildLen / regBlockLen;

                for (int index = 1; index < blockCount; index++)
                {
                    int leftIndex = index - 1;

                    for (int rightIndex = index; rightIndex < blockCount; rightIndex++)
                    {
                        int rightComp = CompareValues(arr[blockPos + leftIndex * regBlockLen],
                                                            arr[blockPos + rightIndex * regBlockLen]);
                        if (rightComp > 0 || (rightComp == 0 && CompareValues(arr[keyPos + leftIndex], arr[keyPos + rightIndex]) > 0))
                        {
                            leftIndex = rightIndex;
                        }
                    }
                    if (leftIndex != index - 1)
                    {
                        GrailMultiSwap(arr, blockPos + (index - 1) * regBlockLen, blockPos + leftIndex * regBlockLen, regBlockLen);
                        GrailSwap(arr, keyPos + (index - 1), keyPos + leftIndex);
                        if (midkey == index - 1 || midkey == leftIndex)
                        {
                            midkey ^= (index - 1) ^ leftIndex;
                        }
                    }
                }

                int aBlockCount = 0;
                int lastLen = 0;
                if (i == combineLen)
                {
                    lastLen = leftOver % regBlockLen;
                }

                if (lastLen != 0)
                {
                    while (aBlockCount < blockCount && CompareValues(arr[blockPos + blockCount * regBlockLen],
                                                                          arr[blockPos + (blockCount - aBlockCount - 1) * regBlockLen]) < 0)
                    {
                        aBlockCount++;
                    }
                }

                if (buffer != null)
                {
                    GrailMergeBuffersLeftWithXBuf(arr, keyPos, keyPos + midkey, blockPos,
                            blockCount - aBlockCount, regBlockLen, aBlockCount, lastLen);
                }
                else
                {
                    GrailMergeBuffersLeft(arr, keyPos, keyPos + midkey, blockPos,
                        blockCount - aBlockCount, regBlockLen, havebuf, aBlockCount, lastLen);
                }
            }
            if (buffer != null)
            {
                for (int i = len; --i >= 0;)
                {
                    Write(arr, pos + i, arr[pos + i - regBlockLen]);
                }

                Array.Copy(buffer, bufferPos, arr, pos - regBlockLen, regBlockLen);
            }
            else if (havebuf)
            {
                while (--len >= 0)
                {
                    GrailSwap(arr, pos + len, pos + len - regBlockLen);
                }
            }
        }

        public void GrailLazyStableSort(int[] arr, int pos, int len)
        {
            for (int dist = 1; dist < len; dist += 2)
            {
                if (CompareValues(arr[pos + dist - 1], arr[pos + dist]) > 0)
                {
                    GrailSwap(arr, pos + (dist - 1), pos + dist);
                }
            }

            for (int part = 2; part < len; part *= 2)
            {
                int left = 0;
                int right = len - 2 * part;

                while (left <= right)
                {
                    GrailMergeWithoutBuffer(arr, pos + left, part, part);
                    left += 2 * part;
                }

                int rest = len - left;
                if (rest > part)
                {
                    GrailMergeWithoutBuffer(arr, pos + left, part, rest - part);
                }
            }
        }

        public void GrailCommonSort(int[] arr, int pos, int len, int[] buffer, int bufferPos, int bufferLen)
        {

            if (len <= 16)
            {
                GrailInsertSort(arr, pos, len);
                return;
            }

            int blockLen = 1;
            while (blockLen * blockLen < len)
            {
                blockLen *= 2;
            }

            int numKeys = (len - 1) / blockLen + 1;

            int keysFound = GrailFindKeys(arr, pos, len, numKeys + blockLen);

            bool bufferEnabled = true;

            if (keysFound < numKeys + blockLen)
            {
                if (keysFound < 4)
                {
                    GrailLazyStableSort(arr, pos, len);
                    return;
                }
                numKeys = blockLen;
                while (numKeys > keysFound)
                {
                    numKeys /= 2;
                }

                bufferEnabled = false;
                blockLen = 0;
            }

            int dist = blockLen + numKeys;
            int buildLen = bufferEnabled ? blockLen : numKeys;

            if (bufferEnabled)
            {
                GrailBuildBlocks(arr, pos + dist, len - dist, buildLen, buffer, bufferPos, bufferLen);
            }
            else
            {
                GrailBuildBlocks(arr, pos + dist, len - dist, buildLen, null, bufferPos, 0);
            }

            // 2 * buildLen are built
            while (len - dist > (buildLen *= 2))
            {
                int regBlockLen = blockLen;
                bool buildBufEnabled = bufferEnabled;

                if (!bufferEnabled)
                {
                    if (numKeys > 4 && numKeys / 8 * numKeys >= buildLen)
                    {
                        regBlockLen = numKeys / 2;
                        buildBufEnabled = true;
                    }
                    else
                    {
                        int calcKeys = 1;
                        int i = buildLen * keysFound / 2;
                        while (calcKeys < numKeys && i != 0)
                        {
                            calcKeys *= 2;
                            i /= 8;
                        }
                        regBlockLen = (2 * buildLen) / calcKeys;
                    }
                }
                GrailCombineBlocks(arr, pos, pos + dist, len - dist, buildLen, regBlockLen, buildBufEnabled,
                        buildBufEnabled && regBlockLen <= bufferLen ? buffer : null, bufferPos);
            }

            GrailInsertSort(arr, pos, dist);
            GrailMergeWithoutBuffer(arr, pos, dist, len - dist);
        }

        private void GrailInPlaceMerge(int[] arr, int pos, int len1, int len2)
        {
            if (len1 < 3 || len2 < 3)
            {
                GrailMergeWithoutBuffer(arr, pos, len1, len2);
                return;
            }

            int midpoint;
            if (len1 < len2)
            {
                midpoint = len1 + len2 / 2;
            }
            else
            {
                midpoint = len1 / 2;
            }

            //Left binary search
            int len1Left, len1Right;
            len1Left = len1Right = GrailBinSearch(arr, pos, len1, pos + midpoint, true);

            //Right binary search
            if (len1Right < len1 && CompareValues(arr[pos + len1Right], arr[pos + midpoint]) == 0)
            {
                len1Right = GrailBinSearch(arr, pos + len1Left, len1 - len1Left, pos + midpoint, false) + len1Left;
            }

            int len2Left, len2Right;
            len2Left = len2Right = GrailBinSearch(arr, pos + len1, len2, pos + midpoint, true);

            if (len2Right < len2 && CompareValues(arr[pos + len1 + len2Right], arr[pos + midpoint]) == 0)
            {
                len2Right = GrailBinSearch(arr, pos + len1 + len2Left, len2 - len2Left, pos + midpoint, false) + len2Left;
            }

            if (len1Left == len1Right)
            {
                GrailRotate(arr, pos + len1Right, len1 - len1Right, len2Right);
            }
            else
            {
                GrailRotate(arr, pos + len1Left, len1 - len1Left, len2Left);

                if (len2Right != len2Left)
                {
                    GrailRotate(arr, pos + (len1Right + len2Left), len1 - len1Right, len2Right - len2Left);
                }
            }

            GrailInPlaceMerge(arr, pos + (len1Right + len2Right), len1 - len1Right, len2 - len2Right);
            GrailInPlaceMerge(arr, pos, len1Left, len2Left);
        }
        public void GrailInPlaceMergeSort(int[] arr, int start, int len)
        {
            for (int dist = start + 1; dist < len; dist += 2)
            {
                if (CompareValues(arr[dist - 1], arr[dist]) > 0)
                {
                    GrailSwap(arr, dist - 1, dist);
                }
            }
            for (int part = 2; part < len; part *= 2)
            {
                int left = start, right = len - 2 * part;

                while (left <= right)
                {
                    GrailInPlaceMerge(arr, left, part, part);
                    left += 2 * part;
                }

                int rest = len - left;
                if (rest > part)
                {
                    GrailInPlaceMerge(arr, left, part, rest - part);
                }
            }
        }






        private unsafe AudioFrame GenerateAudioData(uint samples)
        {
            // Buffer size is (number of samples) * (size of each sample)
            // We choose to generate single channel (mono) audio. For multi-channel, multiply by number of channels
            uint bufferSize = samples * sizeof(float);
            AudioFrame frame = new(bufferSize);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out byte* dataInBytes, out uint capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*)dataInBytes;

                float freq = 1000; // choosing to generate frequency of 1kHz
                float amplitude = 0.3f;
                int sampleRate = (int)graph.EncodingProperties.SampleRate;
                double sampleIncrement = freq * (Math.PI * 2) / sampleRate;

                // Generate a 1kHz sine wave and populate the values in the memory buffer
                for (int i = 0; i < samples; i++)
                {
                    for (int j = 0; j < arr.Length; j++)
                    {
                        dataInFloat[i] = amplitude * arr[j];
                    }
                    //double sinValue = amplitude * Math.Sin(theta);

                    theta += sampleIncrement;
                }
            }

            return frame;
        }

        private async Task CreateAudioGraph()
        {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new(AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                return;
            }

            graph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();
            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
            }

            deviceOutputNode = deviceOutputNodeResult.DeviceOutputNode;

            // Create the FrameInputNode at the same format as the graph, except explicitly set mono.
            AudioEncodingProperties nodeEncodingProperties = graph.EncodingProperties;
            nodeEncodingProperties.ChannelCount = 1;
            frameInputNode = graph.CreateFrameInputNode(nodeEncodingProperties);
            frameInputNode.AddOutgoingConnection(deviceOutputNode);

            // Initialize the Frame Input Node in the stopped state
            frameInputNode.Stop();

            // Hook up an event handler so we can start generating samples when needed
            // This event is triggered when the node is required to provide data
            frameInputNode.QuantumStarted += Node_QuantumStarted;

            // Start the graph since we will only start/stop the frame input node
            graph.Start();
        }

        private void Node_QuantumStarted(AudioFrameInputNode sender, FrameInputNodeQuantumStartedEventArgs args)
        {
            // GenerateAudioData can provide PCM audio data by directly synthesizing it or reading from a file.
            // Need to know how many samples are required. In this case, the node is running at the same rate as the rest of the graph
            // For minimum latency, only provide the required amount of samples. Extra samples will introduce additional latency.
            uint numSamplesNeeded = (uint)args.RequiredSamples;

            if (numSamplesNeeded != 0)
            {
                AudioFrame audioData = GenerateAudioData(numSamplesNeeded);
                frameInputNode.AddFrame(audioData);
            }
        }


    }
}
