using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;

namespace ProbablyFair
{
    public class GeneratorManager
    {
        static RNGCryptoServiceProvider Seeder = new RNGCryptoServiceProvider();

        public static RandomGenerator Create()
        {
            // TODO: Make this not shit

            byte[] key = new byte[32];
            Seeder.GetBytes(key);

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Key = key;

            var transform = aes.CreateEncryptor();

            RandomGenerator gen = new RandomGenerator(key, transform);
            return gen;
        }
    }
}
