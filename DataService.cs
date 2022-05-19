using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Npgsql;

namespace PendingDriveProcessor
{

    public class DataService
    {
        private NpgsqlConnection _connection;

        public DataService()
        {
            _connection = new NpgsqlConnection();
            _connection.ConnectionString = "Host=65.26.61.201;Username=inventory;Password=whygod1234;Database=inventory";
            _connection.Open();
            //65.26.61.201 is the public ip
            //192.168.1.229 is the local ip
        }
        public void InsertFailsafe(List<PhysicalDisk> incoming)
        {
            foreach (PhysicalDisk item in incoming)
            {
                string Query = "insert into hdd (brand, modelid, connector, formfactor, quantity, capacity, lastupdatetime ) values('" + item.Brand + "', '" + item.ModelId + "', '" + item.Connector + "', '" + item.FormFactor + "', " + "1" + ", " + item.Capacity + ", " + "localtimestamp" +")on conflict on constraint hdd_un do update set";
                Query += " brand = '" + item.Brand + "',connector = '" + item.Connector + "',formfactor = '" + item.FormFactor + "',quantity = " + "1" + ",capacity = " + item.Capacity + ", lastupdatetime = localtimestamp;";
                NpgsqlCommand insert = new NpgsqlCommand(Query, _connection);
                insert.ExecuteNonQuery();
                Query = "";
            }
        }
        public void CloseConnection()
        {
            _connection.Close();
        }
    }
}