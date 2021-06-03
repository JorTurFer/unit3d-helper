using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNIT3D_Helper.Entities
{
    public class WorkerOptions
    {
        public const string SectionName = "Worker";
        public string ExecutionCron { get; set; }
        public int FilesInRowReadyPreviouslyBeforeStop { get; set; } = -1;        
    }
}
