using System.Collections.Generic;

namespace InfoPages.Models
{
    public class DashboardViewModel
    {
        public Graph Graph { get; set; }
        public List<PumperStateChange> PumperStateChanges { get; set; }
    }
}
