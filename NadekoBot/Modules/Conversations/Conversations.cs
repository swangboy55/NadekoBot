﻿using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.Classes.Conversations.Commands;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Conversations.Commands;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Conversations
{
    internal class Conversations : DiscordModule
    {
        private const string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations()
        {
            commands.Add(new CopyCommand(this));
            commands.Add(new RipCommand(this));
        }

        public override string Prefix { get; } = String.Format(NadekoBot.Config.CommandPrefixes.Conversations, NadekoBot.Creds.BotId);

        public override void Install(ModuleManager manager)
        {
            var rng = new Random();

            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                cgb.CreateCommand("..")
                    .Description("Adds a new quote with the specified name (single word) and message (no limit).\n**Usage**: .. abc My message")
                    .Parameter("keyword", ParameterType.Required)
                    .Parameter("text", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var text = e.GetArg("text");
                        if (string.IsNullOrWhiteSpace(text))
                            return;
                        await Task.Run(() =>
                            Classes.DbHandler.Instance.InsertData(new DataModels.UserQuote()
                            {
                                DateAdded = DateTime.Now,
                                Keyword = e.GetArg("keyword").ToLowerInvariant(),
                                Text = text,
                                UserName = e.User.Name,
                            })).ConfigureAwait(false);

                        await e.Channel.SendMessage("`New quote added.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("...")
                    .Description("Shows a random quote with a specified name.\n**Usage**: .. abc")
                    .Parameter("keyword", ParameterType.Required)
                    .Do(async e =>
                    {
                        var keyword = e.GetArg("keyword")?.ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(keyword))
                            return;

                        var quote =
                            Classes.DbHandler.Instance.GetRandom<DataModels.UserQuote>(
                                uqm => uqm.Keyword == keyword);

                        if (quote != null)
                            await e.Channel.SendMessage($"📣 {quote.Text}").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("💢`No quote found.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("..qdel")
                    .Alias("..quotedelete")
                    .Description("Deletes all quotes with the specified keyword. You have to either be bot owner or the creator of the quote to delete it.\n**Usage**: `..qdel abc`")
                    .Parameter("quote", ParameterType.Required)
                    .Do(async e =>
                    {
                        var text = e.GetArg("quote")?.Trim();
                        if (string.IsNullOrWhiteSpace(text))
                            return;
                        await Task.Run(() =>
                        {
                            if (NadekoBot.IsOwner(e.User.Id))
                                Classes.DbHandler.Instance.DeleteWhere<UserQuote>(uq => uq.Keyword == text);
                            else
                                Classes.DbHandler.Instance.DeleteWhere<UserQuote>(uq => uq.Keyword == text && uq.UserName == e.User.Name);
                        }).ConfigureAwait(false);

                        await e.Channel.SendMessage("`Done.`").ConfigureAwait(false);
                    });
            });

            manager.CreateCommands(NadekoBot.BotMention, cgb =>
            {
                var client = manager.Client;

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("uptime")
                    .Description("Shows how long Nadeko has been running for.")
                    .Do(async e =>
                    {
                        var time = (DateTime.Now - Process.GetCurrentProcess().StartTime);
                        var str = string.Format("I have been running for {0} days, {1} hours, and {2} minutes.", time.Days, time.Hours, time.Minutes);
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });

                cgb.CreateCommand("die")
                    .Description("Works only for the owner. Shuts the bot down.")
                    .Do(async e =>
                    {
                        if (NadekoBot.IsOwner(e.User.Id))
                        {
                            await e.Channel.SendMessage(e.User.Mention + ", Yes, my love.").ConfigureAwait(false);
                            await Task.Delay(5000).ConfigureAwait(false);
                            Environment.Exit(0);
                        }
                        else
                            await e.Channel.SendMessage(e.User.Mention + ", No.").ConfigureAwait(false);
                    });

                var randServerSw = new Stopwatch();
                randServerSw.Start();

                cgb.CreateCommand("do you love me")
                    .Description("Replies with positive answer only to the bot owner.")
                    .Do(async e =>
                    {
                        if (NadekoBot.IsOwner(e.User.Id))
                            await e.Channel.SendMessage(e.User.Mention + ", Of course I do, my Master.").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage(e.User.Mention + ", Don't be silly.").ConfigureAwait(false);
                    });

                cgb.CreateCommand("how are you")
                    .Alias("how are you?")
                    .Description("Replies positive only if bot owner is online.")
                    .Do(async e =>
                    {
                        if (NadekoBot.IsOwner(e.User.Id))
                        {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as you are here.").ConfigureAwait(false);
                            return;
                        }
                        var kw = e.Server.GetUser(NadekoBot.Creds.OwnerIds[0]);
                        if (kw != null && kw.Status == UserStatus.Online)
                        {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as " + kw.Mention + " is with me.").ConfigureAwait(false);
                        }
                        else
                        {
                            await e.Channel.SendMessage(e.User.Mention + " I am sad. My Master is not with me.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("insult")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Insults @X person.\n**Usage**: @NadekoBot insult @X.")
                    .Do(async e =>
                    {
                        var u = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (u == null)
                        {
                            await e.Channel.SendMessage("Invalid user specified.").ConfigureAwait(false);
                            return;
                        }

                        if (NadekoBot.IsOwner(u.Id))
                        {
                            await e.Channel.SendMessage("I would never insult my master <3").ConfigureAwait(false);
                            return;
                        }
                        if (NadekoBot.HasOwnerName(u.Id, e.Channel))
                        {
                            await e.Channel.SendMessage("Don't steal my master's name, thief!").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(u.Mention + NadekoBot.Locale.Insults[rng.Next(0, NadekoBot.Locale.Insults.Length)]).ConfigureAwait(false);
                    });

                cgb.CreateCommand("fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire.\n**Usage**: @NadekoBot fire [x]")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e =>
                    {
                        var count = 1;
                        int.TryParse(e.Args[0], out count);
                        if (count == 0)
                            count = 1;
                        if (count < 1 || count > 12)
                        {
                            await e.Channel.SendMessage("Number must be between 0 and 12").ConfigureAwait(false);
                            return;
                        }

                        var str = "";
                        for (var i = 0; i < count; i++)
                        {
                            str += firestr;
                        }
                        await e.Channel.SendMessage(str).ConfigureAwait(false);
                    });

                cgb.CreateCommand("slm")
                    .Description("Shows the message where you were last mentioned in this channel (checks last 10k messages)")
                    .Do(async e =>
                    {

                        Message msg = null;
                        var msgs = (await e.Channel.DownloadMessages(100).ConfigureAwait(false))
                        .Where(m => m.MentionedUsers.Contains(e.User))
                        .OrderByDescending(m => m.Timestamp);
                        if (msgs.Any())
                            msg = msgs.First();
                        else
                        {
                            var attempt = 0;
                            Message lastMessage = null;
                            while (msg == null && attempt++ < 5)
                            {
                                var msgsarr = await e.Channel.DownloadMessages(100, lastMessage?.Id).ConfigureAwait(false);
                                msg = msgsarr
                            .Where(m => m.MentionedUsers.Contains(e.User))
                            .OrderByDescending(m => m.Timestamp)
                            .FirstOrDefault();
                                lastMessage = msgsarr.OrderBy(m => m.Timestamp).First();
                            }
                        }
                        if (msg != null)
                            await e.Channel.SendMessage($"Last message mentioning you was at {msg.Timestamp}\n**Message from {msg.User.Name}:** {msg.RawText}")
                                .ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("I can't find a message mentioning you.").ConfigureAwait(false);
                    });

                cgb.CreateCommand("dump")
                    .Description("Dumps all of the invites it can to dump.txt.** Owner Only.**")
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        var i = 0;
                        var j = 0;
                        var invites = "";
                        foreach (var s in client.Servers)
                        {
                            try
                            {
                                var invite = await s.CreateInvite(0).ConfigureAwait(false);
                                invites += invite.Url + "\n";
                                i++;
                            }
                            catch
                            {
                                j++;
                                continue;
                            }
                        }
                        File.WriteAllText("dump.txt", invites);
                        await e.Channel.SendMessage($"Got invites for {i} servers and failed to get invites for {j} servers")
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand("ab")
                    .Description("Try to get 'abalabahaha'")
                    .Do(async e =>
                    {
                        string[] strings = { "ba", "la", "ha" };
                        var construct = "@a";
                        var cnt = rng.Next(4, 7);
                        while (cnt-- > 0)
                        {
                            construct += strings[rng.Next(0, strings.Length)];
                        }
                        await e.Channel.SendMessage(construct).ConfigureAwait(false);
                    });

                cgb.CreateCommand("av")
                    .Alias("avatar")
                    .Parameter("mention", ParameterType.Required)
                    .Description("Shows a mentioned person's avatar.\n**Usage**: ~av @X")
                    .Do(async e =>
                    {
                        var usr = e.Channel.FindUsers(e.GetArg("mention")).FirstOrDefault();
                        if (usr == null)
                        {
                            await e.Channel.SendMessage("Invalid user specified.").ConfigureAwait(false);
                            return;
                        }
                        await e.Channel.SendMessage(await usr.AvatarUrl.ShortenUrl()).ConfigureAwait(false);
                    });

            });
        }



        private static Func<CommandEventArgs, Task> SayYes()
            => async e => await e.Channel.SendMessage("Yes. :)").ConfigureAwait(false);
    }
}
