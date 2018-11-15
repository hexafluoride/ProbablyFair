# ProbablyFair
High performance, provably fair RNG suited for gambling.

----------------

## Details
The RNG scheme involves AES-256 in CTR mode(in this specific implementation, ECB is used instead, and CTR is emulated). The 256-bit seed is also the key for AES. The RNG works in 32-byte increments, and every time new data is needed, an empty plaintext is encrypted with the seed. All required numbers are generated from these 256 bits(there's no buffering, so the unused bits are simply discarded). The primitive for other data types is the `_GetRawLong` function, which simply takes 64 bits from the block cipher's result, and converts it to an unsigned 64-bit integer. Other data types are created as follows:

* Floating point values: `_GetRawLong() / ulong.MaxValue`
* Ranged integer values: `(_GetDouble() * (max - min)) + min`
* Boolean values: `_GetDouble() < chance`

## How is CTR emulated?
CTR isn't natively supported in .NET, so to emulate it, ECB with an internal counter is used. Every time a new value is to be generated, the internal counter value is converted to 8 bytes, then padded to the block cipher's input block size(32 bytes in this implementation), then the counter value is incremented.

## How are the audit files formatted?
To preserve state across restarts, and to be able to audit results later on, the state of an RNG instance is kept within a single serializable class. This includes the counter variable, the seed and the audit log, which is a `List<LogEntry>`. `LogEntry` is a serializable class which includes data about a generated result, including the time of generation, the counter at the time of generation, the raw result from the block cipher, the converted result, the type of the converted result, the parameters passed to the generator function(e.g. the range of integers), and an application-specific tag that identifies generation purpose. All of this is serialized with the .NET serialization file format, which is implemented in the .NET Framework, Mono and .NET Core. The latter two have their implementations licensed in a free manner, which means you can use their parser implementations without any legal trouble, if you choose to write your own auditor tool from scratch.
