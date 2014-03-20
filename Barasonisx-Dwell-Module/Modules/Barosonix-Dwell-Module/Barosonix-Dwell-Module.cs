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
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages.Linden;
using OpenSim.Framework;
using OpenSim.Framework.Capabilities;
using OpenSim.Framework.Console;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Physics.Manager;
using OpenSim.Services.Interfaces;
using Caps = OpenSim.Framework.Capabilities.Caps;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using Mono.Addins;
using Nwc.XmlRpc;

[assembly: Addin("BarosonixDwellModule", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace Barosonix.Dwell.Module
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "BarosonixDwellModule")]
	public class BarosonixDwellModule : IDwellModule, INonSharedRegionModule
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private Scene m_scene;
		private IConfigSource m_Config;
		private string m_DwellServer = "";
		private bool m_NPCaddToDwell = false;
		private int m_AvReturnTime = 60;
		private bool m_Enabled = false;
		public int dwell = 0;
		public string Name { get { return "BarosonixDwellModule"; } }        

		public Type ReplaceableInterface
		{
			get { return null; }
		}
		public void Initialise(IConfigSource source)
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

			m_NPCaddToDwell = DwellConfig.GetBoolean ("NPCaddToDwell", false);
			m_AvReturnTime = DwellConfig.GetInt ("AvReturnTime", 60);
		}

		public void Close()
		{

		}

		public void AddRegion(Scene scene)
		{
			if (!m_Enabled)
				return;

			scene.RegisterModuleInterface<IDwellModule>(this);

			m_scene = scene;

			m_scene.EventManager.OnNewClient += OnNewClient;
			m_scene.EventManager.OnAvatarEnteringNewParcel += OnAvatarEnteringNewParcel;

		}

		public void RemoveRegion(Scene scene)
		{
			if (!m_Enabled)
				return;

			scene.UnregisterModuleInterface<IDwellModule>(this);
		}        

		public void RegionLoaded(Scene scene)
		{

		}   

		public void OnAvatarEnteringNewParcel(ScenePresence avatar, int localLandID, UUID RegionID)
		{
			UUID id = avatar.UUID;
			UUID pid = avatar.currentParcelUUID;

			if (!m_NPCaddToDwell) {
				if (!npccheck (id)) {
					checkav(id,pid,m_AvReturnTime);
					} 
				} else {
				checkav(id,pid,m_AvReturnTime);
				}


		}

		public void OnNewClient(IClientAPI client)
		{

			client.OnParcelDwellRequest += ClientOnParcelDwellRequest;

		}

		private Hashtable GenericXMLRPCRequest(Hashtable ReqParams, string method, string server)
		{
			ArrayList SendParams = new ArrayList();
			SendParams.Add(ReqParams);

			XmlRpcResponse Resp;
			try
			{
				XmlRpcRequest Req = new XmlRpcRequest(method, SendParams);
				Resp = Req.Send(server, 30000);
			}
			catch (WebException ex)
			{
				m_log.ErrorFormat("[DWELL] : Unable to connect to Dwell " +
					"Server {0}.  Exception {1}", m_DwellServer, ex);

				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to fetch Dwell data at this time. ";
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}
			catch (SocketException ex)
			{
				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to fetch Dwell data at this time. "+ ex;
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}
			catch (XmlException ex)
			{
				m_log.ErrorFormat("[DWELL] : Unable to connect to Dwell " +
					"Server {0}.  Exception {1}", m_DwellServer, ex);

				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to fetch Dwell data at this time. ";
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}
			if (Resp.IsFault)
			{
				Hashtable ErrorHash = new Hashtable();
				ErrorHash["success"] = false;
				ErrorHash["errorMessage"] = "Unable to fetch Dwell data at this time. ";
				ErrorHash["errorURI"] = "";
				return ErrorHash;
			}
			Hashtable RespData = (Hashtable)Resp.Value;

			return RespData;
		}

		private void ClientOnParcelDwellRequest(int localID, IClientAPI client)
		{
			ILandObject parcel = m_scene.LandChannel.GetLandObject(localID);
			if (parcel == null)
				return;

			UUID id = (UUID)parcel.LandData.GlobalID.ToString();
			dwell = GetDwell(id);
			client.SendParcelDwellReply (localID, parcel.LandData.GlobalID, dwell);
		}

		private void checkav(UUID av,UUID parcelID,int time)
		{
			string pid = parcelID.ToString();
			string id = av.ToString();
			string avrtime = time.ToString();
			Hashtable ReqHash = new Hashtable();
			ReqHash["id"] = id;
			ReqHash["pid"] = pid;
			ReqHash["avrt"] = avrtime;

			Hashtable result = GenericXMLRPCRequest(ReqHash,
				"Checkav", m_DwellServer);
			if (!Convert.ToBoolean(result["success"]))
			{
			}
		}

		private bool npccheck(UUID clientID)
		{
			ScenePresence p;

			p = m_scene.GetScenePresence(clientID);

			if (p != null && !p.IsChildAgent) {
				if (p.PresenceType == PresenceType.Npc) 
				{
					return true;
				} 
				else
				{
					return false;
				}
			}
			return false;
		}


		public int GetDwell(UUID parcelID)
		{
			string pid = parcelID.ToString();
			Hashtable ReqHash = new Hashtable();
			ReqHash["pid"] = pid;

			Hashtable result = GenericXMLRPCRequest(ReqHash,
				"GetDwell", m_DwellServer);

			if (!Convert.ToBoolean(result["success"]))
			{
				return 0;
			}
			ArrayList dataArray = (ArrayList)result["data"];

			Hashtable d = (Hashtable)dataArray[0];
			int rs = Convert.ToInt32(d["dwellers"].ToString());
			return rs;
		}
		             
	}
}