// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal readonly struct SignatureHelpState(int argumentIndex, int argumentCount, string? argumentName, ImmutableArray<string> argumentNames)
    {
        public readonly int ArgumentIndex = argumentIndex;
        public readonly int ArgumentCount = argumentCount;
        public readonly string? ArgumentName = argumentName;
        public readonly ImmutableArray<string> ArgumentNames = argumentNames;
    }
}
