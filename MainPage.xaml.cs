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
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using static AlgoUWP.Shuffles;

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

    public sealed partial class MainPage : Page
    {


        //timer that we'll use when drawing the array
        private readonly Comparer cmp = new();
        public bool busy;

        //Audio variables
        private AudioGraph graph;
        private AudioDeviceOutputNode deviceOutputNode;
        private AudioFrameInputNode frameInputNode;
        public double theta = 0;
        public Arr arr; // the array


        public MainPage()
        {
            Debug.WriteLine("Started");
            InitializeComponent();

        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            Sort.isPaused = false;
            Binding readvals = new();
            readvals.Source = Sort.reads;
            ReadsText.SetBinding(TextBlock.TextProperty, readvals);
            Binding compvals = new();
            compvals.Source = Sort.comparisons;
            CompsText.SetBinding(TextBlock.TextProperty, compvals);
            Binding mainwritevals = new();
            mainwritevals.Source = Sort.mainwrites;
            MainWritesText.SetBinding(TextBlock.TextProperty, mainwritevals);
            Binding auxwritevals = new();
            auxwritevals.Source = Sort.auxwrites;
            AuxWritesText.SetBinding(TextBlock.TextProperty, auxwritevals);
            Binding swapvals = new();
            swapvals.Source = Sort.swaps;
            SwapsText.SetBinding(TextBlock.TextProperty, swapvals);
            Binding reversalvals = new();
            reversalvals.Source = Sort.reversals;
            ReversalsText.SetBinding(TextBlock.TextProperty, reversalvals);
            var _shuffleval = Enum.GetValues(typeof(Shuffles.ShuffleChoices)).Cast<ShuffleChoices>();
            shufflecomboBox.ItemsSource = _shuffleval.ToList();
            var _distribval = Enum.GetValues(typeof(Shuffles.ShuffleChoices)).Cast<ShuffleChoices>();
            shufflecomboBox.ItemsSource = _distribval.ToList();
            ResetDistribution();
            await CreateAudioGraph();
        }

        private void ShuffleBtn_Click(object sender, RoutedEventArgs e)
        {
            Shuffle();
        }

        private void ResetDistribution()
        {
            arr = new((int)ArraySize.Value, canv, speedSlider); //initialize the array
            //initialize everything
            Sort.isPaused = false;
            Sort.reads = 0;
            Sort.comparisons = 0;
            Sort.swaps = 0;
            Sort.mainwrites = 0;
            Sort.auxwrites = 0;
            Sort.reversals = 0;
            Sort.isPremade = true;
            busy = true;
            Distributions.Linear(arr);
            busy = false;
        }

        private void Shuffle()
        {
            //initialize everything
            Sort.isPaused = false;
            Sort.reads = 0;
            Sort.comparisons = 0;
            Sort.swaps = 0;
            Sort.mainwrites = 0;
            Sort.auxwrites = 0;
            Sort.reversals = 0;
            Sort.isPremade = false;
            busy = true;
            busy = false;

        }

        

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem b = sender as MenuFlyoutItem;
            comboBox.Content = b.Text;
        }

        

        private void ArraySize_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (ArraySize.Value >= 2)
            {
                arr = new((int)ArraySize.Value, canv, speedSlider);
                ResetDistribution();
            }
            
        }

        private void Visualize_Click(object sender, RoutedEventArgs e)
        {
            //initialize everything
            Sort.isPaused = false;
            Sort.reads = 0;
            Sort.comparisons = 0;
            Sort.swaps = 0;
            Sort.mainwrites = 0;
            Sort.auxwrites = 0;
            Sort.reversals = 0;
            busy = true;


            switch (comboBox.Content)
            {
                case "Weird insertion sort":
                    InsertionWhatSort.InsertionWhat(arr, cmp);
                    break;
                default:
                    MessageDialog dialog = new("You need to select an algorithm.", "Alert");
                    //dialog.ShowAsync();
                    break;
            }
            busy = false;

        }

        private bool isLeftButtonDown;
        private void Canv_MouseLeftButtonDown(object sender, PointerRoutedEventArgs e)
        {
            isLeftButtonDown = true;
        }

        private void Canv_MouseMove(object sender, PointerRoutedEventArgs e)
        {
            if (isLeftButtonDown && Sort.isPaused && e.GetCurrentPoint(canv).Position.X > 0 && e.GetCurrentPoint(canv).Position.X < canv.ActualWidth && e.GetCurrentPoint(canv).Position.Y > 0 && e.GetCurrentPoint(canv).Position.Y < canv.ActualHeight)
            {
                Sort.isPremade = true;
                PointerPoint a = e.GetCurrentPoint(canv);
                arr[(int)Math.Ceiling(a.Position.X / (canv.ActualWidth / arr.Length)) - 1] = arr.Length - (int)Math.Ceiling(a.Position.Y / (canv.ActualHeight / arr.Length));
                arr.DrawNumbers();
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
            if (Sort.isPremade)
            {
                MessageDialog dialog = new("To visualize a premade array click \"Visualize\"", "Alert");
                //dialog.ShowAsync();
                return;
            }
            if (Sort.isPaused)
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
            pauseButton.Content = "Play";
            Sort.isPaused = true;
        }

        private void PlayPreview()
        {
            pauseButton.Content = "Pause";
            Sort.isPaused = false;
        }

        private void ResetPauseButtonText()
        {
            pauseButton.Content = "Pause";
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (arr != null)
            {
                arr.DrawNumbers();
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
