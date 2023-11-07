// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineRename
{
    [ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.CSharp), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class CSharpEditorInlineRenameService(
        [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices,
        IGlobalOptionService globalOptions) : AbstractEditorInlineRenameService(refactorNotifyServices, globalOptions)
    {
    }
}
