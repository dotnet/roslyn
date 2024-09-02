// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using System.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities.CoreClr
{
    internal sealed class TestExecutionLoadContext : AssemblyLoadContext
    {
        private readonly ImmutableDictionary<string, ModuleData> _dependencies;

        public TestExecutionLoadContext(IList<ModuleData> dependencies)
        {
            _dependencies = dependencies.ToImmutableDictionary(d => d.FullName, StringComparer.Ordinal);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var comparison = StringComparison.OrdinalIgnoreCase;
            if (assemblyName.Name.StartsWith("System.", comparison) ||
                assemblyName.Name.StartsWith("Microsoft.", comparison) ||
                comparer.Equals(assemblyName.Name, "mscorlib") ||
                comparer.Equals(assemblyName.Name, "System") ||
                comparer.Equals(assemblyName.Name, "netstandard"))
            {
                return null;
            }

            if (_dependencies.TryGetValue(assemblyName.FullName, out var moduleData))
            {
                return LoadImageAsAssembly(moduleData.Image);
            }

            return null;
        }

        private Assembly LoadImageAsAssembly(ImmutableArray<byte> mainImage)
        {
            using (var assemblyStream = new MemoryStream(mainImage.ToArray()))
            {
                return LoadFromStream(assemblyStream);
            }
        }

        internal (int ExitCode, string Output) Execute(ImmutableArray<byte> mainImage, string[] mainArgs, int? expectedOutputLength)
        {
            var mainAssembly = LoadImageAsAssembly(mainImage);
            var entryPoint = mainAssembly.EntryPoint;

            AssertEx.NotNull(entryPoint, "Attempting to execute an assembly that has no entrypoint; is your test trying to execute a DLL?");

            int exitCode = 0;
            SharedConsole.CaptureOutput(() =>
            {
                var count = entryPoint.GetParameters().Length;
                object[] args;
                if (count == 0)
                {
                    args = Array.Empty<object>();
                }
                else if (count == 1)
                {
                    args = new[] { mainArgs ?? Array.Empty<string>() };
                }
                else
                {
                    throw new Exception("Unrecognized entry point");
                }

                exitCode = entryPoint.Invoke(null, args) is int exit ? exit : 0;
            }, expectedOutputLength ?? 0, out var stdOut, out var stdErr);

            var output = stdOut + stdErr;
            return (exitCode, output);
        }

        public SortedSet<string> GetMemberSignaturesFromMetadata(string fullyQualifiedTypeName, string memberName, IEnumerable<ModuleDataId> searchModules)
        {
            try
            {
                var signatures = new SortedSet<string>();
                foreach (var id in searchModules)
                {
                    var name = new AssemblyName(id.FullName);
                    var assembly = LoadFromAssemblyName(name);
                    foreach (var signature in MetadataSignatureHelper.GetMemberSignatures(assembly, fullyQualifiedTypeName, memberName))
                    {
                        signatures.Add(signature);
                    }
                }
                return signatures;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting signatures {fullyQualifiedTypeName}.{memberName}", ex);
            }
        }
    }
}
#endif
