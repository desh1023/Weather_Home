using System;
using System.Windows.Forms;
using FireSharp.Config;
using FireSharp.Response;
using FireSharp.Interfaces;
using System.Threading.Tasks;
using FireSharp;
using LiveCharts;
using LiveCharts.WinForms; // Required for SolidGauge and cartesianChart
using LiveCharts.Wpf;
using System.Collections.Generic;

namespace Weather_Home
{
    public partial class Form1 : Form
    {
        private List<double> airQualityData = new List<double>(); // Store air quality data

        public Form1()
        {
            InitializeComponent();

            // Initialize SolidGauge configuration
            solidGauge1.From = 0;
            solidGauge1.To = 500; // Assuming AQI scale ranges from 0 to 500
            solidGauge1.Value = 0; // Initial value
            solidGauge1.LabelFormatter = value => value.ToString("N0"); // Format value as integer

            // Initialize cartesianChart1
            cartesianChart1.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Air Quality",
                    Values = new ChartValues<double>(),
                    PointGeometry = DefaultGeometries.Circle,
                    PointGeometrySize = 5
                }
            };

            cartesianChart1.AxisX.Add(new Axis
            {
                Title = "Time",
                Labels = new List<string>()
            });

            cartesianChart1.AxisY.Add(new Axis
            {
                Title = "AQI",
                MinValue = 0,
                MaxValue = 500
            });
        }

        IFirebaseConfig ifc = new FirebaseConfig()
        {
            AuthSecret = "PWHdu5Mh717494IHVbVKgMAHwwjPR6oNNxLE9221",
            BasePath = "https://daqsys-1c40d-default-rtdb.firebaseio.com/"
        };

        IFirebaseClient client;
        Timer timer;
        bool isErrorShown = false; // Flag to track if the error message has been shown

        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                client = new FirebaseClient(ifc);

                if (client != null)
                {
                    SetupTimer(); // Set up the timer for periodic updates
                    UpdateGreeting(); // Update the greeting when the form loads
                }
                else
                {
                    MessageBox.Show("Error: Could not initialize Firebase client.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was a problem in your connection: " + ex.Message);
            }
        }

        private void SetupTimer()
        {
            // Set up a timer to poll Firebase every 5 seconds and update labels and greeting
            timer = new Timer();
            timer.Interval = 5000; // 5000 milliseconds = 5 seconds
            timer.Tick += async (s, args) =>
            {
                await UpdateLabelsAsync();
                UpdateGreeting(); // Update the greeting periodically
            };
            timer.Start();
        }

        private async Task UpdateLabelsAsync()
        {
            try
            {
                // Retrieve the temperature value from Firebase
                FirebaseResponse tempResponse = await client.GetAsync("DHT11/Temperature");
                string tempValue = tempResponse.ResultAs<string>();

                if (tempValue != null)
                {
                    // Update label2 text with the temperature value in the UI thread
                    this.Invoke((MethodInvoker)delegate
                    {
                        label2.Text = tempValue;
                    });
                }

                // Retrieve the humidity value from Firebase
                FirebaseResponse humidityResponse = await client.GetAsync("DHT11/Humidity");
                string humidityValue = humidityResponse.ResultAs<string>();

                if (humidityValue != null)
                {
                    // Update label10 text with the humidity value in the UI thread
                    this.Invoke((MethodInvoker)delegate
                    {
                        label10.Text = humidityValue;
                    });
                }

                // Retrieve the decibel value from Firebase
                FirebaseResponse decibelResponse = await client.GetAsync("Sound/Decibel");
                string decibelValue = decibelResponse.ResultAs<string>();

                if (decibelValue != null)
                {
                    // Update label12 text with the decibel value in the UI thread
                    this.Invoke((MethodInvoker)delegate
                    {
                        label12.Text = decibelValue;
                    });
                }

                // Retrieve the air quality value from Firebase
                FirebaseResponse airResponse = await client.GetAsync("MQ135/Air");
                string airValue = airResponse.ResultAs<string>();

                if (airValue != null)
                {
                    double airQualityIndex;
                    if (double.TryParse(airValue, out airQualityIndex))
                    {
                        // Update SolidGauge and label11 text in the UI thread
                        this.Invoke((MethodInvoker)delegate
                        {
                            solidGauge1.Value = airQualityIndex; // Update the gauge value
                            label11.Text = airValue; // Update the air quality label

                            // Update the cartesian chart
                            UpdateAirQualityChart(airQualityIndex);
                        });
                    }
                }

                // Reset the error flag if data retrieval is successful
                isErrorShown = false;
            }
            catch (Exception ex)
            {
                // Show the error message only once
                if (!isErrorShown)
                {
                    MessageBox.Show("Error retrieving data: " + ex.Message);
                    isErrorShown = true; // Set the flag to true so the message doesn't repeat
                }
            }
        }

        private void UpdateAirQualityChart(double airQualityIndex)
        {
            // Add new data to the list and remove the oldest if it exceeds a limit (e.g., 10 points)
            airQualityData.Add(airQualityIndex);
            if (airQualityData.Count > 10)
            {
                airQualityData.RemoveAt(0);
            }

            // Update the cartesian chart
            var lineSeries = cartesianChart1.Series[0] as LineSeries;
            if (lineSeries != null)
            {
                lineSeries.Values.Clear();
                foreach (var value in airQualityData)
                {
                    lineSeries.Values.Add(value);
                }
            }

            // Update X-axis labels with time
            var axisX = cartesianChart1.AxisX[0];
            axisX.Labels.Clear();
            for (int i = 0; i < airQualityData.Count; i++)
            {
                axisX.Labels.Add(DateTime.Now.AddSeconds(-5 * (airQualityData.Count - 1 - i)).ToString("HH:mm:ss"));
            }
        }

        private void UpdateGreeting()
        {
            DateTime now = DateTime.Now;
            string greeting;

            if (now.Hour >= 5 && now.Hour < 12)
            {
                greeting = "Morning!";
            }
            else if (now.Hour >= 12 && now.Hour < 17)
            {
                greeting = "Afternoon!";
            }
            else if (now.Hour >= 17 && now.Hour < 21)
            {
                greeting = "Evening!";
            }
            else
            {
                greeting = "Night!";
            }

            // Update label4 with the greeting message in the UI thread
            this.Invoke((MethodInvoker)delegate
            {
                label4.Text = greeting;
            });
        }

        // Renamed Data class to ButtonData
        public class ButtonData
        {
            public int State { get; set; }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            // This event is intentionally left empty
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
            {
                // Use ButtonData class instead of Data
                var data = new ButtonData { State = 0 };
                SetResponse response = await client.SetAsync("ButtonState/", data);
                MessageBox.Show("Sent 0 to Firebase as an integer.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            // The click event is no longer needed for updating values; updates happen through the timer
        }

        private void label19_Click(object sender, EventArgs e)
        {
            // This event handler doesn't seem necessary in the current context
        }

        private async void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Use ButtonData class instead of Data
                var data = new ButtonData { State = 1 };
                SetResponse response = await client.SetAsync("ButtonState/", data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }

        private async void button2_Click_1(object sender, EventArgs e)
        {
            try
            {
                // Use ButtonData class instead of Data
                var data = new ButtonData { State = 0 };
                SetResponse response = await client.SetAsync("ButtonState/", data);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }
    }
}
