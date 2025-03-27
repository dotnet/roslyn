// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public sealed class AnalyzerLoadFailureEventArgs : EventArgs
    {
        public enum FailureErrorCode
        {
            None = 0,
            UnableToLoadAnalyzer = 1,
            UnableToCreateAnalyzer = 2,
            NoAnalyzers = 3,
            ReferencesFramework = 4,
            ReferencesNewerCompiler = 5
        }

        /// <summary>
        /// If a specific analyzer failed to load the namespace-qualified name of its type, null otherwise.
        /// </summary>
        public string? TypeName { get; }

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
        public Exception? Exception { get; }

        /// <summary>
        /// If <see cref="ErrorCode"/> is <see cref="FailureErrorCode.ReferencesNewerCompiler"/>, returns the compiler version referenced by the analyzer assembly. Otherwise, returns null.
        /// </summary>
        public Version? ReferencedCompilerVersion { get; internal init; }

        public AnalyzerLoadFailureEventArgs(FailureErrorCode errorCode, string message, Exception? exceptionOpt = null, string? typeNameOpt = null)
        {
            if (errorCode <= FailureErrorCode.None || errorCode > FailureErrorCode.ReferencesNewerCompiler)
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
