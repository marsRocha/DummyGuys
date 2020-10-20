using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common
{
    public class PlayerInfo
    {
        public Guid Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public float RotX { get; set; }
        public float RotY { get; set; }
        public float RotZ { get; set; }
    }
}
