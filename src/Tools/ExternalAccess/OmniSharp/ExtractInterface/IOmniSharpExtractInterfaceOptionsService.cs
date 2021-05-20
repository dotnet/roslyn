// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.ExtractInterface
{
    internal interface IOmniSharpExtractInterfaceOptionsService
    {
        // OmniSharp only uses these two arguments from the full IExtractInterfaceOptionsService
        Task<OmniSharpExtractInterfaceOptionsResult> GetExtractInterfaceOptionsAsync(
            List<ISymbol> extractableMembers,
            string defaultInterfaceName);
    }

    internal class OmniSharpExtractInterfaceOptionsResult
    {
        public enum OmniSharpExtractLocation
        {
            SameFile,
            NewFile
        }

        public bool IsCancelled { get; }
        public ImmutableArray<ISymbol> IncludedMembers { get; }
        public string InterfaceName { get; }
        public string FileName { get; }
        public OmniSharpExtractLocation Location { get; }

        public OmniSharpExtractInterfaceOptionsResult(bool isCancelled, ImmutableArray<ISymbol> includedMembers, string interfaceName, string fileName, OmniSharpExtractLocation location)
        {
            IsCancelled = isCancelled;
            IncludedMembers = includedMembers;
            InterfaceName = interfaceName;
            Location = location;
            FileName = fileName;
        }
    }
}
