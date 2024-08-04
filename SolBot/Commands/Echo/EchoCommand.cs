using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolBot.Commands.Echo
{
    public class EchoCommand: ModuleBase<SocketCommandContext>
    {
        [Command("echo")]
        [Summary("Echoes back")]
        public async Task ExecuteAsync([Remainder][Summary("A phrase")] string phrase)
        {
            if (string.IsNullOrEmpty(phrase))
            {
                await ReplyAsync(message: "&echo <phrase>");
                return;
            }

            await ReplyAsync(message: phrase);

        }

    }
}
