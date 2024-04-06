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

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [ExportStatelessXamlLspService(typeof(CodeActionsHandler)), Shared]
    internal class XamlCodeActionsHandler : CodeActionsHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlCodeActionsHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions) : base(codeFixService, codeRefactoringService, globalOptions)
        {
        }
    }

    [ExportStatelessXamlLspService(typeof(CodeActionResolveHandler)), Shared]
    internal class XamlCodeActionResolveHandler : CodeActionResolveHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlCodeActionResolveHandler(
            ICodeFixService codeFixService,
            ICodeRefactoringService codeRefactoringService,
            IGlobalOptionService globalOptions) : base(codeFixService, codeRefactoringService, globalOptions)
        {
        }
    }
}
