using System;

namespace InfoPages.Models
{
    public class EventPoint
    {
        public EventPoint(DateTime x, string type, double? value)
        {
            X = x;
            Type = type;
            Value = value;
        }

        public DateTime X { get; set; }

        public string Type { get; set; }

        //Temperature change in C
        public double? Value { get; set; }
    }
}
