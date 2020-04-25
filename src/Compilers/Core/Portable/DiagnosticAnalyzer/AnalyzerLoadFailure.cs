// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public readonly struct AnalyzerLoadFailure
    {
        public enum ErrorCode
        {
            None = 0,

            /// <summary>
            /// Assembly containing the analyzer failed to load.
            /// <see cref="Exception"/> contains the load exception.
            /// </summary>
            AssemblyLoadFailure = 1,

            /// <summary>
            /// Analyzer type failed to load.
            /// <see cref="TypeName"/> contains the name of the type, <see cref="Exception"/> contains the load exception.
            /// </summary>
            TypeLoadFailure = 2,

            /// <summary>
            /// The analyzer type does not implement the required interface or abstract class.
            /// <see cref="TypeName"/> contains the name of the type.
            /// </summary>
            InvalidImplementation = 3
        }

        /// <summary>
        /// Error code.
        /// </summary>
        public ErrorCode Code { get; }

        /// <summary>
        /// If a specific analyzer failed to load the namespace-qualified name of its type, null otherwise.
        /// </summary>
        public string? TypeName { get; }

        /// <summary>
        /// Exception that was thrown while loading the analyzer assembly or the analyzer type, if any.
        /// </summary>
        public Exception? Exception { get; }

        public AnalyzerLoadFailure(ErrorCode code, string? typeName, Exception? exception)
        {
            Debug.Assert((typeName != null) == (code == ErrorCode.TypeLoadFailure || code == ErrorCode.InvalidImplementation));
            Debug.Assert((exception != null) == (code == ErrorCode.AssemblyLoadFailure || code == ErrorCode.TypeLoadFailure));

            Code = code;
            TypeName = typeName;
            Exception = exception;
        }
    }
}
