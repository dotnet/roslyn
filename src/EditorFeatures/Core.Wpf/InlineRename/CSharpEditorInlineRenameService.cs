// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.InlineRename
{
    [ExportLanguageService(typeof(IEditorInlineRenameService), LanguageNames.CSharp), Shared]
    internal class CSharpEditorInlineRenameService : AbstractEditorInlineRenameService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorInlineRenameService(
            IThreadingContext threadingContext,
            [ImportMany]IEnumerable<IRefactorNotifyService> refactorNotifyServices)
            : base(threadingContext, refactorNotifyServices)
        {
        }
    }
}
