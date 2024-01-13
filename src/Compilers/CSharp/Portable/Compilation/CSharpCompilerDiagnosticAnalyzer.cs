// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
        protected override CommonMessageProvider MessageProvider
            => CodeAnalysis.CSharp.MessageProvider.Instance;

        internal override ImmutableArray<int> GetSupportedErrorCodes()
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

            return builder.ToImmutableAndFree();
        }
    }
}
