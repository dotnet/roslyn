// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal readonly struct FirstFixResult(bool upToDate, CodeFixCollection? codeFixCollection)
    {
        public readonly bool UpToDate = upToDate;
        public readonly CodeFixCollection? CodeFixCollection = codeFixCollection;

        [MemberNotNullWhen(true, nameof(CodeFixCollection))]
        public bool HasFix => CodeFixCollection != null;
    }
}
