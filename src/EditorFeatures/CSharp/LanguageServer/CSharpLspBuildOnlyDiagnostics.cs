﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServer
{
    // Keep in sync with IsBuildOnlyDiagnostic
    // src\Compilers\CSharp\Portable\Errors\ErrorFacts.cs
    [LspBuildOnlyDiagnostics(
        "CS1607", // ErrorCode.WRN_ALinkWarn:
        "CS0169", // ErrorCode.WRN_UnreferencedField:
        "CS0414", // ErrorCode.WRN_UnreferencedFieldAssg:
        "CS0067", // ErrorCode.WRN_UnreferencedEvent:
        "CS0649", // ErrorCode.WRN_UnassignedInternalField:
        "CS0656", // ErrorCode.ERR_MissingPredefinedMember:
        "CS0518", // ErrorCode.ERR_PredefinedTypeNotFound:
        "CS5001", // ErrorCode.ERR_NoEntryPoint:
        "CS0028", // ErrorCode.WRN_InvalidMainSig:
        "CS0017", // ErrorCode.ERR_MultipleEntryPoints:
        "CS7022", // ErrorCode.WRN_MainIgnored:
        "CS1556", // ErrorCode.ERR_MainClassNotClass:
        "CS0402", // ErrorCode.WRN_MainCantBeGeneric:
        "CS1558", // ErrorCode.ERR_NoMainInClass:
        "CS1555", // ErrorCode.ERR_MainClassNotFound:
        "CS8892", // ErrorCode.WRN_SyncAndAsyncEntryPoints:
        "CS0148", // ErrorCode.ERR_BadDelegateConstructor:
        "CS8078", // ErrorCode.ERR_InsufficientStack:
        "CS7038", // ErrorCode.ERR_ModuleEmitFailure:
        "CS0204", // ErrorCode.ERR_TooManyLocals:
        "CS0570", // ErrorCode.ERR_BindToBogus:
        "CS8004", // ErrorCode.ERR_ExportedTypeConflictsWithDeclaration:
        "CS8006", // ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration:
        "CS8005", // ErrorCode.ERR_ExportedTypesConflict:
        "CS8008", // ErrorCode.ERR_ForwardedTypeConflictsWithExportedType:
        "CS4007", // ErrorCode.ERR_ByRefTypeAndAwait:
        "CS8178", // ErrorCode.ERR_RefReturningCallAndAwait:
        "CS4013", // ErrorCode.ERR_SpecialByRefInLambda:
        "CS1969", // ErrorCode.ERR_DynamicRequiredTypesMissing:
        "CS8984", // ErrorCode.ERR_EncUpdateFailedDelegateTypeChanged:
        "CS9026", // ErrorCode.ERR_CannotBeConvertedToUtf8:
        "CS9068" // ErrorCode.ERR_FileTypeNonUniquePath:
        )]
    internal sealed class CSharpLspBuildOnlyDiagnostics : ILspBuildOnlyDiagnostics
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpLspBuildOnlyDiagnostics()
        {
        }
    }
}
