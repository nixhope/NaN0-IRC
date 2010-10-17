using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

// NaN0 IRC v0.3.0a
// Houses the GUI and all associated framework

namespace NaN0IRC
{
    public partial class NaN0IRC : Form
    {
        delegate void SetTextCallback(String[] text);
        delegate void ChangeTopicCallback(String stuff);
        delegate void Restart();
        delegate void ChannelUsers(string thisChannel, string[] users);
        delegate void ChannelAdd(string channelName);

        public const int CHATLINES = 200;
        string[] content = new string[CHATLINES];
        List<string> channelList;
        List<Button> channelButtons;
        string currentChannel;
        Irc irc;
        int currentLine;
        string[] previousLines;
        string server;
        int port;
        string channel;
        string nick;
        string name;
        Thread t1;
        int restartCounter = 0;
        DateTime lastRestart;
        Size originalSize;

        public NaN0IRC()
        {
            InitializeComponent();
            previousLines = new string[10];
            for (int i = 0; i < previousLines.Length; i++)
                previousLines[i] = " ";
            currentLine = previousLines.Length-1;
            lastRestart = DateTime.Now;
            originalSize = this.ClientSize;
        }

        public void irc_stuffHappened(string stuff) // This is for a single-line change
        {
            /*
            for (int i = 0; i < content.Length-1; i++)
                content[i] = content[i+1];
            content[content.Length-1] = stuff;
            if (this.richTextBoxChat.InvokeRequired)
            {
                try
                {
                    // It's on a different thread, so use Invoke.
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke
                        (d, new object[] { content });
                }
                catch (ObjectDisposedException)
                { // AFAIK this only happens if we close the form, anyway. No need to handle
                }
            }
            else
            {
                // It's on the same thread, no need for Invoke
                SetText(content);
            } */
            irc_stuffHappened(irc.changeChannel(currentChannel));
        }

        public void irc_stuffHappened(string[] stuff)
        {
            for (int i = 0; i < content.Length & i < stuff.Length; i++)
                //content[i] = content[i + 1];
                content[content.Length - 1 - i] = stuff[stuff.Length - 1 - i];
            if (this.richTextBoxChat.InvokeRequired)
            {
                try
                {
                    // It's on a different thread, so use Invoke.
                    SetTextCallback d = new SetTextCallback(SetText);
                    this.Invoke
                        (d, new object[] { content });
                }
                catch (ObjectDisposedException)
                { /* AFAIK this only happens if we close the form, anyway. No need to handle*/ }
            }
            else
            {
                // It's on the same thread, no need for Invoke
                SetText(content);
            }
        }

        public void topic(string stuff)
        {
            this.labelTopic.Text = stuff;
        }

        public void populateUsers(string thisChannel, string[] users)
        {
            try
            {
                string[] userListText = new string[users.Length + 1];
                userListText[0] = String.Format("Users in {0}:", thisChannel);
                for (int i = 0; i < users.Length; i++)
                    userListText[i + 1] = users[i];
                textBoxUsers.Lines = userListText;
            }
            catch (ObjectDisposedException)
            { }
        }

        public void newChannel(string channelName)
        {
            int c = channelButtons.Count;
            int w = c*5;
            foreach (Button thing in channelButtons)
                w += thing.Width;
            channelList.Add(channelName);
            //currentChannel = channelName;
            channelButtons.Add(new Button());
            channelButtons[c].Name = channelName;
            channelButtons[c].Location = new Point(w+20,10);
            channelButtons[c].Text = channelName;
            this.Controls.Add(channelButtons[c]);
            channelButtons[c].Click += new System.EventHandler(this.channelButtonClick);
            this.channelButtonClick(channelButtons[c], MouseEventArgs.Empty);
        }

        private void channelButtonClick(object sender, EventArgs e)
        {
            foreach (Button thing in channelButtons)
                if (sender.Equals(thing))
                {
                    irc_stuffHappened(irc.changeChannel(thing.Text));
                    currentChannel = thing.Text;
                    this.labelTopic.Text = irc.getTopic(currentChannel);
                }
        }

