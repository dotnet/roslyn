// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.EncapsulateField;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EncapsulateField;

[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.CSharpContentType)]
[Name(PredefinedCommandHandlerNames.EncapsulateField)]
[Order(After = PredefinedCommandHandlerNames.DocumentationComments)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class EncapsulateFieldCommandHandler(
    IThreadingContext threadingContext,
    ITextBufferUndoManagerProvider undoManager,
    IGlobalOptionService globalOptions,
    IAsynchronousOperationListenerProvider listenerProvider) : AbstractEncapsulateFieldCommandHandler(threadingContext, undoManager, globalOptions, listenerProvider)
{
}
