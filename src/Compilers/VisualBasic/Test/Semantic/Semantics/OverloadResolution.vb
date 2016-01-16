' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.SpecialType
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.OverloadResolution
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics.OverloadResolutionTestHelpers

Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Namespace OverloadResolutionTestHelpers

        Friend Module Extensions

            Public Function ResolveMethodOverloading(
                instanceMethods As ImmutableArray(Of MethodSymbol),
                extensionMethods As ImmutableArray(Of MethodSymbol),
                typeArguments As ImmutableArray(Of TypeSymbol),
                arguments As ImmutableArray(Of BoundExpression),
                argumentNames As ImmutableArray(Of String),
                binder As Binder,
                lateBindingIsAllowed As Boolean,
                Optional includeEliminatedCandidates As Boolean = False
            ) As OverloadResolution.OverloadResolutionResult
                Dim methods As ImmutableArray(Of MethodSymbol)

                If instanceMethods.IsDefaultOrEmpty Then
                    methods = extensionMethods
                ElseIf extensionMethods.IsDefaultOrEmpty Then
                    methods = instanceMethods
                Else
                    methods = instanceMethods.Concat(extensionMethods)
                End If

                Dim methodGroup = New BoundMethodGroup(VisualBasicSyntaxTree.Dummy.GetRoot(Nothing),
                                                       If(typeArguments.IsDefaultOrEmpty,
                                                          Nothing,
                                                          New BoundTypeArguments(VisualBasicSyntaxTree.Dummy.GetRoot(Nothing), typeArguments)),
                                                       methods, LookupResultKind.Good, Nothing, QualificationKind.Unqualified)

                Return OverloadResolution.MethodInvocationOverloadResolution(
                        methodGroup, arguments, argumentNames, binder, includeEliminatedCandidates:=includeEliminatedCandidates, lateBindingIsAllowed:=lateBindingIsAllowed, callerInfoOpt:=Nothing,
                        useSiteDiagnostics:=Nothing)
            End Function
        End Module
    End Namespace

    Public Class OverloadResolutionTests
        Inherits BasicTestBase

        <Fact>
        Public Sub BasicTests()

            Dim optionStrictOn =
<file>
Option Strict On        

Class OptionStrictOn
    Shared Sub Context()
    End Sub
End Class
</file>

            Dim optionStrictOff =
<file>
Option Strict Off        

Class OptionStrictOff
    Shared Sub Context()
    End Sub
