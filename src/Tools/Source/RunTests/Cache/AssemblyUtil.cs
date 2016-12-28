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
        /// There are some DLLs whose abscence is expected and should not be considered an error.  These
        /// are assemblies which are used as light up functionality. 
        /// </summary>
        internal bool IsKnownLightUpAssembly(AssemblyName name)
        {
            switch (name.Name)
            {
                // This light up probing is done by the scripting layer. 
                case "System.Runtime.Loader":
                    return true;

                case "Microsoft.VisualStudio.GraphModel":
                    // This dependency needs to be better rationalized in our model. 
                    // https://github.com/dotnet/roslyn/issues/16201
                    return true;
                case "Microsoft.VisualStudio.TeamSystem.Common":
                    // The MS.VS.CA.Sdk.UI dependency needs to properly list this as a reference and it 
                    // needs to be included in our model. 
                    // https://github.com/dotnet/roslyn/issues/16202
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
