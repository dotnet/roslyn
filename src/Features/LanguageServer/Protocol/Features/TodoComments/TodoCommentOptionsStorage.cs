// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal static class TodoCommentOptionsStorage
    {
        public static readonly Option2<string> TokenList = new("TodoCommentOptions", "TokenList", TodoCommentOptions.Default.TokenList);

        public static TodoCommentOptions GetTodoCommentOptions(this IGlobalOptionService globalOptions)
            => new(globalOptions.GetOption(TokenList) ?? TodoCommentOptions.Default.TokenList);
    }
}
