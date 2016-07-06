using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static SignRoslyn.PathUtil;

namespace SignRoslyn
{
    internal sealed class RunSignUtil
    {
        /// <summary>
        /// The number of bytes from the start of the <see cref="CorHeader"/> to its <see cref="CorFlags"/>.
        /// </summary>
        internal const int OffsetFromStartOfCorHeaderToFlags =
               sizeof(Int32)  // byte count
             + sizeof(Int16)  // major version
             + sizeof(Int16)  // minor version
             + sizeof(Int64); // metadata directory

        private readonly SignData _signData;
        private readonly ISignTool _signTool;

        internal RunSignUtil(ISignTool signTool, SignData signData)
        {
            _signTool = signTool;
            _signData = signData;
        }

        internal void Go()
        {
            // First validate our inputs and give a useful error message about anything that happens to be missing
            // from the binaries directory.
            ValidateBinaries();

            // Next remove public sign from all of the assemblies.  It can interfere with the signing process.
            RemovePublicSign();

            // Next step is to sign all of the assemblies.
            SignAssemblies();

            // Last we sign the VSIX files (being careful to take into account nesting)
            SignVsixes();
        }

        private void RemovePublicSign()
        {
            Console.WriteLine("Removing public sign");
            foreach (var name in _signData.AssemblyNames)
            {
                Console.WriteLine($"\t{name}");
                var path = Path.Combine(_signData.RootBinaryPath, name);
                RemovePublicSign(path);
            }
        }

        /// <summary>
        /// Sign all of the assembly files.  No need to consider nesting here and it can be done in a single pass.
        /// </summary>
        private void SignAssemblies()
        {
            Console.WriteLine("Signing assemblies");
            foreach (var name in _signData.AssemblyNames)
            {
                Console.WriteLine($"\t{name}");
            }

            _signTool.Sign(_signData.AssemblyNames.Select(x => Path.Combine(_signData.RootBinaryPath, x)));
        }

        /// <summary>
        /// Sign all of the VSIX files.  It is possible for VSIX to nest other VSIX so we must consider this when 
        /// picking the order.
        /// </summary>
        private void SignVsixes()
        {
            var round = 0;
            var signedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var toSignList = _signData.VsixNames.ToList();
            do
            {
                Console.WriteLine($"Signing VSIX round {round}");
                var list = new List<string>();
                var i = 0;
                var progress = false;
                while (i < toSignList.Count)
                {
                    var vsixName = toSignList[i];
                    if (AreNestedVsixSigned(vsixName, signedSet))
                    {
                        list.Add(vsixName);
                        toSignList.RemoveAt(i);
                        Console.WriteLine($"\tRepacking {vsixName}");
                        Repack(vsixName);
                        progress = true;
                    }
                    else
                    {
                        i++;
                    }
                }

                if (!progress)
                {
                    throw new Exception("No progress made on nested VSIX which indicates validation bug");
                }

                Console.WriteLine($"\tSigning ...");
                _signTool.Sign(list.Select(x => Path.Combine(_signData.RootBinaryPath, x)));

                // Signing is complete so now we can update the signed set.
                list.ForEach(x => signedSet.Add(x));

                round++;
            } while (toSignList.Count > 0);
        }

        /// <summary>
        /// Repack the VSIX with the signed parts from the binaries directory.
        /// </summary>
        private void Repack(string vsixName)
        {
            var vsixPath = Path.Combine(_signData.RootBinaryPath, vsixName);
            using (var package = Package.Open(vsixPath, FileMode.Open, FileAccess.ReadWrite))
            {
                foreach (var part in package.GetParts())
                {
                    var relativeName = GetPartRelativeFileName(part);
                    var name = Path.GetFileName(relativeName);

                    // Only need to repack assemblies and VSIX parts.
                    if (!IsVsix(name) && !IsAssembly(name))
                    {
                        continue;
                    }

                    var signedPath = Path.Combine(_signData.RootBinaryPath, name);
                    using (var stream = File.OpenRead(signedPath))
                    using (var partStream = part.GetStream(FileMode.Open, FileAccess.ReadWrite))
                    {
                        stream.CopyTo(partStream);
                        partStream.SetLength(stream.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Get the name of all VSIX which are nested inside this VSIX.
        /// </summary>
        private IEnumerable<string> GetNestedVsixRelativeNames(string vsixName)
        {
            return GetVsixPartRelativeNames(vsixName).Where(x => IsVsix(x));
        }

        private bool AreNestedVsixSigned(string vsixName, HashSet<string> signedSet)
        {
            foreach (var relativeName in GetNestedVsixRelativeNames(vsixName))
            {
                var name = Path.GetFileName(relativeName);
                if (!signedSet.Contains(name))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Return all the assembly and VSIX contents nested in the VSIX
        /// </summary>
        private List<string> GetVsixPartRelativeNames(string vsixName)
        {
            var list = new List<string>();
            var vsixPath = Path.Combine(_signData.RootBinaryPath, vsixName);
            using (var package = Package.Open(vsixPath, FileMode.Open, FileAccess.Read))
            {
                foreach (var part in package.GetParts())
                {
                    var name = GetPartRelativeFileName(part);
                    list.Add(name);
                }
            }

            return list;
        }

        private void ValidateBinaries()
        {
            if (!ValidateBinariesExist() || !ValidateVsixContents())
            {
                throw new Exception("Errors validating the state before signing");
            }
        }

        private bool ValidateBinariesExist()
        {
            var allGood = true;
            foreach (var binaryName in _signData.BinaryNames)
            {
                var path = Path.Combine(_signData.RootBinaryPath, binaryName);
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Did not find {binaryName} at {path}");
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool ValidateVsixContents()
        {
            var allBinaryNames = _signData.BinaryNames.Concat(_signData.ExcludeBinaryNames);
            var allBinarySet = new HashSet<string>(allBinaryNames, StringComparer.OrdinalIgnoreCase);
            var allGood = true;
            foreach (var vsixName in _signData.VsixNames)
            {
                if (!ValidateVsixContents(vsixName, allBinarySet))
                {
                    allGood = false;
                }
            }

            return allGood;
        }

        private bool ValidateVsixContents(string vsixName, HashSet<string> allBinarySet)
        {
            var allGood = true;
            foreach (var relativeName in GetVsixPartRelativeNames(vsixName))
            {
                var name = Path.GetFileName(relativeName);
                if (!IsVsix(name) && !IsAssembly(name))
                {
                    continue;
                }

                if (!allBinarySet.Contains(name))
                {
                    Console.WriteLine($"Vsix {vsixName} contains assembly {name} which is not in the binary list");
                    allGood = false;
                }
            }

            return allGood;
        }

        private static string GetPartRelativeFileName(PackagePart part)
        {
            var path = part.Uri.OriginalString;
            if (!string.IsNullOrEmpty(path) && path[0] == '/')
            {
                path = path.Substring(1);
            }

            return path;
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

        private void RemovePublicSign(string assemblyPath, bool force = false)
        {
            using (var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            using (var peReader = new PEReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                if (!IsPublicSigned(peReader) && force)
                {
                    throw new Exception($"{Path.GetFileName(assemblyPath)} is not public signed.");
                }

                stream.Position = peReader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;
                writer.Write((UInt32)(peReader.PEHeaders.CorHeader.Flags | CorFlags.StrongNameSigned));
            }
        }

    }
}
