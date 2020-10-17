using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace GSTORE_Client.Commands
{
    class Exit : Command
    {

        public Exit()
        {
            Description = "Exit";
            Rule = new Regex(
                @"Exit",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Environment.Exit(0);

        }
    }
}