End Class
</file>

            Dim optionStrictOnTree = VisualBasicSyntaxTree.ParseText(optionStrictOn.Value)
            Dim optionStrictOffTree = VisualBasicSyntaxTree.ParseText(optionStrictOff.Value)

            Dim c1 = VisualBasicCompilation.Create("Test1",
                syntaxTrees:={VisualBasicSyntaxTree.ParseText(My.Resources.Resource.OverloadResolutionTestSource),
                              optionStrictOnTree,
                              optionStrictOffTree},
                references:={MscorlibRef, SystemCoreRef})

            Dim sourceModule = DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol)
            Dim optionStrictOnContext = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("OptionStrictOn").Single().GetMembers("Context").Single(), SourceMethodSymbol)
            Dim optionStrictOffContext = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("OptionStrictOff").Single().GetMembers("Context").Single(), SourceMethodSymbol)

            Dim optionStrictOnBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, optionStrictOnTree, optionStrictOnContext)
            Dim optionStrictOffBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, optionStrictOffTree, optionStrictOffContext)

            Dim TestClass1 = c1.Assembly.GlobalNamespace.GetTypeMembers("TestClass1").Single()
            Dim TestClass1_M1 = TestClass1.GetMembers("M1").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M2 = TestClass1.GetMembers("M2").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M3 = TestClass1.GetMembers("M3").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M4 = TestClass1.GetMembers("M4").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M5 = TestClass1.GetMembers("M5").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M6 = TestClass1.GetMembers("M6").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M7 = TestClass1.GetMembers("M7").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M8 = TestClass1.GetMembers("M8").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M9 = TestClass1.GetMembers("M9").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M10 = TestClass1.GetMembers("M10").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_M11 = TestClass1.GetMembers("M11").OfType(Of MethodSymbol)().Single()

            Dim TestClass1_M12 = TestClass1.GetMembers("M12").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M13 = TestClass1.GetMembers("M13").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M14 = TestClass1.GetMembers("M14").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M15 = TestClass1.GetMembers("M15").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M16 = TestClass1.GetMembers("M16").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M17 = TestClass1.GetMembers("M17").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M18 = TestClass1.GetMembers("M18").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M19 = TestClass1.GetMembers("M19").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M20 = TestClass1.GetMembers("M20").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M21 = TestClass1.GetMembers("M21").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M22 = TestClass1.GetMembers("M22").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M23 = TestClass1.GetMembers("M23").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M24 = TestClass1.GetMembers("M24").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M25 = TestClass1.GetMembers("M25").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M26 = TestClass1.GetMembers("M26").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M27 = TestClass1.GetMembers("M27").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_g = TestClass1.GetMembers("g").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_SM = TestClass1.GetMembers("SM").OfType(Of MethodSymbol)().Single()
            Dim TestClass1_SM1 = TestClass1.GetMembers("SM1").OfType(Of MethodSymbol)().Single()

            Dim TestClass1_ShortField = TestClass1.GetMembers("ShortField").OfType(Of FieldSymbol)().Single()
            Dim TestClass1_DoubleField = TestClass1.GetMembers("DoubleField").OfType(Of FieldSymbol)().Single()
            Dim TestClass1_ObjectField = TestClass1.GetMembers("ObjectField").OfType(Of FieldSymbol)().Single()

            Dim base = c1.Assembly.GlobalNamespace.GetTypeMembers("Base").Single()
            Dim baseExt = c1.Assembly.GlobalNamespace.GetTypeMembers("BaseExt").Single()

            Dim derived = c1.Assembly.GlobalNamespace.GetTypeMembers("Derived").Single()
            Dim derivedExt = c1.Assembly.GlobalNamespace.GetTypeMembers("DerivedExt").Single()
            Dim ext = c1.Assembly.GlobalNamespace.GetTypeMembers("Ext").Single()
            Dim ext1 = c1.Assembly.GlobalNamespace.GetTypeMembers("Ext1").Single()

            Dim base_M1 = base.GetMembers("M1").OfType(Of MethodSymbol)().Single()
            Dim base_M2 = base.GetMembers("M2").OfType(Of MethodSymbol)().Single()
            Dim base_M3 = base.GetMembers("M3").OfType(Of MethodSymbol)().Single()
            Dim base_M4 = base.GetMembers("M4").OfType(Of MethodSymbol)().Single()
            Dim base_M5 = base.GetMembers("M5").OfType(Of MethodSymbol)().Single()
            Dim base_M6 = base.GetMembers("M6").OfType(Of MethodSymbol)().Single()
            Dim base_M7 = base.GetMembers("M7").OfType(Of MethodSymbol)().Single()
            Dim base_M8 = base.GetMembers("M8").OfType(Of MethodSymbol)().Single()
            Dim base_M9 = base.GetMembers("M9").OfType(Of MethodSymbol)().Single()

            Dim base_M10 = baseExt.GetMembers("M10").OfType(Of MethodSymbol)().Single()

            Dim derived_M1 = derived.GetMembers("M1").OfType(Of MethodSymbol)().Single()
            Dim derived_M2 = derived.GetMembers("M2").OfType(Of MethodSymbol)().Single()
            Dim derived_M3 = derived.GetMembers("M3").OfType(Of MethodSymbol)().Single()
            Dim derived_M4 = derived.GetMembers("M4").OfType(Of MethodSymbol)().Single()
            Dim derived_M5 = derived.GetMembers("M5").OfType(Of MethodSymbol)().Single()
            Dim derived_M6 = derived.GetMembers("M6").OfType(Of MethodSymbol)().Single()
            Dim derived_M7 = derived.GetMembers("M7").OfType(Of MethodSymbol)().Single()
            Dim derived_M8 = derived.GetMembers("M8").OfType(Of MethodSymbol)().Single()
            Dim derived_M9 = derived.GetMembers("M9").OfType(Of MethodSymbol)().Single()

            Dim derived_M10 = derivedExt.GetMembers("M10").OfType(Of MethodSymbol)().Single()
            Dim derived_M11 = derivedExt.GetMembers("M11").OfType(Of MethodSymbol)().Single()
            Dim derived_M12 = derivedExt.GetMembers("M12").OfType(Of MethodSymbol)().Single()

            Dim ext_M11 = ext.GetMembers("M11").OfType(Of MethodSymbol)().Single()
            Dim ext_M12 = ext.GetMembers("M12").OfType(Of MethodSymbol)().Single()
            Dim ext_M13 = ext.GetMembers("M13").OfType(Of MethodSymbol)().ToArray()
            Dim ext_M14 = ext.GetMembers("M14").OfType(Of MethodSymbol)().Single()
            Dim ext_M15 = ext.GetMembers("M15").OfType(Of MethodSymbol)().Single()
            Dim ext_SM = ext.GetMembers("SM").OfType(Of MethodSymbol)().Single()
            Dim ext_SM1 = ext.GetMembers("SM1").OfType(Of MethodSymbol)().ToArray()

            Dim ext1_M14 = ext1.GetMembers("M14").OfType(Of MethodSymbol)().Single()

            Dim TestClass2 = c1.Assembly.GlobalNamespace.GetTypeMembers("TestClass2").Single()
            Dim TestClass2OfInteger = TestClass2.Construct(c1.GetSpecialType(System_Int32))
            Dim TestClass2OfInteger_S1 = TestClass2OfInteger.GetMembers("S1").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass2OfInteger_S2 = TestClass2OfInteger.GetMembers("S2").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass2OfInteger_S3 = TestClass2OfInteger.GetMembers("S3").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass2OfInteger_S4 = TestClass2OfInteger.GetMembers("S4").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass2OfInteger_S5 = TestClass2OfInteger.GetMembers("S5").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass2OfInteger_S6 = TestClass2OfInteger.GetMembers("S6").OfType(Of MethodSymbol)().ToArray()

            Dim _syntaxNode = optionStrictOffTree.GetVisualBasicRoot(Nothing)

            Dim [nothing] As BoundExpression = New BoundLiteral(_syntaxNode, ConstantValue.Nothing, Nothing)
            Dim intZero As BoundExpression = New BoundLiteral(_syntaxNode, ConstantValue.Create(0I), c1.GetSpecialType(System_Int32))
            Dim longZero As BoundExpression = New BoundLiteral(_syntaxNode, ConstantValue.Create(0L), c1.GetSpecialType(System_Int64))
            Dim unsignedOne As BoundExpression = New BoundLiteral(_syntaxNode, ConstantValue.Create(1UI), c1.GetSpecialType(System_UInt32))
            Dim longConst As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(-1L), c1.GetSpecialType(System_Int64), Nothing)
            Dim intVal As BoundExpression = New BoundUnaryOperator(_syntaxNode, UnaryOperatorKind.Minus, intZero, False, intZero.Type)
            Dim intArray As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.CreateArrayTypeSymbol(intZero.Type))
            Dim TestClass1Val As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, TestClass1)
            Dim omitted As BoundExpression = New BoundOmittedArgument(_syntaxNode, Nothing)
            Dim doubleConst As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(0.0R), c1.GetSpecialType(System_Double), Nothing)
            Dim doubleVal As BoundExpression = New BoundUnaryOperator(_syntaxNode, UnaryOperatorKind.Minus, doubleConst, False, doubleConst.Type)
            Dim shortVal As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.GetSpecialType(System_Int16))
            Dim ushortVal As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.GetSpecialType(System_UInt16))
            Dim objectVal As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.GetSpecialType(System_Object))
            Dim objectArray As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.CreateArrayTypeSymbol(objectVal.Type))

            Dim shortField As BoundExpression = New BoundFieldAccess(_syntaxNode, Nothing, TestClass1_ShortField, True, TestClass1_ShortField.Type)
            Dim doubleField As BoundExpression = New BoundFieldAccess(_syntaxNode, Nothing, TestClass1_DoubleField, True, TestClass1_DoubleField.Type)
            Dim objectField As BoundExpression = New BoundFieldAccess(_syntaxNode, Nothing, TestClass1_ObjectField, True, TestClass1_ObjectField.Type)
            Dim stringVal As BoundExpression = New BoundRValuePlaceholder(_syntaxNode, c1.GetSpecialType(System_String))

            Dim result As OverloadResolution.OverloadResolutionResult

            'TestClass1.M1()
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={TestClass1_M1}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:=Nothing,
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M1(Of TestClass1)() 'error BC32045: 'Public Shared Sub M1()' has no type parameters and so cannot have type arguments.
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={TestClass1_M1}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=(New TypeSymbol() {TestClass1}).AsImmutableOrNull(),
                arguments:=Nothing,
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.BadGenericArity, result.Candidates(0).State)
            Assert.Same(TestClass1_M1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M1(Nothing) 'error BC30057: Too many arguments to 'Public Shared Sub M1()'.
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M1)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentCountMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M2() 'error BC32050: Type parameter 'T' for 'Public Shared Sub M2(Of T)()' cannot be inferred.
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M2)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:=Nothing,
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.TypeInferenceFailed, result.Candidates(0).State)
            Assert.Same(TestClass1_M2, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M2(Of TestClass1)()
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M2)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=(New TypeSymbol() {TestClass1}).AsImmutableOrNull(),
                arguments:=Nothing,
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Equal(TestClass1_M2.Construct((New TypeSymbol() {TestClass1}).AsImmutableOrNull()), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M3()
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:=Nothing,
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Equal(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M3(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Equal(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Equal(TestClass1_M3, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'TestClass1.M3(intArray)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intArray}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Equal(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Equal(TestClass1_M3, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M3(Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Equal(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Equal(TestClass1_M3, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M4(intVal, TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))
            Assert.True(result.Candidates(0).ArgsToParamsOpt.IsDefault)

            'error BC30311: Value of type 'TestClass1' cannot be converted to 'Integer'.
            'TestClass1.M4(TestClass1Val, TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={TestClass1Val, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
            'TestClass1.M4(intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M4(intVal, y:=TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:={Nothing, "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))
            Assert.True(result.Candidates(0).ArgsToParamsOpt.SequenceEqual({0, 1}.AsImmutableOrNull()))

            'TestClass1.M4(X:=intVal, y:=TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:={"X", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))
            Assert.True(result.Candidates(0).ArgsToParamsOpt.SequenceEqual({0, 1}.AsImmutableOrNull()))

            'TestClass1.M4(y:=TestClass1Val, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={TestClass1Val, intVal}.AsImmutableOrNull(),
                argumentNames:={"y", "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))
            Assert.True(result.Candidates(0).ArgsToParamsOpt.SequenceEqual({1, 0}.AsImmutableOrNull()))

            'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
            'TestClass1.M4(y:=intVal, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={"y", "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)
            Assert.True(result.Candidates(0).ArgsToParamsOpt.SequenceEqual({1, 0}.AsImmutableOrNull()))

            'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'error BC30274: Parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching argument.
            'TestClass1.M4(intVal, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'error BC32021: Parameter 'x' in 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching omitted argument.
            'TestClass1.M4(, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={omitted, intVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'error BC30274: Parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)' already has a matching argument.
            'TestClass1.M4(x:=intVal, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={"x", "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'error BC30272: 'z' is not a parameter of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'TestClass1.M4(z:=intVal, y:=TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:={"z", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'error BC30272: 'z' is not a parameter of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'TestClass1.M4(z:=TestClass1Val, x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={TestClass1Val, intVal}.AsImmutableOrNull(),
                argumentNames:={"z", "x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'x' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'TestClass1.M4(, TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={omitted, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30455: Argument not specified for parameter 'y' of 'Public Shared Sub M4(x As Integer, y As TestClass1)'.
            'TestClass1.M4(intVal, )
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, omitted}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30587: Named argument cannot match a ParamArray parameter.
            'TestClass1.M3(x:=intArray)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intArray}.AsImmutableOrNull(),
                argumentNames:={"x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M3, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30587: Named argument cannot match a ParamArray parameter.
            'TestClass1.M3(x:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:={"x"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M3, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30588: Omitted argument cannot match a ParamArray parameter.
            'TestClass1.M5(intVal, )
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M5)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, omitted}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M5, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Same(TestClass1_M5, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30241: Named argument expected.
            'TestClass1.M4(x:=intVal, TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:={"x", Nothing}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30057: Too many arguments to 'Public Shared Sub M2(Of T)()'.
            'TestClass1.M2(Of TestClass1)(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M2)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=(New TypeSymbol() {TestClass1}).AsImmutableOrNull(),
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentCountMismatch, result.Candidates(0).State)
            Assert.Equal(TestClass1_M2.Construct((New TypeSymbol() {TestClass1}).AsImmutableOrNull()), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M6(shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.False(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M6(doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M6(doubleConst)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Integer'.
            'TestClass1.M6(objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)
            Assert.False(result.BestResult.HasValue)

            'TestClass1.M7(shortVal, shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.False(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M7(doubleVal, shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M7(shortVal, doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M7(doubleVal, doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M7(doubleConst, shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleConst, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)


            'TestClass1.M7(shortVal, doubleConst)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, doubleConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M7(doubleConst, doubleConst)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleConst, doubleConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M7(objectVal, shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M7(shortVal, objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal, objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M7(objectVal, objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M7(objectVal, doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M7(doubleConst, doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleConst, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M7(objectVal, doubleConst)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, doubleConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)


            'error BC32029: Option Strict On disallows narrowing from type 'Double' to type 'Short' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
            'TestClass1.M8(TestClass1.ShortField)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M8)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortField}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M8((shortVal))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M8)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.False(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC32029: Option Strict On disallows narrowing from type 'Object' to type 'Short' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
            'TestClass1.M9(TestClass1.ShortField)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M9)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortField}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M9((shortVal))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M9)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.False(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M10(doubleConst)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M10)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M10(TestClass1.DoubleField)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M10)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleField}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M10(TestClass1.ObjectField)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M10)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectField}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'error BC30512: Option Strict On disallows implicit conversions from 'Double' to 'Single'.
            'TestClass1.M10((doubleVal))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M10)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'Option Strict On disallows implicit conversions from 'Object' to 'Single'.
            'TestClass1.M10((objectVal))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M10)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromObject)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)

            'TestClass1.M11(objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M11)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'TestClass1.M11(objectArray)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M11)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectArray}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M12(intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M12(0)), (TestClass1_M12(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M12(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M12(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M12(0)), (TestClass1_M12(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M12(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M12(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'TestClass1.M13(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(0)), (TestClass1_M13(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentCountMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(0)), (TestClass1_M13(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:={"a"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentCountMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'TestClass1.M13(intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(0)), (TestClass1_M13(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(0)), (TestClass1_M13(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={"a", "b"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(3, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(2).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(2))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(1)), (TestClass1_M13(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))


            'TestClass1.M13(intVal, intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M13(0)), (TestClass1_M13(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M13(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))


            'Derived.M1(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M1), (base_M1)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(base_M1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(base_M1), (derived_M1)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(base_M1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'Derived.M2(intVal, z:=stringVal) ' Should bind to Base.M2
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M2), (base_M2)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, stringVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "z"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(derived_M2, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(base_M2, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Derived.M2(intVal, z:=stringVal) ' Should bind to Base.M2
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M2), (base_M2)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, stringVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "z"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.RequiresNarrowing, result.Candidates(0).State)
            Assert.Same(derived_M2, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(base_M2, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'derived.M3(intVal, z:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M3), (base_M3)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "z"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M3, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'error BC30272: 'z' is not a parameter of 'Public Shared Overloads Sub M4(u As Integer, [v As Integer = 0], [w As Integer = 0])'.
            'Derived.M4(intVal, z:=intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M4), (base_M4)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={Nothing, "z"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(derived_M4, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'derived.M5(a:=objectVal) ' Should bind to Base.M5
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M5), (base_M5)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:={"a"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(derived_M5, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(base_M5, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'derived.M6(a:=objectVal) ' Should bind to Base.M6
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M6), (base_M6)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:={"a"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentCountMismatch, result.Candidates(0).State)
            Assert.Same(derived_M6, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(base_M6, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'derived.M7(objectVal, objectVal) ' Should bind to Base.M7
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M7), (base_M7)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(derived_M7, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(base_M7, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'derived.M8(objectVal, objectVal) ' Should bind to Derived.M8
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M8), (base_M8)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(derived_M8, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(derived_M8, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Derived.M9(a:=TestClass1Val, b:=1) ' Should bind to Derived.M9
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M9), (base_M9)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={TestClass1Val, intVal}.AsImmutableOrNull(),
                argumentNames:={"a", "b"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M9, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'error BC30311: Value of type 'Integer' cannot be converted to 'TestClass1'.
            'error BC30311: Value of type 'TestClass1' cannot be converted to 'Integer'.
            'Derived.M9(a:=intVal, b:=TestClass1Val)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M9), (base_M9)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, TestClass1Val}.AsImmutableOrNull(),
                argumentNames:={"a", "b"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(derived_M9, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Derived.M9(Nothing, Nothing) ' Should bind to Derived.M9
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(derived_M9), (base_M9)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing], [nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.True(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M9, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            ' Calls BaseExt.M
            'b.M10(intVal)
            Dim base_M10_Candidate = (base_M10.ReduceExtensionMethod(derived, 0))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={base_M10_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(base_M10_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            ' Calls DerivedExt.M 
            'd.M10(intVal)
            Dim derived_M10_Candidate = (derived_M10.ReduceExtensionMethod(derived, 0))
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={base_M10_Candidate, derived_M10_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M10_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={derived_M10_Candidate, base_M10_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M10_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            ' Calls Ext.M11(derived, ...), because Ext.M11(I1, ...) is hidden since it extends
            ' an interface.
            'd.M11(intVal)
            Dim derived_M11_Candidate = (derived_M11.ReduceExtensionMethod(derived, 0))
            Dim i1_M11_Candidate = (ext_M11.ReduceExtensionMethod(derived, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={derived_M11_Candidate, i1_M11_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M11_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={i1_M11_Candidate, derived_M11_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M11_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            ' Calls derived.M12 since T.M12 target type is more generic.
            'd.M12(10)
            Dim derived_M12_Candidate = (derived_M12.ReduceExtensionMethod(derived, 0))
            Dim ext_M12_Candidate = (ext_M12.ReduceExtensionMethod(derived, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={derived_M12_Candidate, ext_M12_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M12_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M12_Candidate, derived_M12_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M12_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M12_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(ext_M12_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))


            'tc2.S1(10, 10)    ' Calls S1(U, T)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S1(0)), (TestClass2OfInteger_S1(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S1(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S1(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S1(1)), (TestClass2OfInteger_S1(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S1(0).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S1(1).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S1(1)), (TestClass2OfInteger_S1(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={"x", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S1(0).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S1(1).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'tc2.S2(10, 10)    ' Calls S2(Integer, T)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S2(0)), (TestClass2OfInteger_S2(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S2(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S2(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S2(1)), (TestClass2OfInteger_S2(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S2(0).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S2(1).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S2(0)), (TestClass2OfInteger_S2(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:={"x", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S2(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S2(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'M13(Of T, U)(x As T, y As U, z As T)
            'intVal.M13(intVal, intVal)
            Dim ext_M13_0_Candidate = (ext_M13(0).ReduceExtensionMethod(intVal.Type, 0))
            Dim ext_M13_1_Candidate = (ext_M13(1).ReduceExtensionMethod(intVal.Type, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M13_0_Candidate, ext_M13_1_Candidate}.
                               AsImmutableOrNull(),
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(ext_M13_0_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(ext_M13_1_Candidate.OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M13_1_Candidate, ext_M13_0_Candidate}.
                               AsImmutableOrNull(),
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(ext_M13_0_Candidate, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(ext_M13_1_Candidate.OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            ' Extension method precedence
            Dim derived_M11_Candidate_0 = (derived_M11.ReduceExtensionMethod(derived, 0))
            Dim derived_M11_Candidate_1 = (derived_M11.ReduceExtensionMethod(derived, 1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={derived_M11_Candidate_0, derived_M11_Candidate_1}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(derived_M11_Candidate_0, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(derived_M11_Candidate_1, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={derived_M11_Candidate_1, derived_M11_Candidate_0}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(derived_M11_Candidate_0, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(derived_M11_Candidate_1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S3(0)),
                                  (TestClass2OfInteger_S3(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal, intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S3(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S3(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.True(result.BestResult.HasValue)

            'error BC30521: Overload resolution failed because no accessible 'M14' is most specific for these arguments:
            'Extension(method) 'Public Sub M14(Of Integer)(y As Integer, z As Integer)' defined in 'Ext1': Not most specific.
            'Extension(method) 'Public Sub M14(Of Integer)(y As Integer, z As Integer)' defined in 'Ext': Not most specific.
            'intVal.M14(intVal, intVal)
            Dim ext_M14_Candidate = (ext_M14.ReduceExtensionMethod(intVal.Type, 0))
            Dim ext1_M14_Candidate = (ext1_M14.ReduceExtensionMethod(intVal.Type, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M14_Candidate, ext1_M14_Candidate}.
                               AsImmutableOrNull(),
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(ext_M14_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(ext1_M14_Candidate, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.False(result.BestResult.HasValue)

            'error BC30521: Overload resolution failed because no accessible 'S4' is most specific for these arguments:
            'Public Sub S4(Of Integer)(x As Integer, y() As Integer, z As TestClass2(Of Integer), v As Integer)': Not most specific.
            'Public Sub S4(Of Integer)(x As Integer, y() As Integer, z As TestClass2(Of Integer), v As Integer)': Not most specific.
            'tc2.S4(intVal, Nothing, Nothing, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S4(0)),
                                  (TestClass2OfInteger_S4(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, [nothing], [nothing], intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S4(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S4(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.False(result.BestResult.HasValue)

            'error BC30521: Overload resolution failed because no accessible 'S5' is most specific for these arguments:
            'Public Sub S5(x As Integer, y As TestClass2(Of Integer()))': Not most specific.
            'Public Sub S5(x As Integer, y As TestClass2(Of Integer))': Not most specific.
            'tc2.S5(intVal, Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S5(0)),
                                  (TestClass2OfInteger_S5(1)),
                                  (TestClass2OfInteger_S5(2))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, [nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S5(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S5(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(TestClass2OfInteger_S5(2).OriginalDefinition, result.Candidates(2).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S5(0)),
                                  (TestClass2OfInteger_S5(1)),
                                  (TestClass2OfInteger_S5(2))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, [nothing]}.AsImmutableOrNull(),
                argumentNames:={"x", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S5(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S5(1).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(TestClass2OfInteger_S5(2).OriginalDefinition, result.Candidates(2).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.False(result.BestResult.HasValue)

            'intVal.M15(intVal, intVal)
            Dim ext_M15_Candidate = (ext_M15.ReduceExtensionMethod(intVal.Type, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:=Nothing,
                extensionMethods:={ext_M15_Candidate}.
                               AsImmutableOrNull(),
                typeArguments:={intVal.Type}.AsImmutableOrNull(),
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(ext_M15_Candidate, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'S6(x As T, ParamArray y As Integer())
            'tc2.S6(intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass2OfInteger_S6(0)),
                                  (TestClass2OfInteger_S6(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(4, result.Candidates.Length)
            Assert.False(result.Candidates(0).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass2OfInteger_S6(0).OriginalDefinition, result.Candidates(0).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.True(result.Candidates(1).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(1).State)
            Assert.Same(TestClass2OfInteger_S6(0).OriginalDefinition, result.Candidates(1).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.False(result.Candidates(2).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(2).State)
            Assert.Same(TestClass2OfInteger_S6(1).OriginalDefinition, result.Candidates(2).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.True(result.Candidates(3).IsExpandedParamArrayForm)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(3).State)
            Assert.Same(TestClass2OfInteger_S6(1).OriginalDefinition, result.Candidates(3).Candidate.UnderlyingSymbol.OriginalDefinition)
            Assert.Equal(result.BestResult.Value, result.Candidates(3))

            'M14(a As Integer)
            'TestClass1.M14(shortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M14(0)), (TestClass1_M14(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M14(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M14(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M14(1)), (TestClass1_M14(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={shortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M14(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M14(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'M15(a As Integer)
            'TestClass1.M15(0)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M15(0)), (TestClass1_M15(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M15(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M15(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M15(1)), (TestClass1_M15(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M15(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M15(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'M16(a As Short)
            'TestClass1.M16(0L)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M16(0)), (TestClass1_M16(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.True(result.Candidates(1).RequiresNarrowingConversion)
            Assert.True(result.Candidates(1).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'Option Strict Off
            'error BC30519: Overload resolution failed because no accessible 'M16' can be called without a narrowing conversion:
            'Public Shared Sub M16(a As System.TypeCode)': Argument matching parameter 'a' narrows from 'Long' to 'System.TypeCode'.
            'Public Shared Sub M16(a As Short)': Argument matching parameter 'a' narrows from 'Long' to 'Short'.
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M16(0)), (TestClass1_M16(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.False(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.True(result.Candidates(1).RequiresNarrowingConversion)
            Assert.True(result.Candidates(1).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M16(1)), (TestClass1_M16(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.True(result.Candidates(0).RequiresNarrowingConversion)
            Assert.True(result.Candidates(0).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.True(result.Candidates(1).RequiresNarrowingConversion)
            Assert.False(result.Candidates(1).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M16(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'M16(a As System.TypeCode)
            'TestClass1.M16(0)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M16(0)), (TestClass1_M16(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.RequiresNarrowing, result.Candidates(0).State)
            Assert.Same(TestClass1_M16(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M16(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M16(1)), (TestClass1_M16(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.RequiresNarrowing, result.Candidates(1).State)
            Assert.Same(TestClass1_M16(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M16(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'Byte
            'TestClass1.M17(Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M17(0)), (TestClass1_M17(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M17(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M17(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M17(1)), (TestClass1_M17(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M17(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M17(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Short
            'TestClass1.M18(Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M18(0)), (TestClass1_M18(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M18(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M18(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M18(1)), (TestClass1_M18(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M18(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M18(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Integer
            'TestClass1.M19(Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M19(0)), (TestClass1_M19(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M19(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M19(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M19(1)), (TestClass1_M19(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M19(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M19(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Long
            'TestClass1.M20(Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M20(0)), (TestClass1_M20(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M20(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M20(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M20(1)), (TestClass1_M20(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M20(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M20(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'Integer
            'TestClass1.M21(ushortVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M21(0)), (TestClass1_M21(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={ushortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M21(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M21(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M21(1)), (TestClass1_M21(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={ushortVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M21(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M21(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))


            Dim numericTypesPrecedence = {System_SByte, System_Byte, System_Int16, System_UInt16,
                                          System_Int32, System_UInt32, System_Int64, System_UInt64,
                                          System_Decimal, System_Single, System_Double}
            Dim prev As SpecialType = 0

            For i As Integer = 0 To numericTypesPrecedence.Length - 1 Step 1
                Assert.InRange(numericTypesPrecedence(i), prev + 1, Integer.MaxValue)
                prev = numericTypesPrecedence(i)
            Next

            'error BC30521: Overload resolution failed because no accessible 'M22' is most specific for these arguments:
            'Public Shared Sub M22(a As SByte, b As Long)': Not most specific.
            'Public Shared Sub M22(a As Byte, b As ULong)': Not most specific.
            'TestClass1.M22(Nothing, Nothing)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M22(0)), (TestClass1_M22(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing], [nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M22(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M22(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M22(1)), (TestClass1_M22(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={[nothing], [nothing]}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M22(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M22(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'M23(a As Long)
            'TestClass1.M23(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M23(0)), (TestClass1_M23(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M23(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.RequiresNarrowing, result.Candidates(1).State)
            Assert.Same(TestClass1_M23(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M23(0)), (TestClass1_M23(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M23(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.True(result.Candidates(1).RequiresNarrowingConversion)
            Assert.True(result.Candidates(1).RequiresNarrowingNotFromNumericConstant)
            Assert.Same(TestClass1_M23(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'Option strict OFF: late call
            'TestClass1.M23(objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M23(0)), (TestClass1_M23(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.True(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M23(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M23(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Option strict ON
            ' error BC30518: Overload resolution failed because no accessible 'M23' can be called with these arguments:
            'Public Shared Sub M23(a As Short)': Option Strict On disallows implicit conversions from 'Object' to 'Short'.
            'Public Shared Sub M23(a As Long)': Option Strict On disallows implicit conversions from 'Object' to 'Long'.
            'TestClass1.M23(objectVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M23(0)), (TestClass1_M23(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=False,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M23(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M23(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M23(0)), (TestClass1_M23(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=False,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M23(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M23(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Option strict OFF
            'warning BC42016: Implicit conversion from 'Object' to 'Short'.
            'TestClass1.M24(objectVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M24(0)), (TestClass1_M24(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ExtensionMethodVsLateBinding, result.Candidates(0).State)
            Assert.Same(TestClass1_M24(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M24(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))


            'Option strict ON
            'F:\ddp\Roslyn\Main\Open\Compilers\VisualBasic\Test\Semantics\OverloadResolutionTestSource.vb(549) : error BC30518: Overload resolution failed because no accessible 'M24' can be called with these arguments:
            'Public Shared Sub M24(a As Short, b As Integer)': Option Strict On disallows implicit conversions from 'Object' to 'Short'.
            'Public Shared Sub M24(a As Long, b As Short)': Option Strict On disallows implicit conversions from 'Object' to 'Long'.
            'Public Shared Sub M24(a As Long, b As Short)': Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
            'TestClass1.M24(objectVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M24(0)), (TestClass1_M24(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=False,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M24(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M24(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M24(0)), (TestClass1_M24(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={objectVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=False,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M24(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(1).State)
            Assert.Same(TestClass1_M24(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'M25(a As SByte)
            'TestClass1.M25(-1L)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M25(0)),
                                  (TestClass1_M25(1)),
                                  (TestClass1_M25(2))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M25(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M25(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(TestClass1_M25(2), result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(2))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M25(2)),
                                  (TestClass1_M25(0)),
                                  (TestClass1_M25(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M25(2), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M25(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(2).State)
            Assert.Same(TestClass1_M25(1), result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M25(1)),
                                  (TestClass1_M25(2)),
                                  (TestClass1_M25(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longConst}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M25(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M25(2), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.LessApplicable, result.Candidates(2).State)
            Assert.Same(TestClass1_M25(0), result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'BC30518: Overload resolution failed because no accessible 'M26' can be called with these arguments:
            'Public Shared Sub M26(a As Integer, b As Short)': Option Strict On disallows implicit conversions from 'Double' to 'Short'.
            'Public Shared Sub M26(a As Short, b As Integer)': Option Strict On disallows implicit conversions from 'Double' to 'Integer'.
            'TestClass1.M26(-1L, doubleVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M26(0)),
                                  (TestClass1_M26(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longConst, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M26(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M26(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M26(1)),
                                  (TestClass1_M26(0))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longConst, doubleVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M26(1), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M26(0), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30512: Option Strict On disallows implicit conversions from 'Integer' to 'Short'.
            'TestClass1.M27(intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOnBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Sub M14(a As Long)
            'TestClass1.M14(0L)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M14(0)), (TestClass1_M14(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={longZero}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.RequiresNarrowing, result.Candidates(0).State)
            Assert.Same(TestClass1_M14(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M14(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))


            Dim DoubleMaxValue As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(Double.MaxValue), c1.GetSpecialType(System_Double), Nothing)
            Dim IntegerMaxValue As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(Integer.MaxValue), c1.GetSpecialType(System_Int32), Nothing)

            Assert.True(c1.Options.CheckOverflow)

            'error BC30439: Constant expression not representable in type 'Short'.
            'TestClass1.M27(Integer.MaxValue)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={IntegerMaxValue}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30439: Constant expression not representable in type 'Short'.
            'TestClass1.M27(Double.MaxValue)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={DoubleMaxValue}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'error BC30519: Overload resolution failed because no accessible 'M26' can be called without a narrowing conversion:
            'Public Shared Sub M26(a As Integer, b As Short)': Argument matching parameter 'b' narrows from 'Integer' to 'Short'.
            'Public Shared Sub M26(a As Short, b As Integer)': Argument matching parameter 'a' narrows from 'Integer' to 'Short'.
            'TestClass1.M26(intVal, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M26(0)),
                                  (TestClass1_M26(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={intVal, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M26(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M26(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Overflow On - Sub M26(a As Integer, b As Short)
            'TestClass1.M26(Integer.MaxValue, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M26(0)),
                                  (TestClass1_M26(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={IntegerMaxValue, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M26(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M26(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))


            'error BC30521: Overload resolution failed because no accessible 'g' is most specific for these arguments
            'TestClass1.g(1UI)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_g(0)),
                                  (TestClass1_g(1)),
                                  (TestClass1_g(2))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={unsignedOne}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.False(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_g(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_g(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(TestClass1_g(2), result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Should bind to extension method
            'TestClass1Val.SM(x:=intVal, y:=objectVal)
            Dim ext_SM_Candidate = (ext_SM.ReduceExtensionMethod(TestClass1Val.Type, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_SM)}.AsImmutableOrNull(),
                extensionMethods:={ext_SM_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal, objectVal}.AsImmutableOrNull(),
                argumentNames:={"x", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass1_SM, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(ext_SM_Candidate, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(1))

            'error BC30519: Overload resolution failed because no accessible 'SM1' can be called without a narrowing conversion:
            'Extension(method) 'Public Sub SM1(y As Object, x As Short)' defined in 'Ext': Argument matching parameter 'x' narrows from 'Integer' to 'Short'.
            'Extension(method) 'Public Sub SM1(y As Double, x As Integer)' defined in 'Ext': Argument matching parameter 'y' narrows from 'Object' to 'Double'.
            'TestClass1Val.SM1(x:=intVal, y:=objectVal)
            Dim ext_SM1_0_Candidate = (ext_SM1(0).ReduceExtensionMethod(TestClass1Val.Type, 0))
            Dim ext_SM1_1_Candidate = (ext_SM1(1).ReduceExtensionMethod(TestClass1Val.Type, 0))

            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_SM1)}.AsImmutableOrNull(),
                extensionMethods:={ext_SM1_0_Candidate, ext_SM1_1_Candidate}.AsImmutableOrNull(),
                typeArguments:=Nothing,
                arguments:={intVal, objectVal}.AsImmutableOrNull(),
                argumentNames:={"x", "y"}.AsImmutableOrNull(),
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(3, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Shadowed, result.Candidates(0).State)
            Assert.Same(TestClass1_SM1, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(ext_SM1_0_Candidate, result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(2).State)
            Assert.Same(ext_SM1_1_Candidate, result.Candidates(2).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)
        End Sub

        <Fact>
        Public Sub BasicTests2()


            Dim optionStrictOff =
<file>
Option Strict Off        

Class OptionStrictOff
    Shared Sub Context()
    End Sub
End Class
</file>

            Dim optionStrictOffTree = VisualBasicSyntaxTree.ParseText(optionStrictOff.Value)

            Dim c1 = VisualBasicCompilation.Create("Test1",
                syntaxTrees:={Parse(My.Resources.Resource.OverloadResolutionTestSource), optionStrictOffTree},
                references:={TestReferences.NetFx.v4_0_21006.mscorlib},
                options:=TestOptions.ReleaseExe.WithOverflowChecks(False))

            Dim sourceModule = DirectCast(c1.Assembly.Modules(0), SourceModuleSymbol)
            Dim optionStrictOffContext = DirectCast(sourceModule.GlobalNamespace.GetTypeMembers("OptionStrictOff").Single().GetMembers("Context").Single(), SourceMethodSymbol)

            Dim optionStrictOffBinder = BinderBuilder.CreateBinderForMethodBody(sourceModule, optionStrictOffTree, optionStrictOffContext)

            Assert.False(c1.Options.CheckOverflow)

            Dim TestClass1 = c1.Assembly.GlobalNamespace.GetTypeMembers("TestClass1").Single()
            Dim TestClass1_M26 = TestClass1.GetMembers("M26").OfType(Of MethodSymbol)().ToArray()
            Dim TestClass1_M27 = TestClass1.GetMembers("M27").OfType(Of MethodSymbol)().Single()

            Dim _syntaxNode = optionStrictOffTree.GetVisualBasicRoot(Nothing)
            Dim _syntaxTree = optionStrictOffTree

            Dim DoubleMaxValue As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(Double.MaxValue), c1.GetSpecialType(System_Double), Nothing)
            Dim IntegerMaxValue As BoundExpression = New BoundConversion(_syntaxNode, New BoundLiteral(_syntaxNode, ConstantValue.Null, Nothing), ConversionKind.Widening, True, True, ConstantValue.Create(Integer.MaxValue), c1.GetSpecialType(System_Int32), Nothing)
            Dim intVal As BoundExpression = New BoundUnaryOperator(_syntaxNode, UnaryOperatorKind.Minus, IntegerMaxValue, False, IntegerMaxValue.Type)

            Dim result As OverloadResolution.OverloadResolutionResult

            'Overflow Off
            'TestClass1.M27(Integer.MaxValue)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={IntegerMaxValue}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(result.BestResult.Value, result.Candidates(0))

            'Overflow Off
            'error BC30439: Constant expression not representable in type 'Short'.
            'TestClass1.M27(Double.MaxValue)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M27)}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={DoubleMaxValue}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.Equal(1, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.ArgumentMismatch, result.Candidates(0).State)
            Assert.Same(TestClass1_M27, result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)

            'Overflow Off
            'error BC30519: Overload resolution failed because no accessible 'M26' can be called without a narrowing conversion:
            'Public Shared Sub M26(a As Integer, b As Short)': Argument matching parameter 'b' narrows from 'Integer' to 'Short'.
            'Public Shared Sub M26(a As Short, b As Integer)': Argument matching parameter 'a' narrows from 'Integer' to 'Short'.
            'TestClass1.M26(Integer.MaxValue, intVal)
            result = ResolveMethodOverloading(includeEliminatedCandidates:=True,
                instanceMethods:={(TestClass1_M26(0)),
                                  (TestClass1_M26(1))}.AsImmutableOrNull(),
                extensionMethods:=Nothing,
                typeArguments:=Nothing,
                arguments:={IntegerMaxValue, intVal}.AsImmutableOrNull(),
                argumentNames:=Nothing,
                lateBindingIsAllowed:=True,
                binder:=optionStrictOffBinder)

            Assert.False(result.ResolutionIsLateBound)
            Assert.True(result.RemainingCandidatesRequireNarrowingConversion)
            Assert.Equal(2, result.Candidates.Length)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(0).State)
            Assert.Same(TestClass1_M26(0), result.Candidates(0).Candidate.UnderlyingSymbol)
            Assert.Equal(CandidateAnalysisResultState.Applicable, result.Candidates(1).State)
            Assert.Same(TestClass1_M26(1), result.Candidates(1).Candidate.UnderlyingSymbol)
            Assert.False(result.BestResult.HasValue)
        End Sub

        <Fact>
        Public Sub Bug4219()

            Dim compilationDef =
<compilation name="Bug4219">
    <file name="a.vb">
Option Strict On
 
Module Program
    Sub Main()
        Dim a As A(Of Long, Integer)
        a.Foo(y:=1, x:=1)
    End Sub
End Module
 
Class A(Of T, S)
    Sub Foo(x As Integer, y As T)
    End Sub
    Sub Foo(y As Long, x As S)
    End Sub
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42104: Variable 'a' is used before it has been assigned a value. A null reference exception could result at runtime.
        a.Foo(y:=1, x:=1)
        ~
BC30521: Overload resolution failed because no accessible 'Foo' is most specific for these arguments:
    'Public Sub Foo(x As Integer, y As Long)': Not most specific.
    'Public Sub Foo(y As Long, x As Integer)': Not most specific.
        a.Foo(y:=1, x:=1)
          ~~~
</expected>)
        End Sub

        <WorkItem(545633, "DevDiv")>
        <Fact>
        Public Sub Bug14186a()

            Dim compilationDef =
<compilation name="Bug14186a">
    <file name="a.vb">
Imports System
Imports System.Collections.Generic
Public Class Bar
    Shared Sub Equal(Of T)(exp As IEnumerable(Of T), act As IEnumerable(Of T))
        Console.Write("A;")
    End Sub
    Shared Sub Equal(Of T)(exp As T, act As T)
        Console.Write("B;")
    End Sub
End Class
Public Module Foo
    Sub Main()
        Dim foo As IEnumerable(Of Integer) = Nothing
        Bar.Equal(foo, foo)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="A;")
        End Sub

        <WorkItem(545633, "DevDiv")>
        <Fact>
        Public Sub Bug14186b()

            Dim compilationDef =
<compilation name="Bug14186b">
    <file name="a.vb">
Imports System.Collections.Generic
Public Class Bar
    Shared Sub Equal(Of T)(exp As IEnumerable(Of T), act As T)
    End Sub
    Shared Sub Equal(Of T)(exp As T, act As IEnumerable(Of T))
    End Sub
End Class
Public Module Foo
    Sub Main()
        Dim foo As IEnumerable(Of Integer) = Nothing
        Bar.Equal(foo, foo)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Equal' can be called with these arguments:
    'Public Shared Sub Equal(Of T)(exp As IEnumerable(Of T), act As T)': Data type(s) of the type parameter(s) cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
    'Public Shared Sub Equal(Of T)(exp As T, act As IEnumerable(Of T))': Data type(s) of the type parameter(s) cannot be inferred from these arguments because they do not convert to the same type. Specifying the data type(s) explicitly might correct this error.
        Bar.Equal(foo, foo)
            ~~~~~
</expected>)
        End Sub

        <WorkItem(545633, "DevDiv")>
        <Fact>
        Public Sub Bug14186c()

            Dim compilationDef =
<compilation name="Bug14186c">
    <file name="a.vb">
Public Class Bar
    Shared Sub Equal(Of T)(exp As I1(Of T))
    End Sub
    Shared Sub Equal(Of T)(exp As I2(Of T))
    End Sub
End Class
Public Interface I1(Of T)
End Interface
Public Interface I2(Of T)
End Interface
Class P(Of T)
    Implements I1(Of T), I2(Of T)
End Class
Public Module Foo
    Sub Main()
        Dim foo As New P(Of Integer)
        Bar.Equal(foo)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30521: Overload resolution failed because no accessible 'Equal' is most specific for these arguments:
    'Public Shared Sub Equal(Of Integer)(exp As I1(Of Integer))': Not most specific.
    'Public Shared Sub Equal(Of Integer)(exp As I2(Of Integer))': Not most specific.
        Bar.Equal(foo)
            ~~~~~
</expected>)
        End Sub

        <WorkItem(545633, "DevDiv")>
        <Fact>
        Public Sub Bug14186d()

            Dim compilationDef =
<compilation name="Bug14186d">
    <file name="a.vb">
Imports System
Public Class Bar
    Shared Sub Equal(Of T)(exp As I2(Of I2(Of T)))
        Console.Write("A;")
    End Sub
    Shared Sub Equal(Of T)(exp As I2(Of T))
        Console.Write("B;")
    End Sub
End Class
Public Interface I2(Of T)
End Interface
Class P(Of T)
    Implements I2(Of I2(Of T)), I2(Of T)
End Class
Public Module Foo
    Sub Main()
        Dim foo As New P(Of Integer)
        Dim foo2 As I2(Of Integer) = foo
        Bar.Equal(foo2)
        Dim foo3 As I2(Of I2(Of Integer)) = foo
        Bar.Equal(foo3)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="B;A;")
        End Sub

        <WorkItem(545633, "DevDiv")>
        <Fact>
        Public Sub Bug14186e()

            Dim compilationDef =
<compilation name="Bug14186e">
    <file name="a.vb">
Imports System
Public Class Bar
    Shared Sub Equal(Of T)(exp() As I2(Of T))
        Console.Write("A;")
    End Sub
    Shared Sub Equal(Of T)(exp() As T)
        Console.Write("B;")
    End Sub
End Class
Public Interface I2(Of T)
End Interface
Public Module Foo
    Sub Main()
        Dim foo() As I2(Of Integer) = Nothing
        Bar.Equal(foo)
    End Sub
End Module
    </file>
</compilation>

            CompileAndVerify(compilationDef, expectedOutput:="A;")
        End Sub

        <Fact>
        Public Sub Diagnostics1()

            Dim compilationDef =
<compilation name="OverloadResolutionDiagnostics">
    <file name="a.vb">
Option Strict On

Imports System.Console

Module Module1

    Sub Main()
        F1(Of Integer, Integer)()
        F1(Of Integer, Integer)(1, 2)
        F2(Of Integer)()
        F2(Of Integer)(1, 2)
        F3(Of Integer)()
        F3(Of Integer)(1, 2)

        F1(Of Integer)()
        F1(Of Integer)(1, 2)

        F4()
        F4(, , , )
        F4(1, 2, , 4)

        F3(y:=1)
        F3(1, y:=2)
        F3(y:=1, z:=2)

        F4(y:=1, x:=2)
        F4(, y:=1)

        F3(x:=1, x:=2)
        F3(, x:=2)
        F3(1, x:=2)

        F4(x:=1, x:=2)
        F4(, x:=2)
        F4(1, x:=2)
        F5(x:=1, x:=2)

        F2(1)

        Dim g As System.Guid = Nothing

        F6(g, g)
        F6(y:=g, x:=g)

        F4(g, Nothing)
        F4(1, g)

        Dim l As Long = 1
        Dim s As Short = 1

        F3(l)
        F7(g)
        F7(s)
        F7((l))

        F8(y:=Nothing)
    End Sub

    Sub F1(Of T)(x As Integer)
    End Sub

    Sub F2(Of T, S)(x As Integer)
    End Sub

    Sub F3(x As Integer)
    End Sub

    Sub F4(x As Integer, ParamArray y As Integer())
    End Sub

    Sub F5(x As Integer, y As Integer)
    End Sub

    Sub F6(x As Integer, y As Long)
    End Sub

    Sub F7(ByRef x As Integer)
    End Sub

    Sub F8(ParamArray y As Integer())
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32043: Too many type arguments to 'Public Sub F1(Of T)(x As Integer)'.
        F1(Of Integer, Integer)()
          ~~~~~~~~~~~~~~~~~~~~~
BC32043: Too many type arguments to 'Public Sub F1(Of T)(x As Integer)'.
        F1(Of Integer, Integer)(1, 2)
          ~~~~~~~~~~~~~~~~~~~~~
BC32042: Too few type arguments to 'Public Sub F2(Of T, S)(x As Integer)'.
        F2(Of Integer)()
          ~~~~~~~~~~~~
BC32042: Too few type arguments to 'Public Sub F2(Of T, S)(x As Integer)'.
        F2(Of Integer)(1, 2)
          ~~~~~~~~~~~~
BC32045: 'Public Sub F3(x As Integer)' has no type parameters and so cannot have type arguments.
        F3(Of Integer)()
          ~~~~~~~~~~~~
BC32045: 'Public Sub F3(x As Integer)' has no type parameters and so cannot have type arguments.
        F3(Of Integer)(1, 2)
          ~~~~~~~~~~~~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F1(Of Integer)(x As Integer)'.
        F1(Of Integer)()
        ~~~~~~~~~~~~~~
BC30057: Too many arguments to 'Public Sub F1(Of Integer)(x As Integer)'.
        F1(Of Integer)(1, 2)
                          ~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F4(x As Integer, ParamArray y As Integer())'.
        F4()
        ~~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F4(x As Integer, ParamArray y As Integer())'.
        F4(, , , )
        ~~
BC30588: Omitted argument cannot match a ParamArray parameter.
        F4(, , , )
             ~
BC30588: Omitted argument cannot match a ParamArray parameter.
        F4(, , , )
               ~
BC30588: Omitted argument cannot match a ParamArray parameter.
        F4(, , , )
                 ~
BC30588: Omitted argument cannot match a ParamArray parameter.
        F4(1, 2, , 4)
                 ~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F3(x As Integer)'.
        F3(y:=1)
        ~~
BC30272: 'y' is not a parameter of 'Public Sub F3(x As Integer)'.
        F3(y:=1)
           ~
BC30272: 'y' is not a parameter of 'Public Sub F3(x As Integer)'.
        F3(1, y:=2)
              ~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F3(x As Integer)'.
        F3(y:=1, z:=2)
        ~~
BC30272: 'y' is not a parameter of 'Public Sub F3(x As Integer)'.
        F3(y:=1, z:=2)
           ~
BC30272: 'z' is not a parameter of 'Public Sub F3(x As Integer)'.
        F3(y:=1, z:=2)
                 ~
BC30587: Named argument cannot match a ParamArray parameter.
        F4(y:=1, x:=2)
           ~
BC30455: Argument not specified for parameter 'x' of 'Public Sub F4(x As Integer, ParamArray y As Integer())'.
        F4(, y:=1)
        ~~
BC30587: Named argument cannot match a ParamArray parameter.
        F4(, y:=1)
             ~
BC30274: Parameter 'x' of 'Public Sub F3(x As Integer)' already has a matching argument.
        F3(x:=1, x:=2)
                 ~
BC32021: Parameter 'x' in 'Public Sub F3(x As Integer)' already has a matching omitted argument.
        F3(, x:=2)
             ~
BC30274: Parameter 'x' of 'Public Sub F3(x As Integer)' already has a matching argument.
        F3(1, x:=2)
              ~
BC30274: Parameter 'x' of 'Public Sub F4(x As Integer, ParamArray y As Integer())' already has a matching argument.
        F4(x:=1, x:=2)
                 ~
BC32021: Parameter 'x' in 'Public Sub F4(x As Integer, ParamArray y As Integer())' already has a matching omitted argument.
        F4(, x:=2)
             ~
BC30274: Parameter 'x' of 'Public Sub F4(x As Integer, ParamArray y As Integer())' already has a matching argument.
        F4(1, x:=2)
              ~
BC30455: Argument not specified for parameter 'y' of 'Public Sub F5(x As Integer, y As Integer)'.
        F5(x:=1, x:=2)
        ~~
BC30274: Parameter 'x' of 'Public Sub F5(x As Integer, y As Integer)' already has a matching argument.
        F5(x:=1, x:=2)
                 ~
BC32050: Type parameter 'S' for 'Public Sub F2(Of T, S)(x As Integer)' cannot be inferred.
        F2(1)
        ~~
BC32050: Type parameter 'T' for 'Public Sub F2(Of T, S)(x As Integer)' cannot be inferred.
        F2(1)
        ~~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        F6(g, g)
           ~
BC30311: Value of type 'Guid' cannot be converted to 'Long'.
        F6(g, g)
              ~
BC30311: Value of type 'Guid' cannot be converted to 'Long'.
        F6(y:=g, x:=g)
              ~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        F6(y:=g, x:=g)
                    ~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        F4(g, Nothing)
           ~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        F4(1, g)
              ~
BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer'.
        F3(l)
           ~
BC30311: Value of type 'Guid' cannot be converted to 'Integer'.
        F7(g)
           ~
BC32029: Option Strict On disallows narrowing from type 'Integer' to type 'Short' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
        F7(s)
           ~
BC30512: Option Strict On disallows implicit conversions from 'Long' to 'Integer'.
        F7((l))
           ~~~
BC30587: Named argument cannot match a ParamArray parameter.
        F8(y:=Nothing)
           ~
</expected>)
        End Sub

        <Fact>
        Public Sub Diagnostics2()

            Dim compilationDef =
<compilation name="OverloadResolutionDiagnostics">
    <file name="a.vb">
Option Strict On

Imports System.Console

Module Module1

    Sub Main()
        Foo(Of Integer, Integer)()
        Foo(Of Integer, Integer)(1, 2)
        Foo(Of Integer)()
        Foo(Of Integer)(1, 2)

        Dim g As System.Guid = Nothing

        F1(g)
        F1(y:=1)

        F2(1, y:=1)
        F2(1, )

        F3(1, , z:=1)
        F3(1, 1, x:=1)

        Foo(1)
    End Sub

    Sub Foo(Of T)(x As Integer)
    End Sub

    Sub Foo(Of S)(x As Long)
    End Sub

    Sub F1(x As Integer)
    End Sub

    Sub F1(x As Long)
    End Sub

    Sub F2(x As Long, ParamArray y As Integer())
    End Sub

    Sub F2(x As Integer, a As Integer, ParamArray y As Integer())
    End Sub

    Sub F3(x As Long, y As Integer, z As Long)
    End Sub

    Sub F3(x As Long, z As Long, y As Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC32087: Overload resolution failed because no accessible 'Foo' accepts this number of type arguments.
        Foo(Of Integer, Integer)()
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC32087: Overload resolution failed because no accessible 'Foo' accepts this number of type arguments.
        Foo(Of Integer, Integer)(1, 2)
        ~~~~~~~~~~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Foo(Of Integer)()
        ~~~~~~~~~~~~~~~
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        Foo(Of Integer)(1, 2)
        ~~~~~~~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'F1' can be called with these arguments:
    'Public Sub F1(x As Integer)': Value of type 'Guid' cannot be converted to 'Integer'.
    'Public Sub F1(x As Long)': Value of type 'Guid' cannot be converted to 'Long'.
        F1(g)
        ~~
BC30518: Overload resolution failed because no accessible 'F1' can be called with these arguments:
    'Public Sub F1(x As Integer)': 'y' is not a method parameter.
    'Public Sub F1(x As Integer)': Argument not specified for parameter 'x'.
    'Public Sub F1(x As Long)': 'y' is not a method parameter.
    'Public Sub F1(x As Long)': Argument not specified for parameter 'x'.
        F1(y:=1)
        ~~
BC30518: Overload resolution failed because no accessible 'F2' can be called with these arguments:
    'Public Sub F2(x As Long, ParamArray y As Integer())': Named argument cannot match a ParamArray parameter.
    'Public Sub F2(x As Integer, a As Integer, ParamArray y As Integer())': Named argument cannot match a ParamArray parameter.
    'Public Sub F2(x As Integer, a As Integer, ParamArray y As Integer())': Argument not specified for parameter 'a'.
        F2(1, y:=1)
        ~~
BC30518: Overload resolution failed because no accessible 'F2' can be called with these arguments:
    'Public Sub F2(x As Long, ParamArray y As Integer())': Omitted argument cannot match a ParamArray parameter.
    'Public Sub F2(x As Integer, a As Integer, ParamArray y As Integer())': Argument not specified for parameter 'a'.
        F2(1, )
        ~~
BC30518: Overload resolution failed because no accessible 'F3' can be called with these arguments:
    'Public Sub F3(x As Long, y As Integer, z As Long)': Argument not specified for parameter 'y'.
    'Public Sub F3(x As Long, z As Long, y As Integer)': Parameter 'z' already has a matching omitted argument.
    'Public Sub F3(x As Long, z As Long, y As Integer)': Argument not specified for parameter 'y'.
        F3(1, , z:=1)
        ~~
BC30518: Overload resolution failed because no accessible 'F3' can be called with these arguments:
    'Public Sub F3(x As Long, y As Integer, z As Long)': Parameter 'x' already has a matching argument.
    'Public Sub F3(x As Long, y As Integer, z As Long)': Argument not specified for parameter 'z'.
    'Public Sub F3(x As Long, z As Long, y As Integer)': Parameter 'x' already has a matching argument.
    'Public Sub F3(x As Long, z As Long, y As Integer)': Argument not specified for parameter 'y'.
        F3(1, 1, x:=1)
        ~~
BC30518: Overload resolution failed because no accessible 'Foo' can be called with these arguments:
    'Public Sub Foo(Of T)(x As Integer)': Type parameter 'T' cannot be inferred.
    'Public Sub Foo(Of S)(x As Long)': Type parameter 'S' cannot be inferred.
        Foo(1)
        ~~~
</expected>)
        End Sub

        <Fact>
        Public Sub Diagnostics3()

            Dim compilationDef =
<compilation name="OverloadResolutionDiagnostics">
    <file name="a.vb">
Option Strict Off

Imports System.Console

Module Module1

    Sub Main()
        Dim i As Integer = 0

        F1(i)
        F2(i, i)
        F2(1, 1)
    End Sub

    Sub F1(x As Byte)
    End Sub

    Sub F1(x As SByte)
    End Sub

    Sub F1(ByRef x As Long)
    End Sub

    Sub F2(x As Integer, ParamArray y As Byte())
    End Sub

    Sub F2(x As SByte, y As Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'F1' can be called without a narrowing conversion:
    'Public Sub F1(x As Byte)': Argument matching parameter 'x' narrows from 'Integer' to 'Byte'.
    'Public Sub F1(x As SByte)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
    'Public Sub F1(ByRef x As Long)': Copying the value of 'ByRef' parameter 'x' back to the matching argument narrows from type 'Long' to type 'Integer'.
        F1(i)
        ~~
BC30519: Overload resolution failed because no accessible 'F2' can be called without a narrowing conversion:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
        F2(i, i)
        ~~
BC30519: Overload resolution failed because no accessible 'F2' can be called without a narrowing conversion:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
        F2(1, 1)
        ~~
</expected>)
        End Sub


        <Fact>
        Public Sub Diagnostics4()

            Dim compilationDef =
<compilation name="OverloadResolutionDiagnostics">
    <file name="a.vb">
Option Strict On

Imports System.Console

Module Module1

    Sub Main()
        Dim i As Integer = 0

        F1(i)
        F2(i, i)
        F2(1, 1)
    End Sub

    Sub F1(x As Byte)
    End Sub

    Sub F1(x As SByte)
    End Sub

    Sub F1(ByRef x As Long)
    End Sub

    Sub F2(x As Integer, ParamArray y As Byte())
    End Sub

    Sub F2(x As SByte, y As Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'F1' can be called with these arguments:
    'Public Sub F1(x As Byte)': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
    'Public Sub F1(x As SByte)': Option Strict On disallows implicit conversions from 'Integer' to 'SByte'.
    'Public Sub F1(ByRef x As Long)': Option Strict On disallows narrowing from type 'Long' to type 'Integer' in copying the value of 'ByRef' parameter 'x' back to the matching argument.
        F1(i)
        ~~
BC30518: Overload resolution failed because no accessible 'F2' can be called with these arguments:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Option Strict On disallows implicit conversions from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Option Strict On disallows implicit conversions from 'Integer' to 'SByte'.
        F2(i, i)
        ~~
BC30519: Overload resolution failed because no accessible 'F2' can be called without a narrowing conversion:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
        F2(1, 1)
        ~~
</expected>)
        End Sub


        <Fact>
        Public Sub Diagnostics5()

            Dim compilationDef =
<compilation name="OverloadResolutionDiagnostics">
    <file name="a.vb">
Imports System.Console

Module Module1

    Sub Main()
        Dim i As Integer = 0

        F1(i)
        F2(i, i)
        F2(1, 1)
    End Sub

    Sub F1(x As Byte)
    End Sub

    Sub F1(x As SByte)
    End Sub

    Sub F1(ByRef x As Long)
    End Sub

    Sub F2(x As Integer, ParamArray y As Byte())
    End Sub

    Sub F2(x As SByte, y As Integer)
    End Sub
End Module
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'F1' can be called without a narrowing conversion:
    'Public Sub F1(x As Byte)': Argument matching parameter 'x' narrows from 'Integer' to 'Byte'.
    'Public Sub F1(x As SByte)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
    'Public Sub F1(ByRef x As Long)': Copying the value of 'ByRef' parameter 'x' back to the matching argument narrows from type 'Long' to type 'Integer'.
        F1(i)
        ~~
BC30519: Overload resolution failed because no accessible 'F2' can be called without a narrowing conversion:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
        F2(i, i)
        ~~
BC30519: Overload resolution failed because no accessible 'F2' can be called without a narrowing conversion:
    'Public Sub F2(x As Integer, ParamArray y As Byte())': Argument matching parameter 'y' narrows from 'Integer' to 'Byte'.
    'Public Sub F2(x As SByte, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'SByte'.
        F2(1, 1)
        ~~
</expected>)
        End Sub




        <Fact(), WorkItem(527622, "DevDiv")>
        Public Sub NoisyDiagnostics()

            Dim compilationDef =
<compilation>
    <file name="a.vb">
Option Strict On

Imports System.Console

Module Module1

    Sub Main()

        F4(y:=Nothing,)
    End Sub

    Sub F4(x As Integer, y As Integer())
    End Sub
End Module

Class C
    Private Sub M()
        Dim x As String = F(:'BIND:"F("
    End Sub

    Private Function F(arg As Integer) As String
        Return "Hello"
    End Function

    Private Function F(arg As String) As String
        Return "Goodbye"
    End Function
End Class
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30201: Expression expected.
        F4(y:=Nothing,)
                      ~
BC30241: Named argument expected.
        F4(y:=Nothing,)
                      ~
BC30198: ')' expected.
        Dim x As String = F(:'BIND:"F("
                            ~
BC30201: Expression expected.
        Dim x As String = F(:'BIND:"F("
                            ~
</expected>)
        End Sub

        <Fact>
        Public Sub Bug4263()

            Dim compilationDef =
<compilation name="Bug4263">
    <file name="a.vb">
Option Strict Off

Module M
  Sub Main()
    Dim x As String 
    Dim y As Object = Nothing
    x = Foo(y).ToLower()
    x = Foo((y)).ToLower()
  End Sub

  Sub Foo(ByVal x As String)
  End Sub

  Function Foo(ByVal ParamArray x As String()) As String
    return Nothing
  End Function
End Module

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30491: Expression does not produce a value.
    x = Foo(y).ToLower()
        ~~~~~~
BC30491: Expression does not produce a value.
    x = Foo((y)).ToLower()
        ~~~~~~~~
</expected>)

            compilationDef =
<compilation name="Bug4263">
    <file name="a.vb">
Option Strict Off

Imports System

Module M
  Sub Main()
    Dim x As String 
    x = Foo(CObj(Nothing)).ToLower()
    x = Foo(CObj((Nothing))).ToLower()
    x = Foo(CType(Nothing, Object)).ToLower()
    x = Foo(DirectCast(Nothing, Object)).ToLower()
    x = Foo(TryCast(Nothing, Object)).ToLower()
    x = Foo(CType(CStr(Nothing), Object)).ToLower()
    x = Foo(CType(CType(Nothing, ValueType), Object)).ToLower()
    x = Foo(CType(CType(CType(Nothing, Derived()), Base()), Object)).ToLower()
    x = Foo(CType(CType(CType(Nothing, Derived), Derived), Object)).ToLower()
    x = Foo(CType(Nothing, String())).ToLower()
  End Sub

  Sub Foo(ByVal x As String)
  End Sub

  Function Foo(ByVal ParamArray x As String()) As String
        System.Console.WriteLine("Function")
        Return ""
  End Function
End Module

Class Base
End Class

Class Derived
    Inherits Base
End Class
    </file>
</compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, <![CDATA[
Function
Function
Function
Function
Function
Function
Function
Function
Function
Function
]]>)


            compilationDef =
<compilation name="Bug4263">
    <file name="a.vb">
Imports System

Module M
  Sub Main()
    Foo(CObj(Nothing))
  End Sub

  Sub Foo(ByVal x As String)
  End Sub

  Function Foo(ByVal ParamArray x As String()) As String
        System.Console.WriteLine("Function")
        Return ""
  End Function
End Module
    </file>
</compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30518: Overload resolution failed because no accessible 'Foo' can be called with these arguments:
    'Public Sub Foo(x As String)': Option Strict On disallows implicit conversions from 'Object' to 'String'.
    'Public Function Foo(ParamArray x As String()) As String': Option Strict On disallows implicit conversions from 'Object' to 'String()'.
    Foo(CObj(Nothing))
    ~~~
</expected>)

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.Custom))

            Assert.Equal(OptionStrict.Custom, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC42016: Implicit conversion from 'Object' to 'String()'.
    Foo(CObj(Nothing))
        ~~~~~~~~~~~~~
</expected>)


            compilationDef =
<compilation name="Bug4263">
    <file name="a.vb">
Imports System

Module M
  Sub Main()
        Dim x As String = (CObj(Nothing))
  End Sub
End Module
    </file>
</compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe.WithOptionStrict(OptionStrict.On))

            Assert.Equal(OptionStrict.On, compilation.Options.OptionStrict)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30512: Option Strict On disallows implicit conversions from 'Object' to 'String'.
        Dim x As String = (CObj(Nothing))
                          ~~~~~~~~~~~~~~~
</expected>)
        End Sub

        <WorkItem(539850, "DevDiv")>
        <Fact>
        Public Sub TestConversionFromZeroLiteralToEnum()

            Dim compilationDef =
      <compilation name="TestConversionFromZeroLiteralToEnum">
          <file name="Program.vb">
Imports System

Module Program
  Sub Main()
    Console.WriteLine(Foo(0).ToLower())
  End Sub

  Sub Foo(x As DayOfWeek)
  End Sub

  Function Foo(x As Object) As String
    Return "ABC"
  End Function
End Module
    </file>
      </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            CompileAndVerify(compilation, expectedOutput:="abc")

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(compilation, expectedOutput:="abc")
        End Sub

        <WorkItem(528006, "DevDiv")>
        <Fact()>
        Public Sub TestConversionFromZeroLiteralToNullableEnum()

            Dim compilationDef =
    <compilation name="TestConversionFromZeroLiteralToNullableEnum">
        <file name="Program.vb">
Option Strict On

Imports System

Module Program
    Sub Main()
        Console.WriteLine(Foo(0).ToLower())
    End Sub

    Function Foo(x As DayOfWeek?) As String
        Return "ABC"
    End Function
End Module
    </file>
    </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            CompileAndVerify(compilation, expectedOutput:="abc")
        End Sub

        <WorkItem(528011, "DevDiv")>
        <Fact()>
        Public Sub TestInvocationWithNamedArgumentInLambda()

            Dim compilationDef =
      <compilation name="TestInvocationWithNamedArgumentInLambda">
          <file name="Program.vb">
Imports System

Class B
  Sub Foo(x As Integer, ParamArray z As Integer())
     System.Console.WriteLine("B.Foo")
  End Sub
End Class

Class C
  Inherits B
  Overloads Sub Foo(y As Integer)
     System.Console.WriteLine("C.Foo")
  End Sub
End Class

Module M
  Sub Main()
    Dim p as New C()
    p.Foo(x:=1) ' This fails to compile in Dev10, but works in Roslyn
  End Sub
End Module
    </file>
      </compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            CompileAndVerify(compilation, expectedOutput:="B.Foo")

            compilationDef =
      <compilation name="TestInvocationWithNamedArgumentInLambda">
          <file name="Program.vb">
Imports System

Class B
  Sub Foo(x As Integer, ParamArray z As Integer())
  End Sub
End Class

Class C
  Inherits B
  Overloads Sub Foo(y As Integer)
  End Sub
End Class

Class D
  Overloads Sub Foo(x As Integer)
  End Sub
End Class

Module M
  Sub Main()
    Console.WriteLine(Bar(Sub(p) p.Foo(x:=1)).ToLower())
  End Sub

  Sub Bar(a As Action(Of C))
  End Sub
  Function Bar(a As Action(Of D)) As String
    Return "ABC"
  End Function
End Module
    </file>
      </compilation>

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.On))
            compilation.AssertTheseDiagnostics(<![CDATA[
BC30521: Overload resolution failed because no accessible 'Bar' is most specific for these arguments:
    'Public Sub Bar(a As Action(Of C))': Not most specific.
    'Public Function Bar(a As Action(Of D)) As String': Not most specific.
    Console.WriteLine(Bar(Sub(p) p.Foo(x:=1)).ToLower())
                      ~~~]]>)

            compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))
            compilation.AssertTheseDiagnostics(<![CDATA[
BC30521: Overload resolution failed because no accessible 'Bar' is most specific for these arguments:
    'Public Sub Bar(a As Action(Of C))': Not most specific.
    'Public Function Bar(a As Action(Of D)) As String': Not most specific.
    Console.WriteLine(Bar(Sub(p) p.Foo(x:=1)).ToLower())
                      ~~~]]>)
        End Sub

        <WorkItem(539994, "DevDiv")>
        <Fact>
        Public Sub MethodTypeParameterInferenceBadArg()
            ' Method type parameter inference should complete in the case where
            ' the type of a method argument is ErrorType but HasErrors=False.
            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib(
<compilation>
    <file name="a.vb">
    Class C
        Sub M(Of T)(x As T, y As T)
        End Sub
        Sub N()
            Dim d As D = GetD()
            M("", d.F)
        End Sub
    End Class
    </file>
</compilation>)
            Dim diagnostics = compilation.GetDiagnostics().ToArray()
            ' The actual errors are not as important as ensuring compilation completes.
            ' (Just returning successfully from GetDiagnostics() is sufficient in this case.)
            Dim anyErrors = diagnostics.Length > 0
            Assert.True(anyErrors)
        End Sub

        <Fact()>
        Public Sub InaccessibleMethods()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module2
    Private Sub M1(x as Integer)
    End Sub

    Private Sub M1(x as Long)
    End Sub

    Private Sub M2(x as Integer)
    End Sub

    Private Sub M2(x as Long, y as Integer)
    End Sub
End Module

Module Module1

    Sub Main()
        M1(1) 'BIND1:"M1(1)"
        M1(1, 2) 'BIND2:"M1(1, 2)"
        M2(1, 2) 'BIND3:"M2(1, 2)"
    End Sub

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30390: 'Module2.Private Sub M1(x As Integer)' is not accessible in this context because it is 'Private'.
        M1(1) 'BIND1:"M1(1)"
        ~~
BC30517: Overload resolution failed because no 'M1' is accessible.
        M1(1, 2) 'BIND2:"M1(1, 2)"
        ~~
BC30390: 'Module2.Private Sub M2(x As Long, y As Integer)' is not accessible in this context because it is 'Private'.
        M2(1, 2) 'BIND3:"M2(1, 2)"
        ~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            If True Then
                Dim node1 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)
                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Sub Module2.M1(x As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            End If
            If True Then
                Dim node2 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 2)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node2)

                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Sub Module2.M1(x As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
                Assert.Equal("Sub Module2.M1(x As System.Int64)", symbolInfo.CandidateSymbols(1).ToTestDisplayString())
            End If
            If True Then
                Dim node3 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 3)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node3)

                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Sub Module2.M2(x As System.Int64, y As System.Int32)", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            End If
        End Sub

        <Fact()>
        Public Sub InaccessibleProperties()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Imports System

Module Module2
    Private Property P1(x as Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property

    Private Property P1(x as Long) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property

    Private Property P2(x as Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property

    Private Property P2(x as Long, y as Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)

        End Set
    End Property
End Module

Module Module1

    Sub Main()
        P1(1)=1 'BIND1:"P1(1)"
        P1(1, 2)=1 'BIND2:"P1(1, 2)"
        P2(1, 2)=1 'BIND3:"P2(1, 2)"
        Dim x =  P2(1)
    End Sub

End Module
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30389: 'Module2.P1(x As Integer)' is not accessible in this context because it is 'Private'.
        P1(1)=1 'BIND1:"P1(1)"
        ~~
BC30517: Overload resolution failed because no 'P1' is accessible.
        P1(1, 2)=1 'BIND2:"P1(1, 2)"
        ~~
BC30389: 'Module2.P2(x As Long, y As Integer)' is not accessible in this context because it is 'Private'.
        P2(1, 2)=1 'BIND3:"P2(1, 2)"
        ~~
BC30389: 'Module2.P2(x As Integer)' is not accessible in this context because it is 'Private'.
        Dim x =  P2(1)
                 ~~
</expected>)

            Dim tree As SyntaxTree = (From t In compilation.SyntaxTrees Where t.FilePath = "a.vb").Single()
            Dim semanticModel = compilation.GetSemanticModel(tree)
            If True Then
                Dim node1 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 1)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node1)

                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Property Module2.P1(x As System.Int32) As System.Int32", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            End If
            If True Then
                Dim node2 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 2)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node2)

                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(2, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Property Module2.P1(x As System.Int32) As System.Int32", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
                Assert.Equal("Property Module2.P1(x As System.Int64) As System.Int32", symbolInfo.CandidateSymbols(1).ToTestDisplayString())
            End If
            If True Then
                Dim node3 As InvocationExpressionSyntax = CompilationUtils.FindBindingText(Of InvocationExpressionSyntax)(compilation, "a.vb", 3)

                Dim symbolInfo As SymbolInfo = semanticModel.GetSymbolInfo(node3)

                Assert.Equal(CandidateReason.Inaccessible, symbolInfo.CandidateReason)
                Assert.Null(symbolInfo.Symbol)
                Assert.Equal(1, symbolInfo.CandidateSymbols.Length)
                Assert.Equal("Property Module2.P2(x As System.Int64, y As System.Int32) As System.Int32", symbolInfo.CandidateSymbols(0).ToTestDisplayString())
            End If

        End Sub

        <Fact, WorkItem(545574, "DevDiv")>
        Public Sub OverloadWithIntermediateDifferentMember1()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Sub Foo(x As Integer)
    End Sub
End Class
 
Class B
    Inherits A
    Shadows Property Foo As Integer
End Class
 
Class C
    Inherits B
    Overloads Shared Function Foo(x As Object) As Object
        Return Nothing
    End Function

    Shared Sub Bar()
        Foo(1).ToString()
    End Sub
End Class

    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC40004: function 'Foo' conflicts with property 'Foo' in the base class 'B' and should be declared 'Shadows'.
    Overloads Shared Function Foo(x As Object) As Object
                              ~~~
                                                            </expected>)
        End Sub

        <Fact, WorkItem(545574, "DevDiv")>
        Public Sub OverloadWithIntermediateDifferentMember2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Class A
    Shared Sub Foo(x As Integer)
    End Sub
End Class
 
Class B
    Inherits A
    Overloads Property Foo As Integer
End Class
 
Class C
    Inherits B
    Overloads Shared Function Foo(x As Object) As Object
        Return Nothing
    End Function

    Shared Sub Bar()
        Foo(1).ToString()
    End Sub
End Class

    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC40004: property 'Foo' conflicts with sub 'Foo' in the base class 'A' and should be declared 'Shadows'.
    Overloads Property Foo As Integer
                       ~~~
BC40004: function 'Foo' conflicts with property 'Foo' in the base class 'B' and should be declared 'Shadows'.
    Overloads Shared Function Foo(x As Object) As Object
                              ~~~
                                                            </expected>)
        End Sub

        <Fact, WorkItem(545574, "DevDiv")>
        Public Sub OverloadWithIntermediateDifferentMember3()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Interface A
    Sub Foo(x As Integer)
End Interface

Interface B
    Inherits A
    Shadows Property Foo As Integer
End Interface

Interface C
    Inherits B
    Overloads Function Foo(x As Object) As Object

End Interface

Class D
    Shared Sub Bar()
        Dim q As C = Nothing
        q.Foo(1).ToString()
    End Sub

End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC40004: function 'Foo' conflicts with property 'Foo' in the base interface 'B' and should be declared 'Shadows'.
    Overloads Function Foo(x As Object) As Object
                       ~~~
                                                            </expected>)
        End Sub

        <Fact, WorkItem(545520, "DevDiv")>
        Public Sub OverloadSameSigBetweenFunctionAndSub()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
 
Class A
    Shared Function Foo() As Integer()
        Return Nothing
    End Function
End Class
 
Class B
    Inherits A
    Overloads Shared Sub Foo() 
    End Sub
    Sub Main()
        Foo(1).ToString()
    End Sub
End Class    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32016: 'Public Shared Overloads Sub Foo()' has no parameters and its return type cannot be indexed.
        Foo(1).ToString()
        ~~~
                                                            </expected>)
        End Sub

        <Fact, WorkItem(545520, "DevDiv")>
        Public Sub OverloadSameSigBetweenFunctionAndSub2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
 
Class A
    Shared Function Foo() As Integer()
        Return Nothing
    End Function
End Class
 
Class B
    Inherits A
    Overloads Shared Sub Foo(optional a as integer = 3) 
    End Sub
    Sub Main()
        Foo(1).ToString()
    End Sub
End Class    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC32016: 'Public Shared Overloads Sub Foo([a As Integer = 3])' has no parameters and its return type cannot be indexed.
        Foo(1).ToString()
        ~~~
BC30491: Expression does not produce a value.
        Foo(1).ToString()
        ~~~~~~
                                                            </expected>)
        End Sub

        <Fact, WorkItem(545520, "DevDiv")>
        Public Sub OverloadSameSigBetweenFunctionAndSub3()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
 
Class A
    Shared Function Foo() As Integer()
        Return Nothing
    End Function
End Class
 
Class B
    Inherits A
    Overloads Shared Sub Foo(ParamArray a as Integer()) 
    End Sub
    Sub Main()
        Foo(1).ToString()
    End Sub
End Class    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
                                                                 </expected>)
        End Sub

        <Fact, WorkItem(545520, "DevDiv")>
        Public Sub OverloadSameSigBetweenFunctionAndSub4()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Interface A
    Function Foo() As Integer()
End Interface

Interface B
    Sub Foo()
End Interface

Interface C
    Inherits A, B
End Interface

Module M1
    Sub Main()
        Dim c As C = Nothing
        c.Foo(1).ToString()
    End Sub
End Module
]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation, <expected>
BC30516: Overload resolution failed because no accessible 'Foo' accepts this number of arguments.
        c.Foo(1).ToString()
          ~~~
                                                                 </expected>)
        End Sub


        <Fact, WorkItem(546129, "DevDiv")>
        Public Sub SameMethodNameDifferentCase()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Module Test
    Sub Main()
        Dim a = New class1
        Dim O As Object = 5
        a.Bb(O)
    End Sub

    Friend Class class1
        Public Overridable Sub Bb(ByRef y As String)
        End Sub

        Public Overridable Sub BB(ByRef y As Short)
        End Sub
    End Class
End Module
]]></file>
</compilation>)

            compilation.VerifyDiagnostics()

        End Sub

        <WorkItem(544657, "DevDiv")>
        <Fact()>
        Public Sub Regress14728()

            Dim compilationDef =
      <compilation name="Regress14728">
          <file name="Program.vb">
Option Strict Off

Module Module1
    Sub Main()
        Dim o As New class1
        o.CallLateBound("qq", "aa")
    End Sub
    Class class1
        Private Shared CurrentCycle As Integer
        Sub CallLateBound(ByVal ParamArray prmarray1() As Object)
            LateBound(prmarray1.GetUpperBound(0), prmarray1)
        End Sub
        Sub LateBound(ByVal ScenDesc As String, ByVal ParamArray prm1() As Object)
            System.Console.WriteLine(ScenDesc + prm1(0))
        End Sub
    End Class
End Module

    </file>
      </compilation>

            Dim Compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(Compilation, expectedOutput:="1qq")
        End Sub

        <Fact(), WorkItem(544657, "DevDiv")>
        Public Sub Regress14728Err()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict Off

Module Module1
    Sub Main()
        Dim o As New class1
        o.CallLateBound("qq", "aa")
    End Sub
    Class class1
        Private Shared CurrentCycle As Integer
        Sub CallLateBound(ByVal ParamArray prmarray1() As Object)
            LateBound(prmarray1.GetUpperBound(0), prmarray1)
        End Sub

        Sub LateBound(ByVal ScenDesc As String, ByVal ParamArray prm1() As Object)
            System.Console.WriteLine(ScenDesc + prm1(0))
        End Sub

        Sub LateBound()
            System.Console.WriteLine("hi")
        End Sub

    End Class
End Module
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(compilation, expectedOutput:="1qq")
        End Sub

        <Fact, WorkItem(546747, "DevDiv")>
        Public Sub Bug16716_1()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
<ProvideMenuResource(1000, 1)>
Public NotInheritable Class TNuggetPackage

    Sub Test()
        Dim z As New ProvideMenuResourceAttribute(1000, 1)
    End Sub

End Class

Public Class ProvideMenuResourceAttribute
    Inherits System.Attribute

    Public Sub New(x As Short, y As Integer)
    End Sub

    Public Sub New(x As String, y As Integer)
    End Sub
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected>
BC30519: Overload resolution failed because no accessible 'New' can be called without a narrowing conversion:
    'Public Sub New(x As Short, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'Short'.
    'Public Sub New(x As String, y As Integer)': Argument matching parameter 'x' narrows from 'Integer' to 'String'.
        Dim z As New ProvideMenuResourceAttribute(1000, 1)
                     ~~~~~~~~~~~~~~~~~~~~~~~~~~~~
</expected>)

            Dim TNuggetPackage = compilation.GetTypeByMetadataName("TNuggetPackage")

            Assert.Equal("Sub ProvideMenuResourceAttribute..ctor(x As System.Int16, y As System.Int32)", TNuggetPackage.GetAttributes()(0).AttributeConstructor.ToTestDisplayString())
        End Sub

        <Fact, WorkItem(546747, "DevDiv")>
        Public Sub Bug16716_2()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
<ProvideMenuResource(1000, 1)>
Public NotInheritable Class TNuggetPackage
End Class

Public Class ProvideMenuResourceAttribute
    Inherits System.Attribute

    Public Sub New(x As Short, y As String)
    End Sub

    Public Sub New(x As String, y As Short)
    End Sub
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30519: Overload resolution failed because no accessible 'New' can be called without a narrowing conversion:
    'Public Sub New(x As Short, y As String)': Argument matching parameter 'x' narrows from 'Integer' to 'Short'.
    'Public Sub New(x As Short, y As String)': Argument matching parameter 'y' narrows from 'Integer' to 'String'.
    'Public Sub New(x As String, y As Short)': Argument matching parameter 'x' narrows from 'Integer' to 'String'.
    'Public Sub New(x As String, y As Short)': Argument matching parameter 'y' narrows from 'Integer' to 'Short'.
<ProvideMenuResource(1000, 1)>
 ~~~~~~~~~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(546747, "DevDiv")>
        Public Sub Bug16716_3()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
<ProvideMenuResource(1000, 1)>
Public NotInheritable Class TNuggetPackage
End Class

Public Class ProvideMenuResourceAttribute
    Inherits System.Attribute

    Public Sub New(x As String, y As Short)
    End Sub
End Class
    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30934: Conversion from 'Integer' to 'String' cannot occur in a constant expression used as an argument to an attribute.
<ProvideMenuResource(1000, 1)>
                     ~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(546875, "DevDiv"), WorkItem(530930, "DevDiv")>
        Public Sub BigVisitor()
            Dim source =
                <compilation>
                    <file name="a.vb">
Public Module Test
    Sub Main()
        Dim visitor As New ConcreteVisitor()
        visitor.Visit(New Class090())
    End Sub
End Module
                    </file>
                </compilation>

            Dim libRef = TestReferences.SymbolsTests.BigVisitor

            Dim start = DateTime.UtcNow
            CreateCompilationWithMscorlibAndVBRuntimeAndReferences(source, {libRef}).VerifyDiagnostics()
            Dim elapsed = DateTime.UtcNow - start
            Assert.InRange(elapsed.TotalSeconds, 0, 5) ' The key is seconds - not minutes - so feel free to loosen.
        End Sub

        <Fact>
        Public Sub CompareSymbolsOriginalDefinition()
            Dim source =
                <compilation>
                    <file name="a.vb">
                        <![CDATA[
Imports System.Runtime.CompilerServices
<Assembly: InternalsVisibleTo("Foo")>

Public Class Test(Of t1, t2)
    Public Sub Add(x As t1)
    End Sub

    Friend Sub Add(x As t2)
    End Sub
End Class
]]>
                    </file>
                </compilation>

            Dim source2 =
                <compilation name="Foo">
                    <file name="b.vb">
Public Class Test2
    Public Sub Main()
        Dim x = New Test(Of Integer, Integer)()
        x.Add(5)
    End Sub
End Class
                    </file>
                </compilation>

            Dim comp = CreateCompilationWithMscorlib(source, TestOptions.ReleaseDll)

            Dim comp2 = CreateCompilationWithMscorlib(source2, references:={comp.EmitToImageReference()})
            CompilationUtils.AssertTheseDiagnostics(comp2,
                                               <expected>
BC30521: Overload resolution failed because no accessible 'Add' is most specific for these arguments:
    'Public Sub Add(x As Integer)': Not most specific.
    'Friend Sub Add(x As Integer)': Not most specific.
        x.Add(5)
          ~~~
                                               </expected>)
        End Sub

        <Fact(), WorkItem(738688, "DevDiv")>
        Public Sub Regress738688_1()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Module Module1
    Class C0Base
        Overloads Shared Widening Operator CType(x As C0Base) As NullReferenceException
            System.Console.Write("CType1")
            Return Nothing
        End Operator
    End Class
    Class C0
        Inherits C0Base
        Overloads Shared Widening Operator CType(x As C0) As NullReferenceException
            System.Console.Write("CType2")
            Return Nothing
        End Operator
    End Class

    Class C1Base
        Overloads Shared Widening Operator CType(x As C1Base) As NullReferenceException()
            System.Console.Write("CType3")
            Return Nothing
        End Operator
    End Class
    Class C1
        Inherits C1Base
        Overloads Shared Widening Operator CType(x As C1) As NullReferenceException()
            System.Console.Write("CType4")
            Return Nothing
        End Operator
    End Class

    Sub Main()
        Dim x1 As Exception = New C0
        Dim x2 As Exception() = New C1
    End Sub
End Module


]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(compilation, expectedOutput:="CType2CType4")
        End Sub

        <Fact(), WorkItem(738688, "DevDiv")>
        Public Sub Regress738688_2()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Module Module1
    Sub Main()
        C2.foo(New C2)
    End Sub

    Class C2
        Public Shared Widening Operator CType(x As C2) As C2()
            Return New C2() {}
        End Operator

        Public Shared Sub foo(x As String)
        End Sub
        Public Shared Sub foo(ParamArray y As C2())
            Console.WriteLine(y.Length)
        End Sub
    End Class
End Module


]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(OptionStrict.Off))

            CompileAndVerify(compilation, expectedOutput:="1")
        End Sub

        <Fact(), WorkItem(738688, "DevDiv")>
        Public Sub Regress738688Err()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On

Imports System.Collections

Module Module1

    Sub Main()
        cls2.Foo("qq", New cls2)
    End Sub
End Module

Interface IGetExpression

End Interface

Interface IExpression
    Inherits IGetExpression

End Interface

Class cls0
    Implements IExpression
End Class

Class cls1
    Implements IExpression

    Public Shared Widening Operator CType(x As cls1) As IExpression()
        System.Console.WriteLine("CType")
        Return Nothing
    End Operator
End Class

Class cls2
    Inherits cls1

    Public Shared Function Foo(x As String) As String
        Return x
    End Function

    Public Shared Function Foo(x As String, ByVal ParamArray params() As IGetExpression) As String
        System.Console.WriteLine("Foo")
        Return Nothing
    End Function
End Class

    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'Foo' is most specific for these arguments:
    'Public Shared Function Foo(x As String, ParamArray params As IGetExpression()) As String': Not most specific.
        cls2.Foo("qq", New cls2)
             ~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(738688, "DevDiv")>
        Public Sub Regress738688Err01()
            Dim compilation = CreateCompilationWithMscorlibAndVBRuntime(
<compilation>
    <file name="a.vb"><![CDATA[
Option Strict On
Imports System

Module Module1
    Sub Main()
        C2.foo(New C2)
    End Sub

    Interface i1
    End Interface

    Class C2
        Implements i1

        Public Shared Widening Operator CType(x As C2) As i1()
            Return New C2() {}
        End Operator

        ' uncommenting this will change results in VBC
        ' Public Shared Sub foo(x as string)
        ' End Sub

        Public Shared Sub foo(ParamArray y As i1())
            Console.WriteLine(y.Length)
        End Sub
    End Class
End Module


    ]]></file>
</compilation>)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30521: Overload resolution failed because no accessible 'foo' is most specific for these arguments:
    'Public Shared Sub foo(ParamArray y As Module1.i1())': Not most specific.
        C2.foo(New C2)
           ~~~
]]></expected>)
        End Sub

        <Fact(), WorkItem(32, "https://roslyn.codeplex.com/workitem/31")>
        Public Sub BugCodePlex_32()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim b As New B()
        b.Test(Function() 1)
    End Sub

End Module

Class A
    Sub Test(x As System.Func(Of Integer))
        System.Console.WriteLine("A.Test")
    End Sub
End Class

Class B
    Inherits A

    Overloads Sub Test(Of T)(x As System.Linq.Expressions.Expression(Of System.Func(Of T)))
        System.Console.WriteLine("B.Test")
    End Sub
End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntimeAndReferences(compilationDef, {SystemCoreRef}, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="A.Test")
        End Sub


        <Fact(), WorkItem(918579, "DevDiv"), WorkItem(34, "CodePlex")>
        Public Sub Bug918579_01()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim p As IDerived = New CTest()
        Dim x = p.X 
    End Sub

End Module

        Public Interface IBase1
            ReadOnly Property X As Integer
        End Interface

        Public Interface IBase2
            ReadOnly Property X As Integer
        End Interface

        Public Interface IDerived
            Inherits IBase1, IBase2

            Overloads ReadOnly Property X As Integer
        End Interface

        Class CTest
            Implements IDerived

            Public ReadOnly Property IDerived_X As Integer Implements IDerived.X
                Get
                    System.Console.WriteLine("IDerived_X")
                    Return 0
                End Get
            End Property

            Private ReadOnly Property IBase1_X As Integer Implements IBase1.X
                Get
                    System.Console.WriteLine("IBase1_X")
                    Return 0
                End Get
            End Property

            Private ReadOnly Property IBase2_X As Integer Implements IBase2.X
                Get
                    System.Console.WriteLine("IBase2_X")
                    Return 0
                End Get
            End Property
        End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:="IDerived_X")
        End Sub

        <Fact(), WorkItem(918579, "DevDiv"), WorkItem(34, "CodePlex")>
        Public Sub Bug918579_02()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        Dim p As IDerived = New CTest()
        Dim x = p.X(CInt(0)) 
        x = p.X(CShort(0)) 
        x = p.X(CLng(0)) 
    End Sub

End Module

        Public Interface IBase1
            ReadOnly Property X(y As Integer) As Integer
        End Interface

        Public Interface IBase2
            ReadOnly Property X(y As Short) As Integer
        End Interface

        Public Interface IDerived
            Inherits IBase1, IBase2

            Overloads ReadOnly Property X(y As Long) As Integer
        End Interface

        Class CTest
            Implements IDerived

            Public ReadOnly Property IDerived_X(y As Long) As Integer Implements IDerived.X
                Get
                    System.Console.WriteLine("IDerived_X")
                    Return 0
                End Get
            End Property

            Private ReadOnly Property IBase1_X(y As Integer) As Integer Implements IBase1.X
                Get
                    System.Console.WriteLine("IBase1_X")
                    Return 0
                End Get
            End Property

            Private ReadOnly Property IBase2_X(y As Short) As Integer Implements IBase2.X
                Get
                    System.Console.WriteLine("IBase2_X")
                    Return 0
                End Get
            End Property
        End Class
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation, expectedOutput:=
"IBase1_X
IBase2_X
IDerived_X")
        End Sub

        <Fact(), WorkItem(918579, "DevDiv"), WorkItem(34, "CodePlex")>
        Public Sub Bug918579_03()
            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
    End Sub

    Sub Test(x as I3)
        x.M1()
    End Sub

    Sub Test(x as I4)
        x.M1()
    End Sub
End Module

Interface I1(Of T)
    Sub M1()
End Interface 

Interface I2
    Inherits I1(Of String)
    Shadows Sub M1(x as Integer)
End Interface 

Interface I3
    Inherits I2, I1(Of Integer)
End Interface 

Interface I4
    Inherits I1(Of Integer), I2
End Interface 
]]></file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompileAndVerify(compilation)
        End Sub

        <Fact, WorkItem(1034429, "DevDiv")>
        Public Sub Bug1034429()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Security.Permissions

Public Class A
    Inherits Attribute

    Public Sub New(ByVal ParamArray p As SecurityAction)

    End Sub
End Class

Public Class B
    Inherits Attribute

    Public Sub New(ByVal p1 As Integer, ByVal ParamArray p2 As SecurityAction)

    End Sub
End Class

Public Class C
    Inherits Attribute

    Public Sub New(ByVal p1 As Integer, ByVal ParamArray p2 As SecurityAction, ByVal p3 As String)

    End Sub
End Class

Module Module1

    <A(SecurityAction.Assert)>
    <B(p2:=SecurityAction.Assert, p1:=0)>
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
    Sub Main()

    End Sub

End Module
]]>

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30050: ParamArray parameter must be an array.
    Public Sub New(ByVal ParamArray p As SecurityAction)
                                    ~
BC30050: ParamArray parameter must be an array.
    Public Sub New(ByVal p1 As Integer, ByVal ParamArray p2 As SecurityAction)
                                                         ~~
BC30050: ParamArray parameter must be an array.
    Public Sub New(ByVal p1 As Integer, ByVal ParamArray p2 As SecurityAction, ByVal p3 As String)
                                                         ~~
BC30192: End of parameter list expected. Cannot define parameters after a paramarray parameter.
    Public Sub New(ByVal p1 As Integer, ByVal ParamArray p2 As SecurityAction, ByVal p3 As String)
                                                                               ~~~~~~~~~~~~~~~~~~
BC31092: ParamArray parameters must have an array type.
    <A(SecurityAction.Assert)>
     ~
BC30455: Argument not specified for parameter 'p1' of 'Public Sub New(p1 As Integer, ParamArray p2 As SecurityAction)'.
    <B(p2:=SecurityAction.Assert, p1:=0)>
     ~
BC31092: ParamArray parameters must have an array type.
    <B(p2:=SecurityAction.Assert, p1:=0)>
     ~
BC30661: Field or property 'p2' is not found.
    <B(p2:=SecurityAction.Assert, p1:=0)>
       ~~
BC30661: Field or property 'p1' is not found.
    <B(p2:=SecurityAction.Assert, p1:=0)>
                                  ~~
BC30455: Argument not specified for parameter 'p1' of 'Public Sub New(p1 As Integer, ParamArray p2 As SecurityAction, p3 As String)'.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
     ~
BC30455: Argument not specified for parameter 'p2' of 'Public Sub New(p1 As Integer, ParamArray p2 As SecurityAction, p3 As String)'.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
     ~
BC30455: Argument not specified for parameter 'p3' of 'Public Sub New(p1 As Integer, ParamArray p2 As SecurityAction, p3 As String)'.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
     ~
BC30661: Field or property 'p3' is not found.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
       ~~
BC30661: Field or property 'p2' is not found.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
                    ~~
BC30661: Field or property 'p1' is not found.
    <C(p3:="again", p2:=SecurityAction.Assert, p1:=0)>
                                               ~~
]]></expected>)
        End Sub

        <Fact, WorkItem(2604, "https://github.com/dotnet/roslyn/issues/2604")>
        Public Sub FailureDueToAnErrorInALambda_01()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Module Module1

    Sub Main()
        M0(0, Function() doesntexist)
        M1(0, Function() doesntexist)
        M2(0, Function() doesntexist)
    End Sub

    Sub M0(x As Integer, y As System.Func(Of Integer))
    End Sub

    Sub M1(x As Integer, y As System.Func(Of Integer))
    End Sub

    Sub M1(x As Long, y As System.Func(Of Long))
    End Sub

    Sub M2(x As Integer, y As System.Func(Of Integer))
    End Sub

    Sub M2(x As c1, y As System.Func(Of Long))
    End Sub
End Module

Class c1
End Class
]]>

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(compilationDef, TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'doesntexist' is not declared. It may be inaccessible due to its protection level.
        M0(0, Function() doesntexist)
                         ~~~~~~~~~~~
BC30451: 'doesntexist' is not declared. It may be inaccessible due to its protection level.
        M1(0, Function() doesntexist)
                         ~~~~~~~~~~~
BC30451: 'doesntexist' is not declared. It may be inaccessible due to its protection level.
        M2(0, Function() doesntexist)
                         ~~~~~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(4587, "https://github.com/dotnet/roslyn/issues/4587")>
        Public Sub FailureDueToAnErrorInALambda_02()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Linq

Module Module1

    Sub Main()
    End Sub



    Private Async Function GetDataAsync(cs As Characters, imax As Integer) As Task
        Dim Roles = Await cs.GetRoleAsync()

        Dim RoleTasks = Roles.Select(
            Async Function(role As Role) As Task
                Dim Lines = Await role.GetLines()
                If imax <= LinesKey Then Return

                Dim SentenceTasks = Lines.Select(
                    Async Function(Sentence) As Task
                        Dim Words = Await Sentence.GetWordsAsync()
                        If imax <= WordsKey Then Return

                        Dim WordTasks = Words.Select(
                            Async Function(Word) As Task
                                Dim Letters = Await Word.GetLettersAsync()
                                If imax <= LettersKey Then Return

                                Dim StrokeTasks = Letters.Select(
                                    Async Function(Stroke) As Task
                                        Dim endpoints = Await Stroke.GetEndpointsAsync()

                                        Await Task.WhenAll(endpoints.ToArray())
                                    End Function)
                                Await Task.WhenAll(StrokeTasks.ToArray())
                            End Function)
                        Await Task.WhenAll(WordTasks.ToArray())
                    End Function)
                Await Task.WhenAll(SentenceTasks.ToArray())
            End Function)
    End Function



    Function RetryAsync(Of T)(f As Func(Of Task(Of T))) As Task(Of T)
        Return f()
    End Function

End Module


Friend Class Characters
    Function GetRoleAsync() As Task(Of List(Of Role))
        Return Nothing
    End Function
End Class

Class Role
    Function GetLines() As Task(Of List(Of Line))
        Return Nothing
    End Function
End Class

Public Class Line
End Class
]]>

    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(compilationDef, additionalRefs:={SystemCoreRef}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'LinesKey' is not declared. It may be inaccessible due to its protection level.
                If imax <= LinesKey Then Return
                           ~~~~~~~~
BC30456: 'GetWordsAsync' is not a member of 'Line'.
                        Dim Words = Await Sentence.GetWordsAsync()
                                          ~~~~~~~~~~~~~~~~~~~~~~
BC30451: 'WordsKey' is not declared. It may be inaccessible due to its protection level.
                        If imax <= WordsKey Then Return
                                   ~~~~~~~~
BC30518: Overload resolution failed because no accessible 'WhenAll' can be called with these arguments:
    'Public Shared Overloads Function WhenAll(Of TResult)(tasks As IEnumerable(Of Task(Of TResult))) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
    'Public Shared Overloads Function WhenAll(Of TResult)(ParamArray tasks As Task(Of TResult)()) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
                        Await Task.WhenAll(WordTasks.ToArray())
                                   ~~~~~~~
BC30518: Overload resolution failed because no accessible 'WhenAll' can be called with these arguments:
    'Public Shared Overloads Function WhenAll(Of TResult)(tasks As IEnumerable(Of Task(Of TResult))) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
    'Public Shared Overloads Function WhenAll(Of TResult)(ParamArray tasks As Task(Of TResult)()) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
                Await Task.WhenAll(SentenceTasks.ToArray())
                           ~~~~~~~
]]></expected>)
        End Sub

        <Fact, WorkItem(4587, "https://github.com/dotnet/roslyn/issues/4587")>
        Public Sub FailureDueToAnErrorInALambda_03()

            Dim compilationDef =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports System.Linq

Module Module1
 
    Sub Main()
    End Sub

 
    Private Async Function GetDataAsync(DeliveryWindow As DeliveryWindow,
                                        MaxDepth As Integer) As Task
 
        Dim Vendors = Await RetryAsync(Function() DeliveryWindow.GetVendorsAsync())
 
        Dim VendorTasks = Vendors.Select(Async Function(vendor As DeliveryWindowVendor) As Task
                                             Dim Departments = Await RetryAsync(Async Function() Await vendor.GetDeliveryWindowDepartmentsAsync())
 
                                             If MaxDepth <= DepartmentsKey Then
                                                 Return
                                             End If
 
                                             Dim DepartmentTasks = Departments.Select(Async Function(Department) As Task
                                                                                          Dim Vendor9s = Await RetryAsync(Async Function() Await Department.GetDeliveryWindowVendor9Async())
 
                                                                                          If MaxDepth <= Vendor9Key Then
                                                                                              Return
                                                                                          End If
 
                                                                                          Dim Vendor9Tasks = Vendor9s.Select(Async Function(Vendor9) As Task
                                                                                                                                 Dim poTypes = Await RetryAsync(Async Function() Await Vendor9.GetDeliveryWindowPOTypesAsync())
 
                                                                                                                                 If MaxDepth <= POTypesKey Then
                                                                                                                                     Return
                                                                                                                                 End If
 
                                                                                                                                 Dim POTypeTasks = poTypes.Select(Async Function(poType) As Task
                                                                                                                                                                      Dim pos = Await RetryAsync(Async Function() Await poType.GetDeliveryWindowPOAsync())
 
                                                                                                                                                                      If MaxDepth <= POsKey Then
                                                                                                                                                                          Return
                                                                                                                                                                      End If
 
                                                                                                                                                                      Dim POTasks = pos.ToList() _
                                                                                                                                                                                       .Select(Async Function(po) As Task
                                                                                                                                                                                                   Await RetryAsync(Async Function() Await po.GetDeliveryWindowPOLineAsync())
                                                                                                                                                                                               End Function) _
                                                                                                                                                                                       .ToArray()
 
 
                                                                                                                                                                      Await Task.WhenAll(POTasks.ToArray())
                                                                                                                                                                  End Function)
 
 
                                                                                                                                 Await Task.WhenAll(POTypeTasks.ToArray())
                                                                                                                             End Function)
 
 
                                                                                          Await Task.WhenAll(Vendor9Tasks.ToArray())
                                                                                      End Function)
 
                                             Await Task.WhenAll(DepartmentTasks.ToArray())
                                         End Function)
 
        Await Task.WhenAll(VendorTasks.ToArray())
    End Function
 
    Function RetryAsync(Of T)(f As Func(Of Task(Of T))) As Task(Of T)
        Return f()
    End Function
End Module
 
Friend Class DeliveryWindow
    Function GetVendorsAsync() As Task(Of List(Of DeliveryWindowVendor))
        Return Nothing
    End Function
End Class
 
Class DeliveryWindowVendor
    Function GetDeliveryWindowDepartmentsAsync() As Task(Of List(Of DeliveryWindowDepartments))
        Return Nothing
    End Function
End Class
 
Public Class DeliveryWindowDepartments
End Class
]]>
    </file>
</compilation>

            Dim compilation = CompilationUtils.CreateCompilationWithMscorlib45AndVBRuntime(compilationDef, additionalRefs:={SystemCoreRef}, options:=TestOptions.ReleaseExe)

            CompilationUtils.AssertTheseDiagnostics(compilation,
<expected><![CDATA[
BC30451: 'DepartmentsKey' is not declared. It may be inaccessible due to its protection level.
                                             If MaxDepth <= DepartmentsKey Then
                                                            ~~~~~~~~~~~~~~
BC30456: 'GetDeliveryWindowVendor9Async' is not a member of 'DeliveryWindowDepartments'.
                                                                                          Dim Vendor9s = Await RetryAsync(Async Function() Await Department.GetDeliveryWindowVendor9Async())
                                                                                                                                                 ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC30451: 'Vendor9Key' is not declared. It may be inaccessible due to its protection level.
                                                                                          If MaxDepth <= Vendor9Key Then
                                                                                                         ~~~~~~~~~~
BC30518: Overload resolution failed because no accessible 'WhenAll' can be called with these arguments:
    'Public Shared Overloads Function WhenAll(Of TResult)(tasks As IEnumerable(Of Task(Of TResult))) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
    'Public Shared Overloads Function WhenAll(Of TResult)(ParamArray tasks As Task(Of TResult)()) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
                                                                                          Await Task.WhenAll(Vendor9Tasks.ToArray())
                                                                                                     ~~~~~~~
BC30518: Overload resolution failed because no accessible 'WhenAll' can be called with these arguments:
    'Public Shared Overloads Function WhenAll(Of TResult)(tasks As IEnumerable(Of Task(Of TResult))) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
    'Public Shared Overloads Function WhenAll(Of TResult)(ParamArray tasks As Task(Of TResult)()) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
                                             Await Task.WhenAll(DepartmentTasks.ToArray())
                                                        ~~~~~~~
BC30518: Overload resolution failed because no accessible 'WhenAll' can be called with these arguments:
    'Public Shared Overloads Function WhenAll(Of TResult)(tasks As IEnumerable(Of Task(Of TResult))) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
    'Public Shared Overloads Function WhenAll(Of TResult)(ParamArray tasks As Task(Of TResult)()) As Task(Of TResult())': Type parameter 'TResult' cannot be inferred.
        Await Task.WhenAll(VendorTasks.ToArray())
                   ~~~~~~~
]]></expected>)
        End Sub

    End Class
End Namespace
