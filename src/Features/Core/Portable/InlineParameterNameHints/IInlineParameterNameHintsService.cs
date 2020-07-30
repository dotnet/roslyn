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
    internal readonly struct InlineParameterHint
    {
        public InlineParameterHint(SymbolKey key, string name, int position)
        {
            Key = key;
            Name = name;
            Position = position;
        }

        public readonly string Name { get; }
        public readonly int Position { get; }
        public readonly SymbolKey Key { get; }
    }

    internal interface IInlineParameterNameHintsService : ILanguageService
    {
        Task<IEnumerable<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
