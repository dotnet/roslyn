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
    readonly struct InlineParameterHint
    {
        public InlineParameterHint(string name, int pos)
        {
            Name = name;
            Pos = pos;
        }
        public string Name { get; }
        public int Pos { get; }
    }

    internal interface IInlineParamNameHintsService : ILanguageService
    {
        Task<IEnumerable<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken = default);
    }
}
