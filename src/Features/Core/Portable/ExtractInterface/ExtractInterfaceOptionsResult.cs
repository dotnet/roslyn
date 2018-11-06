// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal abstract class ExtractInterfaceOptionsResult
    {
        public static readonly ExtractInterfaceOptionsResult Cancelled = new CanceledExtractInterfaceOptionsResult();

        public bool IsCancelled { get; }
        public IEnumerable<ISymbol> IncludedMembers { get; }
        public string InterfaceName { get; }

        public ExtractInterfaceOptionsResult(bool isCancelled, IEnumerable<ISymbol> includedMembers, string interfaceName)
        {
            this.IsCancelled = isCancelled;
            this.IncludedMembers = includedMembers;
            this.InterfaceName = interfaceName;
        }

        private ExtractInterfaceOptionsResult(bool isCancelled)
        {
            IsCancelled = isCancelled;
        }

        private class CanceledExtractInterfaceOptionsResult : ExtractInterfaceOptionsResult
        {
            public CanceledExtractInterfaceOptionsResult() : base(isCancelled: true)
            { }
        }
    }

    internal class ExtractInterfaceNewFileOptionsResult : ExtractInterfaceOptionsResult
    {
        public string FileName { get; }

        public ExtractInterfaceNewFileOptionsResult(bool isCancelled, IEnumerable<ISymbol> includedMembers, string interfaceName, string fileName)
            : base(isCancelled, includedMembers, interfaceName)
        {
            this.FileName = fileName;
        }
    }

    internal class ExtractInterfaceSameFileOptionsResult : ExtractInterfaceOptionsResult
    {
        public ExtractInterfaceSameFileOptionsResult(bool isCancelled, IEnumerable<ISymbol> includedMembers, string interfaceName)
            : base(isCancelled, includedMembers, interfaceName)
        { }
    }
}
