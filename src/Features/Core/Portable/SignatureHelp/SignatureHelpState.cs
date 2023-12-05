// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal readonly struct SignatureHelpState
    {
        public readonly int ArgumentIndex;
        public readonly int ArgumentCount;
        public readonly string? ArgumentName;
        public readonly ImmutableArray<string> ArgumentNames;

        public SignatureHelpState(int argumentIndex, int argumentCount, string? argumentName, ImmutableArray<string> argumentNames)
        {
            ArgumentIndex = argumentIndex;
            ArgumentCount = argumentCount;
            ArgumentName = argumentName;
            ArgumentNames = argumentNames;
        }
    }
}
