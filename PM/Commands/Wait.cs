using System;
using System.Text.RegularExpressions;
using System.Threading;

namespace PM.Commands
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

            } catch (Exception)
            {
                Console.WriteLine("Failed to parse command Wait");
            }
        }
    }
}
