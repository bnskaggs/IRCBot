﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Modules
{
    class invite : Module
    {
        public override void control(bot ircbot, BotConfig Conf, string[] line, string command, int nick_access, string nick, string channel, bool bot_command, string type)
        {
            if (type.Equals("invite") && line.GetUpperBound(0) >= 3 && String.IsNullOrEmpty(command))
            {
                if (channel.Equals(ircbot.Nick))
                {
                    bool chan_allowed = true;
                    string request_chan = line[3].TrimStart(':');
                    foreach (string chan in ircbot.Conf.Chan_Blacklist.Split(','))
                    {
                        if (chan.Equals(request_chan, StringComparison.InvariantCultureIgnoreCase))
                        {
                            chan_allowed = false;
                            break;
                        }
                    }
                    if (chan_allowed == true)
                    {
                        if (ircbot.get_chan_info(channel) == null)
                        {
                            if (nick_access != Conf.Owner_Level)
                            {
                                foreach (string owner_nick in Conf.Owners)
                                {
                                    ircbot.sendData("NOTICE", owner_nick + " :" + nick + " has invited me to join " + request_chan);
                                    ircbot.sendData("NOTICE", owner_nick + " :If you would like to permanently add this channel, please type " + ircbot.Conf.Command + "addchanlist " + request_chan);
                                }
                            }
                            ircbot.sendData("JOIN", request_chan);
                            ircbot.sendData("PRIVMSG", request_chan + " :" + nick + " has requested me to join this channel.  For more informaiton on what I can do, just type: " + ircbot.Conf.Command + "help");
                        }
                        else
                        {
                            ircbot.sendData("NOTICE", nick + " :I'm already in that channel!");
                        }
                    }
                    else
                    {
                        ircbot.sendData("NOTICE", nick + " :I am not allowed to join that channel.");
                    }
                }
            }
        }
    }
}
