using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ProbablyFair;
using NDesk.Options;
using System.Runtime.InteropServices;

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
                audit_result = Audit(gen);
            else
                audit_result = Audit(gen, checks);

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

        public static bool Audit(RandomGenerator gen, ulong[] nums)
        {
            List<LogEntry> relevant_log = new List<LogEntry>();

            for (int i = 0; i < gen.Log.Count; i++)
            {
                var entry = gen.Log[i];

                if (nums.Contains(entry.Index))
                    relevant_log.Add(entry);
            }

            return Audit(gen, relevant_log);
        }

        public static bool Audit(RandomGenerator gen, List<LogEntry> logs = null)
        {
            if (logs == null)
                logs = gen.Log;

            ulong counter = gen.Counter;
            bool success = true;
            ulong verified = 0;
            ulong failed = 0;

            foreach (var entry in logs)
            {
                Console.Write("{0}: ", entry);
                gen.Counter = entry.Index;
                var bytes = gen.GetRawBytes(8);

                if (bytes.SequenceEqual(entry.RawResult))
                {
                    verified++;

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("verified");
                    Console.ForegroundColor = ConsoleColor.Gray;
                }
                else
                {
                    failed++;

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("mismatch");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine("({0})", bytes.ToShortString());
                    success = false;
                }
            }

            Console.WriteLine("{0} record(s), {1} valid/{2} invalid", logs.Count, verified, failed);

            gen.Counter = counter;

            return success;
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
