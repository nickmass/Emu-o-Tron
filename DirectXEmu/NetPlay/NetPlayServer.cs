using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
namespace NetPlay
{
    class NetPlayServer
    {
        private TcpListener tcpServer;
        private Thread serverThread;
        private bool serverActive;
        Users users;
        public NetPlayServer(int port)
        {
            tcpServer = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            serverThread = new Thread(new ThreadStart(Listen));
            users = new Users();
            serverThread.Start();
        }
        private void Listen()
        {
            tcpServer.Start();
            serverActive = true;
            while (serverActive)
            {
                if (tcpServer.Pending())
                {
                    Thread clientThread = new Thread(new ParameterizedThreadStart(ClientStart));
                    clientThread.Start(tcpServer.AcceptTcpClient());
                }
                Thread.Sleep(1);
            }
        }
        private void ClientStart(object clientTCP)
        {
            int id = users.NextID();
            users[id] = new User();
            users[id].playerId = -1;
            users[id].tcp = (TcpClient)clientTCP;
            users[id].socket = users[id].tcp.Client;
            users[id].stream = users[id].tcp.GetStream();
            users[id].ipAddress = ((IPEndPoint)this.users[id].socket.RemoteEndPoint).Address;
            users[id].port = ((IPEndPoint)users[id].socket.RemoteEndPoint).Port;
            users[id].connected = true;
            while (users[id].tcp.Connected && users[id].connected && serverActive)
            {
                if (this.users[id].stream.DataAvailable)
                {
                    MessageType incomingMessage = (MessageType)users[id].stream.ReadByte();
                    int length = users[id].stream.ReadByte();
                    string message = "";
                    int i = 0;
                    while (i < length)
                    {
                        message += (char)users[id].stream.ReadByte();
                        i++;
                    }
                    if (incomingMessage == MessageType.leave)
                    {
                        users[id].connected = false;
                        users[id].socket.Disconnect(false);
                        users[id].stream.Close();
                        users[id].tcp.Close();
                        users.SendToAll(MessageType.leave, users[id].nick);
                    }
                    else if (incomingMessage == MessageType.nick)
                    {
                        users[id].nick = message;
                        for(int j = 1; j < users.MaxID(); j++)
                        {
                            if (users[j].connected && users[j].playerId > users[id].playerId)
                                users[id].playerId = users[j].playerId;
                        }
                        users[id].playerId++;
                        users.SendMessage(id, MessageType.playerid, ((char)users[id].playerId).ToString());
                        users.SendToAll(MessageType.join, users[id].nick);
                    }
                    else
                    {
                        users.SendToAll(incomingMessage, message);
                    }
                }
                Thread.Sleep(1);
            }
        }

        public void Close()
        {
            serverActive = false;
            Thread.Sleep(100);
            tcpServer.Stop();
        }
    }
}
