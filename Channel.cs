﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Channel class was introduced in NaN0 IRC v0.2.0a

namespace NaN0IRC
{
    class Channel
    {
        string name;
        List<string> users;
        string topic;
        string[] contents;

        public string Name { get { return name; } set { name = value; } }
        public string Topic { get { return topic; } set { topic = value; } }

        public void addUser(string user) {
            if (user.Length > 0)
            {
                if (user.Substring(0, 1) == "@" | user.Substring(0, 1) == "+")
                    user = user.Substring(1);
                users.Add(user);
            }
            users.Sort();
        }

        public bool containsUser(string user) { return users.Contains(user); }
        public void removeUser(string user) { users.Remove(user); }
        public void changeUser(string oldUser, string newUser) { users.Remove(oldUser); users.Add(newUser); }

        public string[] getUsers()
        {
            string[] allUsers = new string[users.Count];
            for (int i = 0; i < users.Count; i++)
                allUsers[i] = users[i];
            return allUsers;
        }

        public Channel(string channelName)
        {
            name = channelName;
            users = new List<string>();
            contents = new string[NaN0IRC.CHATLINES];
        }
    }
}