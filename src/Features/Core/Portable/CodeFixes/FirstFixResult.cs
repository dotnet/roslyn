// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal readonly struct FirstFixResult
    {
        public readonly bool UpToDate;
        public readonly CodeFixCollection? CodeFixCollection;

        [MemberNotNullWhen(true, nameof(CodeFixCollection))]
        public bool HasFix => CodeFixCollection != null;

        public FirstFixResult(bool upToDate, CodeFixCollection? codeFixCollection)
        {
            UpToDate = upToDate;
            CodeFixCollection = codeFixCollection;
        }
    }
}
