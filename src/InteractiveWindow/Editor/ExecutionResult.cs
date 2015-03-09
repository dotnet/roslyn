// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// The result of command execution.  
    /// </summary>
    public struct ExecutionResult
    {
        public static readonly ExecutionResult Success = new ExecutionResult(true);
        public static readonly ExecutionResult Failure = new ExecutionResult(false);
        public static readonly Task<ExecutionResult> Succeeded = Task.FromResult(Success);
        public static readonly Task<ExecutionResult> Failed = Task.FromResult(Failure);

        private readonly bool _isSuccessful;

        public ExecutionResult(bool isSuccessful)
        {
            _isSuccessful = isSuccessful;
        }

        public bool IsSuccessful
        {
            get
            {
                return _isSuccessful;
            }
        }
    }
}