        private void SetText(string[] text)
        {
            try
            {
                this.richTextBoxChat.Lines = text;
                this.richTextBoxChat.SelectionStart = this.richTextBoxChat.TextLength;
                this.richTextBoxChat.ScrollToCaret();
            }
            catch (ObjectDisposedException)
            { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            server = textBoxServer.Text;
            port = int.Parse(textBoxPort.Text);
            channel = textBoxChannel.Text;
            nick = textBoxNick.Text;
            name = "NaN0 "+nick;
            channelList = new List<string>();
            channelButtons = new List<Button>();
            irc = new Irc(server, port, channel, nick, name);
            button1.Visible = false;
            richTextBoxChat.Visible = true;
            textBoxInput.Visible = true;
            textBoxUsers.Visible = true;
            irc.stuffHappened += new StuffHappened(irc_stuffHappened);
            irc.topicChanged += new TopicChanged(irc_topicChanged);
            irc.restart += new global::NaN0IRC.Restart(irc_restart);
            irc.channelUsers += new ListNames(irc_channelUsers);
            irc.joinNewChannel += new NewChannel(irc_joinNewChannel);
            t1 = new Thread(new ThreadStart(irc.connect));
            t1.Start();
            label1.Visible = false;
            label2.Visible = false;
            label3.Visible = false;
            label4.Visible = false;
            textBoxChannel.Visible = false;
            textBoxNick.Visible = false;
            textBoxPort.Visible = false;
            textBoxServer.Visible = false;
            textBoxInput.Focus();
            this.Text += String.Format(" ({0} on {1})", nick, server);
            timer1.Start();
            timer1.Interval = 1000*60*2;
        }

        void irc_joinNewChannel(string channelName)
        {
            if (this.textBoxUsers.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                ChannelAdd d = new ChannelAdd(newChannel);
                this.Invoke
                    (d, new object[] { channelName });
            }
            else
            {
                // It's on the same thread, no need for Invoke
                newChannel(channelName);
            }
        }

        void irc_channelUsers(string thisChannel, string[] channelUsers)
        {
            if (this.textBoxUsers.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                ChannelUsers d = new ChannelUsers(populateUsers);
                this.Invoke
                    (d, new object[] { thisChannel, channelUsers });
            }
            else
            {
                // It's on the same thread, no need for Invoke
                populateUsers(thisChannel, channelUsers);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (sender == timer1)
                irc.ping();
        }

        void irc_restart() //This section still needs some serious development
        {
            try
            {
                irc.write("!QUIT :Attempting to restart connection");
            }
            catch (ObjectDisposedException) { }
            catch (System.IO.IOException) { }
            DateTime now = DateTime.Now;
            if (now.CompareTo(lastRestart.AddMinutes(1)) > 0)
            {
                restartCounter = 0;
                irc_stuffHappened(String.Format("Restart attempt {0}.", restartCounter + 1));
            }
            else
            {
                restartCounter++;
                irc_stuffHappened(String.Format("Restart attempt {0}.", restartCounter + 1));
            }
            lastRestart = now;
            if (restartCounter < 10)
            {
                irc.connect();
            }
            else
            {
                irc_stuffHappened(String.Format("Disconnection process aborted after {0} attempts.",restartCounter));
                button1.Visible = true;
            }
        }

        void irc_topicChanged(string stuff)
        {
            //throw new NotImplementedException();
            if (this.labelTopic.InvokeRequired)
            {
                // It's on a different thread, so use Invoke.
                ChangeTopicCallback d = new ChangeTopicCallback(topic);
                this.Invoke
                    (d, new object[] { stuff });
            }
            else
            {
                // It's on the same thread, no need for Invoke
                this.labelTopic.Text = stuff;
            }
        }

        private void textBoxInput_Enter(object sender, EventArgs e)
        {
            string input = textBoxInput.Text;
            textBoxInput.Text = "";
            if (input.Substring(0, 1) == "/") // Dealing with slash commands
                slashCommands(input);
            else
            { //Need to handle the chat input for the current channel.
                if (currentChannel != "Server")
                    irc.write(String.Format("!PRIVMSG {0} :{1}", currentChannel, input));
                else
                    irc.write("!" + input);
            }
            bool duplicate = false;
            for (int i = 0; i < previousLines.Length - 1; i++)
                if (previousLines[i] == input)
                    duplicate = true;
            if (!duplicate)
            {
                for (int i = 0; i < previousLines.Length - 1; i++)
                    previousLines[i] = previousLines[i + 1];
                previousLines[previousLines.Length - 1] = input;
            }
        }

        private void slashCommands(string input)
        {   // Deals with commands containing a slash /
            input = input.Substring(1); // Removes slash
            string command; // "/COMMAND"
            string arg = ""; // "/COMMAND ARG"
            string message = "";
            if (input.Contains(' '))
            {
                command = input.Substring(0, input.IndexOf(' '));
                input = input.Substring(input.IndexOf(' ') + 1);
                if (input.Contains(' '))
                {
                    arg = input.Substring(0, input.IndexOf(' '));
                    message = input.Substring(input.IndexOf(' ') + 1);
                }
                else
                    arg = input;
            }
            else
                command = input;
            switch (command.ToUpper())
            {
                case "JOIN":
                    if (arg == "")
                        irc.cWrite("Command /JOIN requires an argument, i.e. /JOIN CHANNEL");
                    else
                        irc.write("JOIN " + arg);
                    break;
                case "MSG":
                    if (arg == "")
                        irc.cWrite("Command /MSG requires an argument, i.e. /MSG RECIPIENT");
                    else if (message == "")
                        irc.cWrite("Command /MSG requires a message, i.e. /MSG RECIPIENT MESSAGE");
                    else
                        irc.write(String.Format("!PRIVMSG {0} :{1}", arg, message));
                    break;
                case "PM":
                    if (arg == "")
                        irc.cWrite("Command /PM requires an argument, i.e. /PM RECIPIENT");
                    else if (message == "")
                        irc.cWrite("Command /PM requires a message, i.e. /PM RECIPIENT MESSAGE");
                    else
                        irc.write(String.Format("!PRIVMSG {0} :{1}", arg, message));
                    break;
                case "W":
                    if (arg == "")
                        irc.cWrite("Command /W requires an argument, i.e. /W RECIPIENT");
                    else if (message == "")
                        irc.cWrite("Command /W requires a message, i.e. /W RECIPIENT MESSAGE");
                    else
                        irc.write(String.Format("!PRIVMSG {0} :{1}", arg, message));
                    break;
                case "ME":
                    if (arg == "")
                        irc.cWrite("Command /ME requires an argument, i.e. /ME CHANNEL");
                    else if (message == "")
                        irc.cWrite("Command /ME requires an action, i.e. /ME CHANNEL ACTION");
                    else
                        irc.write(String.Format("!PRIVMSG {0} :\u0001ACTION {1}\u0001", arg, message));
                    break;
                case "EM":
                    if (arg == "")
                        irc.cWrite("Command /EM requires an argument, i.e. /EM CHANNEL");
                    else if (message == "")
                        irc.cWrite("Command /EM requires an action, i.e. /EM CHANNEL ACTION");
                    else
                        irc.write(String.Format("!PRIVMSG {0} :\u0001ACTION {1}\u0001", arg, message));
                    break;
                case "QUIT":
                    if (arg == "")
                        irc.write("QUIT :I was using NaN0 IRC.");
                    else
                        irc.write("QUIT :" + arg + " " + message);
                    break;
                case "LEAVE":
                    if (arg == "")
                        irc.cWrite("Command /LEAVE requires an argument, i.e. /LEAVE CHANNEL");
                    else
                        irc.write("PART " + arg + " :" + message);
                    break;
                case "PART":
                    if (arg == "")
                        irc.cWrite("Command /PART requires an argument, i.e. /PART CHANNEL");
                    else
                        irc.write("PART " + arg + " :" + message);
                    break;
                case "MODE":
                    irc.cWrite("Command /MODE not yet supported due to object structure. WIP.");
                    break;
                case "NICK":
                    if (arg == "")
                        irc.cWrite("Command /NICK requires an argument, i.e. /NICK NEWNICK");
                    else
                        irc.write("NICK " + arg);
                    break;
                case "NS":
                    if (arg == "")
                        irc.cWrite("Command /NS requires an argument, i.e. /NS MESSAGE");
                    else
                        irc.write("!PRIVMSG NICKSERV :" + arg + " " + message);
                    break;
                case "CS":
                    if (arg == "")
                        irc.cWrite("Command /CS requires an argument, i.e. /CS MESSAGE");
                    else
                        irc.write("!PRIVMSG CHANSERV :" + arg + " " + message);
                    break;
                case "TOPIC":
                    if (arg == "")
                        irc.cWrite("Command /TOPIC requires an argument, i.e. /TOPIC #CHANNEL");
                    else if (message == "")
                        irc.cWrite("Command /TOPIC requires a message, i.e. /TOPIC #CHANNEL MESSAGE");
                    else
                        irc.write(String.Format("!TOPIC {0} :{1}", arg, message));
                    break;
                case "":
                    break;
                default:
                    irc.cWrite(String.Format("NaN0IRC Command /{0} not recognised or not currently supported by NaN0 IRC. If you think it should, please contact the author.",
                        command));
                    break;
            }
        }

        private void NaN0IRC_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                irc.closeIRC();
            }
            catch (NullReferenceException)
            {
            }
        }

