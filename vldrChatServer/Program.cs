using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vldrChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpServer server = new TcpServer(5555);
        }
    }
}
