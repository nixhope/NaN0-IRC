using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

// Irc class handles all TCP communications with the IRC server

namespace NaN0IRC
{
    public delegate void StuffHappened(string stuff);
    public delegate void TopicChanged(string stuff);
    public delegate void Restart();
    public delegate void ListNames(string thisChannel, string[] channelUsers);
    public delegate void NewChannel(string channelName);

    class Irc
    {
        public event StuffHappened stuffHappened;
        public event TopicChanged topicChanged;
        public event Restart restart;
        public event ListNames channelUsers;
        public event NewChannel joinNewChannel;

        private string server;
        private int port;
        private string channel;
        private string nick; // The NaN0 user's name
        private string name;
        private TcpClient connection;
        private NetworkStream stream;
        private StreamWriter writer;
        private StreamReader reader;
        private bool quit = false;
        private List<string> boss;
        private List<string> channelNames;
        private List<Channel> channels;
        private bool timestamp = true;
        private bool hidePing = true;
        private RPSLS rpslsGame;
        private bool log = true;
        private bool getResponse = false;
        private DateTime lastPing;
        private string currentChannel;

        public void connect()
        {
            connection = null;
            reader = null;
            stream = null;
            reader = null;
            writer = null;
            cWrite(String.Format("{0} is connecting to {1} on {2}", nick, channel, server));
            try
            {
                connection = new TcpClient(server, port);
            }
            catch (SocketException) { restart(); }
            stream = connection.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            authenticate();
            join(channel);

            while (!quit)
            {
                /* Following code is nifty, but listening is based on events
                cWrite("!LISTENING");
                DateTime now = DateTime.Now;
                if (now.CompareTo(lastPing.AddMinutes(1)) > 0)
                    ping();
                if (now.CompareTo(lastPing.AddSeconds(2)) > 0)
                {
                    cWrite("PING Timeout. Connection to server lost.");
                    //restart();
                }*/
                listen();
            }
            writer.Close();
            reader.Close();
            connection.Close();
        }

        private void listen()
        {
            string input;
            // ObjectDisposedException
            #region Disconnect handling
            try
            {
                input = reader.ReadLine();
            }
            catch (ObjectDisposedException)
            {
                cWrite("NaN0IRC disconnected. Probably due to stream closing.");
                quit = true;
                restart();
                return;
            }
            catch (IOException)
            {
                cWrite("NaN0IRC disconnected. Could not read stream, check internet connection.");
                quit = true;
                restart();
                return;
            }
            #endregion
            if (input != null)
            {
                lastPing = DateTime.Now;
                //cWrite("!"+input); // Checking stuff during testing
                input = parseInput(input);
                if (input.Substring(0, 1) == ":")
                    input = input.Substring(1);
                string[] parts = new string[input.Split(' ').Length];
                parts = input.Split(' ');
                if (!serverMessage(parts, input))
                {
                    if (parts[0] == "PING") // Check if it's a server message, and act accordingly
                        // Server PING, send PONG back
                        this.pong(parts);
                    else
                    {
                        switch (parts[1])
                        {
                            case "JOIN":
                                joined(parts); break;
                            case "PART":
                                left(parts); break;
                            case "MODE":
                                changeMode(parts);
                                break;
                            case "NICK":
                                changeNick(parts);
                                break;
                            case "KICK":
                                kicked(parts);
                                break;
                            case "QUIT":
                                userQuit(parts);
                                break;
                            case "PRIVMSG":
                                String sender = parts[0].Substring(0, input.IndexOf("!"));
                                String recipient = parts[2];
                                String message = input.Substring(input.IndexOf(":") + 1);
                                if (channelNames.Contains(recipient.ToLower()))
                                {   // Message is in a channel
                                    channelMsg(sender, recipient, message);
                                }
                                else if (recipient.ToLower() == nick.ToLower())
                                {   // Message is private to user
                                    privateMsg(sender, message);
                                }
                                break;
                            case "TOPIC":
                                changeTopic(parts);
                                break;
                            default:
                                cWrite("NaN0IRC did not understand server input: " + input);
                                break;
                        }
                    }
                }
            }
        }

