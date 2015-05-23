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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using OxyPlot;
using System.Media;
using OxyPlot.Series;
using System.Windows.Threading;
using NAudio;
using NAudio.Wave;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace Dejitaru_signaru
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        const int SKIP = 10;

        //Image
        WriteableBitmap grayscale;
        int width, height;

        //Audio
        public PlotModel songModel { get; private set; }
        IList<DataPoint> SongPoints;
        public PlotModel fftModelMag { get; private set; }
        IList<DataPoint> FFTPointsMag;
        public PlotModel fftModelPhase { get; private set; }
        IList<DataPoint> FFTPointsPhase;
        

        LineSeries SongLSeries;
        LineSeries FFTMagLSeries;
        LineSeries FFTPhaseLSeries;

        MediaPlayer player;
        DispatcherTimer songNotify;
        WaveFormat sfInfo;
        OxyPlot.Axes.LinearAxis x_axes;
        OxyPlot.Axes.LinearAxis y_axes;

        float[] mono;
        
        public MainWindow()
        {
            InitializeComponent();

            slider_thresh.Value = 65;
            this.songModel = new PlotModel { Title = "Song" };
            this.fftModelMag = new PlotModel { Title = "FFT Magnitude" };
            this.fftModelPhase = new PlotModel { Title = "FFT Phase" };
           // this.songModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "Ellie(x)"));
            this.plot.Model = songModel;
            this.plotfftmag.Model = fftModelMag;
            this.plotfftphase.Model = fftModelPhase;
  
            SongPoints = new List<DataPoint>{
                                  new DataPoint(0, 0)                                  
                              };
            FFTPointsMag = new List<DataPoint>{
                                  new DataPoint(0, 0)                                  
                              };
            FFTPointsPhase = new List<DataPoint>{
                                  new DataPoint(0, 0)                                  
                              };

            SongLSeries = new LineSeries();
            SongLSeries.ItemsSource = SongPoints;
            SongLSeries.Color = OxyColors.CadetBlue;

            FFTMagLSeries = new LineSeries();
            FFTMagLSeries.ItemsSource = FFTPointsMag;
            FFTMagLSeries.Color = OxyColors.Red;


            FFTPhaseLSeries = new LineSeries();
            FFTPhaseLSeries.ItemsSource = FFTPointsPhase;
            FFTPhaseLSeries.Color = OxyColors.Red;

            this.plot.Model.Series.Add(SongLSeries);
            this.plotfftmag.Model.Series.Add(FFTMagLSeries);
            this.plotfftphase.Model.Series.Add(FFTPhaseLSeries);

            songNotify = new DispatcherTimer();
            songNotify.Interval = new TimeSpan(0, 0, 0, 0, 100);
            songNotify.Tick += songNotify_Tick;

            x_axes = new OxyPlot.Axes.LinearAxis()
            {
                Key = "XAxis",
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                AbsoluteMaximum = 1800,
                AbsoluteMinimum = -1,
                MinorStep = 5,
            };
            y_axes = new OxyPlot.Axes.LinearAxis()
            {
                Key = "YAxis",
                Position = OxyPlot.Axes.AxisPosition.Left,
                AbsoluteMaximum = 2,
                AbsoluteMinimum = -2,
                MinorStep = 0.1,
            };
            songModel.Axes.Add(x_axes);
            songModel.Axes.Add(y_axes);
        
            
        }

        void songNotify_Tick(object sender, EventArgs e)
        {
            if (!player.NaturalDuration.HasTimeSpan)
                return;
            
            float perc = (float)(player.Position.TotalSeconds / player.NaturalDuration.TimeSpan.TotalSeconds);
            slider_songSeek.Value = perc * 100;

            int frame_index = (int)(mono.Length * perc);
            if (frame_index >= mono.Length - 1)
                frame_index = mono.Length - 1;
            float frame = mono[frame_index];
            float time = (float)player.Position.TotalSeconds;
            SongPoints.Add(new DataPoint(time, frame));

            songModel.Axes[0].Minimum = time - 8;

            songModel.Axes[0].Maximum = time + 2;

            //songModel.Axes[0].IsPanEnabled = false;

            double panStep = x_axes.Transform(-0.1 + x_axes.Offset);
            if(time > 5)
                x_axes.Pan(panStep); 

    
            plot.InvalidatePlot(true);

            
        }

        Color ConvertToGrayScale(Color rgb)
        {
            byte gray = (byte)(0.21 * rgb.R + 0.72 * rgb.G + 0.07 * rgb.B);
            var gray_pixel = new Color();
            gray_pixel.R = gray; gray_pixel.B = gray; gray_pixel.G = gray; gray_pixel.A = 255;
            return gray_pixel;
        }

        WriteableBitmap LoadGrayScaleBitmap(string file_name)
        {
            WriteableBitmap image;
            //Convert to grayscale
            var color_img = new BitmapImage(new Uri(file_name));
            image = new WriteableBitmap(color_img);
            width = (int)color_img.PixelWidth;
            height = (int)color_img.PixelHeight;
            unsafe
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        //var pixel = image.GetPixel(x, y);

                        Color c = new Color();
                        IntPtr pBackBuffer = image.BackBuffer;


                        byte* pBuff = (byte*)pBackBuffer.ToPointer();

                        //BGRA
                        c.B = pBuff[4 * x + (y * image.BackBufferStride)];
                        c.G = pBuff[4 * x + (y * image.BackBufferStride) + 1];
                        c.R = pBuff[4 * x + (y * image.BackBufferStride) + 2];
                        c.A = pBuff[4 * x + (y * image.BackBufferStride) + 3];

                        Color gray_comp = ConvertToGrayScale(c);

                        pBuff[4 * x + (y * image.BackBufferStride)] = c.B;
                        pBuff[4 * x + (y * image.BackBufferStride) + 1] = c.G;
                        pBuff[4 * x + (y * image.BackBufferStride) + 2] = c.R;
                        pBuff[4 * x + (y * image.BackBufferStride) + 3] = c.A;
                    }

                }
            }
            return image;
        }

        void SetBGRAPixel(WriteableBitmap bitmap, int x, int y, Color c)
        {
            IntPtr pBackBuffer = bitmap.BackBuffer;

            unsafe
            {
                byte* pBuff = (byte*)pBackBuffer.ToPointer();

                //BGRA
                pBuff[4 * x + (y * bitmap.BackBufferStride)] = c.B;
                pBuff[4 * x + (y * bitmap.BackBufferStride) + 1] = c.G;
                pBuff[4 * x + (y * bitmap.BackBufferStride) + 2] = c.R;
                pBuff[4 * x + (y * bitmap.BackBufferStride) + 3] = c.A;
            }
        }
        Color GetBGRAPixel(WriteableBitmap bitmap, int x, int y)
        {
            Color c = new Color();
            
            IntPtr pBackBuffer = bitmap.BackBuffer;

            unsafe
            {
                byte* pBuff = (byte*)pBackBuffer.ToPointer();

                //BGRA
                c.B = pBuff[4 * x + (y * bitmap.BackBufferStride)];
                c.G = pBuff[4 * x + (y * bitmap.BackBufferStride) + 1];
                c.R = pBuff[4 * x + (y * bitmap.BackBufferStride) + 2];
                c.A = pBuff[4 * x + (y * bitmap.BackBufferStride) + 3];
            }
            return c;
        }

        private void CalculateImgFFT(float perc)
        {

            int size = width * height;

            alglib.complex[] input = new alglib.complex[size];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    
                    input[x * height + y] = new alglib.complex(GetBGRAPixel(grayscale, x, y).R, 0);
                }
            }

            alglib.fftc1d(ref input);


            double max_mag = 0;
            double max_phase = 0;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int i = x * height + y;
                    double mag = Math.Sqrt(input[i].x * input[i].x + input[i].y * input[i].y);
                    double phase = Math.Abs(Math.Atan2(input[i].y, input[i].x));
                    if (mag > max_mag)
                        max_mag = mag;
                    if (phase > max_phase)
                        max_phase = phase;
                }
            }

            double c_mag = 255 / Math.Log10(1 + max_mag);
            double c_phase = 255 / Math.Log10(1 + max_phase);


            //Plot FFT
            WriteableBitmap fourier_mag = new WriteableBitmap(width + 1, height + 1, grayscale.DpiX, grayscale.DpiY, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            WriteableBitmap fourier_phase = new WriteableBitmap(width + 1, height + 1, grayscale.DpiX, grayscale.DpiY, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            fourier_mag.Clear();
            fourier_phase.Clear();
            unsafe
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        int i = x * height + y;
                        float r = perc * ((width > height) ? width : height);
                        if ((x - width / 2) * (x - width / 2) + (y - height / 2) * (y - height / 2) > r * r)
                        {
                            input[i].x = 0;
                            input[i].y = 0;
                        }


                        double mag = Math.Sqrt(input[i].x * input[i].x + input[i].y * input[i].y);
                        double phase = Math.Atan2(input[i].y, input[i].x);

                        byte dyn_pix_mag = (byte)(c_mag * Math.Log10(1 + mag));
                        byte dyn_pix_phase = (byte)(c_phase * Math.Log10(1 + phase));


                        IntPtr pBackBuffer = fourier_mag.BackBuffer;

                        byte* pBuff = (byte*)pBackBuffer.ToPointer();

                        //BGRA
                        pBuff[4 * x + (y * fourier_mag.BackBufferStride)] = dyn_pix_mag;
                        pBuff[4 * x + (y * fourier_mag.BackBufferStride) + 1] = dyn_pix_mag;
                        pBuff[4 * x + (y * fourier_mag.BackBufferStride) + 2] = dyn_pix_mag;
                        pBuff[4 * x + (y * fourier_mag.BackBufferStride) + 3] = 255;

                        pBackBuffer = fourier_phase.BackBuffer;

                        pBuff = (byte*)pBackBuffer.ToPointer();

                        //BGRA
                        pBuff[4 * x + (y * fourier_phase.BackBufferStride)] = dyn_pix_phase;
                        pBuff[4 * x + (y * fourier_phase.BackBufferStride) + 1] = dyn_pix_phase;
                        pBuff[4 * x + (y * fourier_phase.BackBufferStride) + 2] = dyn_pix_phase;
                        pBuff[4 * x + (y * fourier_phase.BackBufferStride) + 3] = 255;
                    }


                }
            }


            alglib.fftc1dinv(ref input);


            //PLOT img
            WriteableBitmap filtered = new WriteableBitmap(width + 1, height + 1, grayscale.DpiX, grayscale.DpiY, PixelFormats.Bgra32, BitmapPalettes.Halftone256Transparent);
            filtered.Clear();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int i = x * height + y;
                    float r = perc * ((width > height) ? width : height);

                    unsafe
                    {
                        IntPtr pBackBuffer = filtered.BackBuffer;

                        byte* pBuff = (byte*)pBackBuffer.ToPointer();

                        byte val = (byte)Math.Abs(input[i].x);

                        //BGRA
                        pBuff[4 * x + (y * filtered.BackBufferStride)] = val;
                        pBuff[4 * x + (y * filtered.BackBufferStride) + 1] = val;
                        pBuff[4 * x + (y * filtered.BackBufferStride) + 2] = val;
                        pBuff[4 * x + (y * filtered.BackBufferStride) + 3] = 255;
                    }

                }
            }

            img_filtered.Dispatcher.Invoke(() =>
               {
                    img_filtered.Source = filtered;
                });
            img_fourier_mag.Dispatcher.Invoke(() =>
              {
                  img_fourier_mag.Source = fourier_mag;
              });
            img_fourier_phase.Dispatcher.Invoke(() =>
              {
                  img_fourier_phase.Source = fourier_phase;
              });
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            status_label.Content = "Opening file";
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
              "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
              "Portable Network Graphic (*.png)|*.png";

           
            if (op.ShowDialog() == true)
            {
                if (op.FileName == null || op.FileName == "")
                    return;
                status_label.Content = "Loading file";
                grayscale = LoadGrayScaleBitmap(op.FileName);
                img_grayscale.Source = grayscale;
                img_label.Content = System.IO.Path.GetFileName(op.FileName);

                Slider_ValueChanged(null, null);
                status_label.Content = "Done";

                
            }
           
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                float slider_val = (float)slider_thresh.Value / 100.0f;
                status_label.Content = "Calculating FFT";
                CalculateImgFFT((float)slider_thresh.Value / 100);
                status_label.Content = "Done";
            }
            catch
            {

            }
        }

        
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            
           // (new Thread(() => CalculateImgFFT(slider_val))).Start();

            //CalculateImgFFT((float)slider_thresh.Value/100);
        
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            status_label.Content = "Opening audio file";
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a audio file";
            op.Filter = "WAV Audio Files|*.wav";

            if (op.ShowDialog() == true)
            {
                if (op.FileName == null || op.FileName == "")
                    return;
                FFTPointsMag.Clear();
                FFTPointsPhase.Clear();
                SongPoints.Clear();
                listBoxStatus.Items.Clear();

                status_label.Content = "Loading file";
                player = new MediaPlayer();
                player.Open(new Uri(op.FileName));
                player.Play();

                label_song.Content = "Playing " + System.IO.Path.GetFileName(op.FileName);
                
                songNotify.Start();

                ReadWaveFile(op.FileName);

                status_label.Content = "Done";

                plotFFT();

                ShowSimilarities();
            }
           
        }

        List<Tuple<string, double, int>> check_similarity(string[] audio_files)
        {
            float time = mono.Length / sfInfo.SampleRate;
            int splits = (int)(time/60)/4; //splits of 4 minute intervals
            if (splits < 1)
                splits = 1;
            splits = 3;
            int width = mono.Length / splits;
            List<Tuple<string, double, int>> songs = new List<Tuple<string, double, int>>();
            //System.Threading.Tasks.Parallel.For(0, audio_files.Length, j =>
            for (int j = 0; j < audio_files.Length; j++)
            {
                double[] signal2;
                WaveFormat waveInfo;
                //ReadWaveFile(audio_files[j], out signal2, out waveInfo);
                ReadWaveFile(audio_files[j], out signal2, out waveInfo, SKIP);

                double max_sim = 0;
                int max_index = 0;
                double[] corr;
                alglib.corrr1d(signal2, signal2.Length / (SKIP*SKIP), signal2, signal2.Length/(SKIP*SKIP), out corr);
                double norm = corr[0];
                for (int i = 0; i < splits; i++)
                {
                    double[] signal1 = new double[(int)Math.Ceiling((float)width/SKIP)];

                    for (int k = 0; k < width; k+=SKIP)
                    {
                        signal1[k/SKIP] = mono[i * width + k];
                    }

                    //double[] corr;
                   
                    alglib.corrr1dcircular(signal1, signal1.Length, signal2, signal2.Length, out corr);
                    
                    //alglib.corrr1d(signal1, width / SKIP, signal2, signal2.Length / SKIP, out corr);
                    for (int k = 0; k < corr.Length; k++)
                    {
                        if (Math.Abs(corr[k]) > max_sim)
                        {
                            max_sim = Math.Abs(corr[k]);
                            max_index = k*SKIP + i * width;
                        }
                    }

                }
                
               // max_sim /= ((width) / waveInfo.SampleRate);
               // max_sim /= signal2.Length / waveInfo.SampleRate;
               // max_sim *= time;
               // max_sim = max_sim * max_sim;
                float sim = (float)(max_sim / norm * 1000);

                string song_name = System.IO.Path.GetFileNameWithoutExtension(audio_files[j]);
                songs.Add(new Tuple<string, double, int>(song_name, sim, max_index));
                status_label.Dispatcher.Invoke(() => { status_label.Content = song_name + " - " + sim; });
            }
            //);
            return songs;
        }

        void ShowSimilarities()
        {
            (new Thread(() =>
            {
                while (FFTPointsMag.Count < 10)
                    Thread.Sleep(100);
                status_label.Dispatcher.Invoke(() => { status_label.Content = "Checking similarities"; });
                var songs = check_similarity(new string[] { "D:\\sein\\burn.wav", "D:\\sein\\pokemon.wav", "D:\\sein\\nexttome.wav", "D:\\sein\\talkingbody.wav" });
                double max_size = 0;
                for (int i = 0; i < songs.Count;i++ )
                {
                    if (songs[i].Item2 > max_size)
                        max_size = songs[i].Item2;
                }
                    for (int i = 0; i < songs.Count; i++)
                    {
                        listBoxStatus.Dispatcher.Invoke(() =>
                            {
                                var item = new ListBoxItem();
                                float time = mono.Length / sfInfo.SampleRate;


                                Label label1 = new Label();
                                label1.Width = 100;
                                label1.Content = songs[i].Item1;
                                Label label2 = new Label();
                                label2.Width = 50;
                                label2.Content = (int)(songs[i].Item3 / (float)mono.Length * time);
                                Label label3 = new Label();
                                label3.Width = 75;
                                label3.Content = (int)songs[i].Item2;
                                ProgressBar bar = new ProgressBar();
                                StackPanel panel = new StackPanel();
                                bar.Minimum = 0;
                                bar.Maximum = max_size;
                                bar.Value = songs[i].Item2;
                                bar.Width = 100;
                                panel.Orientation = Orientation.Horizontal;
                                panel.Children.Add(label1);
                                panel.Children.Add(label2);
                                panel.Children.Add(label3);
                                panel.Children.Add(bar);

                                if (songs[i].Item2 > 800)
                                {
                                    OxyPlot.Annotations.LineAnnotation annotation = new OxyPlot.Annotations.LineAnnotation();
                                    annotation.Text = songs[i].Item1;
                                    annotation.Type = OxyPlot.Annotations.LineAnnotationType.Vertical;
                                    annotation.X = (songs[i].Item3 / (float)mono.Length * time);
                                   
                                    songModel.Annotations.Add(annotation);
                                }

                                //item.Content = songs[i].Item1 + " - " + songs[i].Item2 + " - " + (songs[i].Item3 / (float)mono.Length * time);

                                item.Content = panel;
                                
                                listBoxStatus.Items.Add(item);
                            });
                    }
                status_label.Dispatcher.Invoke(() => { status_label.Content = "Done checking similarities"; });
            })).Start();
        }

        void plotFFT()
        {
            status_label.Content = "Loading FFT...";
            (new Thread(() =>
            {
                int size = width * height;

                int skip = 10 * SKIP/2;
                alglib.complex[] input = new alglib.complex[mono.Length / skip + 1];

                for (int i = 0; i < mono.Length; i += skip)
                {
                    input[i / skip] = new alglib.complex(mono[i], 0);
                }

                alglib.fftc1d(ref input);

                skip = (int)((mono.Length / 15114240.0f) * 10)/skip;
                if (skip < 100)
                    skip = 100;
                for (int i = 0; i < input.Length / 2; i += skip)
                {
                    double mag = Math.Sqrt(input[i].x * input[i].x + input[i].y * input[i].y);
                    double phase = Math.Atan2(input[i].y, input[i].x);

                    FFTPointsMag.Add(new DataPoint(i, mag));
                    if(i < mono.Length / 4)
                        FFTPointsPhase.Add(new DataPoint(i, phase));
                }

                plotfftmag.Dispatcher.Invoke(() =>
                {
                    plotfftmag.InvalidatePlot(true);
                });
                plotfftphase.Dispatcher.Invoke(() =>
                {
                    plotfftphase.InvalidatePlot(true);
                });
                status_label.Dispatcher.Invoke(() =>
                    {
                        status_label.Content = "Done";
                    });

            })).Start();            
        }

        void ReadWaveFile(string file_name)
        {
            NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(file_name);

            long samplesDesired = reader.SampleCount;
            byte[] buffer = new byte[samplesDesired * 4];
            short[] left = new short[samplesDesired];
            short[] right = new short[samplesDesired];
            int bytesRead = reader.Read(buffer, 0, (int)reader.Length);
            int index = 0;
            for (int sample = 0; sample < bytesRead / 4; sample++)
            {
                left[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
                right[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
            }
            mono = new float[left.Length];
            float max = 0;
            for(int i =0; i < left.Length; i++)
            {
                if (left[i] > max)
                    max = left[i];
            }
            for(int i =0; i < left.Length; i++)
            {
                mono[i] = left[i]/max;
            }
            sfInfo = reader.WaveFormat;
            reader.Close();
        }

        void ReadWaveFile(string file_name, out double[] arr, out WaveFormat waveInfo, int courseness)
        {
            NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(file_name);

            long samplesDesired = reader.SampleCount;
            byte[] buffer = new byte[samplesDesired * 4];
            short[] left = new short[samplesDesired];
            short[] right = new short[samplesDesired];
            int bytesRead = reader.Read(buffer, 0, (int)reader.Length);
            int index = 0;
            for (int sample = 0; sample < bytesRead / 4; sample++)
            {
                left[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
                right[sample] = BitConverter.ToInt16(buffer, index);
                index += 2;
            }
            float max = 0;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] > max)
                    max = left[i];
            }
            arr = new double[(int) Math.Ceiling((float)left.Length/courseness)];
            for (int i = 0; i < left.Length; i+= courseness)
            {
                arr[i/courseness] = left[i] / max;
            }
            reader.Close();
            waveInfo = reader.WaveFormat;
        }

        private void Slider_DragCompleted_1(object sender, DragCompletedEventArgs e)
        {
            Slider s = sender as Slider;
            // Your code
            MessageBox.Show(s.Value.ToString());
        }
        
    }
}
