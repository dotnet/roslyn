// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting;

internal sealed class IndentUserSettingsFormattingRule : BaseFormattingRule
{
    private readonly CSharpSyntaxFormattingOptions _options;

    public IndentUserSettingsFormattingRule()
        : this(CSharpSyntaxFormattingOptions.Default)
    {
    }

    private IndentUserSettingsFormattingRule(CSharpSyntaxFormattingOptions options)
    {
        _options = options;
    }

    public override AbstractFormattingRule WithOptions(SyntaxFormattingOptions options)
    {
        var newOptions = options as CSharpSyntaxFormattingOptions ?? CSharpSyntaxFormattingOptions.Default;

        if (_options.Indentation.HasFlag(IndentationPlacement.Braces) == newOptions.Indentation.HasFlag(IndentationPlacement.Braces))
        {
            return this;
        }

        return new IndentUserSettingsFormattingRule(newOptions);
    }

    public override void AddIndentBlockOperations(List<IndentBlockOperation> list, SyntaxNode node, in NextIndentBlockOperationAction nextOperation)
    {
        nextOperation.Invoke();

        var bracePair = node.GetBracePair();

        // don't put block indentation operation if the block only contains lambda expression body block
        if (node.IsLambdaBodyBlock() || !bracePair.IsValidBracketOrBracePair())
        {
            return;
        }

        if (_options.Indentation.HasFlag(IndentationPlacement.Braces))
        {
            AddIndentBlockOperation(list, bracePair.openBrace, bracePair.openBrace, bracePair.openBrace.Span);
            AddIndentBlockOperation(list, bracePair.closeBrace, bracePair.closeBrace, bracePair.closeBrace.Span);
        }
    }
}
