// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal class ConvertToLSPSnippetService : ILanguageService
    {
        public string? GenerateLSPSnippet(TextChange textChange, List<(string, List<TextSpan>)> placeholders)
        {
            var textChangeText = textChange.NewText!;

            for (var i = 0; i < placeholders.Count; i++)
            {
                var (identifier, placeholderList) = placeholders[i];
                if (identifier.Length != 0)
                {
                    var newStr = $"${{{i}:{identifier}}}";
                    textChangeText = textChangeText.Replace(identifier, newStr);
                }
                else
                {
                    var location = placeholderList[0];
                    textChangeText = textChangeText.Insert(location.Start - textChange.Span.Start, $"$0");
                }
            }

            return textChangeText;
        }
    }
}
