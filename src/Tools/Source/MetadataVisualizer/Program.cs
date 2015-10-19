// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using Roslyn.Test.MetadataUtilities;
using Roslyn.Utilities;

internal class Program : IDisposable
{
    private class GenerationData
    {
        public MetadataReader MetadataReader;
        public PEReader PEReaderOpt;
        public byte[] DeltaILOpt;
        public object memoryOwner;
    }

    private readonly Arguments _arguments;
    private readonly TextWriter _writer;

    private string _pendingTitle;

    public Program(Arguments arguments)
    {
        _arguments = arguments;
        _writer = (arguments.OutputPath != null) ? new StreamWriter(File.OpenWrite(arguments.OutputPath), Encoding.UTF8) : Console.Out;
    }

    public void Dispose()
    {
        _writer.Dispose();
    }

    private void WriteData(string line, params object[] args)
    {
        if (_pendingTitle != null)
        {
            _writer.WriteLine(_pendingTitle);
            _pendingTitle = null;
        }

        _writer.WriteLine(line, args);
    }

    private static int Main(string[] args)
    {
        var arguments = Arguments.TryParse(args);
        if (arguments == null)
        {
            Console.WriteLine(Arguments.Help);
            return 1;
        }

        using (var p = new Program(arguments))
        {
            if (arguments.Recursive)
            {
                return p.RunRecursive();
            }
            else
            {
                return p.RunOne();
            }
        }
    }

