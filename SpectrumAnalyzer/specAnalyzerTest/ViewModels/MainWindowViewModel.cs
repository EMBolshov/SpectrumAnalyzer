using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Wpf;
using specAnalyzerTest.Models;

namespace specAnalyzerTest.ViewModels
{
    class MainWindowViewModel : BaseViewModel
    {
        public ICommand OpenPicCommand { get; set; }
        public ICommand AnalyzeCommand { get; set; }
        public ICommand AnalyzeSampleCommand { get; set; }
        public string PicPath { get; set; }
        public List<DataPoint> PlotPoints { get; set; }
        private ObservableCollection<AnalyzedSpectrum> _peaks;
        private ObservableCollection<AnalyzedSpectrum> _result;
        public ObservableCollection<AnalyzedSpectrum> Peaks { get { return _peaks; } set { _peaks = value; NotifyPropertyChanged(nameof(Peaks)); } }
        public ObservableCollection<AnalyzedSpectrum> Result { get { return _result; } set { _result = value; NotifyPropertyChanged(nameof(Result)); } }
        public ObservableCollection<AnalyzedSpectrum> SamplePeaks { get; set; }
        public ObservableCollection<AnalyzedSpectrum> ResultSpectrum { get; set; }
        public bool firstAnalyze = true;
        public MainWindowViewModel()
        {
            OpenPicCommand = new BaseCommand(OpenPic, true);
            AnalyzeCommand = new BaseCommand(AnalyzeSpectrum, true);
            AnalyzeSampleCommand = new BaseCommand(AnalyzeSample, true);
            Peaks = new ObservableCollection<AnalyzedSpectrum>();
            SamplePeaks = new ObservableCollection<AnalyzedSpectrum>();
            ResultSpectrum = new ObservableCollection<AnalyzedSpectrum>();
            Result = new ObservableCollection<AnalyzedSpectrum>();
            PlotPoints = new List<DataPoint>();
            PicPath = "null";
        }

        public void OpenPic()
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Title = "Select a picture";
            op.Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                        "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                        "Portable Network Graphic (*.png)|*.png";
            if (op.ShowDialog() == true)
            {
                PicPath = op.FileName;
                NotifyPropertyChanged(nameof(PicPath));
            }
        }

        public void AnalyzeSpectrum()
        {
            try
            {
                Peaks.Clear();
                PlotPoints.Clear();
                if (firstAnalyze)
                {
                    SamplePeaks.Clear();
                }

                var uri = new Uri(PicPath);
                var bitmap = new BitmapImage(uri);
                int stride = bitmap.PixelWidth * 4;
                int size = bitmap.PixelHeight * stride;
                byte[] pixels = new byte[size];
                bitmap.CopyPixels(pixels, stride, 0);
                //make an average values
                //1. get sum of each color components
                int[] avgpixels = new int[stride];
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        int index = y * stride + 4 * x;
                        avgpixels[4 * x] += pixels[index];
                        avgpixels[4 * x + 1] += pixels[index + 1];
                        avgpixels[4 * x + 2] += pixels[index + 2];
                    }
                }

                //2. get an average values - smooth
                for (int x = 0; x < bitmap.PixelWidth; x++)
                {
                    int index = bitmap.PixelHeight * stride + 4 * x;
                    avgpixels[4 * x] /= bitmap.PixelHeight;
                    avgpixels[4 * x + 1] /= bitmap.PixelHeight;
                    avgpixels[4 * x + 2] /= bitmap.PixelHeight;
                }

                //get intensity and make points on plot
                for (int x = 0; x < bitmap.PixelWidth; x++)
                {
                    var spec = new AnalyzedSpectrum();
                    int index = 4 * x;
                    int red = avgpixels[index];
                    int green = avgpixels[index + 1];
                    int blue = avgpixels[index + 2];
                    spec.PeakPixel = x + 380; //scale to wavelenght
                    spec.PeakIntensity = Math.Round(Math.Sqrt(0.299 * red * red + 0.587 * green * green + 0.114 * blue * blue),2); // hsp color model
                    Peaks.Add(spec);
                    if (firstAnalyze)
                    {
                        SamplePeaks.Add(spec);
                    }

                    var _point = new DataPoint(spec.PeakPixel, spec.PeakIntensity);
                    PlotPoints.Add(_point);
                }

                firstAnalyze = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("You must make a photo of spectrum first!");
            }
        }
        public void AnalyzeSample()
        {
            try
            {
                ResultSpectrum.Clear();
                PlotPoints.Clear();
                foreach (var peaks in Peaks.Zip(SamplePeaks, Tuple.Create))
                {
                    var newspec = new AnalyzedSpectrum();
                    newspec.PeakIntensity = Math.Abs(peaks.Item2.PeakIntensity - peaks.Item1.PeakIntensity);
                    newspec.PeakPixel = peaks.Item1.PeakPixel;
                    ResultSpectrum.Add(newspec);
                    var _point = new DataPoint(newspec.PeakPixel, newspec.PeakIntensity);
                    PlotPoints.Add(_point);
                }

                firstAnalyze = true; //reset flag allows to make next measure

                //select and add extrema to result list
                double x0 = 0;
                double y0 = 0;
                foreach (var resspec in ResultSpectrum)
                {
                    //1. find maximum 
                    if (resspec.PeakIntensity > x0)
                    {
                        x0 = resspec.PeakIntensity;
                        y0 = x0;
                    }
                    else
                    {
                        //2. find minimum
                        if (resspec.PeakIntensity > y0)
                        {
                            //3. Add it to collection
                            var diffirence =  x0 - y0;
                            if (diffirence > 30)
                            {
                                var s = new AnalyzedSpectrum();
                                s.PeakIntensity = y0;
                                s.PeakPixel = resspec.PeakPixel - 1;
                                Result.Add(s);
                            }

                            //reset variables
                            x0 = resspec.PeakIntensity;
                            y0 = x0;
                        }
                        else
                        {
                            y0 = resspec.PeakIntensity;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }

    
}
