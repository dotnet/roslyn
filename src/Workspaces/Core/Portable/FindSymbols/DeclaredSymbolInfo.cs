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
        public string Name { get; }
        public string ContainerDisplayName { get; }
        public string FullyQualifiedContainerName { get; }
        public DeclaredSymbolInfoKind Kind { get; }
        public TextSpan Span { get; }
        public ushort ParameterCount { get; }
        public ushort TypeParameterCount { get; }

        /// <summary>
        /// The names directly referenced in source that this type inherits from.
        /// </summary>
        public ImmutableArray<string> InheritanceNames { get; } 

        public DeclaredSymbolInfo(
            string name,
            string containerDisplayName,
            string fullyQualifiedContainerName,
            DeclaredSymbolInfoKind kind,
            TextSpan span,
            ImmutableArray<string> inheritanceNames,
            ushort parameterCount = 0, ushort typeParameterCount = 0)
            : this()
        {
            Name = name;
            ContainerDisplayName = containerDisplayName;
            FullyQualifiedContainerName = fullyQualifiedContainerName;
            Kind = kind;
            Span = span;
            ParameterCount = parameterCount;
            TypeParameterCount = typeParameterCount;
            InheritanceNames = inheritanceNames;
        }

        internal void WriteTo(ObjectWriter writer)
        {
            writer.WriteString(Name);
            writer.WriteString(ContainerDisplayName);
            writer.WriteString(FullyQualifiedContainerName);
            writer.WriteByte((byte)Kind);
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
            var immediateContainer = reader.ReadString();
            var entireContainer = reader.ReadString();
            var kind = (DeclaredSymbolInfoKind)reader.ReadByte();
            var spanStart = reader.ReadInt32();
            var spanLength = reader.ReadInt32();
            var parameterCount = reader.ReadUInt16();
            var typeParameterCount = reader.ReadUInt16();

            var inheritanceNamesLength = reader.ReadInt32();
            var builder = ImmutableArray.CreateBuilder<string>(inheritanceNamesLength);
            for (var i = 0; i < inheritanceNamesLength; i++)
            {
                builder.Add(reader.ReadString());
            }

            return new DeclaredSymbolInfo(
                name, immediateContainer, entireContainer, kind, new TextSpan(spanStart, spanLength),
                builder.MoveToImmutable(), parameterCount, typeParameterCount);
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