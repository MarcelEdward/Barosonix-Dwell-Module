using System;
using System.Text;
using System.Collections;
using Nwc.XmlRpc;


namespace Barosonix.Dwell.Module
{
    public class Functions
    {
        public string encode (string msg)
        {
            byte[] bytes = Encoding.UTF8.GetBytes (msg);
            string txt = Convert.ToBase64String (bytes);
            string s = strrev (txt);
            byte[] bytes2 = Encoding.UTF8.GetBytes (s);
            return Convert.ToBase64String (bytes2);
        }

        public string decode (string msg)
        {
            byte[] bytes = Convert.FromBase64String (msg);
            string revstring = Encoding.UTF8.GetString (bytes);
            string s = this.strrev (revstring);
            byte[] bytes2 = Convert.FromBase64String (s);
            return Encoding.UTF8.GetString (bytes2);
        }

        public string strrev (string txt)
        {
            string text = "";
            for (int i = 0; i < txt.Length; i++) {
                char c = txt [i];
                text = new string (c, 1) + text;
            }
            return text;
        }

        public Hashtable GenericXMLRPCRequest (Hashtable ReqParams, string method, string server)
        {
            ArrayList SendParams = new ArrayList ();
            SendParams.Add (ReqParams);

            // Send Request
            XmlRpcResponse Resp;
            try {
                XmlRpcRequest Req = new XmlRpcRequest (method, SendParams);
                Resp = Req.Send (server, 30000);
            } catch (Exception ex) {
                Hashtable ErrorHash = new Hashtable ();
                ErrorHash ["success"] = false;
                ErrorHash ["message"] = ex;
                return ErrorHash;
            }

            Hashtable RespData = (Hashtable)Resp.Value;

            return RespData;
        }
    }
}
