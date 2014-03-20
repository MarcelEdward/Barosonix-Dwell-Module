This is a Dwell module (parcel Taffic) for Barasonix which at this time and probably for the foreseeable future is just a renamed clone of OpenSim and therefore will work for current OpenSim.

To compile: Simply add the Barasonix-Dwell-Module folder to the addon-modules
directory and re run ./runprebuil.sh then xbuild

To configure  : Add the following to opensim.ini 

[Dwell]             
    DwellModule = BarosonixDwellModule              
    DwellURL = http://yoururlhere               
    NPCaddToDwell = false               
    AvReturnTime = 5                

DwellModule tells OpenSim to use the Barosonix mmodule.

DwellUrl is the url to the dwell xmlrpc.php file

NPCaddToDwell allows or dissallows NPC,s to be able to increment the dwell count

AvReturnTime is the time in minutes that MUST pass before an avatar re entering a parcel will be counted in the dwell count

Next is on to the Database
This module makes use of the land table in the OpenSim database and requires one more table to be added for which I have provided dwell.sql, Simply import into the OpenSim db and then proceede to set up the databaseinfo.php file.

Once that is done then simply start OpenSim.exe and voila Parcel Traffic now works and also if the osssearch module is used aswell then the popular places should work to along with traffic counts in other search pages.



