// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.InlineParameterNameHints
{
    internal readonly struct InlineParameterHint
    {
        public readonly SymbolKey ParameterSymbolKey;
        public readonly string Name;
        public readonly int Position;

        public InlineParameterHint(SymbolKey parameterSymbolKey, string name, int position)
        {
            ParameterSymbolKey = parameterSymbolKey;
            Name = name;
            Position = position;
        }
    }

    internal interface IInlineParameterNameHintsService : ILanguageService
    {
        Task<ImmutableArray<InlineParameterHint>> GetInlineParameterNameHintsAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken);
    }
}
