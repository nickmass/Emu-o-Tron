using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using EmuoTron;

namespace NetPlay
{
    class NetPlayClient
    {
        int playerNumber;
        TcpClient tcpClient;
        NetworkStream stream;
        public string message = "";
        public int pendingMessage;
        public bool connected;
        public byte player1;
        public byte player2;
        string nick;

        public NetPlayClient(string ip, int port, string nick)
        {
            player1 = 0;
            player2 = 0;
            this.playerNumber = 2;
            this.nick = nick;
            tcpClient = new TcpClient();
            tcpClient.Connect(IPAddress.Parse(ip), port);
            connected = true;
            stream = tcpClient.GetStream();
            Thread clientThread = new Thread(new ParameterizedThreadStart(ClientStart));
            clientThread.Start(tcpClient);
        }
        public void SendInput(byte input)
        {
            if (playerNumber == 0 && connected)
            {
                if (input != player1)
                {
                    stream.WriteByte((byte)MessageType.player1);
                    stream.WriteByte((byte)1);
                    stream.WriteByte(input);
                    player1 = input;
                }
            }
            if (playerNumber == 1 && connected)
            {
                if (input != player2)
                {
                    stream.WriteByte((byte)MessageType.player2);
                    stream.WriteByte((byte)1);
                    stream.WriteByte(input);
                    player2 = input;
                }
            }
        }
        private void ClientStart(object clientTCP)
        {
            TcpClient tcp = (TcpClient)clientTCP;
            stream.WriteByte((byte)MessageType.nick);
            stream.WriteByte((byte)nick.Length);
            for (int j = 0; j < nick.Length; j++)
                stream.WriteByte((byte)nick[j]);
            while (tcp.Connected && connected)
            {
                if (stream.DataAvailable)
                {
                    MessageType incomingMessage = (MessageType)stream.ReadByte();
                    int length = stream.ReadByte();
                    string message = "";
                    int i = 0;
                    while (i < length)
                    {
                        message += (char)stream.ReadByte();
                        i++;
                    }
                    if (incomingMessage == MessageType.leave)
                    {
                        this.pendingMessage = 90;
                        this.message = message + " has disconnected";
                    }
                    else if (incomingMessage == MessageType.playerid)
                    {
                        playerNumber = (byte)(message[0]);
                    }
                    else if (incomingMessage == MessageType.join)
                    {
                        this.pendingMessage = 90;
                        this.message = message + " has connected";
                    }
                    else if (incomingMessage == MessageType.message)
                    {
                        this.pendingMessage = 90;
                        this.message = message;
                    }
                    else if (incomingMessage == MessageType.player1 && (playerNumber == 1 || playerNumber > 1))
                    {
                        player1 = (byte)message[0];
                    }
                    else if (incomingMessage == MessageType.player2 && (playerNumber == 0 || playerNumber > 1))
                    {
                        player2 = (byte)message[0];
                    }
                }
                Thread.Sleep(1);
            }
            connected = false;
            stream.Close();
            tcpClient.Close();
        }
        public void Close()
        {
            if (connected)
            {
                stream.WriteByte((byte)MessageType.leave);
                stream.WriteByte((byte)1);
                stream.WriteByte((byte)1);
                connected = false;
                tcpClient.Client.Disconnect(false);
                stream.Close();
                tcpClient.Close();
            }
        }
    }
}
