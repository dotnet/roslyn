// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal enum DeclaredSymbolInfoKind : byte
{
    Class,
    Constant,
    Constructor,
    Delegate,
    Enum,
    EnumMember,
    Event,
    Extension,
    ExtensionMethod,
    Field,
    Indexer,
    Interface,
    Method,
    Module,
    Namespace,
    Property,
    Record,
    RecordStruct,
    Struct,
}

[DataContract]
[method: Obsolete("Do not call directly.  Only around for serialization.  Use Create instead")]
internal readonly struct DeclaredSymbolInfo(
    string name,
    string? nameSuffix,
    string? containerDisplayName,
    string fullyQualifiedContainerName,
    TextSpan span,
    ImmutableArray<string> inheritanceNames,
    uint flags) : IEquatable<DeclaredSymbolInfo>
{
    /// <summary>
    /// The name to pattern match against, and to show in a final presentation layer.
    /// </summary>
    [DataMember(Order = 0)]
    public readonly string Name = name;

    /// <summary>
    /// An optional suffix to be shown in a presentation layer appended to <see cref="Name"/>.
    /// </summary>
    [DataMember(Order = 1)]
    public readonly string? NameSuffix = nameSuffix;

    /// <summary>
    /// Container of the symbol that can be shown in a final presentation layer. 
    /// For example, the container of a type "KeyValuePair" might be 
    /// "System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;".  This can 
    /// then be shown with something like "type System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"
    /// to indicate where the symbol is located.
    /// </summary>
    [DataMember(Order = 2)]
    public readonly string? ContainerDisplayName = containerDisplayName;

    /// <summary>
    /// Dotted container name of the symbol, used for pattern matching.  For example
    /// The fully qualified container of a type "KeyValuePair" would be 
    /// "System.Collections.Generic.Dictionary" (note the lack of type parameters).
    /// This way someone can search for "D.KVP" and have the "D" part of the pattern
    /// match against this.  This should not be shown in a presentation layer.
    /// </summary>
    [DataMember(Order = 3)]
    public readonly string FullyQualifiedContainerName = fullyQualifiedContainerName;

    [DataMember(Order = 4)]
    public readonly TextSpan Span = span;

    /// <summary>
    /// The names directly referenced in source that this type inherits from.
    /// </summary>
    [DataMember(Order = 5)]
    public ImmutableArray<string> InheritanceNames { get; } = inheritanceNames;

    // Store the kind (5 bits), accessibility (4 bits), parameter-count (4 bits), and type-parameter-count (4 bits)
    // in a single int.
    [DataMember(Order = 6)]
    private readonly uint _flags = flags;

    private const uint Lower4BitMask = 0b1111;
    private const uint Lower5BitMask = 0b11111;

    public DeclaredSymbolInfoKind Kind => GetKind(_flags);
    public Accessibility Accessibility => GetAccessibility(_flags);
    public byte ParameterCount => GetParameterCount(_flags);
    public byte TypeParameterCount => GetTypeParameterCount(_flags);
    public bool IsNestedType => GetIsNestedType(_flags);
    public bool IsPartial => GetIsPartial(_flags);
    public bool HasAttributes => GetHasAttributes(_flags);

    public static DeclaredSymbolInfo Create(
        StringTable stringTable,
        string name,
        string? nameSuffix,
        string? containerDisplayName,
        string fullyQualifiedContainerName,
        bool isPartial,
        bool hasAttributes,
        DeclaredSymbolInfoKind kind,
        Accessibility accessibility,
        TextSpan span,
        ImmutableArray<string> inheritanceNames,
        bool isNestedType = false,
        int parameterCount = 0,
        int typeParameterCount = 0)
    {
        // Max value that we can store depending on how many bits we have to store that particular value in.
        const uint Max5BitValue = 0b11111;
        const uint Max4BitValue = 0b1111;

        Contract.ThrowIfTrue((uint)accessibility > Max4BitValue);
        Contract.ThrowIfTrue((uint)kind > Max5BitValue);

        parameterCount = Math.Min(parameterCount, (byte)Max4BitValue);
        typeParameterCount = Math.Min(typeParameterCount, (byte)Max4BitValue);

        var flags =
            (uint)kind |
            ((uint)accessibility << 5) |
            ((uint)parameterCount << 9) |
            ((uint)typeParameterCount << 13) |
            ((isNestedType ? 1u : 0u) << 17) |
            ((isPartial ? 1u : 0u) << 18) |
            ((hasAttributes ? 1u : 0u) << 19);

#pragma warning disable CS0618 // Type or member is obsolete
        return new DeclaredSymbolInfo(
            Intern(stringTable, name),
            Intern(stringTable, nameSuffix),
            Intern(stringTable, containerDisplayName),
            Intern(stringTable, fullyQualifiedContainerName),
            span,
            inheritanceNames,
            flags);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    [return: NotNullIfNotNull(nameof(name))]
    public static string? Intern(StringTable stringTable, string? name)
        => name == null ? null : stringTable.Add(name);

    private static DeclaredSymbolInfoKind GetKind(uint flags)
        => (DeclaredSymbolInfoKind)(flags & Lower5BitMask);

    private static Accessibility GetAccessibility(uint flags)
        => (Accessibility)((flags >> 5) & Lower4BitMask);

    private static byte GetParameterCount(uint flags)
        => (byte)((flags >> 9) & Lower4BitMask);

    private static byte GetTypeParameterCount(uint flags)
        => (byte)((flags >> 13) & Lower4BitMask);

    private static bool GetIsNestedType(uint flags)
        => ((flags >> 17) & 1) == 1;

    private static bool GetIsPartial(uint flags)
        => ((flags >> 18) & 1) == 1;

    private static bool GetHasAttributes(uint flags)
        => ((flags >> 19) & 1) == 1;

    internal void WriteTo(ObjectWriter writer)
    {
        writer.WriteString(Name);
        writer.WriteString(NameSuffix);
        writer.WriteString(ContainerDisplayName);
        writer.WriteString(FullyQualifiedContainerName);
        writer.WriteUInt32(_flags);
        writer.WriteInt32(Span.Start);
        writer.WriteInt32(Span.Length);
        writer.WriteArray(InheritanceNames, static (w, n) => w.WriteString(n));
    }

    internal static DeclaredSymbolInfo ReadFrom_ThrowsOnFailure(StringTable stringTable, ObjectReader reader)
    {
        var name = reader.ReadRequiredString();
        var nameSuffix = reader.ReadString();
        var containerDisplayName = reader.ReadString();
        var fullyQualifiedContainerName = reader.ReadRequiredString();
        var flags = reader.ReadUInt32();
        var spanStart = reader.ReadInt32();
        var spanLength = reader.ReadInt32();

        var inheritanceNames = reader.ReadArray(static r => r.ReadRequiredString());

        var span = new TextSpan(spanStart, spanLength);
        return Create(
            stringTable,
            name,
            nameSuffix,
            containerDisplayName,
            fullyQualifiedContainerName,
            GetIsPartial(flags),
            GetHasAttributes(flags),
            GetKind(flags),
            GetAccessibility(flags),
            span,
            inheritanceNames,
            GetIsNestedType(flags),
            GetParameterCount(flags),
            GetTypeParameterCount(flags));
    }

    public ISymbol? TryResolve(SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        if (root.FullSpan.Contains(this.Span))
        {
            var node = root.FindNode(this.Span);
            return semanticModel.GetDeclaredSymbol(node, cancellationToken);
        }
        else
        {
            var message =
$@"Invalid span in {nameof(DeclaredSymbolInfo)}.
{nameof(this.Span)} = {this.Span}
{nameof(root.FullSpan)} = {root.FullSpan}";

            FatalError.ReportAndCatch(new InvalidOperationException(message));

            return null;
        }
    }

    public override bool Equals(object? obj)
        => obj is DeclaredSymbolInfo info && Equals(info);

    public bool Equals(DeclaredSymbolInfo other)
        => Name == other.Name
           && NameSuffix == other.NameSuffix
           && ContainerDisplayName == other.ContainerDisplayName
           && FullyQualifiedContainerName == other.FullyQualifiedContainerName
           && Span.Equals(other.Span)
           && _flags == other._flags
           && InheritanceNames.SequenceEqual(other.InheritanceNames, arg: true, (s1, s2, _) => s1 == s2);

    public override int GetHashCode()
        => Hash.Combine(Name,
           Hash.Combine(NameSuffix,
           Hash.Combine(ContainerDisplayName,
           Hash.Combine(FullyQualifiedContainerName,
           Hash.Combine(Span.GetHashCode(),
           Hash.Combine((int)_flags,
           Hash.CombineValues(InheritanceNames)))))));
}
