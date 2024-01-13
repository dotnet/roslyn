// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum ArgumentAnalysisResultKind : byte
    {
        Normal,
        Expanded,
        NoCorrespondingParameter,
        FirstInvalid = NoCorrespondingParameter,
        NoCorrespondingNamedParameter,
        DuplicateNamedArgument,
        RequiredParameterMissing,
        NameUsedForPositional,
        BadNonTrailingNamedArgument // if a named argument refers to a different position, all following arguments must be named
    }
}
