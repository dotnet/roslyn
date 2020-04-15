// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptEditorFormattingServiceWrapper
    {
        private readonly IEditorFormattingService _underlyingObject;

        private VSTypeScriptEditorFormattingServiceWrapper(IEditorFormattingService underlyingObject)
            => _underlyingObject = underlyingObject;

        public static VSTypeScriptEditorFormattingServiceWrapper Create(Document document)
            => new VSTypeScriptEditorFormattingServiceWrapper(document.Project.LanguageServices.GetRequiredService<IEditorFormattingService>());

        public Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, CancellationToken cancellationToken)
            => _underlyingObject.GetFormattingChangesAsync(document, textSpan, cancellationToken);
    }
}
