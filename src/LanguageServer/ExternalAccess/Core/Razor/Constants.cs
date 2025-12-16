// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal static class Constants
{
    public const string RazorLanguageName = LanguageInfoProvider.RazorLanguageName;

    public const string CompleteComplexEditCommand = CompletionResultFactory.CompleteComplexEditCommand;

    public const string RunFixAllCodeActionCommandName = CodeActionsHandler.RunFixAllCodeActionCommandName;
    public const string RunNestedCodeActionCommandName = CodeActionsHandler.RunNestedCodeActionCommandName;
    public const string NestedCodeActionsPropertyName = nameof(CodeActionResolveData.NestedCodeActions);
    public const string CodeActionPathPropertyName = nameof(CodeActionResolveData.CodeActionPath);
    public const string FixAllFlavorsPropertyName = nameof(CodeActionResolveData.FixAllFlavors);
}
