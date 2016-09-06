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
                    case (int)ErrorCode.ERR_NoEntryPoint:
                    case (int)ErrorCode.WRN_InvalidMainSig:
                    case (int)ErrorCode.ERR_MultipleEntryPoints:
                    case (int)ErrorCode.WRN_MainIgnored:
                    case (int)ErrorCode.ERR_MainClassNotClass:
                    case (int)ErrorCode.WRN_MainCantBeGeneric:
                    case (int)ErrorCode.ERR_NoMainInClass:
                    case (int)ErrorCode.ERR_MainClassNotFound:
                        // no entry point related errors are live
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
