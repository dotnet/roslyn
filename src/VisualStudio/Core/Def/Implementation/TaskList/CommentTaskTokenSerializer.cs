// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [ExportOptionSerializer(TodoCommentOptions.OptionName), Shared]
    internal class CommentTaskTokenSerializer : IOptionSerializer
    {
        // it seems VS doesn't give a way to get notified if this setting gets changed.
        // so we will only read it once and keep using it until next vs run
        private readonly string _taskTokenList;

        [ImportingConstructor]
        public CommentTaskTokenSerializer(SVsServiceProvider serviceProvider)
        {
            var tokenInfo = serviceProvider.GetService(typeof(SVsTaskList)) as IVsCommentTaskInfo;

            // The SVsTaskList may not be available (e.g. during "devenv /build")
            _taskTokenList = tokenInfo != null ? GetTaskTokenList(tokenInfo) : string.Empty;
        }

        public bool TryFetch(OptionKey optionKey, out object value)
        {
            value = string.Empty;
            if (optionKey != TodoCommentOptions.TokenList)
            {
                return false;
            }

            value = _taskTokenList;
            return true;
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            // it never persists
            return false;
        }

        public static string GetTaskTokenList(IVsCommentTaskInfo tokenInfo)
        {
            int tokenCount;
            if (Succeeded(tokenInfo.TokenCount(out tokenCount)) && tokenCount > 0)
            {
                var tokens = new IVsCommentTaskToken[tokenCount];
                var tokensEnum = default(IVsEnumCommentTaskTokens);
                if (Succeeded(tokenInfo.EnumTokens(out tokensEnum)))
                {
                    uint count;
                    if (Succeeded(tokensEnum.Next((uint)tokenCount, tokens, out count)))
                    {
                        Contract.Requires(tokenCount == count);

                        string text;
                        var priority = new VSTASKPRIORITY[1];
                        var result = new List<string>();
                        foreach (var token in tokens)
                        {
                            if (Succeeded(token.Text(out text)) &&
                                Succeeded(token.Priority(priority)) &&
                                !string.IsNullOrWhiteSpace(text))
                            {
                                result.Add(string.Format("{0}:{1}", text, (int)priority[0]));
                            }
                        }

                        return string.Join("|", result);
                    }
                }
            }

            return string.Empty;
        }

        private static bool Succeeded(int hr)
        {
            return hr == VSConstants.S_OK;
        }
    }
}