        private void restoreWindow(object sender, EventArgs e)
        {
            BringToFront();
            Show();
            WindowState = FormWindowState.Normal;
            this.notifyIcon1.Visible = false;
            this.richTextBoxChat.SelectionStart = this.richTextBoxChat.TextLength;
            this.richTextBoxChat.ScrollToCaret();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void NaN0IRC_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == WindowState)
            {
                Hide();
                this.notifyIcon1.Visible = true;
            }
            else
            {
                int x = this.ClientSize.Width - originalSize.Width;
                int y = this.ClientSize.Height - originalSize.Height;

                //this.labelTopic.Location = new System.Drawing.Point(20, 21);
                //this.labelTopic.Size = new System.Drawing.Size(0, 16);
                this.richTextBoxChat.Location = new System.Drawing.Point(20, 62);
                this.richTextBoxChat.Size = new System.Drawing.Size(617 + x, 485 + y);
                this.textBoxUsers.Location = new System.Drawing.Point(636 + x, 62);
                this.textBoxUsers.Size = new System.Drawing.Size(126, 504 + y);
                this.textBoxInput.Location = new System.Drawing.Point(20, 546 + y);
                this.textBoxInput.Size = new System.Drawing.Size(617 + x, 20);
                this.richTextBoxChat.SelectionStart = this.richTextBoxChat.TextLength;
                this.richTextBoxChat.ScrollToCaret();
            }
        }

