using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIT3D_Helper.Entities
{
    public class Fingerprint
    {
        public string id { get; set; }
        public string name { get; set; }
        public string locale { get; set; }
        public string path { get; set; }
        public string method { get; set; }
    }

    public class Data
    {
        public List<object> torrent { get; set; }
        public List<object> user { get; set; }
        public object gracias_dadas { get; set; }
    }

    public class Torrent
    {
        public string @class { get; set; }
        public int id { get; set; }
        public List<object> relations { get; set; }
        public string connection { get; set; }
    }

    public class User
    {
        public string @class { get; set; }
        public int id { get; set; }
        public List<string> relations { get; set; }
        public string connection { get; set; }
    }

    public class Models
    {
        public Torrent torrent { get; set; }
        public User user { get; set; }
    }

    public class DataMeta
    {
        public Models models { get; set; }
    }

    public class ServerMemo
    {
        public List<object> children { get; set; }
        public List<object> errors { get; set; }
        public string htmlHash { get; set; }
        public Data data { get; set; }
        public DataMeta dataMeta { get; set; }
        public string checksum { get; set; }
    }

    public class Payload
    {
        public string id { get; set; }
        public string method { get; set; }
        public List<int> @params { get; set; }
    }

    public class Update
    {
        public string type { get; set; }
        public Payload payload { get; set; }
    }

    public class ThanksPayload
    {
        public ThanksPayload()
        {
            updates = new List<Update>();
        }
        public Fingerprint fingerprint { get; set; }
        public ServerMemo serverMemo { get; set; }
        public List<Update> updates { get; set; }
    }
}
