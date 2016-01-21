using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ReadOnlyTests : CompilingTestBase
    {
        private static readonly CSharpParseOptions options =
            TestOptions.Regular.WithLocalFunctionsFeature().WithReadOnlyParametersFeature().WithReadOnlyVariablesFeature();

        [Fact]
        public void ReadOnlyParameterNotAllowedOnDelegate()
        {
            var source = @"
delegate void D(readonly int i);
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (2,17): error CS2044: The 'readonly' modifier can only be used with members that have a body
                // delegate void D(readonly int i);
                Diagnostic(ErrorCode.ERR_The_readonly_modifier_can_only_be_used_with_members_that_have_a_body, "readonly").WithLocation(2, 17));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedOnInterfaceMethod()
        {
            var source = @"
interface I
{
    void D(readonly int i);
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,12): error CS2044: The 'readonly' modifier can only be used with members that have a body
                //     void D(readonly int i);
                Diagnostic(ErrorCode.ERR_The_readonly_modifier_can_only_be_used_with_members_that_have_a_body, "readonly").WithLocation(4, 12));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedOnInterfaceIndexer()
        {
            var source = @"
interface I
{
    int this[readonly int i] { get; }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,14): error CS2044: The 'readonly' modifier can only be used with members that have a body
                //     int this[readonly int i] { get; }
                Diagnostic(ErrorCode.ERR_The_readonly_modifier_can_only_be_used_with_members_that_have_a_body, "readonly").WithLocation(4, 14));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedOnAbstractMethod()
        {
            var source = @"
abstract class C
{
    public abstract void M(readonly int i);
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,28): error CS2044: The 'readonly' modifier can only be used with members that have a body
                //     public abstract void M(readonly int i);
                Diagnostic(ErrorCode.ERR_The_readonly_modifier_can_only_be_used_with_members_that_have_a_body, "readonly").WithLocation(4, 28));
        }

        [Fact]
        public void ReadOnlyParameterAllowedWithExpressionBody()
        {
            var source = @"
class C
{
    int M(readonly int i) => i;
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedWithRef1()
        {
            var source = @"
class C
{
    void M(readonly ref int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,21): error CS1115:  The parameter modifier 'ref' cannot be used with 'readonly' 
                //     void M(readonly ref int i)
                Diagnostic(ErrorCode.ERR_The_parameter_modifier_ref_cannot_be_used_with_readonly, "ref").WithLocation(4, 21));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedWithRef2()
        {
            var source = @"
class C
{
    void M(ref readonly int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,16): error CS1115:  The parameter modifier 'ref' cannot be used with 'readonly' 
                //     void M(ref readonly int i)
                Diagnostic(ErrorCode.ERR_The_parameter_modifier_ref_cannot_be_used_with_readonly, "readonly").WithLocation(4, 16));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedWithOut1()
        {
            var source = @"
class C
{
    void M(readonly out int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,21): error CS1114:  The parameter modifier 'out' cannot be used with 'readonly' 
                //     void M(readonly out int i)
                Diagnostic(ErrorCode.ERR_The_parameter_modifier_out_cannot_be_used_with_readonly, "out").WithLocation(4, 21),
                // (4,10): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //     void M(readonly out int i)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("i").WithLocation(4, 10));
        }

        [Fact]
        public void ReadOnlyParameterNotAllowedWithOut2()
        {
            var source = @"
class C
{
    void M(out readonly int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,16): error CS1114:  The parameter modifier 'out' cannot be used with 'readonly' 
                //     void M(out readonly int i)
                Diagnostic(ErrorCode.ERR_The_parameter_modifier_out_cannot_be_used_with_readonly, "readonly").WithLocation(4, 16),
                // (4,10): error CS0177: The out parameter 'i' must be assigned to before control leaves the current method
                //     void M(out readonly int i)
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "M").WithArguments("i").WithLocation(4, 10));
        }

        [Fact]
        public void ReadOnlyReadonlyParameterNotAllowed()
        {
            var source = @"
class C
{
    void M(readonly readonly int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,21): error CS1107: A parameter can only have one 'readonly' modifier
                //     void M(readonly readonly int i)
                Diagnostic(ErrorCode.ERR_DupParamMod, "readonly").WithArguments("readonly").WithLocation(4, 21));
        }

        [Fact]
        public void NoAssignmentToReadOnlyParameter()
        {
            var source = @"
class C
{
    void M(readonly int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoCompoundAssignmentToReadOnlyParameter()
        {
            var source = @"
class C
{
    void M(readonly int i)
    {
        i += 0;
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i += 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoIncrementToReadOnlyParameter()
        {
            var source = @"
class C
{
    void M(readonly int i)
    {
        i++;
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i++;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoRefPassingOfReadOnlyParameter()
        {
            var source = @"
class C
{
    void M(readonly int i)
    {
        M2(ref i);
    }

    void M2(ref int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,16): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         M2(ref i);
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 16));
        }

        [Fact]
        public void NoOutPassingOfReadOnlyParameter()
        {
            var source = @"
class C
{
    void M(readonly int i)
    {
        M2(out i);
    }

    void M2(out int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,16): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         M2(out i);
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 16));
        }

        [Fact]
        public void NoAssignToReadonlyThis1()
        {
            var source = @"
static class C
{
    static void M(readonly this int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoAssignToReadonlyThis2()
        {
            var source = @"
static class C
{
    static void M(readonly this int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoAssignToReadOnlyParams1()
        {
            var source = @"
class C
{
    void M(readonly params int[] i)
    {
        i = null;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void NoAssignToReadOnlyParams2()
        {
            var source = @"
class C
{
    void M(params readonly int[] i)
    {
        i = null;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //         i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void ReadOnlyDoesNotAffectOverrides1()
        {
            var source = @"
class C
{
    public virtual void M(readonly int i)
    {
    }
}

class D : C
{
    public override void M(int i)
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyDoesNotAffectOverrides2()
        {
            var source = @"
class C
{
    public virtual void M(int i)
    {
    }
}

class D : C
{
    public override void M(readonly int i)
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyDoesNotAffectOverloads()
        {
            var source = @"
class C
{
    public void M(readonly int i)
    {
    }

    public void M(int i)
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (8,17): error CS0111: Type 'C' already defines a member called 'M' with the same parameter types
                //     public void M(int i)
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "M").WithArguments("M", "C").WithLocation(8, 17));
        }

        [Fact]
        public void ReadOnlyVariableWithoutInitializer()
        {
            var source = @"
class C
{
    void M()
    {
        readonly int i;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly int i;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9),
                // (6,22): warning CS0168: The variable 'i' is declared but never used
                //         readonly int i;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "i").WithArguments("i").WithLocation(6, 22));
        }

        [Fact]
        public void ReadOnlyReadOnlyVariable()
        {
            var source = @"
class C
{
    void M()
    {
        readonly readonly int i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9),
                // (6,18): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 18),
                // (6,31): warning CS0219: The variable 'i' is assigned but its value is never used
                //         readonly readonly int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 31));
        }

        [Fact]
        public void ReadOnlyConstVariable()
        {
            var source = @"
class C
{
    void M()
    {
        readonly const int i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly const int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9),
                // (6,28): warning CS0219: The variable 'i' is assigned but its value is never used
                //         readonly const int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 28));
        }

        [Fact]
        public void ConstReadOnlyVariable()
        {
            var source = @"
class C
{
    void M()
    {
        const readonly int i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,15): error CS0106: The modifier 'readonly' is not valid for this item
                //         const readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 15),
                // (6,28): warning CS0219: The variable 'i' is assigned but its value is never used
                //         const readonly int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 28));
        }

        [Fact]
        public void ReadOnlyVariableAssignment()
        {
            var source = @"
class C
{
    void M()
    {
        readonly int i = 0;
        i = 1;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9),
                // (6,22): warning CS0219: The variable 'i' is assigned but its value is never used
                //         readonly int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 22));
        }

        [Fact]
        public void ReadOnlyVariableCompoundAssignment()
        {
            var source = @"
class C
{
    void M()
    {
        readonly int i = 0;
        i += 1;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9));
        }

        [Fact]
        public void ReadOnlyVariableRefPass()
        {
            var source = @"
class C
{
    void M()
    {
        readonly int i = 0;
        M2(ref i);
    }

    void M2(ref int i)
    {
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9));
        }

        [Fact]
        public void ReadOnlyVariableOutPass()
        {
            var source = @"
class C
{
    void M()
    {
        readonly int i = 0;
        M2(ref i);
    }

    void M2(out int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlibAndSystemCore(source, parseOptions: options).VerifyDiagnostics(
                // (6,9): error CS0106: The modifier 'readonly' is not valid for this item
                //         readonly int i = 0;
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 9),
                // (7,16): error CS1620: Argument 1 must be passed with the 'out' keyword
                //         M2(ref i);
                Diagnostic(ErrorCode.ERR_BadArgRef, "i").WithArguments("1", "out").WithLocation(7, 16));
        }

        [Fact]
        public void ReadOnlyParameterOnIndexer()
        {
            var source = @"
class C
{
    int this[readonly int i] { get { return i; } }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyParameterOnIndexerNoAssignmentInGetter()
        {
            var source = @"
class C
{
    int this[readonly int i]
    {
        get
        {
            i = 1;
        }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(8, 13),
                // (6,9): error CS0161: 'C.this[int].get': not all code paths return a value
                //         get
                Diagnostic(ErrorCode.ERR_ReturnExpected, "get").WithArguments("C.this[int].get").WithLocation(6, 9));
        }

        [Fact]
        public void ReadOnlyParameterOnIndexerNoAssignmentInSetter()
        {
            var source = @"
class C
{
    int this[readonly int i]
    {
        set
        {
            i = 1;
        }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 1;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(8, 13));
        }

        [Fact]
        public void ReadOnlyParameterOnConstructor()
        {
            var source = @"
class C
{
    public C(readonly int i)
    {
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyParameterOnExternConstructor()
        {
            var source = @"
class C
{
    extern public C(readonly int i);
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (4,21): error CS2044: The 'readonly' modifier can only be used with members that have a body
                //     extern public C(readonly int i);
                Diagnostic(ErrorCode.ERR_The_readonly_modifier_can_only_be_used_with_members_that_have_a_body, "readonly").WithLocation(4, 21),
                // (4,19): warning CS0824: Constructor 'C.C(int)' is marked external
                //     extern public C(readonly int i);
                Diagnostic(ErrorCode.WRN_ExternCtorNoImplementation, "C").WithArguments("C.C(int)").WithLocation(4, 19));
        }

        [Fact]
        public void ReadOnlyParameterOnConstructorNoAssignment()
        {
            var source = @"
class C
{
    public C(readonly int i)
    {
        i = 0;
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
            // (6,9): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
            //         i = 0;
            Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(6, 9));
        }

        [Fact]
        public void ReadOnlyParameterInLambda1()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = (readonly int i) => { };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyParameterInLambda2()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = (readonly int i) =>
        {
            i = 0;
        };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (10,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(10, 13));
        }

        [Fact]
        public void ReadOnlyParameterInLambda3()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = (readonly i) =>
        {
        };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void ReadOnlyParameterInLambda4()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = (readonly i) =>
        {
            i = 0;
        };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (10,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(10, 13));
        }

        [Fact]
        public void ReadOnlyParameterInAnonymousMethod1()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = delegate (readonly int i) { };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics();
        }

        [Fact]
        public void NoAssignmentToReadOnlyParameterInAnonymousMethod()
        {
            var source = @"
using System;

class C
{
    public C()
    {
        Action<int> a = delegate (readonly int i)
        {
            i = 0;
        };
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (10,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(10, 13));
        }

        [Fact]
        public void TestLocalFunction1()
        {
            var source = @"
class C
{
    public C()
    {
        void LocalFunction(readonly int i)
        {
        }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,14): warning CS0168: The variable 'LocalFunction' is declared but never used
                //         void LocalFunction(readonly int i)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "LocalFunction").WithArguments("LocalFunction").WithLocation(6, 14));
        }

        [Fact]
        public void TestLocalFunction2()
        {
            var source = @"
class C
{
    public C()
    {
        void LocalFunction(readonly int i)
        {
            i = 0;
        }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (8,13): error CS1656: Cannot assign to 'i' because it is a 'readonly parameter'
                //             i = 0;
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "i").WithArguments("i", "readonly parameter").WithLocation(8, 13),
                // (6,14): warning CS0168: The variable 'LocalFunction' is declared but never used
                //         void LocalFunction(readonly int i)
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "LocalFunction").WithArguments("LocalFunction").WithLocation(6, 14));
        }

        [Fact]
        public void TestReadOnlyForVariableDeclaration()
        {
            var source = @"
class C
{
    public C()
    {
        for (readonly int i = 0;;) { }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,14): error CS0106: The modifier 'readonly' is not valid for this item
                //         for (readonly int i = 0;;) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 14),
                // (6,27): warning CS0219: The variable 'i' is assigned but its value is never used
                //         for (readonly int i = 0;;) { }
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 27));
        }

        [Fact]
        public void TestReadOnlyForEachVariableDeclaration()
        {
            var source = @"
class C
{
    public C()
    {
        foreach (readonly int i in null) { }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,18): error CS0106: The modifier 'readonly' is not valid for this item
                //         foreach (readonly int i in null) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 18),
                // (6,36): error CS0186: Use of null is not valid in this context
                //         foreach (readonly int i in null) { }
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(6, 36));
        }

        [Fact]
        public void TestReadOnlyUsingVariableDeclaration()
        {
            var source = @"
class C
{
    public C()
    {
        using (readonly int i = null) { }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,16): error CS0106: The modifier 'readonly' is not valid for this item
                //         using (readonly int i = null) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 16),
                // (6,33): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //         using (readonly int i = null) { }
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(6, 33),
                // (6,25): error CS1674: 'int': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (readonly int i = null) { }
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "int i = null").WithArguments("int").WithLocation(6, 25));
        }

        [Fact]
        public void TestReadOnlyFixedVariableDeclaration()
        {
            var source = @"
class C
{
    public unsafe C()
    {
        fixed (readonly int* i = null) { }
    }
}
";

            CreateCompilationWithMscorlib(source, parseOptions: options).VerifyDiagnostics(
                // (6,16): error CS0106: The modifier 'readonly' is not valid for this item
                //         fixed (readonly int* i = null) { }
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "readonly").WithArguments("readonly").WithLocation(6, 16),
                // (4,19): error CS0227: Unsafe code may only appear if compiling with /unsafe
                //     public unsafe C()
                Diagnostic(ErrorCode.ERR_IllegalUnsafe, "C").WithLocation(4, 19),
                // (6,34): error CS0213: You cannot use the fixed statement to take the address of an already fixed expression
                //         fixed (readonly int* i = null) { }
                Diagnostic(ErrorCode.ERR_FixedNotNeeded, "null").WithLocation(6, 34));
        }
    }
}