        // Write to IRC stream
        public void write(string output)
        {
            if (output.Substring(0, 1) == "!") // Command should be copied to console without timestamp
            {
                if (output.Length > 8)
                {
                    if (output.Substring(1, 7).ToUpper() == "PRIVMSG")
                    {
                        string message = output.Substring(output.IndexOf(' ') + 1);
                        string recipient = message.Substring(0, message.IndexOf(' '));
                        message = message.Substring(message.IndexOf(' ') + 2);
                        if (message.Substring(0,1) == "\u0001") // PRIVMSG CHANNEL :\u0001ACTION {message goes here}\u0001
                            cWrite(String.Format("{0}: *{1} {2}*", recipient, nick, message.Substring(8, message.Length - 9)));
                        else
                            cWrite(String.Format("{0}: <{1}> {2}", recipient, nick, message));
                    }
                    else
                    {
                        cWrite(output);
                    }
                }
                else
                {
                    cWrite(output);
                }
                output = output.Substring(1);
            }
            try
            {
                writer.WriteLine(output);
                writer.Flush();
            }
            catch (IOException) { restart(); }
            catch (ObjectDisposedException) { restart(); }
            catch (NullReferenceException) { restart(); }
        }

        // Write to console or GUI
        public void cWrite(string output)
        {
            DateTime now = DateTime.Now;
            if (output.Substring(0, 1) == "!") // Input command written to console, don't timestamp
            {
                this.stuffHappened(output.Substring(1));
                channels[0].changeContents(output);
            }
            else 
            {
                this.logging(output); // Write to Logs
                string cName = output.Substring(0, output.IndexOf(' '));
                if (cName.Substring(cName.Length - 1) == ":")
                    cName = cName.Substring(0, cName.Length - 1);
                bool existingChannel = false;
                foreach (Channel thing in channels)
                    if (thing.Name == cName)
                    {
                        existingChannel = true;
                        if (timestamp)
                        {
                            thing.changeContents(String.Format("{0:yyyy/MM/dd HH:mm} {1}", now, output));
                            this.stuffHappened(String.Format("{0:yyyy/MM/dd HH:mm} {1}", now, output));
                        }
                        else
                        {
                            thing.changeContents(output);
                            this.stuffHappened(output);
                        }
                    }
                if (!existingChannel) // Sender not on known channel list, probably a private user
                {
                    
                    channelNames.Add(cName);
                    channels.Add(new Channel(cName));
                    channels[channels.Count - 1].addUser(cName);
                    channels[channels.Count - 1].addUser(nick);
                    this.joinNewChannel(cName);
                    currentChannel = cName;
                    if (timestamp)
                    {
                        channels[channels.Count-1].changeContents(String.Format("{0:yyyy/MM/dd HH:mm} {1}", now, output));
                        this.stuffHappened(String.Format("{0:yyyy/MM/dd HH:mm} {1}", now, output));
                    }
                    else
                    {
                        channels[channels.Count - 1].changeContents(output);
                        this.stuffHappened(output);
                    }
                }
            }
            Thread.Sleep(100);
        }

        private void logging(string logText)
        {   
            if (log)
            {
                string logID = logText.Substring(0, logText.IndexOf(' '));
                if (logID.Substring(logID.Length - 1) == ":")
                    logID = logID.Substring(0, logID.Length - 1);
                DateTime now = DateTime.Now;
                // Create a folder for the logs
                string path = "Logs";
                System.IO.Directory.CreateDirectory(path); // Does nothing if Directory exists
                // Create a folder for the user
                path = @"Logs/" + nick;
                System.IO.Directory.CreateDirectory(path);
                // Create a folder for the particular logID
                path += @"/" + logID;
                System.IO.Directory.CreateDirectory(path);
                // Create the text file, using the Year Month timestamp
                StreamWriter stream = new StreamWriter(String.Format("{0}\\{1:yyyy MMM}.txt", path, now), true);
                stream.WriteLine(String.Format("{0:yyyy-dd-MM HH:mm} {1}", now, logText));
                stream.Close();
            }
        }

