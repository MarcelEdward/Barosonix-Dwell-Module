using OpenMetaverse;
using System;
namespace Barosonix.Dwell.Module.Data
{
	public interface IDwellData
	{
		int GetDwell(UUID parcel);
	}
}
