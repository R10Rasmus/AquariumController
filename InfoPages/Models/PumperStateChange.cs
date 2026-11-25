using System;

namespace InfoPages.Models
{
    public class PumperStateChange
    {
        public DateTime Timestamp { get; set; }
        public bool FromState { get; set; }
        public bool ToState { get; set; }
    }
}
