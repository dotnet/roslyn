// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(IOptionPersister))]
    internal class CommentTaskTokenSerializer : ForegroundThreadAffinitizedObject, IOptionPersister
    {
        private readonly ServiceInitializer<ITaskList, SVsTaskList> _serviceInitializer;
        private readonly IOptionService _optionService;

        private string _lastCommentTokenCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CommentTaskTokenSerializer(
            VisualStudioWorkspace workspace, [Import(typeof(SAsyncServiceProvider))] Shell.IAsyncServiceProvider serviceProvider)
            : base(assertIsForeground: true)
        {
            _optionService = workspace.Services.GetService<IOptionService>();
            _serviceInitializer = new ServiceInitializer<ITaskList, SVsTaskList>(serviceProvider, service =>
            {
                // The SVsTaskList may not be available or doesn't actually implement ITaskList in the "devenv /build" scenario
                if (service != null)
                {
                    service.PropertyChanged += OnPropertyChanged;
                }

                // GetTaskTokenList is safe in the face of nulls
                _lastCommentTokenCache = GetTaskTokenList(service);

                return Task.CompletedTask;
            }, uiThreadRequired: true);
        }

        public Task PrefetchAsync(CancellationToken cancellationToken)
        {
            return _serviceInitializer.GetServiceAsync(cancellationToken);
        }

        public bool TryFetch(OptionKey optionKey, out object value)
        {
            _serviceInitializer.Ensure(CancellationToken.None);

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
            AssertIsForeground();

            if (e.PropertyName != nameof(ITaskList.CommentTokens))
            {
                return;
            }

            var commentString = GetTaskTokenList(_serviceInitializer.GetService(CancellationToken.None));

            var optionSet = _optionService.GetOptions();
            var optionValue = optionSet.GetOption(TodoCommentOptions.TokenList);
            if (optionValue == commentString)
            {
                return;
            }

            // cache last result
            _lastCommentTokenCache = commentString;

            // let people to know that comment string has changed
            _optionService.SetOptions(optionSet.WithChangedOption(TodoCommentOptions.TokenList, _lastCommentTokenCache));
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
