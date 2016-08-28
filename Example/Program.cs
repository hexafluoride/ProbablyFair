using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ProbablyFair;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            var gen = GeneratorManager.Create();

            while (true)
            {
                int ones = 0;
                int zeroes = 0;

                for (int i = 0; i < 100; i++)
                {
                    var num = gen.GetInteger(2);

                    if (num == 0)
                        zeroes++;
                    else
                        ones++;
                }

                Console.WriteLine("{0}/{1}", zeroes, ones); // basic uniformity test

                Console.ReadKey();
            }
        }
    }
}
