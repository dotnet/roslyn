// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    /// <summary>
    /// provide comment tokens to scan
    /// 
    /// we use this indirection so that we can get different tokens based on host
    /// </summary>
    [Export]
    internal class TodoCommentTokens
    {
        [ImportingConstructor]
        public TodoCommentTokens()
        {
        }

        private class TokenInfo
        {
            internal readonly string OptionText;
            internal readonly ImmutableArray<TodoCommentDescriptor> Tokens;

            public TokenInfo(string optionText, ImmutableArray<TodoCommentDescriptor> tokens)
            {
                this.OptionText = optionText;
                this.Tokens = tokens;
            }
        }

        private TokenInfo _lastTokenInfo;

        public ImmutableArray<TodoCommentDescriptor> GetTokens(Document document)
        {
            var optionText = document.Project.Solution.Options.GetOption(TodoCommentOptions.TokenList);

            var lastInfo = _lastTokenInfo;
            if (lastInfo != null && lastInfo.OptionText == optionText)
            {
                return lastInfo.Tokens;
            }

            var tokens = Parse(optionText);

            System.Threading.Interlocked.CompareExchange(ref _lastTokenInfo, new TokenInfo(optionText, tokens), lastInfo);

            return tokens;
        }
    }
}
