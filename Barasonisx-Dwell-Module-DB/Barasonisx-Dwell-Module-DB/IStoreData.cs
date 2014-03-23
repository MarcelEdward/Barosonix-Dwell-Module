using OpenMetaverse;
using System;
namespace Barosonix.Dwell.Module.Data
{
	public interface IDwellData
	{
		int GetDwell(UUID parcel,string Table);
		int GetTimestamp(UUID av,UUID parcel,string Table);
		void InsertAv(UUID av,UUID parcel,int Tstamp,string Table);
		void UpdateTimestamp(UUID av,UUID parcel,int Tstamp,string Table);
		void UpdateDwell(UUID parcel, int dwell, string Table);
	}
}
