// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    /// <summary>
    /// Test binding of the target-typed conditional (aka ternary) operator.
    /// </summary>
    public class TargetTypedConditionalOperatorTests : CSharpTestBase
    {
        // PROTOTYPE(ngafter): Testing for target-typed semantics is needed.

        /*
        /// <summary>
        /// Both branches have the same type, so no conversion is necessary.
        /// </summary>
        [Fact]
        public void TestSameType()
        {
            TestConditional("true ? 1 : 2", targetType: "System.Int32");
            TestConditional("false ? 'a' : 'b'", targetType: "System.Char");
            TestConditional("true ? 1.5 : GetDouble()", targetType: "System.Double");
            TestConditional("false ? GetObject() : GetObject()", targetType: "System.Object");
            TestConditional("true ? GetUserGeneric<T>() : GetUserGeneric<T>()", targetType: "D<T>");
            TestConditional("false ? GetTypeParameter<T>() : GetTypeParameter<T>()", targetType: "T");
        }

        /// <summary>
        /// Both branches have types and exactly one expression is convertible to the type of the other.
        /// </summary>
        [Fact]
        public void TestOneConversion()
        {
            TestConditional("true ? GetShort() : GetInt()", targetType: "System.Int32");
            TestConditional("false ? \"string\" : GetObject()", targetType: "System.Object");
            TestConditional("true ? GetVariantInterface<string, int>() : GetVariantInterface<object, int>()", targetType: "I<System.String, System.Int32>");
            TestConditional("false ? GetVariantInterface<int, object>() : GetVariantInterface<int, string>()", targetType: "I<System.Int32, System.Object>");
        }

        /// <summary>
        /// Both branches have types and both expression are convertible to the type of the other.
        /// The wider type is preferred.
        /// </summary>
        /// <remarks>
        /// Cases where both conversions are possible and neither is preferred as the
        /// wider of the two are possible only in the presence of user-defined implicit
        /// conversions.  Such cases are tested separately.  
        /// See SemanticErrorTests.CS0172ERR_AmbigQM.
        /// </remarks>
        [Fact]
        public void TestAmbiguousPreferWider()
        {
            TestConditional("true ? 1 : (short)2", targetType: "System.Int32");
            TestConditional("false ? (float)2 : 1", targetType: "System.Single");
            TestConditional("true ? 1.5d : (double)2", targetType: "System.Double");
        }

        /// <summary>
        /// Both branches have types but neither expression is convertible to the type
        /// of the other.
        /// </summary>
        [Fact]
        public void TestNoConversion()
        {
            TestConditional("true ? T : U", null,
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type"),
                Diagnostic(ErrorCode.ERR_BadSKunknown, "U").WithArguments("U", "type"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? T : U").WithArguments("T", "U"));
            TestConditional("false ? T : 1", null,
                Diagnostic(ErrorCode.ERR_BadSKunknown, "T").WithArguments("T", "type"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? T : 1").WithArguments("T", "int"));
            TestConditional("true ? GetUserGeneric<char>() : GetUserNonGeneric()", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetUserGeneric<char>() : GetUserNonGeneric()").WithArguments("D<char>", "C"));
        }

        /// <summary>
        /// Exactly one branch has a type and the other expression is convertible to that type.
        /// </summary>
        [Fact]
        public void TestOneUntypedSuccess()
        {
            TestConditional("true ? GetObject() : null", targetType: "System.Object"); //null literal
            TestConditional("false ? GetString : (System.Func<string>)null", targetType: "System.Func<System.String>"); //method group
            TestConditional("true ? (System.Func<int, int>)null : x => x", targetType: "System.Func<System.Int32, System.Int32>"); //lambda
        }

        /// <summary>
        /// Exactly one branch has a type but the other expression is not convertible to that type.
        /// </summary>
        [Fact]
        public void TestOneUntypedFailure()
        {
            TestConditional("true ? GetInt() : null", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetInt() : null").WithArguments("int", "<null>"));
            TestConditional("false ? GetString : (System.Func<int>)null", null, TestOptions.WithoutImprovedOverloadCandidates,
                Diagnostic(ErrorCode.ERR_BadRetType, "GetString").WithArguments("C.GetString()", "string"));
            TestConditional("false ? GetString : (System.Func<int>)null", null,
                // (6,13): error CS0173: Type of conditional expression cannot be determined because there is no implicit conversion between 'method group' and 'Func<int>'
                //         _ = false ? GetString : (System.Func<int>)null;
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? GetString : (System.Func<int>)null").WithArguments("method group", "System.Func<int>").WithLocation(6, 13));
            TestConditional("true ? (System.Func<int, short>)null : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? (System.Func<int, short>)null : x => x").WithArguments("System.Func<int, short>", "lambda expression"));
        }

        [Fact]
        public void TestBothUntyped()
        {
            TestConditional("true ? null : null", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? null : null").WithArguments("<null>", "<null>"));
            TestConditional("false ? null : GetInt", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? null : GetInt").WithArguments("<null>", "method group"));
            TestConditional("true ? null : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? null : x => x").WithArguments("<null>", "lambda expression"));

            TestConditional("false ? GetInt : GetInt", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? GetInt : GetInt").WithArguments("method group", "method group"));
            TestConditional("true ? GetInt : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetInt : x => x").WithArguments("method group", "lambda expression"));

            TestConditional("false ? x => x : x => x", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "false ? x => x : x => x").WithArguments("lambda expression", "lambda expression"));
        }

        [Fact]
        public void TestFunCall()
        {
            TestConditional("true ? GetVoid() : GetInt()", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true ? GetVoid() : GetInt()").WithArguments("void", "int"));
            TestConditional("GetVoid() ? 1 : 2", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetVoid()").WithArguments("void", "bool"));
            TestConditional("GetInt() ? 1 : 2", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetInt()").WithArguments("int", "bool"));
            TestConditional("GetBool() ? 1 : 2", "System.Int32");
        }

        [Fact]
        public void TestEmptyExpression()
        {
            TestConditional("true ?  : GetInt()", null,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"));
            TestConditional("true ? GetInt() :  ", null,
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";"));
        }

        [Fact]
        public void TestEnum()
        {
            TestConditional("true? 0 : color.Blue", "color");
            TestConditional("true? 5 : color.Blue", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true? 5 : color.Blue").WithArguments("int", "color"));
            TestConditional("true? null : color.Blue", null,
                Diagnostic(ErrorCode.ERR_InvalidQM, "true? null : color.Blue").WithArguments("<null>", "color"));
        }

        [Fact]
        public void TestAs()
        {
            TestConditional(@"(1 < 2) ? ""MyString"" as string : "" """, "System.String");
            TestConditional(@"(1 > 2) ? "" "" : ""MyString"" as string", "System.String");
        }

        [Fact]
        public void TestGeneric()
        {
            TestConditional(@"GetUserNonGeneric()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUserNonGeneric()").WithArguments("C", "bool"));
            TestConditional(@"GetUserGeneric<T>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetUserGeneric<T>()").WithArguments("D<T>", "bool"));
            TestConditional(@"GetTypeParameter<T>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetTypeParameter<T>()").WithArguments("T", "bool"));
            TestConditional(@"GetVariantInterface<T, U>()? 1 : 2", null, Diagnostic(ErrorCode.ERR_NoImplicitConv, "GetVariantInterface<T, U>()").WithArguments("I<T, U>", "bool"));
        }

        [Fact]
        public void TestInvalidCondition()
        {
            // CONSIDER: dev10 reports ERR_ConstOutOfRange
            TestConditional("1 ? 2 : 3", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool"));

            TestConditional("goo ? 'a' : 'b'", null,
                Diagnostic(ErrorCode.ERR_NameNotInContext, "goo").WithArguments("goo"));

            TestConditional("new Goo() ? GetObject() : null", null,
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Goo").WithArguments("Goo"));

            // CONSIDER: dev10 reports ERR_ConstOutOfRange
            TestConditional("1 ? null : null", null,
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "bool"),
                Diagnostic(ErrorCode.ERR_InvalidQM, "1 ? null : null").WithArguments("<null>", "<null>"));
        }

        private static void TestConditional(string conditionalExpression, string targetType, string naturalType, params DiagnosticDescription[] expectedDiagnostics)
        {
            TestConditional(conditionalExpression, targetType, naturalType, null, expectedDiagnostics);
        }

        private static void TestConditional(string conditionalExpression, string targetType, string? naturalType, CSharpParseOptions? parseOptions, params DiagnosticDescription[] expectedDiagnostics)
        {
            string source = $@"
class C
{{
    void Test<T, U>(bool b)
    {{
        {targetType} t = {conditionalExpression};
        Use(t);
    }}

    int GetInt() {{ return 1; }}
    void GetVoid() {{ return ; }}
    bool GetBool() {{ return true; }}
    short GetShort() {{ return 1; }}
    char GetChar() {{ return 'a'; }}
    double GetDouble() {{ return 1.5; }}
    string GetString() {{ return ""hello""; }}
    object GetObject() {{ return new object(); }}
    C GetUserNonGeneric() {{ return new C(); }}
    D<T> GetUserGeneric<T>() {{ return new D<T>(); }}
    T GetTypeParameter<T>() {{ return default(T); }}
    I<T, U> GetVariantInterface<T, U>() {{ return null; }}
    void Use(object t) {{ }}
}}

class D<T> {{ }}
public enum color {{ Red, Blue, Green }};
interface I<in T, out U> {{ }}";

            parseOptions ??= TestOptions.Regular;
            parseOptions = parseOptions.WithLanguageVersion(MessageID.IDS_FeatureTargetTypedConditional.RequiredVersion());
            var tree = Parse(source, options: parseOptions);

            var comp = CreateCompilation(tree);
            comp.VerifyDiagnostics(expectedDiagnostics);

            var compUnit = tree.GetCompilationUnitRoot();
            var classC = (TypeDeclarationSyntax)compUnit.Members.First();
            var methodTest = (MethodDeclarationSyntax)classC.Members.First();
            var stmt = (ExpressionStatementSyntax)methodTest.Body!.Statements.First();
            var assignment = (AssignmentExpressionSyntax)stmt.Expression;
            var conditionalExpr = (ConditionalExpressionSyntax)assignment.Right;

            var model = comp.GetSemanticModel(tree);

            TODO("BELOW HERE NEEDS TO BE WRITTEN.");

            if (targetType != null)
            {
                Assert.Equal(targetType, model.GetTypeInfo(conditionalExpr).Type.ToTestDisplayString());

                if (!expectedDiagnostics.Any())
                {
                    Assert.Equal(SpecialType.System_Boolean, model.GetTypeInfo(conditionalExpr.Condition).Type!.SpecialType);
                    Assert.Equal(targetType, model.GetTypeInfo(conditionalExpr.WhenTrue).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
                    Assert.Equal(targetType, model.GetTypeInfo(conditionalExpr.WhenFalse).ConvertedType.ToTestDisplayString()); //in parent to catch conversion
                }
            }
        }
        */
    }
}
