using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProbablyFair
{
    public class RandomGenerator
    {
        public byte[] Seed { get; set; }
        public byte[] IV { get; set; }

        public int InputBlockSize
        {
            get
            {
                return Transform.InputBlockSize;
            }
        }

        public int OutputBlockSize
        {
            get
            {
                return Transform.OutputBlockSize;
            }
        }

        public ICryptoTransform Transform;

        public RandomGenerator()
        {

        }

        public int GetInteger(int max)
        {
            return GetInteger(0, max);
        }

        public int GetInteger(int min, int max)
        {
            return min + (int)(GetDouble() * (max - min));
        }

        public ulong GetRawLong()
        {
            byte[] buf = Generate(8);
            ulong num = BitConverter.ToUInt64(buf, 0);

            return num;
        }

        public double GetDouble()
        {
            return GetRawLong() / ulong.MaxValue; // tad hacky but eh
        }

        public byte[] Generate(int length)
        {
            if (length % OutputBlockSize != 0)
                length += (length % OutputBlockSize);

            int blocks = length / OutputBlockSize;
            byte[] output = new byte[length];

            for(int i = 0; i < blocks; i++)
            {
                int read = Transform.TransformBlock(new byte[Transform.InputBlockSize], 0, Transform.InputBlockSize, output, i * OutputBlockSize);

                if (read != OutputBlockSize)
                    throw new Exception(string.Format("Unexpected short read(expected {0} bytes, read {1}) from ICryptoTransform", read, OutputBlockSize));
            }

            return output;
        }
    }
}
