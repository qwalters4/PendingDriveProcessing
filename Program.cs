using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Reflection;
using System.Threading;

namespace PendingDriveProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
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

            List<PhysicalDisk> pending = new List<PhysicalDisk>();

            foreach (string file in Directory.EnumerateFiles(Path.Combine(@curPath, "Pending"), "*.json"))
            {
                PhysicalDisk temp = JsonConvert.DeserializeObject<PhysicalDisk>(File.ReadAllText(file));
                pending.Add(temp);
            }

            Dictionary<string, string> knownff = new Dictionary<string, string>();
            string dic = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            knownff = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(@dic, "KnownDriveDictionary.json")));
            int i = 0;

            foreach(PhysicalDisk p in pending)
            {
                if(knownff.ContainsKey(p.ModelId))
                {
                    p.FormFactor = knownff[p.ModelId];
                    i++;
                    Console.WriteLine("ModelID: " + p.ModelId);
                    Console.WriteLine("SerialNumber: " + p.SerialNumber + "\n");
                }
            }

            Console.WriteLine("Number of form factors changed: " + i);


            DirectoryInfo di = new DirectoryInfo(Path.Combine(@curPath, "Pending"));
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }

            Console.WriteLine("Successfully parsed all pending drives.");
            Console.WriteLine("Pushing drives to DB...");
            //Thread.Sleep(2000);

            //CALL WRITE TO DB
            //string cstring;
            //SqlConnection c;
            //cstring = @"Data Source=WIN-50GP30FGO75;Initial Catalog=Demodb;User ID=sa;Password=demol23";
            //c = new SqlConnection(cstring);
            //c.Open();

            //SqlCommand command;
            //SqlDataAdapter adapter = new SqlDataAdapter();
            //string sql = "";
            //sql = "Insert into master (";
            //command = new SqlCommand(sql, c);
            //adapter.InsertCommand = new SqlCommand(sql, c);
            //adapter.InsertCommand.ExecuteNonQuery();
            //command.Dispose();
            //c.Close();
            //////////////////

            Console.WriteLine("Push complete.");
            Console.WriteLine("Archiving...");

            foreach(PhysicalDisk p in pending)
            {
                string path = Path.Combine(@curPath, "Archive", (p.SerialNumber + ".json"));
                JsonSerializer s = new JsonSerializer();
                using (StreamWriter sw = new StreamWriter(path))
                using (JsonWriter w = new JsonTextWriter(sw))
                {
                    s.Serialize(w, p);
                }
            }

            Console.WriteLine("Complete");
            Thread.Sleep(5000);
        }
    }
}
