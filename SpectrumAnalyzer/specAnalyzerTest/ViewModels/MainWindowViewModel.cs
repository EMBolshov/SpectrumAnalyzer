using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string PicPath { get; set; }
        public List<DataPoint> PlotPoints { get; set; }
        private ObservableCollection<AnalyzedSpectrum> _peaks;
        public ObservableCollection<AnalyzedSpectrum> Peaks { get { return _peaks; } set { _peaks = value; NotifyPropertyChanged(nameof(Peaks)); } }
        public MainWindowViewModel()
        {
            OpenPicCommand = new BaseCommand(OpenPic, true);
            AnalyzeCommand = new BaseCommand(AnalyzeSpectrum, true);
            Peaks = new ObservableCollection<AnalyzedSpectrum>();
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
            Peaks.Clear();
            PlotPoints.Clear();
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
                    var _spec = new AnalyzedSpectrum();
                    int index = 4 * x;
                    int red = avgpixels[index];
                    int green = avgpixels[index + 1];
                    int blue = avgpixels[index + 2];
                    _spec.PeakPixel = x;
                    _spec.PeakIntensity = Math.Round(Math.Sqrt(0.299 * red * red + 0.587 * green * green + 0.114 * blue * blue),2);
                    Peaks.Add(_spec);
                    var _point = new DataPoint(_spec.PeakPixel, _spec.PeakIntensity);
                    PlotPoints.Add(_point);
             }
        }
    }

    
}
