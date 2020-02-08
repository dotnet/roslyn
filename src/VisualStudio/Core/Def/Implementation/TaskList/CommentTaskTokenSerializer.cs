﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(IOptionPersister))]
    internal class CommentTaskTokenSerializer : IOptionPersister
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly ITaskList _taskList;

        private string _lastCommentTokenCache = null;

        [ImportingConstructor]
        public CommentTaskTokenSerializer(
            VisualStudioWorkspace workspace,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _workspace = workspace;

            // The SVsTaskList may not be available or doesn't actually implement ITaskList
            // in the "devenv /build" scenario
            _taskList = serviceProvider.GetService(typeof(SVsTaskList)) as ITaskList;

            // GetTaskTokenList is safe in the face of nulls
            _lastCommentTokenCache = GetTaskTokenList(_taskList);

            if (_taskList != null)
            {
                _taskList.PropertyChanged += OnPropertyChanged;
            }
        }

        public bool TryFetch(OptionKey optionKey, out object value)
        {
            value = string.Empty;
            if (optionKey != TodoCommentOptions.TokenList)
            {
                return false;
            }

            value = _lastCommentTokenCache;
            return true;
        }

        public bool TryPersist(OptionKey optionKey, object value)
        {
            // it never persists
            return false;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ITaskList.CommentTokens))
            {
                return;
            }

            var commentString = GetTaskTokenList(_taskList);

            var optionSet = _workspace.Options;
            var optionValue = optionSet.GetOption(TodoCommentOptions.TokenList);
            if (optionValue == commentString)
            {
                return;
            }

            // cache last result
            _lastCommentTokenCache = commentString;

            // let people to know that comment string has changed
            _workspace.SetOptions(optionSet.WithChangedOption(TodoCommentOptions.TokenList, _lastCommentTokenCache));
        }

        private static string GetTaskTokenList(ITaskList taskList)
        {
            var commentTokens = taskList?.CommentTokens;
            if (commentTokens == null || commentTokens.Count == 0)
            {
                return string.Empty;
            }

            var result = new List<string>();
            foreach (var commentToken in commentTokens)
            {
                if (string.IsNullOrWhiteSpace(commentToken.Text))
                {
                    continue;
                }

                result.Add($"{commentToken.Text}:{((int)commentToken.Priority).ToString()}");
            }

            return string.Join("|", result);
        }
    }
}
