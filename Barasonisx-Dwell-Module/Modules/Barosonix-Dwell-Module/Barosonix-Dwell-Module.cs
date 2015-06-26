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

namespace Barosonix.Dwell.Module
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "BarosonixDwellModule")]
	public class BarosonixDwellModule : IDwellModule, INonSharedRegionModule
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		private System.Timers.Timer m_timer = new System.Timers.Timer();
		
		private Scene m_scene;
		private IConfigSource m_Config;
		private string m_DwellServer = "";
		private bool m_NPCaddToDwell = false;
		private int m_AvReturnTime = 10;
		private bool m_Enabled = false;
		private int m_periodToCountInHours = 24;
		private bool m_countUniqueAvatar = false;
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
			m_periodToCountInHours = DwellConfig.GetInt ("PeriodToCountInHours", 24);
			m_countUniqueAvatar = DwellConfig.GetBoolean ("CountUniqueAvatar", false);
			
			m_timer = new System.Timers.Timer();
			m_timer.AutoReset = true;
			m_timer.Enabled = true;
			m_timer.Interval = 60000 * m_AvReturnTime; // 60 secs * return time out of config
			m_timer.Elapsed += ProcessQueue;
			m_timer.Start();			
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

				
		private void ProcessQueue(object sender, System.Timers.ElapsedEventArgs e)
		{
			int number = 0;
			
			Hashtable ReqHash = new Hashtable();
			
			ReqHash["avrt"] = m_AvReturnTime.ToString();
			ReqHash["pch"] = m_periodToCountInHours;
			ReqHash["cua"] = m_countUniqueAvatar.ToString();
			//the region uuid and region name
			ReqHash["regionuuid"] = m_scene.RegionInfo.RegionID.ToString();
			ReqHash["regionname"] = m_scene.RegionInfo.RegionName;
			// get a list of avatars in the region
			//m_log.DebugFormat("[DWELL]: region {0} ", m_scene.RegionInfo.RegionName);
			
			foreach (ScenePresence avatar in m_scene.GetScenePresences()) {
				if (!m_NPCaddToDwell) {
					if (npccheck (avatar.UUID)) {
						continue;
					} 
				} 
				if (!avatar.IsChildAgent)
				{
					string numberStr = number.ToString();
					number = number + 1;
					//m_log.DebugFormat("[DWELL]: add avatar {0} ", avatar.Name);
					Hashtable data = new Hashtable();
					data["id"] = avatar.UUID.ToString();
					data["avatar"] = avatar.Name;
					//m_log.DebugFormat("[DWELL]: avatar postion {0} {1} {2}",avatar.AbsolutePosition.X.ToString(),avatar.AbsolutePosition.Y.ToString(),avatar.AbsolutePosition.Z.ToString());
					ILandObject parcel = m_scene.LandChannel.GetLandObject(avatar.AbsolutePosition);
					
					data["pid"] = parcel.LandData.GlobalID.ToString();

					data["localLandID"] = parcel.LandData.LocalID.ToString();
					data["parcel"] = parcel.LandData.Name.ToString();
					data["parcelOwner"] = parcel.LandData.OwnerID.ToString();
					data["parcelGroupOwned"] = "0";
					data["parcelOwnerName"] = "";
					if (parcel.LandData.IsGroupOwned) {
						data["parcelGroupOwned"] = "1";
						IGroupsModule groups = m_scene.RequestModuleInterface<IGroupsModule>();
						if (groups != null)
						{
							GroupRecord gr = groups.GetGroupRecord(parcel.LandData.OwnerID);
							if (gr != null)
								data["parcelOwnerName"] = gr.GroupName.ToString();	
						}
					} else {
						UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, parcel.LandData.OwnerID);
						data["parcelOwnerName"] = account.Name;	
					}	
					ReqHash[numberStr] = data;
				}
			}
			if (number>0)
			{	
				//send the request and process update the presence count in the dwell database
				ReqHash["number"] = number.ToString();
				//m_log.DebugFormat("[DWELL]: send xmlrpc request");
				Hashtable result = GenericXMLRPCRequest(ReqHash,
					"Checkav", m_DwellServer);
				if (Convert.ToBoolean(result["success"]))
				{
					//m_log.DebugFormat("[DWELL]: got success back, process result");
					ArrayList dataArray = (ArrayList)result["data"];
					
					
					int totalNumber = dataArray.Count;
					//m_log.DebugFormat("[DWELL]: total number of parcels {0}",totalNumber.ToString());
					for (int i = 0; i < totalNumber; i++)
					{
						Hashtable d = (Hashtable)dataArray[i];
						int LocalLandID = Convert.ToInt32(d["localLandID"].ToString());
						ILandObject parcel = m_scene.LandChannel.GetLandObject(LocalLandID);
						parcel.LandData.Dwell = Convert.ToInt32(d["data"].ToString());
						m_scene.LandChannel.UpdateLandObject(LocalLandID, parcel.LandData);
					}

				}				
			}
	
		}

		public void OnNewClient(IClientAPI client)
		{
			client.OnParcelDwellRequest += ClientOnParcelDwellRequest;
			client.OnCompleteMovementToRegion += ClientOnCompleteMovementToRegion;
		}
		
		public void OnAvatarEnteringNewParcel(ScenePresence avatar, int localLandID, UUID RegionID)
		{
			UUID id = avatar.UUID;
			UUID pid = avatar.currentParcelUUID;
			string avatarName = avatar.Name.ToString();
			
			if (!m_NPCaddToDwell) {
				if (!npccheck (id)) {
					checkav(id,pid,m_AvReturnTime,localLandID,avatarName);
					} 
				} else {
				checkav(id,pid,m_AvReturnTime,localLandID,avatarName);
				}
		}

		public void  ClientOnCompleteMovementToRegion(IClientAPI client, bool theBoolean)
		{
			ScenePresence avatar = m_scene.GetScenePresence(client.AgentId);
			if (!avatar.IsChildAgent)
			{
				//m_log.DebugFormat("[DWELL]: region {0} avatar {1}", m_scene.RegionInfo.RegionName, avatar.Name);
				UUID id = client.AgentId;
				string avatarName = avatar.Name.ToString();
				
				ILandObject parcel = m_scene.LandChannel.GetLandObject(avatar.AbsolutePosition);
				UUID pid = parcel.LandData.GlobalID;
				int localLandID = parcel.LandData.LocalID;

				if (!m_NPCaddToDwell) {
					if (!npccheck (id)) {
						checkav(id,pid,m_AvReturnTime,localLandID,avatarName);
					} 
				} else {
					checkav(id,pid,m_AvReturnTime,localLandID,avatarName);
				}				
			}
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
			//m_log.Debug("[DWELL]: ClientOnParcelDwellRequest");
			
			ILandObject parcel = m_scene.LandChannel.GetLandObject(localID);
			if (parcel == null)
				return;

			client.SendParcelDwellReply(localID, parcel.LandData.GlobalID, parcel.LandData.Dwell);	
		}

		private void checkav(UUID av,UUID parcelID,int time, int localLandID, string avatarName)
		{
			//m_log.Debug("[DWELL]: Check av");
			ILandObject parcel = m_scene.LandChannel.GetLandObject(localLandID);
			string pid = parcelID.ToString();
			string id = av.ToString();
			string avrtime = time.ToString();
			
			Hashtable data = new Hashtable();
			data["id"] = id;
			data["pid"] = pid;
			data["parcel"] = parcel.LandData.Name.ToString();
			data["avatar"] = avatarName;
			data["localLandID"] =  localLandID.ToString();
			data["parcel"] = parcel.LandData.Name.ToString();
			data["parcelOwner"] = parcel.LandData.OwnerID.ToString();
			data["parcelGroupOwned"] = "0";
			data["parcelOwnerName"] = "";
			if (parcel.LandData.IsGroupOwned) {
				data["parcelGroupOwned"] = "1";
				IGroupsModule groups = m_scene.RequestModuleInterface<IGroupsModule>();
				if (groups != null)
				{
					GroupRecord gr = groups.GetGroupRecord(parcel.LandData.OwnerID);
					if (gr != null)
						data["parcelOwnerName"] = gr.GroupName.ToString();	
				}
			} else {
				UserAccount account = m_scene.UserAccountService.GetUserAccount(UUID.Zero, parcel.LandData.OwnerID);
				data["parcelOwnerName"] = account.Name;	
			}
				
			Hashtable ReqHash = new Hashtable();
			ReqHash["avrt"] = avrtime;
			ReqHash["pch"] = m_periodToCountInHours;
			ReqHash["cua"] = m_countUniqueAvatar.ToString();
			ReqHash["number"] = "1";
			ReqHash["regionuuid"] = m_scene.RegionInfo.RegionID.ToString();;
			ReqHash["regionname"] = m_scene.RegionInfo.RegionName;
			ReqHash["0"] = data;
			
			Hashtable result = GenericXMLRPCRequest(ReqHash,
				"Checkav", m_DwellServer);
			if (Convert.ToBoolean(result["success"]))
			{
				ArrayList dataArray = (ArrayList)result["data"];
				Hashtable d = (Hashtable)dataArray[0];
				parcel.LandData.Dwell = Convert.ToInt32(d["data"].ToString());
				m_scene.LandChannel.UpdateLandObject(localLandID, parcel.LandData);
			}
		}

		private bool npccheck(UUID clientID)
		{
			ScenePresence p;
			//m_log.Debug("[DWELL]: npccheck");
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
			m_log.Debug("[DWELL]: GetDwell");
			// here just return 0, does not seem to be used
			return 0;
		}
		             
	}
}