        // PING server regularly
        public void ping()
        {
            Random random = new Random();
            write("PING "+Convert.ToString(random.Next(100000,1000000)));
        }

        #region Standard IRC events
        // User joined channel
        private void joined(string[] parts) //OK
        {
            if (parts[2].Substring(0, 1) == ":")
                parts[2] = parts[2].Substring(1);
            string channelJoined = parts[2];
            string user = parts[0].Split('!')[0];
            if (parts[0].Split('!')[0] == nick) // A simple way to work out if the user successfully joined a new channel
            {
                channelNames.Add(channelJoined);
                channels.Add(new Channel(channelJoined));
                this.joinNewChannel(channelJoined);
                currentChannel = channelJoined;
            }
            else
            {
                foreach (Channel thing in channels)
                {
                    if (thing.Name == channelJoined)
                        thing.addUser(user);
                    this.channelUsers(thing.Name, thing.getUsers()); // Event that changes the GUI TextBox
                }
            }
            cWrite(String.Format("{0}: {1} joined {0}", channelJoined, user));
        }

        // User left channel
        private void left(string[] parts)
        {
            string user = parts[2];
            cWrite(String.Format("{0}: {1} left {0}", user, parts[0].Split('!')[0]));
            foreach (Channel thing in channels)
                if (thing.containsUser(user))
                {
                    thing.removeUser(user);
                    this.channelUsers(thing.Name, thing.getUsers()); // Event that changes the GUI TextBox
                }
        }

        // User changed mode
        private void changeMode(string[] parts)
        {
            // Channel = parts[2]
            // User = parts[0].Split('!')[0]
            string mode = "";
            for (int i = 3; i < parts.Length; i++)
            {
                mode += parts[i] + " ";
            }
            if (mode.Substring(0, 1) == ":")
                mode = mode.Substring(1);
            if (channelNames.Contains(parts[2]))
            {
                cWrite(String.Format("{0}: {1} sets {2}", parts[2],
                    parts[0].Split('!')[0], mode));
            }
        } // OK

        // User changed nick
        private void changeNick(string[] parts)
        {
            string oldNick = parts[0].Split('!')[0];
            string newNick = parts[2].Remove(0, 1);
            if (oldNick == nick)
                nick = newNick;
            if (boss.Contains(oldNick))
            {
                boss.Remove(oldNick);
                boss.Add(newNick);
            }
            // cWrite(String.Format("{0} is now known as {1}", oldNick, newNick));
            foreach (Channel thing in channels)
                if (thing.containsUser(oldNick))
                {
                    thing.changeUser(oldNick, newNick);
                    cWrite(String.Format("{0}: {1} is now known as {2}", thing.Name, oldNick, newNick));
                }
        }

        private void kicked(string[] parts)
        {
            string kicker = parts[0].Split('!')[0];
            string kicked = parts[3];
            string where = parts[2];
            string kickMessage = "";
            for (int i = 4; i < parts.Length; i++)
            {
                kickMessage += parts[i] + " ";
            }
            cWrite(String.Format("{0}: {1} kicks {2} ({3})", where, kicker, kicked, kickMessage.Substring(1).Trim()));
            foreach (Channel thing in channels)
                if (thing.containsUser(kicked))
                    thing.removeUser(kicked);
            List<string> currentChannels = new List<string>(channelNames);
            if (kicked.ToLower() == nick.ToLower())
            {
                join(where);
            }
        }

        private void userQuit(string[] parts)
        {
            string quitter = parts[0].Split('!')[0];
            string quitMessage = "";
            for (int i = 2; i < parts.Length; i++)
            {
                quitMessage += parts[i] + " ";
            }
            foreach (Channel thing in channels)
                if (thing.containsUser(quitter))
                {
                    thing.removeUser(quitter);
                    cWrite(String.Format("{0}: {1} has quit ({2})", thing.Name, quitter,
                        quitMessage.Substring(1).Trim()));
                    this.channelUsers(thing.Name, thing.getUsers()); // Event that changes the GUI TextBox. Not the best idea to put it here though.
                }
        }
        #endregion Standard IRC events

