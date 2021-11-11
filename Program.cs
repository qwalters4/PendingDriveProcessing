using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;

namespace PendingDriveProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Check if Pending exists
            Console.Write("Current Directory: ");
            string curPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
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
            List<PhysicalDisk> pending = new List<PhysicalDisk>();

            foreach (string file in Directory.EnumerateFiles(Path.Combine(@curPath, "Pending"), "*.json"))
            {
                PhysicalDisk temp = JsonConvert.DeserializeObject<PhysicalDisk>(File.ReadAllText(file));
                pending.Add(temp);
            }
            #endregion

            #region Deserialize and build dictionary
            Dictionary<string, string> knownff = new Dictionary<string, string>();
            List<string> unknown = new List<string>();
            string dic = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            knownff = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(@dic, "KnownDriveDictionary.json")));
            #endregion

            int i = 0;
            bool found = false;

            #region Check all disks and check if its in DB
            foreach (PhysicalDisk p in pending)
            {
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
                    if(!unknown.Contains(p.ModelId))
                    {
                        Console.WriteLine("ModelID: " + p.ModelId);
                        Console.WriteLine("SerialNumber: " + p.SerialNumber + "\n");
                        unknown.Add(p.ModelId);
                    }
                    pending.Remove(p);
                }
                found = false;
            }
            Console.WriteLine("Successfully parsed all pending drives.");
            #endregion

            File.WriteAllLines(Path.Combine(dic, "UnknownDrives.txt"), unknown);

            Console.WriteLine("Number of form factors changed: " + i);

            #region Sort and write to Archive then delete from Pending
            Console.WriteLine("Archiving...");
            string path;
            foreach (PhysicalDisk p in pending)
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

            DirectoryInfo di = new DirectoryInfo(Path.Combine(@curPath, "Pending"));
            foreach (FileInfo file in di.EnumerateFiles())
            {
                    file.Delete();
            }
            #endregion

            Console.WriteLine("Complete");
            Thread.Sleep(20000);
        }
    }
}
