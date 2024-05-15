using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.Semantics;
using Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Metalama.Compiler.UnitTests.Diagnostics
{
    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerDiagnosticAnalyzerTests : DiagnosticAnalyzerTests
    {
        public MetalamaCompilerDiagnosticAnalyzerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerDiagnosticSuppressorTests : DiagnosticSuppressorTests
    {
        public MetalamaCompilerDiagnosticSuppressorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerGetDiagnosticsTests : GetDiagnosticsTests
    {
        public MetalamaCompilerGetDiagnosticsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerMethodGroupConversion : MethodGroupConversion
    {
        public MetalamaCompilerMethodGroupConversion() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerFlowDiagnosticTests : FlowDiagnosticTests
    {
        public MetalamaCompilerFlowDiagnosticTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerFlowTests : FlowTests
    {
        public MetalamaCompilerFlowTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerLocalFunctions : LocalFunctions
    {
        public MetalamaCompilerLocalFunctions() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRegionAnalysisTests : RegionAnalysisTests
    {
        public MetalamaCompilerRegionAnalysisTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerStructTests : StructTests
    {
        public MetalamaCompilerStructTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerAccessCheckTests : AccessCheckTests
    {
        public MetalamaCompilerAccessCheckTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerAccessibilityTests : AccessibilityTests
    {
        public MetalamaCompilerAccessibilityTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerAmbiguousOverrideTests : AmbiguousOverrideTests
    {
        public MetalamaCompilerAmbiguousOverrideTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerAnonymousFunctionTests : AnonymousFunctionTests
    {
        public MetalamaCompilerAnonymousFunctionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerArglistTests : ArglistTests
    {
        public MetalamaCompilerArglistTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerAwaitExpressionTests : AwaitExpressionTests
    {
        public MetalamaCompilerAwaitExpressionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBetterCandidates : BetterCandidates
    {
        public MetalamaCompilerBetterCandidates() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBindingAsyncTasklikeMoreTests : BindingAsyncTasklikeMoreTests
    {
        public MetalamaCompilerBindingAsyncTasklikeMoreTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBindingAsyncTasklikeTests : BindingAsyncTasklikeTests
    {
        public MetalamaCompilerBindingAsyncTasklikeTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBindingAsyncTests : BindingAsyncTests
    {
        public MetalamaCompilerBindingAsyncTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBindingAwaitTests : BindingAwaitTests
    {
        public MetalamaCompilerBindingAwaitTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerBindingTests : BindingTests
    {
        public MetalamaCompilerBindingTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerColorColorTests : ColorColorTests
    {
        public MetalamaCompilerColorColorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerConditionalOperatorTests : ConditionalOperatorTests
    {
        public MetalamaCompilerConditionalOperatorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerConstantTests : ConstantTests
    {
        public MetalamaCompilerConstantTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerDeconstructionTests : DeconstructionTests
    {
        public MetalamaCompilerDeconstructionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerDelegateTypeTests : DelegateTypeTests
    {
        public MetalamaCompilerDelegateTypeTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSyntaxBinderTests : SyntaxBinderTests
    {
        public MetalamaCompilerSyntaxBinderTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerExpressionBodiedMemberTests : ExpressionBodiedMemberTests
    {
        public MetalamaCompilerExpressionBodiedMemberTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerForEachTests : ForEachTests
    {
        public MetalamaCompilerForEachTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerForLoopErrorTests : ForLoopErrorTests
    {
        public MetalamaCompilerForLoopErrorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerFunctionPointerTests : FunctionPointerTests
    {
        public MetalamaCompilerFunctionPointerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerGenericConstraintsTests : GenericConstraintsTests
    {
        public MetalamaCompilerGenericConstraintsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerGlobalUsingDirectiveTests : GlobalUsingDirectiveTests
    {
        public MetalamaCompilerGlobalUsingDirectiveTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerHideByNameTests : HideByNameTests
    {
        public MetalamaCompilerHideByNameTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerImplicitlyTypeArraysTests : ImplicitlyTypeArraysTests
    {
        public MetalamaCompilerImplicitlyTypeArraysTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerImplicitlyTypedLocalTests : ImplicitlyTypedLocalTests
    {
        public MetalamaCompilerImplicitlyTypedLocalTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerImplicitObjectCreationTests : ImplicitObjectCreationTests
    {
        public MetalamaCompilerImplicitObjectCreationTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerIndexAndRangeTests : IndexAndRangeTests
    {
        public MetalamaCompilerIndexAndRangeTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerInheritanceBindingTests : InheritanceBindingTests
    {
        public MetalamaCompilerInheritanceBindingTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerInitOnlyMemberTests : InitOnlyMemberTests
    {
        public MetalamaCompilerInitOnlyMemberTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }


    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerInterpolationTests : InterpolationTests
    {
        public MetalamaCompilerInterpolationTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerIteratorTests : IteratorTests
    {
        public MetalamaCompilerIteratorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerLambdaDiscardParametersTests : LambdaDiscardParametersTests
    {
        public MetalamaCompilerLambdaDiscardParametersTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerLambdaTests : LambdaTests
    {
        public MetalamaCompilerLambdaTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerLocalFunctionTests : LocalFunctionTests
    {
        public MetalamaCompilerLocalFunctionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerLockTests : Microsoft.CodeAnalysis.CSharp.UnitTests.LockTests
    {
        public MetalamaCompilerLockTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerGetSemanticInfoTests : GetSemanticInfoTests
    {
        public MetalamaCompilerGetSemanticInfoTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerMethodBodyModelTests : MethodBodyModelTests
    {
        public MetalamaCompilerMethodBodyModelTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerMethodTypeInferenceTests : MethodTypeInferenceTests
    {
        public MetalamaCompilerMethodTypeInferenceTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerMultiDimensionalArrayTests : MultiDimensionalArrayTests
    {
        public MetalamaCompilerMultiDimensionalArrayTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNameCollisionTests : NameCollisionTests
    {
        public MetalamaCompilerNameCollisionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNamedAndOptionalTests : NamedAndOptionalTests
    {
        public MetalamaCompilerNamedAndOptionalTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNameLengthTests : NameLengthTests
    {
        public MetalamaCompilerNameLengthTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNameofTests : NameofTests
    {
        public MetalamaCompilerNameofTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNativeIntegerTests : NativeIntegerTests
    {
        public MetalamaCompilerNativeIntegerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNonTrailingNamedArgumentsTests : NonTrailingNamedArgumentsTests
    {
        public MetalamaCompilerNonTrailingNamedArgumentsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNullableConversionTests : NullableConversionTests
    {
        public MetalamaCompilerNullableConversionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNullableReferenceTypesTests : NullableReferenceTypesTests
    {
        public MetalamaCompilerNullableReferenceTypesTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNullableReferenceTypesVsPatterns : NullableReferenceTypesVsPatterns
    {
        public MetalamaCompilerNullableReferenceTypesVsPatterns() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNullableSemanticTests : NullableSemanticTests
    {
        public MetalamaCompilerNullableSemanticTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerNullCoalesceAssignmentTests : NullCoalesceAssignmentTests
    {
        public MetalamaCompilerNullCoalesceAssignmentTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerObjectAndCollectionInitializerTests : ObjectAndCollectionInitializerTests
    {
        public MetalamaCompilerObjectAndCollectionInitializerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerOutVarTests : OutVarTests
    {
        public MetalamaCompilerOutVarTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    /* Disabled because it's so slow.
    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerOverloadResolutionPerfTests : OverloadResolutionPerfTests
    {
        public MetalamaCompilerOverloadResolutionPerfTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
    */

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerOverloadResolutionTests : OverloadResolutionTests
    {
        public MetalamaCompilerOverloadResolutionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests : PatternMatchingTests
    {
        public MetalamaCompilerPatternMatchingTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests2 : PatternMatchingTests2
    {
        public MetalamaCompilerPatternMatchingTests2() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests3 : PatternMatchingTests3
    {
        public MetalamaCompilerPatternMatchingTests3() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests4 : PatternMatchingTests4
    {
        public MetalamaCompilerPatternMatchingTests4() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests_Global : PatternMatchingTests_Global
    {
        public MetalamaCompilerPatternMatchingTests_Global() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternMatchingTests_Scope : PatternMatchingTests_Scope
    {
        public MetalamaCompilerPatternMatchingTests_Scope() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerPatternSwitchTests : PatternSwitchTests
    {
        public MetalamaCompilerPatternSwitchTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerQueryTests : QueryTests
    {
        public MetalamaCompilerQueryTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerReadOnlyStructsTests : ReadOnlyStructsTests
    {
        public MetalamaCompilerReadOnlyStructsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRecordStructTests : RecordStructTests
    {
        public MetalamaCompilerRecordStructTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRecordTests : RecordTests
    {
        public MetalamaCompilerRecordTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRefEscapingTests : RefEscapingTests
    {
        public MetalamaCompilerRefEscapingTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRefExtensionMethodsTests : RefExtensionMethodsTests
    {
        public MetalamaCompilerRefExtensionMethodsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerRefLocalsAndReturnsTests : RefLocalsAndReturnsTests
    {
        public MetalamaCompilerRefLocalsAndReturnsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerScriptSemanticsTests : ScriptSemanticsTests
    {
        public MetalamaCompilerScriptSemanticsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSemanticAnalyzerTests : SemanticAnalyzerTests
    {
        public MetalamaCompilerSemanticAnalyzerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSemanticErrorTests : SemanticErrorTests
    {
        public MetalamaCompilerSemanticErrorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSpanStackSafetyTests : SpanStackSafetyTests
    {
        public MetalamaCompilerSpanStackSafetyTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerStackAllocInitializerTests : StackAllocInitializerTests
    {
        public MetalamaCompilerStackAllocInitializerTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerStackAllocSpanExpressionsTests : StackAllocSpanExpressionsTests
    {
        public MetalamaCompilerStackAllocSpanExpressionsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerStructsTests : StructsTests
    {
        public MetalamaCompilerStructsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSwitchTests : SwitchTests
    {
        public MetalamaCompilerSwitchTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerTargetTypedConditionalOperatorTests : TargetTypedConditionalOperatorTests
    {
        public MetalamaCompilerTargetTypedConditionalOperatorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerDefaultLiteralTests : DefaultLiteralTests
    {
        public MetalamaCompilerDefaultLiteralTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerTopLevelStatementsTests : TopLevelStatementsTests
    {
        public MetalamaCompilerTopLevelStatementsTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerTryCatchTests : TryCatchTests
    {
        public MetalamaCompilerTryCatchTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUninitializedNonNullableFieldTests : UninitializedNonNullableFieldTests
    {
        public MetalamaCompilerUninitializedNonNullableFieldTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUnsafeTests : UnsafeTests
    {
        public MetalamaCompilerUnsafeTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUserDefinedConversionTests : UserDefinedConversionTests
    {
        public MetalamaCompilerUserDefinedConversionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUseSiteErrorTests : UseSiteErrorTests
    {
        public MetalamaCompilerUseSiteErrorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUsingDeclarationTests : UsingDeclarationTests
    {
        public MetalamaCompilerUsingDeclarationTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerUsingStatementTests : UsingStatementTests
    {
        public MetalamaCompilerUsingStatementTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerVarianceTests : VarianceTests
    {
        public MetalamaCompilerVarianceTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerWarningVersionTests : WarningVersionTests
    {
        public MetalamaCompilerWarningVersionTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerGeneratorDriverTests : GeneratorDriverTests
    {
        public MetalamaCompilerGeneratorDriverTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }

    [Trait("Category", "OuterLoop")]
    public class MetalamaCompilerSyntaxAwareGeneratorTests : SyntaxAwareGeneratorTests
    {
        public MetalamaCompilerSyntaxAwareGeneratorTests() => MetalamaCompilerTest.ShouldExecuteTransformer = true;

        public override void Dispose()
        {
            MetalamaCompilerTest.ShouldExecuteTransformer = false;
            base.Dispose();
        }
    }
}
