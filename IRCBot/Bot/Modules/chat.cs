﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Reflection;
using AIMLbot;

namespace Bot.Modules
{
    class chat : Module
    {
        private System.Timers.Timer chat_time = new System.Timers.Timer();
        public bool still_chatting;
        private AIMLbot.Bot myBot;
        private User myUser;
        private List<string> chatting_nick;

        public chat()
        {
            chat_time.AutoReset = false;
            chat_time.Elapsed += stop_chat;
            chat_time.Enabled = false;

            still_chatting = false;
            chatting_nick = new List<string>();
            myBot = new AIMLbot.Bot();
            myBot.loadSettings(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "modules" + Path.DirectorySeparatorChar + "chat" + Path.DirectorySeparatorChar + "Settings.xml");
            myUser = new User("chat_nick", myBot);

            AIMLbot.Utils.AIMLLoader loader = new AIMLbot.Utils.AIMLLoader(myBot);
            myBot.isAcceptingUserInput = false;
            loader.loadAIML(myBot.PathToAIML);
            myBot.isAcceptingUserInput = true;
        }

        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == true)
            {
                foreach (Command tmp_command in this.Commands)
                {
                    bool blocked = tmp_command.Blacklist.Contains(channel) || tmp_command.Blacklist.Contains(nick);
                    bool cmd_found = false;
                    bool spam_check = ircbot.get_spam_check(channel, nick, tmp_command.Spam_Check);
                    if (spam_check == true)
                    {
                        blocked = blocked || ircbot.get_spam_status(channel);
                    }
                    cmd_found = tmp_command.Triggers.Contains(command);
                    if (blocked == true && cmd_found == true)
                    {
                        ircbot.sendData("NOTICE", nick + " :I am currently too busy to process that.");
                    }
                    if (blocked == false && cmd_found == true)
                    {
                        foreach (string trigger in tmp_command.Triggers)
                        {
                            switch (trigger)
                            {
                                case "stopchat":
                                    if (spam_check == true)
                                    {
                                        ircbot.add_spam_count(channel);
                                    }
                                    if (nick_access >= tmp_command.Access)
                                    {
                                        foreach (string chat_nick in chatting_nick)
                                        {
                                            if (chat_nick.Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                still_chatting = false;
                                                chatting_nick.Clear();
                                                chat_time.Enabled = false;
                                                chat_time.Stop();
                                                ircbot.sendData("PRIVMSG", line[2] + " :Ok, I will stop.");
                                                break;
                                            }
                                        }
                                        if (still_chatting == true)
                                        {
                                            ircbot.sendData("PRIVMSG", line[2] + " :You are not currently talking to me.");
                                        }
                                    }
                                    else
                                    {
                                        ircbot.sendData("NOTICE", nick + " :You do not have permission to use that command.");
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            if ((type.Equals("channel") || type.Equals("query")) && bot_command == false)
            {
                chat_time.Interval = Convert.ToInt32(this.Options["chat_timeout"]) * 1000;
                if (line.GetUpperBound(0) >= 3)
                {
                    string msg = "";
                    if (line.GetUpperBound(0) > 3)
                    {
                        msg = line[3].TrimStart(':') + " " + line[4];
                    }
                    else
                    {
                        msg = line[3].TrimStart(':');
                    }
                    string[] words = msg.Split(' ');
                    bool me_in = false;
                    foreach (string word in words)
                    {
                        if (word.Contains(Conf.Nick))
                        {
                            me_in = true;
                            break;
                        }
                    }
                    if (me_in == true || still_chatting == true)
                    {
                        bool nick_found = false;
                        for (int x = 0; x < chatting_nick.Count(); x++)
                        {
                            if (chatting_nick[x].Equals(nick, StringComparison.InvariantCultureIgnoreCase))
                            {
                                nick_found = true;
                            }
                        }
                        if (me_in == true && nick_found == false)
                        {
                            chatting_nick.Add(nick);
                            nick_found = true;
                        }
                        if (nick_found == true)
                        {
                            // Start Chatting
                            still_chatting = false;
                            chat_time.Stop();
                            Request r = new Request(msg, myUser, myBot);
                            Result res = myBot.Chat(r);
                            string owners = "";
                            foreach (string own in Conf.Owners)
                            {
                                owners += Conf.Owners + ",";
                            }
                            owners = owners.TrimEnd(',').Replace(",", " and ");
                            ircbot.sendData("PRIVMSG", line[2] + " :" + res.Output.Replace("[nick]", nick).Replace("[me]", Conf.Nick).Replace("[owner]", owners).Replace("[version]", Assembly.GetExecutingAssembly().GetName().Version.ToString()).Replace("\n", " "));
                            chat_time.Start();
                            still_chatting = true;
                        }
                    }
                }
            }
        }

        private void stop_chat(object sender, EventArgs e)
        {
            still_chatting = false;
            chatting_nick.Clear();
            chat_time.Enabled = false;
            chat_time.Stop();
        }
    }
}
