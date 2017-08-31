using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ProbablyFair;
using NDesk.Options;

namespace Auditor
{
    class Program
    {
        static void Main(string[] args)
        {
            OptionSet set = null;

            bool check_all = true;
            ulong[] checks = new ulong[0];

            set = new OptionSet()
            {
                {"r|refnums=", "If provided, only checks the given refnums. Comma-separated.", r => { checks = r.Split(',').Select(l => ulong.Parse(l)).ToArray(); check_all = false; } },
                {"?|h|help", "Displays this text.", h => ShowHelp(set) }
            };

            string filename = set.Parse(args).LastOrDefault();

            if (string.IsNullOrWhiteSpace(filename))
                ShowHelp(set);

            if(!File.Exists(filename))
            {
                Console.WriteLine("Invalid filename {0}.", filename);
                ShowHelp(set);
            }

            RandomGenerator gen = RandomGenerator.FromFile(filename);

            bool audit_result = false;

            if (check_all)
                audit_result = gen.Audit();
            else
                audit_result = gen.Audit(checks);

            Console.WriteLine();
            Console.WriteLine("Generator hash: {0} (cross-reference with .fair)", gen.HashedName);
            Console.WriteLine("Generator seed: {0}", gen.Seed.ToUsefulString());
            Console.Write("Audit result: ");

            if (audit_result)
                Console.ForegroundColor = ConsoleColor.Green;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("{0}", audit_result ? "success" : "failure");

            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void ShowHelp(OptionSet set)
        {
            string name = System.AppDomain.CurrentDomain.FriendlyName;

            Console.WriteLine("{0} - a tool to audit ProbablyFair logs", name);
            Console.WriteLine("Usage: {0} [-r i,j,k,...] filename", name);
            Console.WriteLine();
            set.WriteOptionDescriptions(Console.Out);

            Environment.Exit(0);
        }
    }
}
