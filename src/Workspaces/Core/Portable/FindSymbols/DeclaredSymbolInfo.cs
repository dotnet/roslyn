// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
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

        public DeclaredSymbolInfo(string name, string containerDisplayName, string fullyQualifiedContainerName, DeclaredSymbolInfoKind kind, TextSpan span, ushort parameterCount = 0, ushort typeParameterCount = 0)
            : this()
        {
            Name = name;
            ContainerDisplayName = containerDisplayName;
            FullyQualifiedContainerName = fullyQualifiedContainerName;
            Kind = kind;
            Span = span;
            ParameterCount = parameterCount;
            TypeParameterCount = typeParameterCount;
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
        }

        internal static DeclaredSymbolInfo ReadFrom(ObjectReader reader)
        {
            try
            {
                var name = reader.ReadString();
                var immediateContainer = reader.ReadString();
                var entireContainer = reader.ReadString();
                var kind = (DeclaredSymbolInfoKind)reader.ReadByte();
                var spanStart = reader.ReadInt32();
                var spanLength = reader.ReadInt32();
                var parameterCount = reader.ReadUInt16();
                var typeParameterCount = reader.ReadUInt16();

                return new DeclaredSymbolInfo(name, immediateContainer, entireContainer, kind, new TextSpan(spanStart, spanLength), parameterCount, typeParameterCount);
            }
            catch
            {
                return default(DeclaredSymbolInfo);
            }
        }
    }
}
