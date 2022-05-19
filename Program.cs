using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PendingDriveProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Check if Pending exists
            Console.Write("Current Directory: ");
            string curPath = @"\\TRUENAS\DiskTesting";
            Console.Write(curPath + "\n");
            Console.WriteLine("Attempting to read from Pending...");
            //Thread.Sleep(1000);
            if (Directory.Exists(Path.Combine(@curPath, "Pending")))
                Console.WriteLine("Read Successful!");
            else
            {
                Console.WriteLine("Read Failure! Exiting...");
                //Thread.Sleep(3000);
                System.Environment.Exit(1);
            }
            #endregion

            #region Read every file from Pending and add to list
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            List<PhysicalDisk> pending = new List<PhysicalDisk>();

            foreach (string file in Directory.EnumerateFiles(Path.Combine(@curPath, "Pending"), "*.json"))
            {
                PhysicalDisk temp = JsonConvert.DeserializeObject<PhysicalDisk>(File.ReadAllText(file));
                pending.Add(temp);
            }
            stopwatch.Stop();
            Console.WriteLine("Finished deserializing in " + stopwatch.ElapsedMilliseconds / 1000.0 + "s.");
            stopwatch.Reset();
            #endregion

            #region Deserialize and build dictionary
            stopwatch.Start();
            Dictionary<string, string> knownff = new Dictionary<string, string>();
            List<string> unknownModel = new List<string>();
            List<PhysicalDisk> unknownDisk = new List<PhysicalDisk>();
            string dic = @"\\TRUENAS\DiskTesting";
            knownff = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(@dic, "KnownDriveDictionary.json")));
            stopwatch.Stop();
            Console.WriteLine("Finished constructing dictionary in " + stopwatch.ElapsedMilliseconds/1000.0 + "s.");
            stopwatch.Reset();
            #endregion

            int i = 0;
            bool found = false;

            #region Check all disks and check if its in DB
            List<string> tokens = new List<string>();
            List<string> brands = new List<string> { "wdc", "wd", "seagate", "intel", "hitachi",
                "adata", "apacer", "apple", "axiom", "corsair", "crucial", "diesel", "fujitsu", 
                "hgst", "hp", "ibm", "kingfast", "kingston", "toshiba", "lenovo", "lexar", "liteon",
                "liteonit", "maxtor", "mercury", "micron", "netapp", "ocz", "owc", "patriot", "samsung",
                "pny", "sandisk", "sic", "sk hynix", "skhynix", "spcc", "visiontek", "wintek", "kioxia",
                "plextor", "transcend", "quantum", "mushkin", "dell"};

            stopwatch.Start();
            Parallel.ForEach(pending, p =>
            {
                if (p.ModelId.Substring(0, 2).ToLower() == "st")
                    p.Brand = "seagate";
                else if (p.ModelId.Substring(0, 2).ToLower() == "ct")
                    p.Brand = "crucial";
                else
                {
                    foreach (string s in brands)
                    {
                        if (p.ModelId.ToLower().Contains(s))
                        {
                            p.Brand = s;
                        }
                    }
                }

                if (p.Brand == null)
                    p.Brand = "Unknown";
                
                foreach (KeyValuePair<string, string> kv in knownff)
                {
                    if (p.ModelId.Contains(kv.Key))
                    {
                        p.FormFactor = kv.Value;
                        i++;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (!unknownModel.Contains(p.ModelId))
                    {
                        Console.WriteLine("ModelID: " + p.ModelId);
                        Console.WriteLine("SerialNumber: " + p.SerialNumber + "\n");
                        unknownModel.Add(p.ModelId);
                    }
                    unknownDisk.Add(p);
                }
                found = false;
            });
            foreach(PhysicalDisk p in unknownDisk)
            {
                pending.Remove(p);
            }
            stopwatch.Stop();
            Console.WriteLine("Successfully parsed all pending drives in " + stopwatch.ElapsedMilliseconds/1000.0 + "s.");
            stopwatch.Reset();
            #endregion

            File.WriteAllLines(Path.Combine(dic, "UnknownDrives.txt"), unknownModel);

            Console.WriteLine("Number of form factors changed: " + i);

            #region Sort and write to Archive then delete from Pending
            Console.WriteLine("Archiving...");
            stopwatch.Start();
            string path;
            foreach( PhysicalDisk p in pending)
            {
                if (!Directory.Exists(Path.Combine(@curPath, "Archive", p.PONumber.ToString())))
                    Directory.CreateDirectory(Path.Combine(@curPath, "Archive", p.PONumber.ToString()));
                path = Path.Combine(@curPath, "Archive", p.PONumber.ToString(), (p.SerialNumber + ".json"));
                JsonSerializer s = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(path))
                using (JsonWriter w = new JsonTextWriter(sw))
                {
                    s.Serialize(w, p);
                }
            }

            DataService dataService = new DataService();
            dataService.InsertFailsafe(pending);

            DirectoryInfo di = new DirectoryInfo(Path.Combine(@curPath, "Pending"));
            List<string> properties = unknownDisk.Select(o => o.SerialNumber).ToList();
            Parallel.ForEach(di.EnumerateFiles(), file =>
            {
                if (!properties.Contains(file.Name.Substring(0, file.Name.Length - 5)))
                    file.Delete();
            });
            stopwatch.Stop();
            Console.WriteLine("Finished in " + stopwatch.ElapsedMilliseconds/1000.0 + "s.");
            #endregion

            Console.WriteLine("Complete");
            Thread.Sleep(20000);
        }
    }
}
