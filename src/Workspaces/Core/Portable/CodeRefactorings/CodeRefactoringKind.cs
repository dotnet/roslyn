// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Most refactorings will have the <see cref="CodeRefactoringKind.Refactoring"/> kind. This allows us to draw
/// attention to Extract and Inline refactorings. 
/// </summary>
/// <remarks>
/// When new values are added here we should account for them in the `CodeActionHelpers` class.
/// </remarks>
internal enum CodeRefactoringKind
{
    Refactoring = 0,
    Extract = 1,
    Inline = 2,
}
