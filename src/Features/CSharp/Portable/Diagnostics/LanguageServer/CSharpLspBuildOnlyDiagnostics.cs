// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.CSharp.LanguageServer;

// Keep in sync with IsBuildOnlyDiagnostic
// src\Compilers\CSharp\Portable\Errors\ErrorFacts.cs
[LspBuildOnlyDiagnostics(
    LanguageNames.CSharp,
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
    "CS4009", // ErrorCode.ERR_NonTaskMainCantBeAsync:
    "CS4013", // ErrorCode.ERR_SpecialByRefInLambda:
    "CS1969", // ErrorCode.ERR_DynamicRequiredTypesMissing:
    "CS9026", // ErrorCode.ERR_CannotBeConvertedToUtf8:
    "CS9068", // ErrorCode.ERR_FileTypeNonUniquePath:
    "CS9144", // ErrorCode.ERR_InterceptorSignatureMismatch
    "CS9148", // ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter
    "CS9149", // ErrorCode.ERR_InterceptorMustNotHaveThisParameter
    "CS9153", // ErrorCode.ERR_DuplicateInterceptor
    "CS9154", // ErrorCode.WRN_InterceptorSignatureMismatch,
    "CS9155", // ErrorCode.ERR_InterceptorNotAccessible
    "CS9156", // ErrorCode.ERR_InterceptorScopedMismatch
    "CS9158", // ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor
    "CS9159", // ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor
    "CS9160", // ErrorCode.ERR_InterceptorCannotInterceptNameof
    "CS9163", // ErrorCode.ERR_SymbolDefinedInAssembly
    "CS9177", // ErrorCode.ERR_InterceptorArityNotCompatible
    "CS9178", // ErrorCode.ERR_InterceptorCannotBeGeneric
    "CS9207", // ErrorCode.ERR_InterceptableMethodMustBeOrdinary
    "CS8419", // ErrorCode.ERR_PossibleAsyncIteratorWithoutYield
    "CS8420", // ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait
    "CS9217", // ErrorCode.ERR_RefLocalAcrossAwait
    "CS9274", // ErrorCode.ERR_DataSectionStringLiteralHashCollision
    "CS9328", // ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync
    "CS8911", // ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported
    "CS7043", // ErrorCode.ERR_EncUpdateFailedMissingSymbol
    "CS7096", // ErrorCode.ERR_EncNoPIAReference
    "CS7101", // ErrorCode.ERR_EncReferenceToAddedMember
    "CS9346"  // ErrorCode.ERR_EncUpdateRequiresEmittingExplicitInterfaceImplementationNotSupportedByTheRuntime
    )]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpLspBuildOnlyDiagnostics() : ILspBuildOnlyDiagnostics
{
}
