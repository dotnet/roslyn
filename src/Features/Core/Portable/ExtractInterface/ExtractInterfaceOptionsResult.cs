// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal class ExtractInterfaceOptionsResult
    {
        public static readonly ExtractInterfaceOptionsResult Cancelled = new ExtractInterfaceOptionsResult(isCancelled: true);

        public bool IsCancelled { get; }
        public IEnumerable<ISymbol> IncludedMembers { get; }
        public string InterfaceName { get; }
        public string FileName { get; }

        public ExtractInterfaceOptionsResult(bool isCancelled, IEnumerable<ISymbol> includedMembers, string interfaceName, string fileName)
        {
            this.IsCancelled = isCancelled;
            this.IncludedMembers = includedMembers;
            this.InterfaceName = interfaceName;
            this.FileName = fileName;
        }

        private ExtractInterfaceOptionsResult(bool isCancelled)
        {
            IsCancelled = isCancelled;
        }
    }
}
