// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Tools;

internal readonly record struct ApiPattern(
    SymbolKindFlags SymbolKinds,
    Regex MetadataNamePattern,
    bool IsIncluded);

[Flags]
internal enum SymbolKindFlags
{
    None = 0,
    NamedType = 1,
    Method = 1 << 1,
    Field = 1 << 3,
}

/// <summary>
/// The task transforms given assemblies by changing the visibility of members defined in these assemblies
/// based on filter patterns specified in the corresponding <see cref="ApiSets"/>.
/// <see cref="ApiSets"/> are text files whose file names (without extension) match the file names of <see cref="References"/>.
/// Each API set specifies a list of patterns that define which members should be included or excluded from the output assembly.
/// All excluded members are made internal or private.
/// </summary>
public sealed class GenerateFilteredReferenceAssembliesTask : Task
{
    private static readonly Regex s_lineSyntax = new("""
        ^
        \s*
        (?<Inclusion>[+|-]?)
        ((?<Kinds>[A-Za-z]+):)?
        (?<MetadataName>[^#]*)
        ([#].*)?
        $
        """, RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);

    [Required]
    public ITaskItem[] ApiSets { get; private set; } = null!;

    [Required]
    public ITaskItem[] References { get; private set; } = null!;

    [Required]
    public string OutputDir { get; private set; } = null!;

    public override bool Execute()
    {
        try
        {
            ExecuteImpl();
        }
        catch (Exception e)
        {
            Log.LogError($"GenerateFilteredReferenceAssembliesTask failed with exception:{Environment.NewLine}{e}");
        }

        return !Log.HasLoggedErrors;
    }

    private void ExecuteImpl()
    {
        ExecuteImpl(ApiSets.Select(item => (item.ItemSpec, (IReadOnlyList<string>)File.ReadAllLines(item.ItemSpec))));
    }

