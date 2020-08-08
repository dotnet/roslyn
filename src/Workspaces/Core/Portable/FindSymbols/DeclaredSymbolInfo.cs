// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal enum DeclaredSymbolInfoKind : byte
    {
        Class,
        Constant,
        Constructor,
        Delegate,
        Enum,
        EnumMember,
        Event,
        ExtensionMethod,
        Field,
        Indexer,
        Interface,
        Method,
        Module,
        Property,
        Struct,
    }

    internal readonly struct DeclaredSymbolInfo : IEquatable<DeclaredSymbolInfo>
    {
        /// <summary>
        /// The name to pattern match against, and to show in a final presentation layer.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// An optional suffix to be shown in a presentation layer appended to <see cref="Name"/>.
        /// Can be null.
        /// </summary>
        public string NameSuffix { get; }

        /// <summary>
        /// Container of the symbol that can be shown in a final presentation layer. 
        /// For example, the container of a type "KeyValuePair" might be 
        /// "System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;".  This can 
        /// then be shown with something like "type System.Collections.Generic.Dictionary&lt;TKey, TValue&gt;"
        /// to indicate where the symbol is located.
        /// </summary>
        public string ContainerDisplayName { get; }

        /// <summary>
        /// Dotted container name of the symbol, used for pattern matching.  For example
        /// The fully qualified container of a type "KeyValuePair" would be 
        /// "System.Collections.Generic.Dictionary" (note the lack of type parameters).
        /// This way someone can search for "D.KVP" and have the "D" part of the pattern
        /// match against this.  This should not be shown in a presentation layer.
        /// </summary>
        public string FullyQualifiedContainerName { get; }

        public TextSpan Span { get; }

        // Store the kind, accessibility, parameter-count, and type-parameter-count
        // in a single int.  Each gets 4 bits which is ample and gives us more space
        // for flags in the future.
        private readonly uint _flags;

        private const uint Lower4BitMask = 0b1111;

        public DeclaredSymbolInfoKind Kind => GetKind(_flags);
        public Accessibility Accessibility => GetAccessibility(_flags);
        public byte ParameterCount => GetParameterCount(_flags);
        public byte TypeParameterCount => GetTypeParameterCount(_flags);
        public bool IsNestedType => GetIsNestedType(_flags);

        /// <summary>
        /// The names directly referenced in source that this type inherits from.
        /// </summary>
        public ImmutableArray<string> InheritanceNames { get; }

        public DeclaredSymbolInfo(
            StringTable stringTable,
            string name,
            string nameSuffix,
            string containerDisplayName,
            string fullyQualifiedContainerName,
            DeclaredSymbolInfoKind kind,
            Accessibility accessibility,
            TextSpan span,
            ImmutableArray<string> inheritanceNames,
            bool isNestedType = false, int parameterCount = 0, int typeParameterCount = 0)
        {
            Name = Intern(stringTable, name);
            NameSuffix = Intern(stringTable, nameSuffix);
            ContainerDisplayName = Intern(stringTable, containerDisplayName);
            FullyQualifiedContainerName = Intern(stringTable, fullyQualifiedContainerName);
            Span = span;
            InheritanceNames = inheritanceNames;

            const uint MaxFlagValue = 0b1111;
            Contract.ThrowIfTrue((uint)accessibility > MaxFlagValue);
            Contract.ThrowIfTrue((uint)kind > MaxFlagValue);
            parameterCount = Math.Min(parameterCount, (byte)MaxFlagValue);
            typeParameterCount = Math.Min(typeParameterCount, (byte)MaxFlagValue);

            _flags =
                (uint)kind |
                ((uint)accessibility << 4) |
                ((uint)parameterCount << 8) |
                ((uint)typeParameterCount << 12) |
                ((isNestedType ? 1u : 0u) << 16);
        }

        public static string Intern(StringTable stringTable, string name)
            => name == null ? null : stringTable.Add(name);

        private static DeclaredSymbolInfoKind GetKind(uint flags)
            => (DeclaredSymbolInfoKind)(flags & Lower4BitMask);

        private static Accessibility GetAccessibility(uint flags)
            => (Accessibility)((flags >> 4) & Lower4BitMask);

        private static byte GetParameterCount(uint flags)
            => (byte)((flags >> 8) & Lower4BitMask);

        private static byte GetTypeParameterCount(uint flags)
            => (byte)((flags >> 12) & Lower4BitMask);

        private static bool GetIsNestedType(uint flags)
            => ((flags >> 16) & 1) == 1;

        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteString(NameSuffix);
            writer.WriteString(ContainerDisplayName);
            writer.WriteString(FullyQualifiedContainerName);
            writer.WriteUInt32(_flags);
            writer.WriteInt32(Span.Start);
            writer.WriteInt32(Span.Length);
            writer.WriteInt32(InheritanceNames.Length);

            foreach (var name in InheritanceNames)
            {
                writer.WriteString(name);
            }
        }

        internal static DeclaredSymbolInfo ReadFrom_ThrowsOnFailure(StringTable stringTable, ObjectReader reader)
        {
            var name = reader.ReadString();
            var nameSuffix = reader.ReadString();
            var containerDisplayName = reader.ReadString();
            var fullyQualifiedContainerName = reader.ReadString();
            var flags = reader.ReadUInt32();
            var spanStart = reader.ReadInt32();
            var spanLength = reader.ReadInt32();

            var inheritanceNamesLength = reader.ReadInt32();
            var builder = ArrayBuilder<string>.GetInstance(inheritanceNamesLength);
            for (var i = 0; i < inheritanceNamesLength; i++)
            {
                builder.Add(reader.ReadString());
            }

            var span = new TextSpan(spanStart, spanLength);
            return new DeclaredSymbolInfo(
                stringTable,
                name: name,
                nameSuffix: nameSuffix,
                containerDisplayName: containerDisplayName,
                fullyQualifiedContainerName: fullyQualifiedContainerName,
                kind: GetKind(flags),
                accessibility: GetAccessibility(flags),
                span: span,
                inheritanceNames: builder.ToImmutableAndFree(),
                parameterCount: GetParameterCount(flags),
                typeParameterCount: GetTypeParameterCount(flags));
        }

        public ISymbol TryResolve(SemanticModel semanticModel, CancellationToken cancellationToken)
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

                FatalError.ReportWithoutCrash(new InvalidOperationException(message));

                return null;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is DeclaredSymbolInfo info && Equals(info);
        }

        public bool Equals(DeclaredSymbolInfo other)
        {
            return Name == other.Name
                && NameSuffix == other.NameSuffix
                && ContainerDisplayName == other.ContainerDisplayName
                && FullyQualifiedContainerName == other.FullyQualifiedContainerName
                && Span.Equals(other.Span)
                && _flags == other._flags
                && InheritanceNames.Equals(other.InheritanceNames);
        }

        public override int GetHashCode()
        {
            var hashCode = 767621558;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NameSuffix);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContainerDisplayName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FullyQualifiedContainerName);
            hashCode = hashCode * -1521134295 + Span.GetHashCode();
            hashCode = hashCode * -1521134295 + _flags.GetHashCode();
            hashCode = hashCode * -1521134295 + InheritanceNames.GetHashCode();
            return hashCode;
        }
    }
}
