// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler;

[ExportStatelessXamlLspService(typeof(CodeActionsHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XamlCodeActionsHandler(
    ICodeFixService codeFixService,
    ICodeRefactoringService codeRefactoringService,
    IGlobalOptionService globalOptions) : CodeActionsHandler(codeFixService, codeRefactoringService, globalOptions);

[ExportStatelessXamlLspService(typeof(CodeActionResolveHandler)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class XamlCodeActionResolveHandler(
    ICodeFixService codeFixService,
    ICodeRefactoringService codeRefactoringService,
    IGlobalOptionService globalOptions) : CodeActionResolveHandler(codeFixService, codeRefactoringService, globalOptions);