    internal void ExecuteImpl(IEnumerable<(string apiSpecPath, IReadOnlyList<string> lines)> apiSets)
    {
        var referencesByName = References.ToDictionary(r => Path.GetFileNameWithoutExtension(r.ItemSpec), r => r.ItemSpec);

        foreach (var (specPath, filters) in apiSets)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(specPath);
            if (!referencesByName.TryGetValue(assemblyName, out var originalReferencePath))
            {
                Log.LogWarning($"Assembly '{assemblyName}' not found among project references");
                continue;
            }

            var filteredReferencePath = Path.Combine(OutputDir, assemblyName + ".dll");
            var errors = new List<(string message, int line)>();
            var patterns = new List<ApiPattern>();
            ParseApiPatterns(filters, errors, patterns);

            foreach (var (message, line) in errors)
            {
                Log.LogWarning($"Invalid API pattern at {specPath} line {line}: {message}");
            }

            var peImageBuffer = File.ReadAllBytes(originalReferencePath);
            Rewrite(peImageBuffer, patterns.ToImmutableArray());

            try
            {
                File.WriteAllBytes(filteredReferencePath, peImageBuffer);
            }
            catch when (File.Exists(filteredReferencePath))
            {
                // Another instance of the task might already be writing the content. 
                Log.LogMessage($"Output file '{filteredReferencePath}' already exists.");
            }
        }
    }

    internal static void ParseApiPatterns(IReadOnlyList<string> lines, List<(string message, int line)> errors, List<ApiPattern> patterns)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            var match = s_lineSyntax.Match(line);
            if (!match.Success)
            {
                errors.Add(("unable to parse", i + 1));
                continue;
            }

            var inclusion = match.Groups["Inclusion"].Value;
            var kinds = match.Groups["Kinds"].Value;
            var metadataName = match.Groups["MetadataName"].Value;

            var hasSymbolKindError = false;
            var symbolKinds = SymbolKindFlags.None;
            foreach (var kind in kinds)
            {
                symbolKinds |= kind switch
                {
                    'F' => SymbolKindFlags.Field,
                    'M' => SymbolKindFlags.Method,
                    'T' => SymbolKindFlags.NamedType,
                    _ => Unexpected()
                };

                SymbolKindFlags Unexpected()
                {
                    hasSymbolKindError = true;
                    errors.Add(($"unexpected symbol kind: '{kind}'", i + 1));
                    return SymbolKindFlags.None;
                }
            }

            if (hasSymbolKindError)
            {
                continue;
            }

            if (symbolKinds == SymbolKindFlags.None)
            {
                symbolKinds = SymbolKindFlags.NamedType;
            }

            if (metadataName is "")
            {
                if (inclusion is not "" || kinds is not "")
                {
                    errors.Add(("expected metadata name", i + 1));
                }

                continue;
            }

            patterns.Add(new()
            {
                SymbolKinds = symbolKinds,
                MetadataNamePattern = ParseApiPattern(metadataName),
                IsIncluded = inclusion is not ['-']
            });
        }
    }

    /// <summary>
    /// Interprets `*` as `.*` and escapes the rest of regex-special characters.
    /// </summary>
    internal static Regex ParseApiPattern(string pattern)
        => new("^" + string.Join(".*", pattern.Trim().Split('*').Select(Regex.Escape)) + "$",
            RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    internal static void GetAllMembers(
        Compilation compilation,
        List<INamedTypeSymbol> types,
        List<IMethodSymbol> methods,
        List<IFieldSymbol> fields)
    {
        Recurse(compilation.GlobalNamespace.GetMembers());

        void Recurse(IEnumerable<ISymbol> members)
        {
            foreach (var member in members)
            {
                switch (member)
                {
                    case INamedTypeSymbol type:
                        if (type.MetadataToken != 0)
                        {
                            types.Add(type);
                            Recurse(type.GetMembers());
                        }
                        break;

                    case IMethodSymbol method:
                        if (method.MetadataToken != 0)
                        {
                            methods.Add(method);
                        }
                        break;

                    case IFieldSymbol field:
                        if (field.MetadataToken != 0)
                        {
                            fields.Add(field);
                        }
                        break;

                    case INamespaceSymbol ns:
                        Recurse(ns.GetMembers());
                        break;
                }
            }
        }
    }

    private static bool IsIncluded(ISymbol symbol, ImmutableArray<ApiPattern> patterns)
    {
        var id = symbol.GetDocumentationCommentId();
        Debug.Assert(id is [_, ':', ..]);
        id = id[2..];

        var kind = GetKindFlags(symbol);

        // Type symbols areconsidered excluded by default.
        // Member symbols are included by default since their type limits the effective visibility.
        var isIncluded = symbol is not INamedTypeSymbol;

        foreach (var pattern in patterns)
        {
            if ((pattern.SymbolKinds & kind) == kind && pattern.MetadataNamePattern.IsMatch(id))
            {
                isIncluded = pattern.IsIncluded;
            }
        }

        return isIncluded;
    }

    private static SymbolKindFlags GetKindFlags(ISymbol symbol)
        => symbol.Kind switch
        {
            SymbolKind.Field => SymbolKindFlags.Field,
            SymbolKind.Method => SymbolKindFlags.Method,
            SymbolKind.NamedType => SymbolKindFlags.NamedType,
            _ => throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
        };

    internal static unsafe void Rewrite(byte[] peImage, ImmutableArray<ApiPattern> patterns)
    {
        // Include all APIs if no patterns are specified.
        if (patterns.IsEmpty)
        {
            return;
        }

        using var readableStream = new MemoryStream(peImage, writable: false);
        var metadataRef = MetadataReference.CreateFromStream(readableStream);
        var compilation = CSharpCompilation.Create("Metadata", references: [metadataRef]);

        // Collect all member definitions that have visibility flags:
        var types = new List<INamedTypeSymbol>();
        var methods = new List<IMethodSymbol>();
        var fields = new List<IFieldSymbol>();
        GetAllMembers(compilation, types, methods, fields);

        // Update visibility flags:
        using var writableStream = new MemoryStream(peImage, writable: true);
        using var peReader = new PEReader(writableStream);
        using var writer = new BinaryWriter(writableStream);

        var headers = peReader.PEHeaders;
        Debug.Assert(headers.PEHeader != null);

        var metadataReader = peReader.GetMetadataReader();
        var metadataOffset = peReader.PEHeaders.MetadataStartOffset;

        UpdateTypeDefinitions(
            writer,
            metadataReader,
            patterns,
            types.OrderBy(t => t.MetadataToken).ToImmutableArray(),
            metadataOffset);

        UpdateMethodDefinitions(
            writer,
            metadataReader,
            patterns,
            methods.OrderBy(t => t.MetadataToken).ToImmutableArray(),
            metadataOffset);

        UpdateFieldDefinitions(
            writer,
            metadataReader,
            patterns,
            fields.OrderBy(t => t.MetadataToken).ToImmutableArray(),
            metadataOffset);

        // unsign:
        if (headers.PEHeader.CertificateTableDirectory.Size > 0)
        {
            var certificateTableDirectoryOffset = (headers.PEHeader.Magic == PEMagic.PE32Plus) ? 144 : 128;
            writableStream.Position = peReader.PEHeaders.PEHeaderStartOffset + certificateTableDirectoryOffset;
            writer.Write((long)0);
        }

        writer.Flush();

        // update mvid:
        var moduleDef = metadataReader.GetModuleDefinition();
        var mvidOffset = metadataOffset + metadataReader.GetHeapMetadataOffset(HeapIndex.Guid) + (MetadataTokens.GetHeapOffset(moduleDef.Mvid) - 1) * sizeof(Guid);
#if DEBUG
        writableStream.Position = mvidOffset;
        Debug.Assert(metadataReader.GetGuid(moduleDef.Mvid) == ReadGuid(writableStream));
#endif
        var newMvid = CreateMvid(writableStream);
        writableStream.Position = mvidOffset;
        WriteGuid(writer, newMvid);

        writer.Flush();
    }

    private static unsafe TSymbol? GetSymbolWithToken<TSymbol>(ImmutableArray<TSymbol> symbols, ref int symbolIndex, EntityHandle handle) where TSymbol : class, ISymbol
        // If the current definition does not have corresponding symbol,
        // we couldn't decode the symbol from metadata. Treat such definition as excluded.
        => (symbolIndex < symbols.Length && symbols[symbolIndex].MetadataToken == MetadataTokens.GetToken(handle)) ? symbols[symbolIndex++] : null;

    private static unsafe void UpdateTypeDefinitions(BinaryWriter writer, MetadataReader metadataReader, ImmutableArray<ApiPattern> patterns, ImmutableArray<INamedTypeSymbol> symbols, int metadataOffset)
    {
        var tableOffset = metadataOffset + metadataReader.GetTableMetadataOffset(TableIndex.TypeDef);
        var tableRowSize = metadataReader.GetTableRowSize(TableIndex.TypeDef);
        var symbolIndex = 0;

        foreach (var handle in metadataReader.TypeDefinitions)
        {
            var symbol = GetSymbolWithToken(symbols, ref symbolIndex, handle);
            if (symbol == null || !IsIncluded(symbol, patterns))
            {
                var typeDef = metadataReader.GetTypeDefinition(handle);

                // reduce visibility so that the type is not visible outside the assembly:
                var oldVisibility = typeDef.Attributes & TypeAttributes.VisibilityMask;
                var newVisibility = oldVisibility switch
                {
                    TypeAttributes.Public => TypeAttributes.NotPublic,
                    TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem => TypeAttributes.NestedAssembly,
                    _ => oldVisibility
                };

                if (oldVisibility == newVisibility)
                {
                    continue;
                }

                // Type attributes are store as the first field of the row and are 4B
                var offset = tableOffset + (MetadataTokens.GetRowNumber(handle) - 1) * tableRowSize + 0;
#if DEBUG
                writer.BaseStream.Position = offset;
                Debug.Assert((TypeAttributes)ReadUInt32(writer.BaseStream) == typeDef.Attributes);
#endif
                writer.BaseStream.Position = offset;
                Debug.Assert(BitConverter.IsLittleEndian);
                writer.Write((uint)(typeDef.Attributes & ~TypeAttributes.VisibilityMask | newVisibility));
            }
        }
    }

    private static unsafe void UpdateMethodDefinitions(BinaryWriter writer, MetadataReader metadataReader, ImmutableArray<ApiPattern> patterns, ImmutableArray<IMethodSymbol> symbols, int metadataOffset)
    {
        var tableOffset = metadataOffset + metadataReader.GetTableMetadataOffset(TableIndex.MethodDef);
        var tableRowSize = metadataReader.GetTableRowSize(TableIndex.MethodDef);
        var symbolIndex = 0;

        foreach (var handle in metadataReader.MethodDefinitions)
        {
            var symbol = GetSymbolWithToken(symbols, ref symbolIndex, handle);
            if (symbol == null || !IsIncluded(symbol, patterns))
            {
                var def = metadataReader.GetMethodDefinition(handle);

                // reduce visibility so that the method is not visible outside the assembly:
                var oldVisibility = def.Attributes & MethodAttributes.MemberAccessMask;
                var newVisibility = MethodAttributes.Private;
                if (oldVisibility == newVisibility)
                {
                    continue;
                }

                // Row: RvaOffset (4B), ImplAttributes (2B), Attributes (2B), ...
                var offset = tableOffset + (MetadataTokens.GetRowNumber(handle) - 1) * tableRowSize + sizeof(uint) + sizeof(ushort);
#if DEBUG
                writer.BaseStream.Position = offset;
                Debug.Assert((MethodAttributes)ReadUInt16(writer.BaseStream) == def.Attributes);
#endif
                writer.BaseStream.Position = offset;
                Debug.Assert(BitConverter.IsLittleEndian);
                writer.Write((ushort)(def.Attributes & ~MethodAttributes.MemberAccessMask | newVisibility));
            }
        }
    }

    private static unsafe void UpdateFieldDefinitions(BinaryWriter writer, MetadataReader metadataReader, ImmutableArray<ApiPattern> patterns, ImmutableArray<IFieldSymbol> symbols, int metadataOffset)
    {
        var tableOffset = metadataOffset + metadataReader.GetTableMetadataOffset(TableIndex.Field);
        var tableRowSize = metadataReader.GetTableRowSize(TableIndex.Field);
        var symbolIndex = 0;

        foreach (var handle in metadataReader.FieldDefinitions)
        {
            var symbol = GetSymbolWithToken(symbols, ref symbolIndex, handle);
            if (symbol == null || !IsIncluded(symbol, patterns))
            {
                var def = metadataReader.GetFieldDefinition(handle);

                // reduce visibility so that the field is not visible outside the assembly:
                var oldVisibility = def.Attributes & FieldAttributes.FieldAccessMask;
                var newVisibility = FieldAttributes.Private;
                if (oldVisibility == newVisibility)
                {
                    continue;
                }

                // Row: Attributes (2B), ...
                var offset = tableOffset + (MetadataTokens.GetRowNumber(handle) - 1) * tableRowSize + 0;
#if DEBUG
                writer.BaseStream.Position = offset;
                Debug.Assert((FieldAttributes)ReadUInt16(writer.BaseStream) == def.Attributes);
#endif
                writer.BaseStream.Position = offset;
                Debug.Assert(BitConverter.IsLittleEndian);
                writer.Write((ushort)(def.Attributes & ~FieldAttributes.FieldAccessMask | newVisibility));
            }
        }
    }

    private static uint ReadUInt32(Stream stream)
        => unchecked((uint)(stream.ReadByte() | stream.ReadByte() << 8 | stream.ReadByte() << 16 | stream.ReadByte() << 24));

    private static uint ReadUInt16(Stream stream)
        => unchecked((uint)(stream.ReadByte() | stream.ReadByte() << 8));

    private static unsafe Guid ReadGuid(Stream stream)
    {
        var buffer = new byte[sizeof(Guid)];
        Debug.Assert(stream.Read(buffer, 0, buffer.Length) == buffer.Length);
        fixed (byte* ptr = buffer)
        {
            var reader = new BlobReader(ptr, buffer.Length);
            return reader.ReadGuid();
        }
    }

    private static unsafe void WriteGuid(BinaryWriter writer, Guid guid)
    {
        var buffer = new byte[sizeof(Guid)];
        var blob = new BlobWriter(buffer);
        blob.WriteGuid(guid);
        writer.Write(buffer, 0, buffer.Length);
    }

    private static Guid CreateMvid(Stream stream)
    {
        stream.Position = 0;
        using var sha = SHA256.Create();
        return BlobContentId.FromHash(sha.ComputeHash(stream)).Guid;
    }
}
