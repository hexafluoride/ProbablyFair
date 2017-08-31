using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ProbablyFair
{
    [Serializable]
    public class RandomGenerator
    {
        public byte[] Seed { get; set; }
        private string CounterLock = string.Empty;
        public ulong Counter { get; set; }

        public List<LogEntry> Log { get; set; }

        public string HashedName
        {
            get
            {
                SHA256CryptoServiceProvider sha = new SHA256CryptoServiceProvider();
                return sha.ComputeHash(Seed).ToUsefulString();
            }
        }

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

        [NonSerialized]
        public ICryptoTransform Transform;

        public RandomGenerator()
        {

        }

        public RandomGenerator(byte[] seed, ICryptoTransform transform)
        {
            Seed = seed;
            Initialize();
            Log = new List<LogEntry>();
        }

        public void Initialize()
        {
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Key = Seed;

            Transform = aes.CreateEncryptor();
        }

        public static RandomGenerator FromFile(string filename)
        {
            if (!File.Exists(filename))
                return null;

            IFormatter formatter = new BinaryFormatter();

            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    var gen = (RandomGenerator)formatter.Deserialize(fs);
                    gen.Initialize();
                    return gen;
                }
            }
            catch
            {
                return null;
            }
        }

        public void Save(string filename)
        {
            lock(CounterLock)
            {
                try
                {
                    IFormatter formatter = new BinaryFormatter();

                    using (FileStream fs = new FileStream(filename + ".tmp", FileMode.OpenOrCreate))
                    {
                        formatter.Serialize(fs, this);
                    }

                    if (File.Exists(filename))
                        File.Delete(filename);

                    File.Move(filename + ".tmp", filename);
                }
                catch
                {
                    throw;
                }
            }
        }

        public bool Audit(List<LogEntry> logs = null)
        {
            if (logs == null)
                logs = Log;

            ulong counter = Counter;
            bool success = true;
            ulong verified = 0;
            ulong failed = 0;

            foreach (var entry in logs)
            {
                Console.Write("{0}: ", entry);
                Counter = entry.Index;
                var bytes = _Generate(8);

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

            Counter = counter;

            return success;
        }

        public bool Audit(ulong[] nums)
        {
            List<LogEntry> relevant_log = new List<LogEntry>();

            for(int i = 0; i < Log.Count; i++)
            {
                var entry = Log[i];

                if (nums.Contains(entry.Index))
                    relevant_log.Add(entry);
            }

            return Audit(relevant_log);
        }

        private byte[] GetNextPlaintext()
        {
            // this makes ECB act like CTR mode
            return new byte[InputBlockSize - 8].Concat(BitConverter.GetBytes(Counter++)).ToArray();
        }

        public int GetInteger(int max, out ulong index, string tag = "")
        {
            return GetInteger(0, max, out index, tag);
        }

        public int GetInteger(int min, int max, out ulong index, string tag = "")
        {
            lock (CounterLock)
            {
                index = Counter;
                int result = _GetInteger(min, max, out byte[] raw);

                LogEntry entry = new LogEntry()
                {
                    Tag = tag,
                    Index = index,
                    Result = result,
                    RawResult = raw,
                    Params = new int[] { min, max },
                    Type = ResultType.Integer
                };
                Log.Add(entry);

                return result;
            }
        }

        public double GetDouble(out ulong index, string tag = "")
        {
            lock (CounterLock)
            {
                index = Counter;
                double result = _GetDouble(out byte[] raw);

                LogEntry entry = new LogEntry()
                {
                    Tag = tag,
                    Index = index,
                    Result = result,
                    RawResult = raw,
                    Type = ResultType.Double
                };
                Log.Add(entry);

                return result;
            }
        }

        public bool GetBoolean(double threshold, out ulong index, string tag = "")
        {
            lock (CounterLock)
            {
                index = Counter;
                bool result = _GetDouble(out byte[] raw) < threshold;

                LogEntry entry = new LogEntry()
                {
                    Tag = tag,
                    Index = index,
                    Result = result,
                    RawResult = raw,
                    Params = new double[] { threshold },
                    Type = ResultType.Boolean
                };
                Log.Add(entry);

                return result;
            }
        }

        public ulong GetRawLong(string tag = "")
        {
            lock (CounterLock)
            {
                ulong i = Counter;
                ulong result = _GetRawLong(out byte[] raw);

                LogEntry entry = new LogEntry()
                {
                    Tag = tag,
                    Index = i,
                    Result = result,
                    RawResult = raw,
                    Type = ResultType.Double
                };
                Log.Add(entry);

                return result;
            }
        }

        private int _GetInteger(int max, out byte[] raw)
        {
            return _GetInteger(0, max, out raw);
        }

        private int _GetInteger(int min, int max, out byte[] raw)
        {
            return min + (int)(_GetDouble(out raw) * (max - min));
        }

        private ulong _GetRawLong(out byte[] raw)
        {
            byte[] buf = _Generate(8);
            raw = buf;
            ulong num = BitConverter.ToUInt64(buf, 0);

            return num;
        }

        private double _GetDouble(out byte[] raw)
        { 
            return (double)_GetRawLong(out raw) / (double)ulong.MaxValue; // tad hacky but eh
        }

        private byte[] _Generate(int length)
        {
            if (length % OutputBlockSize != 0)
                length += (length % OutputBlockSize);

            int blocks = length / OutputBlockSize;
            byte[] output = new byte[length];

            for(int i = 0; i < blocks; i++)
            {
                int read = Transform.TransformBlock(GetNextPlaintext(), 0, Transform.InputBlockSize, output, i * OutputBlockSize);

                if (read != OutputBlockSize)
                    throw new Exception(string.Format("Unexpected short read(expected {0} bytes, read {1}) from ICryptoTransform", read, OutputBlockSize));
            }

            return output;
        }
    }
}
