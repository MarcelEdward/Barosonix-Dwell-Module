This is a Dwell module (parcel Taffic) for opensim

To compile: Simply add the Barasonix-Dwell-Module folder to the addon-modules
directory and re run ./runprebuil.sh then xbuild

Toc configure  : Add the following to opensim.ini 

[Dwell]
    DwellModule = BarosonixDwellModule
    DwellURL = http://yoururlhere
    NPCaddToDwell = false
    AvReturnTime = 5

DwellModule tells Opensim to use the Barosonix mmodule.
DwellUrl is the url to the dwell xmlrpc.php file
NPCaddToDwell allows or dissallows NPC,s to be able to increment the dwell count
AvReturnTime is the time in minutes that MUST pass before an avatar re entering a parcel will be counted in the dwell count