    private unsafe int RunOne()
    {
        var generations = new List<GenerationData>();

        // gen 0:
        var generation = new GenerationData();
        try
        {
            var peStream = File.OpenRead(_arguments.Path);
            var peReader = new PEReader(peStream);
            try
            {
                // first try if we have a full PE image:
                generation.MetadataReader = peReader.GetMetadataReader();
                generation.PEReaderOpt = peReader;
            }
            catch (Exception e) when (e is InvalidOperationException || e is BadImageFormatException)
            {
                // try metadata only:

                var data = peReader.GetEntireImage();
                generation.MetadataReader = new MetadataReader(data.Pointer, data.Length);
            }

            generation.memoryOwner = peReader;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error reading '{0}': {1}", _arguments.Path, e.Message);
            return 1;
        }

        generations.Add(generation);

        // deltas:
        int i = 1;
        foreach (var delta in _arguments.EncDeltas)
        {
            var metadataPath = delta.Item1;
            var ilPathOpt = delta.Item2;

            generation = new GenerationData();
            try
            {
                var mdBytes = File.ReadAllBytes(metadataPath);
                GCHandle pinnedMetadataBytes = GCHandle.Alloc(mdBytes, GCHandleType.Pinned);
                generation.memoryOwner = pinnedMetadataBytes;
                generation.MetadataReader = new MetadataReader((byte*)pinnedMetadataBytes.AddrOfPinnedObject(), mdBytes.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading '{0}': {1}", metadataPath, e.Message);
                return 1;
            }

            if (ilPathOpt != null)
            {
                try
                {
                    generation.DeltaILOpt = File.ReadAllBytes(ilPathOpt);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading '{0}': {1}", ilPathOpt, e.Message);
                    return 1;
                }
            }

            generations.Add(generation);
            i++;
        }

        VisualizeGenerations(generations);
        return 0;
    }

    private unsafe void VisualizeGenerations(List<GenerationData> generations)
    {
        var mdReaders = generations.Select(g => g.MetadataReader).ToArray();
        var visualizer = new MetadataVisualizer(mdReaders, _writer);

        for (int generationIndex = 0; generationIndex < generations.Count; generationIndex++)
        {
            if (_arguments.SkipGenerations.Contains(generationIndex))
            {
                continue;
            }

            var generation = generations[generationIndex];
            var mdReader = generation.MetadataReader;

            visualizer.VisualizeHeaders();

            _writer.WriteLine(">>>");
            _writer.WriteLine(string.Format(">>> Generation {0}:", generationIndex));
            _writer.WriteLine(">>>");
            _writer.WriteLine();

            if (_arguments.DisplayMetadata)
            {
                visualizer.Visualize(generationIndex);
            }

            if (_arguments.DisplayIL)
            {
                VisualizeGenerationIL(visualizer, generationIndex, generation, mdReader);
            }

            VisualizeMemberRefs(mdReader);
        }
    }

    private static unsafe void VisualizeGenerationIL(MetadataVisualizer visualizer, int generationIndex, GenerationData generation, MetadataReader mdReader)
    {
        try
        {
            if (generation.PEReaderOpt != null)
            {
                foreach (var methodHandle in mdReader.MethodDefinitions)
                {
                    var method = mdReader.GetMethodDefinition(methodHandle);
                    var rva = method.RelativeVirtualAddress;
                    if (rva != 0)
                    {
                        var body = generation.PEReaderOpt.GetMethodBody(rva);
                        visualizer.VisualizeMethodBody(body, methodHandle);
                    }
                }
            }
            else if (generation.DeltaILOpt != null)
            {
                fixed (byte* deltaILPtr = generation.DeltaILOpt)
                {
                    foreach (var generationHandle in mdReader.MethodDefinitions)
                    {
                        var method = mdReader.GetMethodDefinition(generationHandle);
                        var rva = method.RelativeVirtualAddress;
                        if (rva != 0)
                        {
                            var body = MethodBodyBlock.Create(new BlobReader(deltaILPtr + rva, generation.DeltaILOpt.Length - rva));

                            visualizer.VisualizeMethodBody(body, generationHandle, generationIndex);
                        }
                    }
                }
            }
        }
        catch (BadImageFormatException)
        {
            visualizer.WriteLine("<bad metadata>");
        }
    }

    private static readonly string[] s_PEExtensions = new[] { "*.dll", "*.exe", "*.netmodule", "*.winmd" };

    private static IEnumerable<string> GetAllBinaries(string dir)
    {
        foreach (var subdir in Directory.GetDirectories(dir))
        {
            foreach (var file in GetAllBinaries(subdir))
            {
                yield return file;
            }
        }

        foreach (var file in from extension in s_PEExtensions
                             from file in Directory.GetFiles(dir, extension)
                             select file)
        {
            yield return file;
        }
    }

    private void VisualizeStatistics(MetadataReader mdReader)
    {
        if (!_arguments.DisplayStatistics)
        {
            return;
        }

        WriteData("> method definitions: {0}, {1:F1}% with bodies",
            mdReader.MethodDefinitions.Count,
            100 * ((double)mdReader.MethodDefinitions.Count(handle => mdReader.GetMethodDefinition(handle).RelativeVirtualAddress != 0) / mdReader.MethodDefinitions.Count));
    }

    private void VisualizeAssemblyReferences(MetadataReader mdReader)
    {
        if (!_arguments.DisplayAssemblyReferences)
        {
            return;
        }

        foreach (var handle in mdReader.AssemblyReferences)
        {
            var ar = mdReader.GetAssemblyReference(handle);

            WriteData("{0}, Version={1}, PKT={2}",
                mdReader.GetString(ar.Name),
                ar.Version,
                BitConverter.ToString(mdReader.GetBlobBytes(ar.PublicKeyOrToken)));
        }
    }

    private void VisualizeMemberRefs(MetadataReader mdReader)
    {
        if (!_arguments.FindRefs.Any())
        {
            return;
        }

        var memberRefs = new HashSet<MemberRefKey>(
            from arg in _arguments.FindRefs
            let split = arg.Split(':')
            where split.Length == 3
            select new MemberRefKey(split[0].Trim(), split[1].Trim(), split[2].Trim()));

        foreach (var handle in mdReader.MemberReferences)
        {
            var memberRef = mdReader.GetMemberReference(handle);

            if (memberRef.Parent.Kind != HandleKind.TypeReference)
            {
                continue;
            }

            var typeRef = mdReader.GetTypeReference((TypeReferenceHandle)memberRef.Parent);
            if (typeRef.ResolutionScope.Kind != HandleKind.AssemblyReference)
            {
                // TODO: handle nested types
                continue;
            }

            var assemblyRef = mdReader.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope);

            var key = new MemberRefKey(
                assemblyNameOpt: mdReader.GetString(assemblyRef.Name),
                assemblyVersionOpt: assemblyRef.Version,
                @namespace: mdReader.GetString(typeRef.Namespace),
                typeName: mdReader.GetString(typeRef.Name),
                memberName: mdReader.GetString(memberRef.Name)
            );

            if (memberRefs.Contains(key))
            {
                WriteData($"0x{MetadataTokens.GetToken(handle):X8}->0x{MetadataTokens.GetToken(memberRef.Parent):X8}:" + $" {key.ToString()}");
            }
        }
    }

