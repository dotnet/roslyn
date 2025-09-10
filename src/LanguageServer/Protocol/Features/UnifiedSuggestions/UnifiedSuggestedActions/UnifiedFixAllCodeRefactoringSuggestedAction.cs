//// Licensed to the .NET Foundation under one or more agreements.
//// The .NET Foundation licenses this file to you under the MIT license.
//// See the LICENSE file in the project root for more information.

//using Microsoft.CodeAnalysis.CodeActions;
//using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

//namespace Microsoft.CodeAnalysis.UnifiedSuggestions;

///// <summary>
///// Similar to FixAllCodeRefactoringSuggestedAction, but in a location that can be used by
///// both local Roslyn and LSP.
///// </summary>
//internal sealed class UnifiedRefactorAllCodeRefactoringSuggestedAction(
//    CodeAction codeAction,
//    CodeActionPriority codeActionPriority,
//    object provider,
//    IRefactorOrFixAllState fixAllState)
//    : UnifiedSuggestedAction(codeAction, codeActionPriority, provider, codeRefactoringKind: null, telemetryId: null)
//{
//    public IRefactorOrFixAllState FixAllState { get; } = fixAllState;
//}
