// See https://aka.ms/new-console-template for more information

using onnx_manager;
using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Text;

namespace Model_runner
{
    class Program
    {
        static Manager manager = new Manager();
        static SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Enter files");
                return;
            }

            var tokensource = new CancellationTokenSource();
            Console.CancelKeyPress += (x, y) => tokensource.Cancel();


            Task[] tasks;
            try
            {
                manager.Downloader();
                int jpgCount = 0;
                foreach (var name in args)
                {
                    if (name.Contains(".jpg"))
                    {
                        jpgCount++;
                    }
                }

                tasks = new Task[jpgCount];
                int i = 0;
                foreach (var name in args)
                {
                    if (!name.Contains(".jpg"))
                        continue;
                    //var image = Image.Load<Rgb24>(name); 
                    tasks[i] = AllActions(name, tokensource.Token);
                    i++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            Task.WaitAll(tasks);
            
        }

        static async Task<List<MyImageInfo>> AllActions(string name, CancellationToken token)
        {
            var image = Image.Load<Rgb24>(name);
            var task = await manager.CallModelAsync(image, token);
            var i = 0;
            foreach (var item in task)
            {
                var a = item.StartImage.Clone();
                //a.SaveAsJpeg(new Random().NextDouble() + name);
                manager.Annotate(a, item.Objectbox);
                a.SaveAsJpeg(item.Objectbox.Class + i++ + name);

                semaphore.Wait();
                if (!File.Exists("res.csv"))
                {
                    File.AppendAllLines("res.csv", new List<string>() { "X; Y; H; W; CLASS; PATH" });
                }
                File.AppendAllLines("res.csv", new List<string>() 
                {
                    $"{item.Objectbox.XMin}; {item.Objectbox.YMin}; {item.Objectbox.YMax - item.Objectbox.YMin}; {item.Objectbox.XMax - item.Objectbox.XMin}; {item.Objectbox.Class}; {Directory.GetCurrentDirectory() + "\\" + name}"
                });
                semaphore.Release();

            }
            return task;
        }
    }

}