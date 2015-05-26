using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using OxyPlot.Series;
using OxyPlot;
using MahApps.Metro.Controls;

namespace Dejitaru_signaru
{
    /// <summary>
    /// Interaction logic for CorrelationWindow.xaml
    /// </summary>
    public partial class CorrelationWindow : MetroWindow
    {
        string file1;
        string file2;

        LineSeries corrLineSeries;
        public PlotModel CorPlotModel { get; private set; }
        IList<DataPoint> corrPoints;
        public CorrelationWindow(string file1, string file2)
        {
            InitializeComponent();
            this.file1 = file1;
            this.file2 = file2;
            Loaded += CorrelationWindow_Loaded;
        }

        void CorrelationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string title =  System.IO.Path.GetFileNameWithoutExtension(file1) + " cross correlation with " + System.IO.Path.GetFileNameWithoutExtension(file2);
            this.CorPlotModel = new PlotModel { Title = title };
            this.correlationPlot.Model = CorPlotModel;

            corrPoints = new List<DataPoint>{
                              
                              };
            corrLineSeries = new LineSeries();
            corrLineSeries.ItemsSource = corrPoints;
            this.correlationPlot.Model.Series.Add(corrLineSeries);
            


            new Thread(() =>
             {
                 double[] arr;
                 Correlation.Correlate(file1, file2, out arr, 1000);

                 int length = arr.Length / 4;
                 int max_index = 0;
                 double max_val = 0;
                 for(int i =0; i < arr.Length; i++)
                 {
                     if(arr[i] > max_val)
                     {
                         max_val = arr[i];
                         max_index = i;
                     }
                 }

                 int start = max_index - length;
                 int end = max_index + length;
                 if(start < 0)
                 {
                     end += -start;
                     start = 0;
                 }
                 if (end > arr.Length - 1)
                     end = arr.Length - 1;

                 for(int i = start; i < end; i++)
                 {
                     if (arr[i] < max_val / 2)
                         if (i % 10 == 0)
                             continue;
                     corrPoints.Add(new DataPoint(i, arr[i]));
                 }

                 this.correlationPlot.Dispatcher.Invoke(() =>
                 {
                     correlationPlot.InvalidatePlot(true);
                 });
                 this.lblLoading.Dispatcher.Invoke(() =>
                     {
                         lblLoading.Visibility = System.Windows.Visibility.Hidden;
                     });

             }).Start();
        }
    }
}
