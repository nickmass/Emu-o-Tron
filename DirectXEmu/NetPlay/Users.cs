using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace NetPlay
{
    class Users
    {
        private Dictionary<int, User> users = new Dictionary<int, User>();
        private int id;
        public Users()
        {
            this.id = 0;
        }
        public int NextID()
        {
            this.id++;
            return this.id;
        }
        public int MaxID()
        {
            return this.id;
        }
        public void SendMessage(int id, MessageType type, string message)
        {
            if (this.users.ContainsKey(id))
            {

                if (users[id].tcp.Connected && users[id].connected)
                {
                    users[id].stream.WriteByte((byte)type);
                    users[id].stream.WriteByte((byte)message.Length);
                    for (int j = 0; j < message.Length; j++)
                        this.users[id].stream.WriteByte((byte)message[j]);
                }
            }
            else
            {
                throw new Exception("Invalid user id");
            }
        }
        public void SendToAllBut(int id, MessageType type, string message)
        {
            for (int i = 0; i <= (id + 1); i++)
            {
                if (users.ContainsKey(i) && id != i)
                {
                    if (users[i].tcp.Connected && users[i].connected)
                    {
                        users[i].stream.WriteByte((byte)type);
                        users[i].stream.WriteByte((byte)message.Length);
                        for (int j = 0; j < message.Length; j++)
                            this.users[i].stream.WriteByte((byte)message[j]);
                    }
                }
            }
        }
        public void SendToAll(MessageType type, string message)
        {
            for (int i = 0; i <= (id + 1); i++)
            {
                if (users.ContainsKey(i))
                {
                    if (users[i].tcp.Connected && users[i].connected)
                    {
                        users[i].stream.WriteByte((byte)type);
                        users[i].stream.WriteByte((byte)message.Length);
                        for (int j = 0; j < message.Length; j++)
                            this.users[i].stream.WriteByte((byte)message[j]);
                    }
                }
            }
        }
        public User this[int index]
        {
            get
            {
                return this.users[index];
            }
            set
            {
                if (this.users.ContainsKey(index))
                {
                    this.users[index] = value;
                }
                else
                {
                    this.users.Add(index, value);
                }
            }
        }
    }
}
