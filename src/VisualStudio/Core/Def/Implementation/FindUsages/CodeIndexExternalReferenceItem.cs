// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.FindUsages
{
    internal partial class VisualStudioFindSymbolMonikerUsagesService
    {
        private class CodeIndexExternalReferenceItem : ExternalReferenceItem
        {
            private readonly VisualStudioFindSymbolMonikerUsagesService _service;
            public readonly JObject ResultObject;

            public CodeIndexExternalReferenceItem(
                VisualStudioFindSymbolMonikerUsagesService service,
                DefinitionItem definition,
                JObject resultObject,
                string repository,
                ExternalScope scope,
                string projectName,
                string displayPath,
                LinePositionSpan span,
                string text) : base(definition, repository, scope, projectName, displayPath, span, text)
            {
                _service = service;
                ResultObject = resultObject;
            }

            public override bool TryNavigateTo(bool isPreview)
                => _service.TryNavigateTo(this, isPreview);
        }
    }
}
