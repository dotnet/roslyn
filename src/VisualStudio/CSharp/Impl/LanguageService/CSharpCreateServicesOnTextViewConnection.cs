// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets;

[Export(typeof(IWpfTextViewConnectionListener))]
[ContentType(ContentTypeNames.CSharpContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpCreateServicesOnTextViewConnection(
    VisualStudioWorkspace workspace,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider)
    : AbstractCreateServicesOnTextViewConnection(
        workspace, globalOptions, listenerProvider, LanguageNames.CSharp)
{
    protected override Task InitializeServiceForProjectWithOpenedDocumentAsync(Project project)
        => Task.CompletedTask;
}
