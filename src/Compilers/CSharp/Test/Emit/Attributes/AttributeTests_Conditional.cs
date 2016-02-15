// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_Conditional : WellKnownAttributesTestBase
    {
        #region Conditional Attribute Type tests

        #region Common Helpers

        private static readonly string s_commonTestSource_ConditionalAttrDefs = @"
using System;
using System.Diagnostics;

// Applied conditional attribute

[Conditional(""cond1"")]
public class PreservedAppliedAttribute : Attribute { }

[Conditional(""cond2"")]
public class OmittedAppliedAttribute : Attribute { }


// Inherited conditional attribute

[Conditional(""cond3"")]
public class BasePreservedInheritedAttribute : Attribute { }
[Conditional(""cond4"")]
public class PreservedInheritedAttribute : BasePreservedInheritedAttribute { }

[Conditional(""cond5"")]
public class BaseOmittedInheritedAttribute : Attribute { }
public class OmittedInheritedAttribute : BaseOmittedInheritedAttribute { }


// Multiple conditional attributes

[Conditional(""cond6""), Conditional(""cond7"")]
public class BasePreservedMultipleAttribute : Attribute { }
[Conditional(""cond8"")]
public class PreservedMultipleAttribute : BasePreservedMultipleAttribute { }

[Conditional(""cond9"")]
public class BaseOmittedMultipleAttribute : Attribute { }
[Conditional(""cond10""), Conditional(""cond11"")]
public class OmittedMultipleAttribute : BaseOmittedMultipleAttribute { }
";

        private static readonly string s_commonTestSource_ConditionalAttributesApplied = @"
[PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
public class Z<[PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute] T>
{
    [return: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public void m([PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]int param1) { }

    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public int f;

    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public int p1
    {
        [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        [return: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        get;
        
        [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        [param: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        set;
    }

    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public int p2
    {
        [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        [return: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        get { return 1; }
    }

    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public int p3
    {
        [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        [return: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        get { return 1; }

        [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        [param: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
        set { }
    }

    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    [field: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    [method: PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    public event Action e;
}

[PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
public enum E
{
    [PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
    A = 1
}

[PreservedAppliedAttribute, OmittedAppliedAttribute, PreservedInheritedAttribute, OmittedInheritedAttribute, PreservedMultipleAttribute, OmittedMultipleAttribute]
public struct S { }

public class Test
{
    public static void Main() {}
}
";
        private void CommonSourceValidatorForCondAttrType(ModuleSymbol module)
        {
            CommonValidatorForCondAttrType(module, isFromSource: true);
        }

        private void CommonMetadataValidatorForCondAttrType(ModuleSymbol module)
        {
            CommonValidatorForCondAttrType(module, isFromSource: false);
        }

        private void CommonValidatorForCondAttrType(ModuleSymbol module, bool isFromSource)
        {
            var attributesArrayBuilder = new List<ImmutableArray<CSharpAttributeData>>();

            var classZ = module.GlobalNamespace.GetTypeMember("Z");
            attributesArrayBuilder.Add(classZ.GetAttributes());
            attributesArrayBuilder.Add(classZ.TypeParameters[0].GetAttributes());

            var methodM = classZ.GetMember<MethodSymbol>("m");
            attributesArrayBuilder.Add(methodM.GetAttributes());
            attributesArrayBuilder.Add(methodM.GetReturnTypeAttributes());
            var param1 = methodM.Parameters[0];
            attributesArrayBuilder.Add(param1.GetAttributes());

            var fieldF = classZ.GetMember<FieldSymbol>("f");
            attributesArrayBuilder.Add(fieldF.GetAttributes());

            var propP1 = classZ.GetMember<PropertySymbol>("p1");
            attributesArrayBuilder.Add(propP1.GetAttributes());
            var propGetMethod = propP1.GetMethod;
            attributesArrayBuilder.Add(propGetMethod.GetAttributes());
            attributesArrayBuilder.Add(propGetMethod.GetReturnTypeAttributes());
            var propSetMethod = propP1.SetMethod;
            attributesArrayBuilder.Add(propSetMethod.GetAttributes());
            attributesArrayBuilder.Add(propSetMethod.Parameters[0].GetAttributes());

            var propP2 = classZ.GetMember<PropertySymbol>("p2");
            attributesArrayBuilder.Add(propP2.GetAttributes());
            propGetMethod = propP2.GetMethod;
            attributesArrayBuilder.Add(propGetMethod.GetAttributes());
            attributesArrayBuilder.Add(propGetMethod.GetReturnTypeAttributes());

            var propP3 = classZ.GetMember<PropertySymbol>("p3");
            attributesArrayBuilder.Add(propP3.GetAttributes());
            propGetMethod = propP3.GetMethod;
            attributesArrayBuilder.Add(propGetMethod.GetAttributes());
            attributesArrayBuilder.Add(propGetMethod.GetReturnTypeAttributes());
            propSetMethod = propP3.SetMethod;
            attributesArrayBuilder.Add(propSetMethod.GetAttributes());
            attributesArrayBuilder.Add(propSetMethod.Parameters[0].GetAttributes());

            var eventE = classZ.GetMember<EventSymbol>("e");
            attributesArrayBuilder.Add(eventE.GetAttributes());
            attributesArrayBuilder.Add(eventE.AddMethod.GetAttributes());
            attributesArrayBuilder.Add(eventE.RemoveMethod.GetAttributes());
            if (isFromSource)
            {
                attributesArrayBuilder.Add(eventE.AssociatedField.GetAttributes());
            }

            var enumE = module.GlobalNamespace.GetTypeMember("E");
            attributesArrayBuilder.Add(enumE.GetAttributes());

            var fieldA = enumE.GetMember<FieldSymbol>("A");
            attributesArrayBuilder.Add(fieldA.GetAttributes());

            var structS = module.GlobalNamespace.GetTypeMember("S");
            attributesArrayBuilder.Add(structS.GetAttributes());

            foreach (var attributes in attributesArrayBuilder)
            {
                // PreservedAppliedAttribute and OmittedAppliedAttribute have applied conditional attributes, such that
                // (a) PreservedAppliedAttribute is conditionally applied to symbols
                // (b) OmittedAppliedAttribute is conditionally NOT applied to symbols

                // PreservedInheritedAttribute and OmittedInheritedAttribute have inherited conditional attributes, such that
                // (a) PreservedInheritedAttribute is conditionally applied to symbols
                // (b) OmittedInheritedAttribute is conditionally NOT applied to symbols

                // PreservedMultipleAttribute and OmittedMultipleAttribute have multiple applied/inherited conditional attributes, such that
                // (a) PreservedMultipleAttribute is conditionally applied to symbols
                // (b) OmittedMultipleAttribute is conditionally NOT applied to symbols

                var actualAttributeNames = attributes.
                    Where(a => a.AttributeClass.Name != "CompilerGeneratedAttribute").
                    Select(a => a.AttributeClass.Name);

                if (isFromSource)
                {
                    // All attributes should be present for source symbols
                    AssertEx.SetEqual(
                        new[]
                        {   "PreservedAppliedAttribute",
                            "OmittedAppliedAttribute",
                            "PreservedInheritedAttribute",
                            "OmittedInheritedAttribute",
                            "PreservedMultipleAttribute",
                            "OmittedMultipleAttribute",
                        },
                        actualAttributeNames);
                }
                else
                {
                    // Only PreservedAppliedAttribute, PreservedInheritedAttribute, PreservedMultipleAttribute should be emitted in metadata
                    AssertEx.SetEqual(
                        new[]
                        {
                            "PreservedAppliedAttribute",
                            "PreservedInheritedAttribute",
                            "PreservedMultipleAttribute",
                        },
                        actualAttributeNames);
                }
            }
        }

        private void TestConditionAttributeType_SameSource(string condDefs)
        {
            // Same source file
            string testSource = condDefs + s_commonTestSource_ConditionalAttrDefs + s_commonTestSource_ConditionalAttributesApplied;
            CompileAndVerify(testSource, sourceSymbolValidator: CommonSourceValidatorForCondAttrType, symbolValidator: CommonMetadataValidatorForCondAttrType, expectedOutput: "");

            // Scenario to test Conditional directive stack creation during SyntaxTree.Create, see Devdiv Bug #13846 for details.
            CompilationUnitSyntax root = SyntaxFactory.ParseCompilationUnit(testSource);
            var syntaxTree = SyntaxFactory.SyntaxTree(root);
            var compilation = CreateCompilationWithMscorlib(syntaxTree, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, sourceSymbolValidator: CommonSourceValidatorForCondAttrType, symbolValidator: CommonMetadataValidatorForCondAttrType, expectedOutput: "");
        }

        private void TestConditionAttributeType_DifferentSource(string condDefsSrcFile1, string condDefsSrcFile2)
        {
            string source1 = condDefsSrcFile1 + s_commonTestSource_ConditionalAttrDefs;
            string source2 = condDefsSrcFile2 + @"
using System;
" + s_commonTestSource_ConditionalAttributesApplied;

            // Different source files, same compilation
            var testSources = new[] { source1, source2 };
            CompileAndVerify(testSources, sourceSymbolValidator: CommonSourceValidatorForCondAttrType, symbolValidator: CommonMetadataValidatorForCondAttrType, expectedOutput: "");

            // Different source files, different compilation
            var comp1 = CreateCompilationWithMscorlib(source1);
            CompileAndVerify(source2, additionalRefs: new[] { comp1.ToMetadataReference() }, sourceSymbolValidator: CommonSourceValidatorForCondAttrType, symbolValidator: CommonMetadataValidatorForCondAttrType, expectedOutput: "");
        }

        #endregion

        #region Tests

        [Fact]
        public void TestConditionAttributeType_01()
        {
            string conditionalDefs = @"
#define cond1
#define cond3
#define cond8
";
            TestConditionAttributeType_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond2
#define cond5
#define cond7
";
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact]
        public void TestConditionAttributeType_02()
        {
            string conditionalDefs = @"
#define cond1
#define cond4
#define cond6
#define cond7
#define cond8
";

            TestConditionAttributeType_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond2
#define cond5
#undef cond1
#undef cond3
#undef cond4
#define cond9
#define cond11
";
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact]
        public void TestConditionAttributeType_03()
        {
            string conditionalDefs = @"
#define cond1
#define cond3
#define cond4
#define cond7
#define cond8
";

            TestConditionAttributeType_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond1
#define cond2
#define cond3
#define cond4
#define cond5
";
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact]
        public void TestConditionAttributeType_04()
        {
            string conditionalDefs = @"
#define cond1
#define cond2
#undef cond1
#undef cond3
#define cond1
#define cond3
#undef cond2
#define cond4
#undef cond3
#undef cond5
#define cond6
#undef cond7
#define cond8
#undef cond6
";
            TestConditionAttributeType_SameSource(conditionalDefs);
            TestConditionAttributeType_DifferentSource(String.Empty, conditionalDefs);
        }

        [Fact]
        public void TestConditionAttributeType_05()
        {
            string conditionalDefs = @"
#if cond
#define cond2
#define cond5
#define cond7
#endif

#define cond1
#define cond3
#define cond8

#if cond2
#undef cond1
#undef cond3
#undef cond8
#endif
";
            TestConditionAttributeType_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond2
#define cond5
#define cond7
";
            TestConditionAttributeType_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        #endregion

        #endregion

        #region Conditional Method tests

        #region Common Helpers

        private static readonly string s_commonTestSource_ConditionalMethodDefs = @"
using System;
using System.Diagnostics;

public class BaseZ
{
    [Conditional(""cond3"")]
    public virtual void PreservedCalls_InheritedConditional_Method() { System.Console.WriteLine(""BaseZ.PreservedCalls_InheritedConditional_Method""); }

    [Conditional(""cond4"")]
    public virtual void OmittedCalls_InheritedConditional_Method() { System.Console.WriteLine(""BaseZ.OmittedCalls_InheritedConditional_Method""); }
}

public class Z: BaseZ
{
    [Conditional(""cond1"")]
    public void PreservedCalls_AppliedConditional_Method() { System.Console.WriteLine(""Z.PreservedCalls_AppliedConditional_Method""); }

    [Conditional(""cond2"")]
    public void OmittedCalls_AppliedConditional_Method() { System.Console.WriteLine(""Z.OmittedCalls_AppliedConditional_Method""); }

    public override void PreservedCalls_InheritedConditional_Method() { System.Console.WriteLine(""Z.PreservedCalls_InheritedConditional_Method""); }

    public override void OmittedCalls_InheritedConditional_Method() { System.Console.WriteLine(""Z.OmittedCalls_InheritedConditional_Method""); }

    [Conditional(""cond5""), Conditional(""cond6"")]
    public void PreservedCalls_MultipleConditional_Method() { System.Console.WriteLine(""Z.PreservedCalls_MultipleConditional_Method""); }

    [Conditional(""cond7""), Conditional(""cond8"")]
    public void OmittedCalls_MultipleConditional_Method() { System.Console.WriteLine(""Z.OmittedCalls_MultipleConditional_Method""); }
}";

        private static readonly string s_commonTestSource_ConditionalMethodCalls = @"
public class Test
{
    public static void Main()
    {
        var z = new Z();
        z.PreservedCalls_AppliedConditional_Method();
        z.OmittedCalls_AppliedConditional_Method();
        z.PreservedCalls_InheritedConditional_Method();
        z.OmittedCalls_InheritedConditional_Method();
        z.PreservedCalls_MultipleConditional_Method();
        z.OmittedCalls_MultipleConditional_Method();
    }
}
";
        private static readonly string s_commonExpectedOutput_ConditionalMethodsTest = @"Z.PreservedCalls_AppliedConditional_Method
Z.PreservedCalls_InheritedConditional_Method
Z.PreservedCalls_MultipleConditional_Method";

        private void TestConditionMethods_SameSource(string condDefs)
        {
            // Same source file
            string testSource = condDefs + s_commonTestSource_ConditionalMethodDefs + s_commonTestSource_ConditionalMethodCalls;
            CompileAndVerify(testSource, expectedOutput: s_commonExpectedOutput_ConditionalMethodsTest);

            // Scenario to test Conditional directive stack creation during SyntaxTree.Create, see Devdiv Bug #13846 for details.
            CompilationUnitSyntax root = SyntaxFactory.ParseCompilationUnit(testSource);
            var syntaxTree = SyntaxFactory.SyntaxTree(root);
            var compilation = CreateCompilationWithMscorlib(syntaxTree, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: s_commonExpectedOutput_ConditionalMethodsTest);
        }

        private void TestConditionMethods_DifferentSource(string condDefsSrcFile1, string condDefsSrcFile2)
        {
            string source1 = condDefsSrcFile1 + s_commonTestSource_ConditionalMethodDefs;
            string source2 = condDefsSrcFile2 + @"
using System;
" + s_commonTestSource_ConditionalMethodCalls;

            // Different source files, same compilation
            var testSources = new[] { source1, source2 };
            CompileAndVerify(testSources, expectedOutput: s_commonExpectedOutput_ConditionalMethodsTest);

            // Different source files, different compilation
            var comp1 = CreateCompilationWithMscorlib(source1, assemblyName: Guid.NewGuid().ToString());
            CompileAndVerify(source2, additionalRefs: new[] { comp1.ToMetadataReference() }, expectedOutput: s_commonExpectedOutput_ConditionalMethodsTest);
        }

        #endregion

        #region Tests

        [Fact]
        public void TestConditionMethods_01()
        {
            string conditionalDefs = @"
#define cond1
#define cond3
#define cond5
";
            TestConditionMethods_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond2
#define cond4
#define cond7
";
            TestConditionMethods_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact]
        public void TestConditionMethods_02()
        {
            string conditionalDefs = @"
#define cond1
#define cond3
#define cond5
#define cond6
";

            TestConditionMethods_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond2
#define cond5
#undef cond1
#undef cond3
#define cond8
";
            TestConditionMethods_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact]
        public void TestConditionMethods_03()
        {
            string conditionalDefs = @"
#define cond1
#define cond3
#define cond5
#define cond7
#define cond6
#undef cond5
#undef cond7
";

            TestConditionMethods_SameSource(conditionalDefs);

            string conditionalDefsDummy = @"
#define cond1
#define cond2
#define cond3
#define cond4
#define cond5
#define cond6
#define cond7
#define cond8
";
            TestConditionMethods_DifferentSource(conditionalDefsDummy, conditionalDefs);
        }

        [Fact, WorkItem(529683, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529683")]
        public void CondMethodInDelegateCreationExpr()
        {
            var compilation = CreateCompilationWithMscorlib(@"
using System.Diagnostics;

class Test
{
    [Conditional(""DEBUG"")]
    public virtual void Conditional()
    {
    }
}

class T1 : Test
{
    public override void Conditional()
    {
    }
}

delegate void D1();

class T5
{
    static void Main()
    {
        T1 t1 = new T1();

        D1 d1 = new D1(t1.Conditional);
    }
}
");
            compilation.VerifyDiagnostics(
                //  (27,24): error CS1618: Cannot create delegate with 'T1.Conditional()' because it has a Conditional attribute
                //         t1.Conditional
                Diagnostic(ErrorCode.ERR_DelegateOnConditional, "t1.Conditional").WithArguments("T1.Conditional()"));
        }

        #endregion

        #endregion

        #region Miscellaneous tests

        [Fact]
        public void ConditionalAttributeArgument_ValidConstantMember()
        {
            string source = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

[Conditional(Foo.M)]
[Conditional(Bar.M)]
public class Foo: Attribute
{
    public const string M = Bar.M;
    public Foo([Optional][Foo]int y) {}
    public static void Main() { var unused = new Foo(); }
}

class Bar
{
    public const string M = ""str"";
}
";
            Func<bool, Action<ModuleSymbol>> validator = isFromSource => module =>
            {
                var globalNamespace = module.GlobalNamespace;

                var classFoo = globalNamespace.GetMember<NamedTypeSymbol>("Foo");
                Assert.True(classFoo.IsConditional);

                var fooCtor = classFoo.InstanceConstructors.First();
                Assert.Equal(1, fooCtor.ParameterCount);

                var paramY = fooCtor.Parameters[0];
                Assert.True(paramY.IsOptional);
                var attributes = paramY.GetAttributes();
                if (isFromSource)
                {
                    Assert.Equal(2, attributes.Length);
                }
                else
                {
                    Assert.Equal(0, attributes.Length);
                }
            };

            CompileAndVerify(source, symbolValidator: validator(false), sourceSymbolValidator: validator(true), expectedOutput: "");
        }

        [Fact]
        public void ConditionalAttributeArgument_InvalidMember()
        {
            string source = @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

[Conditional(Foo.M)]
[Conditional(Foo.M())]
[Conditional(Bar.M)]
[Conditional(Bar.M())]
public class Foo: Attribute
{
    public const string M = Bar.M;
    public Foo([Optional][Foo]int y) {}
    public static void Main() { var unused = new Foo(); }
}

class Bar
{
    public static string M() { return ""str""; }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (12,33): error CS0428: Cannot convert method group 'M' to non-delegate type 'string'. Did you intend to invoke the method?
                //     public const string M = Bar.M;
                Diagnostic(ErrorCode.ERR_MethGrpToNonDel, "M").WithArguments("M", "string"),
                // (7,18): error CS1955: Non-invocable member 'Foo.M' cannot be used like a method.
                // [Conditional(Foo.M())]
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "M").WithArguments("Foo.M"),
                // (8,14): error CS1503: Argument 1: cannot convert from 'method group' to 'string'
                // [Conditional(Bar.M)]
                Diagnostic(ErrorCode.ERR_BadArgType, "Bar.M").WithArguments("1", "method group", "string"),
                // (9,14): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [Conditional(Bar.M())]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "Bar.M()"),
                // (6,14): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                // [Conditional(Foo.M)]
                Diagnostic(ErrorCode.ERR_BadAttributeArgument, "Foo.M"));
        }

        #endregion
    }
}
