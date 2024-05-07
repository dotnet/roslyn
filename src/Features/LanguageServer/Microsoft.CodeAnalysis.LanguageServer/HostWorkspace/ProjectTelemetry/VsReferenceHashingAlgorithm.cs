// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;

/// <summary>
/// Defines a reference hashing algorithm for reference names in telemetry
/// This is the same algorithm used for O# and VS, so must not change.
/// See https://github.com/OmniSharp/omnisharp-roslyn/blob/master/src/OmniSharp.MSBuild/VsReferenceHashingAlgorithm.cs
/// </summary>
internal static class VsReferenceHashingAlgorithm
{
    private static readonly ulong[] s_hashText =
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/70711 tracks removing this suppression.
    [
        0x0000000000000000, 0x0809e8a2969451e9, 0x1013d1452d28a3d2, 0x181a39e7bbbcf23b,
        0x2027a28a5a5147a4, 0x282e4a28ccc5164d, 0x303473cf7779e476, 0x383d9b6de1edb59f,
        0x404f4514b4a28f48, 0x4846adb62236dea1, 0x505c9451998a2c9a, 0x58557cf30f1e7d73,
        0x6068e79eeef3c8ec, 0x68610f3c78679905, 0x707b36dbc3db6b3e, 0x7872de79554f3ad7,
        0x809e8a2969451e90, 0x8897628bffd14f79, 0x908d5b6c446dbd42, 0x9884b3ced2f9ecab,
        0xa0b928a333145934, 0xa8b0c001a58008dd, 0xb0aaf9e61e3cfae6, 0xb8a3114488a8ab0f,
        0xc0d1cf3ddde791d8, 0xc8d8279f4b73c031, 0xd0c21e78f0cf320a, 0xd8cbf6da665b63e3,
        0xe0f66db787b6d67c, 0xe8ff851511228795, 0xf0e5bcf2aa9e75ae, 0xf8ec54503c0a2447,
        0x24b1909974c84e69, 0x2cb8783be25c1f80, 0x34a241dc59e0edbb, 0x3caba97ecf74bc52,
        0x049632132e9909cd, 0x0c9fdab1b80d5824, 0x1485e35603b1aa1f, 0x1c8c0bf49525fbf6,
        0x64fed58dc06ac121, 0x6cf73d2f56fe90c8, 0x74ed04c8ed4262f3, 0x7ce4ec6a7bd6331a,
        0x44d977079a3b8685, 0x4cd09fa50cafd76c, 0x54caa642b7132557, 0x5cc34ee0218774be,
        0xa42f1ab01d8d50f9, 0xac26f2128b190110, 0xb43ccbf530a5f32b, 0xbc352357a631a2c2,
        0x8408b83a47dc175d, 0x8c015098d14846b4, 0x941b697f6af4b48f, 0x9c1281ddfc60e566,
        0xe4605fa4a92fdfb1, 0xec69b7063fbb8e58, 0xf4738ee184077c63, 0xfc7a664312932d8a,
        0xc447fd2ef37e9815, 0xcc4e158c65eac9fc, 0xd4542c6bde563bc7, 0xdc5dc4c948c26a2e,
        0x49632132e9909cd2, 0x416ac9907f04cd3b, 0x5970f077c4b83f00, 0x517918d5522c6ee9,
        0x694483b8b3c1db76, 0x614d6b1a25558a9f, 0x795752fd9ee978a4, 0x715eba5f087d294d,
        0x092c64265d32139a, 0x01258c84cba64273, 0x193fb563701ab048, 0x11365dc1e68ee1a1,
        0x290bc6ac0763543e, 0x21022e0e91f705d7, 0x391817e92a4bf7ec, 0x3111ff4bbcdfa605,
        0xc9fdab1b80d58242, 0xc1f443b91641d3ab, 0xd9ee7a5eadfd2190, 0xd1e792fc3b697079,
        0xe9da0991da84c5e6, 0xe1d3e1334c10940f, 0xf9c9d8d4f7ac6634, 0xf1c03076613837dd,
        0x89b2ee0f34770d0a, 0x81bb06ada2e35ce3, 0x99a13f4a195faed8, 0x91a8d7e88fcbff31,
        0xa9954c856e264aae, 0xa19ca427f8b21b47, 0xb9869dc0430ee97c, 0xb18f7562d59ab895,
        0x6dd2b1ab9d58d2bb, 0x65db59090bcc8352, 0x7dc160eeb0707169, 0x75c8884c26e42080,
        0x4df51321c709951f, 0x45fcfb83519dc4f6, 0x5de6c264ea2136cd, 0x55ef2ac67cb56724,
        0x2d9df4bf29fa5df3, 0x25941c1dbf6e0c1a, 0x3d8e25fa04d2fe21, 0x3587cd589246afc8,
        0x0dba563573ab1a57, 0x05b3be97e53f4bbe, 0x1da987705e83b985, 0x15a06fd2c817e86c,
        0xed4c3b82f41dcc2b, 0xe545d32062899dc2, 0xfd5feac7d9356ff9, 0xf55602654fa13e10,
        0xcd6b9908ae4c8b8f, 0xc56271aa38d8da66, 0xdd78484d8364285d, 0xd571a0ef15f079b4,
        0xad037e9640bf4363, 0xa50a9634d62b128a, 0xbd10afd36d97e0b1, 0xb5194771fb03b158,
        0x8d24dc1c1aee04c7, 0x852d34be8c7a552e, 0x9d370d5937c6a715, 0x953ee5fba152f6fc,
        0x92c64265d32139a4, 0x9acfaac745b5684d, 0x82d59320fe099a76, 0x8adc7b82689dcb9f,
        0xb2e1e0ef89707e00, 0xbae8084d1fe42fe9, 0xa2f231aaa458ddd2, 0xaafbd90832cc8c3b,
        0xd28907716783b6ec, 0xda80efd3f117e705, 0xc29ad6344aab153e, 0xca933e96dc3f44d7,
        0xf2aea5fb3dd2f148, 0xfaa74d59ab46a0a1, 0xe2bd74be10fa529a, 0xeab49c1c866e0373,
        0x1258c84cba642734, 0x1a5120ee2cf076dd, 0x024b1909974c84e6, 0x0a42f1ab01d8d50f,
        0x327f6ac6e0356090, 0x3a76826476a13179, 0x226cbb83cd1dc342, 0x2a6553215b8992ab,
        0x52178d580ec6a87c, 0x5a1e65fa9852f995, 0x42045c1d23ee0bae, 0x4a0db4bfb57a5a47,
        0x72302fd25497efd8, 0x7a39c770c203be31, 0x6223fe9779bf4c0a, 0x6a2a1635ef2b1de3,
        0xb677d2fca7e977cd, 0xbe7e3a5e317d2624, 0xa66403b98ac1d41f, 0xae6deb1b1c5585f6,
        0x96507076fdb83069, 0x9e5998d46b2c6180, 0x8643a133d09093bb, 0x8e4a49914604c252,
        0xf63897e8134bf885, 0xfe317f4a85dfa96c, 0xe62b46ad3e635b57, 0xee22ae0fa8f70abe,
        0xd61f3562491abf21, 0xde16ddc0df8eeec8, 0xc60ce42764321cf3, 0xce050c85f2a64d1a,
        0x36e958d5ceac695d, 0x3ee0b077583838b4, 0x26fa8990e384ca8f, 0x2ef3613275109b66,
        0x16cefa5f94fd2ef9, 0x1ec712fd02697f10, 0x06dd2b1ab9d58d2b, 0x0ed4c3b82f41dcc2,
        0x76a61dc17a0ee615, 0x7eaff563ec9ab7fc, 0x66b5cc84572645c7, 0x6ebc2426c1b2142e,
        0x5681bf4b205fa1b1, 0x5e8857e9b6cbf058, 0x46926e0e0d770263, 0x4e9b86ac9be3538a,
        0xdba563573ab1a576, 0xd3ac8bf5ac25f49f, 0xcbb6b212179906a4, 0xc3bf5ab0810d574d,
        0xfb82c1dd60e0e2d2, 0xf38b297ff674b33b, 0xeb9110984dc84100, 0xe398f83adb5c10e9,
        0x9bea26438e132a3e, 0x93e3cee118877bd7, 0x8bf9f706a33b89ec, 0x83f01fa435afd805,
        0xbbcd84c9d4426d9a, 0xb3c46c6b42d63c73, 0xabde558cf96ace48, 0xa3d7bd2e6ffe9fa1,
        0x5b3be97e53f4bbe6, 0x533201dcc560ea0f, 0x4b28383b7edc1834, 0x4321d099e84849dd,
        0x7b1c4bf409a5fc42, 0x7315a3569f31adab, 0x6b0f9ab1248d5f90, 0x63067213b2190e79,
        0x1b74ac6ae75634ae, 0x137d44c871c26547, 0x0b677d2fca7e977c, 0x036e958d5ceac695,
        0x3b530ee0bd07730a, 0x335ae6422b9322e3, 0x2b40dfa5902fd0d8, 0x2349370706bb8131,
        0xff14f3ce4e79eb1f, 0xf71d1b6cd8edbaf6, 0xef07228b635148cd, 0xe70eca29f5c51924,
        0xdf3351441428acbb, 0xd73ab9e682bcfd52, 0xcf20800139000f69, 0xc72968a3af945e80,
        0xbf5bb6dafadb6457, 0xb7525e786c4f35be, 0xaf48679fd7f3c785, 0xa7418f3d4167966c,
        0x9f7c1450a08a23f3, 0x9775fcf2361e721a, 0x8f6fc5158da28021, 0x87662db71b36d1c8,
        0x7f8a79e7273cf58f, 0x77839145b1a8a466, 0x6f99a8a20a14565d, 0x679040009c8007b4,
        0x5faddb6d7d6db22b, 0x57a433cfebf9e3c2, 0x4fbe0a28504511f9, 0x47b7e28ac6d14010,
        0x3fc53cf3939e7ac7, 0x37ccd451050a2b2e, 0x2fd6edb6beb6d915, 0x27df0514282288fc,
        0x1fe29e79c9cf3d63, 0x17eb76db5f5b6c8a, 0x0ff14f3ce4e79eb1, 0x07f8a79e7273cf58,
    ];
#pragma warning restore format

