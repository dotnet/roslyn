﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static readonly Lazy<ImmutableDictionary<ErrorCode, string>> s_helpLinksMap = new Lazy<ImmutableDictionary<ErrorCode, string>>(CreateHelpLinks);
        private static readonly Lazy<ImmutableDictionary<ErrorCode, string>> s_categoriesMap = new Lazy<ImmutableDictionary<ErrorCode, string>>(CreateCategoriesMap);
        public static readonly ImmutableHashSet<string> NullableWarnings;

        static ErrorFacts()
        {
            ImmutableHashSet<string>.Builder nullableWarnings = ImmutableHashSet.CreateBuilder<string>();

            nullableWarnings.Add(getId(ErrorCode.WRN_NullReferenceAssignment));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullReferenceReceiver));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullReferenceReturn));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullReferenceArgument));
            nullableWarnings.Add(getId(ErrorCode.WRN_UninitializedNonNullableField));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInAssignment));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInArgument));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInArgumentForOutput));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOfTargetDelegate));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOfTargetDelegate));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullAsNonNullable));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullableValueTypeMayBeNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeParameterConstraint));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeParameterReferenceTypeConstraint));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint));
            nullableWarnings.Add(getId(ErrorCode.WRN_ThrowPossibleNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_UnboxPossibleNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull));

            nullableWarnings.Add(getId(ErrorCode.WRN_ConvertingNullableToNonNullable));
            nullableWarnings.Add(getId(ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment));
            nullableWarnings.Add(getId(ErrorCode.WRN_ParameterConditionallyDisallowsNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_ShouldNotReturn));

            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeOnOverride));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverride));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverride));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeOnImplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInTypeOnExplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInConstraintsOnImplicitImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInExplicitlyImplementedInterface));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInInterfaceImplementedByBase));
            nullableWarnings.Add(getId(ErrorCode.WRN_DuplicateInterfaceWithNullabilityMismatchInBaseList));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullReferenceInitializer));
            nullableWarnings.Add(getId(ErrorCode.WRN_ShouldNotReturn));
            nullableWarnings.Add(getId(ErrorCode.WRN_DoesNotReturnMismatch));
            nullableWarnings.Add(getId(ErrorCode.WRN_ParameterConditionallyDisallowsNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementationBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementationBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverrideBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementationBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementationBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverrideBecauseOfAttributes));
            nullableWarnings.Add(getId(ErrorCode.WRN_MemberNotNull));
            nullableWarnings.Add(getId(ErrorCode.WRN_MemberNotNullBadMember));
            nullableWarnings.Add(getId(ErrorCode.WRN_MemberNotNullWhen));

            NullableWarnings = nullableWarnings.ToImmutable();

            static string getId(ErrorCode errorCode)
            {
                return MessageProvider.Instance.GetIdForErrorCode((int)errorCode);
            }
        }

        private static ImmutableDictionary<ErrorCode, string> CreateHelpLinks()
        {
            var map = new Dictionary<ErrorCode, string>()
            {
                // { ERROR_CODE,    HELP_LINK }
            };

            return map.ToImmutableDictionary();
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
            string helpLink;
            if (s_helpLinksMap.Value.TryGetValue(code, out helpLink))
            {
                return helpLink;
            }

            return string.Empty;
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
                // Info and hidden diagnostics have least warning level.
                return Diagnostic.HighestValidWarningLevel;
            }

            switch (code)
            {
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
                    return 3;
                case ErrorCode.WRN_NewRequired:
                case ErrorCode.WRN_NewOrOverrideExpected:
                case ErrorCode.WRN_UnreachableCode:
                case ErrorCode.WRN_UnreferencedLabel:
                case ErrorCode.WRN_NegativeArrayIndex:
                case ErrorCode.WRN_BadRefCompareLeft:
                case ErrorCode.WRN_BadRefCompareRight:
                case ErrorCode.WRN_PatternIsAmbiguous:
                case ErrorCode.WRN_PatternStaticOrInaccessible:
                case ErrorCode.WRN_PatternBadSignature:
                case ErrorCode.WRN_SameFullNameThisNsAgg:
                case ErrorCode.WRN_SameFullNameThisAggAgg:
                case ErrorCode.WRN_SameFullNameThisAggNs:
                case ErrorCode.WRN_GlobalAliasDefn:
                case ErrorCode.WRN_AlwaysNull:
                case ErrorCode.WRN_CmpAlwaysFalse:
                case ErrorCode.WRN_GotoCaseShouldConvert:
                case ErrorCode.WRN_NubExprIsConstBool:
                case ErrorCode.WRN_NubExprIsConstBool2:
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
                case ErrorCode.WRN_Experimental:
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
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnOverrideBecauseOfAttributes:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnOverrideBecauseOfAttributes:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnImplicitImplementationBecauseOfAttributes:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnImplicitImplementationBecauseOfAttributes:
                case ErrorCode.WRN_NullabilityMismatchInReturnTypeOnExplicitImplementationBecauseOfAttributes:
                case ErrorCode.WRN_NullabilityMismatchInParameterTypeOnExplicitImplementationBecauseOfAttributes:
                case ErrorCode.WRN_MemberNotNull:
                case ErrorCode.WRN_MemberNotNullBadMember:
                case ErrorCode.WRN_MemberNotNullWhen:
                    return 1;
                default:
                    return 0;
            }
            // Note: when adding a warning here, consider whether it should be registered as a nullability warning too
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
    }
}