        private void channelMsg(string sender, string recipient, string message)
        { // Someone said something in the Channel
            if (message.Contains("\u0001"))
            {
                message = message.Substring(message.IndexOf(' ') + 1);
                cWrite(String.Format("{0}: *{1} {2}*", recipient, sender, message.Substring(0, message.Length - 1)));
            }
            else if (message.ToLower().Contains(nick.ToLower() + ":"))
                cWrite(String.Format("{0}: <{1}> {2}", recipient, sender, message));
            else
                cWrite(String.Format("{0}: <{1}> {2}", recipient, sender, message));
        }

        public string[] changeChannel(string channelName)
        {
            string[] errorString = new string[1];
            errorString[0] = String.Format("{0} is not an applicable channel", channelName);
            currentChannel = channelName;
            foreach (Channel thing in channels)
                if (thing.Name == channelName)
                {
                    this.channelUsers(thing.Name, thing.getUsers());
                    return thing.Contents;
                }
            return errorString;
        }

        public void closeIRC()
        {
            try
            {
                write("!QUIT :I was using NaN0 IRC.");
            }
            catch (ObjectDisposedException) { }
            quit = true;
        }

        // Private message to user
        private void privateMsg(string sender, string message)
        {
            cWrite(String.Format("{0}: <{0}> {1}",sender, message));
        }

        private bool serverMessage(string[] parts, string input)
        {
            if (parts[0].IndexOf(".") != -1)
            {
                if (parts[0].IndexOf(this.server.Substring(this.server.IndexOf(".") + 1)) != -1)
                {
                    string sender;
                    if (parts[0].IndexOf('!') != -1)
                        sender = parts[0].Substring(0, input.IndexOf("!"));
                    else
                        sender = parts[0];
                    string recipient = parts[2];
                    string message = input.Substring(input.IndexOf(":") + 1);
                    // Server message
                    switch (parts[1])
                    {
                        case "332": topic(parts); break;
                        case "333": topicOwner(parts); break;
                        case "353": listNames(parts, input); break;
                        case "366": /*This is the end of the names list*/ break;
                        case "372": /*This is the start of the MOTD*/ break;
                        case "376": /* End of MOTD*/ join(channel); break;
                        case "433": /* Nickname is already in use restart();*/ break;
                        case "PONG": /*Response to us PINGing the server, but all we care about is a response of any kind*/break;
                        case "NOTICE":
                            cWrite(String.Format("{0}: {1}", sender, message));
                            break;
                        case "MODE": //:ChanServ!judd@maxgaming.net MODE #plt2 -o DarkSentinel2
                            message = message.Substring(message.IndexOf(' ') + 1); //MODE #plt2 -o DarkSentinel2
                            message = message.Substring(message.IndexOf(' ') + 1); //#plt2 -o DarkSentinel2
                            cWrite(String.Format("{0}: {1} sets MODE {2}", message.Substring(0,message.IndexOf(' ')),
                                sender, message.Substring(message.IndexOf(' ') + 1)));
                            break;
                        /*:mgo2.maxgaming.net 366 DarkSentinel #plt1 :End of /NAMES list.
                        :mgo2.maxgaming.net 311 DarkSentinel Hannah ~obsidi 203-211-111-43.ue.woosh.co.nz * :Hannah
                        :mgo2.maxgaming.net 319 DarkSentinel Hannah :#plt1 
                        :mgo2.maxgaming.net 312 DarkSentinel Hannah mgo2.maxgaming.net :MGOirc Server
                        :mgo2.maxgaming.net 317 DarkSentinel Hannah 897 1270866086 :seconds idle, signon time
                        :mgo2.maxgaming.net 318 DarkSentinel Hannah :End of /WHOIS list.*/
                        default:
                            break;
                    }
                    return true;
                }
            }
            return false;
        }

