using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;

namespace NetPlay
{
    class User
    {
        public TcpClient tcp { get; set; }
        public Socket socket { get; set; }
        public IPAddress ipAddress { get; set; }
        public int port { get; set; }
        public string hostName { get; set; }
        public NetworkStream stream { get; set; }
        public bool connected { get; set; }
        public string nick { get; set; }
        public int playerId { get; set; }
    }
}
