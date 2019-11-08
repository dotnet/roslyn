// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private ImmutableArray<TodoCommentDescriptor> Parse(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return ImmutableArray<TodoCommentDescriptor>.Empty;
            }

            var tuples = data.Split('|');
            var result = new List<TodoCommentDescriptor>(tuples.Length);

            foreach (var tuple in tuples)
            {
                if (string.IsNullOrWhiteSpace(tuple))
                {
                    continue;
                }

                var pair = tuple.Split(':');

                if (pair.Length != 2 || string.IsNullOrWhiteSpace(pair[0]))
                {
                    continue;
                }

                if (!int.TryParse(pair[1], NumberStyles.None, CultureInfo.InvariantCulture, out var priority))
                {
                    continue;
                }

                result.Add(new TodoCommentDescriptor(pair[0].Trim(), priority));
            }

            return result.ToImmutableArray();
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
            if (lastInfo is { OptionText: optionText })
            {
                return lastInfo.Tokens;
            }

            var tokens = Parse(optionText);

            System.Threading.Interlocked.CompareExchange(ref _lastTokenInfo, new TokenInfo(optionText, tokens), lastInfo);

            return tokens;
        }
    }
}
