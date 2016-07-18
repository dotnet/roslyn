using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SignRoslyn
{
    internal static partial class SignToolFactory
    {
        /// <summary>
        /// The signing implementation which actually signs binaries.
        /// </summary>
        private sealed class RealSignTool : SignToolBase
        {
            /// <summary>
            /// The number of bytes from the start of the <see cref="CorHeader"/> to its <see cref="CorFlags"/>.
            /// </summary>
            internal const int OffsetFromStartOfCorHeaderToFlags =
                   sizeof(Int32)  // byte count
                 + sizeof(Int16)  // major version
                 + sizeof(Int16)  // minor version
                 + sizeof(Int64); // metadata directory

            internal RealSignTool(string appPath, string binariesPath, string sourcePath)
                : base(appPath, binariesPath, sourcePath)
            {

            }

            protected override int RunMSBuild(ProcessStartInfo startInfo)
            {
                var process = Process.Start(startInfo);
                process.OutputDataReceived += (sender, e) =>
                {
                    Console.WriteLine(e.Data);
                };
                process.BeginOutputReadLine();
                process.WaitForExit();
                return process.ExitCode;
            }

            /// <summary>
            /// Returns true if the PE file meets all of the pre-conditions to be Open Source Signed.
            /// Returns false and logs msbuild errors otherwise.
            /// </summary>
            private static bool IsPublicSigned(PEReader peReader)
            {
                if (!peReader.HasMetadata)
                {
                    return false;
                }

                var mdReader = peReader.GetMetadataReader();
                if (!mdReader.IsAssembly)
                {
                    return false;
                }

                CorHeader header = peReader.PEHeaders.CorHeader;
                return (header.Flags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned;
            }

            public override void RemovePublicSign(string assemblyPath)
            {
                using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                using (var peReader = new PEReader(stream))
                using (var writer = new BinaryWriter(stream))
                {
                    if (!IsPublicSigned(peReader))
                    {
                        return;
                    }

                    stream.Position = peReader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;
                    writer.Write((UInt32)(peReader.PEHeaders.CorHeader.Flags | CorFlags.StrongNameSigned));
                }
            }

            public override bool VerifySignedAssembly(Stream assemblyStream)
            {
                using (var memoryStream = new MemoryStream())
                {
                    assemblyStream.CopyTo(memoryStream);

                    var byteArray = memoryStream.ToArray();
                    unsafe
                    {
                        fixed (byte* bytes = byteArray)
                        {
                            int outFlags;
                            return NativeMethods.StrongNameSignatureVerificationFromImage(
                                bytes,
                                byteArray.Length,
                                NativeMethods.SN_INFLAG_FORCE_VER, out outFlags) &&
                                (outFlags & NativeMethods.SN_OUTFLAG_WAS_VERIFIED) == NativeMethods.SN_OUTFLAG_WAS_VERIFIED;
                        }
                    }
                }
            }

            private unsafe static class NativeMethods
            {
                public const int SN_INFLAG_FORCE_VER = 0x1;
                public const int SN_OUTFLAG_WAS_VERIFIED = 0x1;

                [DllImport("mscoree.dll", CharSet = CharSet.Unicode)]
                [PreserveSig]
                public static extern bool StrongNameSignatureVerificationFromImage(byte* bytes, int length, int inFlags, out int outFlags);
            }
        }
    }
}
