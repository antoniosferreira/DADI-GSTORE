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
            try
            {
                Match match = Rule.Match(input);
                int time = int.Parse(match.Groups["time"].Value);
                Thread.Sleep(time);

            } catch (Exception e) {
                Console.WriteLine(">>> Failed to execute command:" + input);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
