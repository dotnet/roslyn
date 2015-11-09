// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public sealed class TestInteractiveEngine : IInteractiveEvaluator
    {
        internal event EventHandler<string> OnExecute;

        private readonly IContentType _contentType;
        private IInteractiveWindow _currentWindow;

        public TestInteractiveEngine(IContentTypeRegistryService contentTypeRegistryService)
        {
            _contentType = contentTypeRegistryService.GetContentType(TestContentTypeDefinition.ContentTypeName);
        }

        public IInteractiveWindow CurrentWindow
        {
            get
            {
                return _currentWindow;
            }

            set
            {
                _currentWindow = value;
            }
        }

        public void Dispose()
        {
        }

        public Task<ExecutionResult> InitializeAsync()
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public Task<ExecutionResult> ResetAsync(bool initialize = true)
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public bool CanExecuteCode(string text)
        {
            return true;
        }

        public Task<ExecutionResult> ExecuteCodeAsync(string text)
        {
            if (OnExecute != null)
            {
                OnExecute(this, text);
            }
            return Task.FromResult(ExecutionResult.Success);
        }

        public string FormatClipboard()
        {
            return null;
        }

        public void AbortExecution()
        {
        }

        public string GetConfiguration()
        {
            return "config";
        }

        public string GetPrompt()
        {
            return "> ";
        }
    }
}
