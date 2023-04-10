// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics.CSharp
{
    /// <summary>
    /// DiagnosticAnalyzer for C# compiler's syntax/semantic/compilation diagnostics.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpCompilerDiagnosticAnalyzer : CompilerDiagnosticAnalyzer
    {
        private ImmutableArray<int> _supportedErrorCodes;

        internal override CommonMessageProvider MessageProvider
        {
            get
            {
                return CodeAnalysis.CSharp.MessageProvider.Instance;
            }
        }

        internal override ImmutableArray<int> GetSupportedErrorCodes()
        {
            var current = _supportedErrorCodes;
            if (current.IsDefault)
            {
                var errorCodes = Enum.GetValues(typeof(ErrorCode));
                var builder = ArrayBuilder<int>.GetInstance(errorCodes.Length);
                foreach (ErrorCode errorCode in errorCodes)
                {
                    // Compiler diagnostic analyzer does not support build-only diagnostics.
                    if (!ErrorFacts.IsBuildOnlyDiagnostic(errorCode) &&
                        errorCode is not (ErrorCode.Void or ErrorCode.Unknown))
                    {
                        builder.Add((int)errorCode);
                    }
                }

                ImmutableInterlocked.InterlockedCompareExchange(
                    ref _supportedErrorCodes,
                    builder.ToImmutableAndFree(),
                    current);
            }

            Debug.Assert(!_supportedErrorCodes.IsDefault);
            return _supportedErrorCodes;
        }
    }
}
