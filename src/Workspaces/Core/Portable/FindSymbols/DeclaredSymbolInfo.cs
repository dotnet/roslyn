// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        Struct
    }

    internal struct DeclaredSymbolInfo
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

        public DeclaredSymbolInfoKind Kind { get; }
        public Accessibility Accessibility { get; }
        public TextSpan Span { get; }
        public ushort ParameterCount { get; }
        public ushort TypeParameterCount { get; }

        /// <summary>
        /// The names directly referenced in source that this type inherits from.
        /// </summary>
        public ImmutableArray<string> InheritanceNames { get; } 

        public DeclaredSymbolInfo(
            string name,
            string nameSuffix,
            string containerDisplayName,
            string fullyQualifiedContainerName,
            DeclaredSymbolInfoKind kind,
            Accessibility accessibility,
            TextSpan span,
            ImmutableArray<string> inheritanceNames,
            ushort parameterCount = 0, ushort typeParameterCount = 0)
            : this()
        {
            Name = name;
            NameSuffix = nameSuffix;
            ContainerDisplayName = containerDisplayName;
            FullyQualifiedContainerName = fullyQualifiedContainerName;
            Kind = kind;
            Accessibility = accessibility;
            Span = span;
            ParameterCount = parameterCount;
            TypeParameterCount = typeParameterCount;
            InheritanceNames = inheritanceNames;
        }

        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteString(NameSuffix);
            writer.WriteString(ContainerDisplayName);
            writer.WriteString(FullyQualifiedContainerName);
            writer.WriteByte((byte)Kind);
            writer.WriteInt32((int)Accessibility);
            writer.WriteInt32(Span.Start);
            writer.WriteInt32(Span.Length);
            writer.WriteUInt16(ParameterCount);
            writer.WriteUInt16(TypeParameterCount);
            writer.WriteInt32(InheritanceNames.Length);

            foreach (var name in InheritanceNames)
            {
                writer.WriteString(name);
            }
        }

        internal static DeclaredSymbolInfo ReadFrom_ThrowsOnFailure(ObjectReader reader)
        {
            var name = reader.ReadString();
            var nameSuffix = reader.ReadString();
            var containerDisplayName = reader.ReadString();
            var fullyQualifiedContainerName = reader.ReadString();
            var kind = (DeclaredSymbolInfoKind)reader.ReadByte();
            var accessibility = (Accessibility)reader.ReadInt32();
            var spanStart = reader.ReadInt32();
            var spanLength = reader.ReadInt32();
            var parameterCount = reader.ReadUInt16();
            var typeParameterCount = reader.ReadUInt16();

            var inheritanceNamesLength = reader.ReadInt32();
            var builder = ArrayBuilder<string>.GetInstance(inheritanceNamesLength);
            for (var i = 0; i < inheritanceNamesLength; i++)
            {
                builder.Add(reader.ReadString());
            }

            var span = new TextSpan(spanStart, spanLength);
            return new DeclaredSymbolInfo(
                name: name,
                nameSuffix: nameSuffix,
                containerDisplayName: containerDisplayName,
                fullyQualifiedContainerName: fullyQualifiedContainerName,
                kind: kind, accessibility: accessibility, span: span,
                inheritanceNames: builder.ToImmutableAndFree(),
                parameterCount: parameterCount, 
                typeParameterCount: typeParameterCount);
        }

        public async Task<ISymbol> TryResolveAsync(Document document, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return TryResolve(semanticModel, cancellationToken);
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
    }
}