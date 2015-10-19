// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Diagnostics.CSharp
{
    /// <summary>
    /// DiagnosticAnalyzer for C# compiler's syntax/semantic/compilation diagnostics.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpCompilerDiagnosticAnalyzer : CompilerDiagnosticAnalyzer
    {
        internal override CommonMessageProvider MessageProvider
        {
            get
            {
                return CodeAnalysis.CSharp.MessageProvider.Instance;
            }
        }

        internal override ImmutableArray<int> GetSupportedErrorCodes()
        {
            var errorCodes = Enum.GetValues(typeof(ErrorCode));
            var builder = ImmutableArray.CreateBuilder<int>(errorCodes.Length);
            foreach (int errorCode in errorCodes)
            {
                switch (errorCode)
                {
                    case InternalErrorCode.Void:
                    case InternalErrorCode.Unknown:
                        continue;

                    case (int)ErrorCode.WRN_ALinkWarn:
                        // We don't support configuring WRN_ALinkWarn. See comments in method "CSharpDiagnosticFilter.Filter" for more details.
                        continue;

                    case (int)ErrorCode.WRN_UnreferencedField:
                    case (int)ErrorCode.WRN_UnreferencedFieldAssg:
                    case (int)ErrorCode.WRN_UnreferencedEvent:
                    case (int)ErrorCode.WRN_UnassignedInternalField:
                        // unused field. current live error doesn't support this.
                        continue;

                    case (int)ErrorCode.ERR_MissingPredefinedMember:
                        // make it build only error.
                        continue;

                    default:
                        builder.Add(errorCode);
                        break;
                }
            }

            return builder.ToImmutable();
        }
    }
}
