using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace InfinitasPreloader
{
    class Program
    {
        public class KonmaiFile
        {
            public string ResourceFilename { get; set; }
            public string Url { get; set; }
        }

        public static int total = 0;
        public static int completed = 0;
           
        static void Main(string[] args)
        {
            if (args.Length > 0 && args.First() == "-convert")
            {
                ConvertDownloadListToCsv();
                return;
            }

            Console.WriteLine("Preloading Infinitas files...");

            if (!File.Exists("filelist.csv"))
            {
                Console.WriteLine("Error! filelist.csv not found. Make sure its next to the exe.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            var infinitasResourcePath = (string)Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("KONAMI").OpenSubKey("beatmania IIDX INFINITAS").GetValue("ResourceDir");

            Console.WriteLine($"Infinitas resource path is {infinitasResourcePath}");

            var files = File.ReadAllLines("filelist.csv").Select(l => new KonmaiFile() { ResourceFilename = l.Split(',')[0], Url = l.Split(',')[1] }).ToList();
            total = files.Count;
            
            foreach(var file in files)
            {
                DownloadFile(file, infinitasResourcePath).Wait(); // do these in parallel one day if I can figure out the konami limits
            }

            Console.WriteLine("Complete!");

        }

        public static async Task<bool> DownloadFile(KonmaiFile file, string resourcePath)
        {
            try
            {
                var targetFile = resourcePath + "dlcache" + file.ResourceFilename.Replace("/", "\\");
                if (!File.Exists(targetFile))
                {
                    using (var client = new HttpClient())
                    {
                        var url = $"http://atnrsc.konaminet.jp/atn/bm2dx/infinitas/v2/appdata{file.Url}";
                        var response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetFile));

                            using (Stream destination = File.Create(targetFile))
                            {
                                await response.Content.CopyToAsync(destination);
                                completed++;
                                Console.WriteLine($"[{completed}/{total}] Downloaded to {targetFile}");
                            }

                        }
                        else
                        {
                            Console.WriteLine("Failed to download " + url);
                        }
                    }

                } else
                {
                    completed++;
                }

                return true;
            } 
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                completed++;
                return false;
            }

        }

        private static void ConvertDownloadListToCsv()
        {
            Console.WriteLine("Converting decrypted Infinitas downloadlist.xml to CSV file...");

            if (!System.IO.File.Exists("downloadlist.xml"))
            {
                Console.WriteLine("Cannot find downloadlist.xml");
                return;
            }

            var files = new List<KonmaiFile>();

            foreach (var level1Element in XElement.Load("downloadlist.xml").Elements("file"))
            {
                var file = new KonmaiFile();

                file.ResourceFilename = level1Element.Element("savePath").Value;
                file.Url = level1Element.Element("urlPath").Value;

                files.Add(file);
            }

            using (var writer = new StreamWriter("filelist.csv", false))
            {
                foreach (var file in files)
                {
                    writer.WriteLine(file.ResourceFilename + "," + file.Url);
                }
            }

            Console.WriteLine("Written filelist.csv");
            return;
        }
    }
}