        private void loginInfo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(sender, e);
        }

        private void textBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBoxInput.Text != "")
            {
                textBoxInput_Enter(sender, e);
                this.richTextBoxChat.SelectionStart = this.richTextBoxChat.TextLength;
                this.richTextBoxChat.ScrollToCaret();
                e.SuppressKeyPress = true;
                currentLine = previousLines.Length-1;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.textBoxInput.Text = "";
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Up) //Still not working properly
            {
                bool scrolling = false;
                for (int i = 0; i < previousLines.Length; i++)
                    if (previousLines[i] == this.textBoxInput.Text)
                        scrolling = true;
                if (currentLine > 0 & scrolling)
                    currentLine--;
                this.textBoxInput.Text = previousLines[currentLine];
                this.textBoxInput.SelectionStart = this.textBoxInput.Text.Length;
            }
            else if (e.KeyCode == Keys.Down)
            {
                bool scrolling = false;
                for (int i = 0; i < previousLines.Length; i++)
                    if (previousLines[i] == this.textBoxInput.Text)
                        scrolling = true;
                if (currentLine == previousLines.Length - 1)
                    this.textBoxInput.Text = "";
                else if (currentLine < previousLines.Length - 1 & scrolling)
                {
                    currentLine++;
                    this.textBoxInput.Text = previousLines[currentLine];
                    this.textBoxInput.SelectionStart = this.textBoxInput.Text.Length;
                }
            }
        }
    }
}