    /// <summary>
    /// The format in which the input stream should be read.abstract Ulong bitmask
    /// </summary>
    private const ulong Mask = 0xffffffffffffffffUL;

    /// <summary>
    /// Helper method to encode an input string in Unicode format before feeding through masking into hash function
    /// </summary>
    /// <param name="cleartext">The string to be encoded</param>
    /// <returns>An array of byte in unicode format (wchars)</returns>
    private static byte[] EncodeBytes(string cleartext)
    {
        return Encoding.Unicode.GetBytes(cleartext);
    }

    /// <summary>
    /// Calculates the hashed value
    /// </summary>
    /// <param name="inputstream">Array of byte that represents the value that needs hashing</param>
    /// <param name="crc">The format in which the input stream should be read: 0xffffffffffffffffUL</param>
    /// <returns>A numerical value that is the hash</returns>
    private static ulong ComputeHash(byte[] inputstream, ulong crc)
    {
        for (var j = 0; j < inputstream.Length; j++)
        {
            crc = s_hashText[(byte)(crc ^ inputstream[j])] ^ (crc >> 8);
        }

        return crc;

    }

    /// <summary>
    /// Calls the hashing function on the input
    /// </summary>
    /// <param name="cleartext">The value that should be hashed</param>
    /// <returns>A string representation of the hash value</returns>
    public static string HashInput(string cleartext)
    {
        var hashedBytes = ComputeHash(EncodeBytes(cleartext), Mask);

        return hashedBytes.ToString("x");

    }
}
