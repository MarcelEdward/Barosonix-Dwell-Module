This is a Dwell module (parcel Taffic) for Barasonix which at this time and probably for the foreseeable future is just a renamed clone of OpenSim and therefore will work for current OpenSim.

To compile: Simply add the Barasonix-Dwell-Module folder to the addon-modules
directory and re run ./runprebuil.sh then xbuild

To configure  : Add the following to opensim.ini 

[Dwell]     
    DwellModule = BarosonixDwellModule      
    NPCaddToDwell = false       
    AvReturnTime = 5        
    StorageProvider = "Barasonisx-Dwell-Module-DB.dll"      
    ConnectionString = "Data Source=127.0.0.1;Database=opensim;UserID=opensim;Password=**********;Old Guids=true;"      

DwellModule tells OpenSim to use the Barosonix mmodule.

NPCaddToDwell allows or dissallows NPC,s to be able to increment the dwell count

AvReturnTime is the time in minutes that MUST pass before an avatar re entering a parcel will be counted in the dwell count

ConnectionString = the conection info for your db.

Next is on to the Database
This module makes use of the land table in the OpenSim database and requires one more table to be added for which I have provided dwell.sql, Simply import into the OpenSim 

Once that is done then simply start OpenSim.exe and voila Parcel Traffic now works and also if the osssearch module is used aswell then the popular places should work to along with traffic counts in other search pages.



