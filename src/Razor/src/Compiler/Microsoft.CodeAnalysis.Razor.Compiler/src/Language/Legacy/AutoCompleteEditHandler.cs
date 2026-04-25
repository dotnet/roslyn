// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal class AutoCompleteEditHandler : SpanEditHandler
{
    public static void SetupBuilder(SpanEditHandlerBuilder builder, Func<string, IEnumerable<Syntax.InternalSyntax.SyntaxToken>> tokenizer, bool autoCompleteAtEndOfSpan, out AutoCompleteStringAccessor autoCompleteStringAccessor)
    {
        var accessor = new AutoCompleteStringAccessor();
        autoCompleteStringAccessor = accessor;
        builder.Factory = (acceptedCharacters, tokenizer) => new AutoCompleteEditHandler(accessor)
        {
            AcceptedCharacters = acceptedCharacters,
            Tokenizer = tokenizer,
            AutoCompleteAtEndOfSpan = autoCompleteAtEndOfSpan,
        };
    }

    private static readonly int TypeHashCode = typeof(AutoCompleteEditHandler).GetHashCode();

    private readonly AutoCompleteStringAccessor _autoCompleteStringAccessor;

    private AutoCompleteEditHandler(AutoCompleteStringAccessor autoCompleteStringAccessor)
    {
        _autoCompleteStringAccessor = autoCompleteStringAccessor;
    }

    public required bool AutoCompleteAtEndOfSpan { get; init; }

    public string? AutoCompleteString => _autoCompleteStringAccessor.CanAcceptCloseBrace ? "}" : null;

    protected override PartialParseResultInternal CanAcceptChange(SyntaxNode target, SourceChange change)
    {
        if (((AutoCompleteAtEndOfSpan && IsAtEndOfSpan(target, change)) || IsAtEndOfFirstLine(target, change)) &&
            change.IsInsert &&
            ParserHelpers.IsNewLine(change.NewText) &&
            AutoCompleteString != null)
        {
            return PartialParseResultInternal.Rejected | PartialParseResultInternal.AutoCompleteBlock;
        }
        return PartialParseResultInternal.Rejected;
    }

    public override string ToString()
    {
        return base.ToString() + ",AutoComplete:[" + (AutoCompleteString ?? "<null>") + "]" + (AutoCompleteAtEndOfSpan ? ";AtEnd" : ";AtEOL");
    }

    public override bool Equals(object obj)
    {
        var other = obj as AutoCompleteEditHandler;
        return base.Equals(other) &&
            string.Equals(other.AutoCompleteString, AutoCompleteString, StringComparison.Ordinal) &&
            AutoCompleteAtEndOfSpan == other.AutoCompleteAtEndOfSpan;
    }

    public override int GetHashCode()
    {
        // Hash code should include only immutable properties but Equals also checks the type.
        var hashCodeCombiner = HashCodeCombiner.Start();
        hashCodeCombiner.Add(TypeHashCode);
        hashCodeCombiner.Add(AutoCompleteAtEndOfSpan);

        return hashCodeCombiner.CombinedHash;
    }

    internal class AutoCompleteStringAccessor
    {
        private bool? canCompleteBrace;

        public bool CanAcceptCloseBrace
        {
            get
            {
                // Throw if the value is not set.
                Debug.Assert(canCompleteBrace is not null);
                return canCompleteBrace!.Value;
            }
            set
            {
                Debug.Assert(canCompleteBrace is null);
                canCompleteBrace = value;
            }
        }
    }
}
