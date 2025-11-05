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
using Roslyn.Utilities;

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

            nullableWarnings.Add(GetId(ErrorCode.WRN_UninitializedNonNullableBackingField));

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
            RoslynDebug.Assert(!string.IsNullOrEmpty(message), $"{code}");
            return message;
        }

        /// <remarks>Don't call this during a parse--it loads resources</remarks>
        public static string GetMessage(ErrorCode code, CultureInfo culture)
        {
            string message = ResourceManager.GetString(code.ToString(), culture);
            RoslynDebug.Assert(!string.IsNullOrEmpty(message), $"{code}");
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
                case ErrorCode.WRN_UnassignedInternalRefField:
                    // Warning level 10 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 10 (C# 14) and that can be reported for pre-existing code.
                    return 10;
                case ErrorCode.WRN_InterceptsLocationAttributeUnsupportedSignature:
                    // Warning level 9 is exclusively for warnings introduced in the compiler
                    // shipped with dotnet 9 (C# 13) and that can be reported for pre-existing code.
                    return 9;
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
                case ErrorCode.WRN_ExperimentalWithMessage:
                case ErrorCode.WRN_CollectionExpressionRefStructMayAllocate:
                case ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate:
                case ErrorCode.WRN_ConvertingLock:
                case ErrorCode.WRN_PartialMemberSignatureDifference:
                case ErrorCode.WRN_FieldIsAmbiguous:
                case ErrorCode.WRN_UninitializedNonNullableBackingField:
                case ErrorCode.WRN_AccessorDoesNotUseBackingField:
                case ErrorCode.WRN_UnscopedRefAttributeOldRules:
                case ErrorCode.WRN_RedundantPattern:
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
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
            return code switch
            {
                ErrorCode.WRN_ALinkWarn
                or ErrorCode.WRN_UnreferencedField
                or ErrorCode.WRN_UnreferencedFieldAssg
                or ErrorCode.WRN_UnreferencedEvent
                or ErrorCode.WRN_UnassignedInternalField
                or ErrorCode.ERR_MissingPredefinedMember
                or ErrorCode.ERR_PredefinedTypeNotFound
                or ErrorCode.ERR_NoEntryPoint
                or ErrorCode.WRN_InvalidMainSig
                or ErrorCode.ERR_MultipleEntryPoints
                or ErrorCode.WRN_MainIgnored
                or ErrorCode.ERR_MainClassNotClass
                or ErrorCode.WRN_MainCantBeGeneric
                or ErrorCode.ERR_NoMainInClass
                or ErrorCode.ERR_MainClassNotFound
                or ErrorCode.WRN_SyncAndAsyncEntryPoints
                or ErrorCode.ERR_BadDelegateConstructor
                or ErrorCode.ERR_InsufficientStack
                or ErrorCode.ERR_ModuleEmitFailure
                or ErrorCode.ERR_TooManyLocals
                or ErrorCode.ERR_BindToBogus
                or ErrorCode.ERR_ExportedTypeConflictsWithDeclaration
                or ErrorCode.ERR_ForwardedTypeConflictsWithDeclaration
                or ErrorCode.ERR_ExportedTypesConflict
                or ErrorCode.ERR_ForwardedTypeConflictsWithExportedType
                or ErrorCode.ERR_ByRefTypeAndAwait
                or ErrorCode.ERR_RefReturningCallAndAwait
                or ErrorCode.ERR_SpecialByRefInLambda
                or ErrorCode.ERR_DynamicRequiredTypesMissing
                or ErrorCode.ERR_CannotBeConvertedToUtf8
                or ErrorCode.ERR_FileTypeNonUniquePath
                or ErrorCode.ERR_InterceptorSignatureMismatch
                or ErrorCode.ERR_InterceptorMustHaveMatchingThisParameter
                or ErrorCode.ERR_InterceptorMustNotHaveThisParameter
                or ErrorCode.ERR_DuplicateInterceptor
                or ErrorCode.WRN_InterceptorSignatureMismatch
                or ErrorCode.ERR_InterceptorNotAccessible
                or ErrorCode.ERR_InterceptorScopedMismatch
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOnInterceptor
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOnInterceptor
                or ErrorCode.ERR_InterceptorCannotInterceptNameof
                or ErrorCode.ERR_SymbolDefinedInAssembly
                or ErrorCode.ERR_InterceptorArityNotCompatible
                or ErrorCode.ERR_InterceptorCannotBeGeneric
                or ErrorCode.ERR_InterceptableMethodMustBeOrdinary
                or ErrorCode.ERR_PossibleAsyncIteratorWithoutYield
                or ErrorCode.ERR_PossibleAsyncIteratorWithoutYieldOrAwait
                or ErrorCode.ERR_RefLocalAcrossAwait
                or ErrorCode.ERR_DataSectionStringLiteralHashCollision
                or ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync
                or ErrorCode.ERR_NonTaskMainCantBeAsync
                    // Update src\Features\CSharp\Portable\Diagnostics\LanguageServer\CSharpLspBuildOnlyDiagnostics.cs
                    // and TestIsBuildOnlyDiagnostic in src\Compilers\CSharp\Test\Syntax\Diagnostics\DiagnosticTest.cs
                    // whenever new values are added here.
                    => true,

                ErrorCode.Void
                or ErrorCode.Unknown
                or ErrorCode.ERR_NoMetadataFile
                or ErrorCode.FTL_MetadataCantOpenFile
                or ErrorCode.ERR_NoTypeDef
                or ErrorCode.ERR_OutputWriteFailed
                or ErrorCode.ERR_BadBinaryOps
                or ErrorCode.ERR_IntDivByZero
                or ErrorCode.ERR_BadIndexLHS
                or ErrorCode.ERR_BadIndexCount
                or ErrorCode.ERR_BadUnaryOp
                or ErrorCode.ERR_ThisInStaticMeth
                or ErrorCode.ERR_ThisInBadContext
                or ErrorCode.ERR_NoImplicitConv
                or ErrorCode.ERR_NoExplicitConv
                or ErrorCode.ERR_ConstOutOfRange
                or ErrorCode.ERR_AmbigBinaryOps
                or ErrorCode.ERR_AmbigUnaryOp
                or ErrorCode.ERR_InAttrOnOutParam
                or ErrorCode.ERR_ValueCantBeNull
                or ErrorCode.ERR_NoExplicitBuiltinConv
                or ErrorCode.FTL_DebugEmitFailure
                or ErrorCode.ERR_BadVisReturnType
                or ErrorCode.ERR_BadVisParamType
                or ErrorCode.ERR_BadVisFieldType
                or ErrorCode.ERR_BadVisPropertyType
                or ErrorCode.ERR_BadVisIndexerReturn
                or ErrorCode.ERR_BadVisIndexerParam
                or ErrorCode.ERR_BadVisOpReturn
                or ErrorCode.ERR_BadVisOpParam
                or ErrorCode.ERR_BadVisDelegateReturn
                or ErrorCode.ERR_BadVisDelegateParam
                or ErrorCode.ERR_BadVisBaseClass
                or ErrorCode.ERR_BadVisBaseInterface
                or ErrorCode.ERR_EventNeedsBothAccessors
                or ErrorCode.ERR_EventNotDelegate
                or ErrorCode.ERR_InterfaceEventInitializer
                or ErrorCode.ERR_BadEventUsage
                or ErrorCode.ERR_ExplicitEventFieldImpl
                or ErrorCode.ERR_CantOverrideNonEvent
                or ErrorCode.ERR_AddRemoveMustHaveBody
                or ErrorCode.ERR_AbstractEventInitializer
                or ErrorCode.ERR_PossibleBadNegCast
                or ErrorCode.ERR_ReservedEnumerator
                or ErrorCode.ERR_AsMustHaveReferenceType
                or ErrorCode.WRN_LowercaseEllSuffix
                or ErrorCode.ERR_BadEventUsageNoField
                or ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl
                or ErrorCode.ERR_TypeParamMustBeIdentifier
                or ErrorCode.ERR_MemberReserved
                or ErrorCode.ERR_DuplicateParamName
                or ErrorCode.ERR_DuplicateNameInNS
                or ErrorCode.ERR_DuplicateNameInClass
                or ErrorCode.ERR_NameNotInContext
                or ErrorCode.ERR_AmbigContext
                or ErrorCode.WRN_DuplicateUsing
                or ErrorCode.ERR_BadMemberFlag
                or ErrorCode.ERR_BadMemberProtection
                or ErrorCode.WRN_NewRequired
                or ErrorCode.WRN_NewNotRequired
                or ErrorCode.ERR_CircConstValue
                or ErrorCode.ERR_MemberAlreadyExists
                or ErrorCode.ERR_StaticNotVirtual
                or ErrorCode.ERR_OverrideNotNew
                or ErrorCode.WRN_NewOrOverrideExpected
                or ErrorCode.ERR_OverrideNotExpected
                or ErrorCode.ERR_NamespaceUnexpected
                or ErrorCode.ERR_NoSuchMember
                or ErrorCode.ERR_BadSKknown
                or ErrorCode.ERR_BadSKunknown
                or ErrorCode.ERR_ObjectRequired
                or ErrorCode.ERR_AmbigCall
                or ErrorCode.ERR_BadAccess
                or ErrorCode.ERR_MethDelegateMismatch
                or ErrorCode.ERR_RetObjectRequired
                or ErrorCode.ERR_RetNoObjectRequired
                or ErrorCode.ERR_LocalDuplicate
                or ErrorCode.ERR_AssgLvalueExpected
                or ErrorCode.ERR_StaticConstParam
                or ErrorCode.ERR_NotConstantExpression
                or ErrorCode.ERR_NotNullConstRefField
                or ErrorCode.ERR_LocalIllegallyOverrides
                or ErrorCode.ERR_BadUsingNamespace
                or ErrorCode.ERR_NoBreakOrCont
                or ErrorCode.ERR_DuplicateLabel
                or ErrorCode.ERR_NoConstructors
                or ErrorCode.ERR_NoNewAbstract
                or ErrorCode.ERR_ConstValueRequired
                or ErrorCode.ERR_CircularBase
                or ErrorCode.ERR_MethodNameExpected
                or ErrorCode.ERR_ConstantExpected
                or ErrorCode.ERR_V6SwitchGoverningTypeValueExpected
                or ErrorCode.ERR_DuplicateCaseLabel
                or ErrorCode.ERR_InvalidGotoCase
                or ErrorCode.ERR_PropertyLacksGet
                or ErrorCode.ERR_BadExceptionType
                or ErrorCode.ERR_BadEmptyThrow
                or ErrorCode.ERR_BadFinallyLeave
                or ErrorCode.ERR_LabelShadow
                or ErrorCode.ERR_LabelNotFound
                or ErrorCode.ERR_UnreachableCatch
                or ErrorCode.ERR_ReturnExpected
                or ErrorCode.WRN_UnreachableCode
                or ErrorCode.ERR_SwitchFallThrough
                or ErrorCode.WRN_UnreferencedLabel
                or ErrorCode.ERR_UseDefViolation
                or ErrorCode.WRN_UnreferencedVar
                or ErrorCode.ERR_UseDefViolationField
                or ErrorCode.ERR_UnassignedThisUnsupportedVersion
                or ErrorCode.ERR_AmbigQM
                or ErrorCode.ERR_InvalidQM
                or ErrorCode.ERR_NoBaseClass
                or ErrorCode.ERR_BaseIllegal
                or ErrorCode.ERR_ObjectProhibited
                or ErrorCode.ERR_ParamUnassigned
                or ErrorCode.ERR_InvalidArray
                or ErrorCode.ERR_ExternHasBody
                or ErrorCode.ERR_AbstractAndExtern
                or ErrorCode.ERR_BadAttributeParamType
                or ErrorCode.ERR_BadAttributeArgument
                or ErrorCode.WRN_IsAlwaysTrue
                or ErrorCode.WRN_IsAlwaysFalse
                or ErrorCode.ERR_LockNeedsReference
                or ErrorCode.ERR_NullNotValid
                or ErrorCode.ERR_UseDefViolationThisUnsupportedVersion
                or ErrorCode.ERR_ArgsInvalid
                or ErrorCode.ERR_AssgReadonly
                or ErrorCode.ERR_RefReadonly
                or ErrorCode.ERR_PtrExpected
                or ErrorCode.ERR_PtrIndexSingle
                or ErrorCode.WRN_ByRefNonAgileField
                or ErrorCode.ERR_AssgReadonlyStatic
                or ErrorCode.ERR_RefReadonlyStatic
                or ErrorCode.ERR_AssgReadonlyProp
                or ErrorCode.ERR_IllegalStatement
                or ErrorCode.ERR_BadGetEnumerator
                or ErrorCode.ERR_AbstractBaseCall
                or ErrorCode.ERR_RefProperty
                or ErrorCode.ERR_ManagedAddr
                or ErrorCode.ERR_BadFixedInitType
                or ErrorCode.ERR_FixedMustInit
                or ErrorCode.ERR_InvalidAddrOp
                or ErrorCode.ERR_FixedNeeded
                or ErrorCode.ERR_FixedNotNeeded
                or ErrorCode.ERR_UnsafeNeeded
                or ErrorCode.ERR_OpTFRetType
                or ErrorCode.ERR_OperatorNeedsMatch
                or ErrorCode.ERR_BadBoolOp
                or ErrorCode.ERR_MustHaveOpTF
                or ErrorCode.WRN_UnreferencedVarAssg
                or ErrorCode.ERR_CheckedOverflow
                or ErrorCode.ERR_ConstOutOfRangeChecked
                or ErrorCode.ERR_BadVarargs
                or ErrorCode.ERR_ParamsMustBeCollection
                or ErrorCode.ERR_IllegalArglist
                or ErrorCode.ERR_IllegalUnsafe
                or ErrorCode.ERR_AmbigMember
                or ErrorCode.ERR_BadForeachDecl
                or ErrorCode.ERR_ParamsLast
                or ErrorCode.ERR_SizeofUnsafe
                or ErrorCode.ERR_DottedTypeNameNotFoundInNS
                or ErrorCode.ERR_FieldInitRefNonstatic
                or ErrorCode.ERR_SealedNonOverride
                or ErrorCode.ERR_CantOverrideSealed
                or ErrorCode.ERR_VoidError
                or ErrorCode.ERR_ConditionalOnOverride
                or ErrorCode.ERR_PointerInAsOrIs
                or ErrorCode.ERR_CallingFinalizeDeprecated
                or ErrorCode.ERR_SingleTypeNameNotFound
                or ErrorCode.ERR_NegativeStackAllocSize
                or ErrorCode.ERR_NegativeArraySize
                or ErrorCode.ERR_OverrideFinalizeDeprecated
                or ErrorCode.ERR_CallingBaseFinalizeDeprecated
                or ErrorCode.WRN_NegativeArrayIndex
                or ErrorCode.WRN_BadRefCompareLeft
                or ErrorCode.WRN_BadRefCompareRight
                or ErrorCode.ERR_BadCastInFixed
                or ErrorCode.ERR_StackallocInCatchFinally
                or ErrorCode.ERR_VarargsLast
                or ErrorCode.ERR_MissingPartial
                or ErrorCode.ERR_PartialTypeKindConflict
                or ErrorCode.ERR_PartialModifierConflict
                or ErrorCode.ERR_PartialMultipleBases
                or ErrorCode.ERR_PartialWrongTypeParams
                or ErrorCode.ERR_PartialWrongConstraints
                or ErrorCode.ERR_NoImplicitConvCast
                or ErrorCode.ERR_PartialMisplaced
                or ErrorCode.ERR_MisplacedExtension
                or ErrorCode.ERR_ImportedCircularBase
                or ErrorCode.ERR_UseDefViolationOut
                or ErrorCode.ERR_ArraySizeInDeclaration
                or ErrorCode.ERR_InaccessibleGetter
                or ErrorCode.ERR_InaccessibleSetter
                or ErrorCode.ERR_InvalidPropertyAccessMod
                or ErrorCode.ERR_DuplicatePropertyAccessMods
                or ErrorCode.ERR_AccessModMissingAccessor
                or ErrorCode.ERR_UnimplementedInterfaceAccessor
                or ErrorCode.WRN_PatternIsAmbiguous
                or ErrorCode.WRN_PatternNotPublicOrNotInstance
                or ErrorCode.WRN_PatternBadSignature
                or ErrorCode.ERR_FriendRefNotEqualToThis
                or ErrorCode.WRN_SequentialOnPartialClass
                or ErrorCode.ERR_BadConstType
                or ErrorCode.ERR_NoNewTyvar
                or ErrorCode.ERR_BadArity
                or ErrorCode.ERR_BadTypeArgument
                or ErrorCode.ERR_TypeArgsNotAllowed
                or ErrorCode.ERR_HasNoTypeVars
                or ErrorCode.ERR_NewConstraintNotSatisfied
                or ErrorCode.ERR_GenericConstraintNotSatisfiedRefType
                or ErrorCode.ERR_GenericConstraintNotSatisfiedNullableEnum
                or ErrorCode.ERR_GenericConstraintNotSatisfiedNullableInterface
                or ErrorCode.ERR_GenericConstraintNotSatisfiedTyVar
                or ErrorCode.ERR_GenericConstraintNotSatisfiedValType
                or ErrorCode.ERR_DuplicateGeneratedName
                or ErrorCode.ERR_GlobalSingleTypeNameNotFound
                or ErrorCode.ERR_NewBoundMustBeLast
                or ErrorCode.ERR_TypeVarCantBeNull
                or ErrorCode.ERR_DuplicateBound
                or ErrorCode.ERR_ClassBoundNotFirst
                or ErrorCode.ERR_BadRetType
                or ErrorCode.ERR_DuplicateConstraintClause
                or ErrorCode.ERR_CantInferMethTypeArgs
                or ErrorCode.ERR_LocalSameNameAsTypeParam
                or ErrorCode.ERR_AsWithTypeVar
                or ErrorCode.ERR_BadIndexerNameAttr
                or ErrorCode.ERR_AttrArgWithTypeVars
                or ErrorCode.ERR_NewTyvarWithArgs
                or ErrorCode.ERR_AbstractSealedStatic
                or ErrorCode.WRN_AmbiguousXMLReference
                or ErrorCode.WRN_VolatileByRef
                or ErrorCode.ERR_ComImportWithImpl
                or ErrorCode.ERR_ComImportWithBase
                or ErrorCode.ERR_ImplBadConstraints
                or ErrorCode.ERR_DottedTypeNameNotFoundInAgg
                or ErrorCode.ERR_MethGrpToNonDel
                or ErrorCode.ERR_BadExternAlias
                or ErrorCode.ERR_ColColWithTypeAlias
                or ErrorCode.ERR_AliasNotFound
                or ErrorCode.ERR_SameFullNameAggAgg
                or ErrorCode.ERR_SameFullNameNsAgg
                or ErrorCode.WRN_SameFullNameThisNsAgg
                or ErrorCode.WRN_SameFullNameThisAggAgg
                or ErrorCode.WRN_SameFullNameThisAggNs
                or ErrorCode.ERR_SameFullNameThisAggThisNs
                or ErrorCode.ERR_ExternAfterElements
                or ErrorCode.WRN_GlobalAliasDefn
                or ErrorCode.ERR_SealedStaticClass
                or ErrorCode.ERR_PrivateAbstractAccessor
                or ErrorCode.ERR_ValueExpected
                or ErrorCode.ERR_UnboxNotLValue
                or ErrorCode.ERR_AnonMethGrpInForEach
                or ErrorCode.ERR_BadIncDecRetType
                or ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst
                or ErrorCode.ERR_RefValBoundWithClass
                or ErrorCode.ERR_NewBoundWithVal
                or ErrorCode.ERR_RefConstraintNotSatisfied
                or ErrorCode.ERR_ValConstraintNotSatisfied
                or ErrorCode.ERR_CircularConstraint
                or ErrorCode.ERR_BaseConstraintConflict
                or ErrorCode.ERR_ConWithValCon
                or ErrorCode.ERR_AmbigUDConv
                or ErrorCode.WRN_AlwaysNull
                or ErrorCode.ERR_OverrideWithConstraints
                or ErrorCode.ERR_AmbigOverride
                or ErrorCode.ERR_DecConstError
                or ErrorCode.WRN_CmpAlwaysFalse
                or ErrorCode.WRN_FinalizeMethod
                or ErrorCode.ERR_ExplicitImplParams
                or ErrorCode.WRN_GotoCaseShouldConvert
                or ErrorCode.ERR_MethodImplementingAccessor
                or ErrorCode.WRN_NubExprIsConstBool
                or ErrorCode.WRN_ExplicitImplCollision
                or ErrorCode.ERR_AbstractHasBody
                or ErrorCode.ERR_ConcreteMissingBody
                or ErrorCode.ERR_AbstractAndSealed
                or ErrorCode.ERR_AbstractNotVirtual
                or ErrorCode.ERR_StaticConstant
                or ErrorCode.ERR_CantOverrideNonFunction
                or ErrorCode.ERR_CantOverrideNonVirtual
                or ErrorCode.ERR_CantChangeAccessOnOverride
                or ErrorCode.ERR_CantChangeReturnTypeOnOverride
                or ErrorCode.ERR_CantDeriveFromSealedType
                or ErrorCode.ERR_AbstractInConcreteClass
                or ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall
                or ErrorCode.ERR_StaticConstructorWithAccessModifiers
                or ErrorCode.ERR_RecursiveConstructorCall
                or ErrorCode.ERR_ObjectCallingBaseConstructor
                or ErrorCode.ERR_StructWithBaseConstructorCall
                or ErrorCode.ERR_StructLayoutCycle
                or ErrorCode.ERR_InterfacesCantContainFields
                or ErrorCode.ERR_InterfacesCantContainConstructors
                or ErrorCode.ERR_NonInterfaceInInterfaceList
                or ErrorCode.ERR_DuplicateInterfaceInBaseList
                or ErrorCode.ERR_CycleInInterfaceInheritance
                or ErrorCode.ERR_HidingAbstractMethod
                or ErrorCode.ERR_UnimplementedAbstractMethod
                or ErrorCode.ERR_UnimplementedInterfaceMember
                or ErrorCode.ERR_ObjectCantHaveBases
                or ErrorCode.ERR_ExplicitInterfaceImplementationNotInterface
                or ErrorCode.ERR_InterfaceMemberNotFound
                or ErrorCode.ERR_ClassDoesntImplementInterface
                or ErrorCode.ERR_ExplicitInterfaceImplementationInNonClassOrStruct
                or ErrorCode.ERR_MemberNameSameAsType
                or ErrorCode.ERR_EnumeratorOverflow
                or ErrorCode.ERR_CantOverrideNonProperty
                or ErrorCode.ERR_NoGetToOverride
                or ErrorCode.ERR_NoSetToOverride
                or ErrorCode.ERR_PropertyCantHaveVoidType
                or ErrorCode.ERR_PropertyWithNoAccessors
                or ErrorCode.ERR_NewVirtualInSealed
                or ErrorCode.ERR_ExplicitPropertyAddingAccessor
                or ErrorCode.ERR_ExplicitPropertyMissingAccessor
                or ErrorCode.ERR_ConversionWithInterface
                or ErrorCode.ERR_ConversionWithBase
                or ErrorCode.ERR_ConversionWithDerived
                or ErrorCode.ERR_IdentityConversion
                or ErrorCode.ERR_ConversionNotInvolvingContainedType
                or ErrorCode.ERR_DuplicateConversionInClass
                or ErrorCode.ERR_OperatorsMustBeStaticAndPublic
                or ErrorCode.ERR_BadIncDecSignature
                or ErrorCode.ERR_BadUnaryOperatorSignature
                or ErrorCode.ERR_BadBinaryOperatorSignature
                or ErrorCode.ERR_BadShiftOperatorSignature
                or ErrorCode.ERR_InterfacesCantContainConversionOrEqualityOperators
                or ErrorCode.ERR_CantOverrideBogusMethod
                or ErrorCode.ERR_CantCallSpecialMethod
                or ErrorCode.ERR_BadTypeReference
                or ErrorCode.ERR_BadDestructorName
                or ErrorCode.ERR_OnlyClassesCanContainDestructors
                or ErrorCode.ERR_ConflictAliasAndMember
                or ErrorCode.ERR_ConditionalOnSpecialMethod
                or ErrorCode.ERR_ConditionalMustReturnVoid
                or ErrorCode.ERR_DuplicateAttribute
                or ErrorCode.ERR_ConditionalOnInterfaceMethod
                or ErrorCode.ERR_OperatorCantReturnVoid
                or ErrorCode.ERR_InvalidAttributeArgument
                or ErrorCode.ERR_AttributeOnBadSymbolType
                or ErrorCode.ERR_FloatOverflow
                or ErrorCode.ERR_InvalidReal
                or ErrorCode.ERR_ComImportWithoutUuidAttribute
                or ErrorCode.ERR_InvalidNamedArgument
                or ErrorCode.ERR_DllImportOnInvalidMethod
                or ErrorCode.ERR_FieldCantBeRefAny
                or ErrorCode.ERR_ArrayElementCantBeRefAny
                or ErrorCode.WRN_DeprecatedSymbol
                or ErrorCode.ERR_NotAnAttributeClass
                or ErrorCode.ERR_BadNamedAttributeArgument
                or ErrorCode.WRN_DeprecatedSymbolStr
                or ErrorCode.ERR_DeprecatedSymbolStr
                or ErrorCode.ERR_IndexerCantHaveVoidType
                or ErrorCode.ERR_VirtualPrivate
                or ErrorCode.ERR_ArrayInitToNonArrayType
                or ErrorCode.ERR_ArrayInitInBadPlace
                or ErrorCode.ERR_MissingStructOffset
                or ErrorCode.WRN_ExternMethodNoImplementation
                or ErrorCode.WRN_ProtectedInSealed
                or ErrorCode.ERR_InterfaceImplementedByConditional
                or ErrorCode.ERR_InterfaceImplementedImplicitlyByVariadic
                or ErrorCode.ERR_IllegalRefParam
                or ErrorCode.ERR_BadArgumentToAttribute
                or ErrorCode.ERR_StructOffsetOnBadStruct
                or ErrorCode.ERR_StructOffsetOnBadField
                or ErrorCode.ERR_AttributeUsageOnNonAttributeClass
                or ErrorCode.WRN_PossibleMistakenNullStatement
                or ErrorCode.ERR_DuplicateNamedAttributeArgument
                or ErrorCode.ERR_DeriveFromEnumOrValueType
                or ErrorCode.ERR_DefaultMemberOnIndexedType
                or ErrorCode.ERR_BogusType
                or ErrorCode.ERR_CStyleArray
                or ErrorCode.WRN_VacuousIntegralComp
                or ErrorCode.ERR_AbstractAttributeClass
                or ErrorCode.ERR_BadNamedAttributeArgumentType
                or ErrorCode.WRN_AttributeLocationOnBadDeclaration
                or ErrorCode.WRN_InvalidAttributeLocation
                or ErrorCode.WRN_EqualsWithoutGetHashCode
                or ErrorCode.WRN_EqualityOpWithoutEquals
                or ErrorCode.WRN_EqualityOpWithoutGetHashCode
                or ErrorCode.ERR_OutAttrOnRefParam
                or ErrorCode.ERR_OverloadRefKind
                or ErrorCode.ERR_LiteralDoubleCast
                or ErrorCode.WRN_IncorrectBooleanAssg
                or ErrorCode.ERR_ProtectedInStruct
                or ErrorCode.ERR_InconsistentIndexerNames
                or ErrorCode.ERR_ComImportWithUserCtor
                or ErrorCode.ERR_FieldCantHaveVoidType
                or ErrorCode.WRN_NonObsoleteOverridingObsolete
                or ErrorCode.ERR_SystemVoid
                or ErrorCode.ERR_ExplicitParamArrayOrCollection
                or ErrorCode.WRN_BitwiseOrSignExtend
                or ErrorCode.ERR_VolatileStruct
                or ErrorCode.ERR_VolatileAndReadonly
                or ErrorCode.ERR_AbstractField
                or ErrorCode.ERR_BogusExplicitImpl
                or ErrorCode.ERR_ExplicitMethodImplAccessor
                or ErrorCode.WRN_CoClassWithoutComImport
                or ErrorCode.ERR_ConditionalWithOutParam
                or ErrorCode.ERR_AccessorImplementingMethod
                or ErrorCode.ERR_AliasQualAsExpression
                or ErrorCode.ERR_DerivingFromATyVar
                or ErrorCode.ERR_DuplicateTypeParameter
                or ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter
                or ErrorCode.ERR_TypeVariableSameAsParent
                or ErrorCode.ERR_UnifyingInterfaceInstantiations
                or ErrorCode.ERR_TyVarNotFoundInConstraint
                or ErrorCode.ERR_BadBoundType
                or ErrorCode.ERR_SpecialTypeAsBound
                or ErrorCode.ERR_BadVisBound
                or ErrorCode.ERR_LookupInTypeVariable
                or ErrorCode.ERR_BadConstraintType
                or ErrorCode.ERR_InstanceMemberInStaticClass
                or ErrorCode.ERR_StaticBaseClass
                or ErrorCode.ERR_ConstructorInStaticClass
                or ErrorCode.ERR_DestructorInStaticClass
                or ErrorCode.ERR_InstantiatingStaticClass
                or ErrorCode.ERR_StaticDerivedFromNonObject
                or ErrorCode.ERR_StaticClassInterfaceImpl
                or ErrorCode.ERR_OperatorInStaticClass
                or ErrorCode.ERR_ConvertToStaticClass
                or ErrorCode.ERR_ConstraintIsStaticClass
                or ErrorCode.ERR_GenericArgIsStaticClass
                or ErrorCode.ERR_ArrayOfStaticClass
                or ErrorCode.ERR_IndexerInStaticClass
                or ErrorCode.ERR_ParameterIsStaticClass
                or ErrorCode.ERR_ReturnTypeIsStaticClass
                or ErrorCode.ERR_VarDeclIsStaticClass
                or ErrorCode.ERR_BadEmptyThrowInFinally
                or ErrorCode.ERR_InvalidSpecifier
                or ErrorCode.WRN_AssignmentToLockOrDispose
                or ErrorCode.ERR_ForwardedTypeInThisAssembly
                or ErrorCode.ERR_ForwardedTypeIsNested
                or ErrorCode.ERR_CycleInTypeForwarder
                or ErrorCode.ERR_AssemblyNameOnNonModule
                or ErrorCode.ERR_InvalidFwdType
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberStatic
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotPublic
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongReturnType
                or ErrorCode.ERR_DuplicateTypeForwarder
                or ErrorCode.ERR_ExpectedSelectOrGroup
                or ErrorCode.ERR_ExpectedContextualKeywordOn
                or ErrorCode.ERR_ExpectedContextualKeywordEquals
                or ErrorCode.ERR_ExpectedContextualKeywordBy
                or ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator
                or ErrorCode.ERR_InvalidInitializerElementInitializer
                or ErrorCode.ERR_InconsistentLambdaParameterUsage
                or ErrorCode.ERR_PartialMemberCannotBeAbstract
                or ErrorCode.ERR_PartialMemberOnlyInPartialClass
                or ErrorCode.ERR_PartialMemberNotExplicit
                or ErrorCode.ERR_PartialMethodExtensionDifference
                or ErrorCode.ERR_PartialMethodOnlyOneLatent
                or ErrorCode.ERR_PartialMethodOnlyOneActual
                or ErrorCode.ERR_PartialMemberParamsDifference
                or ErrorCode.ERR_PartialMethodMustHaveLatent
                or ErrorCode.ERR_PartialMethodInconsistentConstraints
                or ErrorCode.ERR_PartialMethodToDelegate
                or ErrorCode.ERR_PartialMemberStaticDifference
                or ErrorCode.ERR_PartialMemberUnsafeDifference
                or ErrorCode.ERR_PartialMethodInExpressionTree
                or ErrorCode.ERR_ExplicitImplCollisionOnRefOut
                or ErrorCode.ERR_IndirectRecursiveConstructorCall
                or ErrorCode.WRN_ObsoleteOverridingNonObsolete
                or ErrorCode.WRN_DebugFullNameTooLong
                or ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue
                or ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer
                or ErrorCode.ERR_ImplicitlyTypedVariableMultipleDeclarator
                or ErrorCode.ERR_ImplicitlyTypedVariableAssignedArrayInitializer
                or ErrorCode.ERR_ImplicitlyTypedLocalCannotBeFixed
                or ErrorCode.ERR_ImplicitlyTypedVariableCannotBeConst
                or ErrorCode.WRN_ExternCtorNoImplementation
                or ErrorCode.ERR_TypeVarNotFound
                or ErrorCode.ERR_ImplicitlyTypedArrayNoBestType
                or ErrorCode.ERR_AnonymousTypePropertyAssignedBadValue
                or ErrorCode.ERR_ExpressionTreeContainsBaseAccess
                or ErrorCode.ERR_ExpressionTreeContainsAssignment
                or ErrorCode.ERR_AnonymousTypeDuplicatePropertyName
                or ErrorCode.ERR_StatementLambdaToExpressionTree
                or ErrorCode.ERR_ExpressionTreeMustHaveDelegate
                or ErrorCode.ERR_AnonymousTypeNotAvailable
                or ErrorCode.ERR_LambdaInIsAs
                or ErrorCode.ERR_ExpressionTreeContainsMultiDimensionalArrayInitializer
                or ErrorCode.ERR_MissingArgument
                or ErrorCode.ERR_VariableUsedBeforeDeclaration
                or ErrorCode.ERR_UnassignedThisAutoPropertyUnsupportedVersion
                or ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField
                or ErrorCode.ERR_ExpressionTreeContainsBadCoalesce
                or ErrorCode.ERR_ArrayInitializerExpected
                or ErrorCode.ERR_ArrayInitializerIncorrectLength
                or ErrorCode.ERR_ExpressionTreeContainsNamedArgument
                or ErrorCode.ERR_ExpressionTreeContainsOptionalArgument
                or ErrorCode.ERR_ExpressionTreeContainsIndexedProperty
                or ErrorCode.ERR_IndexedPropertyRequiresParams
                or ErrorCode.ERR_IndexedPropertyMustHaveAllOptionalParams
                or ErrorCode.ERR_IdentifierExpected
                or ErrorCode.ERR_SemicolonExpected
                or ErrorCode.ERR_SyntaxError
                or ErrorCode.ERR_DuplicateModifier
                or ErrorCode.ERR_DuplicateAccessor
                or ErrorCode.ERR_IntegralTypeExpected
                or ErrorCode.ERR_IllegalEscape
                or ErrorCode.ERR_NewlineInConst
                or ErrorCode.ERR_EmptyCharConst
                or ErrorCode.ERR_TooManyCharsInConst
                or ErrorCode.ERR_InvalidNumber
                or ErrorCode.ERR_GetOrSetExpected
                or ErrorCode.ERR_ClassTypeExpected
                or ErrorCode.ERR_NamedArgumentExpected
                or ErrorCode.ERR_TooManyCatches
                or ErrorCode.ERR_ThisOrBaseExpected
                or ErrorCode.ERR_OvlUnaryOperatorExpected
                or ErrorCode.ERR_OvlBinaryOperatorExpected
                or ErrorCode.ERR_IntOverflow
                or ErrorCode.ERR_EOFExpected
                or ErrorCode.ERR_BadEmbeddedStmt
                or ErrorCode.ERR_PPDirectiveExpected
                or ErrorCode.ERR_EndOfPPLineExpected
                or ErrorCode.ERR_CloseParenExpected
                or ErrorCode.ERR_EndifDirectiveExpected
                or ErrorCode.ERR_UnexpectedDirective
                or ErrorCode.ERR_ErrorDirective
                or ErrorCode.WRN_WarningDirective
                or ErrorCode.ERR_TypeExpected
                or ErrorCode.ERR_PPDefFollowsToken
                or ErrorCode.ERR_OpenEndedComment
                or ErrorCode.ERR_OvlOperatorExpected
                or ErrorCode.ERR_EndRegionDirectiveExpected
                or ErrorCode.ERR_UnterminatedStringLit
                or ErrorCode.ERR_BadDirectivePlacement
                or ErrorCode.ERR_IdentifierExpectedKW
                or ErrorCode.ERR_SemiOrLBraceExpected
                or ErrorCode.ERR_MultiTypeInDeclaration
                or ErrorCode.ERR_AddOrRemoveExpected
                or ErrorCode.ERR_UnexpectedCharacter
                or ErrorCode.ERR_ProtectedInStatic
                or ErrorCode.WRN_UnreachableGeneralCatch
                or ErrorCode.ERR_IncrementLvalueExpected
                or ErrorCode.ERR_NoSuchMemberOrExtension
                or ErrorCode.WRN_DeprecatedCollectionInitAddStr
                or ErrorCode.ERR_DeprecatedCollectionInitAddStr
                or ErrorCode.WRN_DeprecatedCollectionInitAdd
                or ErrorCode.ERR_DefaultValueNotAllowed
                or ErrorCode.WRN_DefaultValueForUnconsumedLocation
                or ErrorCode.ERR_PartialWrongTypeParamsVariance
                or ErrorCode.ERR_GlobalSingleTypeNameNotFoundFwd
                or ErrorCode.ERR_DottedTypeNameNotFoundInNSFwd
                or ErrorCode.ERR_SingleTypeNameNotFoundFwd
                or ErrorCode.WRN_IdentifierOrNumericLiteralExpected
                or ErrorCode.ERR_UnexpectedToken
                or ErrorCode.ERR_BadThisParam
                or ErrorCode.ERR_BadTypeforThis
                or ErrorCode.ERR_BadParamModThis
                or ErrorCode.ERR_BadExtensionMeth
                or ErrorCode.ERR_BadExtensionAgg
                or ErrorCode.ERR_DupParamMod
                or ErrorCode.ERR_ExtensionMethodsDecl
                or ErrorCode.ERR_ExtensionAttrNotFound
                or ErrorCode.ERR_ExplicitExtension
                or ErrorCode.ERR_ValueTypeExtDelegate
                or ErrorCode.ERR_BadArgCount
                or ErrorCode.ERR_BadArgType
                or ErrorCode.ERR_NoSourceFile
                or ErrorCode.ERR_CantRefResource
                or ErrorCode.ERR_ResourceNotUnique
                or ErrorCode.ERR_ImportNonAssembly
                or ErrorCode.ERR_RefLvalueExpected
                or ErrorCode.ERR_BaseInStaticMeth
                or ErrorCode.ERR_BaseInBadContext
                or ErrorCode.ERR_RbraceExpected
                or ErrorCode.ERR_LbraceExpected
                or ErrorCode.ERR_InExpected
                or ErrorCode.ERR_InvalidPreprocExpr
                or ErrorCode.ERR_InvalidMemberDecl
                or ErrorCode.ERR_MemberNeedsType
                or ErrorCode.ERR_BadBaseType
                or ErrorCode.WRN_EmptySwitch
                or ErrorCode.ERR_ExpectedEndTry
                or ErrorCode.ERR_InvalidExprTerm
                or ErrorCode.ERR_BadNewExpr
                or ErrorCode.ERR_NoNamespacePrivate
                or ErrorCode.ERR_BadVarDecl
                or ErrorCode.ERR_UsingAfterElements
                or ErrorCode.ERR_BadBinOpArgs
                or ErrorCode.ERR_BadUnOpArgs
                or ErrorCode.ERR_NoVoidParameter
                or ErrorCode.ERR_DuplicateAlias
                or ErrorCode.ERR_BadProtectedAccess
                or ErrorCode.ERR_AddModuleAssembly
                or ErrorCode.ERR_BindToBogusProp2
                or ErrorCode.ERR_BindToBogusProp1
                or ErrorCode.ERR_NoVoidHere
                or ErrorCode.ERR_IndexerNeedsParam
                or ErrorCode.ERR_BadArraySyntax
                or ErrorCode.ERR_BadOperatorSyntax
                or ErrorCode.ERR_OutputNeedsName
                or ErrorCode.ERR_CantHaveWin32ResAndManifest
                or ErrorCode.ERR_CantHaveWin32ResAndIcon
                or ErrorCode.ERR_CantReadResource
                or ErrorCode.ERR_DocFileGen
                or ErrorCode.WRN_XMLParseError
                or ErrorCode.WRN_DuplicateParamTag
                or ErrorCode.WRN_UnmatchedParamTag
                or ErrorCode.WRN_MissingParamTag
                or ErrorCode.WRN_BadXMLRef
                or ErrorCode.ERR_BadStackAllocExpr
                or ErrorCode.ERR_InvalidLineNumber
                or ErrorCode.ERR_MissingPPFile
                or ErrorCode.ERR_ForEachMissingMember
                or ErrorCode.WRN_BadXMLRefParamType
                or ErrorCode.WRN_BadXMLRefReturnType
                or ErrorCode.ERR_BadWin32Res
                or ErrorCode.WRN_BadXMLRefSyntax
                or ErrorCode.ERR_BadModifierLocation
                or ErrorCode.ERR_MissingArraySize
                or ErrorCode.WRN_UnprocessedXMLComment
                or ErrorCode.WRN_FailedInclude
                or ErrorCode.WRN_InvalidInclude
                or ErrorCode.WRN_MissingXMLComment
                or ErrorCode.WRN_XMLParseIncludeError
                or ErrorCode.ERR_BadDelArgCount
                or ErrorCode.ERR_UnexpectedSemicolon
                or ErrorCode.ERR_MethodReturnCantBeRefAny
                or ErrorCode.ERR_CompileCancelled
                or ErrorCode.ERR_MethodArgCantBeRefAny
                or ErrorCode.ERR_AssgReadonlyLocal
                or ErrorCode.ERR_RefReadonlyLocal
                or ErrorCode.ERR_CantUseRequiredAttribute
                or ErrorCode.ERR_NoModifiersOnAccessor
                or ErrorCode.ERR_ParamsCantBeWithModifier
                or ErrorCode.ERR_ReturnNotLValue
                or ErrorCode.ERR_MissingCoClass
                or ErrorCode.ERR_AmbiguousAttribute
                or ErrorCode.ERR_BadArgExtraRef
                or ErrorCode.WRN_CmdOptionConflictsSource
                or ErrorCode.ERR_BadCompatMode
                or ErrorCode.ERR_DelegateOnConditional
                or ErrorCode.ERR_CantMakeTempFile
                or ErrorCode.ERR_BadArgRef
                or ErrorCode.ERR_YieldInAnonMeth
                or ErrorCode.ERR_ReturnInIterator
                or ErrorCode.ERR_BadIteratorArgType
                or ErrorCode.ERR_BadIteratorReturn
                or ErrorCode.ERR_BadYieldInFinally
                or ErrorCode.ERR_BadYieldInTryOfCatch
                or ErrorCode.ERR_EmptyYield
                or ErrorCode.ERR_AnonDelegateCantUse
                or ErrorCode.ERR_AnonDelegateCantUseRefLike
                or ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRef
                or ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefLike
                or ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterInMember
                or ErrorCode.ERR_AnonDelegateCantUseStructPrimaryConstructorParameterCaptured
                or ErrorCode.ERR_BadYieldInCatch
                or ErrorCode.ERR_BadDelegateLeave
                or ErrorCode.WRN_IllegalPragma
                or ErrorCode.WRN_IllegalPPWarning
                or ErrorCode.WRN_BadRestoreNumber
                or ErrorCode.ERR_VarargsIterator
                or ErrorCode.ERR_UnsafeIteratorArgType
                or ErrorCode.ERR_BadCoClassSig
                or ErrorCode.ERR_MultipleIEnumOfT
                or ErrorCode.ERR_FixedDimsRequired
                or ErrorCode.ERR_FixedNotInStruct
                or ErrorCode.ERR_AnonymousReturnExpected
                or ErrorCode.WRN_NonECMAFeature
                or ErrorCode.ERR_ExpectedVerbatimLiteral
                or ErrorCode.ERR_AssgReadonly2
                or ErrorCode.ERR_RefReadonly2
                or ErrorCode.ERR_AssgReadonlyStatic2
                or ErrorCode.ERR_RefReadonlyStatic2
                or ErrorCode.ERR_AssgReadonlyLocal2Cause
                or ErrorCode.ERR_RefReadonlyLocal2Cause
                or ErrorCode.ERR_AssgReadonlyLocalCause
                or ErrorCode.ERR_RefReadonlyLocalCause
                or ErrorCode.WRN_ErrorOverride
                or ErrorCode.ERR_AnonMethToNonDel
                or ErrorCode.ERR_CantConvAnonMethParams
                or ErrorCode.ERR_CantConvAnonMethReturns
                or ErrorCode.ERR_IllegalFixedType
                or ErrorCode.ERR_FixedOverflow
                or ErrorCode.ERR_InvalidFixedArraySize
                or ErrorCode.ERR_FixedBufferNotFixed
                or ErrorCode.ERR_AttributeNotOnAccessor
                or ErrorCode.WRN_InvalidSearchPathDir
                or ErrorCode.ERR_IllegalVarArgs
                or ErrorCode.ERR_IllegalParams
                or ErrorCode.ERR_BadModifiersOnNamespace
                or ErrorCode.ERR_BadPlatformType
                or ErrorCode.ERR_ThisStructNotInAnonMeth
                or ErrorCode.ERR_NoConvToIDisp
                or ErrorCode.ERR_BadParamRef
                or ErrorCode.ERR_BadParamExtraRef
                or ErrorCode.ERR_BadParamType
                or ErrorCode.ERR_BadExternIdentifier
                or ErrorCode.ERR_AliasMissingFile
                or ErrorCode.ERR_GlobalExternAlias
                or ErrorCode.WRN_MultiplePredefTypes
                or ErrorCode.ERR_LocalCantBeFixedAndHoisted
                or ErrorCode.WRN_TooManyLinesForDebugger
                or ErrorCode.ERR_CantConvAnonMethNoParams
                or ErrorCode.ERR_ConditionalOnNonAttributeClass
                or ErrorCode.WRN_CallOnNonAgileField
                or ErrorCode.WRN_InvalidNumber
                or ErrorCode.WRN_IllegalPPChecksum
                or ErrorCode.WRN_EndOfPPLineExpected
                or ErrorCode.WRN_ConflictingChecksum
                or ErrorCode.WRN_InvalidAssemblyName
                or ErrorCode.WRN_UnifyReferenceMajMin
                or ErrorCode.WRN_UnifyReferenceBldRev
                or ErrorCode.ERR_DuplicateImport
                or ErrorCode.ERR_DuplicateImportSimple
                or ErrorCode.ERR_AssemblyMatchBadVersion
                or ErrorCode.ERR_FixedNeedsLvalue
                or ErrorCode.WRN_DuplicateTypeParamTag
                or ErrorCode.WRN_UnmatchedTypeParamTag
                or ErrorCode.WRN_MissingTypeParamTag
                or ErrorCode.ERR_CantChangeTypeOnOverride
                or ErrorCode.ERR_DoNotUseFixedBufferAttr
                or ErrorCode.WRN_AssignmentToSelf
                or ErrorCode.WRN_ComparisonToSelf
                or ErrorCode.ERR_CantOpenWin32Res
                or ErrorCode.WRN_DotOnDefault
                or ErrorCode.ERR_NoMultipleInheritance
                or ErrorCode.ERR_BaseClassMustBeFirst
                or ErrorCode.WRN_BadXMLRefTypeVar
                or ErrorCode.ERR_FriendAssemblyBadArgs
                or ErrorCode.ERR_FriendAssemblySNReq
                or ErrorCode.ERR_DelegateOnNullable
                or ErrorCode.ERR_BadCtorArgCount
                or ErrorCode.ERR_GlobalAttributesNotFirst
                or ErrorCode.ERR_ExpressionExpected
                or ErrorCode.WRN_UnmatchedParamRefTag
                or ErrorCode.WRN_UnmatchedTypeParamRefTag
                or ErrorCode.ERR_DefaultValueMustBeConstant
                or ErrorCode.ERR_DefaultValueBeforeRequiredValue
                or ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgument
                or ErrorCode.ERR_BadNamedArgument
                or ErrorCode.ERR_DuplicateNamedArgument
                or ErrorCode.ERR_RefOutDefaultValue
                or ErrorCode.ERR_NamedArgumentForArray
                or ErrorCode.ERR_DefaultValueForExtensionParameter
                or ErrorCode.ERR_NamedArgumentUsedInPositional
                or ErrorCode.ERR_DefaultValueUsedWithAttributes
                or ErrorCode.ERR_BadNamedArgumentForDelegateInvoke
                or ErrorCode.ERR_NoPIAAssemblyMissingAttribute
                or ErrorCode.ERR_NoCanonicalView
                or ErrorCode.ERR_NoConversionForDefaultParam
                or ErrorCode.ERR_DefaultValueForParamsParameter
                or ErrorCode.ERR_NewCoClassOnLink
                or ErrorCode.ERR_NoPIANestedType
                or ErrorCode.ERR_InteropTypeMissingAttribute
                or ErrorCode.ERR_InteropStructContainsMethods
                or ErrorCode.ERR_InteropTypesWithSameNameAndGuid
                or ErrorCode.ERR_NoPIAAssemblyMissingAttributes
                or ErrorCode.ERR_AssemblySpecifiedForLinkAndRef
                or ErrorCode.ERR_LocalTypeNameClash
                or ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA
                or ErrorCode.ERR_NotNullRefDefaultParameter
                or ErrorCode.ERR_FixedLocalInLambda
                or ErrorCode.ERR_MissingMethodOnSourceInterface
                or ErrorCode.ERR_MissingSourceInterface
                or ErrorCode.ERR_GenericsUsedInNoPIAType
                or ErrorCode.ERR_GenericsUsedAcrossAssemblies
                or ErrorCode.ERR_NoConversionForNubDefaultParam
                or ErrorCode.ERR_InvalidSubsystemVersion
                or ErrorCode.ERR_InteropMethodWithBody
                or ErrorCode.ERR_BadWarningLevel
                or ErrorCode.ERR_BadDebugType
                or ErrorCode.ERR_BadResourceVis
                or ErrorCode.ERR_DefaultValueTypeMustMatch
                or ErrorCode.ERR_DefaultValueBadValueType
                or ErrorCode.ERR_MemberAlreadyInitialized
                or ErrorCode.ERR_MemberCannotBeInitialized
                or ErrorCode.ERR_StaticMemberInObjectInitializer
                or ErrorCode.ERR_ReadonlyValueTypeInObjectInitializer
                or ErrorCode.ERR_ValueTypePropertyInObjectInitializer
                or ErrorCode.ERR_UnsafeTypeInObjectCreation
                or ErrorCode.ERR_EmptyElementInitializer
                or ErrorCode.ERR_InitializerAddHasWrongSignature
                or ErrorCode.ERR_CollectionInitRequiresIEnumerable
                or ErrorCode.ERR_CantOpenWin32Manifest
                or ErrorCode.WRN_CantHaveManifestForModule
                or ErrorCode.ERR_BadInstanceArgType
                or ErrorCode.ERR_QueryDuplicateRangeVariable
                or ErrorCode.ERR_QueryRangeVariableOverrides
                or ErrorCode.ERR_QueryRangeVariableAssignedBadValue
                or ErrorCode.ERR_QueryNoProviderCastable
                or ErrorCode.ERR_QueryNoProviderStandard
                or ErrorCode.ERR_QueryNoProvider
                or ErrorCode.ERR_QueryOuterKey
                or ErrorCode.ERR_QueryInnerKey
                or ErrorCode.ERR_QueryOutRefRangeVariable
                or ErrorCode.ERR_QueryMultipleProviders
                or ErrorCode.ERR_QueryTypeInferenceFailedMulti
                or ErrorCode.ERR_QueryTypeInferenceFailed
                or ErrorCode.ERR_QueryTypeInferenceFailedSelectMany
                or ErrorCode.ERR_ExpressionTreeContainsPointerOp
                or ErrorCode.ERR_ExpressionTreeContainsAnonymousMethod
                or ErrorCode.ERR_AnonymousMethodToExpressionTree
                or ErrorCode.ERR_QueryRangeVariableReadOnly
                or ErrorCode.ERR_QueryRangeVariableSameAsTypeParam
                or ErrorCode.ERR_TypeVarNotFoundRangeVariable
                or ErrorCode.ERR_BadArgTypesForCollectionAdd
                or ErrorCode.ERR_ByRefParameterInExpressionTree
                or ErrorCode.ERR_VarArgsInExpressionTree
                or ErrorCode.ERR_InitializerAddHasParamModifiers
                or ErrorCode.ERR_NonInvocableMemberCalled
                or ErrorCode.WRN_MultipleRuntimeImplementationMatches
                or ErrorCode.WRN_MultipleRuntimeOverrideMatches
                or ErrorCode.ERR_ObjectOrCollectionInitializerWithDelegateCreation
                or ErrorCode.ERR_InvalidConstantDeclarationType
                or ErrorCode.ERR_IllegalVarianceSyntax
                or ErrorCode.ERR_UnexpectedVariance
                or ErrorCode.ERR_BadDynamicTypeof
                or ErrorCode.ERR_ExpressionTreeContainsDynamicOperation
                or ErrorCode.ERR_BadDynamicConversion
                or ErrorCode.ERR_DeriveFromDynamic
                or ErrorCode.ERR_DeriveFromConstructedDynamic
                or ErrorCode.ERR_DynamicTypeAsBound
                or ErrorCode.ERR_ConstructedDynamicTypeAsBound
                or ErrorCode.ERR_ExplicitDynamicAttr
                or ErrorCode.ERR_NoDynamicPhantomOnBase
                or ErrorCode.ERR_NoDynamicPhantomOnBaseIndexer
                or ErrorCode.ERR_BadArgTypeDynamicExtension
                or ErrorCode.WRN_DynamicDispatchToConditionalMethod
                or ErrorCode.ERR_NoDynamicPhantomOnBaseCtor
                or ErrorCode.ERR_BadDynamicMethodArgMemgrp
                or ErrorCode.ERR_BadDynamicMethodArgLambda
                or ErrorCode.ERR_BadDynamicMethodArg
                or ErrorCode.ERR_BadDynamicQuery
                or ErrorCode.ERR_DynamicAttributeMissing
                or ErrorCode.WRN_IsDynamicIsConfusing
                or ErrorCode.ERR_BadAsyncReturn
                or ErrorCode.ERR_BadAwaitInFinally
                or ErrorCode.ERR_BadAwaitInCatch
                or ErrorCode.ERR_BadAwaitArg
                or ErrorCode.ERR_BadAsyncArgType
                or ErrorCode.ERR_BadAsyncExpressionTree
                or ErrorCode.ERR_MixingWinRTEventWithRegular
                or ErrorCode.ERR_BadAwaitWithoutAsync
                or ErrorCode.ERR_BadAsyncLacksBody
                or ErrorCode.ERR_BadAwaitInQuery
                or ErrorCode.ERR_BadAwaitInLock
                or ErrorCode.ERR_TaskRetNoObjectRequired
                or ErrorCode.ERR_FileNotFound
                or ErrorCode.WRN_FileAlreadyIncluded
                or ErrorCode.ERR_NoFileSpec
                or ErrorCode.ERR_SwitchNeedsString
                or ErrorCode.ERR_BadSwitch
                or ErrorCode.WRN_NoSources
                or ErrorCode.ERR_OpenResponseFile
                or ErrorCode.ERR_CantOpenFileWrite
                or ErrorCode.ERR_BadBaseNumber
                or ErrorCode.ERR_BinaryFile
                or ErrorCode.FTL_BadCodepage
                or ErrorCode.ERR_NoMainOnDLL
                or ErrorCode.FTL_InvalidTarget
                or ErrorCode.FTL_InvalidInputFileName
                or ErrorCode.WRN_NoConfigNotOnCommandLine
                or ErrorCode.ERR_InvalidFileAlignment
                or ErrorCode.WRN_DefineIdentifierRequired
                or ErrorCode.FTL_OutputFileExists
                or ErrorCode.ERR_OneAliasPerReference
                or ErrorCode.ERR_SwitchNeedsNumber
                or ErrorCode.ERR_MissingDebugSwitch
                or ErrorCode.ERR_ComRefCallInExpressionTree
                or ErrorCode.WRN_BadUILang
                or ErrorCode.ERR_InvalidFormatForGuidForOption
                or ErrorCode.ERR_MissingGuidForOption
                or ErrorCode.ERR_InvalidOutputName
                or ErrorCode.ERR_InvalidDebugInformationFormat
                or ErrorCode.ERR_LegacyObjectIdSyntax
                or ErrorCode.ERR_SourceLinkRequiresPdb
                or ErrorCode.ERR_CannotEmbedWithoutPdb
                or ErrorCode.ERR_BadSwitchValue
                or ErrorCode.WRN_CLS_NoVarArgs
                or ErrorCode.WRN_CLS_BadArgType
                or ErrorCode.WRN_CLS_BadReturnType
                or ErrorCode.WRN_CLS_BadFieldPropType
                or ErrorCode.WRN_CLS_BadIdentifierCase
                or ErrorCode.WRN_CLS_OverloadRefOut
                or ErrorCode.WRN_CLS_OverloadUnnamed
                or ErrorCode.WRN_CLS_BadIdentifier
                or ErrorCode.WRN_CLS_BadBase
                or ErrorCode.WRN_CLS_BadInterfaceMember
                or ErrorCode.WRN_CLS_NoAbstractMembers
                or ErrorCode.WRN_CLS_NotOnModules
                or ErrorCode.WRN_CLS_ModuleMissingCLS
                or ErrorCode.WRN_CLS_AssemblyNotCLS
                or ErrorCode.WRN_CLS_BadAttributeType
                or ErrorCode.WRN_CLS_ArrayArgumentToAttribute
                or ErrorCode.WRN_CLS_NotOnModules2
                or ErrorCode.WRN_CLS_IllegalTrueInFalse
                or ErrorCode.WRN_CLS_MeaninglessOnPrivateType
                or ErrorCode.WRN_CLS_AssemblyNotCLS2
                or ErrorCode.WRN_CLS_MeaninglessOnParam
                or ErrorCode.WRN_CLS_MeaninglessOnReturn
                or ErrorCode.WRN_CLS_BadTypeVar
                or ErrorCode.WRN_CLS_VolatileField
                or ErrorCode.WRN_CLS_BadInterface
                or ErrorCode.FTL_BadChecksumAlgorithm
                or ErrorCode.ERR_BadAwaitArgIntrinsic
                or ErrorCode.ERR_BadAwaitAsIdentifier
                or ErrorCode.ERR_AwaitInUnsafeContext
                or ErrorCode.ERR_UnsafeAsyncArgType
                or ErrorCode.ERR_VarargsAsync
                or ErrorCode.ERR_BadAwaitArgVoidCall
                or ErrorCode.ERR_CantConvAsyncAnonFuncReturns
                or ErrorCode.ERR_BadAwaiterPattern
                or ErrorCode.ERR_BadSpecialByRefParameter
                or ErrorCode.WRN_UnobservedAwaitableExpression
                or ErrorCode.ERR_SynchronizedAsyncMethod
                or ErrorCode.ERR_BadAsyncReturnExpression
                or ErrorCode.ERR_NoConversionForCallerLineNumberParam
                or ErrorCode.ERR_NoConversionForCallerFilePathParam
                or ErrorCode.ERR_NoConversionForCallerMemberNameParam
                or ErrorCode.ERR_BadCallerLineNumberParamWithoutDefaultValue
                or ErrorCode.ERR_BadCallerFilePathParamWithoutDefaultValue
                or ErrorCode.ERR_BadCallerMemberNameParamWithoutDefaultValue
                or ErrorCode.ERR_BadPrefer32OnLib
                or ErrorCode.WRN_CallerLineNumberParamForUnconsumedLocation
                or ErrorCode.WRN_CallerFilePathParamForUnconsumedLocation
                or ErrorCode.WRN_CallerMemberNameParamForUnconsumedLocation
                or ErrorCode.ERR_DoesntImplementAwaitInterface
                or ErrorCode.ERR_BadAwaitArg_NeedSystem
                or ErrorCode.ERR_CantReturnVoid
                or ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsync
                or ErrorCode.ERR_SecurityCriticalOrSecuritySafeCriticalOnAsyncInClassOrStruct
                or ErrorCode.ERR_BadAwaitWithoutAsyncMethod
                or ErrorCode.ERR_BadAwaitWithoutVoidAsyncMethod
                or ErrorCode.ERR_BadAwaitWithoutAsyncLambda
                or ErrorCode.ERR_NoSuchMemberOrExtensionNeedUsing
                or ErrorCode.ERR_UnexpectedAliasedName
                or ErrorCode.ERR_UnexpectedGenericName
                or ErrorCode.ERR_UnexpectedUnboundGenericName
                or ErrorCode.ERR_GlobalStatement
                or ErrorCode.ERR_BadUsingType
                or ErrorCode.ERR_ReservedAssemblyName
                or ErrorCode.ERR_PPReferenceFollowsToken
                or ErrorCode.ERR_ExpectedPPFile
                or ErrorCode.ERR_ReferenceDirectiveOnlyAllowedInScripts
                or ErrorCode.ERR_NameNotInContextPossibleMissingReference
                or ErrorCode.ERR_MetadataNameTooLong
                or ErrorCode.ERR_AttributesNotAllowed
                or ErrorCode.ERR_ExternAliasNotAllowed
                or ErrorCode.ERR_ConflictingAliasAndDefinition
                or ErrorCode.ERR_GlobalDefinitionOrStatementExpected
                or ErrorCode.ERR_ExpectedSingleScript
                or ErrorCode.ERR_RecursivelyTypedVariable
                or ErrorCode.ERR_YieldNotAllowedInScript
                or ErrorCode.ERR_NamespaceNotAllowedInScript
                or ErrorCode.WRN_StaticInAsOrIs
                or ErrorCode.ERR_InvalidDelegateType
                or ErrorCode.ERR_BadVisEventType
                or ErrorCode.ERR_GlobalAttributesNotAllowed
                or ErrorCode.ERR_PublicKeyFileFailure
                or ErrorCode.ERR_PublicKeyContainerFailure
                or ErrorCode.ERR_FriendRefSigningMismatch
                or ErrorCode.ERR_CannotPassNullForFriendAssembly
                or ErrorCode.ERR_SignButNoPrivateKey
                or ErrorCode.WRN_DelaySignButNoKey
                or ErrorCode.ERR_InvalidVersionFormat
                or ErrorCode.WRN_InvalidVersionFormat
                or ErrorCode.ERR_NoCorrespondingArgument
                or ErrorCode.ERR_ResourceFileNameNotUnique
                or ErrorCode.ERR_DllImportOnGenericMethod
                or ErrorCode.ERR_EncUpdateFailedMissingSymbol
                or ErrorCode.ERR_ParameterNotValidForType
                or ErrorCode.ERR_AttributeParameterRequired1
                or ErrorCode.ERR_AttributeParameterRequired2
                or ErrorCode.ERR_SecurityAttributeMissingAction
                or ErrorCode.ERR_SecurityAttributeInvalidAction
                or ErrorCode.ERR_SecurityAttributeInvalidActionAssembly
                or ErrorCode.ERR_SecurityAttributeInvalidActionTypeOrMethod
                or ErrorCode.ERR_PrincipalPermissionInvalidAction
                or ErrorCode.ERR_FeatureNotValidInExpressionTree
                or ErrorCode.ERR_MarshalUnmanagedTypeNotValidForFields
                or ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields
                or ErrorCode.ERR_PermissionSetAttributeInvalidFile
                or ErrorCode.ERR_PermissionSetAttributeFileReadError
                or ErrorCode.ERR_InvalidVersionFormat2
                or ErrorCode.ERR_InvalidAssemblyCultureForExe
                or ErrorCode.ERR_DuplicateAttributeInNetModule
                or ErrorCode.ERR_CantOpenIcon
                or ErrorCode.ERR_ErrorBuildingWin32Resources
                or ErrorCode.ERR_BadAttributeParamDefaultArgument
                or ErrorCode.ERR_MissingTypeInSource
                or ErrorCode.ERR_MissingTypeInAssembly
                or ErrorCode.ERR_SecurityAttributeInvalidTarget
                or ErrorCode.ERR_InvalidAssemblyName
                or ErrorCode.ERR_NoTypeDefFromModule
                or ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName
                or ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName
                or ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath
                or ErrorCode.ERR_InvalidDynamicCondition
                or ErrorCode.ERR_WinRtEventPassedByRef
                or ErrorCode.ERR_NetModuleNameMismatch
                or ErrorCode.ERR_BadModuleName
                or ErrorCode.ERR_BadCompilationOptionValue
                or ErrorCode.ERR_BadAppConfigPath
                or ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden
                or ErrorCode.ERR_CmdOptionConflictsSource
                or ErrorCode.ERR_FixedBufferTooManyDimensions
                or ErrorCode.ERR_CantReadConfigFile
                or ErrorCode.ERR_BadAwaitInCatchFilter
                or ErrorCode.WRN_FilterIsConstantTrue
                or ErrorCode.ERR_EncNoPIAReference
                or ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage
                or ErrorCode.ERR_MetadataReferencesNotSupported
                or ErrorCode.ERR_InvalidAssemblyCulture
                or ErrorCode.ERR_EncReferenceToAddedMember
                or ErrorCode.ERR_MutuallyExclusiveOptions
                or ErrorCode.ERR_InvalidDebugInfo
                or ErrorCode.WRN_UnimplementedCommandLineSwitch
                or ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName
                or ErrorCode.ERR_InvalidSignaturePublicKey
                or ErrorCode.ERR_ForwardedTypesConflict
                or ErrorCode.WRN_RefCultureMismatch
                or ErrorCode.ERR_AgnosticToMachineModule
                or ErrorCode.ERR_ConflictingMachineModule
                or ErrorCode.WRN_ConflictingMachineAssembly
                or ErrorCode.ERR_CryptoHashFailed
                or ErrorCode.ERR_MissingNetModuleReference
                or ErrorCode.ERR_NetModuleNameMustBeUnique
                or ErrorCode.ERR_UnsupportedTransparentIdentifierAccess
                or ErrorCode.ERR_ParamDefaultValueDiffersFromAttribute
                or ErrorCode.WRN_UnqualifiedNestedTypeInCref
                or ErrorCode.HDN_UnusedUsingDirective
                or ErrorCode.HDN_UnusedExternAlias
                or ErrorCode.WRN_NoRuntimeMetadataVersion
                or ErrorCode.ERR_FeatureNotAvailableInVersion1
                or ErrorCode.ERR_FeatureNotAvailableInVersion2
                or ErrorCode.ERR_FeatureNotAvailableInVersion3
                or ErrorCode.ERR_FeatureNotAvailableInVersion4
                or ErrorCode.ERR_FeatureNotAvailableInVersion5
                or ErrorCode.ERR_FieldHasMultipleDistinctConstantValues
                or ErrorCode.ERR_ComImportWithInitializers
                or ErrorCode.WRN_PdbLocalNameTooLong
                or ErrorCode.ERR_RetNoObjectRequiredLambda
                or ErrorCode.ERR_TaskRetNoObjectRequiredLambda
                or ErrorCode.WRN_AnalyzerCannotBeCreated
                or ErrorCode.WRN_NoAnalyzerInAssembly
                or ErrorCode.WRN_UnableToLoadAnalyzer
                or ErrorCode.ERR_CantReadRulesetFile
                or ErrorCode.ERR_BadPdbData
                or ErrorCode.INF_UnableToLoadSomeTypesInAnalyzer
                or ErrorCode.ERR_InitializerOnNonAutoProperty
                or ErrorCode.ERR_AutoPropertyMustHaveGetAccessor
                or ErrorCode.ERR_InstancePropertyInitializerInInterface
                or ErrorCode.ERR_EnumsCantContainDefaultConstructor
                or ErrorCode.ERR_EncodinglessSyntaxTree
                or ErrorCode.ERR_BlockBodyAndExpressionBody
                or ErrorCode.ERR_FeatureIsExperimental
                or ErrorCode.ERR_FeatureNotAvailableInVersion6
                or ErrorCode.ERR_SwitchFallOut
                or ErrorCode.ERR_NullPropagatingOpInExpressionTree
                or ErrorCode.WRN_NubExprIsConstBool2
                or ErrorCode.ERR_DictionaryInitializerInExpressionTree
                or ErrorCode.ERR_ExtensionCollectionElementInitializerInExpressionTree
                or ErrorCode.ERR_UnclosedExpressionHole
                or ErrorCode.ERR_UseDefViolationProperty
                or ErrorCode.ERR_AutoPropertyMustOverrideSet
                or ErrorCode.ERR_ExpressionHasNoName
                or ErrorCode.ERR_SubexpressionNotInNameof
                or ErrorCode.ERR_AliasQualifiedNameNotAnExpression
                or ErrorCode.ERR_NameofMethodGroupWithTypeParameters
                or ErrorCode.ERR_NoAliasHere
                or ErrorCode.ERR_UnescapedCurly
                or ErrorCode.ERR_EscapedCurly
                or ErrorCode.ERR_TrailingWhitespaceInFormatSpecifier
                or ErrorCode.ERR_EmptyFormatSpecifier
                or ErrorCode.ERR_ErrorInReferencedAssembly
                or ErrorCode.ERR_ExternHasConstructorInitializer
                or ErrorCode.ERR_ExpressionOrDeclarationExpected
                or ErrorCode.ERR_NameofExtensionMethod
                or ErrorCode.WRN_AlignmentMagnitude
                or ErrorCode.ERR_ConstantStringTooLong
                or ErrorCode.ERR_DebugEntryPointNotSourceMethodDefinition
                or ErrorCode.ERR_LoadDirectiveOnlyAllowedInScripts
                or ErrorCode.ERR_PPLoadFollowsToken
                or ErrorCode.ERR_SourceFileReferencesNotSupported
                or ErrorCode.ERR_BadAwaitInStaticVariableInitializer
                or ErrorCode.ERR_InvalidPathMap
                or ErrorCode.ERR_PublicSignButNoKey
                or ErrorCode.ERR_TooManyUserStrings
                or ErrorCode.ERR_TooManyUserStrings_RestartRequired
                or ErrorCode.ERR_PeWritingFailure
                or ErrorCode.WRN_AttributeIgnoredWhenPublicSigning
                or ErrorCode.ERR_OptionMustBeAbsolutePath
                or ErrorCode.ERR_FeatureNotAvailableInVersion7
                or ErrorCode.ERR_DynamicLocalFunctionParamsParameter
                or ErrorCode.ERR_ExpressionTreeContainsLocalFunction
                or ErrorCode.ERR_InvalidInstrumentationKind
                or ErrorCode.ERR_LocalFunctionMissingBody
                or ErrorCode.ERR_InvalidHashAlgorithmName
                or ErrorCode.ERR_ThrowMisplaced
                or ErrorCode.ERR_PatternNullableType
                or ErrorCode.ERR_BadPatternExpression
                or ErrorCode.ERR_SwitchExpressionValueExpected
                or ErrorCode.ERR_SwitchCaseSubsumed
                or ErrorCode.ERR_PatternWrongType
                or ErrorCode.ERR_ExpressionTreeContainsIsMatch
                or ErrorCode.WRN_TupleLiteralNameMismatch
                or ErrorCode.ERR_TupleTooFewElements
                or ErrorCode.ERR_TupleReservedElementName
                or ErrorCode.ERR_TupleReservedElementNameAnyPosition
                or ErrorCode.ERR_TupleDuplicateElementName
                or ErrorCode.ERR_PredefinedTypeMemberNotFoundInAssembly
                or ErrorCode.ERR_MissingDeconstruct
                or ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable
                or ErrorCode.ERR_DeconstructRequiresExpression
                or ErrorCode.ERR_DeconstructWrongCardinality
                or ErrorCode.ERR_CannotDeconstructDynamic
                or ErrorCode.ERR_DeconstructTooFewElements
                or ErrorCode.ERR_ConversionNotTupleCompatible
                or ErrorCode.ERR_DeconstructionVarFormDisallowsSpecificType
                or ErrorCode.ERR_TupleElementNamesAttributeMissing
                or ErrorCode.ERR_ExplicitTupleElementNamesAttribute
                or ErrorCode.ERR_CantChangeTupleNamesOnOverride
                or ErrorCode.ERR_DuplicateInterfaceWithTupleNamesInBaseList
                or ErrorCode.ERR_ImplBadTupleNames
                or ErrorCode.ERR_PartialMemberInconsistentTupleNames
                or ErrorCode.ERR_ExpressionTreeContainsTupleLiteral
                or ErrorCode.ERR_ExpressionTreeContainsTupleConversion
                or ErrorCode.ERR_AutoPropertyCannotBeRefReturning
                or ErrorCode.ERR_RefPropertyMustHaveGetAccessor
                or ErrorCode.ERR_RefPropertyCannotHaveSetAccessor
                or ErrorCode.ERR_CantChangeRefReturnOnOverride
                or ErrorCode.ERR_MustNotHaveRefReturn
                or ErrorCode.ERR_MustHaveRefReturn
                or ErrorCode.ERR_RefReturnMustHaveIdentityConversion
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongRefReturn
                or ErrorCode.ERR_RefReturningCallInExpressionTree
                or ErrorCode.ERR_BadIteratorReturnRef
                or ErrorCode.ERR_BadRefReturnExpressionTree
                or ErrorCode.ERR_RefReturnLvalueExpected
                or ErrorCode.ERR_RefReturnNonreturnableLocal
                or ErrorCode.ERR_RefReturnNonreturnableLocal2
                or ErrorCode.ERR_RefReturnRangeVariable
                or ErrorCode.ERR_RefReturnReadonly
                or ErrorCode.ERR_RefReturnReadonlyStatic
                or ErrorCode.ERR_RefReturnReadonly2
                or ErrorCode.ERR_RefReturnReadonlyStatic2
                or ErrorCode.ERR_RefReturnParameter
                or ErrorCode.ERR_RefReturnParameter2
                or ErrorCode.ERR_RefReturnLocal
                or ErrorCode.ERR_RefReturnLocal2
                or ErrorCode.ERR_RefReturnStructThis
                or ErrorCode.ERR_InitializeByValueVariableWithReference
                or ErrorCode.ERR_InitializeByReferenceVariableWithValue
                or ErrorCode.ERR_RefAssignmentMustHaveIdentityConversion
                or ErrorCode.ERR_ByReferenceVariableMustBeInitialized
                or ErrorCode.ERR_AnonDelegateCantUseLocal
                or ErrorCode.ERR_PredefinedValueTupleTypeNotFound
                or ErrorCode.ERR_SemiOrLBraceOrArrowExpected
                or ErrorCode.ERR_NewWithTupleTypeSyntax
                or ErrorCode.ERR_PredefinedValueTupleTypeMustBeStruct
                or ErrorCode.ERR_DiscardTypeInferenceFailed
                or ErrorCode.ERR_DeclarationExpressionNotPermitted
                or ErrorCode.ERR_MustDeclareForeachIteration
                or ErrorCode.ERR_TupleElementNamesInDeconstruction
                or ErrorCode.ERR_ExpressionTreeContainsThrowExpression
                or ErrorCode.ERR_DelegateRefMismatch
                or ErrorCode.ERR_BadSourceCodeKind
                or ErrorCode.ERR_BadDocumentationMode
                or ErrorCode.ERR_BadLanguageVersion
                or ErrorCode.ERR_ImplicitlyTypedVariableUsedInForbiddenZone
                or ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable
                or ErrorCode.ERR_ExpressionTreeContainsOutVariable
                or ErrorCode.ERR_VarInvocationLvalueReserved
                or ErrorCode.ERR_PublicSignNetModule
                or ErrorCode.ERR_BadAssemblyName
                or ErrorCode.ERR_BadAsyncMethodBuilderTaskProperty
                or ErrorCode.ERR_TypeForwardedToMultipleAssemblies
                or ErrorCode.ERR_ExpressionTreeContainsDiscard
                or ErrorCode.ERR_PatternDynamicType
                or ErrorCode.ERR_VoidAssignment
                or ErrorCode.ERR_VoidInTuple
                or ErrorCode.ERR_Merge_conflict_marker_encountered
                or ErrorCode.ERR_InvalidPreprocessingSymbol
                or ErrorCode.ERR_FeatureNotAvailableInVersion7_1
                or ErrorCode.ERR_LanguageVersionCannotHaveLeadingZeroes
                or ErrorCode.ERR_CompilerAndLanguageVersion
                or ErrorCode.WRN_WindowsExperimental
                or ErrorCode.ERR_TupleInferredNamesNotAvailable
                or ErrorCode.ERR_TypelessTupleInAs
                or ErrorCode.ERR_NoRefOutWhenRefOnly
                or ErrorCode.ERR_NoNetModuleOutputWhenRefOutOrRefOnly
                or ErrorCode.ERR_BadOpOnNullOrDefaultOrNew
                or ErrorCode.ERR_DefaultLiteralNotValid
                or ErrorCode.ERR_PatternWrongGenericTypeInVersion
                or ErrorCode.ERR_AmbigBinaryOpsOnDefault
                or ErrorCode.ERR_FeatureNotAvailableInVersion7_2
                or ErrorCode.WRN_UnreferencedLocalFunction
                or ErrorCode.ERR_DynamicLocalFunctionTypeParameter
                or ErrorCode.ERR_BadNonTrailingNamedArgument
                or ErrorCode.ERR_NamedArgumentSpecificationBeforeFixedArgumentInDynamicInvocation
                or ErrorCode.ERR_RefConditionalAndAwait
                or ErrorCode.ERR_RefConditionalNeedsTwoRefs
                or ErrorCode.ERR_RefConditionalDifferentTypes
                or ErrorCode.ERR_BadParameterModifiers
                or ErrorCode.ERR_RefReadonlyNotField
                or ErrorCode.ERR_RefReadonlyNotField2
                or ErrorCode.ERR_AssignReadonlyNotField
                or ErrorCode.ERR_AssignReadonlyNotField2
                or ErrorCode.ERR_RefReturnReadonlyNotField
                or ErrorCode.ERR_RefReturnReadonlyNotField2
                or ErrorCode.ERR_ExplicitReservedAttr
                or ErrorCode.ERR_TypeReserved
                or ErrorCode.ERR_EmbeddedAttributeMustFollowPattern
                or ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne
                or ErrorCode.ERR_InExtensionMustBeValueType
                or ErrorCode.ERR_FieldsInRoStruct
                or ErrorCode.ERR_AutoPropsInRoStruct
                or ErrorCode.ERR_FieldlikeEventsInRoStruct
                or ErrorCode.ERR_FieldAutoPropCantBeByRefLike
                or ErrorCode.ERR_StackAllocConversionNotPossible
                or ErrorCode.ERR_EscapeCall
                or ErrorCode.ERR_EscapeCall2
                or ErrorCode.ERR_EscapeOther
                or ErrorCode.ERR_CallArgMixing
                or ErrorCode.ERR_MismatchedRefEscapeInTernary
                or ErrorCode.ERR_EscapeVariable
                or ErrorCode.ERR_EscapeStackAlloc
                or ErrorCode.ERR_RefReturnThis
                or ErrorCode.ERR_OutAttrOnInParam
                or ErrorCode.ERR_PredefinedTypeAmbiguous
                or ErrorCode.ERR_InvalidVersionFormatDeterministic
                or ErrorCode.ERR_AttributeCtorInParameter
                or ErrorCode.WRN_FilterIsConstantFalse
                or ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch
                or ErrorCode.ERR_ConditionalInInterpolation
                or ErrorCode.ERR_CantUseVoidInArglist
                or ErrorCode.ERR_InDynamicMethodArg
                or ErrorCode.ERR_FeatureNotAvailableInVersion7_3
                or ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable
                or ErrorCode.ERR_DoNotUseFixedBufferAttrOnProperty
                or ErrorCode.ERR_RefLocalOrParamExpected
                or ErrorCode.ERR_RefAssignNarrower
                or ErrorCode.ERR_NewBoundWithUnmanaged
                or ErrorCode.ERR_UnmanagedConstraintNotSatisfied
                or ErrorCode.ERR_CantUseInOrOutInArglist
                or ErrorCode.ERR_ConWithUnmanagedCon
                or ErrorCode.ERR_UnmanagedBoundWithClass
                or ErrorCode.ERR_InvalidStackAllocArray
                or ErrorCode.ERR_ExpressionTreeContainsTupleBinOp
                or ErrorCode.WRN_TupleBinopLiteralNameMismatch
                or ErrorCode.ERR_TupleSizesMismatchForBinOps
                or ErrorCode.ERR_ExprCannotBeFixed
                or ErrorCode.ERR_InvalidObjectCreation
                or ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter
                or ErrorCode.ERR_OutVariableCannotBeByRef
                or ErrorCode.ERR_DeconstructVariableCannotBeByRef
                or ErrorCode.ERR_OmittedTypeArgument
                or ErrorCode.ERR_FeatureNotAvailableInVersion8
                or ErrorCode.ERR_AltInterpolatedVerbatimStringsNotAvailable
                or ErrorCode.ERR_IteratorMustBeAsync
                or ErrorCode.ERR_NoConvToIAsyncDisp
                or ErrorCode.ERR_AwaitForEachMissingMember
                or ErrorCode.ERR_BadGetAsyncEnumerator
                or ErrorCode.ERR_MultipleIAsyncEnumOfT
                or ErrorCode.ERR_ForEachMissingMemberWrongAsync
                or ErrorCode.ERR_AwaitForEachMissingMemberWrongAsync
                or ErrorCode.ERR_BadDynamicAwaitForEach
                or ErrorCode.ERR_NoConvToIAsyncDispWrongAsync
                or ErrorCode.ERR_NoConvToIDispWrongAsync
                or ErrorCode.ERR_StaticLocalFunctionCannotCaptureVariable
                or ErrorCode.ERR_StaticLocalFunctionCannotCaptureThis
                or ErrorCode.ERR_AttributeNotOnEventAccessor
                or ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage
                or ErrorCode.WRN_UndecoratedCancellationTokenParameter
                or ErrorCode.ERR_MultipleEnumeratorCancellationAttributes
                or ErrorCode.ERR_VarianceInterfaceNesting
                or ErrorCode.ERR_ImplicitIndexIndexerWithName
                or ErrorCode.ERR_ImplicitRangeIndexerWithName
                or ErrorCode.ERR_WrongNumberOfSubpatterns
                or ErrorCode.ERR_PropertyPatternNameMissing
                or ErrorCode.ERR_MissingPattern
                or ErrorCode.ERR_DefaultPattern
                or ErrorCode.ERR_SwitchExpressionNoBestType
                or ErrorCode.ERR_VarMayNotBindToType
                or ErrorCode.WRN_SwitchExpressionNotExhaustive
                or ErrorCode.ERR_SwitchArmSubsumed
                or ErrorCode.ERR_ConstantPatternVsOpenType
                or ErrorCode.WRN_CaseConstantNamedUnderscore
                or ErrorCode.WRN_IsTypeNamedUnderscore
                or ErrorCode.ERR_ExpressionTreeContainsSwitchExpression
                or ErrorCode.ERR_SwitchGoverningExpressionRequiresParens
                or ErrorCode.ERR_TupleElementNameMismatch
                or ErrorCode.ERR_DeconstructParameterNameMismatch
                or ErrorCode.ERR_IsPatternImpossible
                or ErrorCode.WRN_GivenExpressionNeverMatchesPattern
                or ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant
                or ErrorCode.ERR_PointerTypeInPatternMatching
                or ErrorCode.ERR_ArgumentNameInITuplePattern
                or ErrorCode.ERR_DiscardPatternInSwitchStatement
                or ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue
                or ErrorCode.WRN_ThrowPossibleNull
                or ErrorCode.ERR_IllegalSuppression
                or ErrorCode.WRN_ConvertingNullableToNonNullable
                or ErrorCode.WRN_NullReferenceAssignment
                or ErrorCode.WRN_NullReferenceReceiver
                or ErrorCode.WRN_NullReferenceReturn
                or ErrorCode.WRN_NullReferenceArgument
                or ErrorCode.WRN_UnboxPossibleNull
                or ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment
                or ErrorCode.WRN_NullabilityMismatchInTypeOnOverride
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial
                or ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation
                or ErrorCode.WRN_UninitializedNonNullableField
                or ErrorCode.WRN_NullabilityMismatchInAssignment
                or ErrorCode.WRN_NullabilityMismatchInArgument
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate
                or ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate
                or ErrorCode.ERR_ExplicitNullableAttribute
                or ErrorCode.WRN_NullabilityMismatchInArgumentForOutput
                or ErrorCode.WRN_NullAsNonNullable
                or ErrorCode.ERR_NullableUnconstrainedTypeParameter
                or ErrorCode.ERR_AnnotationDisallowedInObjectCreation
                or ErrorCode.WRN_NullableValueTypeMayBeNull
                or ErrorCode.ERR_NullableOptionNotAvailable
                or ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint
                or ErrorCode.WRN_MissingNonNullTypesContextForAnnotation
                or ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation
                or ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint
                or ErrorCode.ERR_TripleDotNotAllowed
                or ErrorCode.ERR_BadNullableContextOption
                or ErrorCode.ERR_NullableDirectiveQualifierExpected
                or ErrorCode.ERR_BadNullableTypeof
                or ErrorCode.ERR_ExpressionTreeCantContainRefStruct
                or ErrorCode.ERR_ElseCannotStartStatement
                or ErrorCode.ERR_ExpressionTreeCantContainNullCoalescingAssignment
                or ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface
                or ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase
                or ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList
                or ErrorCode.ERR_DuplicateExplicitImpl
                or ErrorCode.ERR_UsingVarInSwitchCase
                or ErrorCode.ERR_GoToForwardJumpOverUsingVar
                or ErrorCode.ERR_GoToBackwardJumpOverUsingVar
                or ErrorCode.ERR_IsNullableType
                or ErrorCode.ERR_AsNullableType
                or ErrorCode.ERR_FeatureInPreview
                or ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull
                or ErrorCode.WRN_ImplicitCopyInReadOnlyMember
                or ErrorCode.ERR_StaticMemberCantBeReadOnly
                or ErrorCode.ERR_AutoSetterCantBeReadOnly
                or ErrorCode.ERR_AutoPropertyWithSetterCantBeReadOnly
                or ErrorCode.ERR_InvalidPropertyReadOnlyMods
                or ErrorCode.ERR_DuplicatePropertyReadOnlyMods
                or ErrorCode.ERR_FieldLikeEventCantBeReadOnly
                or ErrorCode.ERR_PartialMemberReadOnlyDifference
                or ErrorCode.ERR_ReadOnlyModMissingAccessor
                or ErrorCode.ERR_OverrideRefConstraintNotSatisfied
                or ErrorCode.ERR_OverrideValConstraintNotSatisfied
                or ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation
                or ErrorCode.ERR_NullableDirectiveTargetExpected
                or ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode
                or ErrorCode.WRN_NullReferenceInitializer
                or ErrorCode.ERR_MultipleAnalyzerConfigsInSameDir
                or ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementation
                or ErrorCode.ERR_RuntimeDoesNotSupportDefaultInterfaceImplementationForMember
                or ErrorCode.ERR_InvalidModifierForLanguageVersion
                or ErrorCode.ERR_ImplicitImplementationOfNonPublicInterfaceMember
                or ErrorCode.ERR_MostSpecificImplementationIsNotFound
                or ErrorCode.ERR_LanguageVersionDoesNotSupportInterfaceImplementationForMember
                or ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember
                or ErrorCode.ERR_DefaultInterfaceImplementationInNoPIAType
                or ErrorCode.ERR_AbstractEventHasAccessors
                or ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint
                or ErrorCode.ERR_DuplicateNullSuppression
                or ErrorCode.ERR_DefaultLiteralNoTargetType
                or ErrorCode.ERR_ReAbstractionInNoPIAType
                or ErrorCode.ERR_InternalError
                or ErrorCode.ERR_ImplicitObjectCreationIllegalTargetType
                or ErrorCode.ERR_ImplicitObjectCreationNotValid
                or ErrorCode.ERR_ImplicitObjectCreationNoTargetType
                or ErrorCode.ERR_BadFuncPointerParamModifier
                or ErrorCode.ERR_BadFuncPointerArgCount
                or ErrorCode.ERR_MethFuncPtrMismatch
                or ErrorCode.ERR_FuncPtrRefMismatch
                or ErrorCode.ERR_FuncPtrMethMustBeStatic
                or ErrorCode.ERR_ExternEventInitializer
                or ErrorCode.ERR_AmbigBinaryOpsOnUnconstrainedDefault
                or ErrorCode.WRN_ParameterConditionallyDisallowsNull
                or ErrorCode.WRN_ShouldNotReturn
                or ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride
                or ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride
                or ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation
                or ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation
                or ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation
                or ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation
                or ErrorCode.WRN_DoesNotReturnMismatch
                or ErrorCode.ERR_NoOutputDirectory
                or ErrorCode.ERR_StdInOptionProvidedButConsoleInputIsNotRedirected
                or ErrorCode.ERR_FeatureNotAvailableInVersion9
                or ErrorCode.WRN_MemberNotNull
                or ErrorCode.WRN_MemberNotNullWhen
                or ErrorCode.WRN_MemberNotNullBadMember
                or ErrorCode.WRN_ParameterDisallowsNull
                or ErrorCode.WRN_ConstOutOfRangeChecked
                or ErrorCode.ERR_DuplicateInterfaceWithDifferencesInBaseList
                or ErrorCode.ERR_DesignatorBeneathPatternCombinator
                or ErrorCode.ERR_UnsupportedTypeForRelationalPattern
                or ErrorCode.ERR_RelationalPatternWithNaN
                or ErrorCode.ERR_ConditionalOnLocalFunction
                or ErrorCode.WRN_GeneratorFailedDuringInitialization
                or ErrorCode.WRN_GeneratorFailedDuringGeneration
                or ErrorCode.ERR_WrongFuncPtrCallingConvention
                or ErrorCode.ERR_MissingAddressOf
                or ErrorCode.ERR_CannotUseReducedExtensionMethodInAddressOf
                or ErrorCode.ERR_CannotUseFunctionPointerAsFixedLocal
                or ErrorCode.ERR_ExpressionTreeContainsPatternImplicitIndexer
                or ErrorCode.ERR_ExpressionTreeContainsFromEndIndexExpression
                or ErrorCode.ERR_ExpressionTreeContainsRangeExpression
                or ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern
                or ErrorCode.WRN_IsPatternAlways
                or ErrorCode.ERR_PartialMethodWithAccessibilityModsMustHaveImplementation
                or ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods
                or ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods
                or ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods
                or ErrorCode.ERR_PartialMemberAccessibilityDifference
                or ErrorCode.ERR_PartialMemberExtendedModDifference
                or ErrorCode.ERR_SimpleProgramLocalIsReferencedOutsideOfTopLevelStatement
                or ErrorCode.ERR_SimpleProgramMultipleUnitsWithTopLevelStatements
                or ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType
                or ErrorCode.ERR_SimpleProgramNotAnExecutable
                or ErrorCode.ERR_UnsupportedCallingConvention
                or ErrorCode.ERR_InvalidFunctionPointerCallingConvention
                or ErrorCode.ERR_InvalidFuncPointerReturnTypeModifier
                or ErrorCode.ERR_DupReturnTypeMod
                or ErrorCode.ERR_AddressOfMethodGroupInExpressionTree
                or ErrorCode.ERR_CannotConvertAddressOfToDelegate
                or ErrorCode.ERR_AddressOfToNonFunctionPointer
                or ErrorCode.ERR_ModuleInitializerMethodMustBeOrdinary
                or ErrorCode.ERR_ModuleInitializerMethodMustBeAccessibleOutsideTopLevelType
                or ErrorCode.ERR_ModuleInitializerMethodMustBeStaticParameterlessVoid
                or ErrorCode.ERR_ModuleInitializerMethodAndContainingTypesMustNotBeGeneric
                or ErrorCode.ERR_PartialMethodReturnTypeDifference
                or ErrorCode.ERR_PartialMemberRefReturnDifference
                or ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial
                or ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureVariable
                or ErrorCode.ERR_StaticAnonymousFunctionCannotCaptureThis
                or ErrorCode.ERR_OverrideDefaultConstraintNotSatisfied
                or ErrorCode.ERR_DefaultConstraintOverrideOnly
                or ErrorCode.WRN_ParameterNotNullIfNotNull
                or ErrorCode.WRN_ReturnNotNullIfNotNull
                or ErrorCode.WRN_PartialMethodTypeDifference
                or ErrorCode.ERR_RuntimeDoesNotSupportCovariantReturnsOfClasses
                or ErrorCode.ERR_RuntimeDoesNotSupportCovariantPropertiesOfClasses
                or ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen
                or ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen
                or ErrorCode.WRN_PrecedenceInversion
                or ErrorCode.ERR_ExpressionTreeContainsWithExpression
                or ErrorCode.WRN_AnalyzerReferencesFramework
                or ErrorCode.WRN_RecordEqualsWithoutGetHashCode
                or ErrorCode.ERR_AssignmentInitOnly
                or ErrorCode.ERR_CantChangeInitOnlyOnOverride
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberWrongInitOnly
                or ErrorCode.ERR_ExplicitPropertyMismatchInitOnly
                or ErrorCode.ERR_BadInitAccessor
                or ErrorCode.ERR_InvalidWithReceiverType
                or ErrorCode.ERR_CannotClone
                or ErrorCode.ERR_CloneDisallowedInRecord
                or ErrorCode.WRN_RecordNamedDisallowed
                or ErrorCode.ERR_UnexpectedArgumentList
                or ErrorCode.ERR_UnexpectedOrMissingConstructorInitializerInRecord
                or ErrorCode.ERR_MultipleRecordParameterLists
                or ErrorCode.ERR_BadRecordBase
                or ErrorCode.ERR_BadInheritanceFromRecord
                or ErrorCode.ERR_BadRecordMemberForPositionalParameter
                or ErrorCode.ERR_NoCopyConstructorInBaseType
                or ErrorCode.ERR_CopyConstructorMustInvokeBaseCopyConstructor
                or ErrorCode.ERR_DoesNotOverrideMethodFromObject
                or ErrorCode.ERR_SealedAPIInRecord
                or ErrorCode.ERR_DoesNotOverrideBaseMethod
                or ErrorCode.ERR_NotOverridableAPIInRecord
                or ErrorCode.ERR_NonPublicAPIInRecord
                or ErrorCode.ERR_SignatureMismatchInRecord
                or ErrorCode.ERR_NonProtectedAPIInRecord
                or ErrorCode.ERR_DoesNotOverrideBaseEqualityContract
                or ErrorCode.ERR_StaticAPIInRecord
                or ErrorCode.ERR_CopyConstructorWrongAccessibility
                or ErrorCode.ERR_NonPrivateAPIInRecord
                or ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion
                or ErrorCode.WRN_UnassignedThisUnsupportedVersion
                or ErrorCode.WRN_ParamUnassigned
                or ErrorCode.WRN_UseDefViolationProperty
                or ErrorCode.WRN_UseDefViolationField
                or ErrorCode.WRN_UseDefViolationThisUnsupportedVersion
                or ErrorCode.WRN_UseDefViolationOut
                or ErrorCode.WRN_UseDefViolation
                or ErrorCode.ERR_CannotSpecifyManagedWithUnmanagedSpecifiers
                or ErrorCode.ERR_RuntimeDoesNotSupportUnmanagedDefaultCallConv
                or ErrorCode.ERR_TypeNotFound
                or ErrorCode.ERR_TypeMustBePublic
                or ErrorCode.ERR_InvalidUnmanagedCallersOnlyCallConv
                or ErrorCode.ERR_CannotUseManagedTypeInUnmanagedCallersOnly
                or ErrorCode.ERR_UnmanagedCallersOnlyMethodOrTypeCannotBeGeneric
                or ErrorCode.ERR_UnmanagedCallersOnlyRequiresStatic
                or ErrorCode.WRN_ParameterIsStaticClass
                or ErrorCode.WRN_ReturnTypeIsStaticClass
                or ErrorCode.ERR_EntryPointCannotBeUnmanagedCallersOnly
                or ErrorCode.ERR_ModuleInitializerCannotBeUnmanagedCallersOnly
                or ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeCalledDirectly
                or ErrorCode.ERR_UnmanagedCallersOnlyMethodsCannotBeConvertedToDelegate
                or ErrorCode.ERR_InitCannotBeReadonly
                or ErrorCode.ERR_UnexpectedVarianceStaticMember
                or ErrorCode.ERR_FunctionPointersCannotBeCalledWithNamedArguments
                or ErrorCode.ERR_EqualityContractRequiresGetter
                or ErrorCode.WRN_UnreadRecordParameter
                or ErrorCode.ERR_BadFieldTypeInRecord
                or ErrorCode.WRN_DoNotCompareFunctionPointers
                or ErrorCode.ERR_RecordAmbigCtor
                or ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported
                or ErrorCode.ERR_InheritingFromRecordWithSealedToString
                or ErrorCode.ERR_HiddenPositionalMember
                or ErrorCode.ERR_GlobalUsingInNamespace
                or ErrorCode.ERR_GlobalUsingOutOfOrder
                or ErrorCode.ERR_AttributesRequireParenthesizedLambdaExpression
                or ErrorCode.ERR_CannotInferDelegateType
                or ErrorCode.ERR_InvalidNameInSubpattern
                or ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfaces
                or ErrorCode.ERR_GenericConstraintNotSatisfiedInterfaceWithStaticAbstractMembers
                or ErrorCode.ERR_BadAbstractUnaryOperatorSignature
                or ErrorCode.ERR_BadAbstractIncDecSignature
                or ErrorCode.ERR_BadAbstractIncDecRetType
                or ErrorCode.ERR_BadAbstractBinaryOperatorSignature
                or ErrorCode.ERR_BadAbstractShiftOperatorSignature
                or ErrorCode.ERR_BadAbstractStaticMemberAccess
                or ErrorCode.ERR_ExpressionTreeContainsAbstractStaticMemberAccess
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberNotStatic
                or ErrorCode.ERR_RuntimeDoesNotSupportStaticAbstractMembersInInterfacesForMember
                or ErrorCode.ERR_ExplicitImplementationOfOperatorsMustBeStatic
                or ErrorCode.ERR_AbstractConversionNotInvolvingContainedType
                or ErrorCode.ERR_InterfaceImplementedByUnmanagedCallersOnlyMethod
                or ErrorCode.HDN_DuplicateWithGlobalUsing
                or ErrorCode.ERR_CantConvAnonMethReturnType
                or ErrorCode.ERR_BuilderAttributeDisallowed
                or ErrorCode.ERR_FeatureNotAvailableInVersion10
                or ErrorCode.ERR_SimpleProgramIsEmpty
                or ErrorCode.ERR_LineSpanDirectiveInvalidValue
                or ErrorCode.ERR_LineSpanDirectiveEndLessThanStart
                or ErrorCode.ERR_WrongArityAsyncReturn
                or ErrorCode.ERR_InterpolatedStringHandlerMethodReturnMalformed
                or ErrorCode.ERR_InterpolatedStringHandlerMethodReturnInconsistent
                or ErrorCode.ERR_NullInvalidInterpolatedStringHandlerArgumentName
                or ErrorCode.ERR_NotInstanceInvalidInterpolatedStringHandlerArgumentName
                or ErrorCode.ERR_InvalidInterpolatedStringHandlerArgumentName
                or ErrorCode.ERR_TypeIsNotAnInterpolatedStringHandlerType
                or ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter
                or ErrorCode.ERR_CannotUseSelfAsInterpolatedStringHandlerArgument
                or ErrorCode.ERR_InterpolatedStringHandlerArgumentAttributeMalformed
                or ErrorCode.ERR_InterpolatedStringHandlerArgumentLocatedAfterInterpolatedString
                or ErrorCode.ERR_InterpolatedStringHandlerArgumentOptionalNotSpecified
                or ErrorCode.ERR_ExpressionTreeContainsInterpolatedStringHandlerConversion
                or ErrorCode.ERR_InterpolatedStringHandlerCreationCannotUseDynamic
                or ErrorCode.ERR_MultipleFileScopedNamespace
                or ErrorCode.ERR_FileScopedAndNormalNamespace
                or ErrorCode.ERR_FileScopedNamespaceNotBeforeAllMembers
                or ErrorCode.ERR_NoImplicitConvTargetTypedConditional
                or ErrorCode.ERR_NonPublicParameterlessStructConstructor
                or ErrorCode.ERR_NoConversionForCallerArgumentExpressionParam
                or ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression
                or ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression
                or ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression
                or ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName
                or ErrorCode.ERR_BadCallerArgumentExpressionParamWithoutDefaultValue
                or ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential
                or ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation
                or ErrorCode.ERR_NewlinesAreNotAllowedInsideANonVerbatimInterpolatedString
                or ErrorCode.ERR_AttrTypeArgCannotBeTypeVar
                or ErrorCode.ERR_AttrDependentTypeNotAllowed
                or ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters
                or ErrorCode.ERR_LambdaWithAttributesToExpressionTree
                or ErrorCode.WRN_CompileTimeCheckedOverflow
                or ErrorCode.WRN_MethGrpToNonDel
                or ErrorCode.ERR_LambdaExplicitReturnTypeVar
                or ErrorCode.ERR_InterpolatedStringsReferencingInstanceCannotBeInObjectInitializers
                or ErrorCode.ERR_CannotUseRefInUnmanagedCallersOnly
                or ErrorCode.ERR_CannotBeMadeNullable
                or ErrorCode.ERR_UnsupportedTypeForListPattern
                or ErrorCode.ERR_MisplacedSlicePattern
                or ErrorCode.WRN_LowerCaseTypeName
                or ErrorCode.ERR_RecordStructConstructorCallsDefaultConstructor
                or ErrorCode.ERR_StructHasInitializersAndNoDeclaredConstructor
                or ErrorCode.ERR_ListPatternRequiresLength
                or ErrorCode.ERR_ScopedMismatchInParameterOfTarget
                or ErrorCode.ERR_ScopedMismatchInParameterOfOverrideOrImplementation
                or ErrorCode.ERR_ScopedMismatchInParameterOfPartial
                or ErrorCode.ERR_RawStringNotInDirectives
                or ErrorCode.ERR_UnterminatedRawString
                or ErrorCode.ERR_TooManyQuotesForRawString
                or ErrorCode.ERR_LineDoesNotStartWithSameWhitespace
                or ErrorCode.ERR_RawStringDelimiterOnOwnLine
                or ErrorCode.ERR_RawStringInVerbatimInterpolatedStrings
                or ErrorCode.ERR_RawStringMustContainContent
                or ErrorCode.ERR_LineContainsDifferentWhitespace
                or ErrorCode.ERR_NotEnoughQuotesForRawString
                or ErrorCode.ERR_NotEnoughCloseBracesForRawString
                or ErrorCode.ERR_TooManyOpenBracesForRawString
                or ErrorCode.ERR_TooManyCloseBracesForRawString
                or ErrorCode.ERR_IllegalAtSequence
                or ErrorCode.ERR_StringMustStartWithQuoteCharacter
                or ErrorCode.ERR_NoEnumConstraint
                or ErrorCode.ERR_NoDelegateConstraint
                or ErrorCode.ERR_MisplacedRecord
                or ErrorCode.ERR_PatternSpanCharCannotBeStringNull
                or ErrorCode.ERR_UseDefViolationPropertyUnsupportedVersion
                or ErrorCode.ERR_UseDefViolationFieldUnsupportedVersion
                or ErrorCode.WRN_UseDefViolationPropertyUnsupportedVersion
                or ErrorCode.WRN_UseDefViolationFieldUnsupportedVersion
                or ErrorCode.WRN_UseDefViolationPropertySupportedVersion
                or ErrorCode.WRN_UseDefViolationFieldSupportedVersion
                or ErrorCode.WRN_UseDefViolationThisSupportedVersion
                or ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion
                or ErrorCode.WRN_UnassignedThisSupportedVersion
                or ErrorCode.ERR_OperatorCantBeChecked
                or ErrorCode.ERR_ImplicitConversionOperatorCantBeChecked
                or ErrorCode.ERR_CheckedOperatorNeedsMatch
                or ErrorCode.ERR_MisplacedUnchecked
                or ErrorCode.ERR_LineSpanDirectiveRequiresSpace
                or ErrorCode.ERR_RequiredNameDisallowed
                or ErrorCode.ERR_OverrideMustHaveRequired
                or ErrorCode.ERR_RequiredMemberCannotBeHidden
                or ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType
                or ErrorCode.ERR_ExplicitRequiredMember
                or ErrorCode.ERR_RequiredMemberMustBeSettable
                or ErrorCode.ERR_RequiredMemberMustBeSet
                or ErrorCode.ERR_RequiredMembersMustBeAssignedValue
                or ErrorCode.ERR_RequiredMembersInvalid
                or ErrorCode.ERR_RequiredMembersBaseTypeInvalid
                or ErrorCode.ERR_ChainingToSetsRequiredMembersRequiresSetsRequiredMembers
                or ErrorCode.ERR_NewConstraintCannotHaveRequiredMembers
                or ErrorCode.ERR_UnsupportedCompilerFeature
                or ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired
                or ErrorCode.ERR_RefReturningPropertiesCannotBeRequired
                or ErrorCode.ERR_ImplicitImplementationOfInaccessibleInterfaceMember
                or ErrorCode.ERR_ScriptsAndSubmissionsCannotHaveRequiredMembers
                or ErrorCode.ERR_BadAbstractEqualityOperatorSignature
                or ErrorCode.ERR_BadBinaryReadOnlySpanConcatenation
                or ErrorCode.ERR_ScopedRefAndRefStructOnly
                or ErrorCode.ERR_ScopedDiscard
                or ErrorCode.ERR_FixedFieldMustNotBeRef
                or ErrorCode.ERR_RefFieldCannotReferToRefStruct
                or ErrorCode.ERR_FileTypeDisallowedInSignature
                or ErrorCode.ERR_FileTypeNoExplicitAccessibility
                or ErrorCode.ERR_FileTypeBase
                or ErrorCode.ERR_FileTypeNested
                or ErrorCode.ERR_GlobalUsingStaticFileType
                or ErrorCode.ERR_FileTypeNameDisallowed
                or ErrorCode.ERR_FeatureNotAvailableInVersion11
                or ErrorCode.ERR_RefFieldInNonRefStruct
                or ErrorCode.WRN_AnalyzerReferencesNewerCompiler
                or ErrorCode.ERR_CannotMatchOnINumberBase
                or ErrorCode.ERR_ScopedTypeNameDisallowed
                or ErrorCode.ERR_ImplicitlyTypedDefaultParameter
                or ErrorCode.ERR_UnscopedRefAttributeUnsupportedTarget
                or ErrorCode.ERR_RuntimeDoesNotSupportRefFields
                or ErrorCode.ERR_ExplicitScopedRef
                or ErrorCode.ERR_UnscopedScoped
                or ErrorCode.WRN_DuplicateAnalyzerReference
                or ErrorCode.ERR_FilePathCannotBeConvertedToUtf8
                or ErrorCode.ERR_FileLocalDuplicateNameInNS
                or ErrorCode.WRN_ScopedMismatchInParameterOfTarget
                or ErrorCode.WRN_ScopedMismatchInParameterOfOverrideOrImplementation
                or ErrorCode.ERR_RefReturnScopedParameter
                or ErrorCode.ERR_RefReturnScopedParameter2
                or ErrorCode.ERR_RefReturnOnlyParameter
                or ErrorCode.ERR_RefReturnOnlyParameter2
                or ErrorCode.ERR_RefAssignReturnOnly
                or ErrorCode.WRN_ManagedAddr
                or ErrorCode.WRN_EscapeVariable
                or ErrorCode.WRN_EscapeStackAlloc
                or ErrorCode.WRN_RefReturnNonreturnableLocal
                or ErrorCode.WRN_RefReturnNonreturnableLocal2
                or ErrorCode.WRN_RefReturnStructThis
                or ErrorCode.WRN_RefAssignNarrower
                or ErrorCode.WRN_MismatchedRefEscapeInTernary
                or ErrorCode.WRN_RefReturnParameter
                or ErrorCode.WRN_RefReturnScopedParameter
                or ErrorCode.WRN_RefReturnParameter2
                or ErrorCode.WRN_RefReturnScopedParameter2
                or ErrorCode.WRN_RefReturnLocal
                or ErrorCode.WRN_RefReturnLocal2
                or ErrorCode.WRN_RefAssignReturnOnly
                or ErrorCode.WRN_RefReturnOnlyParameter
                or ErrorCode.WRN_RefReturnOnlyParameter2
                or ErrorCode.ERR_RefAssignValEscapeWider
                or ErrorCode.WRN_RefAssignValEscapeWider
                or ErrorCode.WRN_OptionalParamValueMismatch
                or ErrorCode.WRN_ParamsArrayInLambdaOnly
                or ErrorCode.ERR_UnscopedRefAttributeUnsupportedMemberTarget
                or ErrorCode.ERR_UnscopedRefAttributeInterfaceImplementation
                or ErrorCode.ERR_UnrecognizedRefSafetyRulesAttributeVersion
                or ErrorCode.ERR_InvalidPrimaryConstructorParameterReference
                or ErrorCode.ERR_AmbiguousPrimaryConstructorParameterAsColorColorReceiver
                or ErrorCode.WRN_CapturedPrimaryConstructorParameterPassedToBase
                or ErrorCode.WRN_UnreadPrimaryConstructorParameter
                or ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter
                or ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter
                or ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter
                or ErrorCode.ERR_AssgReadonlyPrimaryConstructorParameter2
                or ErrorCode.ERR_RefReturnReadonlyPrimaryConstructorParameter2
                or ErrorCode.ERR_RefReadonlyPrimaryConstructorParameter2
                or ErrorCode.ERR_RefReturnPrimaryConstructorParameter
                or ErrorCode.ERR_StructLayoutCyclePrimaryConstructorParameter
                or ErrorCode.ERR_UnexpectedParameterList
                or ErrorCode.WRN_AddressOfInAsync
                or ErrorCode.ERR_BadRefInUsingAlias
                or ErrorCode.ERR_BadUnsafeInUsingDirective
                or ErrorCode.ERR_BadNullableReferenceTypeInUsingAlias
                or ErrorCode.ERR_BadStaticAfterUnsafe
                or ErrorCode.ERR_BadCaseInSwitchArm
                or ErrorCode.ERR_InterceptorsFeatureNotEnabled
                or ErrorCode.ERR_InterceptorContainingTypeCannotBeGeneric
                or ErrorCode.ERR_InterceptorPathNotInCompilation
                or ErrorCode.ERR_InterceptorPathNotInCompilationWithCandidate
                or ErrorCode.ERR_InterceptorPositionBadToken
                or ErrorCode.ERR_InterceptorLineOutOfRange
                or ErrorCode.ERR_InterceptorCharacterOutOfRange
                or ErrorCode.ERR_InterceptorMethodMustBeOrdinary
                or ErrorCode.ERR_InterceptorMustReferToStartOfTokenPosition
                or ErrorCode.ERR_InterceptorFilePathCannotBeNull
                or ErrorCode.ERR_InterceptorNameNotInvoked
                or ErrorCode.ERR_InterceptorNonUniquePath
                or ErrorCode.ERR_InterceptorLineCharacterMustBePositive
                or ErrorCode.ERR_ConstantValueOfTypeExpected
                or ErrorCode.ERR_UnsupportedPrimaryConstructorParameterCapturingRefAny
                or ErrorCode.ERR_InterceptorCannotUseUnmanagedCallersOnly
                or ErrorCode.ERR_BadUsingStaticType
                or ErrorCode.WRN_CapturedPrimaryConstructorParameterInFieldInitializer
                or ErrorCode.ERR_InlineArrayConversionToSpanNotSupported
                or ErrorCode.ERR_InlineArrayConversionToReadOnlySpanNotSupported
                or ErrorCode.ERR_InlineArrayIndexOutOfRange
                or ErrorCode.ERR_InvalidInlineArrayLength
                or ErrorCode.ERR_InvalidInlineArrayLayout
                or ErrorCode.ERR_InvalidInlineArrayFields
                or ErrorCode.ERR_ExpressionTreeContainsInlineArrayOperation
                or ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes
                or ErrorCode.ERR_InlineArrayBadIndex
                or ErrorCode.ERR_NamedArgumentForInlineArray
                or ErrorCode.ERR_CollectionExpressionTargetTypeNotConstructible
                or ErrorCode.ERR_ExpressionTreeContainsCollectionExpression
                or ErrorCode.ERR_CollectionExpressionNoTargetType
                or ErrorCode.WRN_PrimaryConstructorParameterIsShadowedAndNotPassedToBase
                or ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier
                or ErrorCode.WRN_InlineArrayIndexerNotUsed
                or ErrorCode.WRN_InlineArraySliceNotUsed
                or ErrorCode.WRN_InlineArrayConversionOperatorNotUsed
                or ErrorCode.WRN_InlineArrayNotSupportedByLanguage
                or ErrorCode.ERR_CollectionBuilderAttributeMethodNotFound
                or ErrorCode.ERR_CollectionBuilderAttributeInvalidType
                or ErrorCode.ERR_CollectionBuilderAttributeInvalidMethodName
                or ErrorCode.ERR_CollectionBuilderNoElementType
                or ErrorCode.ERR_InlineArrayForEachNotSupported
                or ErrorCode.ERR_RefReadOnlyWrongOrdering
                or ErrorCode.WRN_BadArgRef
                or ErrorCode.WRN_ArgExpectedRefOrIn
                or ErrorCode.WRN_RefReadonlyNotVariable
                or ErrorCode.ERR_BadArgExtraRefLangVersion
                or ErrorCode.WRN_ArgExpectedIn
                or ErrorCode.WRN_OverridingDifferentRefness
                or ErrorCode.WRN_HidingDifferentRefness
                or ErrorCode.WRN_TargetDifferentRefness
                or ErrorCode.ERR_OutAttrOnRefReadonlyParam
                or ErrorCode.WRN_RefReadonlyParameterDefaultValue
                or ErrorCode.WRN_ByValArraySizeConstRequired
                or ErrorCode.WRN_UseDefViolationRefField
                or ErrorCode.ERR_FeatureNotAvailableInVersion12
                or ErrorCode.ERR_CollectionExpressionEscape
                or ErrorCode.WRN_Experimental
                or ErrorCode.WRN_ExperimentalWithMessage
                or ErrorCode.ERR_ExpectedInterpolatedString
                or ErrorCode.ERR_InterceptorGlobalNamespace
                or ErrorCode.WRN_CollectionExpressionRefStructMayAllocate
                or ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate
                or ErrorCode.ERR_CollectionExpressionImmutableArray
                or ErrorCode.ERR_InvalidExperimentalDiagID
                or ErrorCode.ERR_SpreadMissingMember
                or ErrorCode.ERR_CollectionExpressionTargetNoElementType
                or ErrorCode.ERR_CollectionExpressionMissingConstructor
                or ErrorCode.ERR_CollectionExpressionMissingAdd
                or ErrorCode.WRN_ConvertingLock
                or ErrorCode.ERR_DynamicDispatchToParamsCollection
                or ErrorCode.ERR_CollectionInitializerInfiniteChainOfAddCalls
                or ErrorCode.ERR_ParamsCollectionInfiniteChainOfConstructorCalls
                or ErrorCode.ERR_ParamsMemberCannotBeLessVisibleThanDeclaringMember
                or ErrorCode.ERR_ParamsCollectionConstructorDoesntInitializeRequiredMember
                or ErrorCode.ERR_ParamsCollectionExpressionTree
                or ErrorCode.ERR_ParamsCollectionExtensionAddMethod
                or ErrorCode.ERR_ParamsCollectionMissingConstructor
                or ErrorCode.ERR_NoModifiersOnUsing
                or ErrorCode.ERR_CannotDynamicInvokeOnExpression
                or ErrorCode.ERR_InterceptsLocationDataInvalidFormat
                or ErrorCode.ERR_InterceptsLocationUnsupportedVersion
                or ErrorCode.ERR_InterceptsLocationDuplicateFile
                or ErrorCode.ERR_InterceptsLocationFileNotFound
                or ErrorCode.ERR_InterceptsLocationDataInvalidPosition
                or ErrorCode.INF_TooManyBoundLambdas
                or ErrorCode.ERR_BadYieldInUnsafe
                or ErrorCode.ERR_AddressOfInIterator
                or ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics
                or ErrorCode.ERR_RefStructConstraintAlreadySpecified
                or ErrorCode.ERR_AllowsClauseMustBeLast
                or ErrorCode.ERR_ClassIsCombinedWithRefStruct
                or ErrorCode.ERR_NotRefStructConstraintNotSatisfied
                or ErrorCode.ERR_RefStructDoesNotSupportDefaultInterfaceImplementationForMember
                or ErrorCode.ERR_BadNonVirtualInterfaceMemberAccessOnAllowsRefLike
                or ErrorCode.ERR_BadAllowByRefLikeEnumerator
                or ErrorCode.ERR_PartialPropertyMissingImplementation
                or ErrorCode.ERR_PartialPropertyMissingDefinition
                or ErrorCode.ERR_PartialPropertyDuplicateDefinition
                or ErrorCode.ERR_PartialPropertyDuplicateImplementation
                or ErrorCode.ERR_PartialPropertyMissingAccessor
                or ErrorCode.ERR_PartialPropertyUnexpectedAccessor
                or ErrorCode.ERR_PartialPropertyInitMismatch
                or ErrorCode.ERR_PartialMemberTypeDifference
                or ErrorCode.WRN_PartialMemberSignatureDifference
                or ErrorCode.ERR_PartialPropertyRequiredDifference
                or ErrorCode.WRN_FieldIsAmbiguous
                or ErrorCode.ERR_InlineArrayAttributeOnRecord
                or ErrorCode.ERR_FeatureNotAvailableInVersion13
                or ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToOverride
                or ErrorCode.ERR_CannotApplyOverloadResolutionPriorityToMember
                or ErrorCode.ERR_PartialPropertyDuplicateInitializer
                or ErrorCode.WRN_UninitializedNonNullableBackingField
                or ErrorCode.WRN_UnassignedInternalRefField
                or ErrorCode.WRN_AccessorDoesNotUseBackingField
                or ErrorCode.ERR_IteratorRefLikeElementType
                or ErrorCode.WRN_UnscopedRefAttributeOldRules
                or ErrorCode.WRN_InterceptsLocationAttributeUnsupportedSignature
                or ErrorCode.ERR_ImplicitlyTypedParamsParameter
                or ErrorCode.ERR_VariableDeclarationNamedField
                or ErrorCode.ERR_PartialMemberMissingImplementation
                or ErrorCode.ERR_PartialMemberMissingDefinition
                or ErrorCode.ERR_PartialMemberDuplicateDefinition
                or ErrorCode.ERR_PartialMemberDuplicateImplementation
                or ErrorCode.ERR_PartialEventInitializer
                or ErrorCode.ERR_PartialConstructorInitializer
                or ErrorCode.ERR_ExtensionDisallowsName
                or ErrorCode.ERR_ExtensionDisallowsMember
                or ErrorCode.ERR_BadExtensionContainingType
                or ErrorCode.ERR_ExtensionParameterDisallowsDefaultValue
                or ErrorCode.ERR_ReceiverParameterOnlyOne
                or ErrorCode.ERR_ExtensionResolutionFailed
                or ErrorCode.ERR_ReceiverParameterSameNameAsTypeParameter
                or ErrorCode.ERR_LocalSameNameAsExtensionTypeParameter
                or ErrorCode.ERR_TypeParameterSameNameAsExtensionTypeParameter
                or ErrorCode.ERR_LocalSameNameAsExtensionParameter
                or ErrorCode.ERR_ValueParameterSameNameAsExtensionParameter
                or ErrorCode.ERR_TypeParameterSameNameAsExtensionParameter
                or ErrorCode.ERR_InvalidExtensionParameterReference
                or ErrorCode.ERR_ValueParameterSameNameAsExtensionTypeParameter
                or ErrorCode.ERR_UnderspecifiedExtension
                or ErrorCode.ERR_ExpressionTreeContainsExtensionPropertyAccess
                or ErrorCode.ERR_PPIgnoredFollowsToken
                or ErrorCode.ERR_PPIgnoredNeedsFileBasedProgram
                or ErrorCode.ERR_PPIgnoredFollowsIf
                or ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne
                or ErrorCode.ERR_InExtensionParameterMustBeValueType
                or ErrorCode.ERR_ProtectedInExtension
                or ErrorCode.ERR_InstanceMemberWithUnnamedExtensionsParameter
                or ErrorCode.ERR_InitInExtension
                or ErrorCode.ERR_ModifierOnUnnamedReceiverParameter
                or ErrorCode.ERR_ExtensionTypeNameDisallowed
                or ErrorCode.ERR_ExpressionTreeContainsNamedArgumentOutOfPosition
                or ErrorCode.ERR_OperatorsMustBePublic
                or ErrorCode.ERR_OperatorMustReturnVoid
                or ErrorCode.ERR_CloseUnimplementedInterfaceMemberOperatorMismatch
                or ErrorCode.ERR_OperatorMismatchOnOverride
                or ErrorCode.ERR_BadCompoundAssignmentOpArgs
                or ErrorCode.ERR_PPShebangInProjectBasedProgram
                or ErrorCode.ERR_NameofExtensionMember
                or ErrorCode.ERR_BadExtensionUnaryOperatorSignature
                or ErrorCode.ERR_BadExtensionIncDecSignature
                or ErrorCode.ERR_BadExtensionBinaryOperatorSignature
                or ErrorCode.ERR_BadExtensionShiftOperatorSignature
                or ErrorCode.ERR_OperatorInExtensionOfStaticClass
                or ErrorCode.ERR_InstanceOperatorStructExtensionWrongReceiverRefKind
                or ErrorCode.ERR_InstanceOperatorExtensionWrongReceiverType
                or ErrorCode.ERR_ExpressionTreeContainsExtensionBasedConditionalLogicalOperator
                or ErrorCode.ERR_InterpolatedStringHandlerArgumentDisallowed
                or ErrorCode.ERR_MemberNameSameAsExtendedType
                or ErrorCode.ERR_FeatureNotAvailableInVersion14
                or ErrorCode.ERR_ExtensionBlockCollision
                or ErrorCode.ERR_MethodImplAttributeAsyncCannotBeUsed
                or ErrorCode.ERR_AttributeCannotBeAppliedManually
                or ErrorCode.ERR_BadSpreadInCatchFilter
                or ErrorCode.ERR_ExplicitInterfaceMemberTypeMismatch
                or ErrorCode.ERR_ExplicitInterfaceMemberReturnTypeMismatch
                or ErrorCode.HDN_RedundantPattern
                or ErrorCode.WRN_RedundantPattern
                or ErrorCode.HDN_RedundantPatternStackGuard
                or ErrorCode.ERR_BadVisBaseType
                or ErrorCode.ERR_AmbigExtension
                    => false,
            };
#pragma warning restore CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.
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

            if (IsWarning(code) || IsInfo(code) || IsHidden(code))
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
