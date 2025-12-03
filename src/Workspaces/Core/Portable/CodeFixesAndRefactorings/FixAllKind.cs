// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// An enum to distinguish if we are performing a Fix all occurrences for a code fix or a code refactoring.
/// </summary>
internal enum FixAllKind
{
    CodeFix,
    Refactoring
}
