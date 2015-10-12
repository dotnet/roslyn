// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public partial class CodeGenerationTests
    {
        public class CSharp
        {
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddNamespace()
            {
                var input = "namespace [|N1|] { }";
                var expected = "namespace N1 { namespace N2 { } }";
                TestAddNamespace(input, expected,
                    name: "N2");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int F; }";
                TestAddField(input, expected,
                    type: GetTypeSymbol(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStaticField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { private static string F; }";
                TestAddField(input, expected,
                    type: GetTypeSymbol(typeof(string)),
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddArrayField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int[] F; }";
                TestAddField(input, expected,
                    type: CreateArrayType(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsafeField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe int F; }";
                TestAddField(input, expected,
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    type: GetTypeSymbol(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddFieldToCompilationUnit()
            {
                var input = "";
                var expected = "public int F;";
                TestAddField(input, expected,
                    type: GetTypeSymbol(typeof(int)), addToCompilationUnit: true);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructor()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public C() { } }";
                TestAddConstructor(input, expected);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructorWithoutBody()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public C(); }";
                TestAddConstructor(input, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructorResolveNamespaceImport()
            {
                var input = "class [|C|] { }";
                var expected = "using System; class C { public C(DateTime dt, int i) { } }";
                TestAddConstructor(input, expected,
                    parameters: Parameters(Parameter(typeof(DateTime), "dt"), Parameter(typeof(int), "i")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddChainedConstructor()
            {
                var input = "class [|C|] { public C(int i) { } }";
                var expected = "class C { public C() : this(42) { } public C(int i) { } }";
                TestAddConstructor(input, expected,
                    thisArguments: new[] { CS.SyntaxFactory.ParseExpression("42") });
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStaticConstructor()
            {
                var input = "class [|C|] { }";
                var expected = "class C { static C() { } }";
                TestAddConstructor(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544082)]
            public void AddClass()
            {
                var input = "namespace [|N|] { }";
                var expected = @"namespace N
{
    public class C
    {
    }
}";
                TestAddNamedType(input, expected,
                    compareTokens: false);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddClassEscapeName()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public class @class { } }";
                TestAddNamedType(input, expected,
                    name: "class");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddClassUnicodeName()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public class classæøå { } }";
                TestAddNamedType(input, expected,
                    name: "cl\u0061ssæøå");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public void AddStaticClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public static class C { } }";
                TestAddNamedType(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public void AddSealedClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { private sealed class C { } }";
                TestAddNamedType(input, expected,
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isSealed: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public void AddAbstractClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { protected internal abstract class C { } }";
                TestAddNamedType(input, expected,
                    accessibility: Accessibility.ProtectedOrInternal,
                    modifiers: new DeclarationModifiers(isAbstract: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStruct()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { internal struct S { } }";
                TestAddNamedType(input, expected,
                    name: "S",
                    accessibility: Accessibility.Internal,
                    typeKind: TypeKind.Struct);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public void AddSealedStruct()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public struct S { } }";
                TestAddNamedType(input, expected,
                    name: "S",
                    modifiers: new DeclarationModifiers(isSealed: true),
                    accessibility: Accessibility.Public,
                    typeKind: TypeKind.Struct);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddInterface()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public interface I { } }";
                TestAddNamedType(input, expected,
                    name: "I",
                    typeKind: TypeKind.Interface);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544080)]
            public void AddEnum()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public enum E { } }";
                TestAddNamedType(input, expected, "E",
                    typeKind: TypeKind.Enum);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544527)]
            public void AddEnumWithValues()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public enum E { F1 = 1, F2 = 2 } }";
                TestAddNamedType(input, expected, "E",
                    typeKind: TypeKind.Enum,
                    members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544080)]
            public void AddDelegateType()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public delegate int D(string s); }";
                TestAddDelegateType(input, expected,
                    returnType: typeof(int),
                    parameters: Parameters(Parameter(typeof(string), "s")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public void AddSealedDelegateType()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public delegate int D(string s); }";
                TestAddDelegateType(input, expected,
                    returnType: typeof(int),
                    parameters: Parameters(Parameter(typeof(string), "s")),
                    modifiers: new DeclarationModifiers(isSealed: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddEvent()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public event System.Action E; }";
                TestAddEvent(input, expected,
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsafeEvent()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe event System.Action E; }";
                TestAddEvent(input, expected,
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddEventWithAccessors()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public event System.Action E { add { } remove { } } }";
                TestAddEvent(input, expected,
                    addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(), Accessibility.NotApplicable, SpecializedCollections.EmptyList<SyntaxNode>()),
                    removeMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(), Accessibility.NotApplicable, SpecializedCollections.EmptyList<SyntaxNode>()),
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodToClass()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public void M() { } }";
                TestAddMethod(input, expected,
                    returnType: typeof(void));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodToClassEscapedName()
            {
                var input = "class [|C|] { }";
                var expected = "using System; class C { public DateTime @static() { } }";
                TestAddMethod(input, expected,
                    name: "static",
                    returnType: typeof(DateTime));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStaticMethodToStruct()
            {
                var input = "struct [|S|] { }";
                var expected = "struct S { public static int M() { $$ } }";
                TestAddMethod(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddSealedOverrideMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public sealed override int GetHashCode() { $$ } }";
                TestAddMethod(input, expected,
                    name: "GetHashCode",
                    modifiers: new DeclarationModifiers(isOverride: true, isSealed: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAbstractMethod()
            {
                var input = "abstract class [|C|] { }";
                var expected = "abstract class C { public abstract int M(); }";
                TestAddMethod(input, expected,
                    modifiers: new DeclarationModifiers(isAbstract: true),
                    returnType: typeof(int));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodWithoutBody()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int M(); }";
                TestAddMethod(input, expected,
                    returnType: typeof(int),
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddGenericMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int M<T>() { $$ } }";
                TestAddMethod(input, expected,
                    returnType: typeof(int),
                    typeParameters: new[] { CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T") },
                    statements: "return new T().GetHashCode();");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddVirtualMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { protected virtual int M() { $$ } }";
                TestAddMethod(input, expected,
                    accessibility: Accessibility.Protected,
                    modifiers: new DeclarationModifiers(isVirtual: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsafeNewMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe new string ToString() { $$ } }";
                TestAddMethod(input, expected,
                    name: "ToString",
                    modifiers: new DeclarationModifiers(isNew: true, isUnsafe: true),
                    returnType: typeof(string),
                    statements: "return String.Empty;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddExplicitImplementationOfUnsafeMethod()
            {
                var input = "interface I { unsafe void M(int i); } class [|C|] : I { }";
                var expected = "interface I { unsafe void M(int i); } class C : I { unsafe void I.M(int i) { } }";
                TestAddMethod(input, expected,
                    name: "M",
                    returnType: typeof(void),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    explicitInterface: s => s.LookupSymbols(input.IndexOf('M'), null, "M").First() as IMethodSymbol);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddExplicitImplementation()
            {
                var input = "interface I { void M(int i); } class [|C|] : I { }";
                var expected = "interface I { void M(int i); } class C : I { void I.M(int i) { } }";
                TestAddMethod(input, expected,
                    name: "M",
                    returnType: typeof(void),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    explicitInterface: s => s.LookupSymbols(input.IndexOf('M'), null, "M").First() as IMethodSymbol);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddTrueFalseOperators()
            {
                var input = @"
class [|C|]
{
}";
                var expected = @"
class C
{
    public static bool operator true (C other) { $$ }
    public static bool operator false (C other) { $$ }
}
";
                TestAddOperators(input, expected,
                    new[] { CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False },
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(bool),
                    statements: "return false;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnaryOperators()
            {
                var input = @"
class [|C|]
{
}";
                var expected = @"
class C
{
    public static object operator + (C other) { $$ }
    public static object operator - (C other) { $$ }
    public static object operator ! (C other) { $$ }
    public static object operator ~ (C other) { $$ }
    public static object operator ++ (C other) { $$ }
    public static object operator -- (C other) { $$ }
}";
                TestAddOperators(input, expected,
                    new[]
                    {
                        CodeGenerationOperatorKind.UnaryPlus,
                        CodeGenerationOperatorKind.UnaryNegation,
                        CodeGenerationOperatorKind.LogicalNot,
                        CodeGenerationOperatorKind.OnesComplement,
                        CodeGenerationOperatorKind.Increment,
                        CodeGenerationOperatorKind.Decrement
                    },
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(object),
                    statements: "return null;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddBinaryOperators()
            {
                var input = @"
class [|C|]
{
}";
                var expected = @"
class C
{
    public static object operator + (C a, C b) { $$ }
    public static object operator - (C a, C b) { $$ }
    public static object operator * (C a, C b) { $$ }
    public static object operator / (C a, C b) { $$ }
    public static object operator % (C a, C b) { $$ }
    public static object operator & (C a, C b) { $$ }
    public static object operator | (C a, C b) { $$ }
    public static object operator ^ (C a, C b) { $$ }
    public static object operator << (C a, C b) { $$ }
    public static object operator >> (C a, C b) { $$ }
}";
                TestAddOperators(input, expected,
                    new[]
                    {
                        CodeGenerationOperatorKind.Addition,
                        CodeGenerationOperatorKind.Subtraction,
                        CodeGenerationOperatorKind.Multiplication,
                        CodeGenerationOperatorKind.Division,
                        CodeGenerationOperatorKind.Modulus,
                        CodeGenerationOperatorKind.BitwiseAnd,
                        CodeGenerationOperatorKind.BitwiseOr,
                        CodeGenerationOperatorKind.ExclusiveOr,
                        CodeGenerationOperatorKind.LeftShift,
                        CodeGenerationOperatorKind.RightShift
                    },
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(object),
                    statements: "return null;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddComparisonOperators()
            {
                var input = @"
class [|C|]
{
}";
                var expected = @"
class C
{
    public static bool operator == (C a, C b) { $$ }
    public static bool operator != (C a, C b) { $$ }
    public static bool operator < (C a, C b) { $$ }
    public static bool operator > (C a, C b) { $$ }
    public static bool operator <= (C a, C b) { $$ }
    public static bool operator >= (C a, C b) { $$ }
}";
                TestAddOperators(input, expected,
                    new[]
                    {
                        CodeGenerationOperatorKind.Equality,
                        CodeGenerationOperatorKind.Inequality,
                        CodeGenerationOperatorKind.GreaterThan,
                        CodeGenerationOperatorKind.LessThan,
                        CodeGenerationOperatorKind.LessThanOrEqual,
                        CodeGenerationOperatorKind.GreaterThanOrEqual
                    },
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(bool),
                    statements: "return true;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsupportedOperator()
            {
                var input = "class [|C|] { }";
                TestAddUnsupportedOperator(input,
                    operatorKind: CodeGenerationOperatorKind.Like,
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(bool),
                    statements: "return true;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddExplicitConversion()
            {
                var input = @"class [|C|] { }";
                var expected = @"class C { public static explicit operator int(C other) { $$ } }";
                TestAddConversion(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    statements: "return 0;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddImplicitConversion()
            {
                var input = @"class [|C|] { }";
                var expected = @"class C { public static implicit operator int(C other) { $$ } }";
                TestAddConversion(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    isImplicit: true,
                    statements: "return 0;");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStatements()
            {
                var input = "class C { public void [|M|]() { Console.WriteLine(1); } }";
                var expected = "class C { public void M() { Console.WriteLine(1); $$ } }";
                TestAddStatements(input, expected, "Console.WriteLine(2);");
            }

            [WorkItem(840265)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddDefaultParameterWithNonDefaultValueToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(string text = \"Hello\") { } }";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(string), "text", true, "Hello")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddDefaultParameterWithDefaultValueToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(double number = default(double)) { } }";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(double), "number", true)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(int num, string text =\"Hello!\", float floating = 0.5F) { } }";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5f)));
            }

            [WorkItem(841365)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParamsParameterToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(params char[] characters) { } }";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(char[]), "characters", isParams: true)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544015)]
            public void AddAutoProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int P { get; internal set; } }";
                TestAddProperty(input, expected,
                    type: typeof(int),
                    setterAccessibility: Accessibility.Internal);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsafeAutoProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe int P { get; internal set; } }";
                TestAddProperty(input, expected,
                    type: typeof(int),
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    setterAccessibility: Accessibility.Internal);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddIndexer()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public string this[int i] { get { $$ } } }";
                TestAddProperty(input, expected,
                    type: typeof(string),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    getStatements: "return String.Empty;",
                    isIndexer: true);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParameterfulProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public string get_P(int i, int j) { $$ } public void set_P(int i, int j, string value) { } }";
                TestAddProperty(input, expected,
                    type: typeof(string),
                    getStatements: "return String.Empty;",
                    setStatements: "",
                    parameters: Parameters(Parameter(typeof(int), "i"), Parameter(typeof(int), "j")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToTypes()
            {
                var input = "class [|C|] { }";
                var expected = "[System.Serializable] class C { }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromTypes()
            {
                var input = @"[System.Serializable] class [|C|] { }";
                var expected = "class C { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToMethods()
            {
                var input = "class C { public void [|M()|] { } }";
                var expected = "class C { [System.Serializable] public void M() { } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromMethods()
            {
                var input = "class C { [System.Serializable] public void [|M()|] { } }";
                var expected = "class C { public void M() { } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToFields()
            {
                var input = "class C { [|public int F|]; }";
                var expected = "class C { [System.Serializable] public int F; }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromFields()
            {
                var input = "class C { [System.Serializable] public int [|F|]; }";
                var expected = "class C { public int F; }";
                TestRemoveAttribute<FieldDeclarationSyntax>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToProperties()
            {
                var input = "class C { public int [|P|] { get; set; }}";
                var expected = "class C { [System.Serializable] public int P { get; set; } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromProperties()
            {
                var input = "class C { [System.Serializable] public int [|P|] { get; set; }}";
                var expected = "class C { public int P { get; set; } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToPropertyAccessor()
            {
                var input = "class C { public int P { [|get|]; set; }}";
                var expected = "class C { public int P { [System.Serializable] get; set; } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromPropertyAccessor()
            {
                var input = "class C { public int P { [System.Serializable] [|get|]; set; } }";
                var expected = "class C { public int P { get; set; } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToEnums()
            {
                var input = "enum [|C|] { One, Two }";
                var expected = "[System.Serializable] enum C { One, Two }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromEnums()
            {
                var input = "[System.Serializable] enum [|C|] { One, Two }";
                var expected = "enum C { One, Two }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToEnumMembers()
            {
                var input = "enum C { [|One|], Two }";
                var expected = "enum C { [System.Serializable] One, Two }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromEnumMembers()
            {
                var input = "enum C { [System.Serializable] [|One|], Two }";
                var expected = "enum C { One, Two }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToIndexer()
            {
                var input = "class C { public int [|this[int y]|] { get; set; }}";
                var expected = "class C { [System.Serializable] public int this[int y] { get; set; } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromIndexer()
            {
                var input = "class C { [System.Serializable] public int [|this[int y]|] { get; set; }}";
                var expected = "class C { public int this[int y] { get; set; } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToOperator()
            {
                var input = "class C { public static C operator [|+|] (C c1, C c2) { return new C(); }}";
                var expected = "class C { [System.Serializable] public static C operator + (C c1, C c2) { return new C(); } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromOperator()
            {
                var input = "class C { [System.Serializable] public static C operator [|+|](C c1, C c2) { return new C(); }}";
                var expected = "class C { public static C operator +(C c1, C c2) { return new C(); } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToDelegate()
            {
                var input = "delegate int [|D()|];";
                var expected = "[System.Serializable] delegate int D();";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromDelegate()
            {
                var input = "[System.Serializable] delegate int [|D()|];";
                var expected = "delegate int D();";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToParam()
            {
                var input = "class C { public void M([|int x|]) { } }";
                var expected = "class C { public void M([System.Serializable] int x) { } }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromParam()
            {
                var input = "class C { public void M([System.Serializable] [|int x|]) { } }";
                var expected = "class C { public void M(int x) { } }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToTypeParam()
            {
                var input = "class C<[|T|]> { }";
                var expected = "class C<[System.Serializable] T> { }";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromTypeParam()
            {
                var input = "class C<[System.Serializable] [|T|]> { }";
                var expected = "class C<T> { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToCompilationUnit()
            {
                var input = "[|class C { } class D {} |]";
                var expected = "[assembly: System.Serializable] class C{ } class D {}";
                TestAddAttribute(input, expected, typeof(SerializableAttribute), SyntaxFactory.Token(SyntaxKind.AssemblyKeyword));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeWithWrongTarget()
            {
                var input = "[|class C { } class D {} |]";
                var expected = "";
                Assert.Throws<AggregateException>(() => TestAddAttribute(input, expected, typeof(SerializableAttribute), SyntaxFactory.Token(SyntaxKind.RefKeyword)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithTrivia()
            {
                // With trivia.
                var input = @"// Comment 1
[System.Serializable] // Comment 2
/* Comment 3*/ class [|C|] { }";
                var expected = @"// Comment 1
/* Comment 3*/ class C { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithTrivia_NewLine()
            {
                // With trivia, redundant newline at end of attribute removed.
                var input = @"// Comment 1
[System.Serializable]
/* Comment 3*/ class [|C|] { }";
                var expected = @"// Comment 1
/* Comment 3*/ class C { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithMultipleAttributes()
            {
                // Multiple attributes.
                var input = @"// Comment 1
/*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
/* Comment 8*/
class [|C|] { }";
                var expected = @"// Comment 1
/*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
/* Comment 8*/
class C { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithMultipleAttributeLists()
            {
                // Multiple attributes.
                var input = @"// Comment 1
/*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
[ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
/* Comment12*/
class [|C|] { }";
                var expected = @"// Comment 1
/*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
[ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
/* Comment12*/
class C { }";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateModifiers()
            {
                var input = @"public static class [|C|] // Comment 1
{
    // Comment 2
}";
                var expected = @"internal partial sealed class C // Comment 1
{
    // Comment 2
}";
                var eol = SyntaxFactory.EndOfLine(@"");
                var newModifiers = new[] { SyntaxFactory.Token(SyntaxKind.InternalKeyword).WithLeadingTrivia(eol) }.Concat(
                    CreateModifierTokens(new DeclarationModifiers(isSealed: true, isPartial: true), LanguageNames.CSharp));

                TestUpdateDeclaration<ClassDeclarationSyntax>(input, expected, modifiers: newModifiers);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateAccessibility()
            {
                var input = @"// Comment 0
public static class [|C|] // Comment 1
{
    // Comment 2
}";
                var expected = @"// Comment 0
internal static class C // Comment 1
{
    // Comment 2
}";
                TestUpdateDeclaration<ClassDeclarationSyntax>(input, expected, accessibility: Accessibility.Internal);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateDeclarationType()
            {
                var input = @"
public static class C
{
    // Comment 1
    public static char [|F|]() { return 0; }
}";
                var expected = @"
public static class C
{
    // Comment 1
    public static int F() { return 0; }
}";
                TestUpdateDeclaration<MethodDeclarationSyntax>(input, expected, getType: GetTypeSymbol(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateDeclarationMembers()
            {
                var input = @"
public static class [|C|]
{
    // Comment 0
    public int {|RetainedMember:f|};

    // Comment 1
    public static char F() { return 0; }
}";
                var expected = @"
public static class C
{
    // Comment 0
    public int f;
    public int f2;
}";
                var getField = CreateField(Accessibility.Public, new DeclarationModifiers(), typeof(int), "f2");
                var getMembers = new List<Func<SemanticModel, ISymbol>>();
                getMembers.Add(getField);
                TestUpdateDeclaration<ClassDeclarationSyntax>(input, expected, getNewMembers: getMembers);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateDeclarationMembers_DifferentOrder()
            {
                var input = @"
public static class [|C|]
{
    // Comment 0
    public int f;

    // Comment 1
    public static char {|RetainedMember:F|}() { return 0; }
}";
                var expected = @"
public static class C
{
    public int f2;

    // Comment 1
    public static char F() { return 0; }
}";
                var getField = CreateField(Accessibility.Public, new DeclarationModifiers(), typeof(int), "f2");
                var getMembers = new List<Func<SemanticModel, ISymbol>>();
                getMembers.Add(getField);
                TestUpdateDeclaration<ClassDeclarationSyntax>(input, expected, getNewMembers: getMembers, declareNewMembersAtTop: true);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public void SortAroundDestructor()
            {
                var generationSource = "public class [|C|] { public C(){} public int this[int index]{get{return 0;}set{value = 0;}} }";
                var initial = "public class [|C|] { ~C(){} }";
                var expected = @"
public class C
{
    public C(){}
    ~C(){}
    public int this[int index] { get{} set{} }
}";
                TestGenerateFromSourceSymbol(generationSource, initial, expected, onlyGenerateMembers: true);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public void SortOperators()
            {
                var generationSource = @"
namespace N
{
    public class [|C|]
    {
        // Unary operators
        public static bool operator false (C other) { return false; }
        public static bool operator true (C other) { return true; }
        public static C operator ++ (C other) { return null; }
        public static C operator -- (C other) { return null; }
        public static C operator ~ (C other) { return null; }
        public static C operator ! (C other) { return null; }
        public static C operator - (C other) { return null; }
        public static C operator + (C other) { return null; }
    
        // Binary operators
        public static C operator >> (C a, int shift) { return null; }
        public static C operator << (C a, int shift) { return null; }
        public static C operator ^ (C a, C b) { return null; }
        public static C operator | (C a, C b) { return null; }
        public static C operator & (C a, C b) { return null; }
        public static C operator % (C a, C b) { return null; }
        public static C operator / (C a, C b) { return null; }
        public static C operator * (C a, C b) { return null; }
        public static C operator - (C a, C b) { return null; }
        public static C operator + (C a, C b) { return null; }

        // Comparison operators
        public static bool operator >= (C a, C b) { return true; }
        public static bool operator <= (C a, C b) { return true; }
        public static bool operator > (C a, C b) { return true; }
        public static bool operator < (C a, C b) { return true; }
        public static bool operator != (C a, C b) { return true; }
        public static bool operator == (C a, C b) { return true; }
    }
}";
                var initial = "namespace [|N|] { }";
                var expected = @"
namespace N
{
    public class C
    {
        public static C operator +(C other);
        public static C operator +(C a, C b);
        public static C operator -(C other);
        public static C operator -(C a, C b);
        public static C operator !(C other);
        public static C operator ~(C other);
        public static C operator ++(C other);
        public static C operator --(C other);
        public static C operator *(C a, C b);
        public static C operator /(C a, C b);
        public static C operator %(C a, C b);
        public static C operator &(C a, C b);
        public static C operator |(C a, C b);
        public static C operator ^(C a, C b);
        public static C operator <<(C a, int shift);
        public static C operator >>(C a, int shift);
        public static bool operator ==(C a, C b);
        public static bool operator !=(C a, C b);
        public static bool operator <(C a, C b);
        public static bool operator >(C a, C b);
        public static bool operator <=(C a, C b);
        public static bool operator >=(C a, C b);
        public static bool operator true(C other);
        public static bool operator false(C other);
    }
}";
                TestGenerateFromSourceSymbol(generationSource, initial, expected,
                    forceLanguage: LanguageNames.CSharp,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }
        }

        [WorkItem(665008)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestExtensionMethods()
        {
            var generationSource = @"
public static class [|C|]
{
    public static void ExtMethod1(this string s, int y, string z) {}
}";
            var initial = "public static class [|C|] {}";
            var expected = @"
public static class C
{
    public static void ExtMethod1(this string s, int y, string z);
}
";
            TestGenerateFromSourceSymbol(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }

        [WorkItem(530829)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestVBPropertiesWithParams()
        {
            var generationSource = @"
Namespace N
    Public Class [|C|]
        Public Overridable Property IndexProp(ByVal p1 As Integer) As String
            Get
                Return Nothing
            End Get
            Set(ByVal value As String)
            End Set
        End Property
    End Class
End Namespace
";

            var initial = "namespace [|N|] {}";
            var expected = @"
namespace N 
{ 
    public class C 
    { 
        public virtual string get_IndexProp ( int p1 ) ; 
        public virtual void set_IndexProp ( int p1 , string value ) ; 
    } 
} 
";
            TestGenerateFromSourceSymbol(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
        }

        [WorkItem(812738)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestRefParamsWithDefaultValue()
        {
            var generationSource = @"
Public Class [|C|]
    Public Sub Foo(x As Integer, Optional ByRef y As Integer = 10, Optional ByRef z As Object = Nothing)
    End Sub
End Class";
            var initial = "public class [|C|] {}";
            var expected = @"
public class C
{
    public void Foo(int x, ref int y, ref object z);
}
";
            TestGenerateFromSourceSymbol(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }

        [WorkItem(848357)]
        [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public void TestConstraints()
        {
            var generationSource = @"
namespace N
{
    public class [|C|]<T, U> where T : struct where U : class
    {
        public void Foo<Q, R>() where Q : new() where R : IComparable { }
        public delegate void D<T, U>(T t, U u) where T : struct where U : class;
    }
}
";
            var initial = "namespace [|N|] {}";
            var expected = @"
namespace N
{
    public class C<T, U> where T : struct where U : class
    {
        public void Foo<Q, R>() where Q : new() where R : IComparable;
        public delegate void D<T, U>(T t, U u) where T : struct where U : class;
    }
}
";
            TestGenerateFromSourceSymbol(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }
    }
}
