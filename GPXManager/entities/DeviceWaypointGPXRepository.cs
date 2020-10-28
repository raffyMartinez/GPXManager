using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.OleDb;

namespace GPXManager.entities
{
    public class DeviceWaypointGPXRepository
    {
        public List<DeviceWaypointGPX> DeviceWaypointGPXes { get; set; }

        public DeviceWaypointGPXRepository()
        {
            DeviceWaypointGPXes = getDeviceWaypointGPXes();
        }

        public List<DeviceWaypointGPX> getDeviceWaypointGPXes()
        {
            var thisList = new List<DeviceWaypointGPX>();
            var dt = new DataTable();
            using (var conection = new OleDbConnection(Global.ConnectionString))
            {
                try
                {
                    conection.Open();
                    string query = $"Select * from device_waypoints_gpx";


                    var adapter = new OleDbDataAdapter(query, conection);
                    adapter.Fill(dt);
                    if (dt.Rows.Count > 0)
                    {
                        thisList.Clear();
                        foreach (DataRow dr in dt.Rows)
                        {
                            DeviceWaypointGPX gpx = new DeviceWaypointGPX();
                            gpx.Filename = dr["FileName"].ToString();
                            gpx.GPS = Entities.GPSViewModel.GetGPSEx(dr["DeviceID"].ToString());
                            gpx.RowID = int.Parse(dr["RowID"].ToString());
                            gpx.GPX = dr["WaypointGPX"].ToString();
                            gpx.MD5 = dr["md5"].ToString();
                            thisList.Add(gpx);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);

                }
            }
             return thisList;
        }

        public bool Add(DeviceWaypointGPX gpx)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Insert into device_waypoints_gpx (DeviceID,FileName,WaypointGPX,RowID,md5,DateAdded,DateModified)
                           Values (
                                    '{gpx.GPS.DeviceID}',
                                    '{gpx.Filename}', 
                                    '{gpx.GPX}',
                                     {gpx.RowID},
                                    '{gpx.MD5}',
                                    '{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}',
                                    '{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}'
                                  )";
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
                var sql = $"Delete * from device_waypoints_gpx";
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
        public bool Update(DeviceWaypointGPX gpx)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $@"Update device_waypoints_gpx set
                                DeviceID= '{gpx.GPS.DeviceID}',
                                WaypointGPX = '{gpx.GPX}',
                                FileName = '{gpx.Filename}',
                                md5='{gpx.MD5}',
                                DateModified='{DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}'
                            WHERE RowID = {gpx.RowID}";
                using (OleDbCommand update = new OleDbCommand(sql, conn))
                {
                    success = update.ExecuteNonQuery() > 0;
                }
            }
            return success;
        }

        public bool Delete(int rowID)
        {
            bool success = false;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                var sql = $"Delete * from device_waypoints_gpx where RowID={rowID}";
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

        public int MaxRecordNumber()
        {
            int max_rec_no = 0;
            using (OleDbConnection conn = new OleDbConnection(Global.ConnectionString))
            {
                conn.Open();
                const string sql = "SELECT Max(RowID) AS max_id FROM device_waypoints_gpx";
                using (OleDbCommand getMax = new OleDbCommand(sql, conn))
                {
                    max_rec_no = (int)getMax.ExecuteScalar();
                }
            }
            return max_rec_no;
        }
    }
}
