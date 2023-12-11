using Microsoft.Win32;
using onnx_manager;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Xml.Linq;

namespace Lab2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<ImageInfoWpf> imageInfoWpf = new List<ImageInfoWpf>();
        public ICommand ProcessFiles { get => processFiles; }
        public ICommand Cancel { get => cancel; }
        public ICommand Delete { get => delete; }


        private Manager manager = new Manager();

        private ICommand processFiles;
        private ICommand cancel;
        private ICommand delete;

        private bool cancelflag = false;
        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();
            manager.Downloader();

            processFiles = new AsyncRelayCommand(
                async obj =>
                {
                    tokenSource = new CancellationTokenSource();
                    cancelflag = true;
                    //var imageInfoWpf = new List<ImageInfoWpf>();
                    var fileDialog = new OpenFileDialog()
                    {
                        Multiselect = true,
                        Filter = "Image (*.jpg) | *.jpg",
                    };
                    var names = new List<string>();
                    if (fileDialog.ShowDialog() == true)
                    {
                        foreach (var fileName in fileDialog.FileNames)
                        {
                            names.Add(fileName);
                        }
                    }

                    foreach (var fileName in names)
                    {
                        var image = SixLabors.ImageSharp.Image.Load<Rgb24>(fileName);
                        var result = await manager.CallModelAsync(image, tokenSource.Token);
                        if (result.Count == 0 || result == null)
                            continue;
                        var imageInfo = new ImageInfoWpf()
                        {
                            StartImage = new ImageSharpImageSource<Rgb24>(image),
                            EndImage = new ImageSharpImageSource<Rgb24>(result[0].EndImage),
                            FileName = fileName,
                            ObjectNumber = result.Count
                        };
                        imageInfoWpf.Add(imageInfo);
                        imageInfoWpf = imageInfoWpf.OrderByDescending(x => x.ObjectNumber).ThenBy(x => x.FileName).ToList();
                        StartListBox.ItemsSource = null;
                        StartListBox.ItemsSource = imageInfoWpf;
                    }
                    cancelflag = false;
                },
                obj => true
            );

            cancel = new AsyncRelayCommand(
                async obj =>
                {
                    tokenSource.Cancel();
                    cancelflag = false;
                },
                obj => cancelflag
            );

            delete = new AsyncRelayCommand(
                async obj =>
                {
                    imageInfoWpf.Clear();
                    StartListBox.ItemsSource = null;
                },
                obj => imageInfoWpf.Count != 0
                );

            DataContext = this;
        }
    }

    public class ImageInfoWpf
    {
        public ImageSharpImageSource<Rgb24> StartImage { get; set; }
        public ImageSharpImageSource<Rgb24> EndImage { get; set; }
        public int ObjectNumber { get; set; }
        public string FileName { get; set; }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object, bool> canExecute;
        private readonly Func<object, Task> executeAsync;
        private bool isExecuting;

        public AsyncRelayCommand(Func<object, Task> executeAsync, Func<object, bool> canExecute = null)
        {
            this.executeAsync = executeAsync;
            this.canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            if (isExecuting)
            {
                return false;
            }
            else
            {
                return canExecute is null || canExecute(parameter);
            }
        }

        public void Execute(object? parameter)
        {
            if (!isExecuting)
            {
                isExecuting = true;
                executeAsync(parameter).ContinueWith(_ => 
                {
                    isExecuting = false;
                    CommandManager.InvalidateRequerySuggested();
                }, scheduler: TaskScheduler.FromCurrentSynchronizationContext());
            }
        }
    }
}
