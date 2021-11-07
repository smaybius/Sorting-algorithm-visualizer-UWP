using algoritmaanalizi;
using System;
using System.Collections.Generic;
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

namespace Algo
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

        private void ShuffleArray()
        {
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
                        Array.Reverse(arr);
                        AddHistorySnap();
                        break;
                    case 3: //sorted
                        Array.Sort(arr);
                        AddHistorySnap();
                        break;
                    case 4: //reverse sorted
                        Array.Sort(arr);
                        Array.Reverse(arr);
                        AddHistorySnap();
                        break;
                    case 5: //max heapified
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
                    default: //none
                        AddHistorySnap();
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
                case 6:
                    OddEvenSort(arr, arr.Length); //first custom sort, also the first to not be hardcoded to the program's system.
                    DrawHistory();
                    break;
                case 7:
                    RadixSort(arr);
                    DrawHistory();
                    break;
                case 8:
                    GrailSort gs = new();
                    gs.GrailSortInPlace(arr, 0, arr.Length - 1);
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

        private void DistribcomboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //SwitchDist();
        }

        public int[] MergeSort(int startI, int endI)
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

        public void OddEvenSort(int[] array, int length)
        {
            bool isSorted = false;

            while (!isSorted)
            {
                isSorted = true;

                //Swap i and i+1 if they are out of order, for i == odd numbers
                for (int i = 1; i <= length - 2; i += 2)
                {
                    if (array[i] > array[i + 1])
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        isSorted = false;
                    }
                    selectedArr = new int[] { i };
                    AddHistorySnap();
                }

                //Swap i and i+1 if they are out of order, for i == even numbers
                for (int i = 0; i <= length - 2; i += 2)
                {
                    if (array[i] > array[i + 1])
                    {
                        int temp = array[i];
                        array[i] = array[i + 1];
                        array[i + 1] = temp;
                        isSorted = false;
                    }
                    selectedArr = new int[] { i };
                    AddHistorySnap();
                }
            }
            return;
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
                    }
                    selectedArr = new int[] { i };
                    AddHistorySnap();
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

        private static void InitializeBuckets(List<Queue<int>> buckets)
        {
            for (int i = 0; i < 10; i++)
            {
                Queue<int> q = new();
                buckets.Add(q);
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
