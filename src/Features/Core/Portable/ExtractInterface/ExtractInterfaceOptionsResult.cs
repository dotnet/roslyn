// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal class ExtractInterfaceOptionsResult
    {
        public enum ExtractLocation
        {
            SameFile,
            NewFile
        }

        public static readonly ExtractInterfaceOptionsResult Cancelled = new ExtractInterfaceOptionsResult(isCancelled: true);

        public bool IsCancelled { get; }
        public ImmutableArray<ISymbol> IncludedMembers { get; }
        public string InterfaceName { get; }
        public string FileName { get; }
        public ExtractLocation Location { get; }

        public ExtractInterfaceOptionsResult(bool isCancelled, ImmutableArray<ISymbol> includedMembers, string interfaceName, string fileName, ExtractLocation location)
        {
            IsCancelled = isCancelled;
            IncludedMembers = includedMembers;
            InterfaceName = interfaceName;
            Location = location;
            FileName = fileName;
        }

        private ExtractInterfaceOptionsResult(bool isCancelled)
        {
            IsCancelled = isCancelled;
        }
    }
}
