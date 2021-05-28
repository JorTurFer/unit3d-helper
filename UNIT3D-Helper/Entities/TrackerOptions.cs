using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIT3D_Helper.Entities
{
    public class TrackerOptions
    {
        public const string SectionName = "Tracker";
        public Uri Url { get; set; }
        public string Username { get; set; }
        public string Coockie { get; set; }  
    }
}
