// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddNamespace()
            {
                var input = "namespace [|N1|] { }";
                var expected = "namespace N1 { namespace N2 { } }";
                await TestAddNamespaceAsync(input, expected,
                    name: "N2");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int F; }";
                await TestAddFieldAsync(input, expected,
                    type: GetTypeSymbol(typeof(int)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddStaticField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { private static string F; }";
                await TestAddFieldAsync(input, expected,
                    type: GetTypeSymbol(typeof(string)),
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddArrayField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int[] F; }";
                await TestAddFieldAsync(input, expected,
                    type: CreateArrayType(typeof(int)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnsafeField()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe int F; }";
                await TestAddFieldAsync(input, expected,
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    type: GetTypeSymbol(typeof(int)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddFieldToCompilationUnit()
            {
                var input = "";
                var expected = "public int F;";
                await TestAddFieldAsync(input, expected,
                    type: GetTypeSymbol(typeof(int)), addToCompilationUnit: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddConstructor()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public C() { } }";
                await TestAddConstructorAsync(input, expected);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddConstructorWithoutBody()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public C(); }";
                await TestAddConstructorAsync(input, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddConstructorResolveNamespaceImport()
            {
                var input = "class [|C|] { }";
                var expected = "using System; class C { public C(DateTime dt, int i) { } }";
                await TestAddConstructorAsync(input, expected,
                    parameters: Parameters(Parameter(typeof(DateTime), "dt"), Parameter(typeof(int), "i")));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddChainedConstructor()
            {
                var input = "class [|C|] { public C(int i) { } }";
                var expected = "class C { public C() : this(42) { } public C(int i) { } }";
                await TestAddConstructorAsync(input, expected,
                    thisArguments: new[] { CS.SyntaxFactory.ParseExpression("42") });
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddStaticConstructor()
            {
                var input = "class [|C|] { }";
                var expected = "class C { static C() { } }";
                await TestAddConstructorAsync(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544082)]
            public async Task AddClass()
            {
                var input = "namespace [|N|] { }";
                var expected = @"namespace N
{
    public class C
    {
    }
}";
                await TestAddNamedTypeAsync(input, expected,
                    compareTokens: false);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddClassEscapeName()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public class @class { } }";
                await TestAddNamedTypeAsync(input, expected,
                    name: "class");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddClassUnicodeName()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public class class\u00E6\u00F8\u00E5 { } }";
                await TestAddNamedTypeAsync(input, expected,
                    name: "cl\u0061ss\u00E6\u00F8\u00E5");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public async Task AddStaticClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public static class C { } }";
                await TestAddNamedTypeAsync(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public async Task AddSealedClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { private sealed class C { } }";
                await TestAddNamedTypeAsync(input, expected,
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isSealed: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544405)]
            public async Task AddAbstractClass()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { protected internal abstract class C { } }";
                await TestAddNamedTypeAsync(input, expected,
                    accessibility: Accessibility.ProtectedOrInternal,
                    modifiers: new DeclarationModifiers(isAbstract: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddStruct()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { internal struct S { } }";
                await TestAddNamedTypeAsync(input, expected,
                    name: "S",
                    accessibility: Accessibility.Internal,
                    typeKind: TypeKind.Struct);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public async Task AddSealedStruct()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public struct S { } }";
                await TestAddNamedTypeAsync(input, expected,
                    name: "S",
                    modifiers: new DeclarationModifiers(isSealed: true),
                    accessibility: Accessibility.Public,
                    typeKind: TypeKind.Struct);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddInterface()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public interface I { } }";
                await TestAddNamedTypeAsync(input, expected,
                    name: "I",
                    typeKind: TypeKind.Interface);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544080)]
            public async Task AddEnum()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public enum E { } }";
                await TestAddNamedTypeAsync(input, expected, "E",
                    typeKind: TypeKind.Enum);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544527)]
            public async Task AddEnumWithValues()
            {
                var input = "namespace [|N|] { }";
                var expected = "namespace N { public enum E { F1 = 1, F2 = 2 } }";
                await TestAddNamedTypeAsync(input, expected, "E",
                    typeKind: TypeKind.Enum,
                    members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544080)]
            public async Task AddDelegateType()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public delegate int D(string s); }";
                await TestAddDelegateTypeAsync(input, expected,
                    returnType: typeof(int),
                    parameters: Parameters(Parameter(typeof(string), "s")));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public async Task AddSealedDelegateType()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public delegate int D(string s); }";
                await TestAddDelegateTypeAsync(input, expected,
                    returnType: typeof(int),
                    parameters: Parameters(Parameter(typeof(string), "s")),
                    modifiers: new DeclarationModifiers(isSealed: true));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddEvent()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public event System.Action E; }";
                await TestAddEventAsync(input, expected,
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnsafeEvent()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe event System.Action E; }";
                await TestAddEventAsync(input, expected,
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddEventWithAccessors()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public event System.Action E { add { } remove { } } }";
                await TestAddEventAsync(input, expected,
                    addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(), Accessibility.NotApplicable, SpecializedCollections.EmptyList<SyntaxNode>()),
                    removeMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(SpecializedCollections.EmptyList<AttributeData>(), Accessibility.NotApplicable, SpecializedCollections.EmptyList<SyntaxNode>()),
                    codeGenerationOptions: new CodeGenerationOptions(addImports: false));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddMethodToClass()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public void M() { } }";
                await TestAddMethodAsync(input, expected,
                    returnType: typeof(void));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddMethodToClassEscapedName()
            {
                var input = "class [|C|] { }";
                var expected = "using System; class C { public DateTime @static() { } }";
                await TestAddMethodAsync(input, expected,
                    name: "static",
                    returnType: typeof(DateTime));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddStaticMethodToStruct()
            {
                var input = "struct [|S|] { }";
                var expected = "struct S { public static int M() { $$ } }";
                await TestAddMethodAsync(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddSealedOverrideMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public sealed override int GetHashCode() { $$ } }";
                await TestAddMethodAsync(input, expected,
                    name: "GetHashCode",
                    modifiers: new DeclarationModifiers(isOverride: true, isSealed: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAbstractMethod()
            {
                var input = "abstract class [|C|] { }";
                var expected = "abstract class C { public abstract int M(); }";
                await TestAddMethodAsync(input, expected,
                    modifiers: new DeclarationModifiers(isAbstract: true),
                    returnType: typeof(int));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddMethodWithoutBody()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int M(); }";
                await TestAddMethodAsync(input, expected,
                    returnType: typeof(int),
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddGenericMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int M<T>() { $$ } }";
                await TestAddMethodAsync(input, expected,
                    returnType: typeof(int),
                    typeParameters: new[] { CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T") },
                    statements: "return new T().GetHashCode();");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddVirtualMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { protected virtual int M() { $$ } }";
                await TestAddMethodAsync(input, expected,
                    accessibility: Accessibility.Protected,
                    modifiers: new DeclarationModifiers(isVirtual: true),
                    returnType: typeof(int),
                    statements: "return 0;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnsafeNewMethod()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe new string ToString() { $$ } }";
                await TestAddMethodAsync(input, expected,
                    name: "ToString",
                    modifiers: new DeclarationModifiers(isNew: true, isUnsafe: true),
                    returnType: typeof(string),
                    statements: "return String.Empty;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddExplicitImplementationOfUnsafeMethod()
            {
                var input = "interface I { unsafe void M(int i); } class [|C|] : I { }";
                var expected = "interface I { unsafe void M(int i); } class C : I { unsafe void I.M(int i) { } }";
                await TestAddMethodAsync(input, expected,
                    name: "M",
                    returnType: typeof(void),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    explicitInterface: s => s.LookupSymbols(input.IndexOf('M'), null, "M").First() as IMethodSymbol);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddExplicitImplementation()
            {
                var input = "interface I { void M(int i); } class [|C|] : I { }";
                var expected = "interface I { void M(int i); } class C : I { void I.M(int i) { } }";
                await TestAddMethodAsync(input, expected,
                    name: "M",
                    returnType: typeof(void),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    explicitInterface: s => s.LookupSymbols(input.IndexOf('M'), null, "M").First() as IMethodSymbol);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddTrueFalseOperators()
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
                await TestAddOperatorsAsync(input, expected,
                    new[] { CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False },
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(bool),
                    statements: "return false;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnaryOperators()
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
                await TestAddOperatorsAsync(input, expected,
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

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddBinaryOperators()
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
                await TestAddOperatorsAsync(input, expected,
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

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddComparisonOperators()
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
                await TestAddOperatorsAsync(input, expected,
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

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnsupportedOperator()
            {
                var input = "class [|C|] { }";
                await TestAddUnsupportedOperatorAsync(input,
                    operatorKind: CodeGenerationOperatorKind.Like,
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(bool),
                    statements: "return true;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddExplicitConversion()
            {
                var input = @"class [|C|] { }";
                var expected = @"class C { public static explicit operator int(C other) { $$ } }";
                await TestAddConversionAsync(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    statements: "return 0;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddImplicitConversion()
            {
                var input = @"class [|C|] { }";
                var expected = @"class C { public static implicit operator int(C other) { $$ } }";
                await TestAddConversionAsync(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    isImplicit: true,
                    statements: "return 0;");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddStatements()
            {
                var input = "class C { public void [|M|]() { Console.WriteLine(1); } }";
                var expected = "class C { public void M() { Console.WriteLine(1); $$ } }";
                await TestAddStatementsAsync(input, expected, "Console.WriteLine(2);");
            }

            [WorkItem(840265)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddDefaultParameterWithNonDefaultValueToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(string text = \"Hello\") { } }";
                await TestAddParametersAsync(input, expected,
                    Parameters(Parameter(typeof(string), "text", true, "Hello")));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddDefaultParameterWithDefaultValueToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(double number = default(double)) { } }";
                await TestAddParametersAsync(input, expected,
                    Parameters(Parameter(typeof(double), "number", true)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddParametersToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(int num, string text =\"Hello!\", float floating = 0.5F) { } }";
                await TestAddParametersAsync(input, expected,
                    Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5f)));
            }

            [WorkItem(841365)]
            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddParamsParameterToMethod()
            {
                var input = "class C { public void [|M|]() { } }";
                var expected = "class C { public void M(params char[] characters) { } }";
                await TestAddParametersAsync(input, expected,
                    Parameters(Parameter(typeof(char[]), "characters", isParams: true)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544015)]
            public async Task AddAutoProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public int P { get; internal set; } }";
                await TestAddPropertyAsync(input, expected,
                    type: typeof(int),
                    setterAccessibility: Accessibility.Internal);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddUnsafeAutoProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public unsafe int P { get; internal set; } }";
                await TestAddPropertyAsync(input, expected,
                    type: typeof(int),
                    modifiers: new DeclarationModifiers(isUnsafe: true),
                    setterAccessibility: Accessibility.Internal);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddIndexer()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public string this[int i] { get { $$ } } }";
                await TestAddPropertyAsync(input, expected,
                    type: typeof(string),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    getStatements: "return String.Empty;",
                    isIndexer: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddParameterfulProperty()
            {
                var input = "class [|C|] { }";
                var expected = "class C { public string get_P(int i, int j) { $$ } public void set_P(int i, int j, string value) { } }";
                await TestAddPropertyAsync(input, expected,
                    type: typeof(string),
                    getStatements: "return String.Empty;",
                    setStatements: "",
                    parameters: Parameters(Parameter(typeof(int), "i"), Parameter(typeof(int), "j")));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToTypes()
            {
                var input = "class [|C|] { }";
                var expected = "[System.Serializable] class C { }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromTypes()
            {
                var input = @"[System.Serializable] class [|C|] { }";
                var expected = "class C { }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToMethods()
            {
                var input = "class C { public void [|M()|] { } }";
                var expected = "class C { [System.Serializable] public void M() { } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromMethods()
            {
                var input = "class C { [System.Serializable] public void [|M()|] { } }";
                var expected = "class C { public void M() { } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToFields()
            {
                var input = "class C { [|public int F|]; }";
                var expected = "class C { [System.Serializable] public int F; }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromFields()
            {
                var input = "class C { [System.Serializable] public int [|F|]; }";
                var expected = "class C { public int F; }";
                await TestRemoveAttributeAsync<FieldDeclarationSyntax>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToProperties()
            {
                var input = "class C { public int [|P|] { get; set; }}";
                var expected = "class C { [System.Serializable] public int P { get; set; } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromProperties()
            {
                var input = "class C { [System.Serializable] public int [|P|] { get; set; }}";
                var expected = "class C { public int P { get; set; } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToPropertyAccessor()
            {
                var input = "class C { public int P { [|get|]; set; }}";
                var expected = "class C { public int P { [System.Serializable] get; set; } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromPropertyAccessor()
            {
                var input = "class C { public int P { [System.Serializable] [|get|]; set; } }";
                var expected = "class C { public int P { get; set; } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToEnums()
            {
                var input = "enum [|C|] { One, Two }";
                var expected = "[System.Serializable] enum C { One, Two }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromEnums()
            {
                var input = "[System.Serializable] enum [|C|] { One, Two }";
                var expected = "enum C { One, Two }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToEnumMembers()
            {
                var input = "enum C { [|One|], Two }";
                var expected = "enum C { [System.Serializable] One, Two }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromEnumMembers()
            {
                var input = "enum C { [System.Serializable] [|One|], Two }";
                var expected = "enum C { One, Two }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToIndexer()
            {
                var input = "class C { public int [|this[int y]|] { get; set; }}";
                var expected = "class C { [System.Serializable] public int this[int y] { get; set; } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromIndexer()
            {
                var input = "class C { [System.Serializable] public int [|this[int y]|] { get; set; }}";
                var expected = "class C { public int this[int y] { get; set; } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToOperator()
            {
                var input = "class C { public static C operator [|+|] (C c1, C c2) { return new C(); }}";
                var expected = "class C { [System.Serializable] public static C operator + (C c1, C c2) { return new C(); } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromOperator()
            {
                var input = "class C { [System.Serializable] public static C operator [|+|](C c1, C c2) { return new C(); }}";
                var expected = "class C { public static C operator +(C c1, C c2) { return new C(); } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToDelegate()
            {
                var input = "delegate int [|D()|];";
                var expected = "[System.Serializable] delegate int D();";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromDelegate()
            {
                var input = "[System.Serializable] delegate int [|D()|];";
                var expected = "delegate int D();";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToParam()
            {
                var input = "class C { public void M([|int x|]) { } }";
                var expected = "class C { public void M([System.Serializable] int x) { } }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromParam()
            {
                var input = "class C { public void M([System.Serializable] [|int x|]) { } }";
                var expected = "class C { public void M(int x) { } }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToTypeParam()
            {
                var input = "class C<[|T|]> { }";
                var expected = "class C<[System.Serializable] T> { }";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeFromTypeParam()
            {
                var input = "class C<[System.Serializable] [|T|]> { }";
                var expected = "class C<T> { }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeToCompilationUnit()
            {
                var input = "[|class C { } class D {} |]";
                var expected = "[assembly: System.Serializable] class C{ } class D {}";
                await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute), SyntaxFactory.Token(SyntaxKind.AssemblyKeyword));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task AddAttributeWithWrongTarget()
            {
                var input = "[|class C { } class D {} |]";
                var expected = "";
                await Assert.ThrowsAsync<AggregateException>(async () =>
                    await TestAddAttributeAsync(input, expected, typeof(SerializableAttribute), SyntaxFactory.Token(SyntaxKind.RefKeyword)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeWithTrivia()
            {
                // With trivia.
                var input = @"// Comment 1
[System.Serializable] // Comment 2
/* Comment 3*/ class [|C|] { }";
                var expected = @"// Comment 1
/* Comment 3*/ class C { }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeWithTrivia_NewLine()
            {
                // With trivia, redundant newline at end of attribute removed.
                var input = @"// Comment 1
[System.Serializable]
/* Comment 3*/ class [|C|] { }";
                var expected = @"// Comment 1
/* Comment 3*/ class C { }";
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeWithMultipleAttributes()
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
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task RemoveAttributeWithMultipleAttributeLists()
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
                await TestRemoveAttributeAsync<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestUpdateModifiers()
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

                await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(input, expected, modifiers: newModifiers);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestUpdateAccessibility()
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
                await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(input, expected, accessibility: Accessibility.Internal);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestUpdateDeclarationType()
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
                await TestUpdateDeclarationAsync<MethodDeclarationSyntax>(input, expected, getType: GetTypeSymbol(typeof(int)));
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestUpdateDeclarationMembers()
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
                await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(input, expected, getNewMembers: getMembers);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public async Task TestUpdateDeclarationMembers_DifferentOrder()
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
                await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(input, expected, getNewMembers: getMembers, declareNewMembersAtTop: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task SortAroundDestructor()
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
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected, onlyGenerateMembers: true);
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public async Task SortOperators()
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
                await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                    forceLanguage: LanguageNames.CSharp,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }
        }

        [WorkItem(665008)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestExtensionMethods()
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
            await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }

        [WorkItem(530829)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestVBPropertiesWithParams()
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
            await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
        }

        [WorkItem(812738)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestRefParamsWithDefaultValue()
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
            await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }

        [WorkItem(848357)]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestConstraints()
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
            await TestGenerateFromSourceSymbolAsync(generationSource, initial, expected,
                codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                onlyGenerateMembers: true);
        }
    }
}
