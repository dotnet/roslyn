// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class DiagnosticTest : CSharpTestBase
    {
        /// <summary>
        /// Ensure string resources are included.
        /// </summary>
        [Fact]
        public void Resources()
        {
            var excludedErrorCodes = new[]
            {
                ErrorCode.Void,
                ErrorCode.Unknown,
                ErrorCode.WRN_ALinkWarn, // Not reported, but retained to allow configuring class of related warnings. See CSharpDiagnosticFilter.Filter.
            };
            foreach (ErrorCode code in Enum.GetValues(typeof(ErrorCode)))
            {
                if (Array.IndexOf(excludedErrorCodes, code) >= 0)
                {
                    continue;
                }

                Assert.False(string.IsNullOrEmpty(ErrorFacts.GetMessage(code, CultureInfo.InvariantCulture)), $"Message for error {code} is null or empty.");
            }
        }

        /// <summary>
        /// ErrorCode should not have duplicates.
        /// </summary>
        [Fact]
        public void NoDuplicates()
        {
            var values = Enum.GetValues(typeof(ErrorCode));
            var set = new HashSet<ErrorCode>();
            foreach (ErrorCode value in values)
            {
                Assert.True(set.Add(value), $"{value} is duplicated!");
            }
        }

        [Fact]
        public void TestDiagnostic()
        {
            MockMessageProvider provider = new MockMessageProvider();
            SyntaxTree syntaxTree = new MockCSharpSyntaxTree();
            CultureInfo englishCulture = CultureHelpers.EnglishCulture;

            DiagnosticInfo di1 = new DiagnosticInfo(provider, 1);
            Assert.Equal(1, di1.Code);
            Assert.Equal(DiagnosticSeverity.Error, di1.Severity);
            Assert.Equal("MOCK0001", di1.MessageIdentifier);
            Assert.Equal("The first error", di1.GetMessage(englishCulture));

            DiagnosticInfo di2 = new DiagnosticInfo(provider, 1002, "Elvis", "Mort");
            Assert.Equal(1002, di2.Code);
            Assert.Equal(DiagnosticSeverity.Warning, di2.Severity);
            Assert.Equal("MOCK1002", di2.MessageIdentifier);
            Assert.Equal("The second warning about Elvis and Mort", di2.GetMessage(englishCulture));

            Location l1 = new SourceLocation(syntaxTree, new TextSpan(5, 8));
            var d1 = new CSDiagnostic(di2, l1);
            Assert.Equal(l1, d1.Location);
            Assert.Same(syntaxTree, d1.Location.SourceTree);
            Assert.Equal(new TextSpan(5, 8), d1.Location.SourceSpan);
            Assert.Equal(0, d1.AdditionalLocations.Count());
            Assert.Same(di2, d1.Info);
        }

        [Fact]
        public void TestCustomErrorInfo()
        {
            MockMessageProvider provider = new MockMessageProvider();
            SyntaxTree syntaxTree = new MockCSharpSyntaxTree();

            DiagnosticInfo di3 = new CustomErrorInfo(provider, "OtherSymbol", new SourceLocation(syntaxTree, new TextSpan(14, 8)));
            var d3 = new CSDiagnostic(di3, new SourceLocation(syntaxTree, new TextSpan(1, 1)));
            Assert.Same(syntaxTree, d3.Location.SourceTree);
            Assert.Equal(new TextSpan(1, 1), d3.Location.SourceSpan);
            Assert.Equal(1, d3.AdditionalLocations.Count());
            Assert.Equal(new TextSpan(14, 8), d3.AdditionalLocations.First().SourceSpan);
            Assert.Equal("OtherSymbol", (d3.Info as CustomErrorInfo).OtherSymbol);
        }

        [Fact, WorkItem(66037, "https://github.com/dotnet/roslyn/issues/66037")]
        public void DiagnosticInfo_WithSeverity()
        {
            var comp = CreateCompilation("");
            var args = new object[] { comp.GlobalNamespace };
            var symbol = (Symbol)comp.GlobalNamespace;
            var type = TypeWithAnnotations.Create(comp.GetSpecialType(SpecialType.System_Object));

            verifyWithSeverity(new CSDiagnosticInfo(ErrorCode.ERR_AbstractField));
            verifyWithSeverity(new DiagnosticInfoWithSymbols(ErrorCode.ERR_DuplicateTypeParameter, args,
                ImmutableArray.Create(symbol)));
            verifyWithSeverity(new LazyArrayElementCantBeRefAnyDiagnosticInfo(type));
            verifyWithSeverity(new LazyObsoleteDiagnosticInfo(symbol, symbol, BinderFlags.None));
            verifyWithSeverity(new LazyUseSiteDiagnosticsInfoForNullableType(LanguageVersion.CSharp11, type));
            verifyWithSeverity(new SyntaxDiagnosticInfo(1, 2, ErrorCode.ERR_DuplicateTypeParameter, args));
            verifyWithSeverity(new XmlSyntaxDiagnosticInfo(XmlParseErrorCode.XML_EndTagExpected, args));

            static void verifyWithSeverity(DiagnosticInfo diagnostic)
            {
                var other = diagnostic.GetInstanceWithSeverity(DiagnosticSeverity.Info);
                Assert.NotSame(diagnostic, other);
                Assert.Equal(DiagnosticSeverity.Info, other.Severity);

                Assert.Same(diagnostic, diagnostic.GetInstanceWithSeverity(diagnostic.Severity));
            }
        }

        [WorkItem(537801, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537801")]
        [Fact]
        public void MissingNamespaceOpenBracket()
        {
            var text = @"namespace NS

    interface ITest {
        void Method();
    }

End namespace
";

            var comp = CreateCompilation(text);
            var actualErrors = comp.GetDiagnostics();
            Assert.InRange(actualErrors.Count(), 1, int.MaxValue);
        }

        [WorkItem(540086, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540086")]
        [Fact]
        public void ErrorApplyIndexingToMethod()
        {
            var text = @"using System;
public class A
{
    static void Main(string[] args)
    {
        Console.WriteLine(goo[0]);
    }

    static int[] goo()
    {
        return new int[0];
    }
}";

            var comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadIndexLHS, Line = 6, Column = 27 });

            text = @"
public class A
{
    static void Main(string[] args)
    {        
    }

    void goo(object o)
    {
        System.Console.WriteLine(o.GetType().GetMethods[0].Name);
    }
}";
            comp = DiagnosticsUtils.VerifyErrorsAndGetCompilationWithMscorlib(text,
                new ErrorDescription { Code = (int)ErrorCode.ERR_BadIndexLHS, Line = 10, Column = 34 });
        }

        [WorkItem(540329, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540329")]
        [Fact]
        public void ErrorMemberAccessOnLiteralToken()
        {
            var text = @"
class X
{
    static void Main()
    {
        // this statement should produce an error
        int x = null.Length;
        // this statement is valid
        string three = 3.ToString();
    }
}";

            CreateCompilation(text).VerifyDiagnostics(
                // (6,17): error CS0023: Operator '.' cannot be applied to operand of type '<null>'
                Diagnostic(ErrorCode.ERR_BadUnaryOp, @"null.Length").WithArguments(".", "<null>"));
        }

        [WorkItem(542911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542911")]
        [Fact]
        public void WarningLevel_1()
        {
            foreach (ErrorCode errorCode in Enum.GetValues(typeof(ErrorCode)))
            {
                string errorCodeName = errorCode.ToString();
                if (errorCodeName.StartsWith("WRN", StringComparison.Ordinal))
                {
                    Assert.True(ErrorFacts.IsWarning(errorCode));
                    Assert.NotEqual(0, ErrorFacts.GetWarningLevel(errorCode));
                }
                else if (errorCodeName.StartsWith("ERR", StringComparison.Ordinal))
                {
                    Assert.False(ErrorFacts.IsWarning(errorCode));
                    Assert.Equal(0, ErrorFacts.GetWarningLevel(errorCode));
                }
            }
        }

        [WorkItem(542911, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542911")]
        [Fact]
        public void WarningLevel_2()
        {
            // Check a few warning levels recently added

            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DeprecatedCollectionInitAddStr));
            Assert.Equal(1, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DefaultValueForUnconsumedLocation));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_UnmatchedParamRefTag));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_UnmatchedTypeParamRefTag));
            Assert.Equal(1, ErrorFacts.GetWarningLevel(ErrorCode.WRN_ReferencedAssemblyReferencesLinkedPIA));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_DynamicDispatchToConditionalMethod));
            Assert.Equal(3, ErrorFacts.GetWarningLevel(ErrorCode.WRN_IsDynamicIsConfusing));
            Assert.Equal(2, ErrorFacts.GetWarningLevel(ErrorCode.WRN_NoSources));

            // If a new warning is added, this test will fail and adding the new case with the expected error level will be required.

            foreach (ErrorCode errorCode in Enum.GetValues(typeof(ErrorCode)))
            {
                if ((int)errorCode < 7000)
                {
                    continue;
                }

                string errorCodeName = errorCode.ToString();
                if (errorCodeName.StartsWith("WRN", StringComparison.Ordinal))
                {
                    Assert.True(ErrorFacts.IsWarning(errorCode));
                    switch (errorCode)
                    {
                        case ErrorCode.WRN_DelaySignButNoKey:
                        case ErrorCode.WRN_AttributeIgnoredWhenPublicSigning:
                        case ErrorCode.WRN_UnimplementedCommandLineSwitch:
                        case ErrorCode.WRN_CallerFilePathPreferredOverCallerMemberName:
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerMemberName:
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerFilePath:
                        case ErrorCode.WRN_AssemblyAttributeFromModuleIsOverridden:
                        case ErrorCode.WRN_RefCultureMismatch:
                        case ErrorCode.WRN_ConflictingMachineAssembly:
                        case ErrorCode.WRN_FilterIsConstantFalse:
                        case ErrorCode.WRN_FilterIsConstantTrue:
                        case ErrorCode.WRN_FilterIsConstantFalseRedundantTryCatch:
                        case ErrorCode.WRN_AnalyzerCannotBeCreated:
                        case ErrorCode.WRN_NoAnalyzerInAssembly:
                        case ErrorCode.WRN_UnableToLoadAnalyzer:
                        case ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName:
                        case ErrorCode.WRN_AlignmentMagnitude:
                        case ErrorCode.WRN_TupleLiteralNameMismatch:
                        case ErrorCode.WRN_WindowsExperimental:
                        case ErrorCode.WRN_AttributesOnBackingFieldsNotAvailable:
                        case ErrorCode.WRN_TupleBinopLiteralNameMismatch:
                        case ErrorCode.WRN_TypeParameterSameAsOuterMethodTypeParameter:
                        case ErrorCode.WRN_SwitchExpressionNotExhaustive:
                        case ErrorCode.WRN_IsTypeNamedUnderscore:
                        case ErrorCode.WRN_GivenExpressionNeverMatchesPattern:
                        case ErrorCode.WRN_GivenExpressionAlwaysMatchesConstant:
                        case ErrorCode.WRN_UnconsumedEnumeratorCancellationAttributeUsage:
                        case ErrorCode.WRN_UndecoratedCancellationTokenParameter:
                        case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen:
                        case ErrorCode.WRN_SwitchExpressionNotExhaustiveWithUnnamedEnumValue:
                        case ErrorCode.WRN_RecordNamedDisallowed:
                        case ErrorCode.WRN_ParameterNotNullIfNotNull:
                        case ErrorCode.WRN_ReturnNotNullIfNotNull:
                        case ErrorCode.WRN_UnreadRecordParameter:
                        case ErrorCode.WRN_DoNotCompareFunctionPointers:
                        case ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter:
                        case ErrorCode.WRN_CallerArgumentExpressionParamForUnconsumedLocation:
                        case ErrorCode.WRN_CallerLineNumberPreferredOverCallerArgumentExpression:
                        case ErrorCode.WRN_CallerFilePathPreferredOverCallerArgumentExpression:
                        case ErrorCode.WRN_CallerMemberNamePreferredOverCallerArgumentExpression:
                        case ErrorCode.WRN_CallerArgumentExpressionAttributeHasInvalidParameterName:
                        case ErrorCode.WRN_CallerArgumentExpressionAttributeSelfReferential:
                        case ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired:
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
                        case ErrorCode.WRN_Experimental:
                        case ErrorCode.WRN_ExperimentalWithMessage:
                        case ErrorCode.WRN_ConvertingLock:
                        case ErrorCode.WRN_PartialMemberSignatureDifference:
                        case ErrorCode.WRN_UnscopedRefAttributeOldRules:
                        case ErrorCode.WRN_ConvertingNullableToNonNullable:
                        case ErrorCode.WRN_NullReferenceAssignment:
                        case ErrorCode.WRN_NullReferenceReceiver:
                        case ErrorCode.WRN_NullReferenceReturn:
                        case ErrorCode.WRN_NullReferenceArgument:
                        case ErrorCode.WRN_DisallowNullAttributeForbidsMaybeNullAssignment:
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
                        case ErrorCode.WRN_CaseConstantNamedUnderscore:
                        case ErrorCode.ERR_FeatureInPreview:
                        case ErrorCode.WRN_ThrowPossibleNull:
                        case ErrorCode.WRN_UnboxPossibleNull:
                        case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNull:
                        case ErrorCode.WRN_SwitchExpressionNotExhaustiveForNullWithWhen:
                        case ErrorCode.WRN_ImplicitCopyInReadOnlyMember:
                        case ErrorCode.WRN_NullabilityMismatchInTypeParameterNotNullConstraint:
                        case ErrorCode.WRN_NullReferenceInitializer:
                        case ErrorCode.WRN_ParameterConditionallyDisallowsNull:
                        case ErrorCode.WRN_ShouldNotReturn:
                        case ErrorCode.WRN_DoesNotReturnMismatch:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnImplicitImplementation:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnImplicitImplementation:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnExplicitImplementation:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnExplicitImplementation:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInReturnTypeOnOverride:
                        case ErrorCode.WRN_TopLevelNullabilityMismatchInParameterTypeOnOverride:
                        case ErrorCode.WRN_ConstOutOfRangeChecked:
                        case ErrorCode.WRN_MemberNotNull:
                        case ErrorCode.WRN_MemberNotNullWhen:
                        case ErrorCode.WRN_MemberNotNullBadMember:
                        case ErrorCode.WRN_GeneratorFailedDuringInitialization:
                        case ErrorCode.WRN_GeneratorFailedDuringGeneration:
                        case ErrorCode.WRN_ParameterDisallowsNull:
                        case ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern:
                        case ErrorCode.WRN_IsPatternAlways:
                        case ErrorCode.WRN_AnalyzerReferencesFramework:
                        case ErrorCode.WRN_InterpolatedStringHandlerArgumentAttributeIgnoredOnLambdaParameters:
                        case ErrorCode.WRN_CompileTimeCheckedOverflow:
                        case ErrorCode.WRN_MethGrpToNonDel:
                        case ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion:
                        case ErrorCode.WRN_UnassignedThisSupportedVersion:
                        case ErrorCode.WRN_UseDefViolationPropertySupportedVersion:
                        case ErrorCode.WRN_UseDefViolationFieldSupportedVersion:
                        case ErrorCode.WRN_UseDefViolationThisSupportedVersion:
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
                        case ErrorCode.WRN_UseDefViolationRefField:
                        case ErrorCode.WRN_CollectionExpressionRefStructMayAllocate:
                        case ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate:
                        case ErrorCode.INF_TooManyBoundLambdas:
                        case ErrorCode.WRN_FieldIsAmbiguous:
                        case ErrorCode.WRN_UninitializedNonNullableBackingField:
                        case ErrorCode.WRN_AccessorDoesNotUseBackingField:
                        case ErrorCode.WRN_RedundantPattern:
                            Assert.Equal(1, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_MainIgnored:
                        case ErrorCode.WRN_UnqualifiedNestedTypeInCref:
                        case ErrorCode.WRN_NoRuntimeMetadataVersion:
                            Assert.Equal(2, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_PdbLocalNameTooLong:
                        case ErrorCode.WRN_UnreferencedLocalFunction:
                        case ErrorCode.WRN_RecordEqualsWithoutGetHashCode:
                            Assert.Equal(3, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_InvalidVersionFormat:
                            Assert.Equal(4, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_NubExprIsConstBool2:
                        case ErrorCode.WRN_StaticInAsOrIs:
                        case ErrorCode.WRN_PrecedenceInversion:
                        case ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion:
                        case ErrorCode.WRN_UnassignedThisUnsupportedVersion:
                        case ErrorCode.WRN_ParamUnassigned:
                        case ErrorCode.WRN_UseDefViolationProperty:
                        case ErrorCode.WRN_UseDefViolationField:
                        case ErrorCode.WRN_UseDefViolationPropertyUnsupportedVersion:
                        case ErrorCode.WRN_UseDefViolationFieldUnsupportedVersion:
                        case ErrorCode.WRN_UseDefViolationThisUnsupportedVersion:
                        case ErrorCode.WRN_UseDefViolationOut:
                        case ErrorCode.WRN_UseDefViolation:
                        case ErrorCode.WRN_SyncAndAsyncEntryPoints:
                        case ErrorCode.WRN_ParameterIsStaticClass:
                        case ErrorCode.WRN_ReturnTypeIsStaticClass:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 5 and C# 9.
                            Assert.Equal(5, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_PartialMethodTypeDifference:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 6 and C# 10.
                            Assert.Equal(6, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_LowerCaseTypeName:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 7 and C# 11.
                            Assert.Equal(7, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_AddressOfInAsync:
                        case ErrorCode.WRN_ByValArraySizeConstRequired:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 8 and C# 12.
                            Assert.Equal(8, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_InterceptsLocationAttributeUnsupportedSignature:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 9 and C# 13.
                            Assert.Equal(9, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        case ErrorCode.WRN_UnassignedInternalRefField:
                            // These are the warnings introduced with the warning "wave" shipped with dotnet 10 and C# 14.
                            Assert.Equal(10, ErrorFacts.GetWarningLevel(errorCode));
                            break;
                        default:
                            // If a new warning is added, this test will fail
                            // and whoever is adding the new warning will have to update it with the expected error level.
                            Assert.True(false, $"Please update this test case with a proper warning level ({ErrorFacts.GetWarningLevel(errorCode)}) for '{errorCodeName}'");
                            break;
                    }
                }
            }
        }

        [Fact]
        public void NullableWarnings()
        {
            foreach (ErrorCode error in Enum.GetValues(typeof(ErrorCode)))
            {
                if ((int)error < 8600 || (int)error >= 8912)
                {
                    continue;
                }

                if (!error.ToString().StartsWith("WRN"))
                {
                    // Only interested in warnings
                    continue;
                }

                if (ErrorFacts.NullableWarnings.Contains(MessageProvider.Instance.GetIdForErrorCode((int)error)))
                {
                    continue;
                }

                // Nullable-unrelated warnings in the C# 8 range should be added to this array.
                var nullableUnrelatedWarnings = new[]
                {
                    ErrorCode.WRN_MissingNonNullTypesContextForAnnotation,
                    ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode,
                    ErrorCode.WRN_ImplicitCopyInReadOnlyMember,
                    ErrorCode.WRN_GeneratorFailedDuringInitialization,
                    ErrorCode.WRN_GeneratorFailedDuringGeneration,
                    ErrorCode.WRN_GivenExpressionAlwaysMatchesPattern,
                    ErrorCode.WRN_IsPatternAlways,
                    ErrorCode.WRN_ConstOutOfRangeChecked,
                    ErrorCode.WRN_SwitchExpressionNotExhaustiveWithWhen,
                    ErrorCode.WRN_PrecedenceInversion,
                    ErrorCode.WRN_UnassignedThisAutoPropertyUnsupportedVersion,
                    ErrorCode.WRN_UnassignedThisUnsupportedVersion,
                    ErrorCode.WRN_ParamUnassigned,
                    ErrorCode.WRN_UseDefViolationProperty,
                    ErrorCode.WRN_UseDefViolationField,
                    ErrorCode.WRN_UseDefViolationThisUnsupportedVersion,
                    ErrorCode.WRN_UseDefViolationOut,
                    ErrorCode.WRN_UseDefViolation,
                    ErrorCode.WRN_SyncAndAsyncEntryPoints,
                    ErrorCode.WRN_ParameterIsStaticClass,
                    ErrorCode.WRN_ReturnTypeIsStaticClass,
                    ErrorCode.WRN_RecordNamedDisallowed,
                    ErrorCode.WRN_RecordEqualsWithoutGetHashCode,
                    ErrorCode.WRN_AnalyzerReferencesFramework,
                    ErrorCode.WRN_UnreadRecordParameter,
                    ErrorCode.WRN_DoNotCompareFunctionPointers,
                    ErrorCode.WRN_PartialMethodTypeDifference,
                    ErrorCode.WRN_ParameterOccursAfterInterpolatedStringHandlerParameter
                };

                Assert.Contains(error, nullableUnrelatedWarnings);
            }
        }

        [Fact]
        public void Warning_1()
        {
            var text = @"


public class C
{
    static private volatile int i;
    static public void Test (ref int i) {}
    public static void Main()
    {
        Test (ref i);
    }	
}
";

            CreateCompilation(text, options: TestOptions.ReleaseExe).VerifyDiagnostics(
                // (10,19): warning CS0420: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(420), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = TestOptions.ReleaseExe.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (10,19): error CS0420: Warning as Error: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(420)] = ReportDiagnostic.Error;
            option = TestOptions.ReleaseExe.WithGeneralDiagnosticOption(ReportDiagnostic.Default).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (10,19): error CS0420: Warning as Error: 'C.i': a reference to a volatile field will not be treated as volatile
                //         Test (ref i);
                Diagnostic(ErrorCode.WRN_VolatileByRef, "i").WithArguments("C.i").WithWarningAsError(true));
        }

        [Fact]
        public void Warning_2()
        {
            var text = @"


public class C
{
    public static void Main()
    {
	int x;
	int j = 0;
    }	
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Suppress);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Error;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,6): error CS0168: Warning as Error: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,6): warning CS0168: The variable 'x' is declared but never used
                // 	int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,6): warning CS0219: The variable 'j' is assigned but its value is never used
                // 	int j = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "j").WithArguments("j"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoErrorCodes1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoErrorCodes2()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning restore // comment
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable // comment
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericErrorCodes1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 168 // comment
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodes1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0168 // comment
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericErrorCodes2()
        {
            var text = @"


public class C
{
    public static void Main()
    {
#pragma warning restore 168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable 168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodes2()
        {
            var text = @"


public class C
{
    public static void Main()
    {
#pragma warning restore CS0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning disable CS0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (10,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesAreCaseSensitive()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable cs0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore cs0168
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesMustMatchExactly1()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS168, CS0219L
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0219L
        int z;      // CS0168
#pragma warning disable CS00168
        int w;      // CS0168
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13),
                // (12,13): warning CS0168: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true),
                // (12,13): error CS0168: Warning as Error: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13),
                // (12,13): warning CS0168: The variable 'w' is declared but never used
                //         int w;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "w").WithArguments("w").WithLocation(12, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_IdentifierErrorCodesMustMatchExactly2()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable ＣＳ０１６８
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore ＣＳ０１６８
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13).WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13).WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(7, 13),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(10, 13));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BlockScopeIsNotSignificant1()
        {
            var text = @"
public class C
{
    public static void Run()
    {
#pragma warning disable
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BlockScopeIsNotSignificant2()
        {
            var text = @"
#pragma warning disable
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (16,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes1()
        {
            var text = @"

#pragma warning disable 168, CS0219
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (17,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes2()
        {
            var text = @"
#pragma warning disable 168, CS0219 // comment
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore CS0219
        int z;
    }
}
";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NumericAndIdentifierErrorCodes3()
        {
            var text = @"
#pragma warning disable CS0465, 168, CS0219
public class C
{
    public static void Run()
    {
        int _x; // CS0168
    }

    public virtual void Finalize() // CS0465
    {
    }

    public static void Main()
    {
        int x;      // CS0168
        int y = 0;  // CS0219
        Run();
#pragma warning restore
        int z;
    }
}
";
            // Verify that warnings can be disabled using a mixed list of numeric literals and identifier
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (20,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(3);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (20,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithGeneralDiagnosticOption(ReportDiagnostic.Error);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2).WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BadSyntax1()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""),
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): warning CS0168: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""),
                // (8,17): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(1633), ReportDiagnostic.Suppress);
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,17): warning CS0219: The variable 'y' is assigned but its value is never used
                //             int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,17): warning CS0168: The variable 'z' is declared but never used
                //             int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,8): warning CS1633: Unrecognized #pragma directive
                // #pragma
                Diagnostic(ErrorCode.WRN_IllegalPragma, ""));
        }

        [Fact]
        public void PragmaWarning_BadSyntax2()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 1633
#pragma
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_BadSyntax3()
        {
            var text = @"

public class C
{
    public static void Main()
   {
#pragma warning
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable, restore, enable or safeonly
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable, restore, enable or safeonly
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,16): warning CS1634: Expected disable, restore, enable or safeonly
                // #pragma warning
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, ""));
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes1()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable 1
#pragma warning disable CS168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore all
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (10,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes2()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable CS0001, 168, all
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"),
                // (11,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes3()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore 1
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics();

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics();

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_OnlyRestoreWithoutDisableIsNoOp()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning restore
        int x;      // CS0168
        int y = 0;  // CS0219
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            IDictionary<string, ReportDiagnostic> warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarning_StringLiteralsAreNotAllowed()
        {
            var text = @"

public class C
{
    public static void Main()
    {
#pragma warning disable ""CS0168
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (8,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (8,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25),
                // (9,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y"));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (7,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(7, 25));
        }

        [Fact]
        public void PragmaWarning_MostKeywordsAreAllowedAsErrorCodes()
        {
            // Lexing / parsing of identifiers inside #pragma is identical to that inside #define for the below cases.
            // The #define cases below also produce no errors in previous versions of the compiler.
            var text = @"
#define class
#define static
#define int
#define public
#define null
#define warning
#define define
public class C
{
    public static void Main()
    {
#pragma warning disable class, static, int
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore warning
#pragma warning restore public, null, define
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (12,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(14, 13),
                // (13,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(15, 13));
        }

        /// <remarks>
        /// See <see cref="SyntaxFacts.IsPreprocessorContextualKeyword"/>.
        /// </remarks>
        [Fact]
        public void PragmaWarning_SomeKeywordsAreNotAllowedAsErrorCodes()
        {
            // A small number of keywords are not legal as error codes inside #pragma. This is because
            // the lexer processes these keywords specially inside preprocessor directives i.e. it returns
            // keyword tokens instead of identifier tokens for these.
            // Lexing / parsing of identifiers inside #pragma is identical to that inside #define for the below cases.
            // The #define cases below also produce identical errors in previous versions of the compiler.
            var text = @"
#define true
#define default
#define hidden
#define disable
#define checksum
#define restore
#define false
public class C
{
    public static void Main()
    {
#pragma warning disable true
#pragma warning disable default
#pragma warning disable hidden
#pragma warning disable disable
#pragma warning restore checksum
#pragma warning restore restore
#pragma warning restore false
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (2,9): error CS1001: Identifier expected
                // #define true
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "true").WithLocation(2, 9),
                // (3,9): error CS1001: Identifier expected
                // #define default
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "default").WithLocation(3, 9),
                // (4,9): error CS1001: Identifier expected
                // #define hidden
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "hidden").WithLocation(4, 9),
                // (5,9): error CS1001: Identifier expected
                // #define disable
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "disable").WithLocation(5, 9),
                // (6,9): error CS1001: Identifier expected
                // #define checksum
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "checksum").WithLocation(6, 9),
                // (7,9): error CS1001: Identifier expected
                // #define restore
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "restore").WithLocation(7, 9),
                // (8,9): error CS1001: Identifier expected
                // #define false
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "false").WithLocation(8, 9),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable true
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "true").WithLocation(13, 25),
                // (14,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable default
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "default").WithLocation(14, 25),
                // (15,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable hidden
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "hidden").WithLocation(15, 25),
                // (16,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable disable
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "disable").WithLocation(16, 25),
                // (17,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore checksum
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "checksum").WithLocation(17, 25),
                // (18,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore restore
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "restore").WithLocation(18, 25),
                // (19,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore false
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "false").WithLocation(19, 25));
        }

        [Fact]
        public void PragmaWarning_VeryLongIdentifiersAreAllowed()
        {
            var text = @"
#define __A_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890
public class C
{
    public static void Main()
    {
#pragma warning disable __B_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890, CS0168, CS0219
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore __B_123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789023456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678902345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890, CS0168, CS0219
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics();

            var nodes = ParseWithRoundTripCheck(text).GetRoot().DescendantNodes(descendIntoTrivia: true);
            var defineName = nodes.OfType<Syntax.DefineDirectiveTriviaSyntax>().Single().Name;
            var errorCodeName = nodes.OfType<Syntax.PragmaWarningDirectiveTriviaSyntax>().First()
                                     .ErrorCodes.OfType<Syntax.IdentifierNameSyntax>().First().Identifier;

            // Lexing / parsing of identifiers inside #pragma warning directives is identical
            // to that inside #define directives except that very long identifiers inside #define
            // are truncated to 128 characters to maintain backwards compatibility with previous
            // versions of the compiler.
            Assert.Equal(128, defineName.ValueText.Length);
            Assert.Equal(2335, defineName.Text.Length);

            // Since support for identifiers inside #pragma warning directives is new, 
            // we don't have any backwards compatibility constraints. So we can preserve the
            // identifier exactly as it appears in source.
            Assert.Equal(2335, errorCodeName.ValueText.Length);
            Assert.Equal(2335, errorCodeName.Text.Length);
        }

        [Fact]
        public void PragmaWarning_EscapedKeywordsAreNotAllowedAsErrorCodes()
        {
            var text = @"
#define @true
#define @class
public class C
{
    public static void Main()
    {
#pragma warning disable @true
#pragma warning restore @class
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (2,9): error CS1001: Identifier expected
                // #define @true
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "@").WithLocation(2, 9),
                // (3,9): error CS1001: Identifier expected
                // #define @class
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "@").WithLocation(3, 9),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable @true
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(8, 25),
                // (9,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore @class
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(9, 25));
        }

        [Fact]
        public void PragmaWarning_ExpressionsAreNotAllowedAsErrorCodes()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0168 + CS0219
        int x;      // CS0168
        int y = 0;  // CS0219
#pragma warning restore CS0168.Empty

#pragma warning disable (CS0168)
        int z;      // CS0168
#pragma warning restore -168
#pragma warning restore 168.1
#pragma warning restore 168L
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (6,32): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning disable CS0168 + CS0219
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, "+").WithLocation(6, 32),
                // (9,31): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore CS0168.Empty
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, ".").WithLocation(9, 31),
                // (11,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable (CS0168)
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "(").WithLocation(11, 25),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore -168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "-").WithLocation(13, 25),
                // (14,28): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore 168.1
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, ".").WithLocation(14, 28),
                // (15,28): warning CS1696: Single-line comment or end-of-line expected
                // #pragma warning restore 168L
                Diagnostic(ErrorCode.WRN_EndOfPPLineExpected, "L").WithLocation(15, 28),
                // (8,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         int y = 0;  // CS0219
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(8, 13),
                // (12,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithLocation(12, 13));
        }

        [Fact]
        public void PragmaWarning_WarningsForBadPragmaSyntaxCanBeSuppressed()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS1072, CS1634
#pragma warning disable ~class
#pragma warning restore ""CS0219
#pragma warning blah
#pragma warning restore

#pragma warning disable @class
#pragma warning restore ""CS0168
#pragma warning blah
    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (12,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable @class
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, "@").WithLocation(12, 25),
                // (13,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore "CS0168
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, @"""CS0168").WithLocation(13, 25),
                // (14,17): warning CS1634: Expected disable, restore, enable or safeonly
                // #pragma warning blah
                Diagnostic(ErrorCode.WRN_IllegalPPWarning, "blah").WithLocation(14, 17));
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/36550")]
        public void PragmaWarning_Enable()
        {
            var text1 = @"
class Test
{
    void Main()
    {
        if (true)
        {
            int x;
        }
        else
        {
            return;
        }
    }
}
";

            var expected1 = new DiagnosticDescription[]
            {
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (12,13): warning CS0162: Unreachable code detected
                //             return;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "return")
            };

            CreateCompilation(text1, parseOptions: TestOptions.Regular7_3).VerifyDiagnostics(expected1);
            CreateCompilation(text1).VerifyDiagnostics(expected1);

            var options = TestOptions.DebugDll.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress);

            CreateCompilation(text1, parseOptions: TestOptions.Regular7_3, options: options).VerifyDiagnostics();
            CreateCompilation(text1, options: options).VerifyDiagnostics();

            var text2 = @"
#pragma warning enable
" + text1;

            CreateCompilation(text2, parseOptions: TestOptions.Regular7_3, options: options).VerifyDiagnostics(expected1);
            CreateCompilation(text2, options: options).VerifyDiagnostics(expected1);

            var text3 = @"
#pragma warning enable CS0168, 162
" + text1;

            CreateCompilation(text3, parseOptions: TestOptions.Regular7_3, options: options).VerifyDiagnostics(expected1);
            CreateCompilation(text3, options: options).VerifyDiagnostics(expected1);

            var text4 = @"
#pragma warning enable CS0168
" + text1;

            var expected2 = new DiagnosticDescription[]
            {
                // (8,17): warning CS0168: The variable 'x' is declared but never used
                //             int x;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x")
            };

            CreateCompilation(text4, parseOptions: TestOptions.Regular7_3, options: options).VerifyDiagnostics(expected2);
            CreateCompilation(text4, options: options).VerifyDiagnostics(expected2);

            var text5 = @"
#pragma warning enable 168
" + text1;

            CreateCompilation(text5, parseOptions: TestOptions.Regular7_3, options: options).VerifyDiagnostics(expected2);
            CreateCompilation(text5, options: options).VerifyDiagnostics(expected2);
        }

        [Fact]
        public void PragmaWarning_ErrorsCantBeSuppressed()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable CS0029
        int x = string.Empty;
#pragma warning restore CS0029
#pragma warning disable 29
        int y = string.Empty;
#pragma warning restore 29

    }
}";
            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (7,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         int x = string.Empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string.Empty").WithArguments("string", "int").WithLocation(7, 17),
                // (10,17): error CS0029: Cannot implicitly convert type 'string' to 'int'
                //         int y = string.Empty;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "string.Empty").WithArguments("string", "int").WithLocation(10, 17));
        }

        [Fact]
        public void PragmaWarning_MissingErrorCodes()
        {
            var text = @"
public class C
{
    public static void Main()
    {
#pragma warning disable ,
        int x;      // CS0168
#pragma warning restore , ,
        int z;
    }
}";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (7,13): warning CS0168: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x"),
                // (9,13): warning CS0168: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z"));

            var warnings = new Dictionary<string, ReportDiagnostic>();
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(168), ReportDiagnostic.Error);
            CSharpCompilationOptions option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (7,13): error CS0168: Warning as Error: The variable 'x' is declared but never used
                //         int x;      // CS0168
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithWarningAsError(true),
                // (9,13): error CS0168: Warning as Error: The variable 'z' is declared but never used
                //         int z;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "z").WithArguments("z").WithWarningAsError(true));

            warnings[MessageProvider.Instance.GetIdForErrorCode(168)] = ReportDiagnostic.Suppress;
            option = commonoption.WithSpecificDiagnosticOptions(warnings);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","));

            option = commonoption.WithWarningLevel(2);
            CreateCompilation(text, options: option).VerifyDiagnostics(
                // (6,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning disable ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,25): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","),
                // (8,27): warning CS1072: Expected identifier or numeric literal.
                // #pragma warning restore , ,
                Diagnostic(ErrorCode.WRN_IdentifierOrNumericLiteralExpected, ","));
        }

        [WorkItem(546814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546814")]
        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes4()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
using System;

class Program
{
#pragma warning disable 1691
#pragma warning disable 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics();
        }

        [WorkItem(546814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546814")]
        [Fact]
        public void PragmaWarning_NoValidationForErrorCodes5()
        {
            // Previous versions of the compiler used to report a warning (CS1691)
            // whenever an unrecognized warning code was supplied in a #pragma directive.
            // We no longer generate a warning in such cases.
            var text = @"
using System;

class Program
{
#pragma warning disable 1691, 59526
        public static void Main() { Console.Read(); }

#pragma warning restore 1691, 56529
} ";

            CSharpCompilationOptions commonoption = TestOptions.ReleaseExe;
            CreateCompilation(text, options: commonoption).VerifyDiagnostics();
        }

        [Fact]
        public void PragmaWarningDirectiveMap()
        {
            var text = @"
using System;
public class C
{
#pragma warning disable 
    public static void Main()
#pragma warning restore 168
    {
        int x;
#pragma warning disable CS0168
        int y;      // CS0168
#pragma warning restore
        int z = 0;  // CS0219
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "goo.cs");
            Assert.Equal(PragmaWarningState.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "public class").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "public static").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "public static").Start));
            Assert.Equal(PragmaWarningState.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int x").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int x").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int y").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int y").Start));
            Assert.Equal(PragmaWarningState.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "int z").Start));
            Assert.Equal(PragmaWarningState.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "int z").Start));
        }

        [Fact]
        public void PragmaWarningDirectiveMapWithIfDirective()
        {
            var text = @"
using System;
class Program
{
    static void Main(string[] args)
    {
#pragma warning disable
        var x = 10;
#if false
#pragma warning restore
#endif
        var y = 10;
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "goo.cs");
            Assert.Equal(PragmaWarningState.Default, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "static void").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "var x").Start));
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(219), GetSpanIn(syntaxTree, "var y").Start));
        }

        [WorkItem(545407, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545407")]
        [Fact]
        public void PragmaWarningDirectiveMapAtTheFirstLine()
        {
            var text = @"#pragma warning disable
using System;
class Program
{
    static void Main(string[] args)
    {
    }
}";
            SyntaxTree syntaxTree = SyntaxFactory.ParseSyntaxTree(text, path: "goo.cs");
            Assert.Equal(PragmaWarningState.Disabled, syntaxTree.GetPragmaDirectiveWarningState(MessageProvider.Instance.GetIdForErrorCode(168), GetSpanIn(syntaxTree, "static void").Start));
        }

        private TextSpan GetSpanIn(SyntaxTree syntaxTree, string textToFind)
        {
            string s = syntaxTree.GetText().ToString();
            int index = s.IndexOf(textToFind, StringComparison.Ordinal);
            Assert.True(index >= 0, "textToFind not found in the tree");
            return new TextSpan(index, textToFind.Length);
        }

        [WorkItem(543705, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543705")]
        [WorkItem(39992, "https://github.com/dotnet/roslyn/issues/39992")]
        [Fact]
        public void GetDiagnosticsCalledTwice()
        {
            var text = @"
interface IMyEnumerator { }

public class Test
{
    static IMyEnumerator Goo()
    {
        yield break;
    }

    public static int Main()
    {
        return 1;
    }
}";
            var compilation = CreateCompilation(text);

            Assert.Equal(1, compilation.GetDiagnostics().Length);
            Assert.Equal(1, compilation.GetDiagnostics().Length);
        }

        [WorkItem(39992, "https://github.com/dotnet/roslyn/issues/39992")]
        [Fact]
        public void GetDiagnosticsCalledTwice_GetEmitDiagnostics()
        {
            var text = @"
interface IMyEnumerator { }

public class Test
{
    static IMyEnumerator Goo()
    {
        yield break;
    }

    public static int Main()
    {
        return 1;
    }
}";
            var compilation = CreateCompilation(text);
            var expected = new DiagnosticDescription[] {
                // (6,26): error CS1624: The body of 'Test.Goo()' cannot be an iterator block because 'IMyEnumerator' is not an iterator interface type
                //     static IMyEnumerator Goo()
                Diagnostic(ErrorCode.ERR_BadIteratorReturn, "Goo").WithArguments("Test.Goo()", "IMyEnumerator").WithLocation(6, 26)
            };
            compilation.VerifyDiagnostics(expected);
            compilation.VerifyEmitDiagnostics(expected);
        }

        [Fact]
        public void TestArgumentEquality()
        {
            var text = @"
using System;

public class Test
{
    public static void Main()
    {
        (Console).WriteLine();
    }
}";
            var tree = Parse(text);

            // (8,10): error CS0119: 'Console' is a type, which is not valid in the given context
            AssertEx.Equal(CreateCompilation(tree).GetDiagnostics(), CreateCompilation(tree).GetDiagnostics());
        }

        /// <summary>
        /// Test that invalid type argument lists produce clean error messages
        /// with minimal noise
        /// </summary>
        [WorkItem(7177, "https://github.com/dotnet/roslyn/issues/7177")]
        [Fact]
        public void InvalidTypeArgumentList()
        {
            var text = @"using System;
public class A
{
    static void Main(string[] args)
    {
        // Invalid type arguments
        object a1 = typeof(Action<0>);
        object a2 = typeof(Action<static>);

        // Valid type arguments
        object a3 = typeof(Action<string>);
        object a4 = typeof(Action<>);

        // Invalid with multiple types
        object a5 = typeof(Func<0,1>);
        object a6 = typeof(Func<0,bool>);
        object a7 = typeof(Func<static,bool>);

        // Valid with multiple types
        object a8 = typeof(Func<string,bool>);
        object a9 = typeof(Func<,>);

        // Invalid with nested types
        object a10 = typeof(Action<Action<0>>);
        object a11 = typeof(Action<Action<static>>);
        object a12 = typeof(Action<Action<>>);

        // Valid with nested types
        object a13 = typeof(Action<Action<string>>);
    }
}";

            CSharpCompilationOptions options = TestOptions.ReleaseExe;
            CreateCompilation(text, options: options).VerifyDiagnostics(
                // (7,35): error CS1031: Type expected
                //         object a1 = typeof(Action<0>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(7, 35),
                // (8,35): error CS1031: Type expected
                //         object a2 = typeof(Action<static>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(8, 35),
                // (15,33): error CS1031: Type expected
                //         object a5 = typeof(Func<0,1>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(15, 33),
                // (15,35): error CS1031: Type expected
                //         object a5 = typeof(Func<0,1>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "1").WithLocation(15, 35),
                // (16,33): error CS1031: Type expected
                //         object a6 = typeof(Func<0,bool>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(16, 33),
                // (17,33): error CS1031: Type expected
                //         object a7 = typeof(Func<static,bool>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(17, 33),
                // (24,43): error CS1031: Type expected
                //         object a10 = typeof(Action<Action<0>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "0").WithLocation(24, 43),
                // (25,43): error CS1031: Type expected
                //         object a11 = typeof(Action<Action<static>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, "static").WithLocation(25, 43),
                // (26,36): error CS7003: Unexpected use of an unbound generic name
                //         object a12 = typeof(Action<Action<>>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Action<>").WithLocation(26, 36));
        }

        /// <summary>
        ///    Tests if CS0075 - "To cast a negative value, you must enclose the value in parentheses" is correctly emitted.
        /// </summary>
        [Fact]
        public void PossibleBadNegCast()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        var y = (ConsoleColor) - 1;
        var z = (System.ConsoleColor) - 1;
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(new[]
            {
                // (6,17): error CS0075: To cast a negative value, you must enclose the value in parentheses.
                //         var y = (ConsoleColor) - 1;
                Diagnostic(ErrorCode.ERR_PossibleBadNegCast, "(ConsoleColor) - 1").WithLocation(6, 17),
                // (7,17): error CS0075: To cast a negative value, you must enclose the value in parentheses.
                //         var z = (System.ConsoleColor) - 1;
                Diagnostic(ErrorCode.ERR_PossibleBadNegCast, "(System.ConsoleColor) - 1").WithLocation(7, 17)
            });
        }

        /// <summary>
        ///    Tests if fixing CS0075 - "To cast a negative value, you must enclose the value in parentheses" works. (fixed version of <see cref="PossibleBadNegCast"/>).
        /// </summary>
        [Fact]
        public void PossibleBadNegCastFixed()
        {
            var source = @"using System;
class Program
{
    static void Main()
    {
        var y = (ConsoleColor) (- 1);
        var z = (System.ConsoleColor) (- 1);
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(new[]
            {
                // (6,13): warning CS0219: The variable 'y' is assigned but its value is never used
                //         var y = (ConsoleColor) (- 1);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "y").WithArguments("y").WithLocation(6, 13),
                // (7,13): warning CS0219: The variable 'z' is assigned but its value is never used
                //         var z = (System.ConsoleColor) (- 1);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(7, 13)
            });
        }

        /// <summary>
        ///    Tests if CS0075 - "To cast a negative value, you must enclose the value in parentheses" is only emitted if the left side is (would be) a cast expression.
        /// </summary>
        [Fact]
        public void PossibleBadNegCastNotEmitted()
        {
            var source = @"using System;

class Program
{
    static void Main()
    {
        var w = ((ConsoleColor)) - 1;
        var x = ConsoleColor - 1;
        var y = ((System.ConsoleColor)) - 1;
        var z = System.ConsoleColor - 1;
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(new[]
            {
                // (7,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var w = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(7, 19),
                // (7,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var w = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(7, 19),
                // (7,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var w = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(7, 19),
                // (8,17): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var x = ConsoleColor - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(8, 17),
                // (9,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((System.ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(9, 19),
                // (9,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((System.ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(9, 19),
                // (9,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((System.ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(9, 19),
                // (10,17): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var z = System.ConsoleColor - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "System.ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(10, 17)
            });
        }

        /// <summary>
        ///    Tests if CS0075 - "To cast a negative value, you must enclose the value in parentheses" is also emitted for dynamic casts.
        /// </summary>
        [Fact]
        public void PossibleBadNegCastDynamic()
        {
            var source = @"class Program
{
    static void Main()
    {
        var y = (dynamic) - 1;
        var z = (@dynamic) - 1;
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(new[]
            {
                // (5,18): error CS0103: The name 'dynamic' does not exist in the current context
                //         var y = (dynamic) - 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(5, 18),
                // (5,17): error CS0075: To cast a negative value, you must enclose the value in parentheses.
                //         var y = (dynamic) - 1;
                Diagnostic(ErrorCode.ERR_PossibleBadNegCast, "(dynamic) - 1").WithLocation(5, 17),
                // (6,18): error CS0103: The name 'dynamic' does not exist in the current context
                //         var z = (@dynamic) - 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "@dynamic").WithArguments("dynamic").WithLocation(6, 18),
                // (6,17): error CS0075: To cast a negative value, you must enclose the value in parentheses.
                //         var z = (@dynamic) - 1;
                Diagnostic(ErrorCode.ERR_PossibleBadNegCast, "(@dynamic) - 1").WithLocation(6, 17)
            });
        }

        /// <summary>
        ///    Tests if CS0075 - "To cast a negative value, you must enclose the value in parentheses" is also emitted for dynamic casts when a local variable called 'dynamic' is defined.
        /// </summary>
        [Fact]
        public void PossibleBadNegCastDynamicWithLocal()
        {
            var source = @"class Program
{
    static void Main()
    {
        var dynamic = 1;
        var y = (dynamic) - 1;
        var z = (@dynamic) - 1;
    }
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics();
        }

        /// <summary>
        ///    Tests if CS0075 - "To cast a negative value, you must enclose the value in parentheses" is also emitted for dynamic casts when a method called 'dynamic' is defined.
        /// </summary>
        [Fact]
        public void PossibleBadNegCastDynamicWithMethod()
        {
            var source = @"class Program
{
    static void Main()
    {
        var y = (dynamic) - 1;
        var z = (@dynamic) - 1;
    }

    static void dynamic() {}
}";

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(new[]
            {
                // (5,17): error CS0019: Operator '-' cannot be applied to operands of type 'method group' and 'int'
                //         var y = (dynamic) - 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(dynamic) - 1").WithArguments("-", "method group", "int").WithLocation(5, 17),
                // (6,17): error CS0019: Operator '-' cannot be applied to operands of type 'method group' and 'int'
                //         var z = (@dynamic) - 1;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "(@dynamic) - 1").WithArguments("-", "method group", "int").WithLocation(6, 17)
            });
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32057")]
        public void PossibleBadNegCastNestedParentheses()
        {
            var source = """
                using System;
                class Program
                {
                    static void Main()
                    {
                        var y = ((ConsoleColor)) - 1;
                        var z = ((dynamic)) - 1;
                    }
                }
                """;

            var compilation = CreateCompilation(source);
            compilation.VerifyDiagnostics(
                // (6,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(6, 19),
                // (6,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(6, 19),
                // (6,19): error CS0119: 'ConsoleColor' is a type, which is not valid in the given context
                //         var y = ((ConsoleColor)) - 1;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "ConsoleColor").WithArguments("System.ConsoleColor", "type").WithLocation(6, 19),
                // (7,19): error CS0103: The name 'dynamic' does not exist in the current context
                //         var z = ((dynamic)) - 1;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(7, 19));
        }

        #region Mocks
        internal class CustomErrorInfo : DiagnosticInfo
        {
            public readonly object OtherSymbol;
            public readonly Location OtherLocation;
            public override IReadOnlyList<Location> AdditionalLocations
            {
                get
                {
                    return new Location[1] { OtherLocation };
                }
            }

            public CustomErrorInfo(CommonMessageProvider provider, object otherSymbol, Location otherLocation)
                : base(provider, 2)
            {
                this.OtherSymbol = otherSymbol;
                this.OtherLocation = otherLocation;
            }
        }

        internal sealed class MockMessageProvider : TestMessageProvider
        {
            public override DiagnosticSeverity GetSeverity(int code)
            {
                if (code >= 1000)
                {
                    return DiagnosticSeverity.Warning;
                }
                else
                {
                    return DiagnosticSeverity.Error;
                }
            }

            public override string LoadMessage(int code, CultureInfo language)
            {
                switch (code)
                {
                    case 1:
                        return "The first error";
                    case 2:
                        return "The second error is associated with symbol {0}";
                    case 1001:
                        return "The first warning";
                    case 1002:
                        return "The second warning about {0} and {1}";
                    default:
                        return null;
                }
            }

            public override LocalizableString GetDescription(int code)
            {
                return string.Empty;
            }

            public override LocalizableString GetTitle(int code)
            {
                return string.Empty;
            }

            public override LocalizableString GetMessageFormat(int code)
            {
                return string.Empty;
            }

            public override string GetHelpLink(int code)
            {
                return string.Empty;
            }

            public override string GetCategory(int code)
            {
                return string.Empty;
            }

            public override string CodePrefix
            {
                get { return "MOCK"; }
            }

            public override int GetWarningLevel(int code)
            {
                if (code >= 1000)
                {
                    return code % 4 + 1;
                }
                else
                {
                    return 0;
                }
            }

            public override string GetErrorDisplayString(ISymbol symbol)
            {
                return MessageProvider.Instance.GetErrorDisplayString(symbol);
            }

            public override bool GetIsEnabledByDefault(int code)
            {
                return true;
            }

#if DEBUG
            internal override bool ShouldAssertExpectedMessageArgumentsLength(int errorCode) => false;
#endif
        }

        #endregion

        #region CoreCLR Signing Tests

        [ConditionalFact(typeof(UnixLikeOnly), typeof(ClrOnly)), WorkItem(9288, "https://github.com/dotnet/roslyn/issues/9288")]
        public void Bug9288_keycontainer()
        {
            const string source = "";

            var ca = CreateCompilation(source, options: TestOptions.ReleaseDll.WithStrongNameProvider(new DesktopStrongNameProvider()).WithCryptoKeyContainer("bogus"));

            ca.VerifyEmitDiagnostics(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                // error CS7028: Error signing output with public key from container 'bogus' -- Assembly signing not supported.
                Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure).WithArguments("bogus", "Assembly signing not supported.").WithLocation(1, 1)
            );
        }

        // There are three places where we catch a ClrStrongNameMissingException,
        // but the third cannot happen - only if a key is successfully retrieved
        // from a keycontainer, and then we fail to get IClrStrongName afterwards
        // for the actual signing. However, we error on the key read, and can never
        // get to the third case (but there's still error handling if that changes)

        #endregion

        #region PathMap Linux Tests
        // Like the above (CoreCLR Signing Tests), these aren't actually syntax tests, but this is in one of only two assemblies tested on linux

        [Theory]
        [InlineData("C:\\", "/", "C:\\", "/")]
        [InlineData("C:\\temp\\", "/temp/", "C:\\temp", "/temp")]
        [InlineData("C:\\temp\\", "/temp/", "C:\\temp\\", "/temp/")]
        [InlineData("/", "C:\\", "/", "C:\\")]
        [InlineData("/temp/", "C:\\temp\\", "/temp", "C:\\temp")]
        [InlineData("/temp/", "C:\\temp\\", "/temp/", "C:\\temp\\")]
        public void PathMapKeepsCrossPlatformRoot(string expectedFrom, string expectedTo, string sourceFrom, string sourceTo)
        {
            var pathmapArg = $"/pathmap:{sourceFrom}={sourceTo}";
            var parsedArgs = CSharpCommandLineParser.Default.Parse(new[] { pathmapArg, "a.cs" }, TempRoot.Root, RuntimeEnvironment.GetRuntimeDirectory(), null);
            parsedArgs.Errors.Verify();
            var expected = new KeyValuePair<string, string>(expectedFrom, expectedTo);
            Assert.Equal(expected, parsedArgs.PathMap[0]);
        }

        [Fact]
        public void PathMapInconsistentSlashes()
        {
            CSharpCommandLineArguments parse(params string[] args)
            {
                var parsedArgs = CSharpCommandLineParser.Default.Parse(args, TempRoot.Root, RuntimeEnvironment.GetRuntimeDirectory(), null);
                parsedArgs.Errors.Verify();
                return parsedArgs;
            }

            var sep = PathUtilities.DirectorySeparatorChar;
            Assert.Equal(new KeyValuePair<string, string>("C:\\temp/goo" + sep, "/temp\\goo" + sep), parse("/pathmap:C:\\temp/goo=/temp\\goo", "a.cs").PathMap[0]);
            Assert.Equal(new KeyValuePair<string, string>("noslash" + sep, "withoutslash" + sep), parse("/pathmap:noslash=withoutslash", "a.cs").PathMap[0]);
            var doublemap = parse("/pathmap:/temp=/goo,/temp/=/bar", "a.cs").PathMap;
            Assert.Equal(new KeyValuePair<string, string>("/temp/", "/goo/"), doublemap[0]);
            Assert.Equal(new KeyValuePair<string, string>("/temp/", "/bar/"), doublemap[1]);
        }
        #endregion

        [Fact]
        public void TestIsBuildOnlyDiagnostic()
        {
            foreach (ErrorCode errorCode in Enum.GetValues(typeof(ErrorCode)))
            {
                // ErrorFacts.IsBuildOnlyDiagnostic with throw if any new ErrorCode
                // is added but not explicitly handled within it.
                // Update ErrorFacts.IsBuildOnlyDiagnostic if the below call throws.
                var isBuildOnly = ErrorFacts.IsBuildOnlyDiagnostic(errorCode);

                switch (errorCode)
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
                    case ErrorCode.ERR_RefLocalAcrossAwait:
                    case ErrorCode.ERR_DataSectionStringLiteralHashCollision:
                    case ErrorCode.ERR_UnsupportedFeatureInRuntimeAsync:
                    case ErrorCode.ERR_NonTaskMainCantBeAsync:
                    case ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported:
                        Assert.True(isBuildOnly, $"Check failed for ErrorCode.{errorCode}");
                        break;

                    default:
                        Assert.False(isBuildOnly, $"Check failed for ErrorCode.{errorCode}");
                        break;
                }
            }
        }
    }
}
