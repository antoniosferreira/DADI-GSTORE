using System;
using System.Text.RegularExpressions;


namespace GSTORE_Client.Commands
{
    public abstract class Command
    {
        internal Client Client;
        protected string Description;
        protected Regex Rule;

        public void PrintDescription()
        {
            Console.WriteLine(Description);
        }

        public bool Check(string input)
        {
            try
            {
                return Rule.Matches(input).Count > 0;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }

        public abstract void Exec(string input);
    }
}
