using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Xml;
using log4net;
using Barosonix.Dwell.Module.Data;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using OpenSim.Services.Interfaces;
using OpenSim.Server.Base;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Mono.Addins;
using Nwc.XmlRpc;

[assembly: Addin ("BarosonixDwellModule", "0.1")]
[assembly: AddinDependency ("OpenSim", "0.5")]
namespace Barosonix.Dwell.Module
{
    [Extension (Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "BarosonixDwellModule")]
    public class BarosonixDwellModule : IDwellModule, INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_Config;
        private IDwellData m_DwellData;
        private bool m_NPCaddToDwell = false;
        private int m_AvReturnTime = 60;
        private Functions m_Funcs;
        private string m_DwellServer = "";
        private bool m_Enabled = false;
        public int dwell = 0;

        public string Name { get { return "BarosonixDwellModule"; } }

        public Type ReplaceableInterface {
            get { return null; }
        }

        public void Initialise (IConfigSource source)
        {
            m_Config = source;

            IConfig DwellConfig = m_Config.Configs ["Dwell"];

            if (DwellConfig == null) {
                m_Enabled = false;
                return;
            }

            m_Enabled = (DwellConfig.GetString ("DwellModule", "BarosonixDwellModule") == "BarosonixDwellModule");

            if (!m_Enabled) {
                return;
            }
            m_DwellServer = DwellConfig.GetString ("DwellURL", "");

            if (m_DwellServer == "") {
                m_Enabled = false;
                return;
            } 
            this.m_Funcs = new Functions ();
            string StorageProvider = DwellConfig.GetString ("StorageProvider", "");
            string ConnectionString = DwellConfig.GetString ("ConnectionString", "");

            if (StorageProvider == string.Empty || ConnectionString == string.Empty) {
                m_Enabled = false;
                m_log.ErrorFormat ("[DWELL]: missing service specifications Not Enabled", new object[0]);
                return;
            }
            m_DwellData = ServerUtils.LoadPlugin<IDwellData> (StorageProvider, new object[] {
                ConnectionString
            });

            m_NPCaddToDwell = DwellConfig.GetBoolean ("NPCaddToDwell", false);
            m_AvReturnTime = DwellConfig.GetInt ("AvReturnTime", 60);

        }

        public void Close ()
        {

        }

        public void AddRegion (Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.RegisterModuleInterface<IDwellModule> (this);

            m_scene = scene;

            m_scene.EventManager.OnNewClient += OnNewClient;
            m_scene.EventManager.OnAvatarEnteringNewParcel += OnAvatarEnteringNewParcel;

        }

        public void RemoveRegion (Scene scene)
        {
            if (!m_Enabled)
                return;

            scene.UnregisterModuleInterface<IDwellModule> (this);
        }

        public void RegionLoaded (Scene scene)
        {

        }

        public void OnAvatarEnteringNewParcel (ScenePresence avatar, int localLandID, UUID RegionID)
        {
            UUID id = avatar.UUID;
            UUID pid = avatar.currentParcelUUID;
          

            if (!m_NPCaddToDwell) {
                INPCModule module = m_scene.RequestModuleInterface<INPCModule>();
                if (module != null)
                {
                    if (!module.IsNPC (id, m_scene)) {
                            checkav (id, pid, m_AvReturnTime);
                     }
                     }
                } else {
                checkav (id, pid, m_AvReturnTime);
                }
           
        }

        public void OnNewClient (IClientAPI client)
        {
            client.OnParcelDwellRequest += ClientOnParcelDwellRequest;
        }

        private void ClientOnParcelDwellRequest (int localID, IClientAPI client)
        {
            ILandObject parcel = m_scene.LandChannel.GetLandObject (localID);
            if (parcel == null)
                return;

            UUID id = (UUID)parcel.LandData.GlobalID.ToString ();
            dwell = GetDwell (id);
            client.SendParcelDwellReply (localID, parcel.LandData.GlobalID, dwell);
        }

        private int GetDwellers (UUID parcelID)
        {
            int dwell = 0;
            Hashtable ReqHash = new Hashtable ();
            ReqHash ["uuid"] = parcelID.ToString ();

            Hashtable result = m_Funcs.GenericXMLRPCRequest (ReqHash,
                                   "GetDwell", m_DwellServer);
            dwell = Convert.ToInt32 (result ["Dwell"]);
            return dwell;
        }

        private void SetDwellers (UUID parcelID, int dwell)
        {

            Hashtable ReqHash = new Hashtable ();
            ReqHash ["uuid"] = parcelID.ToString ();
            ReqHash ["dwell"] = dwell.ToString ();

            Hashtable result = m_Funcs.GenericXMLRPCRequest (ReqHash,
                                   "SetDwell", m_DwellServer);

        }

        private void checkav (UUID av, UUID pid, int time)
        {

            int dif = 0;
            int rtime = time * 60;
            int dbstamp = m_DwellData.GetTimestamp (av, pid, "landDwell");
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1))).TotalSeconds;
            int cstamp = unixTimestamp;
            int cdwell = GetDwellers (pid);
               if (dbstamp == 0) {
                m_DwellData.InsertAv (av, pid, cstamp, "landDwell");
             SetDwellers (pid, cdwell + 1);
            } else {
               dif = (cstamp - dbstamp);
              if (dif < rtime) {
             } else {
                SetDwellers (pid, cdwell + 1);
                m_DwellData.UpdateTimestamp (av, pid, cstamp, "landDwell");
             }

             }
        }


        private ScenePresence FindClient (UUID agentID)
        {

            ScenePresence presence = m_scene.GetScenePresence (agentID);
            if (presence != null && !presence.IsChildAgent) {
            } else {
                return presence;
            }

                    
            return null;
           
        }

        public int GetDwell (UUID parcelID)
        {
            int result = GetDwellers (parcelID);
            return result;
        }
    }
}