    private struct MemberRefKey : IEquatable<MemberRefKey>
    {
        public readonly string AssemblyNameOpt;
        public readonly Version AssemblyVersionOpt;
        public readonly string Namespace;
        public readonly string TypeName;
        public readonly string MemberName;

        public MemberRefKey(string assemblyName, string qualifiedTypeName, string memberName)
        {
            if (assemblyName.Length > 0)
            {
                var an = new AssemblyName(assemblyName);
                AssemblyNameOpt = an.Name;
                AssemblyVersionOpt = an.Version;
            }
            else
            {
                AssemblyNameOpt = null;
                AssemblyVersionOpt = null;
            }

            var lastDot = qualifiedTypeName.LastIndexOf('.');
            Namespace = (lastDot >= 0) ? qualifiedTypeName.Substring(0, lastDot) : "";
            TypeName = (lastDot >= 0) ? qualifiedTypeName.Substring(lastDot + 1) : "";

            MemberName = memberName;
        }

        public MemberRefKey(
            string assemblyNameOpt,
            Version assemblyVersionOpt,
            string @namespace,
            string typeName,
            string memberName)
        {
            AssemblyNameOpt = assemblyNameOpt;
            AssemblyVersionOpt = assemblyVersionOpt;
            Namespace = @namespace;
            TypeName = typeName;
            MemberName = memberName;
        }

        public override bool Equals(object obj)
        {
            return obj is MemberRefKey && Equals((MemberRefKey)obj);
        }

        public override int GetHashCode()
        {
            // don't include assembly name/version
            return Hash.Combine(Namespace,
                   Hash.Combine(TypeName,
                   Hash.Combine(MemberName, 0)));
        }

        public bool Equals(MemberRefKey other)
        {
            return (this.AssemblyNameOpt == null || other.AssemblyNameOpt == null || this.AssemblyNameOpt.Equals(other.AssemblyNameOpt, StringComparison.OrdinalIgnoreCase)) &&
                   (this.AssemblyVersionOpt == null || other.AssemblyVersionOpt == null || this.AssemblyVersionOpt.Equals(other.AssemblyVersionOpt)) &&
                   this.Namespace.Equals(other.Namespace) &&
                   this.TypeName.Equals(other.TypeName) &&
                   this.MemberName.Equals(other.MemberName);
        }

        public override string ToString()
        {
            return (AssemblyNameOpt != null ? $"{AssemblyNameOpt}, Version={AssemblyVersionOpt}" : "") +
                   $":{Namespace}{(Namespace.Length > 0 ? "." : "")}{TypeName}:{MemberName}";
        }
    }

    private int RunRecursive()
    {
        bool hasError = false;

        foreach (var file in GetAllBinaries(_arguments.Path))
        {
            using (var peReader = new PEReader(File.OpenRead(file)))
            {
                try
                {
                    if (!peReader.HasMetadata)
                    {
                        continue;
                    }
                }
                catch (BadImageFormatException e)
                {
                    _writer.WriteLine("{0}: {1}", file, e.Message);
                    hasError = true;
                    continue;
                }

                _pendingTitle = file;

                try
                {
                    var mdReader = peReader.GetMetadataReader();

                    VisualizeAssemblyReferences(mdReader);
                    VisualizeStatistics(mdReader);
                    VisualizeMemberRefs(mdReader);
                }
                catch (BadImageFormatException e)
                {
                    WriteData("ERROR: {0}", e.Message);
                    hasError = true;
                    continue;
                }

                if (_pendingTitle == null)
                {
                    _writer.WriteLine();
                }

                _pendingTitle = null;
            }
        }

        return hasError ? 1 : 0;
    }
}
