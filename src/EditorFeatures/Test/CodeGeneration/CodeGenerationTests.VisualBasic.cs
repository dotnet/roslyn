// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration;

public sealed partial class CodeGenerationTests
{
    [UseExportProvider]
    public class VisualBasic
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddNamespace()
            => TestAddNamespaceAsync("Namespace [|N1|]\n End Namespace", """
                Namespace N1
                    Namespace N2
                    End Namespace
                End Namespace
                """,
                name: "N2");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddField()
            => TestAddFieldAsync("Class [|C|]\n End Class", """
                Class C
                    Public F As Integer
                End Class
                """,
                type: GetTypeSymbol(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddSharedField()
            => TestAddFieldAsync("Class [|C|]\n End Class", """
                Class C
                    Private Shared F As String
                End Class
                """,
                type: GetTypeSymbol(typeof(string)),
                accessibility: Accessibility.Private,
                modifiers: new DeclarationModifiers(isStatic: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddArrayField()
            => TestAddFieldAsync("Class [|C|]\n End Class", """
                Class C
                    Public F As Integer()
                End Class
                """,
                type: CreateArrayType(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructor()
            => TestAddConstructorAsync("Class [|C|]\n End Class", """
                Class C
                    Public Sub New()
                    End Sub
                End Class
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530785")]
        public Task AddConstructorWithXmlComment()
            => TestAddConstructorAsync("""
                Public Class [|C|]
                ''' <summary>
                ''' Do Nothing
                ''' </summary>
                Public Sub GetStates()
                End Sub
                End Class
                """, """
                Public Class C
                    Public Sub New()
                    End Sub
                    ''' <summary>
                    ''' Do Nothing
                    ''' </summary>
                    Public Sub GetStates()
                End Sub
                End Class
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructorWithoutBody()
            => TestAddConstructorAsync("Class [|C|]\n End Class", """
                Class C
                    Public Sub New()
                End Class
                """,
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddConstructorResolveNamespaceImport()
            => TestAddConstructorAsync("Class [|C|]\n End Class", """
                Imports System.Text

                Class C
                    Public Sub New(s As StringBuilder)
                    End Sub
                End Class
                """,
                parameters: Parameters(Parameter(typeof(StringBuilder), "s")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddSharedConstructor()
            => TestAddConstructorAsync("Class [|C|]\n End Class", """
                Class C
                    Shared Sub New()
                    End Sub
                End Class
                """,
                modifiers: new DeclarationModifiers(isStatic: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddChainedConstructor()
            => TestAddConstructorAsync("Class [|C|]\n Public Sub New(i As Integer)\n End Sub\n End Class", """
                Class C
                    Public Sub New()
                        Me.New(42)
                    End Sub

                    Public Sub New(i As Integer)
                 End Sub
                 End Class
                """,
                thisArguments: [VB.SyntaxFactory.ParseExpression("42")]);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544476")]
        public Task AddClass()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Class C
                    End Class
                End Namespace
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddClassEscapeName()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Class [Class]
                    End Class
                End Namespace
                """,
                name: "Class");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddClassUnicodeName()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Class [Class]
                    End Class
                End Namespace
                """,
                name: "Class");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544477")]
        public Task AddNotInheritableClass()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public NotInheritable Class C
                    End Class
                End Namespace
                """,
                modifiers: new DeclarationModifiers(isSealed: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544477")]
        public Task AddMustInheritClass()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Friend MustInherit Class C
                    End Class
                End Namespace
                """,
                accessibility: Accessibility.Internal,
                modifiers: new DeclarationModifiers(isAbstract: true));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStructure()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Friend Structure S
                    End Structure
                End Namespace
                """,
                name: "S",
                accessibility: Accessibility.Internal,
                typeKind: TypeKind.Struct);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public Task AddSealedStructure()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Structure S
                    End Structure
                End Namespace
                """,
                name: "S",
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isSealed: true),
                typeKind: TypeKind.Struct);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddInterface()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Interface I
                    End Interface
                End Namespace
                """,
                name: "I",
                typeKind: TypeKind.Interface);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544528")]
        public Task AddEnum()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Enum E
                        F1
                    End Enum
                End Namespace
                """, "E",
                typeKind: TypeKind.Enum,
                members: Members(CreateEnumField("F1", null)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544527")]
        public Task AddEnumWithValues()
            => TestAddNamedTypeAsync("Namespace [|N|]\n End Namespace", """
                Namespace N
                    Public Enum E
                        F1 = 1
                        F2 = 2
                    End Enum
                End Namespace
                """, "E",
                typeKind: TypeKind.Enum,
                members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEnumMember()
            => TestAddFieldAsync("Public Enum [|E|]\n F1 = 1\n F2 = 2\n End Enum", """
                Public Enum E
                 F1 = 1
                 F2 = 2
                    F3
                End Enum
                """,
                name: "F3");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEnumMemberWithValue()
            => TestAddFieldAsync("Public Enum [|E|]\n F1 = 1\n F2\n End Enum", """
                Public Enum E
                 F1 = 1
                 F2
                    F3 = 3
                End Enum
                """,
                name: "F3", hasConstantValue: true, constantValue: 3);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544529")]
        public Task AddDelegateType()
            => TestAddDelegateTypeAsync("Class [|C|]\n End Class", """
                Class C
                    Public Delegate Function D(s As String) As Integer
                End Class
                """,
                returnType: typeof(int),
                parameters: Parameters(Parameter(typeof(string), "s")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546224")]
        public Task AddSealedDelegateType()
            => TestAddDelegateTypeAsync("Class [|C|]\n End Class", """
                Class C
                    Public Delegate Function D(s As String) As Integer
                End Class
                """,
                returnType: typeof(int),
                modifiers: new DeclarationModifiers(isSealed: true),
                parameters: Parameters(Parameter(typeof(string), "s")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEvent()
            => TestAddEventAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Event E As Action
                End Class
                """,
                context: new CodeGenerationContext(addImports: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddCustomEventToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                Public Class [|C2|]
                    Public Custom Event Click As EventHandler
                        AddHandler(ByVal value As EventHandler)
                            Events.AddHandler("ClickEvent", value)
                        End AddHandler
                        RemoveHandler(ByVal value As EventHandler)
                            Events.RemoveHandler("ClickEvent", value)
                        End RemoveHandler
                        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                            CType(Events("ClickEvent"), EventHandler).Invoke(sender, e)
                        End RaiseEvent
                    End Event
                End Class
                """, "Class [|C1|]\nEnd Class", """
                Class C1
                    Public Custom Event Click As EventHandler
                        AddHandler(ByVal value As EventHandler)
                            Events.AddHandler("ClickEvent", value)
                        End AddHandler
                        RemoveHandler(ByVal value As EventHandler)
                            Events.RemoveHandler("ClickEvent", value)
                        End RemoveHandler
                        RaiseEvent(ByVal sender As Object, ByVal e As EventArgs)
                            CType(Events("ClickEvent"), EventHandler).Invoke(sender, e)
                        End RaiseEvent
                    End Event
                End Class
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddEventWithAccessorAndImplementsClause()
        {
            static ImmutableArray<IEventSymbol> GetExplicitInterfaceEvent(SemanticModel semanticModel)
            {
                return [new CodeGenerationEventSymbol(
                        GetTypeSymbol(typeof(System.ComponentModel.INotifyPropertyChanged))(semanticModel),
                        attributes: default,
                        Accessibility.Public,
                        modifiers: default,
                        GetTypeSymbol(typeof(System.ComponentModel.PropertyChangedEventHandler))(semanticModel),
                        explicitInterfaceImplementations: default,
                        nameof(System.ComponentModel.INotifyPropertyChanged.PropertyChanged), null, null, null)];
            }

            await TestAddEventAsync("Class [|C|] \n End Class", """
                Class C
                    Public Custom Event E As ComponentModel.PropertyChangedEventHandler Implements ComponentModel.INotifyPropertyChanged.PropertyChanged
                        AddHandler(value As ComponentModel.PropertyChangedEventHandler)
                        End AddHandler
                        RemoveHandler(value As ComponentModel.PropertyChangedEventHandler)
                        End RemoveHandler
                        RaiseEvent(sender As Object, e As ComponentModel.PropertyChangedEventArgs)
                        End RaiseEvent
                    End Event
                End Class
                """,
                addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    [], Accessibility.NotApplicable, []),
                    getExplicitInterfaceImplementations: GetExplicitInterfaceEvent,
                    type: typeof(System.ComponentModel.PropertyChangedEventHandler),
                    context: new CodeGenerationContext(addImports: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddEventWithAddAccessor()
            => TestAddEventAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Custom Event E As Action
                        AddHandler(value As Action)
                        End AddHandler
                        RemoveHandler(value As Action)
                        End RemoveHandler
                        RaiseEvent()
                        End RaiseEvent
                    End Event
                End Class
                """,
                addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol([], Accessibility.NotApplicable, []),
                context: new CodeGenerationContext(addImports: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddEventWithAccessors()
        {
            var addStatements = ImmutableArray.Create<SyntaxNode>(VB.SyntaxFactory.ParseExecutableStatement("Console.WriteLine(0)"));
            var removeStatements = ImmutableArray.Create<SyntaxNode>(VB.SyntaxFactory.ParseExecutableStatement("Console.WriteLine(1)"));
            var raiseStatements = ImmutableArray.Create<SyntaxNode>(VB.SyntaxFactory.ParseExecutableStatement("Console.WriteLine(2)"));
            await TestAddEventAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Custom Event E As Action
                        AddHandler(value As Action)
                            Console.WriteLine(0)
                        End AddHandler
                        RemoveHandler(value As Action)
                            Console.WriteLine(1)
                        End RemoveHandler
                        RaiseEvent()
                            Console.WriteLine(2)
                        End RaiseEvent
                    End Event
                End Class
                """,
                addMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    [], Accessibility.NotApplicable, addStatements),
                removeMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    [], Accessibility.NotApplicable, removeStatements),
                raiseMethod: CodeGenerationSymbolFactory.CreateAccessorSymbol(
                    [], Accessibility.NotApplicable, raiseStatements),
                context: new CodeGenerationContext(addImports: false));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodToClass()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Public Sub M()
                    End Sub
                End Class
                """,
                returnType: typeof(void));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddMethodToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                Public Class [|C2|]
                    Public Function FInt() As Integer
                        Return 0
                    End Function
                End Class
                """, "Class [|C1|]\nEnd Class", """
                Class C1
                    Public Function FInt() As Integer
                        Return 0
                    End Function
                End Class
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodToClassEscapedName()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Protected Friend Sub [Sub]()
                    End Sub
                End Class
                """,
                accessibility: Accessibility.ProtectedOrInternal,
                name: "Sub",
                returnType: typeof(void));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544477")]
        public Task AddSharedMethodToStructure()
            => TestAddMethodAsync("Structure [|S|]\n End Structure", """
                Structure S
                    Public Shared Function M() As Integer
                        Return 0
                    End Function
                End Structure
                """,
                modifiers: new DeclarationModifiers(isStatic: true),
                returnType: typeof(int),
                statements: "Return 0");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddNotOverridableOverridesMethod()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Public NotOverridable Overrides Function GetHashCode() As Integer
                        $$
                    End Function
                End Class
                """,
                name: "GetHashCode",
                modifiers: new DeclarationModifiers(isOverride: true, isSealed: true),
                returnType: typeof(int),
                statements: "Return 0");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMustOverrideMethod()
            => TestAddMethodAsync("MustInherit Class [|C|]\n End Class", "MustInherit Class C\n    Public MustOverride Sub M()\nEnd Class",
                modifiers: new DeclarationModifiers(isAbstract: true),
                returnType: typeof(void));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddMethodWithoutBody()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Public Sub M()
                End Class
                """,
                returnType: typeof(void),
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddGenericMethod()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Public Function M(Of T)() As Integer
                        $$
                    End Function
                End Class
                """,
                returnType: typeof(int),
                typeParameters: [CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T")],
                statements: "Return new T().GetHashCode()");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddVirtualMethod()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Protected Overridable Function M() As Integer
                        $$
                    End Function
                End Class
                """,
                accessibility: Accessibility.Protected,
                modifiers: new DeclarationModifiers(isVirtual: true),
                returnType: typeof(int),
                statements: "Return 0");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddShadowsMethod()
            => TestAddMethodAsync("Class [|C|]\n End Class", """
                Class C
                    Public Shadows Function ToString() As String
                        $$
                    End Function
                End Class
                """,
                name: "ToString",
                accessibility: Accessibility.Public,
                modifiers: new DeclarationModifiers(isNew: true),
                returnType: typeof(string),
                statements: "Return String.Empty");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddExplicitImplementation()
        {
            var input = "Interface I\n Sub M(i As Integer)\n End Interface\n Class [|C|]\n Implements I\n End Class";
            await TestAddMethodAsync(input, """
                Interface I
                 Sub M(i As Integer)
                 End Interface
                 Class C
                 Implements I

                    Public Sub M(i As Integer) Implements I.M
                    End Sub
                End Class
                """,
                name: "M",
                returnType: typeof(void),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getExplicitInterfaces: s => [.. s.LookupSymbols(input.IndexOf('M'), null, "M").OfType<IMethodSymbol>()]);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddTrueFalseOperators()
            => TestAddOperatorsAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Shared Operator IsTrue(other As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator IsFalse(other As C) As Boolean
                        $$
                    End Operator
                End Class
                """,
                [CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False],
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(bool),
                statements: "Return False");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnaryOperators()
            => TestAddOperatorsAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Shared Operator +(other As C) As Object
                        $$
                    End Operator

                    Public Shared Operator -(other As C) As Object
                        $$
                    End Operator

                    Public Shared Operator Not(other As C) As Object
                        $$
                    End Operator
                End Class
                """,
                [
                    CodeGenerationOperatorKind.UnaryPlus,
                    CodeGenerationOperatorKind.UnaryNegation,
                    CodeGenerationOperatorKind.LogicalNot
                ],
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(object),
                statements: "Return Nothing");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddBinaryOperators()
            => TestAddOperatorsAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Shared Operator +(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator -(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator *(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator /(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator \(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator ^(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator &(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator Like(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator Mod(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator And(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator Or(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator Xor(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator <<(a As C, b As C) As Object
                        $$
                    End Operator

                    Public Shared Operator >>(a As C, b As C) As Object
                        $$
                    End Operator
                End Class
                """,
                [
                    CodeGenerationOperatorKind.Addition,
                    CodeGenerationOperatorKind.Subtraction,
                    CodeGenerationOperatorKind.Multiplication,
                    CodeGenerationOperatorKind.Division,
                    CodeGenerationOperatorKind.IntegerDivision,
                    CodeGenerationOperatorKind.Exponent,
                    CodeGenerationOperatorKind.Concatenate,
                    CodeGenerationOperatorKind.Like,
                    CodeGenerationOperatorKind.Modulus,
                    CodeGenerationOperatorKind.BitwiseAnd,
                    CodeGenerationOperatorKind.BitwiseOr,
                    CodeGenerationOperatorKind.ExclusiveOr,
                    CodeGenerationOperatorKind.LeftShift,
                    CodeGenerationOperatorKind.RightShift
                ],
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(object),
                statements: "Return Nothing");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddComparisonOperators()
            => TestAddOperatorsAsync("""
                Class [|C|]
                End Class
                """, """
                Class C
                    Public Shared Operator =(a As C, b As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator <>(a As C, b As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator >(a As C, b As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator <(a As C, b As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator >=(a As C, b As C) As Boolean
                        $$
                    End Operator

                    Public Shared Operator <=(a As C, b As C) As Boolean
                        $$
                    End Operator
                End Class
                """,
                [
                    CodeGenerationOperatorKind.Equality,
                    CodeGenerationOperatorKind.Inequality,
                    CodeGenerationOperatorKind.GreaterThan,
                    CodeGenerationOperatorKind.LessThan,
                    CodeGenerationOperatorKind.GreaterThanOrEqual,
                    CodeGenerationOperatorKind.LessThanOrEqual
                ],
                parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                returnType: typeof(bool),
                statements: "Return True");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddUnsupportedOperator()
            => TestAddUnsupportedOperatorAsync("Class [|C|]\n End Class",
                operatorKind: CodeGenerationOperatorKind.Increment,
                parameters: Parameters(Parameter("C", "other")),
                returnType: typeof(bool),
                statements: "Return True");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddExplicitConversion()
            => TestAddConversionAsync("Class [|C|]\n End Class", """
                Class C
                    Public Shared Narrowing Operator CType(other As C) As Integer
                        $$
                    End Operator
                End Class
                """,
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                statements: "Return 0");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddImplicitConversion()
            => TestAddConversionAsync("Class [|C|]\n End Class", """
                Class C
                    Public Shared Widening Operator CType(other As C) As Integer
                        $$
                    End Operator
                End Class
                """,
                toType: typeof(int),
                fromType: Parameter("C", "other"),
                isImplicit: true,
                statements: "Return 0");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStatementsToSub()
            => TestAddStatementsAsync("Class C\n [|Public Sub M\n Console.WriteLine(1)\n End Sub|]\n End Class", """
                Class C
                 Public Sub M
                 Console.WriteLine(1)
                $$ End Sub
                 End Class
                """, "Console.WriteLine(2)");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStatementsToOperator()
            => TestAddStatementsAsync("Class C\n [|Shared Operator +(arg As C) As C\n Return arg\n End Operator|]\n End Class", """
                Class C
                 Shared Operator +(arg As C) As C
                 Return arg
                $$ End Operator
                 End Class
                """, "Return Nothing");

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddStatementsToPropertySetter()
            => TestAddStatementsAsync("Imports System\n Class C\n WriteOnly Property P As String\n [|Set\n End Set|]\n End Property\n End Class", """
                Imports System
                 Class C
                 WriteOnly Property P As String
                 Set
                $$ End Set
                 End Property
                 End Class
                """, """
                Console.WriteLine("Setting the value"
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddParametersToMethod()
            => TestAddParametersAsync("Class C\n Public [|Sub M()\n End Sub|]\n End Class", """
                Class C
                 Public Sub M(numAs Integer, OptionaltextAs String = "Hello!",OptionalfloatingAs Single = 0.5)
                 End Sub
                 End Class
                """,
                Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5F)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844460")]
        public Task AddParametersToPropertyBlock()
            => TestAddParametersAsync("Class C\n [|Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property|]\n End Class", """
                Class C
                 Public Property P (numAs Integer) As String
                 Get
                 Return String.Empty
                 End Get
                 Set(value As String)
                 End Set
                 End Property
                 End Class
                """,
                Parameters(Parameter(typeof(int), "num")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844460")]
        public Task AddParametersToPropertyStatement()
            => TestAddParametersAsync("Class C\n [|Public Property P As String|]\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class", """
                Class C
                 Public Property P (numAs Integer) As String
                 Get
                 Return String.Empty
                 End Get
                 Set(value As String)
                 End Set
                 End Property
                 End Class
                """,
                Parameters(Parameter(typeof(int), "num")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844460")]
        public Task AddParametersToPropertyGetter_ShouldNotSucceed()
            => TestAddParametersAsync("Class C\n Public Property P As String\n [|Get\n Return String.Empty\n End Get|]\n Set(value As String)\n End Set\n End Property\n End Class", "Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class",
                Parameters(Parameter(typeof(int), "num")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/844460")]
        public Task AddParametersToPropertySetter_ShouldNotSucceed()
            => TestAddParametersAsync("Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n [|Set(value As String)\n End Set|]\n End Property\n End Class", "Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class",
                Parameters(Parameter(typeof(int), "num")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddParametersToOperator()
            => TestAddParametersAsync("Class C\n [|Shared Operator +(a As C) As C\n Return a\n End Operator|]\n End Class", """
                Class C
                 Shared Operator +(a As C,bAs C) As C
                 Return a
                 End Operator
                 End Class
                """,
                Parameters(Parameter("C", "b")));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAutoProperty()
            => TestAddPropertyAsync("Class [|C|]\n End Class", """
                Class C
                    Public Property P As Integer
                End Class
                """,
                type: typeof(int));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task AddPropertyToClassFromSourceSymbol()
        {
            var context = new CodeGenerationContext(reuseSyntax: true);
            await TestGenerateFromSourceSymbolAsync("""
                Public Class [|C2|]
                    Public Property P As Integer
                        Get
                            Return 0
                        End Get
                    End Property
                End Class
                """, "Class [|C1|]\nEnd Class", """
                Class C1
                    Public Property P As Integer
                        Get
                            Return 0
                        End Get
                    End Property
                End Class
                """, onlyGenerateMembers: true, context: context);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddPropertyWithoutAccessorBodies()
            => TestAddPropertyAsync("Class [|C|]\n End Class", """
                Class C
                    Public Property P As Integer
                End Class
                """,
                type: typeof(int),
                getStatements: "Return 0",
                setStatements: "Me.P = Value",
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddIndexer()
            => TestAddPropertyAsync("Class [|C|]\n End Class", """
                Class C
                    Default Public ReadOnly Property Item(i As Integer) As String
                        Get
                            $$
                        End Get
                    End Property
                End Class
                """,
                name: "Item",
                type: typeof(string),
                parameters: Parameters(Parameter(typeof(int), "i")),
                getStatements: "Return String.Empty",
                isIndexer: true);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToTypes()
            => TestAddAttributeAsync("Class [|C|]\n End Class", """
                <Serializable>
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromTypes()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                <Serializable>
                Class [|C|]
                End Class
                """, """
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToMethods()
            => TestAddAttributeAsync("Class C\n Public Sub [|M()|] \n End Sub \n End Class", """
                Class C
                    <Serializable>
                    Public Sub M()
                    End Sub 
                 End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromMethods()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Class C
                    <Serializable>
                    Public Sub [|M()|]
                    End Sub
                End Class
                """, """
                Class C
                    Public Sub M()
                    End Sub
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToFields()
            => TestAddAttributeAsync("Class C\n [|Public F As Integer|]\n End Class", """
                Class C
                    <Serializable>
                    Public F As Integer
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromFields()
            => TestRemoveAttributeAsync<FieldDeclarationSyntax>("""
                Class C
                    <Serializable>
                    Public [|F|] As Integer
                End Class
                """, """
                Class C
                    Public F As Integer
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToProperties()
            => TestAddAttributeAsync("Class C \n Public Property [|P|] As Integer \n End Class", """
                Class C
                    <Serializable>
                    Public Property P As Integer
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromProperties()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Class C
                    <Serializable>
                    Public Property [|P|] As Integer
                End Class
                """, """
                Class C
                    Public Property P As Integer
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToPropertyAccessor()
            => TestAddAttributeAsync("Class C \n Public ReadOnly Property P As Integer \n [|Get|] \n Return 10 \n End Get \n End Property \n  End Class", """
                Class C 
                 Public ReadOnly Property P As Integer
                        <Serializable>
                        Get
                            Return 10 
                 End Get 
                 End Property 
                  End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromPropertyAccessor()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Class C
                    Public Property P As Integer
                        <Serializable>
                        [|Get|]
                            Return 10
                        End Get
                End Class
                """, """
                Class C
                    Public Property P As Integer
                        Get
                            Return 10
                        End Get
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToEnums()
            => TestAddAttributeAsync("Module M \n [|Enum C|] \n One \n Two \n End Enum\n End Module", """
                Module M
                    <Serializable>
                    Enum C
                        One 
                 Two 
                 End Enum
                 End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromEnums()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Module M
                    <Serializable>
                    Enum [|C|]
                        One
                        Two
                    End Enum
                End Module
                """, """
                Module M
                    Enum C
                        One
                        Two
                    End Enum
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToEnumMembers()
            => TestAddAttributeAsync("Module M \n Enum C \n [|One|] \n Two \n End Enum\n End Module", """
                Module M 
                 Enum C
                        <Serializable>
                        One
                        Two 
                 End Enum
                 End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromEnumMembers()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Module M
                    Enum C
                        <Serializable>
                        [|One|]
                        Two
                    End Enum
                End Module
                """, """
                Module M
                    Enum C
                        One
                        Two
                    End Enum
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToModule()
            => TestAddAttributeAsync("Module [|M|] \n End Module", """
                <Serializable>
                Module M
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromModule()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                <Serializable>
                Module [|M|]
                End Module
                """, """
                Module M
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToOperator()
            => TestAddAttributeAsync("Class C \n Public Shared Operator [|+|] (x As C, y As C) As C \n Return New C() \n End Operator \n End Class", """
                Class C
                    <Serializable>
                    Public Shared Operator +(x As C, y As C) As C
                        Return New C() 
                 End Operator 
                 End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromOperator()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Module M
                    Class C
                        <Serializable>
                        Public Shared Operator [|+|](x As C, y As C) As C
                            Return New C()
                        End Operator
                    End Class
                End Module
                """, """
                Module M
                    Class C
                        Public Shared Operator +(x As C, y As C) As C
                            Return New C()
                        End Operator
                    End Class
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToDelegate()
            => TestAddAttributeAsync("Module M \n Delegate Sub [|D()|]\n End Module", "Module M\n    <Serializable>\n    Delegate Sub D()\nEnd Module", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromDelegate()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Module M
                    <Serializable>
                    Delegate Sub [|D()|]
                End Module
                """, """
                Module M
                    Delegate Sub D()
                End Module
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToParam()
            => TestAddAttributeAsync("Class C \n Public Sub M([|x As Integer|]) \n End Sub \n End Class", "Class C \n Public Sub M(<Serializable> x As Integer) \n End Sub \n End Class", typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeFromParam()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                Class C
                    Public Sub M(<Serializable> [|x As Integer|])
                    End Sub
                End Class
                """, """
                Class C
                    Public Sub M(x As Integer)
                    End Sub
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeToCompilationUnit()
            => TestAddAttributeAsync("[|Class C \n End Class \n Class D \n End Class|]", "<Assembly: Serializable>\nClass C\nEnd Class\nClass D\nEnd Class", typeof(SerializableAttribute), VB.SyntaxFactory.Token(VB.SyntaxKind.AssemblyKeyword));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task AddAttributeWithWrongTarget()
            => Assert.ThrowsAsync<AggregateException>(async () =>
                await TestAddAttributeAsync("[|Class C \n End Class \n Class D \n End Class|]", "<Assembly: Serializable> Class C \n End Class \n Class D \n End Class", typeof(SerializableAttribute), VB.SyntaxFactory.Token(VB.SyntaxKind.ReturnKeyword)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithTrivia()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                ' Comment 1
                <System.Serializable> ' Comment 2
                Class [|C|]
                End Class
                """, """
                ' Comment 1
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithTrivia_NewLine()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                ' Comment 1
                <System.Serializable>
                Class [|C|]
                End Class
                """, """
                ' Comment 1
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithMultipleAttributes()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                ' Comment 1
                <  System.Serializable   ,  System.Flags> ' Comment 2
                Class [|C|]
                End Class
                """, """
                ' Comment 1
                <System.Flags> ' Comment 2
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task RemoveAttributeWithMultipleAttributeLists()
            => TestRemoveAttributeAsync<SyntaxNode>("""
                ' Comment 1
                <  System.Serializable   ,  System.Flags> ' Comment 2
                <System.Obsolete> ' Comment 3
                Class [|C|]
                End Class
                """, """
                ' Comment 1
                <System.Flags> ' Comment 2
                <System.Obsolete> ' Comment 3
                Class C
                End Class
                """, typeof(SerializableAttribute));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateModifiers()
        {
            var eol = VB.SyntaxFactory.EndOfLine(@"");
            var newModifiers = new[] { VB.SyntaxFactory.Token(VB.SyntaxKind.FriendKeyword).WithLeadingTrivia(eol) }.Concat(
                CreateModifierTokens(new DeclarationModifiers(isSealed: true, isPartial: true), LanguageNames.VisualBasic));

            await TestUpdateDeclarationAsync<ClassStatementSyntax>("""
                Public Shared Class [|C|] ' Comment 1
                    ' Comment 2
                End Class
                """, """
                Friend Partial NotInheritable Class C ' Comment 1
                    ' Comment 2
                End Class
                """, modifiers: newModifiers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task TestUpdateAccessibility()
            => TestUpdateDeclarationAsync<ClassStatementSyntax>("""
                ' Comment 0
                Public Shared Class [|C|] ' Comment 1
                    ' Comment 2
                End Class
                """, """
                ' Comment 0
                Protected Friend Shared Class C ' Comment 1
                    ' Comment 2
                End Class
                """, accessibility: Accessibility.ProtectedOrFriend);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public Task TestUpdateDeclarationType()
            => TestUpdateDeclarationAsync<MethodStatementSyntax>("""
                Public Shared Class C
                    ' Comment 1
                    Public Shared Function [|F|]() As Char
                        Return 0
                    End Function
                End Class
                """, """
                Public Shared Class C
                    ' Comment 1
                    Public Shared Function F() As Integer
                        Return 0
                    End Function
                End Class
                """, getType: GetTypeSymbol(typeof(int)));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        public async Task TestUpdateDeclarationMembers()
        {
            var getField = CreateField(Accessibility.Public, new DeclarationModifiers(isStatic: true), typeof(int), "f2");
            var getMembers = ImmutableArray.Create(getField);
            await TestUpdateDeclarationAsync<ClassBlockSyntax>("""
                Public Shared Class [|C|]
                    ' Comment 0
                    Public Shared {|RetainedMember:f|} As Integer

                    ' Comment 1
                    Public Shared Function F() As Char
                        Return 0
                    End Function
                End Class
                """, """
                Public Shared Class C
                    ' Comment 0
                    Public Shared f As Integer
                    Public Shared f2 As Integer
                End Class
                """, getNewMembers: getMembers);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public Task SortModules()
            => TestGenerateFromSourceSymbolAsync("Public Class [|C|] \n End Class", "Namespace [|N|] \n Module M \n End Module \n End Namespace", """
                Namespace N
                    Public Class C
                    End Class

                    Module M 
                 End Module 
                 End Namespace
                """);

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
        public Task SortOperators()
            => TestGenerateFromSourceSymbolAsync("""
                Namespace N
                    Public Class [|C|]
                        ' Unary operators
                        Public Shared Operator IsFalse(other As C) As Boolean
                            Return False
                        End Operator
                        Public Shared Operator IsTrue(other As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator Not(other As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator -(other As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator + (other As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Narrowing Operator CType(c As C) As Integer
                            Return 0
                        End Operator

                        ' Binary operators
                        Public Shared Operator >= (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator <= (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator > (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator < (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator <> (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator = (a As C, b As C) As Boolean
                            Return True
                        End Operator
                        Public Shared Operator >> (a As C, shift As Integer) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator << (a As C, shift As Integer) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator Xor(a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator Or(a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator And(a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator Mod(a As C, b As C) As C
                            Return Nothing
                        End Operator
                          Public Shared Operator Like (a As C, b As C) As C
                            Return Nothing
                        End Operator
                          Public Shared Operator & (a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator ^ (a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator \ (a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator / (a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator *(a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator -(a As C, b As C) As C
                            Return Nothing
                        End Operator
                        Public Shared Operator + (a As C, b As C) As C
                            Return Nothing
                        End Operator
                    End Class
                End Namespace
                """, "Namespace [|N|] \n End Namespace", """
                Namespace N
                    Public Class C
                        Public Shared Operator +(other As C) As C
                        Public Shared Operator +(a As C, b As C) As C
                        Public Shared Operator -(other As C) As C
                        Public Shared Operator -(a As C, b As C) As C
                        Public Shared Operator *(a As C, b As C) As C
                        Public Shared Operator /(a As C, b As C) As C
                        Public Shared Operator \(a As C, b As C) As C
                        Public Shared Operator ^(a As C, b As C) As C
                        Public Shared Operator &(a As C, b As C) As C
                        Public Shared Operator Not(other As C) As C
                        Public Shared Operator Like(a As C, b As C) As C
                        Public Shared Operator Mod(a As C, b As C) As C
                        Public Shared Operator And(a As C, b As C) As C
                        Public Shared Operator Or(a As C, b As C) As C
                        Public Shared Operator Xor(a As C, b As C) As C
                        Public Shared Operator <<(a As C, shift As Integer) As C
                        Public Shared Operator >>(a As C, shift As Integer) As C
                        Public Shared Operator =(a As C, b As C) As Boolean
                        Public Shared Operator <>(a As C, b As C) As Boolean
                        Public Shared Operator >(a As C, b As C) As Boolean
                        Public Shared Operator <(a As C, b As C) As Boolean
                        Public Shared Operator >=(a As C, b As C) As Boolean
                        Public Shared Operator <=(a As C, b As C) As Boolean
                        Public Shared Operator IsTrue(other As C) As Boolean
                        Public Shared Operator IsFalse(other As C) As Boolean
                        Public Shared Narrowing Operator CType(c As C) As Integer
                    End Class
                End Namespace
                """,
                forceLanguage: LanguageNames.VisualBasic,
                context: new CodeGenerationContext(generateMethodBodies: false));

        [Fact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
        [WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/848357")]
        public Task TestConstraints()
            => TestGenerateFromSourceSymbolAsync("""
                Namespace N
                    Public Class [|C|](Of T As Structure, U As Class)
                        Public Sub Goo(Of Q As New, R As IComparable)()
                        End Sub
                        Public Delegate Sub D(Of T1 As Structure, T2 As Class)(t As T1, u As T2)
                    End Class
                End Namespace
                """, "Namespace [|N|] \n End Namespace", """
                Namespace N
                    Public Class C(Of T As Structure, U As Class)
                        Public Sub Goo(Of Q As New, R As IComparable)()
                        Public Delegate Sub D(Of T1 As Structure, T2 As Class)(t As T1, u As T2)
                    End Class
                End Namespace
                """,
                context: new CodeGenerationContext(generateMethodBodies: false),
                onlyGenerateMembers: true);
    }
}
