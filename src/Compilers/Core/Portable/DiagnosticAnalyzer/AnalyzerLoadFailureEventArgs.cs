// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public sealed class AnalyzerLoadFailureEventArgs : EventArgs
    {
        public enum FailureErrorCode
        {
            None = 0,
            UnableToLoadAnalyzer = 1,
            UnableToCreateAnalyzer = 2,
            NoAnalyzers = 3
        }

        /// <summary>
        /// If a specific analyzer failed to load the namespace-qualified name of its type, null otherwise.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Error code.
        /// </summary>
        public FailureErrorCode ErrorCode { get; }

        /// <summary>
        /// Exception that was thrown while loading the analyzer. May be null.
        /// </summary>
        public Exception Exception { get; }

        public AnalyzerLoadFailureEventArgs(FailureErrorCode errorCode, string message, Exception exceptionOpt = null, string typeNameOpt = null)
        {
            if (errorCode <= FailureErrorCode.None || errorCode > FailureErrorCode.NoAnalyzers)
            {
                throw new ArgumentOutOfRangeException(nameof(errorCode));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            ErrorCode = errorCode;
            Message = message;
            TypeName = typeNameOpt;
            Exception = exceptionOpt;
        }
    }
}
