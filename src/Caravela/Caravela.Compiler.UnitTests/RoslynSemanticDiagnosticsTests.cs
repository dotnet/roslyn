using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics;
using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Caravela.Compiler.UnitTests.Diagnostics
{
    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerDiagnosticAnalyzerTests : DiagnosticAnalyzerTests
    {
        public CaravelaCompilerDiagnosticAnalyzerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerDiagnosticSuppressorTests : DiagnosticSuppressorTests
    {
        public CaravelaCompilerDiagnosticSuppressorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerGetDiagnosticsTests : GetDiagnosticsTests
    {
        public CaravelaCompilerGetDiagnosticsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerMethodGroupConversion : MethodGroupConversion
    {
        public CaravelaCompilerMethodGroupConversion() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerFlowDiagnosticTests : FlowDiagnosticTests
    {
        public CaravelaCompilerFlowDiagnosticTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerFlowTests : FlowTests
    {
        public CaravelaCompilerFlowTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerLocalFunctions : LocalFunctions
    {
        public CaravelaCompilerLocalFunctions() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerRegionAnalysisTests : RegionAnalysisTests
    {
        public CaravelaCompilerRegionAnalysisTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerStructTests : StructTests
    {
        public CaravelaCompilerStructTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerAccessCheckTests : AccessCheckTests
    {
        public CaravelaCompilerAccessCheckTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerAccessibilityTests : AccessibilityTests
    {
        public CaravelaCompilerAccessibilityTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerAmbiguousOverrideTests : AmbiguousOverrideTests
    {
        public CaravelaCompilerAmbiguousOverrideTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerAnonymousFunctionTests : AnonymousFunctionTests
    {
        public CaravelaCompilerAnonymousFunctionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerArglistTests : ArglistTests
    {
        public CaravelaCompilerArglistTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerAwaitExpressionTests : AwaitExpressionTests
    {
        public CaravelaCompilerAwaitExpressionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBetterCandidates : BetterCandidates
    {
        public CaravelaCompilerBetterCandidates() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBindingAsyncTasklikeMoreTests : BindingAsyncTasklikeMoreTests
    {
        public CaravelaCompilerBindingAsyncTasklikeMoreTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBindingAsyncTasklikeTests : BindingAsyncTasklikeTests
    {
        public CaravelaCompilerBindingAsyncTasklikeTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBindingAsyncTests : BindingAsyncTests
    {
        public CaravelaCompilerBindingAsyncTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBindingAwaitTests : BindingAwaitTests
    {
        public CaravelaCompilerBindingAwaitTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerBindingTests : BindingTests
    {
        public CaravelaCompilerBindingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerColorColorTests : ColorColorTests
    {
        public CaravelaCompilerColorColorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerConditionalOperatorTests : ConditionalOperatorTests
    {
        public CaravelaCompilerConditionalOperatorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerConstantTests : ConstantTests
    {
        public CaravelaCompilerConstantTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerDeconstructionTests : DeconstructionTests
    {
        public CaravelaCompilerDeconstructionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSyntaxBinderTests : SyntaxBinderTests
    {
        public CaravelaCompilerSyntaxBinderTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerExpressionBodiedMemberTests : ExpressionBodiedMemberTests
    {
        public CaravelaCompilerExpressionBodiedMemberTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerForEachTests : ForEachTests
    {
        public CaravelaCompilerForEachTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerForLoopErrorTests : ForLoopErrorTests
    {
        public CaravelaCompilerForLoopErrorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerFunctionPointerTests : FunctionPointerTests
    {
        public CaravelaCompilerFunctionPointerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerGenericConstraintsTests : GenericConstraintsTests
    {
        public CaravelaCompilerGenericConstraintsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerHideByNameTests : HideByNameTests
    {
        public CaravelaCompilerHideByNameTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerImplicitlyTypeArraysTests : ImplicitlyTypeArraysTests
    {
        public CaravelaCompilerImplicitlyTypeArraysTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerImplicitlyTypedLocalTests : ImplicitlyTypedLocalTests
    {
        public CaravelaCompilerImplicitlyTypedLocalTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerIndexAndRangeTests : IndexAndRangeTests
    {
        public CaravelaCompilerIndexAndRangeTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerInheritanceBindingTests : InheritanceBindingTests
    {
        public CaravelaCompilerInheritanceBindingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerInitOnlyMemberTests : InitOnlyMemberTests
    {
        public CaravelaCompilerInitOnlyMemberTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerInteractiveUsingTests : InteractiveUsingTests
    {
        public CaravelaCompilerInteractiveUsingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerInterpolationTests : InterpolationTests
    {
        public CaravelaCompilerInterpolationTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerIteratorTests : IteratorTests
    {
        public CaravelaCompilerIteratorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerLambdaDiscardParametersTests : LambdaDiscardParametersTests
    {
        public CaravelaCompilerLambdaDiscardParametersTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerLambdaTests : LambdaTests
    {
        public CaravelaCompilerLambdaTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerLocalFunctionTests : LocalFunctionTests
    {
        public CaravelaCompilerLocalFunctionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerLockTests : LockTests
    {
        public CaravelaCompilerLockTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerGetSemanticInfoTests : GetSemanticInfoTests
    {
        public CaravelaCompilerGetSemanticInfoTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerMethodBodyModelTests : MethodBodyModelTests
    {
        public CaravelaCompilerMethodBodyModelTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerMethodTypeInferenceTests : MethodTypeInferenceTests
    {
        public CaravelaCompilerMethodTypeInferenceTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerMultiDimensionalArrayTests : MultiDimensionalArrayTests
    {
        public CaravelaCompilerMultiDimensionalArrayTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNameCollisionTests : NameCollisionTests
    {
        public CaravelaCompilerNameCollisionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNamedAndOptionalTests : NamedAndOptionalTests
    {
        public CaravelaCompilerNamedAndOptionalTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNameLengthTests : NameLengthTests
    {
        public CaravelaCompilerNameLengthTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNameofTests : NameofTests
    {
        public CaravelaCompilerNameofTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNativeIntegerTests : NativeIntegerTests
    {
        public CaravelaCompilerNativeIntegerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNonTrailingNamedArgumentsTests : NonTrailingNamedArgumentsTests
    {
        public CaravelaCompilerNonTrailingNamedArgumentsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNullableConversionTests : NullableConversionTests
    {
        public CaravelaCompilerNullableConversionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNullableReferenceTypesTests : NullableReferenceTypesTests
    {
        public CaravelaCompilerNullableReferenceTypesTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNullableReferenceTypesVsPatterns : NullableReferenceTypesVsPatterns
    {
        public CaravelaCompilerNullableReferenceTypesVsPatterns() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNullableSemanticTests : NullableSemanticTests
    {
        public CaravelaCompilerNullableSemanticTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerNullCoalesceAssignmentTests : NullCoalesceAssignmentTests
    {
        public CaravelaCompilerNullCoalesceAssignmentTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerObjectAndCollectionInitializerTests : ObjectAndCollectionInitializerTests
    {
        public CaravelaCompilerObjectAndCollectionInitializerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerOutVarTests : OutVarTests
    {
        public CaravelaCompilerOutVarTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerOverloadResolutionPerfTests : OverloadResolutionPerfTests
    {
        public CaravelaCompilerOverloadResolutionPerfTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerOverloadResolutionTests : OverloadResolutionTests
    {
        public CaravelaCompilerOverloadResolutionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests : PatternMatchingTests
    {
        public CaravelaCompilerPatternMatchingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests2 : PatternMatchingTests2
    {
        public CaravelaCompilerPatternMatchingTests2() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests3 : PatternMatchingTests3
    {
        public CaravelaCompilerPatternMatchingTests3() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests4 : PatternMatchingTests4
    {
        public CaravelaCompilerPatternMatchingTests4() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests_Global : PatternMatchingTests_Global
    {
        public CaravelaCompilerPatternMatchingTests_Global() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternMatchingTests_Scope : PatternMatchingTests_Scope
    {
        public CaravelaCompilerPatternMatchingTests_Scope() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerPatternSwitchTests : PatternSwitchTests
    {
        public CaravelaCompilerPatternSwitchTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerQueryTests : QueryTests
    {
        public CaravelaCompilerQueryTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerReadOnlyStructsTests : ReadOnlyStructsTests
    {
        public CaravelaCompilerReadOnlyStructsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerRecordTests : RecordTests
    {
        public CaravelaCompilerRecordTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerRefEscapingTests : RefEscapingTests
    {
        public CaravelaCompilerRefEscapingTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerRefExtensionMethodsTests : RefExtensionMethodsTests
    {
        public CaravelaCompilerRefExtensionMethodsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerRefLocalsAndReturnsTests : RefLocalsAndReturnsTests
    {
        public CaravelaCompilerRefLocalsAndReturnsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerScriptSemanticsTests : ScriptSemanticsTests
    {
        public CaravelaCompilerScriptSemanticsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSemanticAnalyzerTests : SemanticAnalyzerTests
    {
        public CaravelaCompilerSemanticAnalyzerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSemanticErrorTests : SemanticErrorTests
    {
        public CaravelaCompilerSemanticErrorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSpanStackSafetyTests : SpanStackSafetyTests
    {
        public CaravelaCompilerSpanStackSafetyTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerStackAllocInitializerTests : StackAllocInitializerTests
    {
        public CaravelaCompilerStackAllocInitializerTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerStackAllocSpanExpressionsTests : StackAllocSpanExpressionsTests
    {
        public CaravelaCompilerStackAllocSpanExpressionsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerStructsTests : StructsTests
    {
        public CaravelaCompilerStructsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSwitchTests : SwitchTests
    {
        public CaravelaCompilerSwitchTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerTargetTypedConditionalOperatorTests : TargetTypedConditionalOperatorTests
    {
        public CaravelaCompilerTargetTypedConditionalOperatorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerDefaultLiteralTests : DefaultLiteralTests
    {
        public CaravelaCompilerDefaultLiteralTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerTargetTypedObjectCreationTests : TargetTypedObjectCreationTests
    {
        public CaravelaCompilerTargetTypedObjectCreationTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerTopLevelStatementsTests : TopLevelStatementsTests
    {
        public CaravelaCompilerTopLevelStatementsTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerTryCatchTests : TryCatchTests
    {
        public CaravelaCompilerTryCatchTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUninitializedNonNullableFieldTests : UninitializedNonNullableFieldTests
    {
        public CaravelaCompilerUninitializedNonNullableFieldTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUnsafeTests : UnsafeTests
    {
        public CaravelaCompilerUnsafeTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUserDefinedConversionTests : UserDefinedConversionTests
    {
        public CaravelaCompilerUserDefinedConversionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUseSiteErrorTests : UseSiteErrorTests
    {
        public CaravelaCompilerUseSiteErrorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUsingDeclarationTests : UsingDeclarationTests
    {
        public CaravelaCompilerUsingDeclarationTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerUsingStatementTests : UsingStatementTests
    {
        public CaravelaCompilerUsingStatementTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerVarianceTests : VarianceTests
    {
        public CaravelaCompilerVarianceTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerWarningVersionTests : WarningVersionTests
    {
        public CaravelaCompilerWarningVersionTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerGeneratorDriverTests : GeneratorDriverTests
    {
        public CaravelaCompilerGeneratorDriverTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class CaravelaCompilerSyntaxAwareGeneratorTests : SyntaxAwareGeneratorTests
    {
        public CaravelaCompilerSyntaxAwareGeneratorTests() => CaravelaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            CaravelaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
