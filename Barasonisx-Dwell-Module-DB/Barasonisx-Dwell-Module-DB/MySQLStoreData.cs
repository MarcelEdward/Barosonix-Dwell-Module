using MySql.Data.MySqlClient;
using OpenMetaverse;
using System;
using System.Data;
namespace Barosonix.Dwell.Module.Data
{
	public class MySQLStoreData : MySQLGenericTableHandler<DwellData>, IDwellData
	{
		public MySQLStoreData(string connectionString, string realm) : base(connectionString, realm, "land")
		{
			this.m_Realm = realm;
			this.m_connectionString = connectionString;
		}

		public int GetDwell(UUID parcel)
		{
			int result = 0;
			using (MySqlConnection mySqlConnection = new MySqlConnection(this.m_connectionString))
			{
				mySqlConnection.Open();
				using (MySqlCommand mySqlCommand = new MySqlCommand("select * from `" + this.m_Realm + "` where UUID = ?parcelID", mySqlConnection))
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
