// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptFormattingInteractionServiceWrapper
    {
        private readonly IFormattingInteractionService _underlyingObject;

        private VSTypeScriptFormattingInteractionServiceWrapper(IFormattingInteractionService underlyingObject)
        {
            _underlyingObject = underlyingObject;
        }

        public static VSTypeScriptFormattingInteractionServiceWrapper Create(Document document)
            => new(document.Project.LanguageServices.GetRequiredService<IFormattingInteractionService>());

        public Task<IList<TextChange>> GetFormattingChangesAsync(Document document, TextSpan? textSpan, DocumentOptionSet? documentOptions, CancellationToken cancellationToken)
            => _underlyingObject.GetFormattingChangesAsync(document, textSpan, documentOptions, cancellationToken);
    }
}
