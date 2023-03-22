// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.CodeGeneration;

namespace Microsoft.CodeAnalysis.ExtractInterface
{
    internal sealed class ExtractInterfaceOptionsResult
    {
        public enum ExtractLocation
        {
            SameFile,
            NewFile
        }

        public static readonly ExtractInterfaceOptionsResult Cancelled = new(isCancelled: true);

        public bool IsCancelled { get; }
        public ImmutableArray<ISymbol> IncludedMembers { get; }
        public string InterfaceName { get; }
        public string FileName { get; }
        public ExtractLocation Location { get; }
        public CleanCodeGenerationOptionsProvider FallbackOptions { get; }

        public ExtractInterfaceOptionsResult(bool isCancelled, ImmutableArray<ISymbol> includedMembers, string interfaceName, string fileName, ExtractLocation location, CleanCodeGenerationOptionsProvider fallbackOptions)
        {
            IsCancelled = isCancelled;
            IncludedMembers = includedMembers;
            InterfaceName = interfaceName;
            Location = location;
            FallbackOptions = fallbackOptions;
            FileName = fileName;
        }

        private ExtractInterfaceOptionsResult(bool isCancelled)
            => IsCancelled = isCancelled;
    }
}
