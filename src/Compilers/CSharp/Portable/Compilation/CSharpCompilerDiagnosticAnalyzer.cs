// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                    case (int)ErrorCode.ERR_PredefinedTypeNotFound:
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
                    case (int)ErrorCode.WRN_SyncAndAsyncEntryPoints:
                        // no entry point related errors are live
                        continue;
                    case (int)ErrorCode.ERR_BadDelegateConstructor:
                    case (int)ErrorCode.ERR_InsufficientStack:
                    case (int)ErrorCode.ERR_ModuleEmitFailure:
                    case (int)ErrorCode.ERR_TooManyLocals:
                    case (int)ErrorCode.ERR_BindToBogus:
                    case (int)ErrorCode.ERR_ExportedTypeConflictsWithDeclaration:
                    case (int)ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration:
                    case (int)ErrorCode.ERR_ExportedTypesConflict:
                    case (int)ErrorCode.ERR_ForwardedTypeConflictsWithExportedType:
                    case (int)ErrorCode.ERR_ByRefTypeAndAwait:
                    case (int)ErrorCode.ERR_RefReturningCallAndAwait:
                    case (int)ErrorCode.ERR_SpecialByRefInLambda:
                    case (int)ErrorCode.ERR_DynamicRequiredTypesMissing:
                        // known build only errors which GetDiagnostics doesn't produce
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
