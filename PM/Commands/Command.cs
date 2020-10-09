using System;
using System.Text.RegularExpressions;


namespace PM.Commands
{
    public abstract class Command
    {

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
