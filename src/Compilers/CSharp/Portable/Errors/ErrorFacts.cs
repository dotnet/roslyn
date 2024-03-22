// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static partial class ErrorFacts
    {
        private const string s_titleSuffix = "_Title";
        private const string s_descriptionSuffix = "_Description";
        private static readonly Lazy<ImmutableDictionary<ErrorCode, string>> s_categoriesMap = new Lazy<ImmutableDictionary<ErrorCode, string>>(CreateCategoriesMap);
        public static readonly ImmutableHashSet<string> NullableWarnings;

        static ErrorFacts()
        {
            ImmutableHashSet<string>.Builder nullableWarnings = ImmutableHashSet.CreateBuilder<string>();

            nullableWarnings.Add(GetId(ErrorCode.WRN_NullReferenceAssignment));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullReferenceReceiver));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullReferenceReturn));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullReferenceArgument));
            nullableWarnings.Add(GetId(ErrorCode.WRN_UninitializedNonNullableField));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInAssignment));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInArgument));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInArgumentForOutput));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullAsNonNullable));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullableValueTypeMayBeNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ThrowPossibleNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_UnboxPossibleNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen));

            nullableWarnings.Add(GetId(ErrorCode.WRN_ConvertingNullableToNonNullable));
            nullableWarnings.Add(GetId(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ParameterConditionallyDisallowsNull));

            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase));
            nullableWarnings.Add(GetId(ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullReferenceInitializer));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ShouldNotReturn));
            nullableWarnings.Add(GetId(ErrorCode.WRN_DoesNotReturnMismatch));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation));
            nullableWarnings.Add(GetId(ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride));
            nullableWarnings.Add(GetId(ErrorCode.WRN_MemberNotNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_MemberNotNullBadMember));
            nullableWarnings.Add(GetId(ErrorCode.WRN_MemberNotNullWhen));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ParameterDisallowsNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ParameterNotNullIfNotNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_ReturnNotNullIfNotNull));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor));
            nullableWarnings.Add(GetId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor));

            NullableWarnings = nullableWarnings.ToImmutable();
        }

        private static string GetId(ErrorCode errorCode)
        {
            return MessageProvider.Instance.GetIdForErrorCode((int)errorCode);
        }

        private static ImmutableDictionary<ErrorCode, string> CreateCategoriesMap()
        {
            var map = new Dictionary<ErrorCode, string>()
            {
                // { ERROR_CODE,    CATEGORY }
            };

            return map.ToImmutableDictionary();
        }

        internal static DiagnosticSeverity GetSeverity(ErrorCode code)
        {
            if (code == ErrorCode.Void)
            {
                return InternalDiagnosticSeverity.Void;
            }
            else if (code == ErrorCode.Unknown)
            {
                return InternalDiagnosticSeverity.Unknown;
            }
            else if (IsWarning(code))
            {
                return DiagnosticSeverity.Warning;
            }
            else if (IsInfo(code))
            {
                return DiagnosticSeverity.Info;
            }
            else if (IsHidden(code))
            {
                return DiagnosticSeverity.Hidden;
            }
            else
            {
                return DiagnosticSeverity.Error;
            }
        }

        /// <remarks>Don't call this during a parse--it loads resources</remarks>
        public static string GetMessage(MessageID code, CultureInfo culture)
        {
            string message = ResourceManager.GetString(code.ToString(), culture);
            Debug.Assert(!string.IsNullOrEmpty(message), code.ToString());
            return message;
        }

        /// <remarks>Don't call this during a parse--it loads resources</remarks>
        public static string GetMessage(ErrorCode code, CultureInfo culture)
        {
            string message = ResourceManager.GetString(code.ToString(), culture);
            Debug.Assert(!string.IsNullOrEmpty(message), code.ToString());
            return message;
        }

        public static LocalizableResourceString GetMessageFormat(ErrorCode code)
        {
            return new LocalizableResourceString(code.ToString(), ResourceManager, typeof(ErrorFacts));
        }

        public static LocalizableResourceString GetTitle(ErrorCode code)
        {
            return new LocalizableResourceString(code.ToString() + s_titleSuffix, ResourceManager, typeof(ErrorFacts));
        }

        public static LocalizableResourceString GetDescription(ErrorCode code)
        {
            return new LocalizableResourceString(code.ToString() + s_descriptionSuffix, ResourceManager, typeof(ErrorFacts));
        }

        public static string GetHelpLink(ErrorCode code)
        {
            return $"https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k({GetId(code)})";
        }

        public static string GetCategory(ErrorCode code)
        {
            string category;
            if (s_categoriesMap.Value.TryGetValue(code, out category))
            {
                return category;
            }

            return Diagnostic.CompilerDiagnosticCategory;
        }

        /// <remarks>Don't call this during a parse--it loads resources</remarks>
        public static string GetMessage(XmlParseErrorCode id, CultureInfo culture)
        {
            return ResourceManager.GetString(id.ToString(), culture);
        }

        private static System.Resources.ResourceManager s_resourceManager;
        private static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (s_resourceManager == null)
                {
                    s_resourceManager = new System.Resources.ResourceManager(typeof(CSharpResources).FullName, typeof(ErrorCode).GetTypeInfo().Assembly);
                }

                return s_resourceManager;
            }
        }

        internal static int GetWarningLevel(ErrorCode code)
        {
            if (IsInfo(code) || IsHidden(code))
            {
                // Info and hidden diagnostics should always be produced because some analyzers depend on them.
                return Diagnostic.InfoAndHiddenWarningLevel;
            }

            // Warning wave warnings (warning level > 4) should be documented in
            // docs/compilers/CSharp/Warnversion Warning Waves.md
            switch (code)
            {
                case ErrorCode.WRN_AddressOfInAsync:
                case ErrorCode.WRN_ByValArraySizeConstRequired:
                    // Warning level 8 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 8 (C# 12) and that can be reported for pre-existing code.
                    return 8;
                case ErrorCode.WRN_LowerCaseTypeName:
                    // Warning level 7 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 7 (C# 11) and that can be reported for pre-existing code.
                    return 7;
                case ErrorCode.WRN_PartialMethodTypeDifference:
                    // Warning level 6 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 6 (C# 10) and that can be reported for pre-existing code.
                    return 6;
                case ErrorCode.WRN_NubExprIsConstBool2:
                case ErrorCode.WRN_StaticInAsOrIs:
                case ErrorCode.WRN_PrecedenceInversion:
                case ErrorCode.WRN_UseDefViolationPropertyUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationFieldUnsupportedVersion:
                case ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion:
                case ErrorCode.WRN_UnassignedThisUnsupportedVersion:
                case ErrorCode.WRN_ParamUnassigned:
                case ErrorCode.WRN_UseDefViolationProperty:
                case ErrorCode.WRN_UseDefViolationField:
                case ErrorCode.WRN_UseDefViolationThisUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationOut:
                case ErrorCode.WRN_UseDefViolation:
                case ErrorCode.WRN_SyncAndAsyncEntryPoints:
                case ErrorCode.WRN_ParameterIsStaticClass:
                case ErrorCode.WRN_ReturnTypeIsStaticClass:
                    // Warning level 5 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 5 (C# 9) and that can be reported for pre-existing code.
                    return 5;
                case ErrorCode.WRN_InvalidMainSig:
                case ErrorCode.WRN_LowercaseEllSuffix:
                case ErrorCode.WRN_NewNotRequired:
                case ErrorCode.WRN_MainCantBeGeneric:
                case ErrorCode.WRN_ProtectedInSealed:
                case ErrorCode.WRN_UnassignedInternalField:
                case ErrorCode.WRN_MissingParamTag:
                case ErrorCode.WRN_MissingXMLComment:
                case ErrorCode.WRN_MissingTypeParamTag:
                case ErrorCode.WRN_InvalidVersionFormat:
                    return 4;
                case ErrorCode.WRN_UnreferencedEvent:
                case ErrorCode.WRN_DuplicateUsing:
                case ErrorCode.WRN_UnreferencedVar:
                case ErrorCode.WRN_UnreferencedField:
                case ErrorCode.WRN_UnreferencedVarAssg:
                case ErrorCode.WRN_UnreferencedLocalFunction:
                case ErrorCode.WRN_SequentialOnPartialClass:
                case ErrorCode.WRN_UnreferencedFieldAssg:
                case ErrorCode.WRN_AmbiguousXMLReference:
                case ErrorCode.WRN_PossibleMistakenNullStatement:
                case ErrorCode.WRN_EqualsWithoutGetHashCode:
                case ErrorCode.WRN_EqualityOpWithoutEquals:
                case ErrorCode.WRN_EqualityOpWithoutGetHashCode:
                case ErrorCode.WRN_IncorrectBooleanAssg:
                case ErrorCode.WRN_BitwiseOrSignExtend:
                case ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter:
                case ErrorCode.WRN_InvalidAssemblyName:
                case ErrorCode.WRN_UnifyReferenceBldRev:
                case ErrorCode.WRN_AssignmentToSelf:
                case ErrorCode.WRN_ComparisonToSelf:
                case ErrorCode.WRN_IsDynamicIsConfusing:
                case ErrorCode.WRN_DebugFullNameTooLong:
                case ErrorCode.WRN_PdbLocalNameTooLong:
                case ErrorCode.WRN_RecordEqualsWithoutGetHashCode:
                    return 3;
                case ErrorCode.WRN_NewRequired:
                case ErrorCode.WRN_NewOrOverrideExpected:
                case ErrorCode.WRN_UnreachableCode:
                case ErrorCode.WRN_UnreferencedLabel:
                case ErrorCode.WRN_NegativeArrayIndex:
                case ErrorCode.WRN_BadRefCompareLeft:
                case ErrorCode.WRN_BadRefCompareRight:
                case ErrorCode.WRN_PatternIsAmbiguous:
                case ErrorCode.WRN_PatternNotPublicOrNotInstance:
                case ErrorCode.WRN_PatternBadSignature:
                case ErrorCode.WRN_SameFullNameThisNsAgg:
                case ErrorCode.WRN_SameFullNameThisAggAgg:
                case ErrorCode.WRN_SameFullNameThisAggNs:
                case ErrorCode.WRN_GlobalAliasDefn:
                case ErrorCode.WRN_AlwaysNull:
                case ErrorCode.WRN_CmpAlwaysFalse:
                case ErrorCode.WRN_GotoCaseShouldConvert:
                case ErrorCode.WRN_NubExprIsConstBool:
                case ErrorCode.WRN_ExplicitImplCollision:
                case ErrorCode.WRN_DeprecatedSymbolStr:
                case ErrorCode.WRN_VacuousIntegralComp:
                case ErrorCode.WRN_AssignmentToLockOrDispose:
                case ErrorCode.WRN_DeprecatedCollectionInitAddStr:
                case ErrorCode.WRN_DeprecatedCollectionInitAdd:
                case ErrorCode.WRN_DuplicateParamTag:
                case ErrorCode.WRN_UnmatchedParamTag:
                case ErrorCode.WRN_UnprocessedXMLComment:
                case ErrorCode.WRN_InvalidSearchPathDir:
                case ErrorCode.WRN_UnifyReferenceMajMin:
                case ErrorCode.WRN_DuplicateTypeParamTag:
                case ErrorCode.WRN_UnmatchedTypeParamTag:
                case ErrorCode.WRN_UnmatchedParamRefTag:
                case ErrorCode.WRN_UnmatchedTypeParamRefTag:
                case ErrorCode.WRN_CantHaveManifestForModule:
                case ErrorCode.WRN_DynamicDispatchToConditionalMethod:
                case ErrorCode.WRN_NoSources:
                case ErrorCode.WRN_CLS_MeaninglessOnPrivateType:
                case ErrorCode.WRN_CLS_AssemblyNotCLS2:
                case ErrorCode.WRN_MainIgnored:
                case ErrorCode.WRN_UnqualifiedNestedTypeInCref:
                case ErrorCode.WRN_NoRuntimeMetadataVersion:
                    return 2;
                case ErrorCode.WRN_IsAlwaysTrue:
                case ErrorCode.WRN_IsAlwaysFalse:
                case ErrorCode.WRN_ByRefNonAgileField:
                case ErrorCode.WRN_VolatileByRef:
                case ErrorCode.WRN_FinalizeMethod:
                case ErrorCode.WRN_DeprecatedSymbol:
                case ErrorCode.WRN_ExternMethodNoImplementation:
                case ErrorCode.WRN_AttributeLocationOnBadDeclaration:
                case ErrorCode.WRN_InvalidAttributeLocation:
                case ErrorCode.WRN_NonObsoleteOverridingObsolete:
                case ErrorCode.WRN_CoClassWithoutComImport:
                case ErrorCode.WRN_ObsoleteOverridingNonObsolete:
                case ErrorCode.WRN_ExternCtorNoImplementation:
                case ErrorCode.WRN_WarningDirective:
                case ErrorCode.WRN_UnreachableGeneralCatch:
                case ErrorCode.WRN_DefaultValueForUnconsumedLocation:
                case ErrorCode.WRN_EmptySwitch:
                case ErrorCode.WRN_XMLParseError:
                case ErrorCode.WRN_BadXMLRef:
                case ErrorCode.WRN_BadXMLRefParamType:
                case ErrorCode.WRN_BadXMLRefReturnType:
                case ErrorCode.WRN_BadXMLRefSyntax:
                case ErrorCode.WRN_FailedInclude:
                case ErrorCode.WRN_InvalidInclude:
                case ErrorCode.WRN_XMLParseIncludeError:
                case ErrorCode.WRN_ALinkWarn:
                case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                case ErrorCode.WRN_CmdOptionConflictsSource:
                case ErrorCode.WRN_IllegalPragma:
                case ErrorCode.WRN_IllegalPPWarning:
                case ErrorCode.WRN_BadRestoreNumber:
                case ErrorCode.WRN_NonECMAFeature:
                case ErrorCode.WRN_ErrorOverride:
                case ErrorCode.WRN_MultiplePredefTypes:
                case ErrorCode.WRN_TooManyLinesForDebugger:
                case ErrorCode.WRN_CallOnNonAgileField:
                case ErrorCode.WRN_InvalidNumber:
                case ErrorCode.WRN_IllegalPPChecksum:
                case ErrorCode.WRN_EndOfPPLineExpected:
                case ErrorCode.WRN_ConflictingChecksum:
                case ErrorCode.WRN_DotOnDefault:
                case ErrorCode.WRN_BadXMLRefTypeVar:
                case ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA:
                case ErrorCode.WRN_MultipleRuntimeImplementationMatches:
                case ErrorCode.WRN_MultipleRuntimeOverrideMatches:
                case ErrorCode.WRN_FileAlreadyIncluded:
                case ErrorCode.WRN_NoConfigNotOnCommandLine:
                case ErrorCode.WRN_AnalyzerCannotBeCreated:
                case ErrorCode.WRN_NoAnalyzerInAssembly:
                case ErrorCode.WRN_UnableToLoadAnalyzer:
                case ErrorCode.WRN_DefineIdentifierRequired:
                case ErrorCode.WRN_CLS_NoVarArgs:
                case ErrorCode.WRN_CLS_BadArgType:
                case ErrorCode.WRN_CLS_BadReturnType:
                case ErrorCode.WRN_CLS_BadFieldPropType:
                case ErrorCode.WRN_CLS_BadIdentifierCase:
                case ErrorCode.WRN_CLS_OverloadRefOut:
                case ErrorCode.WRN_CLS_OverloadUnnamed:
                case ErrorCode.WRN_CLS_BadIdentifier:
                case ErrorCode.WRN_CLS_BadBase:
                case ErrorCode.WRN_CLS_BadInterfaceMember:
                case ErrorCode.WRN_CLS_NoAbstractMembers:
                case ErrorCode.WRN_CLS_NotOnModules:
                case ErrorCode.WRN_CLS_ModuleMissingCLS:
                case ErrorCode.WRN_CLS_AssemblyNotCLS:
                case ErrorCode.WRN_CLS_BadAttributeType:
                case ErrorCode.WRN_CLS_ArrayArgumentToAttribute:
                case ErrorCode.WRN_CLS_NotOnModules2:
                case ErrorCode.WRN_CLS_IllegalTrueInFalse:
                case ErrorCode.WRN_CLS_MeaninglessOnParam:
                case ErrorCode.WRN_CLS_MeaninglessOnReturn:
                case ErrorCode.WRN_CLS_BadTypeVar:
                case ErrorCode.WRN_CLS_VolatileField:
                case ErrorCode.WRN_CLS_BadInterface:
                case ErrorCode.WRN_UnobservedAwaitableExpression:
                case ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                case ErrorCode.WRN_DelaySignButNoKey:
                case ErrorCode.WRN_UnimplementedCommandLineSwitch:
                case ErrorCode.WRN_AsyncLacksAwaits:
                case ErrorCode.WRN_BadUILang:
                case ErrorCode.WRN_RefCultureMismatch:
                case ErrorCode.WRN_ConflictingMachineAssembly:
                case ErrorCode.WRN_FilterIsConstantTrue:
                case ErrorCode.WRN_FilterIsConstantFalse:
                case ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch:
                case ErrorCode.WRN_IdentifierOrNumericLiteralExpected:
                case ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName:
                case ErrorCode.WRN_AlignmentMagnitude:
                case ErrorCode.WRN_AttributeIgnoredWhenPublicSigning:
                case ErrorCode.WRN_TupleLiteralNameMismatch:
                case ErrorCode.WRN_WindowsExperimental:
                case ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable:
                case ErrorCode.WRN_TupleBinopLiteralNameMismatch:
                case ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter:
                case ErrorCode.WRN_ConvertingNullableToNonNullable:
                case ErrorCode.WRN_NullReferenceAssignment:
                case ErrorCode.WRN_NullReferenceReceiver:
                case ErrorCode.WRN_NullReferenceReturn:
                case ErrorCode.WRN_NullReferenceArgument:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial:
                case ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation:
                case ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList:
                case ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase:
                case ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation:
                case ErrorCode.WRN_UninitializedNonNullableField:
                case ErrorCode.WRN_NullabilityMismatchInAssignment:
                case ErrorCode.WRN_NullabilityMismatchInArgument:
                case ErrorCode.WRN_NullabilityMismatchInArgumentForOutput:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate:
                case ErrorCode.WRN_NullAsNonNullable:
                case ErrorCode.WRN_NullableValueTypeMayBeNull:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint:
                case ErrorCode.WRN_MissingNonNullTypesContextForAnnotation:
                case ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode:
                case ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint:
                case ErrorCode.WRN_SwitchExpressionNotExhaustive:
                case ErrorCode.WRN_IsTypeNamedUnderscore:
                case ErrorCode.WRN_GivenExpressionNeverMatchesPattern:
                case ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue:
                case ErrorCode.WRN_CaseConstantNamedUnderscore:
                case ErrorCode.WRN_ThrowPossibleNull:
                case ErrorCode.WRN_UnboxPossibleNull:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull:
                case ErrorCode.WRN_ImplicitCopyInReadOnlyMember:
                case ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage:
                case ErrorCode.WRN_UndecoratedCancellationTokenParameter:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint:
                case ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment:
                case ErrorCode.WRN_ParameterConditionallyDisallowsNull:
                case ErrorCode.WRN_NullReferenceInitializer:
                case ErrorCode.WRN_ShouldNotReturn:
                case ErrorCode.WRN_DoesNotReturnMismatch:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation:
                case ErrorCode.WRN_ConstOutOfRangeChecked:
                case ErrorCode.WRN_MemberNotNull:
                case ErrorCode.WRN_MemberNotNullBadMember:
                case ErrorCode.WRN_MemberNotNullWhen:
                case ErrorCode.WRN_GeneratorFailedDuringInitialization:
                case ErrorCode.WRN_GeneratorFailedDuringGeneration:
                case ErrorCode.WRN_ParameterDisallowsNull:
                case ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern:
                case ErrorCode.WRN_IsPatternAlways:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen:
                case ErrorCode.WRN_RecordNamedDisallowed:
                case ErrorCode.WRN_ParameterNotNullIfNotNull:
                case ErrorCode.WRN_ReturnNotNullIfNotNull:
                case ErrorCode.WRN_AnalyzerReferencesFramework:
                case ErrorCode.WRN_UnreadRecordParameter:
                case ErrorCode.WRN_DoNotCompareFunctionPointers:
                case ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName:
                case ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential:
                case ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter:
                case ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters:
                case ErrorCode.WRN_CompileTimeCheckedOverflow:
                case ErrorCode.WRN_MethGrpToNonDel:
                case ErrorCode.WRN_UseDefViolationPropertySupportedVersion:
                case ErrorCode.WRN_UseDefViolationFieldSupportedVersion:
                case ErrorCode.WRN_UseDefViolationThisSupportedVersion:
                case ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion:
                case ErrorCode.WRN_UnassignedThisSupportedVersion:
                case ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired:
                case ErrorCode.WRN_AnalyzerReferencesNewerCompiler:
                case ErrorCode.WRN_DuplicateAnalyzerReference:
                case ErrorCode.WRN_ScopedMismatchInParameterOfTarget:
                case ErrorCode.WRN_ScopedMismatchInParameterOfOverrideOrImplementation:
                case ErrorCode.WRN_ManagedAddr:
                case ErrorCode.WRN_EscapeVariable:
                case ErrorCode.WRN_EscapeStackAlloc:
                case ErrorCode.WRN_RefReturnNonreturnableLocal:
                case ErrorCode.WRN_RefReturnNonreturnableLocal2:
                case ErrorCode.WRN_RefReturnStructThis:
                case ErrorCode.WRN_RefAssignNarrower:
                case ErrorCode.WRN_MismatchedRefEscapeInTernary:
                case ErrorCode.WRN_RefReturnParameter:
                case ErrorCode.WRN_RefReturnScopedParameter:
                case ErrorCode.WRN_RefReturnParameter2:
                case ErrorCode.WRN_RefReturnScopedParameter2:
                case ErrorCode.WRN_RefReturnLocal:
                case ErrorCode.WRN_RefReturnLocal2:
                case ErrorCode.WRN_RefAssignReturnOnly:
                case ErrorCode.WRN_RefReturnOnlyParameter:
                case ErrorCode.WRN_RefReturnOnlyParameter2:
                case ErrorCode.WRN_RefAssignValEscapeWider:
                case ErrorCode.WRN_OptionalParamValueMismatch:
                case ErrorCode.WRN_ParamsArrayInLambdaOnly:
                case ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase:
                case ErrorCode.WRN_UnreadPrimaryConstructorParameter:
                case ErrorCode.WRN_InterceptorSignatureMismatch:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor:
                case ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer:
                case ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase:
                case ErrorCode.WRN_InlineArrayIndexerNotUsed:
                case ErrorCode.WRN_InlineArraySliceNotUsed:
                case ErrorCode.WRN_InlineArrayConversionOperatorNotUsed:
                case ErrorCode.WRN_InlineArrayNotSupportedByLanguage:
                case ErrorCode.WRN_BadArgRef:
                case ErrorCode.WRN_ArgExpectedRefOrIn:
                case ErrorCode.WRN_RefReadonlyNotVariable:
                case ErrorCode.WRN_ArgExpectedIn:
                case ErrorCode.WRN_OverridingDifferentRefness:
                case ErrorCode.WRN_HidingDifferentRefness:
                case ErrorCode.WRN_TargetDifferentRefness:
                case ErrorCode.WRN_RefReadonlyParameterDefaultValue:
                case ErrorCode.WRN_UseDefViolationRefField:
                case ErrorCode.WRN_Experimental:
                case ErrorCode.WRN_CollectionExpressionRefStructMayAllocate:
                case ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate:
                case ErrorCode.WRN_ConvertingLock:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor:

                    return 1;
                default:
                    return 0;
            }
            // Note: when adding a warning here, consider whether it should be registered as a nullability warning too
        }

        /// <summary>
        /// Returns true if this is a build-only diagnostic that is never reported from
        /// <see cref="SemanticModel.GetDiagnostics(Text.TextSpan?, System.Threading.CancellationToken)"/> API.
        /// Diagnostics generated during compilation phases such as lowering, emit, etc.
        /// are example of build-only diagnostics.
        /// </summary>
        internal static bool IsBuildOnlyDiagnostic(ErrorCode code)
        {
            switch (code)
            {
                case ErrorCode.WRN_ALinkWarn:
                case ErrorCode.WRN_UnreferencedField:
                case ErrorCode.WRN_UnreferencedFieldAssg:
                case ErrorCode.WRN_UnreferencedEvent:
                case ErrorCode.WRN_UnassignedInternalField:
                case ErrorCode.ERR_MissingPredefinedMember:
                case ErrorCode.ERR_PredefinedTypeNotFound:
                case ErrorCode.ERR_NoEntryPoint:
                case ErrorCode.WRN_InvalidMainSig:
                case ErrorCode.ERR_MultipleEntryPoints:
                case ErrorCode.WRN_MainIgnored:
                case ErrorCode.ERR_MainClassNotClass:
                case ErrorCode.WRN_MainCantBeGeneric:
                case ErrorCode.ERR_NoMainInClass:
                case ErrorCode.ERR_MainClassNotFound:
                case ErrorCode.WRN_SyncAndAsyncEntryPoints:
                case ErrorCode.ERR_BadDelegateConstructor:
                case ErrorCode.ERR_InsufficientStack:
                case ErrorCode.ERR_ModuleEmitFailure:
                case ErrorCode.ERR_TooManyLocals:
                case ErrorCode.ERR_BindToBogus:
                case ErrorCode.ERR_ExportedTypeConflictsWithDeclaration:
                case ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration:
                case ErrorCode.ERR_ExportedTypesConflict:
                case ErrorCode.ERR_ForwardedTypeConflictsWithExportedType:
                case ErrorCode.ERR_ByRefTypeAndAwait:
                case ErrorCode.ERR_RefReturningCallAndAwait:
                case ErrorCode.ERR_SpecialByRefInLambda:
                case ErrorCode.ERR_DynamicRequiredTypesMissing:
                case ErrorCode.ERR_CannotBeConvertedToUtf8:
                case ErrorCode.ERR_FileTypeNonUniquePath:
                case ErrorCode.ERR_InterceptorSignatureMismatch:
                case ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter:
                case ErrorCode.ERR_InterceptorMustNotHaveThisParameter:
                case ErrorCode.ERR_DuplicateInterceptor:
                case ErrorCode.WRN_InterceptorSignatureMismatch:
                case ErrorCode.ERR_InterceptorNotAccessible:
                case ErrorCode.ERR_InterceptorScopedMismatch:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor:
                case ErrorCode.ERR_InterceptorCannotInterceptNameof:
                case ErrorCode.ERR_SymbolDefinedInAssembly:
                case ErrorCode.ERR_InterceptorArityNotCompatible:
                case ErrorCode.ERR_InterceptorCannotBeGeneric:
                case ErrorCode.ERR_InterceptableMethodMustBeOrdinary:
                case ErrorCode.ERR_PossibleAsyncIteratorWithoutYield:
                case ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait:
                    // Update src\EditorFeatures\CSharp\LanguageServer\CSharpLspBuildOnlyDiagnostics.cs
                    // whenever new values are added here.
                    return true;
                case ErrorCode.Void:
                case ErrorCode.Unknown:
                case ErrorCode.ERR_NoMetadataFile:
                case ErrorCode.FTL_MetadataCantOpenFile:
                case ErrorCode.ERR_NoTypeDef:
                case ErrorCode.ERR_OutputWriteFailed:
                case ErrorCode.ERR_BadBinaryOps:
                case ErrorCode.ERR_IntDivByZero:
                case ErrorCode.ERR_BadIndexLHS:
                case ErrorCode.ERR_BadIndexCount:
                case ErrorCode.ERR_BadUnaryOp:
                case ErrorCode.ERR_ThisInStaticMeth:
                case ErrorCode.ERR_ThisInBadContext:
                case ErrorCode.ERR_NoImplicitConv:
                case ErrorCode.ERR_NoExplicitConv:
                case ErrorCode.ERR_ConstOutOfRange:
                case ErrorCode.ERR_AmbigBinaryOps:
                case ErrorCode.ERR_AmbigUnaryOp:
                case ErrorCode.ERR_InAttrOnOutParam:
                case ErrorCode.ERR_ValueCantBeNull:
                case ErrorCode.ERR_NoExplicitBuiltinConv:
                case ErrorCode.FTL_DebugEmitFailure:
                case ErrorCode.ERR_BadVisReturnType:
                case ErrorCode.ERR_BadVisParamType:
                case ErrorCode.ERR_BadVisFieldType:
                case ErrorCode.ERR_BadVisPropertyType:
                case ErrorCode.ERR_BadVisIndexerReturn:
                case ErrorCode.ERR_BadVisIndexerParam:
                case ErrorCode.ERR_BadVisOpReturn:
                case ErrorCode.ERR_BadVisOpParam:
                case ErrorCode.ERR_BadVisDelegateReturn:
                case ErrorCode.ERR_BadVisDelegateParam:
                case ErrorCode.ERR_BadVisBaseClass:
                case ErrorCode.ERR_BadVisBaseInterface:
                case ErrorCode.ERR_EventNeedsBothAccessors:
                case ErrorCode.ERR_EventNotDelegate:
                case ErrorCode.ERR_InterfaceEventInitializer:
                case ErrorCode.ERR_BadEventUsage:
                case ErrorCode.ERR_ExplicitEventFieldImpl:
                case ErrorCode.ERR_CantOverrideNonEvent:
                case ErrorCode.ERR_AddRemoveMustHaveBody:
                case ErrorCode.ERR_AbstractEventInitializer:
                case ErrorCode.ERR_PossibleBadNegCast:
                case ErrorCode.ERR_ReservedEnumerator:
                case ErrorCode.ERR_AsMustHaveReferenceType:
                case ErrorCode.WRN_LowercaseEllSuffix:
                case ErrorCode.ERR_BadEventUsageNoField:
                case ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl:
                case ErrorCode.ERR_TypeParamMustBeIdentifier:
                case ErrorCode.ERR_MemberReserved:
                case ErrorCode.ERR_DuplicateParamName:
                case ErrorCode.ERR_DuplicateNameInNS:
                case ErrorCode.ERR_DuplicateNameInClass:
                case ErrorCode.ERR_NameNotInContext:
                case ErrorCode.ERR_AmbigContext:
                case ErrorCode.WRN_DuplicateUsing:
                case ErrorCode.ERR_BadMemberFlag:
                case ErrorCode.ERR_BadMemberProtection:
                case ErrorCode.WRN_NewRequired:
                case ErrorCode.WRN_NewNotRequired:
                case ErrorCode.ERR_CircConstValue:
                case ErrorCode.ERR_MemberAlreadyExists:
                case ErrorCode.ERR_StaticNotVirtual:
                case ErrorCode.ERR_OverrideNotNew:
                case ErrorCode.WRN_NewOrOverrideExpected:
                case ErrorCode.ERR_OverrideNotExpected:
                case ErrorCode.ERR_NamespaceUnexpected:
                case ErrorCode.ERR_NoSuchMember:
                case ErrorCode.ERR_BadSKknown:
                case ErrorCode.ERR_BadSKunknown:
                case ErrorCode.ERR_ObjectRequired:
                case ErrorCode.ERR_AmbigCall:
                case ErrorCode.ERR_BadAccess:
                case ErrorCode.ERR_MethDelegateMismatch:
                case ErrorCode.ERR_RetObjectRequired:
                case ErrorCode.ERR_RetNoObjectRequired:
                case ErrorCode.ERR_LocalDuplicate:
                case ErrorCode.ERR_AssgLvalueExpected:
                case ErrorCode.ERR_StaticConstParam:
                case ErrorCode.ERR_NotConstantExpression:
                case ErrorCode.ERR_NotNullConstRefField:
                case ErrorCode.ERR_LocalIllegallyOverrides:
                case ErrorCode.ERR_BadUsingNamespace:
                case ErrorCode.ERR_NoBreakOrCont:
                case ErrorCode.ERR_DuplicateLabel:
                case ErrorCode.ERR_NoConstructors:
                case ErrorCode.ERR_NoNewAbstract:
                case ErrorCode.ERR_ConstValueRequired:
                case ErrorCode.ERR_CircularBase:
                case ErrorCode.ERR_MethodNameExpected:
                case ErrorCode.ERR_ConstantExpected:
                case ErrorCode.ERR_V6SwitchGoverningTypeValueExpected:
                case ErrorCode.ERR_DuplicateCaseLabel:
                case ErrorCode.ERR_InvalidGotoCase:
                case ErrorCode.ERR_PropertyLacksGet:
                case ErrorCode.ERR_BadExceptionType:
                case ErrorCode.ERR_BadEmptyThrow:
                case ErrorCode.ERR_BadFinallyLeave:
                case ErrorCode.ERR_LabelShadow:
                case ErrorCode.ERR_LabelNotFound:
                case ErrorCode.ERR_UnreachableCatch:
                case ErrorCode.ERR_ReturnExpected:
                case ErrorCode.WRN_UnreachableCode:
                case ErrorCode.ERR_SwitchFallThrough:
                case ErrorCode.WRN_UnreferencedLabel:
                case ErrorCode.ERR_UseDefViolation:
                case ErrorCode.WRN_UnreferencedVar:
                case ErrorCode.ERR_UseDefViolationField:
                case ErrorCode.ERR_UnassignedThisUnsupportedVersion:
                case ErrorCode.ERR_AmbigQM:
                case ErrorCode.ERR_InvalidQM:
                case ErrorCode.ERR_NoBaseClass:
                case ErrorCode.ERR_BaseIllegal:
                case ErrorCode.ERR_ObjectProhibited:
                case ErrorCode.ERR_ParamUnassigned:
                case ErrorCode.ERR_InvalidArray:
                case ErrorCode.ERR_ExternHasBody:
                case ErrorCode.ERR_AbstractAndExtern:
                case ErrorCode.ERR_BadAttributeParamType:
                case ErrorCode.ERR_BadAttributeArgument:
                case ErrorCode.WRN_IsAlwaysTrue:
                case ErrorCode.WRN_IsAlwaysFalse:
                case ErrorCode.ERR_LockNeedsReference:
                case ErrorCode.ERR_NullNotValid:
                case ErrorCode.ERR_UseDefViolationThisUnsupportedVersion:
                case ErrorCode.ERR_ArgsInvalid:
                case ErrorCode.ERR_AssgReadonly:
                case ErrorCode.ERR_RefReadonly:
                case ErrorCode.ERR_PtrExpected:
                case ErrorCode.ERR_PtrIndexSingle:
                case ErrorCode.WRN_ByRefNonAgileField:
                case ErrorCode.ERR_AssgReadonlyStatic:
                case ErrorCode.ERR_RefReadonlyStatic:
                case ErrorCode.ERR_AssgReadonlyProp:
                case ErrorCode.ERR_IllegalStatement:
                case ErrorCode.ERR_BadGetEnumerator:
                case ErrorCode.ERR_AbstractBaseCall:
                case ErrorCode.ERR_RefProperty:
                case ErrorCode.ERR_ManagedAddr:
                case ErrorCode.ERR_BadFixedInitType:
                case ErrorCode.ERR_FixedMustInit:
                case ErrorCode.ERR_InvalidAddrOp:
                case ErrorCode.ERR_FixedNeeded:
                case ErrorCode.ERR_FixedNotNeeded:
                case ErrorCode.ERR_UnsafeNeeded:
                case ErrorCode.ERR_OpTFRetType:
                case ErrorCode.ERR_OperatorNeedsMatch:
                case ErrorCode.ERR_BadBoolOp:
                case ErrorCode.ERR_MustHaveOpTF:
                case ErrorCode.WRN_UnreferencedVarAssg:
                case ErrorCode.ERR_CheckedOverflow:
                case ErrorCode.ERR_ConstOutOfRangeChecked:
                case ErrorCode.ERR_BadVarargs:
                case ErrorCode.ERR_ParamsMustBeCollection:
                case ErrorCode.ERR_IllegalArglist:
                case ErrorCode.ERR_IllegalUnsafe:
                case ErrorCode.ERR_AmbigMember:
                case ErrorCode.ERR_BadForeachDecl:
                case ErrorCode.ERR_ParamsLast:
                case ErrorCode.ERR_SizeofUnsafe:
                case ErrorCode.ERR_DottedTypeNameNotFoundInNS:
                case ErrorCode.ERR_FieldInitRefNonstatic:
                case ErrorCode.ERR_SealedNonOverride:
                case ErrorCode.ERR_CantOverrideSealed:
                case ErrorCode.ERR_VoidError:
                case ErrorCode.ERR_ConditionalOnOverride:
                case ErrorCode.ERR_PointerInAsOrIs:
                case ErrorCode.ERR_CallingFinalizeDeprecated:
                case ErrorCode.ERR_SingleTypeNameNotFound:
                case ErrorCode.ERR_NegativeStackAllocSize:
                case ErrorCode.ERR_NegativeArraySize:
                case ErrorCode.ERR_OverrideFinalizeDeprecated:
                case ErrorCode.ERR_CallingBaseFinalizeDeprecated:
                case ErrorCode.WRN_NegativeArrayIndex:
                case ErrorCode.WRN_BadRefCompareLeft:
                case ErrorCode.WRN_BadRefCompareRight:
                case ErrorCode.ERR_BadCastInFixed:
                case ErrorCode.ERR_StackallocInCatchFinally:
                case ErrorCode.ERR_VarargsLast:
                case ErrorCode.ERR_MissingPartial:
                case ErrorCode.ERR_PartialTypeKindConflict:
                case ErrorCode.ERR_PartialModifierConflict:
                case ErrorCode.ERR_PartialMultipleBases:
                case ErrorCode.ERR_PartialWrongTypeParams:
                case ErrorCode.ERR_PartialWrongConstraints:
                case ErrorCode.ERR_NoImplicitConvCast:
                case ErrorCode.ERR_PartialMisplaced:
                case ErrorCode.ERR_ImportedCircularBase:
                case ErrorCode.ERR_UseDefViolationOut:
                case ErrorCode.ERR_ArraySizeInDeclaration:
                case ErrorCode.ERR_InaccessibleGetter:
                case ErrorCode.ERR_InaccessibleSetter:
                case ErrorCode.ERR_InvalidPropertyAccessMod:
                case ErrorCode.ERR_DuplicatePropertyAccessMods:
                case ErrorCode.ERR_AccessModMissingAccessor:
                case ErrorCode.ERR_UnimplementedInterfaceAccessor:
                case ErrorCode.WRN_PatternIsAmbiguous:
                case ErrorCode.WRN_PatternNotPublicOrNotInstance:
                case ErrorCode.WRN_PatternBadSignature:
                case ErrorCode.ERR_FriendRefNotEqualToThis:
                case ErrorCode.WRN_SequentialOnPartialClass:
                case ErrorCode.ERR_BadConstType:
                case ErrorCode.ERR_NoNewTyvar:
                case ErrorCode.ERR_BadArity:
                case ErrorCode.ERR_BadTypeArgument:
                case ErrorCode.ERR_TypeArgsNotAllowed:
                case ErrorCode.ERR_HasNoTypeVars:
                case ErrorCode.ERR_NewConstraintNotSatisfied:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedRefType:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedValType:
                case ErrorCode.ERR_DuplicateGeneratedName:
                case ErrorCode.ERR_GlobalSingleTypeNameNotFound:
                case ErrorCode.ERR_NewBoundMustBeLast:
                case ErrorCode.ERR_TypeVarCantBeNull:
                case ErrorCode.ERR_DuplicateBound:
                case ErrorCode.ERR_ClassBoundNotFirst:
                case ErrorCode.ERR_BadRetType:
                case ErrorCode.ERR_DuplicateConstraintClause:
                case ErrorCode.ERR_CantInferMethTypeArgs:
                case ErrorCode.ERR_LocalSameNameAsTypeParam:
                case ErrorCode.ERR_AsWithTypeVar:
                case ErrorCode.ERR_BadIndexerNameAttr:
                case ErrorCode.ERR_AttrArgWithTypeVars:
                case ErrorCode.ERR_NewTyvarWithArgs:
                case ErrorCode.ERR_AbstractSealedStatic:
                case ErrorCode.WRN_AmbiguousXMLReference:
                case ErrorCode.WRN_VolatileByRef:
                case ErrorCode.ERR_ComImportWithImpl:
                case ErrorCode.ERR_ComImportWithBase:
                case ErrorCode.ERR_ImplBadConstraints:
                case ErrorCode.ERR_DottedTypeNameNotFoundInAgg:
                case ErrorCode.ERR_MethGrpToNonDel:
                case ErrorCode.ERR_BadExternAlias:
                case ErrorCode.ERR_ColColWithTypeAlias:
                case ErrorCode.ERR_AliasNotFound:
                case ErrorCode.ERR_SameFullNameAggAgg:
                case ErrorCode.ERR_SameFullNameNsAgg:
                case ErrorCode.WRN_SameFullNameThisNsAgg:
                case ErrorCode.WRN_SameFullNameThisAggAgg:
                case ErrorCode.WRN_SameFullNameThisAggNs:
                case ErrorCode.ERR_SameFullNameThisAggThisNs:
                case ErrorCode.ERR_ExternAfterElements:
                case ErrorCode.WRN_GlobalAliasDefn:
                case ErrorCode.ERR_SealedStaticClass:
                case ErrorCode.ERR_PrivateAbstractAccessor:
                case ErrorCode.ERR_ValueExpected:
                case ErrorCode.ERR_UnboxNotLValue:
                case ErrorCode.ERR_AnonMethGrpInForEach:
                case ErrorCode.ERR_BadIncDecRetType:
                case ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst:
                case ErrorCode.ERR_RefValBoundWithClass:
                case ErrorCode.ERR_NewBoundWithVal:
                case ErrorCode.ERR_RefConstraintNotSatisfied:
                case ErrorCode.ERR_ValConstraintNotSatisfied:
                case ErrorCode.ERR_CircularConstraint:
                case ErrorCode.ERR_BaseConstraintConflict:
                case ErrorCode.ERR_ConWithValCon:
                case ErrorCode.ERR_AmbigUDConv:
                case ErrorCode.WRN_AlwaysNull:
                case ErrorCode.ERR_OverrideWithConstraints:
                case ErrorCode.ERR_AmbigOverride:
                case ErrorCode.ERR_DecConstError:
                case ErrorCode.WRN_CmpAlwaysFalse:
                case ErrorCode.WRN_FinalizeMethod:
                case ErrorCode.ERR_ExplicitImplParams:
                case ErrorCode.WRN_GotoCaseShouldConvert:
                case ErrorCode.ERR_MethodImplementingAccessor:
                case ErrorCode.WRN_NubExprIsConstBool:
                case ErrorCode.WRN_ExplicitImplCollision:
                case ErrorCode.ERR_AbstractHasBody:
                case ErrorCode.ERR_ConcreteMissingBody:
                case ErrorCode.ERR_AbstractAndSealed:
                case ErrorCode.ERR_AbstractNotVirtual:
                case ErrorCode.ERR_StaticConstant:
                case ErrorCode.ERR_CantOverrideNonFunction:
                case ErrorCode.ERR_CantOverrideNonVirtual:
                case ErrorCode.ERR_CantChangeAccessOnOverride:
                case ErrorCode.ERR_CantChangeReturnTypeOnOverride:
                case ErrorCode.ERR_CantDeriveFromSealedType:
                case ErrorCode.ERR_AbstractInConcreteClass:
                case ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall:
                case ErrorCode.ERR_StaticConstructorWithAccessModifiers:
                case ErrorCode.ERR_RecursiveConstructorCall:
                case ErrorCode.ERR_ObjectCallingBaseConstructor:
                case ErrorCode.ERR_StructWithBaseConstructorCall:
                case ErrorCode.ERR_StructLayoutCycle:
                case ErrorCode.ERR_InterfacesCantContainFields:
                case ErrorCode.ERR_InterfacesCantContainConstructors:
                case ErrorCode.ERR_NonInterfaceInInterfaceList:
                case ErrorCode.ERR_DuplicateInterfaceInBaseList:
                case ErrorCode.ERR_CycleInInterfaceInheritance:
                case ErrorCode.ERR_HidingAbstractMethod:
                case ErrorCode.ERR_UnimplementedAbstractMethod:
                case ErrorCode.ERR_UnimplementedInterfaceMember:
                case ErrorCode.ERR_ObjectCantHaveBases:
                case ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface:
                case ErrorCode.ERR_InterfaceMemberNotFound:
                case ErrorCode.ERR_ClassDoesntImplementInterface:
                case ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct:
                case ErrorCode.ERR_MemberNameSameAsType:
                case ErrorCode.ERR_EnumeratorOverflow:
                case ErrorCode.ERR_CantOverrideNonProperty:
                case ErrorCode.ERR_NoGetToOverride:
                case ErrorCode.ERR_NoSetToOverride:
                case ErrorCode.ERR_PropertyCantHaveVoidType:
                case ErrorCode.ERR_PropertyWithNoAccessors:
                case ErrorCode.ERR_NewVirtualInSealed:
                case ErrorCode.ERR_ExplicitPropertyAddingAccessor:
                case ErrorCode.ERR_ExplicitPropertyMissingAccessor:
                case ErrorCode.ERR_ConversionWithInterface:
                case ErrorCode.ERR_ConversionWithBase:
                case ErrorCode.ERR_ConversionWithDerived:
                case ErrorCode.ERR_IdentityConversion:
                case ErrorCode.ERR_ConversionNotInvolvingContainedType:
                case ErrorCode.ERR_DuplicateConversionInClass:
                case ErrorCode.ERR_OperatorsMustBeStatic:
                case ErrorCode.ERR_BadIncDecSignature:
                case ErrorCode.ERR_BadUnaryOperatorSignature:
                case ErrorCode.ERR_BadBinaryOperatorSignature:
                case ErrorCode.ERR_BadShiftOperatorSignature:
                case ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators:
                case ErrorCode.ERR_CantOverrideBogusMethod:
                case ErrorCode.ERR_CantCallSpecialMethod:
                case ErrorCode.ERR_BadTypeReference:
                case ErrorCode.ERR_BadDestructorName:
                case ErrorCode.ERR_OnlyClassesCanContainDestructors:
                case ErrorCode.ERR_ConflictAliasAndMember:
                case ErrorCode.ERR_ConditionalOnSpecialMethod:
                case ErrorCode.ERR_ConditionalMustReturnVoid:
                case ErrorCode.ERR_DuplicateAttribute:
                case ErrorCode.ERR_ConditionalOnInterfaceMethod:
                case ErrorCode.ERR_OperatorCantReturnVoid:
                case ErrorCode.ERR_InvalidAttributeArgument:
                case ErrorCode.ERR_AttributeOnBadSymbolType:
                case ErrorCode.ERR_FloatOverflow:
                case ErrorCode.ERR_InvalidReal:
                case ErrorCode.ERR_ComImportWithoutUuidAttribute:
                case ErrorCode.ERR_InvalidNamedArgument:
                case ErrorCode.ERR_DllImportOnInvalidMethod:
                case ErrorCode.ERR_FieldCantBeRefAny:
                case ErrorCode.ERR_ArrayElementCantBeRefAny:
                case ErrorCode.WRN_DeprecatedSymbol:
                case ErrorCode.ERR_NotAnAttributeClass:
                case ErrorCode.ERR_BadNamedAttributeArgument:
                case ErrorCode.WRN_DeprecatedSymbolStr:
                case ErrorCode.ERR_DeprecatedSymbolStr:
                case ErrorCode.ERR_IndexerCantHaveVoidType:
                case ErrorCode.ERR_VirtualPrivate:
                case ErrorCode.ERR_ArrayInitToNonArrayType:
                case ErrorCode.ERR_ArrayInitInBadPlace:
                case ErrorCode.ERR_MissingStructOffset:
                case ErrorCode.WRN_ExternMethodNoImplementation:
                case ErrorCode.WRN_ProtectedInSealed:
                case ErrorCode.ERR_InterfaceImplementedByConditional:
                case ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic:
                case ErrorCode.ERR_IllegalRefParam:
                case ErrorCode.ERR_BadArgumentToAttribute:
                case ErrorCode.ERR_StructOffsetOnBadStruct:
                case ErrorCode.ERR_StructOffsetOnBadField:
                case ErrorCode.ERR_AttributeUsageOnNonAttributeClass:
                case ErrorCode.WRN_PossibleMistakenNullStatement:
                case ErrorCode.ERR_DuplicateNamedAttributeArgument:
                case ErrorCode.ERR_DeriveFromEnumOrValueType:
                case ErrorCode.ERR_DefaultMemberOnIndexedType:
                case ErrorCode.ERR_BogusType:
                case ErrorCode.ERR_CStyleArray:
                case ErrorCode.WRN_VacuousIntegralComp:
                case ErrorCode.ERR_AbstractAttributeClass:
                case ErrorCode.ERR_BadNamedAttributeArgumentType:
                case ErrorCode.WRN_AttributeLocationOnBadDeclaration:
                case ErrorCode.WRN_InvalidAttributeLocation:
                case ErrorCode.WRN_EqualsWithoutGetHashCode:
                case ErrorCode.WRN_EqualityOpWithoutEquals:
                case ErrorCode.WRN_EqualityOpWithoutGetHashCode:
                case ErrorCode.ERR_OutAttrOnRefParam:
                case ErrorCode.ERR_OverloadRefKind:
                case ErrorCode.ERR_LiteralDoubleCast:
                case ErrorCode.WRN_IncorrectBooleanAssg:
                case ErrorCode.ERR_ProtectedInStruct:
                case ErrorCode.ERR_InconsistentIndexerNames:
                case ErrorCode.ERR_ComImportWithUserCtor:
                case ErrorCode.ERR_FieldCantHaveVoidType:
                case ErrorCode.WRN_NonObsoleteOverridingObsolete:
                case ErrorCode.ERR_SystemVoid:
                case ErrorCode.ERR_ExplicitParamArrayOrCollection:
                case ErrorCode.WRN_BitwiseOrSignExtend:
                case ErrorCode.ERR_VolatileStruct:
                case ErrorCode.ERR_VolatileAndReadonly:
                case ErrorCode.ERR_AbstractField:
                case ErrorCode.ERR_BogusExplicitImpl:
                case ErrorCode.ERR_ExplicitMethodImplAccessor:
                case ErrorCode.WRN_CoClassWithoutComImport:
                case ErrorCode.ERR_ConditionalWithOutParam:
                case ErrorCode.ERR_AccessorImplementingMethod:
                case ErrorCode.ERR_AliasQualAsExpression:
                case ErrorCode.ERR_DerivingFromATyVar:
                case ErrorCode.ERR_DuplicateTypeParameter:
                case ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter:
                case ErrorCode.ERR_TypeVariableSameAsParent:
                case ErrorCode.ERR_UnifyingInterfaceInstantiations:
                case ErrorCode.ERR_TyVarNotFoundInConstraint:
                case ErrorCode.ERR_BadBoundType:
                case ErrorCode.ERR_SpecialTypeAsBound:
                case ErrorCode.ERR_BadVisBound:
                case ErrorCode.ERR_LookupInTypeVariable:
                case ErrorCode.ERR_BadConstraintType:
                case ErrorCode.ERR_InstanceMemberInStaticClass:
                case ErrorCode.ERR_StaticBaseClass:
                case ErrorCode.ERR_ConstructorInStaticClass:
                case ErrorCode.ERR_DestructorInStaticClass:
                case ErrorCode.ERR_InstantiatingStaticClass:
                case ErrorCode.ERR_StaticDerivedFromNonObject:
                case ErrorCode.ERR_StaticClassInterfaceImpl:
                case ErrorCode.ERR_OperatorInStaticClass:
                case ErrorCode.ERR_ConvertToStaticClass:
                case ErrorCode.ERR_ConstraintIsStaticClass:
                case ErrorCode.ERR_GenericArgIsStaticClass:
                case ErrorCode.ERR_ArrayOfStaticClass:
                case ErrorCode.ERR_IndexerInStaticClass:
                case ErrorCode.ERR_ParameterIsStaticClass:
                case ErrorCode.ERR_ReturnTypeIsStaticClass:
                case ErrorCode.ERR_VarDeclIsStaticClass:
                case ErrorCode.ERR_BadEmptyThrowInFinally:
                case ErrorCode.ERR_InvalidSpecifier:
                case ErrorCode.WRN_AssignmentToLockOrDispose:
                case ErrorCode.ERR_ForwardedTypeInThisAssembly:
                case ErrorCode.ERR_ForwardedTypeIsNested:
                case ErrorCode.ERR_CycleInTypeForwarder:
                case ErrorCode.ERR_AssemblyNameOnNonModule:
                case ErrorCode.ERR_InvalidFwdType:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType:
                case ErrorCode.ERR_DuplicateTypeForwarder:
                case ErrorCode.ERR_ExpectedSelectOrGroup:
                case ErrorCode.ERR_ExpectedContextualKeywordOn:
                case ErrorCode.ERR_ExpectedContextualKeywordEquals:
                case ErrorCode.ERR_ExpectedContextualKeywordBy:
                case ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator:
                case ErrorCode.ERR_InvalidInitializerElementInitializer:
                case ErrorCode.ERR_InconsistentLambdaParameterUsage:
                case ErrorCode.ERR_PartialMethodInvalidModifier:
                case ErrorCode.ERR_PartialMethodOnlyInPartialClass:
                case ErrorCode.ERR_PartialMethodNotExplicit:
                case ErrorCode.ERR_PartialMethodExtensionDifference:
                case ErrorCode.ERR_PartialMethodOnlyOneLatent:
                case ErrorCode.ERR_PartialMethodOnlyOneActual:
                case ErrorCode.ERR_PartialMethodParamsDifference:
                case ErrorCode.ERR_PartialMethodMustHaveLatent:
                case ErrorCode.ERR_PartialMethodInconsistentConstraints:
                case ErrorCode.ERR_PartialMethodToDelegate:
                case ErrorCode.ERR_PartialMethodStaticDifference:
                case ErrorCode.ERR_PartialMethodUnsafeDifference:
                case ErrorCode.ERR_PartialMethodInExpressionTree:
                case ErrorCode.ERR_ExplicitImplCollisionOnRefOut:
                case ErrorCode.ERR_IndirectRecursiveConstructorCall:
                case ErrorCode.WRN_ObsoleteOverridingNonObsolete:
                case ErrorCode.WRN_DebugFullNameTooLong:
                case ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue:
                case ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer:
                case ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator:
                case ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer:
                case ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed:
                case ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst:
                case ErrorCode.WRN_ExternCtorNoImplementation:
                case ErrorCode.ERR_TypeVarNotFound:
                case ErrorCode.ERR_ImplicitlyTypedArrayNoBestType:
                case ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue:
                case ErrorCode.ERR_ExpressionTreeContainsBaseAccess:
                case ErrorCode.ERR_ExpressionTreeContainsAssignment:
                case ErrorCode.ERR_AnonymousTypeDuplicatePropertyName:
                case ErrorCode.ERR_StatementLambdaToExpressionTree:
                case ErrorCode.ERR_ExpressionTreeMustHaveDelegate:
                case ErrorCode.ERR_AnonymousTypeNotAvailable:
                case ErrorCode.ERR_LambdaInIsAs:
                case ErrorCode.ERR_ExpressionTreeContainsMultiDimensionalArrayInitializer:
                case ErrorCode.ERR_MissingArgument:
                case ErrorCode.ERR_VariableUsedBeforeDeclaration:
                case ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion:
                case ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField:
                case ErrorCode.ERR_ExpressionTreeContainsBadCoalesce:
                case ErrorCode.ERR_ArrayInitializerExpected:
                case ErrorCode.ERR_ArrayInitializerIncorrectLength:
                case ErrorCode.ERR_ExpressionTreeContainsNamedArgument:
                case ErrorCode.ERR_ExpressionTreeContainsOptionalArgument:
                case ErrorCode.ERR_ExpressionTreeContainsIndexedProperty:
                case ErrorCode.ERR_IndexedPropertyRequiresParams:
                case ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams:
                case ErrorCode.ERR_IdentifierExpected:
                case ErrorCode.ERR_SemicolonExpected:
                case ErrorCode.ERR_SyntaxError:
                case ErrorCode.ERR_DuplicateModifier:
                case ErrorCode.ERR_DuplicateAccessor:
                case ErrorCode.ERR_IntegralTypeExpected:
                case ErrorCode.ERR_IllegalEscape:
                case ErrorCode.ERR_NewlineInConst:
                case ErrorCode.ERR_EmptyCharConst:
                case ErrorCode.ERR_TooManyCharsInConst:
                case ErrorCode.ERR_InvalidNumber:
                case ErrorCode.ERR_GetOrSetExpected:
                case ErrorCode.ERR_ClassTypeExpected:
                case ErrorCode.ERR_NamedArgumentExpected:
                case ErrorCode.ERR_TooManyCatches:
                case ErrorCode.ERR_ThisOrBaseExpected:
                case ErrorCode.ERR_OvlUnaryOperatorExpected:
                case ErrorCode.ERR_OvlBinaryOperatorExpected:
                case ErrorCode.ERR_IntOverflow:
                case ErrorCode.ERR_EOFExpected:
                case ErrorCode.ERR_BadEmbeddedStmt:
                case ErrorCode.ERR_PPDirectiveExpected:
                case ErrorCode.ERR_EndOfPPLineExpected:
                case ErrorCode.ERR_CloseParenExpected:
                case ErrorCode.ERR_EndifDirectiveExpected:
                case ErrorCode.ERR_UnexpectedDirective:
                case ErrorCode.ERR_ErrorDirective:
                case ErrorCode.WRN_WarningDirective:
                case ErrorCode.ERR_TypeExpected:
                case ErrorCode.ERR_PPDefFollowsToken:
                case ErrorCode.ERR_OpenEndedComment:
                case ErrorCode.ERR_OvlOperatorExpected:
                case ErrorCode.ERR_EndRegionDirectiveExpected:
                case ErrorCode.ERR_UnterminatedStringLit:
                case ErrorCode.ERR_BadDirectivePlacement:
                case ErrorCode.ERR_IdentifierExpectedKW:
                case ErrorCode.ERR_SemiOrLBraceExpected:
                case ErrorCode.ERR_MultiTypeInDeclaration:
                case ErrorCode.ERR_AddOrRemoveExpected:
                case ErrorCode.ERR_UnexpectedCharacter:
                case ErrorCode.ERR_ProtectedInStatic:
                case ErrorCode.WRN_UnreachableGeneralCatch:
                case ErrorCode.ERR_IncrementLvalueExpected:
                case ErrorCode.ERR_NoSuchMemberOrExtension:
                case ErrorCode.WRN_DeprecatedCollectionInitAddStr:
                case ErrorCode.ERR_DeprecatedCollectionInitAddStr:
                case ErrorCode.WRN_DeprecatedCollectionInitAdd:
                case ErrorCode.ERR_DefaultValueNotAllowed:
                case ErrorCode.WRN_DefaultValueForUnconsumedLocation:
                case ErrorCode.ERR_PartialWrongTypeParamsVariance:
                case ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd:
                case ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd:
                case ErrorCode.ERR_SingleTypeNameNotFoundFwd:
                case ErrorCode.WRN_IdentifierOrNumericLiteralExpected:
                case ErrorCode.ERR_UnexpectedToken:
                case ErrorCode.ERR_BadThisParam:
                case ErrorCode.ERR_BadTypeforThis:
                case ErrorCode.ERR_BadParamModThis:
                case ErrorCode.ERR_BadExtensionMeth:
                case ErrorCode.ERR_BadExtensionAgg:
                case ErrorCode.ERR_DupParamMod:
                case ErrorCode.ERR_ExtensionMethodsDecl:
                case ErrorCode.ERR_ExtensionAttrNotFound:
                case ErrorCode.ERR_ExplicitExtension:
                case ErrorCode.ERR_ValueTypeExtDelegate:
                case ErrorCode.ERR_BadArgCount:
                case ErrorCode.ERR_BadArgType:
                case ErrorCode.ERR_NoSourceFile:
                case ErrorCode.ERR_CantRefResource:
                case ErrorCode.ERR_ResourceNotUnique:
                case ErrorCode.ERR_ImportNonAssembly:
                case ErrorCode.ERR_RefLvalueExpected:
                case ErrorCode.ERR_BaseInStaticMeth:
                case ErrorCode.ERR_BaseInBadContext:
                case ErrorCode.ERR_RbraceExpected:
                case ErrorCode.ERR_LbraceExpected:
                case ErrorCode.ERR_InExpected:
                case ErrorCode.ERR_InvalidPreprocExpr:
                case ErrorCode.ERR_InvalidMemberDecl:
                case ErrorCode.ERR_MemberNeedsType:
                case ErrorCode.ERR_BadBaseType:
                case ErrorCode.WRN_EmptySwitch:
                case ErrorCode.ERR_ExpectedEndTry:
                case ErrorCode.ERR_InvalidExprTerm:
                case ErrorCode.ERR_BadNewExpr:
                case ErrorCode.ERR_NoNamespacePrivate:
                case ErrorCode.ERR_BadVarDecl:
                case ErrorCode.ERR_UsingAfterElements:
                case ErrorCode.ERR_BadBinOpArgs:
                case ErrorCode.ERR_BadUnOpArgs:
                case ErrorCode.ERR_NoVoidParameter:
                case ErrorCode.ERR_DuplicateAlias:
                case ErrorCode.ERR_BadProtectedAccess:
                case ErrorCode.ERR_AddModuleAssembly:
                case ErrorCode.ERR_BindToBogusProp2:
                case ErrorCode.ERR_BindToBogusProp1:
                case ErrorCode.ERR_NoVoidHere:
                case ErrorCode.ERR_IndexerNeedsParam:
                case ErrorCode.ERR_BadArraySyntax:
                case ErrorCode.ERR_BadOperatorSyntax:
                case ErrorCode.ERR_OutputNeedsName:
                case ErrorCode.ERR_CantHaveWin32ResAndManifest:
                case ErrorCode.ERR_CantHaveWin32ResAndIcon:
                case ErrorCode.ERR_CantReadResource:
                case ErrorCode.ERR_DocFileGen:
                case ErrorCode.WRN_XMLParseError:
                case ErrorCode.WRN_DuplicateParamTag:
                case ErrorCode.WRN_UnmatchedParamTag:
                case ErrorCode.WRN_MissingParamTag:
                case ErrorCode.WRN_BadXMLRef:
                case ErrorCode.ERR_BadStackAllocExpr:
                case ErrorCode.ERR_InvalidLineNumber:
                case ErrorCode.ERR_MissingPPFile:
                case ErrorCode.ERR_ForEachMissingMember:
                case ErrorCode.WRN_BadXMLRefParamType:
                case ErrorCode.WRN_BadXMLRefReturnType:
                case ErrorCode.ERR_BadWin32Res:
                case ErrorCode.WRN_BadXMLRefSyntax:
                case ErrorCode.ERR_BadModifierLocation:
                case ErrorCode.ERR_MissingArraySize:
                case ErrorCode.WRN_UnprocessedXMLComment:
                case ErrorCode.WRN_FailedInclude:
                case ErrorCode.WRN_InvalidInclude:
                case ErrorCode.WRN_MissingXMLComment:
                case ErrorCode.WRN_XMLParseIncludeError:
                case ErrorCode.ERR_BadDelArgCount:
                case ErrorCode.ERR_UnexpectedSemicolon:
                case ErrorCode.ERR_MethodReturnCantBeRefAny:
                case ErrorCode.ERR_CompileCancelled:
                case ErrorCode.ERR_MethodArgCantBeRefAny:
                case ErrorCode.ERR_AssgReadonlyLocal:
                case ErrorCode.ERR_RefReadonlyLocal:
                case ErrorCode.ERR_CantUseRequiredAttribute:
                case ErrorCode.ERR_NoModifiersOnAccessor:
                case ErrorCode.ERR_ParamsCantBeWithModifier:
                case ErrorCode.ERR_ReturnNotLValue:
                case ErrorCode.ERR_MissingCoClass:
                case ErrorCode.ERR_AmbiguousAttribute:
                case ErrorCode.ERR_BadArgExtraRef:
                case ErrorCode.WRN_CmdOptionConflictsSource:
                case ErrorCode.ERR_BadCompatMode:
                case ErrorCode.ERR_DelegateOnConditional:
                case ErrorCode.ERR_CantMakeTempFile:
                case ErrorCode.ERR_BadArgRef:
                case ErrorCode.ERR_YieldInAnonMeth:
                case ErrorCode.ERR_ReturnInIterator:
                case ErrorCode.ERR_BadIteratorArgType:
                case ErrorCode.ERR_BadIteratorReturn:
                case ErrorCode.ERR_BadYieldInFinally:
                case ErrorCode.ERR_BadYieldInTryOfCatch:
                case ErrorCode.ERR_EmptyYield:
                case ErrorCode.ERR_AnonDelegateCantUse:
                case ErrorCode.ERR_AnonDelegateCantUseRefLike:
                case ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef:
                case ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike:
                case ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember:
                case ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured:
                case ErrorCode.ERR_IllegalInnerUnsafe:
                case ErrorCode.ERR_BadYieldInCatch:
                case ErrorCode.ERR_BadDelegateLeave:
                case ErrorCode.WRN_IllegalPragma:
                case ErrorCode.WRN_IllegalPPWarning:
                case ErrorCode.WRN_BadRestoreNumber:
                case ErrorCode.ERR_VarargsIterator:
                case ErrorCode.ERR_UnsafeIteratorArgType:
                case ErrorCode.ERR_BadCoClassSig:
                case ErrorCode.ERR_MultipleIEnumOfT:
                case ErrorCode.ERR_FixedDimsRequired:
                case ErrorCode.ERR_FixedNotInStruct:
                case ErrorCode.ERR_AnonymousReturnExpected:
                case ErrorCode.WRN_NonECMAFeature:
                case ErrorCode.ERR_ExpectedVerbatimLiteral:
                case ErrorCode.ERR_AssgReadonly2:
                case ErrorCode.ERR_RefReadonly2:
                case ErrorCode.ERR_AssgReadonlyStatic2:
                case ErrorCode.ERR_RefReadonlyStatic2:
                case ErrorCode.ERR_AssgReadonlyLocal2Cause:
                case ErrorCode.ERR_RefReadonlyLocal2Cause:
                case ErrorCode.ERR_AssgReadonlyLocalCause:
                case ErrorCode.ERR_RefReadonlyLocalCause:
                case ErrorCode.WRN_ErrorOverride:
                case ErrorCode.ERR_AnonMethToNonDel:
                case ErrorCode.ERR_CantConvAnonMethParams:
                case ErrorCode.ERR_CantConvAnonMethReturns:
                case ErrorCode.ERR_IllegalFixedType:
                case ErrorCode.ERR_FixedOverflow:
                case ErrorCode.ERR_InvalidFixedArraySize:
                case ErrorCode.ERR_FixedBufferNotFixed:
                case ErrorCode.ERR_AttributeNotOnAccessor:
                case ErrorCode.WRN_InvalidSearchPathDir:
                case ErrorCode.ERR_IllegalVarArgs:
                case ErrorCode.ERR_IllegalParams:
                case ErrorCode.ERR_BadModifiersOnNamespace:
                case ErrorCode.ERR_BadPlatformType:
                case ErrorCode.ERR_ThisStructNotInAnonMeth:
                case ErrorCode.ERR_NoConvToIDisp:
                case ErrorCode.ERR_BadParamRef:
                case ErrorCode.ERR_BadParamExtraRef:
                case ErrorCode.ERR_BadParamType:
                case ErrorCode.ERR_BadExternIdentifier:
                case ErrorCode.ERR_AliasMissingFile:
                case ErrorCode.ERR_GlobalExternAlias:
                case ErrorCode.WRN_MultiplePredefTypes:
                case ErrorCode.ERR_LocalCantBeFixedAndHoisted:
                case ErrorCode.WRN_TooManyLinesForDebugger:
                case ErrorCode.ERR_CantConvAnonMethNoParams:
                case ErrorCode.ERR_ConditionalOnNonAttributeClass:
                case ErrorCode.WRN_CallOnNonAgileField:
                case ErrorCode.WRN_InvalidNumber:
                case ErrorCode.WRN_IllegalPPChecksum:
                case ErrorCode.WRN_EndOfPPLineExpected:
                case ErrorCode.WRN_ConflictingChecksum:
                case ErrorCode.WRN_InvalidAssemblyName:
                case ErrorCode.WRN_UnifyReferenceMajMin:
                case ErrorCode.WRN_UnifyReferenceBldRev:
                case ErrorCode.ERR_DuplicateImport:
                case ErrorCode.ERR_DuplicateImportSimple:
                case ErrorCode.ERR_AssemblyMatchBadVersion:
                case ErrorCode.ERR_FixedNeedsLvalue:
                case ErrorCode.WRN_DuplicateTypeParamTag:
                case ErrorCode.WRN_UnmatchedTypeParamTag:
                case ErrorCode.WRN_MissingTypeParamTag:
                case ErrorCode.ERR_CantChangeTypeOnOverride:
                case ErrorCode.ERR_DoNotUseFixedBufferAttr:
                case ErrorCode.WRN_AssignmentToSelf:
                case ErrorCode.WRN_ComparisonToSelf:
                case ErrorCode.ERR_CantOpenWin32Res:
                case ErrorCode.WRN_DotOnDefault:
                case ErrorCode.ERR_NoMultipleInheritance:
                case ErrorCode.ERR_BaseClassMustBeFirst:
                case ErrorCode.WRN_BadXMLRefTypeVar:
                case ErrorCode.ERR_FriendAssemblyBadArgs:
                case ErrorCode.ERR_FriendAssemblySNReq:
                case ErrorCode.ERR_DelegateOnNullable:
                case ErrorCode.ERR_BadCtorArgCount:
                case ErrorCode.ERR_GlobalAttributesNotFirst:
                case ErrorCode.ERR_ExpressionExpected:
                case ErrorCode.WRN_UnmatchedParamRefTag:
                case ErrorCode.WRN_UnmatchedTypeParamRefTag:
                case ErrorCode.ERR_DefaultValueMustBeConstant:
                case ErrorCode.ERR_DefaultValueBeforeRequiredValue:
                case ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument:
                case ErrorCode.ERR_BadNamedArgument:
                case ErrorCode.ERR_DuplicateNamedArgument:
                case ErrorCode.ERR_RefOutDefaultValue:
                case ErrorCode.ERR_NamedArgumentForArray:
                case ErrorCode.ERR_DefaultValueForExtensionParameter:
                case ErrorCode.ERR_NamedArgumentUsedInPositional:
                case ErrorCode.ERR_DefaultValueUsedWithAttributes:
                case ErrorCode.ERR_BadNamedArgumentForDelegateInvoke:
                case ErrorCode.ERR_NoPIAAssemblyMissingAttribute:
                case ErrorCode.ERR_NoCanonicalView:
                case ErrorCode.ERR_NoConversionForDefaultParam:
                case ErrorCode.ERR_DefaultValueForParamsParameter:
                case ErrorCode.ERR_NewCoClassOnLink:
                case ErrorCode.ERR_NoPIANestedType:
                case ErrorCode.ERR_InteropTypeMissingAttribute:
                case ErrorCode.ERR_InteropStructContainsMethods:
                case ErrorCode.ERR_InteropTypesWithSameNameAndGuid:
                case ErrorCode.ERR_NoPIAAssemblyMissingAttributes:
                case ErrorCode.ERR_AssemblySpecifiedForLinkAndRef:
                case ErrorCode.ERR_LocalTypeNameClash:
                case ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA:
                case ErrorCode.ERR_NotNullRefDefaultParameter:
                case ErrorCode.ERR_FixedLocalInLambda:
                case ErrorCode.ERR_MissingMethodOnSourceInterface:
                case ErrorCode.ERR_MissingSourceInterface:
                case ErrorCode.ERR_GenericsUsedInNoPIAType:
                case ErrorCode.ERR_GenericsUsedAcrossAssemblies:
                case ErrorCode.ERR_NoConversionForNubDefaultParam:
                case ErrorCode.ERR_InvalidSubsystemVersion:
                case ErrorCode.ERR_InteropMethodWithBody:
                case ErrorCode.ERR_BadWarningLevel:
                case ErrorCode.ERR_BadDebugType:
                case ErrorCode.ERR_BadResourceVis:
                case ErrorCode.ERR_DefaultValueTypeMustMatch:
                case ErrorCode.ERR_DefaultValueBadValueType:
                case ErrorCode.ERR_MemberAlreadyInitialized:
                case ErrorCode.ERR_MemberCannotBeInitialized:
                case ErrorCode.ERR_StaticMemberInObjectInitializer:
                case ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer:
                case ErrorCode.ERR_ValueTypePropertyInObjectInitializer:
                case ErrorCode.ERR_UnsafeTypeInObjectCreation:
                case ErrorCode.ERR_EmptyElementInitializer:
                case ErrorCode.ERR_InitializerAddHasWrongSignature:
                case ErrorCode.ERR_CollectionInitRequiresIEnumerable:
                case ErrorCode.ERR_CantOpenWin32Manifest:
                case ErrorCode.WRN_CantHaveManifestForModule:
                case ErrorCode.ERR_BadInstanceArgType:
                case ErrorCode.ERR_QueryDuplicateRangeVariable:
                case ErrorCode.ERR_QueryRangeVariableOverrides:
                case ErrorCode.ERR_QueryRangeVariableAssignedBadValue:
                case ErrorCode.ERR_QueryNoProviderCastable:
                case ErrorCode.ERR_QueryNoProviderStandard:
                case ErrorCode.ERR_QueryNoProvider:
                case ErrorCode.ERR_QueryOuterKey:
                case ErrorCode.ERR_QueryInnerKey:
                case ErrorCode.ERR_QueryOutRefRangeVariable:
                case ErrorCode.ERR_QueryMultipleProviders:
                case ErrorCode.ERR_QueryTypeInferenceFailedMulti:
                case ErrorCode.ERR_QueryTypeInferenceFailed:
                case ErrorCode.ERR_QueryTypeInferenceFailedSelectMany:
                case ErrorCode.ERR_ExpressionTreeContainsPointerOp:
                case ErrorCode.ERR_ExpressionTreeContainsAnonymousMethod:
                case ErrorCode.ERR_AnonymousMethodToExpressionTree:
                case ErrorCode.ERR_QueryRangeVariableReadOnly:
                case ErrorCode.ERR_QueryRangeVariableSameAsTypeParam:
                case ErrorCode.ERR_TypeVarNotFoundRangeVariable:
                case ErrorCode.ERR_BadArgTypesForCollectionAdd:
                case ErrorCode.ERR_ByRefParameterInExpressionTree:
                case ErrorCode.ERR_VarArgsInExpressionTree:
                case ErrorCode.ERR_InitializerAddHasParamModifiers:
                case ErrorCode.ERR_NonInvocableMemberCalled:
                case ErrorCode.WRN_MultipleRuntimeImplementationMatches:
                case ErrorCode.WRN_MultipleRuntimeOverrideMatches:
                case ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation:
                case ErrorCode.ERR_InvalidConstantDeclarationType:
                case ErrorCode.ERR_IllegalVarianceSyntax:
                case ErrorCode.ERR_UnexpectedVariance:
                case ErrorCode.ERR_BadDynamicTypeof:
                case ErrorCode.ERR_ExpressionTreeContainsDynamicOperation:
                case ErrorCode.ERR_BadDynamicConversion:
                case ErrorCode.ERR_DeriveFromDynamic:
                case ErrorCode.ERR_DeriveFromConstructedDynamic:
                case ErrorCode.ERR_DynamicTypeAsBound:
                case ErrorCode.ERR_ConstructedDynamicTypeAsBound:
                case ErrorCode.ERR_ExplicitDynamicAttr:
                case ErrorCode.ERR_NoDynamicPhantomOnBase:
                case ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer:
                case ErrorCode.ERR_BadArgTypeDynamicExtension:
                case ErrorCode.WRN_DynamicDispatchToConditionalMethod:
                case ErrorCode.ERR_NoDynamicPhantomOnBaseCtor:
                case ErrorCode.ERR_BadDynamicMethodArgMemgrp:
                case ErrorCode.ERR_BadDynamicMethodArgLambda:
                case ErrorCode.ERR_BadDynamicMethodArg:
                case ErrorCode.ERR_BadDynamicQuery:
                case ErrorCode.ERR_DynamicAttributeMissing:
                case ErrorCode.WRN_IsDynamicIsConfusing:
                case ErrorCode.ERR_BadAsyncReturn:
                case ErrorCode.ERR_BadAwaitInFinally:
                case ErrorCode.ERR_BadAwaitInCatch:
                case ErrorCode.ERR_BadAwaitArg:
                case ErrorCode.ERR_BadAsyncArgType:
                case ErrorCode.ERR_BadAsyncExpressionTree:
                case ErrorCode.ERR_MixingWinRTEventWithRegular:
                case ErrorCode.ERR_BadAwaitWithoutAsync:
                case ErrorCode.ERR_BadAsyncLacksBody:
                case ErrorCode.ERR_BadAwaitInQuery:
                case ErrorCode.ERR_BadAwaitInLock:
                case ErrorCode.ERR_TaskRetNoObjectRequired:
                case ErrorCode.WRN_AsyncLacksAwaits:
                case ErrorCode.ERR_FileNotFound:
                case ErrorCode.WRN_FileAlreadyIncluded:
                case ErrorCode.ERR_NoFileSpec:
                case ErrorCode.ERR_SwitchNeedsString:
                case ErrorCode.ERR_BadSwitch:
                case ErrorCode.WRN_NoSources:
                case ErrorCode.ERR_OpenResponseFile:
                case ErrorCode.ERR_CantOpenFileWrite:
                case ErrorCode.ERR_BadBaseNumber:
                case ErrorCode.ERR_BinaryFile:
                case ErrorCode.FTL_BadCodepage:
                case ErrorCode.ERR_NoMainOnDLL:
                case ErrorCode.FTL_InvalidTarget:
                case ErrorCode.FTL_InvalidInputFileName:
                case ErrorCode.WRN_NoConfigNotOnCommandLine:
                case ErrorCode.ERR_InvalidFileAlignment:
                case ErrorCode.WRN_DefineIdentifierRequired:
                case ErrorCode.FTL_OutputFileExists:
                case ErrorCode.ERR_OneAliasPerReference:
                case ErrorCode.ERR_SwitchNeedsNumber:
                case ErrorCode.ERR_MissingDebugSwitch:
                case ErrorCode.ERR_ComRefCallInExpressionTree:
                case ErrorCode.WRN_BadUILang:
                case ErrorCode.ERR_InvalidFormatForGuidForOption:
                case ErrorCode.ERR_MissingGuidForOption:
                case ErrorCode.ERR_InvalidOutputName:
                case ErrorCode.ERR_InvalidDebugInformationFormat:
                case ErrorCode.ERR_LegacyObjectIdSyntax:
                case ErrorCode.ERR_SourceLinkRequiresPdb:
                case ErrorCode.ERR_CannotEmbedWithoutPdb:
                case ErrorCode.ERR_BadSwitchValue:
                case ErrorCode.WRN_CLS_NoVarArgs:
                case ErrorCode.WRN_CLS_BadArgType:
                case ErrorCode.WRN_CLS_BadReturnType:
                case ErrorCode.WRN_CLS_BadFieldPropType:
                case ErrorCode.WRN_CLS_BadIdentifierCase:
                case ErrorCode.WRN_CLS_OverloadRefOut:
                case ErrorCode.WRN_CLS_OverloadUnnamed:
                case ErrorCode.WRN_CLS_BadIdentifier:
                case ErrorCode.WRN_CLS_BadBase:
                case ErrorCode.WRN_CLS_BadInterfaceMember:
                case ErrorCode.WRN_CLS_NoAbstractMembers:
                case ErrorCode.WRN_CLS_NotOnModules:
                case ErrorCode.WRN_CLS_ModuleMissingCLS:
                case ErrorCode.WRN_CLS_AssemblyNotCLS:
                case ErrorCode.WRN_CLS_BadAttributeType:
                case ErrorCode.WRN_CLS_ArrayArgumentToAttribute:
                case ErrorCode.WRN_CLS_NotOnModules2:
                case ErrorCode.WRN_CLS_IllegalTrueInFalse:
                case ErrorCode.WRN_CLS_MeaninglessOnPrivateType:
                case ErrorCode.WRN_CLS_AssemblyNotCLS2:
                case ErrorCode.WRN_CLS_MeaninglessOnParam:
                case ErrorCode.WRN_CLS_MeaninglessOnReturn:
                case ErrorCode.WRN_CLS_BadTypeVar:
                case ErrorCode.WRN_CLS_VolatileField:
                case ErrorCode.WRN_CLS_BadInterface:
                case ErrorCode.FTL_BadChecksumAlgorithm:
                case ErrorCode.ERR_BadAwaitArgIntrinsic:
                case ErrorCode.ERR_BadAwaitAsIdentifier:
                case ErrorCode.ERR_AwaitInUnsafeContext:
                case ErrorCode.ERR_UnsafeAsyncArgType:
                case ErrorCode.ERR_VarargsAsync:
                case ErrorCode.ERR_BadAwaitArgVoidCall:
                case ErrorCode.ERR_NonTaskMainCantBeAsync:
                case ErrorCode.ERR_CantConvAsyncAnonFuncReturns:
                case ErrorCode.ERR_BadAwaiterPattern:
                case ErrorCode.ERR_BadSpecialByRefLocal:
                case ErrorCode.WRN_UnobservedAwaitableExpression:
                case ErrorCode.ERR_SynchronizedAsyncMethod:
                case ErrorCode.ERR_BadAsyncReturnExpression:
                case ErrorCode.ERR_NoConversionForCallerLineNumberParam:
                case ErrorCode.ERR_NoConversionForCallerFilePathParam:
                case ErrorCode.ERR_NoConversionForCallerMemberNameParam:
                case ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue:
                case ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue:
                case ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue:
                case ErrorCode.ERR_BadPrefer32OnLib:
                case ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation:
                case ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation:
                case ErrorCode.ERR_DoesntImplementAwaitInterface:
                case ErrorCode.ERR_BadAwaitArg_NeedSystem:
                case ErrorCode.ERR_CantReturnVoid:
                case ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync:
                case ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct:
                case ErrorCode.ERR_BadAwaitWithoutAsyncMethod:
                case ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod:
                case ErrorCode.ERR_BadAwaitWithoutAsyncLambda:
                case ErrorCode.ERR_NoSuchMemberOrExtensionNeedUsing:
                case ErrorCode.ERR_UnexpectedAliasedName:
                case ErrorCode.ERR_UnexpectedGenericName:
                case ErrorCode.ERR_UnexpectedUnboundGenericName:
                case ErrorCode.ERR_GlobalStatement:
                case ErrorCode.ERR_BadUsingType:
                case ErrorCode.ERR_ReservedAssemblyName:
                case ErrorCode.ERR_PPReferenceFollowsToken:
                case ErrorCode.ERR_ExpectedPPFile:
                case ErrorCode.ERR_ReferenceDirectiveOnlyAllowedInScripts:
                case ErrorCode.ERR_NameNotInContextPossibleMissingReference:
                case ErrorCode.ERR_MetadataNameTooLong:
                case ErrorCode.ERR_AttributesNotAllowed:
                case ErrorCode.ERR_ExternAliasNotAllowed:
                case ErrorCode.ERR_ConflictingAliasAndDefinition:
                case ErrorCode.ERR_GlobalDefinitionOrStatementExpected:
                case ErrorCode.ERR_ExpectedSingleScript:
                case ErrorCode.ERR_RecursivelyTypedVariable:
                case ErrorCode.ERR_YieldNotAllowedInScript:
                case ErrorCode.ERR_NamespaceNotAllowedInScript:
                case ErrorCode.WRN_StaticInAsOrIs:
                case ErrorCode.ERR_InvalidDelegateType:
                case ErrorCode.ERR_BadVisEventType:
                case ErrorCode.ERR_GlobalAttributesNotAllowed:
                case ErrorCode.ERR_PublicKeyFileFailure:
                case ErrorCode.ERR_PublicKeyContainerFailure:
                case ErrorCode.ERR_FriendRefSigningMismatch:
                case ErrorCode.ERR_CannotPassNullForFriendAssembly:
                case ErrorCode.ERR_SignButNoPrivateKey:
                case ErrorCode.WRN_DelaySignButNoKey:
                case ErrorCode.ERR_InvalidVersionFormat:
                case ErrorCode.WRN_InvalidVersionFormat:
                case ErrorCode.ERR_NoCorrespondingArgument:
                case ErrorCode.ERR_ResourceFileNameNotUnique:
                case ErrorCode.ERR_DllImportOnGenericMethod:
                case ErrorCode.ERR_EncUpdateFailedMissingAttribute:
                case ErrorCode.ERR_ParameterNotValidForType:
                case ErrorCode.ERR_AttributeParameterRequired1:
                case ErrorCode.ERR_AttributeParameterRequired2:
                case ErrorCode.ERR_SecurityAttributeMissingAction:
                case ErrorCode.ERR_SecurityAttributeInvalidAction:
                case ErrorCode.ERR_SecurityAttributeInvalidActionAssembly:
                case ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod:
                case ErrorCode.ERR_PrincipalPermissionInvalidAction:
                case ErrorCode.ERR_FeatureNotValidInExpressionTree:
                case ErrorCode.ERR_MarshalUnmanagedTypeNotValidForFields:
                case ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields:
                case ErrorCode.ERR_PermissionSetAttributeInvalidFile:
                case ErrorCode.ERR_PermissionSetAttributeFileReadError:
                case ErrorCode.ERR_InvalidVersionFormat2:
                case ErrorCode.ERR_InvalidAssemblyCultureForExe:
                case ErrorCode.ERR_DuplicateAttributeInNetModule:
                case ErrorCode.ERR_CantOpenIcon:
                case ErrorCode.ERR_ErrorBuildingWin32Resources:
                case ErrorCode.ERR_BadAttributeParamDefaultArgument:
                case ErrorCode.ERR_MissingTypeInSource:
                case ErrorCode.ERR_MissingTypeInAssembly:
                case ErrorCode.ERR_SecurityAttributeInvalidTarget:
                case ErrorCode.ERR_InvalidAssemblyName:
                case ErrorCode.ERR_NoTypeDefFromModule:
                case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                case ErrorCode.ERR_InvalidDynamicCondition:
                case ErrorCode.ERR_WinRtEventPassedByRef:
                case ErrorCode.ERR_NetModuleNameMismatch:
                case ErrorCode.ERR_BadModuleName:
                case ErrorCode.ERR_BadCompilationOptionValue:
                case ErrorCode.ERR_BadAppConfigPath:
                case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                case ErrorCode.ERR_CmdOptionConflictsSource:
                case ErrorCode.ERR_FixedBufferTooManyDimensions:
                case ErrorCode.ERR_CantReadConfigFile:
                case ErrorCode.ERR_BadAwaitInCatchFilter:
                case ErrorCode.WRN_FilterIsConstantTrue:
                case ErrorCode.ERR_EncNoPIAReference:
                case ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage:
                case ErrorCode.ERR_MetadataReferencesNotSupported:
                case ErrorCode.ERR_InvalidAssemblyCulture:
                case ErrorCode.ERR_EncReferenceToAddedMember:
                case ErrorCode.ERR_MutuallyExclusiveOptions:
                case ErrorCode.ERR_InvalidDebugInfo:
                case ErrorCode.WRN_UnimplementedCommandLineSwitch:
                case ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName:
                case ErrorCode.ERR_InvalidSignaturePublicKey:
                case ErrorCode.ERR_ForwardedTypesConflict:
                case ErrorCode.WRN_RefCultureMismatch:
                case ErrorCode.ERR_AgnosticToMachineModule:
                case ErrorCode.ERR_ConflictingMachineModule:
                case ErrorCode.WRN_ConflictingMachineAssembly:
                case ErrorCode.ERR_CryptoHashFailed:
                case ErrorCode.ERR_MissingNetModuleReference:
                case ErrorCode.ERR_NetModuleNameMustBeUnique:
                case ErrorCode.ERR_UnsupportedTransparentIdentifierAccess:
                case ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute:
                case ErrorCode.WRN_UnqualifiedNestedTypeInCref:
                case ErrorCode.HDN_UnusedUsingDirective:
                case ErrorCode.HDN_UnusedExternAlias:
                case ErrorCode.WRN_NoRuntimeMetadataVersion:
                case ErrorCode.ERR_FeatureNotAvailableInVersion1:
                case ErrorCode.ERR_FeatureNotAvailableInVersion2:
                case ErrorCode.ERR_FeatureNotAvailableInVersion3:
                case ErrorCode.ERR_FeatureNotAvailableInVersion4:
                case ErrorCode.ERR_FeatureNotAvailableInVersion5:
                case ErrorCode.ERR_FieldHasMultipleDistinctConstantValues:
                case ErrorCode.ERR_ComImportWithInitializers:
                case ErrorCode.WRN_PdbLocalNameTooLong:
                case ErrorCode.ERR_RetNoObjectRequiredLambda:
                case ErrorCode.ERR_TaskRetNoObjectRequiredLambda:
                case ErrorCode.WRN_AnalyzerCannotBeCreated:
                case ErrorCode.WRN_NoAnalyzerInAssembly:
                case ErrorCode.WRN_UnableToLoadAnalyzer:
                case ErrorCode.ERR_CantReadRulesetFile:
                case ErrorCode.ERR_BadPdbData:
                case ErrorCode.INF_UnableToLoadSomeTypesInAnalyzer:
                case ErrorCode.ERR_InitializerOnNonAutoProperty:
                case ErrorCode.ERR_AutoPropertyMustHaveGetAccessor:
                case ErrorCode.ERR_InstancePropertyInitializerInInterface:
                case ErrorCode.ERR_EnumsCantContainDefaultConstructor:
                case ErrorCode.ERR_EncodinglessSyntaxTree:
                case ErrorCode.ERR_BlockBodyAndExpressionBody:
                case ErrorCode.ERR_FeatureIsExperimental:
                case ErrorCode.ERR_FeatureNotAvailableInVersion6:
                case ErrorCode.ERR_SwitchFallOut:
                case ErrorCode.ERR_NullPropagatingOpInExpressionTree:
                case ErrorCode.WRN_NubExprIsConstBool2:
                case ErrorCode.ERR_DictionaryInitializerInExpressionTree:
                case ErrorCode.ERR_ExtensionCollectionElementInitializerInExpressionTree:
                case ErrorCode.ERR_UnclosedExpressionHole:
                case ErrorCode.ERR_UseDefViolationProperty:
                case ErrorCode.ERR_AutoPropertyMustOverrideSet:
                case ErrorCode.ERR_ExpressionHasNoName:
                case ErrorCode.ERR_SubexpressionNotInNameof:
                case ErrorCode.ERR_AliasQualifiedNameNotAnExpression:
                case ErrorCode.ERR_NameofMethodGroupWithTypeParameters:
                case ErrorCode.ERR_NoAliasHere:
                case ErrorCode.ERR_UnescapedCurly:
                case ErrorCode.ERR_EscapedCurly:
                case ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier:
                case ErrorCode.ERR_EmptyFormatSpecifier:
                case ErrorCode.ERR_ErrorInReferencedAssembly:
                case ErrorCode.ERR_ExternHasConstructorInitializer:
                case ErrorCode.ERR_ExpressionOrDeclarationExpected:
                case ErrorCode.ERR_NameofExtensionMethod:
                case ErrorCode.WRN_AlignmentMagnitude:
                case ErrorCode.ERR_ConstantStringTooLong:
                case ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition:
                case ErrorCode.ERR_LoadDirectiveOnlyAllowedInScripts:
                case ErrorCode.ERR_PPLoadFollowsToken:
                case ErrorCode.ERR_SourceFileReferencesNotSupported:
                case ErrorCode.ERR_BadAwaitInStaticVariableInitializer:
                case ErrorCode.ERR_InvalidPathMap:
                case ErrorCode.ERR_PublicSignButNoKey:
                case ErrorCode.ERR_TooManyUserStrings:
                case ErrorCode.ERR_PeWritingFailure:
                case ErrorCode.WRN_AttributeIgnoredWhenPublicSigning:
                case ErrorCode.ERR_OptionMustBeAbsolutePath:
                case ErrorCode.ERR_FeatureNotAvailableInVersion7:
                case ErrorCode.ERR_DynamicLocalFunctionParamsParameter:
                case ErrorCode.ERR_ExpressionTreeContainsLocalFunction:
                case ErrorCode.ERR_InvalidInstrumentationKind:
                case ErrorCode.ERR_LocalFunctionMissingBody:
                case ErrorCode.ERR_InvalidHashAlgorithmName:
                case ErrorCode.ERR_ThrowMisplaced:
                case ErrorCode.ERR_PatternNullableType:
                case ErrorCode.ERR_BadPatternExpression:
                case ErrorCode.ERR_SwitchExpressionValueExpected:
                case ErrorCode.ERR_SwitchCaseSubsumed:
                case ErrorCode.ERR_PatternWrongType:
                case ErrorCode.ERR_ExpressionTreeContainsIsMatch:
                case ErrorCode.WRN_TupleLiteralNameMismatch:
                case ErrorCode.ERR_TupleTooFewElements:
                case ErrorCode.ERR_TupleReservedElementName:
                case ErrorCode.ERR_TupleReservedElementNameAnyPosition:
                case ErrorCode.ERR_TupleDuplicateElementName:
                case ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly:
                case ErrorCode.ERR_MissingDeconstruct:
                case ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable:
                case ErrorCode.ERR_DeconstructRequiresExpression:
                case ErrorCode.ERR_DeconstructWrongCardinality:
                case ErrorCode.ERR_CannotDeconstructDynamic:
                case ErrorCode.ERR_DeconstructTooFewElements:
                case ErrorCode.ERR_ConversionNotTupleCompatible:
                case ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType:
                case ErrorCode.ERR_TupleElementNamesAttributeMissing:
                case ErrorCode.ERR_ExplicitTupleElementNamesAttribute:
                case ErrorCode.ERR_CantChangeTupleNamesOnOverride:
                case ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList:
                case ErrorCode.ERR_ImplBadTupleNames:
                case ErrorCode.ERR_PartialMethodInconsistentTupleNames:
                case ErrorCode.ERR_ExpressionTreeContainsTupleLiteral:
                case ErrorCode.ERR_ExpressionTreeContainsTupleConversion:
                case ErrorCode.ERR_AutoPropertyCannotBeRefReturning:
                case ErrorCode.ERR_RefPropertyMustHaveGetAccessor:
                case ErrorCode.ERR_RefPropertyCannotHaveSetAccessor:
                case ErrorCode.ERR_CantChangeRefReturnOnOverride:
                case ErrorCode.ERR_MustNotHaveRefReturn:
                case ErrorCode.ERR_MustHaveRefReturn:
                case ErrorCode.ERR_RefReturnMustHaveIdentityConversion:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn:
                case ErrorCode.ERR_RefReturningCallInExpressionTree:
                case ErrorCode.ERR_BadIteratorReturnRef:
                case ErrorCode.ERR_BadRefReturnExpressionTree:
                case ErrorCode.ERR_RefReturnLvalueExpected:
                case ErrorCode.ERR_RefReturnNonreturnableLocal:
                case ErrorCode.ERR_RefReturnNonreturnableLocal2:
                case ErrorCode.ERR_RefReturnRangeVariable:
                case ErrorCode.ERR_RefReturnReadonly:
                case ErrorCode.ERR_RefReturnReadonlyStatic:
                case ErrorCode.ERR_RefReturnReadonly2:
                case ErrorCode.ERR_RefReturnReadonlyStatic2:
                case ErrorCode.ERR_RefReturnParameter:
                case ErrorCode.ERR_RefReturnParameter2:
                case ErrorCode.ERR_RefReturnLocal:
                case ErrorCode.ERR_RefReturnLocal2:
                case ErrorCode.ERR_RefReturnStructThis:
                case ErrorCode.ERR_InitializeByValueVariableWithReference:
                case ErrorCode.ERR_InitializeByReferenceVariableWithValue:
                case ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion:
                case ErrorCode.ERR_ByReferenceVariableMustBeInitialized:
                case ErrorCode.ERR_AnonDelegateCantUseLocal:
                case ErrorCode.ERR_BadIteratorLocalType:
                case ErrorCode.ERR_BadAsyncLocalType:
                case ErrorCode.ERR_PredefinedValueTupleTypeNotFound:
                case ErrorCode.ERR_SemiOrLBraceOrArrowExpected:
                case ErrorCode.ERR_NewWithTupleTypeSyntax:
                case ErrorCode.ERR_PredefinedValueTupleTypeMustBeStruct:
                case ErrorCode.ERR_DiscardTypeInferenceFailed:
                case ErrorCode.ERR_DeclarationExpressionNotPermitted:
                case ErrorCode.ERR_MustDeclareForeachIteration:
                case ErrorCode.ERR_TupleElementNamesInDeconstruction:
                case ErrorCode.ERR_ExpressionTreeContainsThrowExpression:
                case ErrorCode.ERR_DelegateRefMismatch:
                case ErrorCode.ERR_BadSourceCodeKind:
                case ErrorCode.ERR_BadDocumentationMode:
                case ErrorCode.ERR_BadLanguageVersion:
                case ErrorCode.ERR_ImplicitlyTypedOutVariableUsedInTheSameArgumentList:
                case ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable:
                case ErrorCode.ERR_ExpressionTreeContainsOutVariable:
                case ErrorCode.ERR_VarInvocationLvalueReserved:
                case ErrorCode.ERR_PublicSignNetModule:
                case ErrorCode.ERR_BadAssemblyName:
                case ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty:
                case ErrorCode.ERR_TypeForwardedToMultipleAssemblies:
                case ErrorCode.ERR_ExpressionTreeContainsDiscard:
                case ErrorCode.ERR_PatternDynamicType:
                case ErrorCode.ERR_VoidAssignment:
                case ErrorCode.ERR_VoidInTuple:
                case ErrorCode.ERR_Merge_conflict_marker_encountered:
                case ErrorCode.ERR_InvalidPreprocessingSymbol:
                case ErrorCode.ERR_FeatureNotAvailableInVersion7_1:
                case ErrorCode.ERR_LanguageVersionCannotHaveLeadingZeroes:
                case ErrorCode.ERR_CompilerAndLanguageVersion:
                case ErrorCode.WRN_WindowsExperimental:
                case ErrorCode.ERR_TupleInferredNamesNotAvailable:
                case ErrorCode.ERR_TypelessTupleInAs:
                case ErrorCode.ERR_NoRefOutWhenRefOnly:
                case ErrorCode.ERR_NoNetModuleOutputWhenRefOutOrRefOnly:
                case ErrorCode.ERR_BadOpOnNullOrDefaultOrNew:
                case ErrorCode.ERR_DefaultLiteralNotValid:
                case ErrorCode.ERR_PatternWrongGenericTypeInVersion:
                case ErrorCode.ERR_AmbigBinaryOpsOnDefault:
                case ErrorCode.ERR_FeatureNotAvailableInVersion7_2:
                case ErrorCode.WRN_UnreferencedLocalFunction:
                case ErrorCode.ERR_DynamicLocalFunctionTypeParameter:
                case ErrorCode.ERR_BadNonTrailingNamedArgument:
                case ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation:
                case ErrorCode.ERR_RefConditionalAndAwait:
                case ErrorCode.ERR_RefConditionalNeedsTwoRefs:
                case ErrorCode.ERR_RefConditionalDifferentTypes:
                case ErrorCode.ERR_BadParameterModifiers:
                case ErrorCode.ERR_RefReadonlyNotField:
                case ErrorCode.ERR_RefReadonlyNotField2:
                case ErrorCode.ERR_AssignReadonlyNotField:
                case ErrorCode.ERR_AssignReadonlyNotField2:
                case ErrorCode.ERR_RefReturnReadonlyNotField:
                case ErrorCode.ERR_RefReturnReadonlyNotField2:
                case ErrorCode.ERR_ExplicitReservedAttr:
                case ErrorCode.ERR_TypeReserved:
                case ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne:
                case ErrorCode.ERR_InExtensionMustBeValueType:
                case ErrorCode.ERR_FieldsInRoStruct:
                case ErrorCode.ERR_AutoPropsInRoStruct:
                case ErrorCode.ERR_FieldlikeEventsInRoStruct:
                case ErrorCode.ERR_RefStructInterfaceImpl:
                case ErrorCode.ERR_BadSpecialByRefIterator:
                case ErrorCode.ERR_FieldAutoPropCantBeByRefLike:
                case ErrorCode.ERR_StackAllocConversionNotPossible:
                case ErrorCode.ERR_EscapeCall:
                case ErrorCode.ERR_EscapeCall2:
                case ErrorCode.ERR_EscapeOther:
                case ErrorCode.ERR_CallArgMixing:
                case ErrorCode.ERR_MismatchedRefEscapeInTernary:
                case ErrorCode.ERR_EscapeVariable:
                case ErrorCode.ERR_EscapeStackAlloc:
                case ErrorCode.ERR_RefReturnThis:
                case ErrorCode.ERR_OutAttrOnInParam:
                case ErrorCode.ERR_PredefinedValueTupleTypeAmbiguous3:
                case ErrorCode.ERR_InvalidVersionFormatDeterministic:
                case ErrorCode.ERR_AttributeCtorInParameter:
                case ErrorCode.WRN_FilterIsConstantFalse:
                case ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch:
                case ErrorCode.ERR_ConditionalInInterpolation:
                case ErrorCode.ERR_CantUseVoidInArglist:
                case ErrorCode.ERR_InDynamicMethodArg:
                case ErrorCode.ERR_FeatureNotAvailableInVersion7_3:
                case ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable:
                case ErrorCode.ERR_DoNotUseFixedBufferAttrOnProperty:
                case ErrorCode.ERR_RefLocalOrParamExpected:
                case ErrorCode.ERR_RefAssignNarrower:
                case ErrorCode.ERR_NewBoundWithUnmanaged:
                case ErrorCode.ERR_UnmanagedConstraintNotSatisfied:
                case ErrorCode.ERR_CantUseInOrOutInArglist:
                case ErrorCode.ERR_ConWithUnmanagedCon:
                case ErrorCode.ERR_UnmanagedBoundWithClass:
                case ErrorCode.ERR_InvalidStackAllocArray:
                case ErrorCode.ERR_ExpressionTreeContainsTupleBinOp:
                case ErrorCode.WRN_TupleBinopLiteralNameMismatch:
                case ErrorCode.ERR_TupleSizesMismatchForBinOps:
                case ErrorCode.ERR_ExprCannotBeFixed:
                case ErrorCode.ERR_InvalidObjectCreation:
                case ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter:
                case ErrorCode.ERR_OutVariableCannotBeByRef:
                case ErrorCode.ERR_DeconstructVariableCannotBeByRef:
                case ErrorCode.ERR_OmittedTypeArgument:
                case ErrorCode.ERR_FeatureNotAvailableInVersion8:
                case ErrorCode.ERR_AltInterpolatedVerbatimStringsNotAvailable:
                case ErrorCode.ERR_IteratorMustBeAsync:
                case ErrorCode.ERR_NoConvToIAsyncDisp:
                case ErrorCode.ERR_AwaitForEachMissingMember:
                case ErrorCode.ERR_BadGetAsyncEnumerator:
                case ErrorCode.ERR_MultipleIAsyncEnumOfT:
                case ErrorCode.ERR_ForEachMissingMemberWrongAsync:
                case ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync:
                case ErrorCode.ERR_BadDynamicAwaitForEach:
                case ErrorCode.ERR_NoConvToIAsyncDispWrongAsync:
                case ErrorCode.ERR_NoConvToIDispWrongAsync:
                case ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable:
                case ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis:
                case ErrorCode.ERR_AttributeNotOnEventAccessor:
                case ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage:
                case ErrorCode.WRN_UndecoratedCancellationTokenParameter:
                case ErrorCode.ERR_MultipleEnumeratorCancellationAttributes:
                case ErrorCode.ERR_VarianceInterfaceNesting:
                case ErrorCode.ERR_ImplicitIndexIndexerWithName:
                case ErrorCode.ERR_ImplicitRangeIndexerWithName:
                case ErrorCode.ERR_WrongNumberOfSubpatterns:
                case ErrorCode.ERR_PropertyPatternNameMissing:
                case ErrorCode.ERR_MissingPattern:
                case ErrorCode.ERR_DefaultPattern:
                case ErrorCode.ERR_SwitchExpressionNoBestType:
                case ErrorCode.ERR_VarMayNotBindToType:
                case ErrorCode.WRN_SwitchExpressionNotExhaustive:
                case ErrorCode.ERR_SwitchArmSubsumed:
                case ErrorCode.ERR_ConstantPatternVsOpenType:
                case ErrorCode.WRN_CaseConstantNamedUnderscore:
                case ErrorCode.WRN_IsTypeNamedUnderscore:
                case ErrorCode.ERR_ExpressionTreeContainsSwitchExpression:
                case ErrorCode.ERR_SwitchGoverningExpressionRequiresParens:
                case ErrorCode.ERR_TupleElementNameMismatch:
                case ErrorCode.ERR_DeconstructParameterNameMismatch:
                case ErrorCode.ERR_IsPatternImpossible:
                case ErrorCode.WRN_GivenExpressionNeverMatchesPattern:
                case ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant:
                case ErrorCode.ERR_PointerTypeInPatternMatching:
                case ErrorCode.ERR_ArgumentNameInITuplePattern:
                case ErrorCode.ERR_DiscardPatternInSwitchStatement:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue:
                case ErrorCode.WRN_ThrowPossibleNull:
                case ErrorCode.ERR_IllegalSuppression:
                case ErrorCode.WRN_ConvertingNullableToNonNullable:
                case ErrorCode.WRN_NullReferenceAssignment:
                case ErrorCode.WRN_NullReferenceReceiver:
                case ErrorCode.WRN_NullReferenceReturn:
                case ErrorCode.WRN_NullReferenceArgument:
                case ErrorCode.WRN_UnboxPossibleNull:
                case ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation:
                case ErrorCode.WRN_UninitializedNonNullableField:
                case ErrorCode.WRN_NullabilityMismatchInAssignment:
                case ErrorCode.WRN_NullabilityMismatchInArgument:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate:
                case ErrorCode.ERR_ExplicitNullableAttribute:
                case ErrorCode.WRN_NullabilityMismatchInArgumentForOutput:
                case ErrorCode.WRN_NullAsNonNullable:
                case ErrorCode.ERR_NullableUnconstrainedTypeParameter:
                case ErrorCode.ERR_AnnotationDisallowedInObjectCreation:
                case ErrorCode.WRN_NullableValueTypeMayBeNull:
                case ErrorCode.ERR_NullableOptionNotAvailable:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint:
                case ErrorCode.WRN_MissingNonNullTypesContextForAnnotation:
                case ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint:
                case ErrorCode.ERR_TripleDotNotAllowed:
                case ErrorCode.ERR_BadNullableContextOption:
                case ErrorCode.ERR_NullableDirectiveQualifierExpected:
                case ErrorCode.ERR_BadNullableTypeof:
                case ErrorCode.ERR_ExpressionTreeCantContainRefStruct:
                case ErrorCode.ERR_ElseCannotStartStatement:
                case ErrorCode.ERR_ExpressionTreeCantContainNullCoalescingAssignment:
                case ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface:
                case ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase:
                case ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList:
                case ErrorCode.ERR_DuplicateExplicitImpl:
                case ErrorCode.ERR_UsingVarInSwitchCase:
                case ErrorCode.ERR_GoToForwardJumpOverUsingVar:
                case ErrorCode.ERR_GoToBackwardJumpOverUsingVar:
                case ErrorCode.ERR_IsNullableType:
                case ErrorCode.ERR_AsNullableType:
                case ErrorCode.ERR_FeatureInPreview:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull:
                case ErrorCode.WRN_ImplicitCopyInReadOnlyMember:
                case ErrorCode.ERR_StaticMemberCantBeReadOnly:
                case ErrorCode.ERR_AutoSetterCantBeReadOnly:
                case ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly:
                case ErrorCode.ERR_InvalidPropertyReadOnlyMods:
                case ErrorCode.ERR_DuplicatePropertyReadOnlyMods:
                case ErrorCode.ERR_FieldLikeEventCantBeReadOnly:
                case ErrorCode.ERR_PartialMethodReadOnlyDifference:
                case ErrorCode.ERR_ReadOnlyModMissingAccessor:
                case ErrorCode.ERR_OverrideRefConstraintNotSatisfied:
                case ErrorCode.ERR_OverrideValConstraintNotSatisfied:
                case ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation:
                case ErrorCode.ERR_NullableDirectiveTargetExpected:
                case ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode:
                case ErrorCode.WRN_NullReferenceInitializer:
                case ErrorCode.ERR_MultipleAnalyzerConfigsInSameDir:
                case ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation:
                case ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember:
                case ErrorCode.ERR_InvalidModifierForLanguageVersion:
                case ErrorCode.ERR_ImplicitImplementationOfNonPublicInterfaceMember:
                case ErrorCode.ERR_MostSpecificImplementationIsNotFound:
                case ErrorCode.ERR_LanguageVersionDoesNotSupportInterfaceImplementationForMember:
                case ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember:
                case ErrorCode.ERR_DefaultInterfaceImplementationInNoPIAType:
                case ErrorCode.ERR_AbstractEventHasAccessors:
                case ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint:
                case ErrorCode.ERR_DuplicateNullSuppression:
                case ErrorCode.ERR_DefaultLiteralNoTargetType:
                case ErrorCode.ERR_ReAbstractionInNoPIAType:
                case ErrorCode.ERR_InternalError:
                case ErrorCode.ERR_ImplicitObjectCreationIllegalTargetType:
                case ErrorCode.ERR_ImplicitObjectCreationNotValid:
                case ErrorCode.ERR_ImplicitObjectCreationNoTargetType:
                case ErrorCode.ERR_BadFuncPointerParamModifier:
                case ErrorCode.ERR_BadFuncPointerArgCount:
                case ErrorCode.ERR_MethFuncPtrMismatch:
                case ErrorCode.ERR_FuncPtrRefMismatch:
                case ErrorCode.ERR_FuncPtrMethMustBeStatic:
                case ErrorCode.ERR_ExternEventInitializer:
                case ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault:
                case ErrorCode.WRN_ParameterConditionallyDisallowsNull:
                case ErrorCode.WRN_ShouldNotReturn:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation:
                case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation:
                case ErrorCode.WRN_DoesNotReturnMismatch:
                case ErrorCode.ERR_NoOutputDirectory:
                case ErrorCode.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected:
                case ErrorCode.ERR_FeatureNotAvailableInVersion9:
                case ErrorCode.WRN_MemberNotNull:
                case ErrorCode.WRN_MemberNotNullWhen:
                case ErrorCode.WRN_MemberNotNullBadMember:
                case ErrorCode.WRN_ParameterDisallowsNull:
                case ErrorCode.WRN_ConstOutOfRangeChecked:
                case ErrorCode.ERR_DuplicateInterfaceWithDifferencesInBaseList:
                case ErrorCode.ERR_DesignatorBeneathPatternCombinator:
                case ErrorCode.ERR_UnsupportedTypeForRelationalPattern:
                case ErrorCode.ERR_RelationalPatternWithNaN:
                case ErrorCode.ERR_ConditionalOnLocalFunction:
                case ErrorCode.WRN_GeneratorFailedDuringInitialization:
                case ErrorCode.WRN_GeneratorFailedDuringGeneration:
                case ErrorCode.ERR_WrongFuncPtrCallingConvention:
                case ErrorCode.ERR_MissingAddressOf:
                case ErrorCode.ERR_CannotUseReducedExtensionMethodInAddressOf:
                case ErrorCode.ERR_CannotUseFunctionPointerAsFixedLocal:
                case ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer:
                case ErrorCode.ERR_ExpressionTreeContainsFromEndIndexExpression:
                case ErrorCode.ERR_ExpressionTreeContainsRangeExpression:
                case ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern:
                case ErrorCode.WRN_IsPatternAlways:
                case ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation:
                case ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods:
                case ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods:
                case ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods:
                case ErrorCode.ERR_PartialMethodAccessibilityDifference:
                case ErrorCode.ERR_PartialMethodExtendedModDifference:
                case ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement:
                case ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements:
                case ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType:
                case ErrorCode.ERR_SimpleProgramDisallowsMainType:
                case ErrorCode.ERR_SimpleProgramNotAnExecutable:
                case ErrorCode.ERR_UnsupportedCallingConvention:
                case ErrorCode.ERR_InvalidFunctionPointerCallingConvention:
                case ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier:
                case ErrorCode.ERR_DupReturnTypeMod:
                case ErrorCode.ERR_AddressOfMethodGroupInExpressionTree:
                case ErrorCode.ERR_CannotConvertAddressOfToDelegate:
                case ErrorCode.ERR_AddressOfToNonFunctionPointer:
                case ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary:
                case ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType:
                case ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid:
                case ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric:
                case ErrorCode.ERR_PartialMethodReturnTypeDifference:
                case ErrorCode.ERR_PartialMethodRefReturnDifference:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial:
                case ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable:
                case ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis:
                case ErrorCode.ERR_OverrideDefaultConstraintNotSatisfied:
                case ErrorCode.ERR_DefaultConstraintOverrideOnly:
                case ErrorCode.WRN_ParameterNotNullIfNotNull:
                case ErrorCode.WRN_ReturnNotNullIfNotNull:
                case ErrorCode.WRN_PartialMethodTypeDifference:
                case ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses:
                case ErrorCode.ERR_RuntimeDoesNotSupportCovariantPropertiesOfClasses:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen:
                case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen:
                case ErrorCode.WRN_PrecedenceInversion:
                case ErrorCode.ERR_ExpressionTreeContainsWithExpression:
                case ErrorCode.WRN_AnalyzerReferencesFramework:
                case ErrorCode.WRN_RecordEqualsWithoutGetHashCode:
                case ErrorCode.ERR_AssignmentInitOnly:
                case ErrorCode.ERR_CantChangeInitOnlyOnOverride:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly:
                case ErrorCode.ERR_ExplicitPropertyMismatchInitOnly:
                case ErrorCode.ERR_BadInitAccessor:
                case ErrorCode.ERR_InvalidWithReceiverType:
                case ErrorCode.ERR_CannotClone:
                case ErrorCode.ERR_CloneDisallowedInRecord:
                case ErrorCode.WRN_RecordNamedDisallowed:
                case ErrorCode.ERR_UnexpectedArgumentList:
                case ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord:
                case ErrorCode.ERR_MultipleRecordParameterLists:
                case ErrorCode.ERR_BadRecordBase:
                case ErrorCode.ERR_BadInheritanceFromRecord:
                case ErrorCode.ERR_BadRecordMemberForPositionalParameter:
                case ErrorCode.ERR_NoCopyConstructorInBaseType:
                case ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor:
                case ErrorCode.ERR_DoesNotOverrideMethodFromObject:
                case ErrorCode.ERR_SealedAPIInRecord:
                case ErrorCode.ERR_DoesNotOverrideBaseMethod:
                case ErrorCode.ERR_NotOverridableAPIInRecord:
                case ErrorCode.ERR_NonPublicAPIInRecord:
                case ErrorCode.ERR_SignatureMismatchInRecord:
                case ErrorCode.ERR_NonProtectedAPIInRecord:
                case ErrorCode.ERR_DoesNotOverrideBaseEqualityContract:
                case ErrorCode.ERR_StaticAPIInRecord:
                case ErrorCode.ERR_CopyConstructorWrongAccessibility:
                case ErrorCode.ERR_NonPrivateAPIInRecord:
                case ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion:
                case ErrorCode.WRN_UnassignedThisUnsupportedVersion:
                case ErrorCode.WRN_ParamUnassigned:
                case ErrorCode.WRN_UseDefViolationProperty:
                case ErrorCode.WRN_UseDefViolationField:
                case ErrorCode.WRN_UseDefViolationThisUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationOut:
                case ErrorCode.WRN_UseDefViolation:
                case ErrorCode.ERR_CannotSpecifyManagedWithUnmanagedSpecifiers:
                case ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv:
                case ErrorCode.ERR_TypeNotFound:
                case ErrorCode.ERR_TypeMustBePublic:
                case ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv:
                case ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly:
                case ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric:
                case ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic:
                case ErrorCode.WRN_ParameterIsStaticClass:
                case ErrorCode.WRN_ReturnTypeIsStaticClass:
                case ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly:
                case ErrorCode.ERR_ModuleInitializerCannotBeUnmanagedCallersOnly:
                case ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly:
                case ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate:
                case ErrorCode.ERR_InitCannotBeReadonly:
                case ErrorCode.ERR_UnexpectedVarianceStaticMember:
                case ErrorCode.ERR_FunctionPointersCannotBeCalledWithNamedArguments:
                case ErrorCode.ERR_EqualityContractRequiresGetter:
                case ErrorCode.WRN_UnreadRecordParameter:
                case ErrorCode.ERR_BadFieldTypeInRecord:
                case ErrorCode.WRN_DoNotCompareFunctionPointers:
                case ErrorCode.ERR_RecordAmbigCtor:
                case ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported:
                case ErrorCode.ERR_InheritingFromRecordWithSealedToString:
                case ErrorCode.ERR_HiddenPositionalMember:
                case ErrorCode.ERR_GlobalUsingInNamespace:
                case ErrorCode.ERR_GlobalUsingOutOfOrder:
                case ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression:
                case ErrorCode.ERR_CannotInferDelegateType:
                case ErrorCode.ERR_InvalidNameInSubpattern:
                case ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces:
                case ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers:
                case ErrorCode.ERR_BadAbstractUnaryOperatorSignature:
                case ErrorCode.ERR_BadAbstractIncDecSignature:
                case ErrorCode.ERR_BadAbstractIncDecRetType:
                case ErrorCode.ERR_BadAbstractBinaryOperatorSignature:
                case ErrorCode.ERR_BadAbstractShiftOperatorSignature:
                case ErrorCode.ERR_BadAbstractStaticMemberAccess:
                case ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess:
                case ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotStatic:
                case ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfacesForMember:
                case ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic:
                case ErrorCode.ERR_AbstractConversionNotInvolvingContainedType:
                case ErrorCode.ERR_InterfaceImplementedByUnmanagedCallersOnlyMethod:
                case ErrorCode.HDN_DuplicateWithGlobalUsing:
                case ErrorCode.ERR_CantConvAnonMethReturnType:
                case ErrorCode.ERR_BuilderAttributeDisallowed:
                case ErrorCode.ERR_FeatureNotAvailableInVersion10:
                case ErrorCode.ERR_SimpleProgramIsEmpty:
                case ErrorCode.ERR_LineSpanDirectiveInvalidValue:
                case ErrorCode.ERR_LineSpanDirectiveEndLessThanStart:
                case ErrorCode.ERR_WrongArityAsyncReturn:
                case ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed:
                case ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent:
                case ErrorCode.ERR_NullInvalidInterpolatedStringHandlerArgumentName:
                case ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName:
                case ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName:
                case ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType:
                case ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter:
                case ErrorCode.ERR_CannotUseSelfAsInterpolatedStringHandlerArgument:
                case ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed:
                case ErrorCode.ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString:
                case ErrorCode.ERR_InterpolatedStringHandlerArgumentOptionalNotSpecified:
                case ErrorCode.ERR_ExpressionTreeContainsInterpolatedStringHandlerConversion:
                case ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic:
                case ErrorCode.ERR_MultipleFileScopedNamespace:
                case ErrorCode.ERR_FileScopedAndNormalNamespace:
                case ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers:
                case ErrorCode.ERR_NoImplicitConvTargetTypedConditional:
                case ErrorCode.ERR_NonPublicParameterlessStructConstructor:
                case ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam:
                case ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression:
                case ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName:
                case ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue:
                case ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential:
                case ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation:
                case ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString:
                case ErrorCode.ERR_AttrTypeArgCannotBeTypeVar:
                case ErrorCode.ERR_AttrDependentTypeNotAllowed:
                case ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters:
                case ErrorCode.ERR_LambdaWithAttributesToExpressionTree:
                case ErrorCode.WRN_CompileTimeCheckedOverflow:
                case ErrorCode.WRN_MethGrpToNonDel:
                case ErrorCode.ERR_LambdaExplicitReturnTypeVar:
                case ErrorCode.ERR_InterpolatedStringsReferencingInstanceCannotBeInObjectInitializers:
                case ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly:
                case ErrorCode.ERR_CannotBeMadeNullable:
                case ErrorCode.ERR_UnsupportedTypeForListPattern:
                case ErrorCode.ERR_MisplacedSlicePattern:
                case ErrorCode.WRN_LowerCaseTypeName:
                case ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor:
                case ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor:
                case ErrorCode.ERR_ListPatternRequiresLength:
                case ErrorCode.ERR_ScopedMismatchInParameterOfTarget:
                case ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation:
                case ErrorCode.ERR_ScopedMismatchInParameterOfPartial:
                case ErrorCode.ERR_ParameterNullCheckingNotSupported:
                case ErrorCode.ERR_RawStringNotInDirectives:
                case ErrorCode.ERR_UnterminatedRawString:
                case ErrorCode.ERR_TooManyQuotesForRawString:
                case ErrorCode.ERR_LineDoesNotStartWithSameWhitespace:
                case ErrorCode.ERR_RawStringDelimiterOnOwnLine:
                case ErrorCode.ERR_RawStringInVerbatimInterpolatedStrings:
                case ErrorCode.ERR_RawStringMustContainContent:
                case ErrorCode.ERR_LineContainsDifferentWhitespace:
                case ErrorCode.ERR_NotEnoughQuotesForRawString:
                case ErrorCode.ERR_NotEnoughCloseBracesForRawString:
                case ErrorCode.ERR_TooManyOpenBracesForRawString:
                case ErrorCode.ERR_TooManyCloseBracesForRawString:
                case ErrorCode.ERR_IllegalAtSequence:
                case ErrorCode.ERR_StringMustStartWithQuoteCharacter:
                case ErrorCode.ERR_NoEnumConstraint:
                case ErrorCode.ERR_NoDelegateConstraint:
                case ErrorCode.ERR_MisplacedRecord:
                case ErrorCode.ERR_PatternSpanCharCannotBeStringNull:
                case ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion:
                case ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationPropertyUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationFieldUnsupportedVersion:
                case ErrorCode.WRN_UseDefViolationPropertySupportedVersion:
                case ErrorCode.WRN_UseDefViolationFieldSupportedVersion:
                case ErrorCode.WRN_UseDefViolationThisSupportedVersion:
                case ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion:
                case ErrorCode.WRN_UnassignedThisSupportedVersion:
                case ErrorCode.ERR_OperatorCantBeChecked:
                case ErrorCode.ERR_ImplicitConversionOperatorCantBeChecked:
                case ErrorCode.ERR_CheckedOperatorNeedsMatch:
                case ErrorCode.ERR_MisplacedUnchecked:
                case ErrorCode.ERR_LineSpanDirectiveRequiresSpace:
                case ErrorCode.ERR_RequiredNameDisallowed:
                case ErrorCode.ERR_OverrideMustHaveRequired:
                case ErrorCode.ERR_RequiredMemberCannotBeHidden:
                case ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType:
                case ErrorCode.ERR_ExplicitRequiredMember:
                case ErrorCode.ERR_RequiredMemberMustBeSettable:
                case ErrorCode.ERR_RequiredMemberMustBeSet:
                case ErrorCode.ERR_RequiredMembersMustBeAssignedValue:
                case ErrorCode.ERR_RequiredMembersInvalid:
                case ErrorCode.ERR_RequiredMembersBaseTypeInvalid:
                case ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers:
                case ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers:
                case ErrorCode.ERR_UnsupportedCompilerFeature:
                case ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired:
                case ErrorCode.ERR_RefReturningPropertiesCannotBeRequired:
                case ErrorCode.ERR_ImplicitImplementationOfInaccessibleInterfaceMember:
                case ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers:
                case ErrorCode.ERR_BadAbstractEqualityOperatorSignature:
                case ErrorCode.ERR_BadBinaryReadOnlySpanConcatenation:
                case ErrorCode.ERR_ScopedRefAndRefStructOnly:
                case ErrorCode.ERR_ScopedDiscard:
                case ErrorCode.ERR_FixedFieldMustNotBeRef:
                case ErrorCode.ERR_RefFieldCannotReferToRefStruct:
                case ErrorCode.ERR_FileTypeDisallowedInSignature:
                case ErrorCode.ERR_FileTypeNoExplicitAccessibility:
                case ErrorCode.ERR_FileTypeBase:
                case ErrorCode.ERR_FileTypeNested:
                case ErrorCode.ERR_GlobalUsingStaticFileType:
                case ErrorCode.ERR_FileTypeNameDisallowed:
                case ErrorCode.ERR_FeatureNotAvailableInVersion11:
                case ErrorCode.ERR_RefFieldInNonRefStruct:
                case ErrorCode.WRN_AnalyzerReferencesNewerCompiler:
                case ErrorCode.ERR_CannotMatchOnINumberBase:
                case ErrorCode.ERR_ScopedTypeNameDisallowed:
                case ErrorCode.ERR_ImplicitlyTypedDefaultParameter:
                case ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget:
                case ErrorCode.ERR_RuntimeDoesNotSupportRefFields:
                case ErrorCode.ERR_ExplicitScopedRef:
                case ErrorCode.ERR_UnscopedScoped:
                case ErrorCode.WRN_DuplicateAnalyzerReference:
                case ErrorCode.ERR_FilePathCannotBeConvertedToUtf8:
                case ErrorCode.ERR_FileLocalDuplicateNameInNS:
                case ErrorCode.WRN_ScopedMismatchInParameterOfTarget:
                case ErrorCode.WRN_ScopedMismatchInParameterOfOverrideOrImplementation:
                case ErrorCode.ERR_RefReturnScopedParameter:
                case ErrorCode.ERR_RefReturnScopedParameter2:
                case ErrorCode.ERR_RefReturnOnlyParameter:
                case ErrorCode.ERR_RefReturnOnlyParameter2:
                case ErrorCode.ERR_RefAssignReturnOnly:
                case ErrorCode.WRN_ManagedAddr:
                case ErrorCode.WRN_EscapeVariable:
                case ErrorCode.WRN_EscapeStackAlloc:
                case ErrorCode.WRN_RefReturnNonreturnableLocal:
                case ErrorCode.WRN_RefReturnNonreturnableLocal2:
                case ErrorCode.WRN_RefReturnStructThis:
                case ErrorCode.WRN_RefAssignNarrower:
                case ErrorCode.WRN_MismatchedRefEscapeInTernary:
                case ErrorCode.WRN_RefReturnParameter:
                case ErrorCode.WRN_RefReturnScopedParameter:
                case ErrorCode.WRN_RefReturnParameter2:
                case ErrorCode.WRN_RefReturnScopedParameter2:
                case ErrorCode.WRN_RefReturnLocal:
                case ErrorCode.WRN_RefReturnLocal2:
                case ErrorCode.WRN_RefAssignReturnOnly:
                case ErrorCode.WRN_RefReturnOnlyParameter:
                case ErrorCode.WRN_RefReturnOnlyParameter2:
                case ErrorCode.ERR_RefAssignValEscapeWider:
                case ErrorCode.WRN_RefAssignValEscapeWider:
                case ErrorCode.WRN_OptionalParamValueMismatch:
                case ErrorCode.WRN_ParamsArrayInLambdaOnly:
                case ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget:
                case ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation:
                case ErrorCode.ERR_UnrecognizedRefSafetyRulesAttributeVersion:
                case ErrorCode.ERR_BadSpecialByRefUsing:
                case ErrorCode.ERR_InvalidPrimaryConstructorParameterReference:
                case ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver:
                case ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase:
                case ErrorCode.WRN_UnreadPrimaryConstructorParameter:
                case ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter:
                case ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter:
                case ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter:
                case ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter2:
                case ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter2:
                case ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter2:
                case ErrorCode.ERR_RefReturnPrimaryConstructorParameter:
                case ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter:
                case ErrorCode.ERR_UnexpectedParameterList:
                case ErrorCode.WRN_AddressOfInAsync:
                case ErrorCode.ERR_BadRefInUsingAlias:
                case ErrorCode.ERR_BadUnsafeInUsingDirective:
                case ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias:
                case ErrorCode.ERR_BadStaticAfterUnsafe:
                case ErrorCode.ERR_BadCaseInSwitchArm:
                case ErrorCode.ERR_InterceptorsFeatureNotEnabled:
                case ErrorCode.ERR_InterceptorContainingTypeCannotBeGeneric:
                case ErrorCode.ERR_InterceptorPathNotInCompilation:
                case ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate:
                case ErrorCode.ERR_InterceptorPositionBadToken:
                case ErrorCode.ERR_InterceptorLineOutOfRange:
                case ErrorCode.ERR_InterceptorCharacterOutOfRange:
                case ErrorCode.ERR_InterceptorMethodMustBeOrdinary:
                case ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition:
                case ErrorCode.ERR_InterceptorFilePathCannotBeNull:
                case ErrorCode.ERR_InterceptorNameNotInvoked:
                case ErrorCode.ERR_InterceptorNonUniquePath:
                case ErrorCode.ERR_InterceptorLineCharacterMustBePositive:
                case ErrorCode.ERR_ConstantValueOfTypeExpected:
                case ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefAny:
                case ErrorCode.ERR_InterceptorCannotUseUnmanagedCallersOnly:
                case ErrorCode.ERR_BadUsingStaticType:
                case ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer:
                case ErrorCode.ERR_InlineArrayConversionToSpanNotSupported:
                case ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported:
                case ErrorCode.ERR_InlineArrayIndexOutOfRange:
                case ErrorCode.ERR_InvalidInlineArrayLength:
                case ErrorCode.ERR_InvalidInlineArrayLayout:
                case ErrorCode.ERR_InvalidInlineArrayFields:
                case ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation:
                case ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes:
                case ErrorCode.ERR_InlineArrayBadIndex:
                case ErrorCode.ERR_NamedArgumentForInlineArray:
                case ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible:
                case ErrorCode.ERR_ExpressionTreeContainsCollectionExpression:
                case ErrorCode.ERR_CollectionExpressionNoTargetType:
                case ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase:
                case ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier:
                case ErrorCode.WRN_InlineArrayIndexerNotUsed:
                case ErrorCode.WRN_InlineArraySliceNotUsed:
                case ErrorCode.WRN_InlineArrayConversionOperatorNotUsed:
                case ErrorCode.WRN_InlineArrayNotSupportedByLanguage:
                case ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound:
                case ErrorCode.ERR_CollectionBuilderAttributeInvalidType:
                case ErrorCode.ERR_CollectionBuilderAttributeInvalidMethodName:
                case ErrorCode.ERR_CollectionBuilderNoElementType:
                case ErrorCode.ERR_InlineArrayForEachNotSupported:
                case ErrorCode.ERR_RefReadOnlyWrongOrdering:
                case ErrorCode.WRN_BadArgRef:
                case ErrorCode.WRN_ArgExpectedRefOrIn:
                case ErrorCode.WRN_RefReadonlyNotVariable:
                case ErrorCode.ERR_BadArgExtraRefLangVersion:
                case ErrorCode.WRN_ArgExpectedIn:
                case ErrorCode.WRN_OverridingDifferentRefness:
                case ErrorCode.WRN_HidingDifferentRefness:
                case ErrorCode.WRN_TargetDifferentRefness:
                case ErrorCode.ERR_OutAttrOnRefReadonlyParam:
                case ErrorCode.WRN_RefReadonlyParameterDefaultValue:
                case ErrorCode.WRN_ByValArraySizeConstRequired:
                case ErrorCode.WRN_UseDefViolationRefField:
                case ErrorCode.ERR_FeatureNotAvailableInVersion12:
                case ErrorCode.ERR_CollectionExpressionEscape:
                case ErrorCode.WRN_Experimental:
                case ErrorCode.ERR_ExpectedInterpolatedString:
                case ErrorCode.ERR_InterceptorGlobalNamespace:
                case ErrorCode.WRN_CollectionExpressionRefStructMayAllocate:
                case ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate:
                case ErrorCode.ERR_CollectionExpressionImmutableArray:
                case ErrorCode.ERR_InvalidExperimentalDiagID:
                case ErrorCode.ERR_SpreadMissingMember:
                case ErrorCode.ERR_CollectionExpressionTargetNoElementType:
                case ErrorCode.ERR_CollectionExpressionMissingConstructor:
                case ErrorCode.ERR_CollectionExpressionMissingAdd:
                case ErrorCode.WRN_ConvertingLock:
                case ErrorCode.ERR_BadSpecialByRefLock:
                case ErrorCode.ERR_CantInferMethTypeArgs_DynamicArgumentWithParamsCollections:
                case ErrorCode.ERR_ParamsCollectionAmbiguousDynamicArgument:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionMethod:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionIndexer:
                case ErrorCode.WRN_DynamicDispatchToParamsCollectionConstructor:
                case ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls:
                case ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember:
                case ErrorCode.ERR_ParamsCollectionConstructorDoesntInitializeRequiredMember:
                case ErrorCode.ERR_ParamsCollectionExpressionTree:
                case ErrorCode.ERR_ParamsCollectionExtensionAddMethod:
                case ErrorCode.ERR_ParamsCollectionMissingConstructor:
                case ErrorCode.ERR_NoModifiersOnUsing:
                    return false;
                default:
                    // NOTE: All error codes must be explicitly handled in this switch statement
                    //       to ensure that we correctly classify all error codes as build-only or not.
                    throw new NotImplementedException($"ErrorCode.{code}");
            }
        }

        /// <summary>
        /// When converting an anonymous function to a delegate type, there are some diagnostics
        /// that will occur regardless of the delegate type - particularly those that do not
        /// depend on the substituted types (e.g. name uniqueness).  Even though we need to
        /// produce a diagnostic in such cases, we do not need to abandon overload resolution -
        /// we can choose the overload that is best without regard to such diagnostics.
        /// </summary>
        /// <returns>True if seeing the ErrorCode should prevent a delegate conversion
        /// from completing successfully.</returns>
        internal static bool PreventsSuccessfulDelegateConversion(ErrorCode code)
        {
            if (code == ErrorCode.Void || code == ErrorCode.Unknown)
            {
                return false;
            }

            if (IsWarning(code))
            {
                return false;
            }

            switch (code)
            {
                case ErrorCode.ERR_DuplicateParamName:
                case ErrorCode.ERR_LocalDuplicate:
                case ErrorCode.ERR_LocalIllegallyOverrides:
                case ErrorCode.ERR_LocalSameNameAsTypeParam:
                case ErrorCode.ERR_QueryRangeVariableOverrides:
                case ErrorCode.ERR_QueryRangeVariableSameAsTypeParam:
                case ErrorCode.ERR_DeprecatedCollectionInitAddStr:
                case ErrorCode.ERR_DeprecatedSymbolStr:
                case ErrorCode.ERR_MissingPredefinedMember:
                case ErrorCode.ERR_DefaultValueUsedWithAttributes:
                case ErrorCode.ERR_ExplicitParamArrayOrCollection:
                    return false;
                default:
                    return true;
            }
        }

        /// <remarks>
        /// WARNING: will resolve lazy diagnostics - do not call this before the member lists are completed
        /// or you could trigger infinite recursion.
        /// </remarks>
        internal static bool PreventsSuccessfulDelegateConversion(DiagnosticBag diagnostics)
        {
            foreach (Diagnostic diag in diagnostics.AsEnumerable()) // Checking the code would have resolved them anyway.
            {
                if (ErrorFacts.PreventsSuccessfulDelegateConversion((ErrorCode)diag.Code))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool PreventsSuccessfulDelegateConversion(ImmutableArray<Diagnostic> diagnostics)
        {
            foreach (var diag in diagnostics)
            {
                if (ErrorFacts.PreventsSuccessfulDelegateConversion((ErrorCode)diag.Code))
                {
                    return true;
                }
            }

            return false;
        }

        internal static ErrorCode GetStaticClassParameterCode(bool useWarning)
            => useWarning ? ErrorCode.WRN_ParameterIsStaticClass : ErrorCode.ERR_ParameterIsStaticClass;

        internal static ErrorCode GetStaticClassReturnCode(bool useWarning)
            => useWarning ? ErrorCode.WRN_ReturnTypeIsStaticClass : ErrorCode.ERR_ReturnTypeIsStaticClass;
    }
}
