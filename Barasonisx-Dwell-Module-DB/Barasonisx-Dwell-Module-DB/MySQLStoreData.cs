using MySql.Data.MySqlClient;
using OpenMetaverse;
using System;
using System.Data;
namespace Barosonix.Dwell.Module.Data
{
	public class MySQLDwellData : MySQLGenericTableHandler<DwellData>, IDwellData
	{
		public MySQLDwellData(string connectionString) : base(connectionString, "","")
		{

			this.m_connectionString = connectionString;
		}

		public int GetTimestamp(UUID av,UUID parcel,string Table)
		{
			int result = 0;
			using (MySqlConnection mySqlConnection = new MySqlConnection(this.m_connectionString))
			{
				mySqlConnection.Open();
				using (MySqlCommand mySqlCommand = new MySqlCommand("select * from `" + Table + "` where id = ?avID and pid = ?parcelID", mySqlConnection))
				{
					mySqlCommand.Parameters.AddWithValue("?avID", av.ToString());
					mySqlCommand.Parameters.AddWithValue("?parcelID", parcel.ToString());
					IDataReader dataReader = mySqlCommand.ExecuteReader();
					if (dataReader.Read())
					{
						string val = dataReader["timestamp"].ToString();
						result = Convert.ToInt32(val);
					}
				}
			}
			return result;
		}

		public void InsertAv(UUID av,UUID parcel,int Tstamp,string Table)
		{
			using (MySqlCommand mySqlCommand = new MySqlCommand())
			{
				mySqlCommand.CommandText = string.Format("replace INTO {0} (id, pid, timestamp) VALUES (?id, ?pid, ?timestamp)",Table);
				mySqlCommand.Parameters.AddWithValue("?id", av.ToString());
				mySqlCommand.Parameters.AddWithValue("?pid", parcel.ToString());
				mySqlCommand.Parameters.AddWithValue("?timestamp", Tstamp.ToString());
				base.ExecuteNonQuery(mySqlCommand);
			}
		}

		public void UpdateTimestamp(UUID av,UUID parcel,int Tstamp,string Table)
		{
			using (MySqlCommand mySqlCommand = new MySqlCommand())
			{
				mySqlCommand.CommandText = string.Format("update {0} set timestamp = ?Tstamp where `id`= ?avID and `pid`= ?parcelID", Table);
				mySqlCommand.Parameters.AddWithValue("?Tstamp", Tstamp.ToString());
				mySqlCommand.Parameters.AddWithValue("?parcelID", parcel.ToString());
				mySqlCommand.Parameters.AddWithValue("?avID", av.ToString());
				base.ExecuteNonQuery(mySqlCommand);
			}
		}

		public void UpdateDwell(UUID parcel, int dwell, string Table)
		{

			using (MySqlCommand mySqlCommand = new MySqlCommand())
			{
				mySqlCommand.CommandText = string.Format("update {0} set Dwell = ?Dwell where `UUID`= ?parID", Table);
				mySqlCommand.Parameters.AddWithValue("?parID", parcel.ToString());
				mySqlCommand.Parameters.AddWithValue("?Dwell", dwell.ToString());
				base.ExecuteNonQuery(mySqlCommand);
			}
		}

		public int GetDwell(UUID parcel,string Table)
		{
			int result = 0;
			using (MySqlConnection mySqlConnection = new MySqlConnection(this.m_connectionString))
			{
				mySqlConnection.Open();
				using (MySqlCommand mySqlCommand = new MySqlCommand("select * from `" + Table + "` where UUID = ?parcelID", mySqlConnection))
				{
					mySqlCommand.Parameters.AddWithValue("?parcelID", parcel.ToString());
					IDataReader dataReader = mySqlCommand.ExecuteReader();
					if (dataReader.Read())
					{
						string val = dataReader["Dwell"].ToString();
						result = Convert.ToInt32(val);
					}
				}
			}
			return result;
		}
	}
}
