using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Algo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //bool for the pause button && extra functionallity
        private bool isPaused;

        //current array of numbers (the one being shown)
        private int[] arr;

        //currently highlighted indexes
        private int[] selectedArr;

        //all sorting steps (arrays of numbers)
        private List<int[]> sortHistory;

        //all highlighted indexes during the sorting steps
        private List<int[]> selectedHistory;

        //timer that we'll use when drawing the array
        private DispatcherTimer timer;

        //for custom arrays
        private bool isPremade;

        //how many comparisons were needed for the sort in total
        private long comparisons;

        //how many array accesses were needed for the sort
        private long arrAccesses;
        public MainPage()
        {
            InitializeComponent();
            arr = new int[(int)ArraySize.Value];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = i + 1;
            }
            Array.Sort(arr);
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            ShuffleArray();
        }

        private void ShuffleArray()
        {
            //create a random starting array, if its not already premade
            if (!isPremade)
            {
                if (timer != null) { timer.Stop(); }
                isPaused = false;
                selectedHistory = new List<int[]>();
                sortHistory = new List<int[]>();
                Random random = new();
                arr = new int[(int)ArraySize.Value];
                for (int i = 0; i < arr.Length; i++)
                {
                    arr[i] = i + 1;
                }
                switch (shufflecomboBox.SelectedIndex)
                {
                    case 0: //random
                        for (int i = 0; i < arr.Length; i++)
                        {
                            int oldArrItem = arr[i];
                            int switchIndex = random.Next(i, arr.Length);
                            arr[i] = arr[switchIndex];
                            arr[switchIndex] = oldArrItem;
                            selectedArr = new int[] { i };
                            AddHistorySnap();
                        }
                        break;
                    case 1: //reversed
                        Array.Reverse(arr);
                        AddHistorySnap();
                        break;
                    case 2: //sorted
                        Array.Sort(arr);
                        AddHistorySnap();
                        break;
                    case 3: //reverse sorted
                        Array.Sort(arr);
                        Array.Reverse(arr);
                        AddHistorySnap();
                        break;
                    case 4: //none
                        break;
                    case 5: //max heapified
                        break;
                    default: //none, option 4
                        break;

                }
                DrawHistory();
            }
            isPremade = false;

        }

        private void Visualize_Click(object sender, RoutedEventArgs e)
        {
            //initialize everything
            if (timer != null) { timer.Stop(); }
            isPaused = false;
            comparisons = 0;
            arrAccesses = 0;
            selectedHistory = new List<int[]>();
            sortHistory = new List<int[]>();
            ResetPauseButtonText();
            //ShuffleArray();


            switch (comboBox.SelectedIndex)
            {
                case 0:
                    MergeSort(0, arr.Length);
                    DrawHistory();
                    break;
                case 1:
                    InsertionSort();
                    DrawHistory();
                    break;
                case 2:
                    QuickSort(0, arr.Length);
                    DrawHistory();
                    break;
                case 3:
                    BubbleSort();
                    DrawHistory();
                    break;
                case 4:
                    SelectionSort();
                    DrawHistory();
                    break;
                case 5:
                    HeapSort();
                    DrawHistory();
                    break;
                default:
                    MessageDialog dialog = new("You need to select an algorithm.", "Alert");
                    //dialog.ShowAsync();
                    break;
            }

            comparisonsTxt.Text = comparisons.ToString();
            arrayAccTxt.Text = arrAccesses.ToString();
            numsTxt.Text = ArraySize.Value.ToString();

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

        private void AddHistorySnap()
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
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(speedSlider.Value)
            };
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

        private int[] MergeSort(int startI, int endI)
        {
            int length = endI - startI;
            if (length == 1)
            {
                arrAccesses++;
                return new int[] { arr[startI] };
            }
            int[] A = MergeSort(startI, startI + length / 2);
            int[] B = MergeSort(startI + length / 2, endI);
            int[] AB = new int[A.Length + B.Length];
            int iA = 0;
            int iB = 0;

            for (int i = 0; i < AB.Length; i++)
            {

                arrAccesses += 4;
                comparisons++;
                if (iB < B.Length && (iA == A.Length || B[iB] < A[iA]))
                {
                    comparisons++;
                    if (iA != A.Length)
                    {
                        comparisons++;
                    }
                    AB[i] = B[iB];
                    arr[startI + i] = B[iB];
                    iB++;
                }
                else
                {
                    if (iB < B.Length)
                    {
                        comparisons += 2;
                    }

                    AB[i] = A[iA];
                    arr[startI + i] = A[iA];
                    iA++;
                }
                selectedArr = new int[] { startI + i };
                AddHistorySnap();
            }

            return AB;
        }

        private void InsertionSort()
        {
            for (int i = 1; i < arr.Length; i++)
            {
                int curr = i;
                while (curr - 1 >= 0 && arr[curr - 1] > arr[curr])
                {
                    arrAccesses += 6;

                    comparisons += 2;
                    int oldIValue = arr[curr];
                    arr[curr] = arr[curr - 1];
                    arr[curr - 1] = oldIValue;
                    curr--;
                }

                comparisons++;
                if (curr - 1 >= 0)
                {
                    comparisons++;
                }

                selectedArr = new int[] { curr };
                AddHistorySnap();
            }
        }

        private void QuickSort(int startI, int endI)
        {
            comparisons++;
            if (endI - startI < 1)
            {
                return;
            }

            int pI = endI - 1;

            int i = startI;
            int j = startI;

            while (j < endI - 1)
            {
                comparisons++;
                arrAccesses += 2;
                if (arr[j] <= arr[pI])
                {
                    comparisons++;
                    arrAccesses += 4;
                    int oldJ = arr[j];
                    arr[j] = arr[i];
                    arr[i] = oldJ;
                    i++;
                    selectedArr = new int[] { j, i };
                    AddHistorySnap();
                }
                j++;
            }
            comparisons++;

            int oldI = arr[i];
            arr[i] = arr[pI];
            arr[pI] = oldI;
            pI = i;
            arrAccesses += 4;

            selectedArr = new int[] { pI, i };
            AddHistorySnap();

            QuickSort(startI, pI);
            QuickSort(pI + 1, endI);
        }

        private void BubbleSort()
        {
            for (int i = arr.Length; i >= 0; i--)
            {
                for (int j = 0; j < i - 1; j++)
                {
                    if (arr[j] > arr[j + 1])
                    {
                        int oldJ = arr[j];
                        arr[j] = arr[j + 1];
                        arr[j + 1] = oldJ;
                        arrAccesses += 6;

                    }
                    comparisons++;
                }
                selectedArr = new int[] { i };
                AddHistorySnap();

            }
        }

        private void SelectionSort()
        {
            for (int i = 0; i < arr.Length; i++)
            {
                int minI = i;
                for (int j = i + 1; j < arr.Length; j++)
                {
                    if (arr[minI] > arr[j])
                    {
                        minI = j;
                    }
                    arrAccesses += 2;
                    comparisons++;
                }
                int oldI = arr[i];
                arr[i] = arr[minI];
                arr[minI] = oldI;
                arrAccesses += 4;
                selectedArr = new int[] { i };
                AddHistorySnap();

            }
        }

        private void HeapSort()
        {

            for (int i = arr.Length / 2 - 1; i >= 0; i--)
            {
                Heapify(i, arr.Length);
            }

            for (int i = arr.Length - 1; i >= 0; i--)
            {
                int oldI = arr[i];
                arr[i] = arr[0];
                arr[0] = oldI;
                arrAccesses += 4;
                Heapify(0, i);
            }

            selectedArr = new int[] { arr.Length - 1 };

            AddHistorySnap();

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
                    AddHistorySnap();
                    Heapify(maxI, topI);
                }
            }
        }
    }
}
