using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;
namespace GPXManager.entities
{
    public class GPSRepository
    {
        public List<GPS> GPSes { get; set; }

        public GPSRepository()
        {
            GPSes = getGPSes();
        }

        private List<GPS> getGPSes()
        {
            List<GPS> listGPS = new List<GPS>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from devices";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        listGPS.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            GPS gps = new GPS();
                            gps.DeviceID = dr["DeviceID"].ToString();
                            gps.Code = dr["Code"].ToString();
                            gps.DeviceName = dr["DeviceName"].ToString();
                            gps.Brand = dr["Brand"].ToString();
                            gps.Model = dr["Model"].ToString();
                            gps.Folder = dr["Folder"].ToString();
                            listGPS.Add(gps);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }
                return listGPS;
            }
        }

        public bool Add(GPS gps)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into devices(Code,DeviceName,Brand,Model,DeviceID,Folder,DateAdded)
                           Values ('{gps.Code}','{gps.DeviceName}', '{gps.Brand}','{gps.Model}','{gps.DeviceID}','{gps.Folder}','{DateTime.Now.ToString("dd-MMMM-yyyyy HH:mm:ss")}')";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Update(GPS gps)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update devices set
                                DeviceName= '{gps.DeviceName}',
                                Brand = '{gps.Brand}',
                                Model = '{gps.Model}',
                                Folder = '{gps.Folder}'
                            WHERE Code = '{gps.Code}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }
        public bool ClearTable()
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from devices";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        update.ExecuteNonQuery();
                        success = true;
                    }
                    catch (OleDbException)
                    {
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        success = false;
                    }
                }
            }
            return success;
        }
        public bool Delete(string code)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from devices where GPSCode='{code}'";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    try
                    {
                        success = update.ExecuteNonQuery() > 0;
                    }
                    catch (OleDbException)
                    {
                        success = false;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(ex);
                        success = false;
                    }
                }
            }
            return success;
        }
    }
}
