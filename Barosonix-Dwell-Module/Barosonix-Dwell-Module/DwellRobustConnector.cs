using System;
using System.Collections;
using System.Net;
using System.Reflection;
using Barosonix.Dwell.Module.Data;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Server.Base;
using OpenSim.Server.Handlers.Base;
using OpenSim.Services.Interfaces;
using log4net;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace Barosonix.Dwell.Module
{
    public class DwellRobustConnector : ServiceConnector
    {
        private static readonly ILog m_log = LogManager.GetLogger (MethodBase.GetCurrentMethod ().DeclaringType);
        private IDwellData m_Database;
        private bool m_debugEnabled;
        public bool m_Enabled;

        public DwellRobustConnector (IConfigSource config, IHttpServer server, string configName) : base (config, server, configName)
        {

            IConfig DwellConfig = config.Configs ["Dwell"];
            if (DwellConfig == null) {
                this.m_Enabled = false;
                DwellRobustConnector.m_log.DebugFormat ("[Dwell.Robust.Connector]: Configuration Error Not Enabled", new object[0]);
                return;
            }
            this.m_Enabled = true;
            string StorageProvider = DwellConfig.GetString ("StorageProvider", string.Empty);
            string ConnectionString = DwellConfig.GetString ("ConnectionString", string.Empty);


            if (StorageProvider == string.Empty || ConnectionString == string.Empty) {
                this.m_Enabled = false;
                DwellRobustConnector.m_log.ErrorFormat ("[Dwell.Robust.Connector]: missing service specifications Not Enabled", new object[0]);
                return;
            }
            this.m_Database = ServerUtils.LoadPlugin<IDwellData> (StorageProvider, new object[] {
                ConnectionString
            });
            DwellRobustConnector.m_log.DebugFormat ("[Dwell.Robust.Connector]: Initialzing", new object[0]);

            server.AddXmlRPCHandler ("GetDwell", new XmlRpcMethod (this.GetDwell));
            server.AddXmlRPCHandler ("SetDwell", new XmlRpcMethod (this.SetDwell));

            m_Database.migrate ();
        }

        private XmlRpcResponse GetDwell (XmlRpcRequest request, IPEndPoint remoteClient)
        {
            int dwell = 0;
            XmlRpcResponse xmlRpcResponse = new XmlRpcResponse ();
            Hashtable hashtable = new Hashtable ();
            try {
                if (request.Params.Count > 0) {
                    Hashtable hashtable2 = (Hashtable)request.Params [0];
                    string pid = (string)hashtable2 ["uuid"];
                    dwell = m_Database.GetDwell ((UUID)pid, "land");
                    hashtable ["Dwell"] = dwell.ToString ();
                }
            } catch (Exception exception) {
                m_log.Error ("[Dwell.Robust.Connector]: Caught unexpected exception:", exception);
            }
            xmlRpcResponse.Value = hashtable;
            return xmlRpcResponse;
        }


        private XmlRpcResponse SetDwell (XmlRpcRequest request, IPEndPoint remoteClient)
        {
           
            XmlRpcResponse xmlRpcResponse = new XmlRpcResponse ();
            Hashtable hashtable = new Hashtable ();

            try {
                if (request.Params.Count > 0) {
                    Hashtable hashtable2 = (Hashtable)request.Params [0];
                    string pid = (string)hashtable2 ["uuid"];
                    int dwell = Convert.ToInt32(hashtable2 ["dwell"]);
                    m_Database.SetDwell ((UUID)pid, dwell, "land");
                   
                }
            }
            catch (Exception exception) {
                m_log.Error ("[Dwell.Robust.Connector]: Caught unexpected exception:", exception);
            }
            xmlRpcResponse.Value = hashtable;
            return xmlRpcResponse;
        }
    }
}
