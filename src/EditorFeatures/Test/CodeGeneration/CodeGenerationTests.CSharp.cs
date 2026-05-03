// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration;

using static CSharpSyntaxTokens;

[Trait(Traits.Feature, Traits.Features.CodeGeneration)]
public sealed partial class CodeGenerationTests
{
    [UseExportProvider]
    public class CSharp
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddNamespace()
            => TestAddNamespaceAsync("namespace [|N1|] { }", """
                namespace N1 {
                    namespace N2
                    {
                    }
                }
                """,
                name: "N2");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddField()
            => TestAddFieldAsync("class [|C|] { }", """
                class C
                {
                    public int F;
                }
                """,
                type: GetTypeSymbol(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStaticField()
            => TestAddFieldAsync("class [|C|] { }", """
                class C
                {
                    private static string F;
                }
                """,
                type: GetTypeSymbol(typeof(string)),
                accessibility: Accessibility.Private,
                modifiers: new Editing.DeclarationModifiers(isStatic: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddArrayField()
            => TestAddFieldAsync("class [|C|] { }", """
                class C
                {
                    public int[] F;
                }
                """,
                type: CreateArrayType(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsafeField()
            => TestAddFieldAsync("class [|C|] { }", """
                class C
                {
                    public unsafe int F;
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                type: GetTypeSymbol(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddFieldToCompilationUnit()
            => TestAddFieldAsync("", "public int F;\n",
                type: GetTypeSymbol(typeof(int)), addToCompilationUnit: true);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructor()
            => TestAddConstructorAsync("class [|C|] { }", """
                class C
                {
                    public C()
                    {
                    }
                }
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructorWithoutBody()
            => TestAddConstructorAsync("class [|C|] { }", """
                class C
                {
                    public C();
                }
                """,
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructorResolveNamespaceImport()
            => TestAddConstructorAsync("class [|C|] { }", """
                using System;

                class C
                {
                    public C(DateTime dt, int i)
                    {
                    }
                }
                """,
                parameters: Parameters(Parameter(typeof(DateTime), "dt"), Parameter(typeof(int), "i")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddChainedConstructor()
            => TestAddConstructorAsync("class [|C|] { public C(int i) { } }", "class C { public C() : this(42) { } public C(int i) { } }",
                thisArguments: [CS.SyntaxFactory.ParseExpression("42")]);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStaticConstructor()
            => TestAddConstructorAsync("class [|C|] { }", """
                class C
                {
                    static C()
                    {
                    }
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isStatic: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544082")]
        public Task AddClass()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public class C
                    {
                    }
                }
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddClassEscapeName()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public class @class
                    {
                    }
                }
                """,
                name: "class");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddClassUnicodeName()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public class classæøå
                    {
                    }
                }
                """,
                name: "classæøå");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public Task AddStaticClass()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public static class C
                    {
                    }
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isStatic: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public Task AddStaticAbstractClass()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public static class C
                    {
                    }
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isStatic: true, isAbstract: true));

        [Theory, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        [InlineData(Accessibility.NotApplicable)]
        [InlineData(Accessibility.Internal)]
        [InlineData(Accessibility.Public)]
        public Task AddFileClass(Accessibility accessibility)
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    file class C
                    {
                    }
                }
                """,
                accessibility: accessibility,
                modifiers: new Editing.DeclarationModifiers(isFile: true));

        [Theory, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        [InlineData("struct", TypeKind.Struct)]
        [InlineData("interface", TypeKind.Interface)]
        [InlineData("enum", TypeKind.Enum)]
        public async Task AddFileType(string kindString, TypeKind typeKind)
        {
            var expected = """
                namespace N
                {
                    file 
                """ + kindString + """
                 C
                    {
                    }
                }
                """;
            await TestAddNamedTypeAsync("namespace [|N|] { }", expected,
                typeKind: typeKind,
                modifiers: new Editing.DeclarationModifiers(isFile: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public Task AddSealedClass()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    private sealed class C
                    {
                    }
                }
                """,
                accessibility: Accessibility.Private,
                modifiers: new Editing.DeclarationModifiers(isSealed: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public Task AddAbstractClass()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    protected internal abstract class C
                    {
                    }
                }
                """,
                accessibility: Accessibility.ProtectedOrInternal,
                modifiers: new Editing.DeclarationModifiers(isAbstract: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStruct()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    internal struct S
                    {
                    }
                }
                """,
                name: "S",
                accessibility: Accessibility.Internal,
                typeKind: TypeKind.Struct);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public Task AddSealedStruct()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public struct S
                    {
                    }
                }
                """,
                name: "S",
                modifiers: new Editing.DeclarationModifiers(isSealed: true),
                accessibility: Accessibility.Public,
                typeKind: TypeKind.Struct);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddInterface()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public interface I
                    {
                    }
                }
                """,
                name: "I",
                typeKind: TypeKind.Interface);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544080")]
        public Task AddEnum()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public enum E
                    {
                    }
                }
                """, "E",
                typeKind: TypeKind.Enum);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544527")]
        public Task AddEnumWithValues()
            => TestAddNamedTypeAsync("namespace [|N|] { }", """
                namespace N
                {
                    public enum E
                    {
                        F1 = 1,
                        F2 = 2
                    }
                }
                """, "E",
                typeKind: TypeKind.Enum,
                members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544080")]
        public Task AddDelegateType()
            => TestAddDelegateTypeAsync("class [|C|] { }", """
                class C
                {
                    public delegate int D(string s);
                }
                """,
                returnType: typeof(int),
                parameters: Parameters(Parameter(typeof(string), "s")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public Task AddSealedDelegateType()
            => TestAddDelegateTypeAsync("class [|C|] { }", """
                class C
                {
                    public delegate int D(string s);
                }
                """,
                returnType: typeof(int),
                parameters: Parameters(Parameter(typeof(string), "s")),
                modifiers: new Editing.DeclarationModifiers(isSealed: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEvent()
            => TestAddEventAsync("class [|C|] { }", """
                class C
                {
                    public event System.Action E;
                }
                """,
                context: new CodeGenerationContext(addImports: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddCustomEventToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                class [|C2|]
                {
                    event EventHandler Click
                    {
                        add
                        {
                            Events.AddHandler("ClickEvent", value)
                        }
                        remove
                        {
                            Events.RemoveHandler("ClickEvent", value)
                        }
                    }
                }
                """, "class [|C1|] { }", """
                class C1
                {
                    event EventHandler Click
                    {
                        add
                        {
                            Events.AddHandler("ClickEvent", value)
                        }
                        remove
                        {
                            Events.RemoveHandler("ClickEvent", value)
                        }
                    }
                }
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsafeEvent()
            => TestAddEventAsync("class [|C|] { }", """
                class C
                {
                    public unsafe event System.Action E;
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                context: new CodeGenerationContext(addImports: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEventWithAccessors()
            => TestAddEventAsync("class [|C|] { }", """
                class C
                {
                    public event System.Action E
                    {
                        add
                        {
                        }

                        remove
                        {
                        }
                    }
                }
                """,
                addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol([], Accessibility.NotApplicable, []),
                removeMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol([], Accessibility.NotApplicable, []),
                context: new CodeGenerationContext(addImports: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodToClass()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    public void M()
                    {
                    }
                }
                """,
                returnType: typeof(void));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                class [|C2|]
                {
                    public int FInt()
                    {
                        return 0;
                    }
                }
                """, "class [|C1|] { }", """
                class C1
                {
                    public int FInt()
                    {
                        return 0;
                    }
                }
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodToClassEscapedName()
            => TestAddMethodAsync("class [|C|] { }", """
                using System;

                class C
                {
                    public DateTime @static()
                    {
                    }
                }
                """,
                name: "static",
                returnType: typeof(DateTime));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStaticMethodToStruct()
            => TestAddMethodAsync("struct [|S|] { }", """
                struct S
                {
                    public static int M()
                    {
                        $$
                    }
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isStatic: true),
                returnType: typeof(int),
                statements: "return 0;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddSealedOverrideMethod()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    public sealed override int GetHashCode()
                    {
                        $$
                    }
                }
                """,
                name: "GetHashCode",
                modifiers: new Editing.DeclarationModifiers(isOverride: true, isSealed: true),
                returnType: typeof(int),
                statements: "return 0;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAbstractMethod()
            => TestAddMethodAsync("abstract class [|C|] { }", """
                abstract class C
                {
                    public abstract int M();
                }
                """,
                modifiers: new Editing.DeclarationModifiers(isAbstract: true),
                returnType: typeof(int));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodWithoutBody()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    public int M();
                }
                """,
                returnType: typeof(int),
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddGenericMethod()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    public int M<T>()
                    {
                        $$
                    }
                }
                """,
                returnType: typeof(int),
                typeParameters: [CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T")],
                statements: "return new T().GetHashCode();");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddVirtualMethod()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    protected virtual int M()
                    {
                        $$
                    }
                }
                """,
                accessibility: Accessibility.Protected,
                modifiers: new Editing.DeclarationModifiers(isVirtual: true),
                returnType: typeof(int),
                statements: "return 0;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsafeNewMethod()
            => TestAddMethodAsync("class [|C|] { }", """
                class C
                {
                    public unsafe new string ToString()
                    {
                        $$
                    }
                }
                """,
                name: "ToString",
                modifiers: new Editing.DeclarationModifiers(isNew: true, isUnsafe: true),
                returnType: typeof(string),
                statements: "return String.Empty;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddExplicitImplementationOfUnsafeMethod()
        {
            var input = "interface I { unsafe void M(int i); } class [|C|] : I { }";
            await TestAddMethodAsync(input, """
                interface I { unsafe void M(int i); }
                class C : I
                {
                    unsafe void I.M(int i)
                    {
                    }
                }
                """,
                name: "M",
                returnType: typeof(void),
                parameters: Parameters(Parameter(typeof(int), "i")),
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                getExplicitInterfaces: s => [.. s.LookupSymbols(input.IndexOf('M'), null, "M").OfType<IMethodSymbol>()]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddExplicitImplementation()
        {
            var input = "interface I { void M(int i); } class [|C|] : I { }";
            await TestAddMethodAsync(input, """
                interface I { void M(int i); }
                class C : I
                {
                    void I.M(int i)
                    {
                    }
                }
                """,
                name: "M",
                returnType: typeof(void),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getExplicitInterfaces: s => [.. s.LookupSymbols(input.IndexOf('M'), null, "M").OfType<IMethodSymbol>()]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddTrueFalseOperators()
            => TestAddOperatorsAsync("""
                class [|C|]
                {
                }
                """, """
                class C
                {
                    public static bool operator true(C other)
                    {
                        $$
                    }

                    public static bool operator false(C other)
                    {
                        $$
                    }
                }
                """,
                [CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False],
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(bool),
                statements: "return false;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnaryOperators()
            => TestAddOperatorsAsync("""
                class [|C|]
                {
                }
                """, """
                class C
                {
                    public static object operator +(C other)
                    {
                        $$
                    }

                    public static object operator -(C other)
                    {
                        $$
                    }

                    public static object operator !(C other)
                    {
                        $$
                    }

                    public static object operator ~(C other)
                    {
                        $$
                    }

                    public static object operator ++(C other)
                    {
                        $$
                    }

                    public static object operator --(C other)
                    {
                        $$
                    }
                }
                """,
                [
                    CodeGenerationOperatorKind.UnaryPlus,
                    CodeGenerationOperatorKind.UnaryNegation,
                    CodeGenerationOperatorKind.LogicalNot,
                    CodeGenerationOperatorKind.OnesComplement,
                    CodeGenerationOperatorKind.Increment,
                    CodeGenerationOperatorKind.Decrement
                ],
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(object),
                statements: "return null;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddBinaryOperators()
            => TestAddOperatorsAsync("""
                class [|C|]
                {
                }
                """, """
                class C
                {
                    public static object operator +(C a, C b)
                    {
                        $$
                    }

                    public static object operator -(C a, C b)
                    {
                        $$
                    }

                    public static object operator *(C a, C b)
                    {
                        $$
                    }

                    public static object operator /(C a, C b)
                    {
                        $$
                    }

                    public static object operator %(C a, C b)
                    {
                        $$
                    }

                    public static object operator &(C a, C b)
                    {
                        $$
                    }

                    public static object operator |(C a, C b)
                    {
                        $$
                    }

                    public static object operator ^(C a, C b)
                    {
                        $$
                    }

                    public static object operator <<(C a, C b)
                    {
                        $$
                    }

                    public static object operator >>(C a, C b)
                    {
                        $$
                    }
                }
                """,
                [
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
                ],
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(object),
                statements: "return null;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddComparisonOperators()
            => TestAddOperatorsAsync("""
                class [|C|]
                {
                }
                """, """
                class C
                {
                    public static bool operator ==(C a, C b)
                    {
                        $$
                    }

                    public static bool operator !=(C a, C b)
                    {
                        $$
                    }

                    public static bool operator <(C a, C b)
                    {
                        $$
                    }

                    public static bool operator >(C a, C b)
                    {
                        $$
                    }

                    public static bool operator <=(C a, C b)
                    {
                        $$
                    }

                    public static bool operator >=(C a, C b)
                    {
                        $$
                    }
                }
                """,
                [
                    CodeGenerationOperatorKind.Equality,
                    CodeGenerationOperatorKind.Inequality,
                    CodeGenerationOperatorKind.GreaterThan,
                    CodeGenerationOperatorKind.LessThan,
                    CodeGenerationOperatorKind.LessThanOrEqual,
                    CodeGenerationOperatorKind.GreaterThanOrEqual
                ],
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(bool),
                statements: "return true;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsupportedOperator()
            => TestAddUnsupportedOperatorAsync("class [|C|] { }",
                operatorKind: CodeGenerationOperatorKind.Like,
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(bool),
                statements: "return true;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddExplicitConversion()
            => TestAddConversionAsync(@"class [|C|] { }", """
                class C
                {
                    public static explicit operator int(C other)
                    {
                        $$
                    }
                }
                """,
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                statements: "return 0;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddImplicitConversion()
            => TestAddConversionAsync(@"class [|C|] { }", """
                class C
                {
                    public static implicit operator int(C other)
                    {
                        $$
                    }
                }
                """,
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                isImplicit: true,
                statements: "return 0;");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStatements()
            => TestAddStatementsAsync("class C { public void [|M|]() { Console.WriteLine(1); } }", "class C { public void M() { Console.WriteLine(1); $$} }", "Console.WriteLine(2);");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/840265")]
        public Task AddDefaultParameterWithNonDefaultValueToMethod()
            => TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(string text =\"Hello\") { } }",
                Parameters(Parameter(typeof(string), "text", true, "Hello")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddDefaultParameterWithDefaultValueToMethod()
            => TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(double number =0) { } }",
                Parameters(Parameter(typeof(double), "number", true)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddParametersToMethod()
            => TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(int num, string text =\"Hello!\", float floating =0.5F) { } }",
                Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5f)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/841365")]
        public Task AddParamsParameterToMethod()
            => TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(params char[]characters) { } }",
                Parameters(Parameter(typeof(char[]), "characters", isParams: true)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544015")]
        public Task AddAutoProperty()
            => TestAddPropertyAsync("class [|C|] { }", """
                class C
                {
                    public int P { get; internal set; }
                }
                """,
                type: typeof(int),
                setterAccessibility: Accessibility.Internal);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsafeAutoProperty()
            => TestAddPropertyAsync("class [|C|] { }", """
                class C
                {
                    public unsafe int P { get; internal set; }
                }
                """,
                type: typeof(int),
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                setterAccessibility: Accessibility.Internal);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddPropertyToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                class [|C2|]
                {
                    public int P
                    {
                        get
                        {
                            return 0;
                        }
                    }
                }
                """, "class [|C1|] { }", """
                class C1
                {
                    public int P
                    {
                        get
                        {
                            return 0;
                        }
                    }
                }
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddIndexer1()
            => TestAddPropertyAsync("class [|C|] { }", "class C { public string this[int i] => String.Empty; }",
                type: typeof(string),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getStatements: "return String.Empty;",
                isIndexer: true);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddIndexer2()
            => TestAddPropertyAsync("class [|C|] { }", """
                class C
                {
                    public string this[int i]
                    {
                        get
                        {
                            $$
                        }
                    }
                }
                """,
                type: typeof(string),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getStatements: "return String.Empty;",
                isIndexer: true,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                });

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddParameterfulProperty()
            => TestAddPropertyAsync("class [|C|] { }", """
                class C
                {
                    public string get_P(int i, int j)
                    {
                        $$
                    }

                    public void set_P(int i, int j, string value)
                    {
                    }
                }
                """,
                type: typeof(string),
                getStatements: "return String.Empty;",
                setStatements: "",
                parameters: Parameters(Parameter(typeof(int), "i"), Parameter(typeof(int), "j")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToTypes()
            => TestAddAttributeAsync("class [|C|] { }", """
                [System.Serializable]
                class C { }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromTypes()
            => TestRemoveAttributeAsync<SyntaxNode>(@"[System.Serializable] class [|C|] { }", "class C { }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToMethods()
            => TestAddAttributeAsync("class C { public void [|M()|] { } }", "class C { [System.Serializable] public void M() { } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromMethods()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public void [|M()|] { } }", "class C { public void M() { } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToFields()
            => TestAddAttributeAsync("class C { [|public int F|]; }", "class C { [System.Serializable] public int F; }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromFields()
            => TestRemoveAttributeAsync<FieldDeclarationSyntax>("class C { [System.Serializable] public int [|F|]; }", "class C { public int F; }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToProperties()
            => TestAddAttributeAsync("class C { public int [|P|] { get; set; }}", "class C { [System.Serializable] public int P { get; set; } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromProperties()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public int [|P|] { get; set; }}", "class C { public int P { get; set; } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToPropertyAccessor()
            => TestAddAttributeAsync("class C { public int P { [|get|]; set; }}", "class C { public int P { [System.Serializable] get; set; }}", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromPropertyAccessor()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { public int P { [System.Serializable] [|get|]; set; } }", "class C { public int P { get; set; } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToEnums()
            => TestAddAttributeAsync("enum [|C|] { One, Two }", """
                [System.Serializable]
                enum C { One, Two }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromEnums()
            => TestRemoveAttributeAsync<SyntaxNode>("[System.Serializable] enum [|C|] { One, Two }", "enum C { One, Two }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToEnumMembers()
            => TestAddAttributeAsync("enum C { [|One|], Two }", "enum C { [System.Serializable] One, Two }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromEnumMembers()
            => TestRemoveAttributeAsync<SyntaxNode>("enum C { [System.Serializable] [|One|], Two }", "enum C { One, Two }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToIndexer()
            => TestAddAttributeAsync("class C { public int [|this[int y]|] { get; set; }}", "class C { [System.Serializable] public int this[int y] { get; set; } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromIndexer()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public int [|this[int y]|] { get; set; }}", "class C { public int this[int y] { get; set; } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToOperator()
            => TestAddAttributeAsync("class C { public static C operator [|+|] (C c1, C c2) { return new C(); }}", "class C { [System.Serializable] public static C operator +(C c1, C c2) { return new C(); } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromOperator()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public static C operator [|+|](C c1, C c2) { return new C(); }}", "class C { public static C operator +(C c1, C c2) { return new C(); } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToDelegate()
            => TestAddAttributeAsync("delegate int [|D()|];", """
                [System.Serializable]
                delegate int D();
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromDelegate()
            => TestRemoveAttributeAsync<SyntaxNode>("[System.Serializable] delegate int [|D()|];", "delegate int D();", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToParam()
            => TestAddAttributeAsync("class C { public void M([|int x|]) { } }", "class C { public void M([System.Serializable] int x) { } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromParam()
            => TestRemoveAttributeAsync<SyntaxNode>("class C { public void M([System.Serializable] [|int x|]) { } }", "class C { public void M(int x) { } }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToTypeParam()
            => TestAddAttributeAsync("class C<[|T|]> { }", "class C<[System.Serializable] T> { }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromTypeParam()
            => TestRemoveAttributeAsync<SyntaxNode>("class C<[System.Serializable] [|T|]> { }", "class C<T> { }", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToCompilationUnit()
            => TestAddAttributeAsync("[|class C { } class D {} |]", """
                [assembly: System.Serializable]

                class C { }
                class D { }
                """, typeof(SerializableAttribute), AssemblyKeyword);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeWithWrongTarget()
            => Assert.ThrowsAsync<AggregateException>(async () =>
                await TestAddAttributeAsync("[|class C { } class D {} |]", "", typeof(SerializableAttribute), RefKeyword));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeWithArrayParams()
            => TestAddAttributeAsync("""
                using System; 
                class ExampleAttribute : Attribute
                {
                    public ExampleAttribute(int[] items) { }
                }
                class ExampleType
                {
                    [Example(new[] { 1, 2, 3 })]
                    public void {|method:M|}() { }
                }
                class C
                {
                    public void [|M|]2() { }
                }
                """, """
                using System; 
                class ExampleAttribute : Attribute
                {
                    public ExampleAttribute(int[] items) { }
                }
                class ExampleType
                {
                    [Example(new[] { 1, 2, 3 })]
                    public void M() { }
                }
                class C
                {
                    [Example(new[] { 1, 2, 3 })]
                    public void M2() { }
                }
                """, (context) =>
            {
                var method = context.GetAnnotatedDeclaredSymbols("method", context.SemanticModel).Single();
                var attribute = method.GetAttributes().Single();
                return attribute;
            });

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithTrivia()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                // Comment 1
                [System.Serializable] // Comment 2
                /* Comment 3*/ class [|C|] { }
                """, """
                // Comment 1
                /* Comment 3*/ class C { }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithTrivia_NewLine()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                // Comment 1
                [System.Serializable]
                /* Comment 3*/ class [|C|] { }
                """, """
                // Comment 1
                /* Comment 3*/ class C { }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithMultipleAttributes()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                // Comment 1
                /*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
                /* Comment 8*/
                class [|C|] { }
                """, """
                // Comment 1
                /*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
                /* Comment 8*/
                class C { }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithMultipleAttributeLists()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                // Comment 1
                /*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
                [ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
                /* Comment12*/
                class [|C|] { }
                """, """
                // Comment 1
                /*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
                [ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
                /* Comment12*/
                class C { }
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateModifiers()
        {
            var eol = SyntaxFactory.EndOfLine(@"");
            var newModifiers = new[] { InternalKeyword.WithLeadingTrivia(eol) }.Concat(
                CreateModifierTokens(new Editing.DeclarationModifiers(isSealed: true, isPartial: true), LanguageNames.CSharp));

            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>("""
                public static class [|C|] // Comment 1
                {
                    // Comment 2
                }
                """, """
                internal partial sealed class C // Comment 1
                {
                    // Comment 2
                }
                """, modifiers: newModifiers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task TestUpdateAccessibility()
            => TestUpdateDeclarationAsync<ClassDeclarationSyntax>("""
                // Comment 0
                public static class [|C|] // Comment 1
                {
                    // Comment 2
                }
                """, """
                // Comment 0
                internal static class C // Comment 1
                {
                    // Comment 2
                }
                """, accessibility: Accessibility.Internal);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task TestUpdateDeclarationType()
            => TestUpdateDeclarationAsync<MethodDeclarationSyntax>("""
                public static class C
                {
                    // Comment 1
                    public static char [|F|]() { return 0; }
                }
                """, """
                public static class C
                {
                    // Comment 1
                    public static int F() { return 0; }
                }
                """, getType: GetTypeSymbol(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationMembers()
        {
            var getField = CreateField(Accessibility.Public, new Editing.DeclarationModifiers(), typeof(int), "f2");
            var getMembers = ImmutableArray.Create(getField);
            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(
                """
                public static class [|C|]
                {
                    // Comment 0
                    public int {|RetainedMember:f|};

                    // Comment 1
                    public static char F() { return 0; }
                }
                """, """
                public static class C
                {
                    // Comment 0
                    public int f;
                    public int f2;
                }
                """, getNewMembers: getMembers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationMembers_DifferentOrder()
        {
            var getField = CreateField(Accessibility.Public, new Editing.DeclarationModifiers(), typeof(int), "f2");
            var getMembers = ImmutableArray.Create(getField);
            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>("""
                public static class [|C|]
                {
                    // Comment 0
                    public int f;

                    // Comment 1
                    public static char {|RetainedMember:F|}() { return 0; }
                }
                """, """
                public static class C
                {
                    public int f2;

                    // Comment 1
                    public static char F() { return 0; }
                }
                """, getNewMembers: getMembers, declareNewMembersAtTop: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public Task SortAroundDestructor()
            => TestGenerateFromSourceSymbolAsync("public class [|C|] { public C(){} public int this[int index]{get{return 0;}set{value = 0;}} }", "public class [|C|] { ~C(){} }", """
                public class C {
                    public C()
                    {
                    }

                    ~C(){}

                    public int this[int index]
                    {
                        get
                        {
                        }

                        set
                        {
                        }
                    }
                }
                """, onlyGenerateMembers: true);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public Task SortOperators()
            => TestGenerateFromSourceSymbolAsync("""
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
                }
                """, "namespace [|N|] { }", """
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
                }
                """,
                forceLanguage: LanguageNames.CSharp,
                context: new CodeGenerationContext(generateMethodBodies: false));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665008")]
    public Task TestExtensionMethods()
        => TestGenerateFromSourceSymbolAsync("""
            public static class [|C|]
            {
                public static void ExtMethod1(this string s, int y, string z) {}
            }
            """, "public static class [|C|] {}", """
            public static class C
            {
                public static void ExtMethod1(this string s, int y, string z);
            }
            """,
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530829")]
    public Task TestVBPropertiesWithParams()
        => TestGenerateFromSourceSymbolAsync("""
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
            """, "namespace [|N|] {}", """
            namespace N
            {
                public class C
                {
                    public virtual string get_IndexProp(int p1);
                    public virtual void set_IndexProp(int p1, string value);
                }
            }
            """,
            context: new CodeGenerationContext(generateMethodBodies: false));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/812738")]
    public Task TestRefParamsWithDefaultValue()
        => TestGenerateFromSourceSymbolAsync("""
            Public Class [|C|]
                Public Sub Goo(x As Integer, Optional ByRef y As Integer = 10, Optional ByRef z As Object = Nothing)
                End Sub
            End Class
            """, "public class [|C|] {}", """
            public class C
            {
                public void Goo(int x, ref int y, ref object z);
            }
            """,
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/848357")]
    public Task TestConstraints()
        => TestGenerateFromSourceSymbolAsync("""
            namespace N
            {
                public class [|C|]<T, U> where T : struct where U : class
                {
                    public void Goo<Q, R>() where Q : new() where R : IComparable { }
                    public delegate void D<T, U>(T t, U u) where T : struct where U : class;
                }
            }
            """, "namespace [|N|] {}", """
            namespace N
            {
                public class C<T, U>
                    where T : struct
                    where U : class
                {
                    public void Goo<Q, R>()
                        where Q : new()
                        where R : IComparable;

                    public delegate void D<T, U>(T t, U u)
                        where T : struct
                        where U : class;
                }
            }
            """,
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);
}
