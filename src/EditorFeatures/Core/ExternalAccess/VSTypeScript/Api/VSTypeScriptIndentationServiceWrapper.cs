// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable CS0618 // Type or member is obsolete (https://github.com/dotnet/roslyn/issues/35872)

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptIndentationServiceWrapper
    {
        private readonly IIndentationService _underlyingObject;

        private VSTypeScriptIndentationServiceWrapper(IIndentationService underlyingObject)
            => _underlyingObject = underlyingObject;

        public static VSTypeScriptIndentationServiceWrapper Create(Document document)
            => new VSTypeScriptIndentationServiceWrapper(document.Project.LanguageServices.GetRequiredService<IIndentationService>());

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "External access API.")]
        public async Task<VSTypeScriptIndentationResultWrapper?> GetDesiredIndentation(Document document, int lineNumber, CancellationToken cancellationToken)
        {
            var result = await _underlyingObject.GetDesiredIndentation(document, lineNumber, cancellationToken).ConfigureAwait(false);
            return result.HasValue ? new VSTypeScriptIndentationResultWrapper(result.Value) : (VSTypeScriptIndentationResultWrapper?)null;
        }
    }
}
