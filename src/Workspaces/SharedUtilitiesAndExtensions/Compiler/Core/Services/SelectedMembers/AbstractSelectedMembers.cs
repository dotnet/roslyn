// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSelectedMembers
    {
        protected static bool IsBeforeOrAfterNodeOnSameLine(
            SourceText text, SyntaxNode root, SyntaxNode member, int position)
        {
            var token = root.FindToken(position);
            if (token == member.GetFirstToken() &&
                position <= token.SpanStart &&
                text.AreOnSameLine(position, token.SpanStart))
            {
                return true;
            }

            if (token == member.GetLastToken() &&
                position >= token.Span.End &&
                text.AreOnSameLine(position, token.Span.End))
            {
                return true;
            }

            return false;
        }
    }
}
