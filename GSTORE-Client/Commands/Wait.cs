using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace GSTORE_Client.Commands
{
    class Wait : Command
    {
        public Wait()
        {
            Description = "Wait <time>";
            Rule = new Regex(
                @"Wait (?<time>\d+).*",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public override void Exec(string input)
        {
            Match match = Rule.Match(input);

            if (match.Success) {
                int time = int.Parse(match.Groups["time"].Value);
                Thread.Sleep(time);
            } else
                Console.WriteLine(">>> Failed to parse command Wait");
        }
    }
}
