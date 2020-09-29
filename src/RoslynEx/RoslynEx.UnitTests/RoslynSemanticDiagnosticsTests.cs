using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics;
using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;
using Roslyn.Test.Utilities;
using Xunit;

namespace RoslynEx.UnitTests.Diagnostics
{
    [Trait("Category", "OuterLoop")]
    public class RoslynExDiagnosticAnalyzerTests : DiagnosticAnalyzerTests
    {
        public RoslynExDiagnosticAnalyzerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExDiagnosticSuppressorTests : DiagnosticSuppressorTests
    {
        public RoslynExDiagnosticSuppressorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExGetDiagnosticsTests : GetDiagnosticsTests
    {
        public RoslynExGetDiagnosticsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExMethodGroupConversion : MethodGroupConversion
    {
        public RoslynExMethodGroupConversion() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExFlowDiagnosticTests : FlowDiagnosticTests
    {
        public RoslynExFlowDiagnosticTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExFlowTests : FlowTests
    {
        public RoslynExFlowTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExLocalFunctions : LocalFunctions
    {
        public RoslynExLocalFunctions() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExRegionAnalysisTests : RegionAnalysisTests
    {
        public RoslynExRegionAnalysisTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExStructTests : StructTests
    {
        public RoslynExStructTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExAccessCheckTests : AccessCheckTests
    {
        public RoslynExAccessCheckTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExAccessibilityTests : AccessibilityTests
    {
        public RoslynExAccessibilityTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExAmbiguousOverrideTests : AmbiguousOverrideTests
    {
        public RoslynExAmbiguousOverrideTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExAnonymousFunctionTests : AnonymousFunctionTests
    {
        public RoslynExAnonymousFunctionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExArglistTests : ArglistTests
    {
        public RoslynExArglistTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExAwaitExpressionTests : AwaitExpressionTests
    {
        public RoslynExAwaitExpressionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBetterCandidates : BetterCandidates
    {
        public RoslynExBetterCandidates() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBindingAsyncTasklikeMoreTests : BindingAsyncTasklikeMoreTests
    {
        public RoslynExBindingAsyncTasklikeMoreTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBindingAsyncTasklikeTests : BindingAsyncTasklikeTests
    {
        public RoslynExBindingAsyncTasklikeTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBindingAsyncTests : BindingAsyncTests
    {
        public RoslynExBindingAsyncTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBindingAwaitTests : BindingAwaitTests
    {
        public RoslynExBindingAwaitTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExBindingTests : BindingTests
    {
        public RoslynExBindingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExColorColorTests : ColorColorTests
    {
        public RoslynExColorColorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExConditionalOperatorTests : ConditionalOperatorTests
    {
        public RoslynExConditionalOperatorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExConstantTests : ConstantTests
    {
        public RoslynExConstantTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExDeconstructionTests : DeconstructionTests
    {
        public RoslynExDeconstructionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSyntaxBinderTests : SyntaxBinderTests
    {
        public RoslynExSyntaxBinderTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExExpressionBodiedMemberTests : ExpressionBodiedMemberTests
    {
        public RoslynExExpressionBodiedMemberTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExForEachTests : ForEachTests
    {
        public RoslynExForEachTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExForLoopErrorTests : ForLoopErrorTests
    {
        public RoslynExForLoopErrorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExFunctionPointerTests : FunctionPointerTests
    {
        public RoslynExFunctionPointerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExGenericConstraintsTests : GenericConstraintsTests
    {
        public RoslynExGenericConstraintsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExHideByNameTests : HideByNameTests
    {
        public RoslynExHideByNameTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExImplicitlyTypeArraysTests : ImplicitlyTypeArraysTests
    {
        public RoslynExImplicitlyTypeArraysTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExImplicitlyTypedLocalTests : ImplicitlyTypedLocalTests
    {
        public RoslynExImplicitlyTypedLocalTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExIndexAndRangeTests : IndexAndRangeTests
    {
        public RoslynExIndexAndRangeTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExInheritanceBindingTests : InheritanceBindingTests
    {
        public RoslynExInheritanceBindingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExInitOnlyMemberTests : InitOnlyMemberTests
    {
        public RoslynExInitOnlyMemberTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExInteractiveUsingTests : InteractiveUsingTests
    {
        public RoslynExInteractiveUsingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExInterpolationTests : InterpolationTests
    {
        public RoslynExInterpolationTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExIteratorTests : IteratorTests
    {
        public RoslynExIteratorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExLambdaDiscardParametersTests : LambdaDiscardParametersTests
    {
        public RoslynExLambdaDiscardParametersTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExLambdaTests : LambdaTests
    {
        public RoslynExLambdaTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExLocalFunctionTests : LocalFunctionTests
    {
        public RoslynExLocalFunctionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExLockTests : LockTests
    {
        public RoslynExLockTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExGetSemanticInfoTests : GetSemanticInfoTests
    {
        public RoslynExGetSemanticInfoTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExMethodBodyModelTests : MethodBodyModelTests
    {
        public RoslynExMethodBodyModelTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExMethodTypeInferenceTests : MethodTypeInferenceTests
    {
        public RoslynExMethodTypeInferenceTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExMultiDimensionalArrayTests : MultiDimensionalArrayTests
    {
        public RoslynExMultiDimensionalArrayTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNameCollisionTests : NameCollisionTests
    {
        public RoslynExNameCollisionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNamedAndOptionalTests : NamedAndOptionalTests
    {
        public RoslynExNamedAndOptionalTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNameLengthTests : NameLengthTests
    {
        public RoslynExNameLengthTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNameofTests : NameofTests
    {
        public RoslynExNameofTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNativeIntegerTests : NativeIntegerTests
    {
        public RoslynExNativeIntegerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNonTrailingNamedArgumentsTests : NonTrailingNamedArgumentsTests
    {
        public RoslynExNonTrailingNamedArgumentsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNullableConversionTests : NullableConversionTests
    {
        public RoslynExNullableConversionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNullableReferenceTypesTests : NullableReferenceTypesTests
    {
        public RoslynExNullableReferenceTypesTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNullableReferenceTypesVsPatterns : NullableReferenceTypesVsPatterns
    {
        public RoslynExNullableReferenceTypesVsPatterns() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNullableSemanticTests : NullableSemanticTests
    {
        public RoslynExNullableSemanticTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExNullCoalesceAssignmentTests : NullCoalesceAssignmentTests
    {
        public RoslynExNullCoalesceAssignmentTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExObjectAndCollectionInitializerTests : ObjectAndCollectionInitializerTests
    {
        public RoslynExObjectAndCollectionInitializerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExOutVarTests : OutVarTests
    {
        public RoslynExOutVarTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExOverloadResolutionPerfTests : OverloadResolutionPerfTests
    {
        public RoslynExOverloadResolutionPerfTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExOverloadResolutionTests : OverloadResolutionTests
    {
        public RoslynExOverloadResolutionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests : PatternMatchingTests
    {
        public RoslynExPatternMatchingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests2 : PatternMatchingTests2
    {
        public RoslynExPatternMatchingTests2() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests3 : PatternMatchingTests3
    {
        public RoslynExPatternMatchingTests3() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests4 : PatternMatchingTests4
    {
        public RoslynExPatternMatchingTests4() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests_Global : PatternMatchingTests_Global
    {
        public RoslynExPatternMatchingTests_Global() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternMatchingTests_Scope : PatternMatchingTests_Scope
    {
        public RoslynExPatternMatchingTests_Scope() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExPatternSwitchTests : PatternSwitchTests
    {
        public RoslynExPatternSwitchTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExQueryTests : QueryTests
    {
        public RoslynExQueryTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExReadOnlyStructsTests : ReadOnlyStructsTests
    {
        public RoslynExReadOnlyStructsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExRecordTests : RecordTests
    {
        public RoslynExRecordTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExRefEscapingTests : RefEscapingTests
    {
        public RoslynExRefEscapingTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExRefExtensionMethodsTests : RefExtensionMethodsTests
    {
        public RoslynExRefExtensionMethodsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExRefLocalsAndReturnsTests : RefLocalsAndReturnsTests
    {
        public RoslynExRefLocalsAndReturnsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExScriptSemanticsTests : ScriptSemanticsTests
    {
        public RoslynExScriptSemanticsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSemanticAnalyzerTests : SemanticAnalyzerTests
    {
        public RoslynExSemanticAnalyzerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSemanticErrorTests : SemanticErrorTests
    {
        public RoslynExSemanticErrorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSpanStackSafetyTests : SpanStackSafetyTests
    {
        public RoslynExSpanStackSafetyTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExStackAllocInitializerTests : StackAllocInitializerTests
    {
        public RoslynExStackAllocInitializerTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExStackAllocSpanExpressionsTests : StackAllocSpanExpressionsTests
    {
        public RoslynExStackAllocSpanExpressionsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExStructsTests : StructsTests
    {
        public RoslynExStructsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSwitchTests : SwitchTests
    {
        public RoslynExSwitchTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExTargetTypedConditionalOperatorTests : TargetTypedConditionalOperatorTests
    {
        public RoslynExTargetTypedConditionalOperatorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExDefaultLiteralTests : DefaultLiteralTests
    {
        public RoslynExDefaultLiteralTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExTargetTypedObjectCreationTests : TargetTypedObjectCreationTests
    {
        public RoslynExTargetTypedObjectCreationTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExTopLevelStatementsTests : TopLevelStatementsTests
    {
        public RoslynExTopLevelStatementsTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExTryCatchTests : TryCatchTests
    {
        public RoslynExTryCatchTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUninitializedNonNullableFieldTests : UninitializedNonNullableFieldTests
    {
        public RoslynExUninitializedNonNullableFieldTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUnsafeTests : UnsafeTests
    {
        public RoslynExUnsafeTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUserDefinedConversionTests : UserDefinedConversionTests
    {
        public RoslynExUserDefinedConversionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUseSiteErrorTests : UseSiteErrorTests
    {
        public RoslynExUseSiteErrorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUsingDeclarationTests : UsingDeclarationTests
    {
        public RoslynExUsingDeclarationTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExUsingStatementTests : UsingStatementTests
    {
        public RoslynExUsingStatementTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExVarianceTests : VarianceTests
    {
        public RoslynExVarianceTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExWarningVersionTests : WarningVersionTests
    {
        public RoslynExWarningVersionTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExGeneratorDriverTests : GeneratorDriverTests
    {
        public RoslynExGeneratorDriverTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class RoslynExSyntaxAwareGeneratorTests : SyntaxAwareGeneratorTests
    {
        public RoslynExSyntaxAwareGeneratorTests() => RoslynExTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            RoslynExTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
