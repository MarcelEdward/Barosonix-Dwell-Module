/*
     * Copyright (c) Contributors, http://opensimulator.org/
     * See CONTRIBUTORS.TXT for a full list of copyright holders.
     *
     * Redistribution and use in source and binary forms, with or without
     * modification, are permitted provided that the following conditions are met:
     * * Redistributions of source code must retain the above copyright
     * notice, this list of conditions and the following disclaimer.
     * * Redistributions in binary form must reproduce the above copyright
     * notice, this list of conditions and the following disclaimer in the
     * documentation and/or other materials provided with the distribution.
     * * Neither the name of the OpenSimulator Project nor the
     * names of its contributors may be used to endorse or promote products
     * derived from this software without specific prior written permission.
     *
     * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
     * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
     * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
     * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
     * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
     * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
     * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
     * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
     * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
     * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     */

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

[assembly: Addin("Barosonix-Dwell-Module", "0.1")]
[assembly: AddinDependency("OpenSim", "0.5")]

namespace Barosonix.Dwell.Module
{
	[Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule")]
	public class DwellModule : IDwellModule, INonSharedRegionModule
	{
		private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private List<Scene> m_Scenes = new List<Scene>();
		private static List<ILandObject> Parcels = new List<ILandObject> ();
		private Scene m_scene;
		public int dwell = 0;

		public Type ReplaceableInterface
		{
			get { return typeof(IDwellModule); }
		}

		public string Name
		{
			get { return "DwellModule"; }
		}

		public void Initialise(IConfigSource source)
		{
			m_log.Info ("[DWELL]:Initialised");
		}

		public void AddRegion(Scene scene)
		{
			m_log.Info ("[DWELL]:Add Region");
			scene.RegisterModuleInterface<IDwellModule>(this);

			m_scene = scene;

			m_scene.EventManager.OnNewClient += OnNewClient;
			m_scene.EventManager.OnAvatarEnteringNewParcel += OnAvatarEnteringNewParcel;
			lock(m_Scenes)
			{
				m_Scenes.Add(scene);
				register(scene);
			}
		}

		public void RegionLoaded(Scene scene)
		{
			//m_log.Info ("[DWELL]:Region Loaded");
			//Parcels
			//int test  =  (scene).LandChannel.AllParcels().Count;
			//foreach (ILandObject Parcel in Parcels)
			//{
			//	LandData ParcelData = Parcel.LandData;
			//m_log.Info ("[DWELL]:Parcel ID = " + test.ToString ());//ParcelData.Name.ToString());
			//}

			//List<ILandObject> m_los = scene.LandChannel.AllParcels();

			//foreach (ILandObject landObject in m_los)
			//{
			//	m_log.Info ("[DWELL]:BOGIES");
			//}
		}

		public void RemoveRegion(Scene scene)
		{

		}

		public void Close()
		{
		}

		public void register(Scene scene)
		{
			List<ILandObject> m_los = scene.LandChannel.AllParcels();

			foreach (ILandObject landObject in m_los)
			{
				landObject.LandData.Dwell = 99;
				m_log.Info ("[DWELL]:BOGIES");
			}
		}




		public void OnAvatarEnteringNewParcel(ScenePresence avatar, int localLandID, UUID RegionID)
		{

			UUID id = new UUID ();
			UUID pid = new UUID ();
			pid = avatar.currentParcelUUID;
			id = avatar.UUID;
			UpdateDwell(id,pid);

		}

		public void OnNewClient(IClientAPI client)
		{

			client.OnParcelDwellRequest += ClientOnParcelDwellRequest;

		}

		private Hashtable GenericXMLRPCRequest(Hashtable ReqParams, string method, string server)
		{
			ArrayList SendParams = new ArrayList();
			SendParams.Add(ReqParams);

			// Send Request
			XmlRpcResponse Resp;
			try
			{
				XmlRpcRequest Req = new XmlRpcRequest(method, SendParams);
				Resp = Req.Send(server, 30000);
			}
			catch (WebException ex)
			{
				m_log.ErrorFormat("[DWELL] : Unable to connect to Dwell " +
					"Server {0}.  Exception {1}", "http://192.168.0.15/services/dwell/xmlrpc.php", ex);

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
				ErrorHash["errorMessage"] = "Unable to fetch Dwell data at this time. ";
				ErrorHash["errorURI"] = "";

				return ErrorHash;
			}
			catch (XmlException ex)
			{
				m_log.ErrorFormat("[DWELL] : Unable to connect to Dwell " +
					"Server {0}.  Exception {1}", "http://192.168.0.15/services/dwell/xmlrpc.php", ex);

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

		public void UpdateDwell(UUID av,UUID parcelID)
		{
			string pid = parcelID.ToString();
			string id = av.ToString();
			m_log.Info ("[DWELL]:Sending Dwell Update request for parcel "+pid+" and av "+id);
			Hashtable ReqHash = new Hashtable();
			ReqHash["id"] = id;
			ReqHash["pid"] = pid;

			Hashtable result = GenericXMLRPCRequest(ReqHash,
				"UpdateDwell", "http://192.168.0.15/services/dwell/xmlrpc.php");
			ArrayList dataArray = (ArrayList)result["data"];

			Hashtable d = (Hashtable)dataArray[0];
			string rs = d["report"].ToString();
			m_log.Info ("[DWELL]:"+rs);



		}

		public int GetDwell(UUID parcelID)
		{
			string pid = parcelID.ToString();
			Hashtable ReqHash = new Hashtable();
			m_log.ErrorFormat ("[DWELL]:" + pid);
			ReqHash["pid"] = pid;

			Hashtable result = GenericXMLRPCRequest(ReqHash,
				"GetDwell", "http://192.168.0.15/services/dwell/xmlrpc.php");

			if (!Convert.ToBoolean(result["success"]))
			{
				////remoteClient.SendAgentAlertMessage(
				//result["errorMessage"].ToString(), false);
				return 0;
			}
			ArrayList dataArray = (ArrayList)result["data"];

			Hashtable d = (Hashtable)dataArray[0];
			int rs = Convert.ToInt32(d["dwellers"].ToString());
			return rs;
		}
	}
}