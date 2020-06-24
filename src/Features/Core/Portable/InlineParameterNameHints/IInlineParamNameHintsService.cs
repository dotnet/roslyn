// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineParameterNameHints
{
    struct NameAndSpan
    {
        public string _name;
        public TextSpan _span;
    }

    internal interface IInlineParamNameHintsService : ILanguageService
    {
        Task<IEnumerable<NameAndSpan>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default);
    }
}
