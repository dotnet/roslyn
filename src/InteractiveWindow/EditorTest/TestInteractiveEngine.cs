// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.InteractiveWindow.UnitTests
{
    public class TestInteractiveEngine : IInteractiveEvaluator
    {
        private readonly IContentType _contentType;
        private IInteractiveWindow _currentWindow;

        public TestInteractiveEngine(IContentTypeRegistryService contentTypeRegistryService)
        {
            _contentType = contentTypeRegistryService.GetContentType(TestContentTypeDefinition.ContentTypeName);
        }

        public IContentType ContentType
        {
            get { return _contentType; }
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

        public Task<ExecutionResult> ResetAsync(ResetOptions options)
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public bool CanExecuteText(string text)
        {
            return true;
        }

        public Task<ExecutionResult> ExecuteTextAsync(string text)
        {
            return Task.FromResult(ExecutionResult.Success);
        }

        public string FormatClipboard()
        {
            return "";
        }

        public void AbortCommand()
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
