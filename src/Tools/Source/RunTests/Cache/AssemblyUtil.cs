// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class AssemblyUtil
    {
        /// <summary>
        /// The path where binaries need to be loaded from.
        /// </summary>
        internal string BinariesPath { get; }

        internal AssemblyUtil(string binariesPath)
        {
            BinariesPath = binariesPath;
        }

        /// <summary>
        /// There are some DLLs whose absence is expected and should not be considered an error.  These
        /// are assemblies which are either light up components or are a part of the VS reference graph
        /// which are never deployed for our tests.
        ///
        /// The key here though is to be very explicit about DLLs which are okay to be absent.  In the past
        /// we had build issues which failed to properly deploy important binaries like MS.CA and hence 
        /// produced bad content cache keys.
        /// </summary>
        internal bool IsKnownMissingAssembly(AssemblyName name)
        {
            switch (name.Name)
            {
                case "System.Runtime.Loader":
                    // This light up probing is done by the scripting layer. 
                    return true;
                case "Microsoft.Diagnostics.Tracing.EventSource":
                    // Part of ETW tracing and not used by suites at this time.
                    return true;
                case "Microsoft.VisualStudio.CodeAnalysis":
                case "Microsoft.VisualStudio.CodeAnalysis.Sdk":
                case "Microsoft.VisualStudio.TeamSystem.Common":
                case "Microsoft.VisualStudio.Repository":
                case "Microsoft.VisualStudio.DeveloperTools":
                case "Microsoft.VisualStudio.Diagnostics.Assert":
                case "Microsoft.VisualStudio.Diagrams.View.Interfaces":
                case "Microsoft.VisualStudio.Shell.ViewManager":
                case "Microsoft.VisualStudio.VCProjectEngine":
                case "Microsoft.VisualStudio.VirtualTreeGrid":
                    // These are assemblies which are a part of the tranisitive build graph but are known to
                    // not be a part of our testing code.
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get all of the <see cref="AssemblyName"/> values referenced by the specified assembly.
        /// </summary>
        internal List<AssemblyName> GetReferencedAssemblies(string assemblyPath)
        {
            using (var stream = File.OpenRead(assemblyPath))
            using (var peReader = new PEReader(stream))
            {
                var metadataReader = peReader.GetMetadataReader();

                var list = new List<AssemblyName>();
                foreach (var handle in metadataReader.AssemblyReferences)
                {
                    var reference = metadataReader.GetAssemblyReference(handle);
                    var name = new AssemblyName();
                    name.Name = metadataReader.GetString(reference.Name);
                    name.Version = reference.Version;
                    name.CultureName = metadataReader.GetString(reference.Culture);

                    var keyOrToken = metadataReader.GetBlobContent(reference.PublicKeyOrToken);
                    if (0 != (reference.Flags & AssemblyFlags.PublicKey))
                    {
                        name.SetPublicKey(keyOrToken.ToArray());
                    }
                    else if (!keyOrToken.IsEmpty)
                    {
                        name.SetPublicKeyToken(keyOrToken.ToArray());
                    }

                    list.Add(name);
                }

                return list;
            }
        }

        /// <summary>
        /// Get the path for the given <see cref="AssemblyName"/> value.
        /// </summary>
        /// <remarks>
        /// This implementation assumes that we are running on the desktop runtime without any 
        /// hidden probing paths.  Hence if the assembly isn't in the application directory then
        /// it must be in the GAC.  This is a fine assumption for now as we only run this on the
        /// desktop runtime but if caching is ever moved to CoreClr this will need to be revisited.
        ///
        /// In particular need to consider the ramifications if the tool and tests run on a
        /// different runtime.
        /// </remarks>
        internal bool TryGetAssemblyPath(AssemblyName name, out string assemblyPath)
        {
            Assembly assembly;
            try
            {
                // An assembly of the appropriate version in the GAC will win before any 
                // local assembly.  Must consider it first.
                assembly = Assembly.Load(name);
                if (assembly.GlobalAssemblyCache)
                {
                    assemblyPath = assembly.Location;
                    return true;
                }
            }
            catch
            {
                // It's okay and expected for probing to fail here.
                assembly = null;
            }

            var dllPath = Path.Combine(BinariesPath, $"{name.Name}.dll");
            if (File.Exists(dllPath))
            {
                assemblyPath = dllPath;
                return true;
            }

            var exePath = Path.Combine(BinariesPath, $"{name.Name}.exe");
            if (File.Exists(exePath))
            {
                assemblyPath = exePath;
                return true;
            }

            if (assembly != null)
            {
                assemblyPath = assembly.Location;
                return true;
            }

            assemblyPath = null;
            return false;
        }
    }
}
