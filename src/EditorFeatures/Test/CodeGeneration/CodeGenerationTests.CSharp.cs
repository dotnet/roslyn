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
        public async Task AddNamespace()
        {
            await TestAddNamespaceAsync("namespace [|N1|] { }", @"namespace N1 {
    namespace N2
    {
    }
}",
                name: "N2");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddField()
        {
            await TestAddFieldAsync("class [|C|] { }", @"class C
{
    public int F;
}",
                type: GetTypeSymbol(typeof(int)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddStaticField()
        {
            await TestAddFieldAsync("class [|C|] { }", @"class C
{
    private static string F;
}",
                type: GetTypeSymbol(typeof(string)),
                accessibility: Accessibility.Private,
                modifiers: new Editing.DeclarationModifiers(isStatic: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddArrayField()
        {
            await TestAddFieldAsync("class [|C|] { }", @"class C
{
    public int[] F;
}",
                type: CreateArrayType(typeof(int)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnsafeField()
        {
            await TestAddFieldAsync("class [|C|] { }", @"class C
{
    public unsafe int F;
}",
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                type: GetTypeSymbol(typeof(int)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddFieldToCompilationUnit()
        {
            await TestAddFieldAsync("", "public int F;\n",
                type: GetTypeSymbol(typeof(int)), addToCompilationUnit: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddConstructor()
        {
            await TestAddConstructorAsync("class [|C|] { }", @"class C
{
    public C()
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddConstructorWithoutBody()
        {
            await TestAddConstructorAsync("class [|C|] { }", @"class C
{
    public C();
}",
                context: new CodeGenerationContext(generateMethodBodies: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddConstructorResolveNamespaceImport()
        {
            await TestAddConstructorAsync("class [|C|] { }", @"using System;

class C
{
    public C(DateTime dt, int i)
    {
    }
}",
                parameters: Parameters(Parameter(typeof(DateTime), "dt"), Parameter(typeof(int), "i")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddChainedConstructor()
        {
            await TestAddConstructorAsync("class [|C|] { public C(int i) { } }", "class C { public C() : this(42) { } public C(int i) { } }",
                thisArguments: [CS.SyntaxFactory.ParseExpression("42")]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddStaticConstructor()
        {
            await TestAddConstructorAsync("class [|C|] { }", @"class C
{
    static C()
    {
    }
}",
                modifiers: new Editing.DeclarationModifiers(isStatic: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544082")]
        public async Task AddClass()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public class C
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddClassEscapeName()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public class @class
    {
    }
}",
                name: "class");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddClassUnicodeName()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public class classæøå
    {
    }
}",
                name: "classæøå");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public async Task AddStaticClass()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public static class C
    {
    }
}",
                modifiers: new Editing.DeclarationModifiers(isStatic: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public async Task AddStaticAbstractClass()
        {
            // note that 'abstract' is dropped here
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public static class C
    {
    }
}",
                modifiers: new Editing.DeclarationModifiers(isStatic: true, isAbstract: true));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        [InlineData(Accessibility.NotApplicable)]
        [InlineData(Accessibility.Internal)]
        [InlineData(Accessibility.Public)]
        public async Task AddFileClass(Accessibility accessibility)
        {
            // note: when invalid combinations of modifiers+accessibility are present here,
            // we actually drop the accessibility. This is similar to what is done if someone declares a 'static abstract class C { }'.
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    file class C
    {
    }
}",
                accessibility: accessibility,
                modifiers: new Editing.DeclarationModifiers(isFile: true));
        }

        [Theory, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        [InlineData("struct", TypeKind.Struct)]
        [InlineData("interface", TypeKind.Interface)]
        [InlineData("enum", TypeKind.Enum)]
        public async Task AddFileType(string kindString, TypeKind typeKind)
        {
            var expected = @"namespace N
{
    file " + kindString + @" C
    {
    }
}";
            await TestAddNamedTypeAsync("namespace [|N|] { }", expected,
                typeKind: typeKind,
                modifiers: new Editing.DeclarationModifiers(isFile: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public async Task AddSealedClass()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    private sealed class C
    {
    }
}",
                accessibility: Accessibility.Private,
                modifiers: new Editing.DeclarationModifiers(isSealed: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544405")]
        public async Task AddAbstractClass()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    protected internal abstract class C
    {
    }
}",
                accessibility: Accessibility.ProtectedOrInternal,
                modifiers: new Editing.DeclarationModifiers(isAbstract: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddStruct()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    internal struct S
    {
    }
}",
                name: "S",
                accessibility: Accessibility.Internal,
                typeKind: TypeKind.Struct);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public async Task AddSealedStruct()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public struct S
    {
    }
}",
                name: "S",
                modifiers: new Editing.DeclarationModifiers(isSealed: true),
                accessibility: Accessibility.Public,
                typeKind: TypeKind.Struct);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddInterface()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public interface I
    {
    }
}",
                name: "I",
                typeKind: TypeKind.Interface);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544080")]
        public async Task AddEnum()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public enum E
    {
    }
}", "E",
                typeKind: TypeKind.Enum);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544527")]
        public async Task AddEnumWithValues()
        {
            await TestAddNamedTypeAsync("namespace [|N|] { }", @"namespace N
{
    public enum E
    {
        F1 = 1,
        F2 = 2
    }
}", "E",
                typeKind: TypeKind.Enum,
                members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544080")]
        public async Task AddDelegateType()
        {
            await TestAddDelegateTypeAsync("class [|C|] { }", @"class C
{
    public delegate int D(string s);
}",
                returnType: typeof(int),
                parameters: Parameters(Parameter(typeof(string), "s")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public async Task AddSealedDelegateType()
        {
            await TestAddDelegateTypeAsync("class [|C|] { }", @"class C
{
    public delegate int D(string s);
}",
                returnType: typeof(int),
                parameters: Parameters(Parameter(typeof(string), "s")),
                modifiers: new Editing.DeclarationModifiers(isSealed: true));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddEvent()
        {
            await TestAddEventAsync("class [|C|] { }", @"class C
{
    public event System.Action E;
}",
                context: new CodeGenerationContext(addImports: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddCustomEventToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync(@"class [|C2|]
{
    event EventHandler Click
    {
        add
        {
            Events.AddHandler(""ClickEvent"", value)
        }
        remove
        {
            Events.RemoveHandler(""ClickEvent"", value)
        }
    }
}", "class [|C1|] { }", @"class C1
{
    event EventHandler Click
    {
        add
        {
            Events.AddHandler(""ClickEvent"", value)
        }
        remove
        {
            Events.RemoveHandler(""ClickEvent"", value)
        }
    }
}", onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnsafeEvent()
        {
            await TestAddEventAsync("class [|C|] { }", @"class C
{
    public unsafe event System.Action E;
}",
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                context: new CodeGenerationContext(addImports: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddEventWithAccessors()
        {
            await TestAddEventAsync("class [|C|] { }", @"class C
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
}",
                addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol([], Accessibility.NotApplicable, []),
                removeMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol([], Accessibility.NotApplicable, []),
                context: new CodeGenerationContext(addImports: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodToClass()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    public void M()
    {
    }
}",
                returnType: typeof(void));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync(@"class [|C2|]
{
    public int FInt()
    {
        return 0;
    }
}", "class [|C1|] { }", @"class C1
{
    public int FInt()
    {
        return 0;
    }
}", onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodToClassEscapedName()
        {
            await TestAddMethodAsync("class [|C|] { }", @"using System;

class C
{
    public DateTime @static()
    {
    }
}",
                name: "static",
                returnType: typeof(DateTime));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddStaticMethodToStruct()
        {
            await TestAddMethodAsync("struct [|S|] { }", @"struct S
{
    public static int M()
    {
        $$
    }
}",
                modifiers: new Editing.DeclarationModifiers(isStatic: true),
                returnType: typeof(int),
                statements: "return 0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddSealedOverrideMethod()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    public sealed override int GetHashCode()
    {
        $$
    }
}",
                name: "GetHashCode",
                modifiers: new Editing.DeclarationModifiers(isOverride: true, isSealed: true),
                returnType: typeof(int),
                statements: "return 0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAbstractMethod()
        {
            await TestAddMethodAsync("abstract class [|C|] { }", @"abstract class C
{
    public abstract int M();
}",
                modifiers: new Editing.DeclarationModifiers(isAbstract: true),
                returnType: typeof(int));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodWithoutBody()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    public int M();
}",
                returnType: typeof(int),
                context: new CodeGenerationContext(generateMethodBodies: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddGenericMethod()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    public int M<T>()
    {
        $$
    }
}",
                returnType: typeof(int),
                typeParameters: [CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T")],
                statements: "return new T().GetHashCode();");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddVirtualMethod()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    protected virtual int M()
    {
        $$
    }
}",
                accessibility: Accessibility.Protected,
                modifiers: new Editing.DeclarationModifiers(isVirtual: true),
                returnType: typeof(int),
                statements: "return 0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnsafeNewMethod()
        {
            await TestAddMethodAsync("class [|C|] { }", @"class C
{
    public unsafe new string ToString()
    {
        $$
    }
}",
                name: "ToString",
                modifiers: new Editing.DeclarationModifiers(isNew: true, isUnsafe: true),
                returnType: typeof(string),
                statements: "return String.Empty;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddExplicitImplementationOfUnsafeMethod()
        {
            var input = "interface I { unsafe void M(int i); } class [|C|] : I { }";
            await TestAddMethodAsync(input, @"interface I { unsafe void M(int i); }
class C : I
{
    unsafe void I.M(int i)
    {
    }
}",
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
            await TestAddMethodAsync(input, @"interface I { void M(int i); }
class C : I
{
    void I.M(int i)
    {
    }
}",
                name: "M",
                returnType: typeof(void),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getExplicitInterfaces: s => [.. s.LookupSymbols(input.IndexOf('M'), null, "M").OfType<IMethodSymbol>()]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddTrueFalseOperators()
        {
            await TestAddOperatorsAsync(@"
class [|C|]
{
}", @"
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
}",
                [CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False],
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(bool),
                statements: "return false;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnaryOperators()
        {
            await TestAddOperatorsAsync(@"
class [|C|]
{
}", @"
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddBinaryOperators()
        {
            await TestAddOperatorsAsync(@"
class [|C|]
{
}", @"
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddComparisonOperators()
        {
            await TestAddOperatorsAsync(@"
class [|C|]
{
}", @"
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
}",
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnsupportedOperator()
        {
            await TestAddUnsupportedOperatorAsync("class [|C|] { }",
                operatorKind: CodeGenerationOperatorKind.Like,
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(bool),
                statements: "return true;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddExplicitConversion()
        {
            await TestAddConversionAsync(@"class [|C|] { }", @"class C
{
    public static explicit operator int(C other)
    {
        $$
    }
}",
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                statements: "return 0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddImplicitConversion()
        {
            await TestAddConversionAsync(@"class [|C|] { }", @"class C
{
    public static implicit operator int(C other)
    {
        $$
    }
}",
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                isImplicit: true,
                statements: "return 0;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddStatements()
        {
            await TestAddStatementsAsync("class C { public void [|M|]() { Console.WriteLine(1); } }", "class C { public void M() { Console.WriteLine(1); $$} }", "Console.WriteLine(2);");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/840265")]
        public async Task AddDefaultParameterWithNonDefaultValueToMethod()
        {
            await TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(string text =\"Hello\") { } }",
                Parameters(Parameter(typeof(string), "text", true, "Hello")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddDefaultParameterWithDefaultValueToMethod()
        {
            await TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(double number =0) { } }",
                Parameters(Parameter(typeof(double), "number", true)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddParametersToMethod()
        {
            await TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(int num, string text =\"Hello!\", float floating =0.5F) { } }",
                Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5f)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/841365")]
        public async Task AddParamsParameterToMethod()
        {
            await TestAddParametersAsync("class C { public void [|M|]() { } }", "class C { public void M(params char[]characters) { } }",
                Parameters(Parameter(typeof(char[]), "characters", isParams: true)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544015")]
        public async Task AddAutoProperty()
        {
            await TestAddPropertyAsync("class [|C|] { }", @"class C
{
    public int P { get; internal set; }
}",
                type: typeof(int),
                setterAccessibility: Accessibility.Internal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddUnsafeAutoProperty()
        {
            await TestAddPropertyAsync("class [|C|] { }", @"class C
{
    public unsafe int P { get; internal set; }
}",
                type: typeof(int),
                modifiers: new Editing.DeclarationModifiers(isUnsafe: true),
                setterAccessibility: Accessibility.Internal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddPropertyToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync(@"class [|C2|]
{
    public int P
    {
        get
        {
            return 0;
        }
    }
}", "class [|C1|] { }", @"class C1
{
    public int P
    {
        get
        {
            return 0;
        }
    }
}", onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddIndexer1()
        {
            await TestAddPropertyAsync("class [|C|] { }", "class C { public string this[int i] => String.Empty; }",
                type: typeof(string),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getStatements: "return String.Empty;",
                isIndexer: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddIndexer2()
        {
            await TestAddPropertyAsync("class [|C|] { }", @"class C
{
    public string this[int i]
    {
        get
        {
            $$
        }
    }
}",
                type: typeof(string),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getStatements: "return String.Empty;",
                isIndexer: true,
                options: new OptionsCollection(LanguageNames.CSharp)
                {
                    { CSharpCodeStyleOptions.PreferExpressionBodiedAccessors, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                    { CSharpCodeStyleOptions.PreferExpressionBodiedIndexers, CSharpCodeStyleOptions.NeverWithSilentEnforcement },
                });
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddParameterfulProperty()
        {
            await TestAddPropertyAsync("class [|C|] { }", @"class C
{
    public string get_P(int i, int j)
    {
        $$
    }

    public void set_P(int i, int j, string value)
    {
    }
}",
                type: typeof(string),
                getStatements: "return String.Empty;",
                setStatements: "",
                parameters: Parameters(Parameter(typeof(int), "i"), Parameter(typeof(int), "j")));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToTypes()
        {
            await TestAddAttributeAsync("class [|C|] { }", @"[System.Serializable]
class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromTypes()
        {
            await TestRemoveAttributeAsync<SyntaxNode>(@"[System.Serializable] class [|C|] { }", "class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToMethods()
        {
            await TestAddAttributeAsync("class C { public void [|M()|] { } }", "class C { [System.Serializable] public void M() { } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromMethods()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public void [|M()|] { } }", "class C { public void M() { } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToFields()
        {
            await TestAddAttributeAsync("class C { [|public int F|]; }", "class C { [System.Serializable] public int F; }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromFields()
        {
            await TestRemoveAttributeAsync<FieldDeclarationSyntax>("class C { [System.Serializable] public int [|F|]; }", "class C { public int F; }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToProperties()
        {
            await TestAddAttributeAsync("class C { public int [|P|] { get; set; }}", "class C { [System.Serializable] public int P { get; set; } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromProperties()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public int [|P|] { get; set; }}", "class C { public int P { get; set; } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToPropertyAccessor()
        {
            await TestAddAttributeAsync("class C { public int P { [|get|]; set; }}", "class C { public int P { [System.Serializable] get; set; }}", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromPropertyAccessor()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { public int P { [System.Serializable] [|get|]; set; } }", "class C { public int P { get; set; } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToEnums()
        {
            await TestAddAttributeAsync("enum [|C|] { One, Two }", @"[System.Serializable]
enum C { One, Two }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromEnums()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("[System.Serializable] enum [|C|] { One, Two }", "enum C { One, Two }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToEnumMembers()
        {
            await TestAddAttributeAsync("enum C { [|One|], Two }", "enum C { [System.Serializable] One, Two }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromEnumMembers()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("enum C { [System.Serializable] [|One|], Two }", "enum C { One, Two }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToIndexer()
        {
            await TestAddAttributeAsync("class C { public int [|this[int y]|] { get; set; }}", "class C { [System.Serializable] public int this[int y] { get; set; } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromIndexer()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public int [|this[int y]|] { get; set; }}", "class C { public int this[int y] { get; set; } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToOperator()
        {
            await TestAddAttributeAsync("class C { public static C operator [|+|] (C c1, C c2) { return new C(); }}", "class C { [System.Serializable] public static C operator +(C c1, C c2) { return new C(); } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromOperator()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { [System.Serializable] public static C operator [|+|](C c1, C c2) { return new C(); }}", "class C { public static C operator +(C c1, C c2) { return new C(); } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToDelegate()
        {
            await TestAddAttributeAsync("delegate int [|D()|];", @"[System.Serializable]
delegate int D();", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromDelegate()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("[System.Serializable] delegate int [|D()|];", "delegate int D();", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToParam()
        {
            await TestAddAttributeAsync("class C { public void M([|int x|]) { } }", "class C { public void M([System.Serializable] int x) { } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromParam()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C { public void M([System.Serializable] [|int x|]) { } }", "class C { public void M(int x) { } }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToTypeParam()
        {
            await TestAddAttributeAsync("class C<[|T|]> { }", "class C<[System.Serializable] T> { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeFromTypeParam()
        {
            await TestRemoveAttributeAsync<SyntaxNode>("class C<[System.Serializable] [|T|]> { }", "class C<T> { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeToCompilationUnit()
        {
            await TestAddAttributeAsync("[|class C { } class D {} |]", @"[assembly: System.Serializable]

class C { }
class D { }", typeof(SerializableAttribute), AssemblyKeyword);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeWithWrongTarget()
        {
            await Assert.ThrowsAsync<AggregateException>(async () =>
                await TestAddAttributeAsync("[|class C { } class D {} |]", "", typeof(SerializableAttribute), RefKeyword));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddAttributeWithArrayParams()
        {
            await TestAddAttributeAsync("""
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
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeWithTrivia()
        {
            await TestRemoveAttributeAsync<SyntaxNode>(@"// Comment 1
[System.Serializable] // Comment 2
/* Comment 3*/ class [|C|] { }", @"// Comment 1
/* Comment 3*/ class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeWithTrivia_NewLine()
        {
            await TestRemoveAttributeAsync<SyntaxNode>(@"// Comment 1
[System.Serializable]
/* Comment 3*/ class [|C|] { }", @"// Comment 1
/* Comment 3*/ class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeWithMultipleAttributes()
        {
            await TestRemoveAttributeAsync<SyntaxNode>(@"// Comment 1
/*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
/* Comment 8*/
class [|C|] { }", @"// Comment 1
/*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
/* Comment 8*/
class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task RemoveAttributeWithMultipleAttributeLists()
        {
            await TestRemoveAttributeAsync<SyntaxNode>(@"// Comment 1
/*Comment2*/[ /*Comment3*/ System.Serializable /*Comment4*/, /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
[ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
/* Comment12*/
class [|C|] { }", @"// Comment 1
/*Comment2*/[ /*Comment3*/  /*Comment5*/System.Flags /*Comment6*/] /*Comment7*/
[ /*Comment9*/ System.Obsolete /*Comment10*/] /*Comment11*/
/* Comment12*/
class C { }", typeof(SerializableAttribute));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateModifiers()
        {
            var eol = SyntaxFactory.EndOfLine(@"");
            var newModifiers = new[] { InternalKeyword.WithLeadingTrivia(eol) }.Concat(
                CreateModifierTokens(new Editing.DeclarationModifiers(isSealed: true, isPartial: true), LanguageNames.CSharp));

            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(@"public static class [|C|] // Comment 1
{
    // Comment 2
}", @"internal partial sealed class C // Comment 1
{
    // Comment 2
}", modifiers: newModifiers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateAccessibility()
        {
            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(@"// Comment 0
public static class [|C|] // Comment 1
{
    // Comment 2
}", @"// Comment 0
internal static class C // Comment 1
{
    // Comment 2
}", accessibility: Accessibility.Internal);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationType()
        {
            await TestUpdateDeclarationAsync<MethodDeclarationSyntax>(@"
public static class C
{
    // Comment 1
    public static char [|F|]() { return 0; }
}", @"
public static class C
{
    // Comment 1
    public static int F() { return 0; }
}", getType: GetTypeSymbol(typeof(int)));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationMembers()
        {
            var getField = CreateField(Accessibility.Public, new Editing.DeclarationModifiers(), typeof(int), "f2");
            var getMembers = ImmutableArray.Create(getField);
            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(
                @"
public static class [|C|]
{
    // Comment 0
    public int {|RetainedMember:f|};

    // Comment 1
    public static char F() { return 0; }
}", @"
public static class C
{
    // Comment 0
    public int f;
    public int f2;
}", getNewMembers: getMembers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationMembers_DifferentOrder()
        {
            var getField = CreateField(Accessibility.Public, new Editing.DeclarationModifiers(), typeof(int), "f2");
            var getMembers = ImmutableArray.Create(getField);
            await TestUpdateDeclarationAsync<ClassDeclarationSyntax>(@"
public static class [|C|]
{
    // Comment 0
    public int f;

    // Comment 1
    public static char {|RetainedMember:F|}() { return 0; }
}", @"
public static class C
{
    public int f2;

    // Comment 1
    public static char F() { return 0; }
}", getNewMembers: getMembers, declareNewMembersAtTop: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public async Task SortAroundDestructor()
        {
            await TestGenerateFromSourceSymbolAsync("public class [|C|] { public C(){} public int this[int index]{get{return 0;}set{value = 0;}} }", "public class [|C|] { ~C(){} }", @"public class C {
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
}", onlyGenerateMembers: true);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public async Task SortOperators()
        {
            await TestGenerateFromSourceSymbolAsync(@"
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
}", "namespace [|N|] { }", @"namespace N
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
}",
                forceLanguage: LanguageNames.CSharp,
                context: new CodeGenerationContext(generateMethodBodies: false));
        }
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/665008")]
    public async Task TestExtensionMethods()
    {
        await TestGenerateFromSourceSymbolAsync(@"
public static class [|C|]
{
    public static void ExtMethod1(this string s, int y, string z) {}
}", "public static class [|C|] {}", @"public static class C
{
    public static void ExtMethod1(this string s, int y, string z);
}",
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530829")]
    public async Task TestVBPropertiesWithParams()
    {
        await TestGenerateFromSourceSymbolAsync(@"
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
", "namespace [|N|] {}", @"namespace N
{
    public class C
    {
        public virtual string get_IndexProp(int p1);
        public virtual void set_IndexProp(int p1, string value);
    }
}",
            context: new CodeGenerationContext(generateMethodBodies: false));
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/812738")]
    public async Task TestRefParamsWithDefaultValue()
    {
        await TestGenerateFromSourceSymbolAsync(@"
Public Class [|C|]
    Public Sub Goo(x As Integer, Optional ByRef y As Integer = 10, Optional ByRef z As Object = Nothing)
    End Sub
End Class", "public class [|C|] {}", @"public class C
{
    public void Goo(int x, ref int y, ref object z);
}",
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);
    }

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/848357")]
    public async Task TestConstraints()
    {
        await TestGenerateFromSourceSymbolAsync(@"
namespace N
{
    public class [|C|]<T, U> where T : struct where U : class
    {
        public void Goo<Q, R>() where Q : new() where R : IComparable { }
        public delegate void D<T, U>(T t, U u) where T : struct where U : class;
    }
}
", "namespace [|N|] {}", @"namespace N
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
}",
            context: new CodeGenerationContext(generateMethodBodies: false),
            onlyGenerateMembers: true);
    }
}
