// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class ForLoopErrorTests : CompilingTestBase
    {
        // Condition expression
        [Fact]
        public void CS1525ERR_InvalidExprTerm_ConditionExpression()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int k = 0, j = 0; k < 100, j > 5; k++)
        {
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ","),
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(","),
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";"),
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                    Diagnostic(ErrorCode.ERR_RbraceExpected, ")"),
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "j > 5"),
                    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k")
                );
        }

        // Condition expression must be bool type
        [Fact]
        public void CS0029ERR_NoImplicitConv_ConditionExpressionMustbeBool()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 10; i; i = i - 1)
        {
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoImplicitConv, "i").WithArguments("int", "bool"));
        }

        // Condition expression could not be nullable bool type
        [Fact]
        public void CS0266ERR_NoImplicitConvCast_ConditionExpressionMustbeBool()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        bool? b = true;
        for (int i = 0; b; i = i + 1)
        {
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "b").WithArguments("bool?", "bool"));
        }

        // Content within For
        [Fact]
        public void CS1026ERR_CloseParenExpected_ContentOfFor()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 10; i < 100;;);
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_CloseParenExpected, ";"),
                    Diagnostic(ErrorCode.ERR_RbraceExpected, ")")
                );

            text =
@"
class C
{
    static void Main(string[] args)
    {
        for ();
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"),
                    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")")
                );
        }

        // 'GotoStatement' is not yet implemented in Roslyn
        // Goto in for Loops
        [Fact]
        public void CS0159ERR_LabelNotFound_GotoForNestedLoop_4()
        {
            var text =
@"
class C
{
    static void Main(string[] args)
    {
        for (int i = 0; i < 5; i = i + 1)
        {
            goto outerLoop;
            for (int j = 0; j < 10; j = j + 1)
            {
            outerLoop:
                return;
            }
        }
    }
}
";
            CreateCompilation(text).
                VerifyDiagnostics(Diagnostic(ErrorCode.ERR_LabelNotFound, "outerLoop").WithArguments("outerLoop"),
                Diagnostic(ErrorCode.WRN_UnreachableCode, "j"),
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "outerLoop"));
        }

        // 'QueryExpression' is not yet implemented in Roslyn.
        // Query expression in condition expression
        [Fact]
        public void CS0029ERR_NoImplicitConv_QueryInCondition()
        {
            var text =
@"
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        for (;from x in new[] { 1, 2, 3 }
             let z = x.ToString()
             select z into w
             select w;  ) { }        // invalid
    }
}
";
            CreateCompilationWithMscorlib40AndSystemCore(text).
                VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_NoImplicitConv,
@"from x in new[] { 1, 2, 3 }
             let z = x.ToString()
             select z into w
             select w").WithArguments("System.Collections.Generic.IEnumerable<string>", "bool"));
        }

        // Query expression in iterator expressions
        [Fact]
        public void CS0201ERR_IllegalStatement_QueryInIterator()
        {
            var text =
@"
using System.Linq;
class C
{
    static void Main(string[] args)
    {
        for (;;from x in new[] { 1, 2, 3 }
             let z = x.ToString()
             select z into w
             select w) { }        // invalid
    }
}
";

            var comp = CreateCompilation(text);
            DiagnosticsUtils.VerifyErrorCodesNoLineColumn(comp.GetDiagnostics(),
                new ErrorDescription { Code = (int)ErrorCode.ERR_IllegalStatement });
        }
    }
}