        #region SimpleBotCommands
        // Prints the topic (from server)
        private void topic(string[] parts)
        {
            string topic = "";
            string thisChannel = parts[3];
            if (parts[4].Substring(0, 1) == ":")
                parts[4] = parts[4].Substring(1);
            for (int i = 4; i < parts.Length; i++)
                topic += parts[i] + " ";
            topic = topic.Trim();
            cWrite(String.Format("{0}: Topic is \"{1}\"", thisChannel, topic));
            this.topicChanged(String.Format("{0}: Topic is \"{1}\"", thisChannel, topic));
            foreach (Channel thing in channels)
                if (thing.Name == thisChannel)
                    thing.Topic = topic;
        }

        // User changes topic
        private void changeTopic(string[] parts)
        {   // Parts will be of the form Squirrel!~Squirrel@203-211-97-51.ue.woosh.co.nz TOPIC #plt2 :New topic. Yay Yay Yay
            string topic = "";
            string topicChanger = parts[0].Substring(0, parts[0].IndexOf('!'));
            for (int i = 3; i < parts.Length; i++)
                topic += parts[i] + " ";
            if (topic.Substring(0, 1) == ":")
                topic = topic.Substring(1);
            string thisChannel = parts[2];
            topic = topic.Trim();
            cWrite(String.Format("{0}: {1} Changes topic to \"{2}\"", thisChannel, topicChanger, topic));
            string time = DateTime.Now.ToString("HH:mm dd MMM yyyy (dddd)");
            this.topicChanged(String.Format("{0}: Topic is \"{1}\" set by {2}, {3}", thisChannel, topic,
                topicChanger, time));
            foreach (Channel thing in channels)
                if (thing.Name == thisChannel)
                    thing.Topic = topic;
        }

        // Prints the topic owner (from server)
        private void topicOwner(string[] parts)
        {
            DateTime topicTime = new DateTime(1970, 1, 1, 0, 0, 0);
            topicTime = topicTime.AddSeconds(double.Parse(parts[5]));
            string time = topicTime.ToString("HH:mm dd MMM yyyy (dddd)");
            cWrite(String.Format("{0}: Topic set by {1}, {2} (Server Time)", parts[3],
                parts[4].Split('!')[0], time));
        }

        // Lists names of people in channel (from server)
        private void listNames(string[] parts, string input)
        {
            string channelNames = input.Substring(input.IndexOf(":", 1) + 1);
            string thisChannel = parts[4];
            cWrite(String.Format("{0}: Users: {1}", thisChannel, channelNames));
            string[] names = new string[channelNames.Split(' ').Length];
            names = channelNames.Split(' ');
            foreach (Channel thing in channels)
                if (thing.Name == thisChannel)
                {
                    foreach (string userName in names)
                        thing.addUser(userName);
                    this.channelUsers(thing.Name, thing.getUsers()); // Event that changes the GUI TextBox
                }
        }

        // Responds to PING with a PONG
        private void pong(string[] ping)
        {
            string pong = "";
            for (int i = 1; i < ping.Length; i++)
            {
                pong += ping[i] + " ";
            }
            write("PONG " + pong);
            if (!hidePing)
                cWrite("PONG " + pong);
        }

        // Authenticate to server on connect
        private void authenticate()
        {
            write(String.Format("USER {0} 0 * :{1}", nick, name));
            write("NICK " + nick);
        }

        // Join a channel and adds it to the Channels list
        private void join(String joinChannel)
        {
            write("JOIN " + joinChannel);
            if (channelNames.Contains(joinChannel.ToLower()))
                channelNames.Remove(joinChannel.ToLower());
            channelNames.Add(joinChannel.ToLower());
        }
        #endregion SimpleBotCommands

        // Messes with the server's input
        private string parseInput(string input)
        {
            if (input.Substring(0, 1) == ":")
                input = input.Substring(1);
            if (getResponse)
                cWrite("!" + input);
            return input;
        }

        #region constructor
        public Irc(string ircServer, int ircPort, string ircChannel, string ircNick, string ircName)
        {
            this.server = ircServer;
            this.port = ircPort;
            this.channel = ircChannel;
            this.currentChannel = ircChannel;
            this.nick = ircNick;
            this.name = ircName;
            this.boss = new List<string>();
            this.channelNames = new List<string>();
            this.channels = new List<Channel>();
            this.lastPing = DateTime.Now;
        }
        #endregion
    }
}
