// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Roslyn.Test.Utilities;
using Xunit;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeGeneration
{
    public partial class CodeGenerationTests
    {
        public class VisualBasic
        {
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddNamespace()
            {
                var input = "Namespace [|N1|]\n End Namespace";
                var expected = "Namespace N1\n Namespace N2\n End Namespace\n End Namespace";
                TestAddNamespace(input, expected,
                    name: "N2");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddField()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public F As Integer\n End Class";
                TestAddField(input, expected,
                    type: GetTypeSymbol(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddSharedField()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Private Shared F As String\n End Class";
                TestAddField(input, expected,
                    type: GetTypeSymbol(typeof(string)),
                    accessibility: Accessibility.Private,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddArrayField()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public F As Integer()\n End Class";
                TestAddField(input, expected,
                    type: CreateArrayType(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructor()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Sub New()\n End Sub\n End Class";
                TestAddConstructor(input, expected);
            }

            [WorkItem(530785)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructorWithXmlComment()
            {
                var input = @"
Public Class [|C|]
''' <summary>
''' Do Nothing
''' </summary>
Public Sub GetStates()
End Sub
End Class";
                var expected = @"
Public Class C
    Public Sub New()
    End Sub
    ''' <summary>
    ''' Do Nothing
    ''' </summary>
    Public Sub GetStates()
End Sub
End Class";
                TestAddConstructor(input, expected, compareTokens: false);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructorWithoutBody()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Sub New()\n End Class";
                TestAddConstructor(input, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddConstructorResolveNamespaceImport()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Imports System.Text\n Class C\n Public Sub New(s As StringBuilder)\n End Sub\n End Class";
                TestAddConstructor(input, expected,
                    parameters: Parameters(Parameter(typeof(StringBuilder), "s")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddSharedConstructor()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Shared Sub New()\n End Sub\n End Class";
                TestAddConstructor(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddChainedConstructor()
            {
                var input = "Class [|C|]\n Public Sub New(i As Integer)\n End Sub\n End Class";
                var expected = "Class C\n Public Sub New()\n Me.New(42)\n End Sub\n Public Sub New(i As Integer)\n End Sub\n End Class";
                TestAddConstructor(input, expected,
                    thisArguments: new[] { VB.SyntaxFactory.ParseExpression("42") });
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544476)]
            public void AddClass()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = @"Namespace N
    Public Class C
    End Class
End Namespace";
                TestAddNamedType(input, expected,
                    compareTokens: false);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddClassEscapeName()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Class [Class]\n End Class\n End Namespace";
                TestAddNamedType(input, expected,
                    name: "Class");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddClassUnicodeName()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Class [Class]\n End Class\n End Namespace";
                TestAddNamedType(input, expected,
                    name: "Cl\u0061ss");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544477)]
            public void AddNotInheritableClass()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public NotInheritable Class C\n End Class\n End Namespace";
                TestAddNamedType(input, expected,
                    modifiers: new DeclarationModifiers(isSealed: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544477)]
            public void AddMustInheritClass()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Friend MustInherit Class C\n End Class\n End Namespace";
                TestAddNamedType(input, expected,
                    accessibility: Accessibility.Internal,
                    modifiers: new DeclarationModifiers(isAbstract: true));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStructure()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Friend Structure S\n End Structure\n End Namespace";
                TestAddNamedType(input, expected,
                    name: "S",
                    accessibility: Accessibility.Internal,
                    typeKind: TypeKind.Struct);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public void AddSealedStructure()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Structure S\n End Structure\n End Namespace";
                TestAddNamedType(input, expected,
                    name: "S",
                    accessibility: Accessibility.Public,
                    modifiers: new DeclarationModifiers(isSealed: true),
                    typeKind: TypeKind.Struct);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddInterface()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Interface I\n End Interface\n End Namespace";
                TestAddNamedType(input, expected,
                    name: "I",
                    typeKind: TypeKind.Interface);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544528)]
            public void AddEnum()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Enum E\n F1\n End Enum\n End Namespace";
                TestAddNamedType(input, expected, "E",
                    typeKind: TypeKind.Enum,
                    members: Members(CreateEnumField("F1", null)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544527)]
            public void AddEnumWithValues()
            {
                var input = "Namespace [|N|]\n End Namespace";
                var expected = "Namespace N\n Public Enum E\n F1 = 1\n F2 = 2\n End Enum\n End Namespace";
                TestAddNamedType(input, expected, "E",
                    typeKind: TypeKind.Enum,
                    members: Members(CreateEnumField("F1", 1), CreateEnumField("F2", 2)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddEnumMember()
            {
                var input = "Public Enum [|E|]\n F1 = 1\n F2 = 2\n End Enum";
                var expected = "Public Enum E\n F1 = 1\n F2 = 2\n F3\n End Enum";
                TestAddField(input, expected,
                    name: "F3");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddEnumMemberWithValue()
            {
                var input = "Public Enum [|E|]\n F1 = 1\n F2\n End Enum";
                var expected = "Public Enum E\n F1 = 1\n F2\n F3 = 3\n End Enum";
                TestAddField(input, expected,
                    name: "F3", hasConstantValue: true, constantValue: 3);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544529)]
            public void AddDelegateType()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Delegate Function D(s As String) As Integer\n End Class";
                TestAddDelegateType(input, expected,
                    returnType: typeof(int),
                    parameters: Parameters(Parameter(typeof(string), "s")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(546224)]
            public void AddSealedDelegateType()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Delegate Function D(s As String) As Integer\n End Class";
                TestAddDelegateType(input, expected,
                    returnType: typeof(int),
                    modifiers: new DeclarationModifiers(isSealed: true),
                    parameters: Parameters(Parameter(typeof(string), "s")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodToClass()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Sub M()\n End Sub\n End Class";
                TestAddMethod(input, expected,
                    returnType: typeof(void));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodToClassEscapedName()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Protected Friend Sub [Sub]()\n End Sub\n End Class";
                TestAddMethod(input, expected,
                    accessibility: Accessibility.ProtectedOrInternal,
                    name: "Sub",
                    returnType: typeof(void));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration), WorkItem(544477)]
            public void AddSharedMethodToStructure()
            {
                var input = "Structure [|S|]\n End Structure";
                var expected = "Structure S\n Public Shared Function M() As Integer\n Return 0\n End Function\n End Structure";
                TestAddMethod(input, expected,
                    modifiers: new DeclarationModifiers(isStatic: true),
                    returnType: typeof(int),
                    statements: "Return 0");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddNotOverridableOverridesMethod()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public NotOverridable Overrides Function GetHashCode() As Integer\n $$ \nEnd Function\n End Class";
                TestAddMethod(input, expected,
                    name: "GetHashCode",
                    modifiers: new DeclarationModifiers(isOverride: true, isSealed: true),
                    returnType: typeof(int),
                    statements: "Return 0");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMustOverrideMethod()
            {
                var input = "MustInherit Class [|C|]\n End Class";
                var expected = "MustInherit Class C\n Public MustOverride Sub M()\n End Class";
                TestAddMethod(input, expected,
                    modifiers: new DeclarationModifiers(isAbstract: true),
                    returnType: typeof(void));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddMethodWithoutBody()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Sub M()\n End Class";
                TestAddMethod(input, expected,
                    returnType: typeof(void),
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddGenericMethod()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Function M(Of T)() As Integer\n $$ \nEnd Function\n End Class";
                TestAddMethod(input, expected,
                    returnType: typeof(int),
                    typeParameters: new[] { CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T") },
                    statements: "Return new T().GetHashCode()");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddVirtualMethod()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Protected Overridable Function M() As Integer\n $$ End Function\n End Class";
                TestAddMethod(input, expected,
                    accessibility: Accessibility.Protected,
                    modifiers: new DeclarationModifiers(isVirtual: true),
                    returnType: typeof(int),
                    statements: "Return 0\n");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddShadowsMethod()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Shadows Function ToString() As String\n $$ End Function\n End Class";
                TestAddMethod(input, expected,
                    name: "ToString",
                    accessibility: Accessibility.Public,
                    modifiers: new DeclarationModifiers(isNew: true),
                    returnType: typeof(string),
                    statements: "Return String.Empty\n");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddExplicitImplementation()
            {
                var input = "Interface I\n Sub M(i As Integer)\n End Interface\n Class [|C|]\n Implements I\n End Class";
                var expected = "Interface I\n Sub M(i As Integer)\n End Interface\n Class C\n Implements I\n Public Sub M(i As Integer) Implements I.M\n End Sub\n End Class";
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
Class [|C|]
End Class
";
                var expected = @"
Class C
    Public Shared Operator IsTrue(other As C) As Boolean
        $$
    End Operator
    Public Shared Operator IsFalse(other As C) As Boolean
        $$
    End Operator
End Class
";
                TestAddOperators(input, expected,
                    new[] { CodeGenerationOperatorKind.True, CodeGenerationOperatorKind.False },
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(bool),
                    statements: "Return False");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnaryOperators()
            {
                var input = @"
Class [|C|]
End Class
";
                var expected = @"
Class C
    Public Shared Operator + (other As C) As Object
        $$
    End Operator
    Public Shared Operator -(other As C) As Object
        $$
    End Operator
    Public Shared Operator Not(other As C) As Object
        $$
    End Operator
End Class
";
                TestAddOperators(input, expected,
                    new[]
                    {
                        CodeGenerationOperatorKind.UnaryPlus,
                        CodeGenerationOperatorKind.UnaryNegation,
                        CodeGenerationOperatorKind.LogicalNot
                    },
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(object),
                    statements: "Return Nothing");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddBinaryOperators()
            {
                var input = @"
Class [|C|]
End Class
";
                var expected = @"
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
";
                TestAddOperators(input, expected,
                    new[]
                    {
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
                    },
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(object),
                    statements: "Return Nothing");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddComparisonOperators()
            {
                var input = @"
Class [|C|]
End Class
";
                var expected = @"
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
";
                TestAddOperators(input, expected,
                    new[]
                    {
                        CodeGenerationOperatorKind.Equality,
                        CodeGenerationOperatorKind.Inequality,
                        CodeGenerationOperatorKind.GreaterThan,
                        CodeGenerationOperatorKind.LessThan,
                        CodeGenerationOperatorKind.GreaterThanOrEqual,
                        CodeGenerationOperatorKind.LessThanOrEqual
                    },
                    parameters: Parameters(Parameter("C", "a"), Parameter("C", "b")),
                    returnType: typeof(bool),
                    statements: "Return True");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddUnsupportedOperator()
            {
                var input = "Class [|C|]\n End Class";
                TestAddUnsupportedOperator(input,
                    operatorKind: CodeGenerationOperatorKind.Increment,
                    parameters: Parameters(Parameter("C", "other")),
                    returnType: typeof(bool),
                    statements: "Return True");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddExplicitConversion()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Shared Narrowing Operator CType(other As C) As Integer\n $$\n End Operator\n End Class";
                TestAddConversion(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    statements: "Return 0");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddImplicitConversion()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Shared Widening Operator CType(other As C) As Integer\n $$\n End Operator\n End Class";
                TestAddConversion(input, expected,
                    toType: typeof(int),
                    fromType: Parameter("C", "other"),
                    isImplicit: true,
                    statements: "Return 0");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStatementsToSub()
            {
                var input = "Class C\n [|Public Sub M\n Console.WriteLine(1)\n End Sub|]\n End Class";
                var expected = "Class C\n Public Sub M\n Console.WriteLine(1)\n $$\n End Sub\n End Class";
                TestAddStatements(input, expected, "Console.WriteLine(2)");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStatementsToOperator()
            {
                var input = "Class C\n [|Shared Operator +(arg As C) As C\n Return arg\n End Operator|]\n End Class";
                var expected = "Class C\n Shared Operator +(arg As C) As C\n Return arg\n $$\n End Operator\n End Class";
                TestAddStatements(input, expected, "Return Nothing");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddStatementsToPropertySetter()
            {
                var input = "Imports System\n Class C\n WriteOnly Property P As String\n [|Set\n End Set|]\n End Property\n End Class";
                var expected = "Imports System\n Class C\n WriteOnly Property P As String\n Set\n $$\n End Set\n End Property\n End Class";
                TestAddStatements(input, expected, "Console.WriteLine(\"Setting the value\"");
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToMethod()
            {
                var input = "Class C\n Public [|Sub M()\n End Sub|]\n End Class";
                var expected = "Class C\n Public Sub M(num As Integer, Optional text As String = \"Hello!\", Optional floating As Single = 0.5)\n End Sub\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num"), Parameter(typeof(string), "text", true, "Hello!"), Parameter(typeof(float), "floating", true, .5F)));
            }

            [WorkItem(844460)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToPropertyBlock()
            {
                var input = "Class C\n [|Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property|]\n End Class";
                var expected = "Class C\n Public Property P(num As Integer) As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num")));
            }

            [WorkItem(844460)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToPropertyStatement()
            {
                var input = "Class C\n [|Public Property P As String|]\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class";
                var expected = "Class C\n Public Property P(num As Integer) As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num")));
            }

            [WorkItem(844460)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToPropertyGetter_ShouldNotSucceed()
            {
                var input = "Class C\n Public Property P As String\n [|Get\n Return String.Empty\n End Get|]\n Set(value As String)\n End Set\n End Property\n End Class";
                var expected = "Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num")));
            }

            [WorkItem(844460)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToPropertySetter_ShouldNotSucceed()
            {
                var input = "Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n [|Set(value As String)\n End Set|]\n End Property\n End Class";
                var expected = "Class C\n Public Property P As String\n Get\n Return String.Empty\n End Get\n Set(value As String)\n End Set\n End Property\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter(typeof(int), "num")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddParametersToOperator()
            {
                var input = "Class C\n [|Shared Operator +(a As C) As C\n Return a\n End Operator|]\n End Class";
                var expected = "Class C\n Shared Operator +(a As C, b As C) As C\n Return a\n End Operator\n End Class";
                TestAddParameters(input, expected,
                    Parameters(Parameter("C", "b")));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAutoProperty()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Property P As Integer\n End Class";
                TestAddProperty(input, expected,
                    type: typeof(int));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddPropertyWithoutAccessorBodies()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Public Property P As Integer\n End Class";
                TestAddProperty(input, expected,
                    type: typeof(int),
                    getStatements: "Return 0",
                    setStatements: "Me.P = Value",
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddIndexer()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "Class C\n Default Public ReadOnly Property Item(i As Integer) As String\n Get\n $$ \nEnd Get\n End Property\n End Class";
                TestAddProperty(input, expected,
                    name: "Item",
                    type: typeof(string),
                    parameters: Parameters(Parameter(typeof(int), "i")),
                    getStatements: "Return String.Empty",
                    isIndexer: true);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToTypes()
            {
                var input = "Class [|C|]\n End Class";
                var expected = "<Serializable> Class C\n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromTypes()
            {
                var input = @"
<Serializable>
Class [|C|]
End Class";
                var expected = @"
Class C
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToMethods()
            {
                var input = "Class C\n Public Sub [|M()|] \n End Sub \n End Class";
                var expected = "Class C\n <Serializable> Public Sub M() \n End Sub \n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromMethods()
            {
                var input = @"
Class C
    <Serializable>
    Public Sub [|M()|]
    End Sub
End Class";
                var expected = @"
Class C
    Public Sub M()
    End Sub
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToFields()
            {
                var input = "Class C\n [|Public F As Integer|]\n End Class";
                var expected = "Class C\n <Serializable> Public F As Integer\n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromFields()
            {
                var input = @"
Class C
    <Serializable>
    Public [|F|] As Integer
End Class";
                var expected = @"
Class C
    Public F As Integer
End Class";
                TestRemoveAttribute<FieldDeclarationSyntax>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToProperties()
            {
                var input = "Class C \n Public Property [|P|] As Integer \n End Class";
                var expected = "Class C \n <Serializable> Public Property P As Integer \n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromProperties()
            {
                var input = @"
Class C
    <Serializable>
    Public Property [|P|] As Integer
End Class";
                var expected = @"
Class C
    Public Property P As Integer
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToPropertyAccessor()
            {
                var input = "Class C \n Public ReadOnly Property P As Integer \n [|Get|] \n Return 10 \n End Get \n End Property \n  End Class";
                var expected = "Class C \n Public ReadOnly Property P As Integer \n <Serializable> Get \n Return 10 \n End Get \n End Property \n  End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromPropertyAccessor()
            {
                var input = @"
Class C
    Public Property P As Integer
        <Serializable>
        [|Get|]
            Return 10
        End Get
End Class";
                var expected = @"
Class C
    Public Property P As Integer
        Get
            Return 10
        End Get
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToEnums()
            {
                var input = "Module M \n [|Enum C|] \n One \n Two \n End Enum\n End Module";
                var expected = "Module M \n <Serializable> Enum C \n One \n Two \n End Enum\n End Module";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromEnums()
            {
                var input = @"
Module M
    <Serializable>
    Enum [|C|]
        One
        Two
    End Enum
End Module";
                var expected = @"
Module M
    Enum C
        One
        Two
    End Enum
End Module";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToEnumMembers()
            {
                var input = "Module M \n Enum C \n [|One|] \n Two \n End Enum\n End Module";
                var expected = "Module M \n Enum C \n <Serializable> One \n Two \n End Enum\n End Module";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromEnumMembers()
            {
                var input = @"
Module M
    Enum C
        <Serializable>
        [|One|]
        Two
    End Enum
End Module";
                var expected = @"
Module M
    Enum C
        One
        Two
    End Enum
End Module";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToModule()
            {
                var input = "Module [|M|] \n End Module";
                var expected = "<Serializable> Module M \n End Module";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromModule()
            {
                var input = @"
<Serializable>
Module [|M|]
End Module";
                var expected = @"
Module M
End Module";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToOperator()
            {
                var input = "Class C \n Public Shared Operator [|+|] (x As C, y As C) As C \n Return New C() \n End Operator \n End Class";
                var expected = "Class C \n <Serializable> Public Shared Operator +(x As C, y As C) As C \n Return New C() \n End Operator \n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromOperator()
            {
                var input = @"
Module M
    Class C
        <Serializable>
        Public Shared Operator [|+|](x As C, y As C) As C
            Return New C()
        End Operator
    End Class
End Module";
                var expected = @"
Module M
    Class C
        Public Shared Operator +(x As C, y As C) As C
            Return New C()
        End Operator
    End Class
End Module";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToDelegate()
            {
                var input = "Module M \n Delegate Sub [|D()|]\n End Module";
                var expected = "Module M \n <Serializable> Delegate Sub D()\n End Module";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromDelegate()
            {
                var input = @"
Module M
    <Serializable>
    Delegate Sub [|D()|]
End Module";
                var expected = @"
Module M
    Delegate Sub D()
End Module";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToParam()
            {
                var input = "Class C \n Public Sub M([|x As Integer|]) \n End Sub \n End Class";
                var expected = "Class C \n Public Sub M(<Serializable> x As Integer) \n End Sub \n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeFromParam()
            {
                var input = @"
Class C
    Public Sub M(<Serializable> [|x As Integer|])
    End Sub
End Class";
                var expected = @"
Class C
    Public Sub M(x As Integer)
    End Sub
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeToCompilationUnit()
            {
                var input = "[|Class C \n End Class \n Class D \n End Class|]";
                var expected = "<Assembly: Serializable> Class C \n End Class \n Class D \n End Class";
                TestAddAttribute(input, expected, typeof(SerializableAttribute), VB.SyntaxFactory.Token(VB.SyntaxKind.AssemblyKeyword));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void AddAttributeWithWrongTarget()
            {
                var input = "[|Class C \n End Class \n Class D \n End Class|]";
                var expected = "<Assembly: Serializable> Class C \n End Class \n Class D \n End Class";
                Assert.Throws<AggregateException>(() => TestAddAttribute(input, expected, typeof(SerializableAttribute), VB.SyntaxFactory.Token(VB.SyntaxKind.ReturnKeyword)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithTrivia()
            {
                // With trivia.
                var input = @"' Comment 1
<System.Serializable> ' Comment 2
Class [|C|]
End Class";
                var expected = @"' Comment 1
Class C
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithTrivia_NewLine()
            {
                // With trivia, redundant newline at end of attribute removed.
                var input = @"' Comment 1
<System.Serializable>
Class [|C|]
End Class";
                var expected = @"' Comment 1
Class C
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithMultipleAttributes()
            {
                // Multiple attributes.
                var input = @"' Comment 1
<  System.Serializable   ,  System.Flags> ' Comment 2
Class [|C|]
End Class";
                var expected = @"' Comment 1
<System.Flags> ' Comment 2
Class C
End Class";
                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void RemoveAttributeWithMultipleAttributeLists()
            {
                // Multiple attribute lists.
                var input = @"' Comment 1
<  System.Serializable   ,  System.Flags> ' Comment 2
<System.Obsolete> ' Comment 3
Class [|C|]
End Class";
                var expected = @"' Comment 1
<System.Flags> ' Comment 2
<System.Obsolete> ' Comment 3
Class C
End Class";

                TestRemoveAttribute<SyntaxNode>(input, expected, typeof(SerializableAttribute));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateModifiers()
            {
                var input = @"Public Shared Class [|C|] ' Comment 1
    ' Comment 2
End Class";
                var expected = @"Friend Partial NotInheritable Class C ' Comment 1
    ' Comment 2
End Class";
                var eol = VB.SyntaxFactory.EndOfLine(@"");
                var newModifiers = new[] { VB.SyntaxFactory.Token(VB.SyntaxKind.FriendKeyword).WithLeadingTrivia(eol) }.Concat(
                    CreateModifierTokens(new DeclarationModifiers(isSealed: true, isPartial: true), LanguageNames.VisualBasic));

                TestUpdateDeclaration<ClassStatementSyntax>(input, expected, modifiers: newModifiers);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateAccessibility()
            {
                var input = @"' Comment 0
Public Shared Class [|C|] ' Comment 1
    ' Comment 2
End Class";
                var expected = @"' Comment 0
Protected Friend Shared Class C ' Comment 1
    ' Comment 2
End Class";
                TestUpdateDeclaration<ClassStatementSyntax>(input, expected, accessibility: Accessibility.ProtectedOrFriend);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateDeclarationType()
            {
                var input = @"
Public Shared Class C
    ' Comment 1
    Public Shared Function [|F|]() As Char
        Return 0
    End Function
End Class";
                var expected = @"
Public Shared Class C
    ' Comment 1
    Public Shared Function F() As Integer
        Return 0
    End Function
End Class";
                TestUpdateDeclaration<MethodStatementSyntax>(input, expected, getType: GetTypeSymbol(typeof(int)));
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestUpdateDeclarationMembers()
            {
                var input = @"
Public Shared Class [|C|]
    ' Comment 0
    Public Shared {|RetainedMember:f|} As Integer

    ' Comment 1
    Public Shared Function F() As Char
        Return 0
    End Function
End Class";
                var expected = @"
Public Shared Class C
    ' Comment 0
    Public Shared f As Integer
    Public Shared f2 As Integer
End Class";
                var getField = CreateField(Accessibility.Public, new DeclarationModifiers(isStatic: true), typeof(int), "f2");
                var getMembers = new List<Func<SemanticModel, ISymbol>>();
                getMembers.Add(getField);
                TestUpdateDeclaration<ClassBlockSyntax>(input, expected, getNewMembers: getMembers);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public void SortModules()
            {
                var generationSource = "Public Class [|C|] \n End Class";
                var initial = "Namespace [|N|] \n Module M \n End Module \n End Namespace";
                var expected = "Namespace N \n Public Class C \n End Class \n Module M \n End Module \n End Namespace";
                TestGenerateFromSourceSymbol(generationSource, initial, expected);
            }

            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGenerationSortDeclarations)]
            public void SortOperators()
            {
                var generationSource = @"
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
End Namespace";
                var initial = "Namespace [|N|] \n End Namespace";
                var expected = @"
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
End Namespace";
                TestGenerateFromSourceSymbol(generationSource, initial, expected,
                    forceLanguage: LanguageNames.VisualBasic,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false));
            }

            [WorkItem(848357)]
            [WpfFact, Trait(Traits.Feature, Traits.Features.CodeGeneration)]
            public void TestConstraints()
            {
                var generationSource = @"
Namespace N
    Public Class [|C|](Of T As Structure, U As Class)
        Public Sub Foo(Of Q As New, R As IComparable)()
        End Sub
        Public Delegate Sub D(Of T1 As Structure, T2 As Class)(t As T1, u As T2)
    End Class
End Namespace
";
                var initial = "Namespace [|N|] \n End Namespace";
                var expected = @"
Namespace N
    Public Class C(Of T As Structure, U As Class)
        Public Sub Foo(Of Q As New, R As IComparable)()
        Public Delegate Sub D(Of T1 As Structure, T2 As Class)(t As T1, u As T2)
    End Class
End Namespace
";
                TestGenerateFromSourceSymbol(generationSource, initial, expected,
                    codeGenerationOptions: new CodeGenerationOptions(generateMethodBodies: false),
                    onlyGenerateMembers: true);
            }
        }
    }
}
