' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Linq
Imports System.Text
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.CodeGen
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.RuntimeMembers
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Emit
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary> 
    ''' A helper class for synthesizing quantities of code. 
    ''' </summary>    
    ''' <remarks>
    ''' Code if the #If False out is code ported from C# that isn't currently used, and
    ''' hence has no code coverage. It may or may not work correctly, but should be a useful
    ''' starting point.
    ''' </remarks>
    Friend Class SyntheticBoundNodeFactory
        Private _currentClass As NamedTypeSymbol
        Private _syntax As SyntaxNode

        Public ReadOnly Diagnostics As BindingDiagnosticBag
        Public ReadOnly TopLevelMethod As MethodSymbol
        Public ReadOnly CompilationState As TypeCompilationState

        Public Property CurrentMethod As MethodSymbol

        Public ReadOnly Property CurrentType As NamedTypeSymbol
            Get
                Return Me._currentClass
            End Get
        End Property

        Public ReadOnly Property Compilation As VisualBasicCompilation
            Get
                Return Me.CompilationState.Compilation
            End Get
        End Property

        Public Property Syntax As SyntaxNode
            Get
                Return _syntax
            End Get
            Set(value As SyntaxNode)
                _syntax = value
            End Set
        End Property

        Private ReadOnly Property EmitModule As PEModuleBuilder
            Get
                Return If(Me.CompilationState IsNot Nothing, Me.CompilationState.ModuleBuilderOpt, Nothing)
            End Get
        End Property

        Public Sub New(topLevelMethod As MethodSymbol, currentMethod As MethodSymbol, node As SyntaxNode, compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag)
            Me.New(topLevelMethod, currentMethod, Nothing, node, compilationState, diagnostics)
        End Sub

        Public Sub New(topLevelMethod As MethodSymbol, currentMethod As MethodSymbol, currentClass As NamedTypeSymbol, node As SyntaxNode, compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag)
            Debug.Assert(compilationState IsNot Nothing)
            Me.CompilationState = compilationState
            Me.CurrentMethod = currentMethod
            Me.TopLevelMethod = topLevelMethod
            Me._currentClass = currentClass
            Me._syntax = node
            Me.Diagnostics = diagnostics
        End Sub

        Public Sub AddNestedType(nestedType As NamedTypeSymbol)
            Dim [module] As PEModuleBuilder = Me.EmitModule
            If [module] IsNot Nothing Then
                [module].AddSynthesizedDefinition(_currentClass, nestedType.GetCciAdapter())
            End If
        End Sub

        Public Sub OpenNestedType(nestedType As NamedTypeSymbol)
            AddNestedType(nestedType)
            Me._currentClass = nestedType
            Me.CurrentMethod = Nothing
        End Sub

        Public Sub AddField(containingType As NamedTypeSymbol, field As FieldSymbol)
            Dim [module] As PEModuleBuilder = Me.EmitModule
            If [module] IsNot Nothing Then
                [module].AddSynthesizedDefinition(containingType, field.GetCciAdapter())
            End If
        End Sub

        Public Sub AddMethod(containingType As NamedTypeSymbol, method As MethodSymbol)
            Dim [module] As PEModuleBuilder = Me.EmitModule
            If [module] IsNot Nothing Then
                [module].AddSynthesizedDefinition(containingType, method.GetCciAdapter())
            End If
        End Sub

        Public Sub AddProperty(containingType As NamedTypeSymbol, prop As PropertySymbol)
            Dim [module] As PEModuleBuilder = Me.EmitModule
            If [module] IsNot Nothing Then
                [module].AddSynthesizedDefinition(containingType, prop.GetCciAdapter())
            End If
        End Sub

        Public Function StateMachineField(type As TypeSymbol, implicitlyDefinedBy As Symbol, name As String, Optional accessibility As Accessibility = Accessibility.Private) As SynthesizedFieldSymbol
            Dim result As New StateMachineFieldSymbol(Me.CurrentType, implicitlyDefinedBy, type, name, accessibility:=accessibility)
            AddField(CurrentType, result)
            Return result
        End Function

        Public Function StateMachineField(type As TypeSymbol, implicitlyDefinedBy As Symbol, name As String, synthesizedKind As SynthesizedLocalKind, slotIndex As Integer, Optional accessibility As Accessibility = Accessibility.Private) As SynthesizedFieldSymbol
            Dim result As New StateMachineFieldSymbol(Me.CurrentType, implicitlyDefinedBy, type, name, synthesizedKind, slotIndex, accessibility)
            AddField(CurrentType, result)
            Return result
        End Function

        Public Function StateMachineField(type As TypeSymbol, implicitlyDefinedBy As Symbol, name As String, slotDebugInfo As LocalSlotDebugInfo, slotIndex As Integer, Optional accessibility As Accessibility = Accessibility.Private) As SynthesizedFieldSymbol
            Dim result As New StateMachineFieldSymbol(Me.CurrentType, implicitlyDefinedBy, type, name, slotDebugInfo, slotIndex, accessibility)
            AddField(CurrentType, result)
            Return result
        End Function

#If False Then

        Public Sub AddField(field As FieldSymbol)
            EmitModule.AddCompilerGeneratedDefinition(_currentClass, field)
        End Sub

        Public Function AddField(type As TypeSymbol, name As [String], Optional isPublic As Boolean = False) As SynthesizedFieldSymbol
            Dim result = New SynthesizedFieldSymbol(_currentClass, _currentClass, type, name, isPublic:=isPublic)
            AddField(result)
            Return result
        End Function

        Public Sub AddMethod(method As MethodSymbol)
            EmitModule.AddCompilerGeneratedDefinition(_currentClass, method)
            Me.CurrentMethod = method
        End Sub

#End If

        Public Function GenerateLabel(prefix As String) As GeneratedLabelSymbol
            Return New GeneratedLabelSymbol(prefix)
        End Function

        Public Function [Me]() As BoundMeReference
            Debug.Assert(Me.CurrentMethod IsNot Nothing AndAlso Not Me.CurrentMethod.IsShared)
            Dim boundNode = New BoundMeReference(_syntax, Me.CurrentMethod.MeParameter.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function ReferenceOrByrefMe() As BoundExpression
            Debug.Assert(Me.CurrentMethod IsNot Nothing AndAlso Not Me.CurrentMethod.IsShared)

            Dim type = Me.CurrentMethod.MeParameter.Type

            Dim boundNode = If(type.IsReferenceType,
                                DirectCast(Me.Me, BoundExpression),
                                New BoundValueTypeMeReference(_syntax, Me.CurrentMethod.MeParameter.Type))

            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Base() As BoundMyBaseReference
            Debug.Assert(Me.CurrentMethod IsNot Nothing AndAlso Not Me.CurrentMethod.IsShared)
            Dim boundNode = New BoundMyBaseReference(_syntax, Me.CurrentMethod.MeParameter.Type.BaseTypeNoUseSiteDiagnostics)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Parameter(p As ParameterSymbol) As BoundParameter
            Dim boundNode = New BoundParameter(_syntax, p, p.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Field(receiver As BoundExpression, f As FieldSymbol, isLValue As Boolean) As BoundFieldAccess
            Dim boundNode = New BoundFieldAccess(_syntax, receiver, f, isLValue, f.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [Property](member As WellKnownMember) As BoundExpression
            Dim propertySym As PropertySymbol = WellKnownMember(Of PropertySymbol)(member)
            'if (propertySym == null) return BoundBadExpression
            Debug.Assert(propertySym.IsShared)
            Return [Call](Nothing, propertySym.GetMethod())
        End Function

        Public Function [Property](receiver As BoundExpression, member As WellKnownMember) As BoundExpression
            Dim propertySym As PropertySymbol = WellKnownMember(Of PropertySymbol)(member)
            Debug.Assert(receiver.Type.GetMembers(propertySym.Name).OfType(Of PropertySymbol)().Single() = propertySym)
            'if (propertySym == null) return BoundBadExpression
            Debug.Assert(Not propertySym.IsShared)
            Return [Call](receiver, propertySym.GetMethod())
        End Function

        Public Function [Property](receiver As BoundExpression, name As String) As BoundExpression
            ' TODO: unroll loop and add diagnostics for failure
            ' TODO: should we use GetBaseProperty() to ensure we generate a call to the overridden method?
            ' TODO: replace this with a mechanism that uses WellKnownMember instead of string names.
            Dim propertySym = receiver.Type.GetMembers(name).OfType(Of PropertySymbol)().[Single]()
            Debug.Assert(Not propertySym.IsShared)
            Return [Call](receiver, propertySym.GetMethod())
        End Function

        Public Function [Property](receiver As NamedTypeSymbol, name As String) As BoundExpression
            ' TODO: unroll loop and add diagnostics for failure
            Dim propertySym = receiver.GetMembers(name).OfType(Of PropertySymbol)().[Single]()
            Debug.Assert(propertySym.IsShared)
            Return [Call](Nothing, propertySym.GetMethod())
        End Function

        Public Function SpecialType(st As SpecialType) As NamedTypeSymbol
            Return Binder.GetSpecialType(Me.Compilation, st, _syntax, Me.Diagnostics)
        End Function

        Public Function NullableOf(type As TypeSymbol) As NamedTypeSymbol
            ' Get the Nullable type
            Dim nullableType As NamedTypeSymbol = SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Nullable_T)

            If nullableType.IsErrorType Then
                Return nullableType
            End If

            Debug.Assert(nullableType.IsGenericType AndAlso nullableType.Arity = 1)

            ' Construct the Nullable(Of T).
            Return nullableType.Construct(ImmutableArray.Create(type))
        End Function

        Public Function WellKnownType(wt As WellKnownType) As NamedTypeSymbol
            Return Binder.GetWellKnownType(Me.Compilation, wt, _syntax, Me.Diagnostics)
        End Function

        Public Function WellKnownMember(Of T As Symbol)(wm As WellKnownMember, Optional isOptional As Boolean = False) As T
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol) = Nothing
            Dim member = DirectCast(Binder.GetWellKnownTypeMember(Me.Compilation, wm, useSiteInfo), T)

            If useSiteInfo.DiagnosticInfo IsNot Nothing AndAlso isOptional Then
                member = Nothing
            Else
                Me.Diagnostics.Add(useSiteInfo, _syntax)
            End If

            Return member
        End Function

        Public Function SpecialMember(sm As SpecialMember) As Symbol
            Dim memberSymbol As Symbol = Me.Compilation.GetSpecialTypeMember(sm)
            Dim useSiteInfo As UseSiteInfo(Of AssemblySymbol)

            If memberSymbol Is Nothing Then
                Dim memberDescriptor As MemberDescriptor = SpecialMembers.GetDescriptor(sm)
                useSiteInfo = New UseSiteInfo(Of AssemblySymbol)(GetDiagnosticForMissingRuntimeHelper(memberDescriptor.DeclaringTypeMetadataName, memberDescriptor.Name, CompilationState.Compilation.Options.EmbedVbCoreRuntime))
            Else
                useSiteInfo = Binder.GetUseSiteInfoForMemberAndContainingType(memberSymbol)
            End If

            Me.Diagnostics.Add(useSiteInfo, _syntax)
            Return memberSymbol
        End Function

#If False Then
        Public Function SpecialMember(sm As SpecialMember) As Symbol
            Return Compilation.GetSpecialTypeMember(sm)
        End Function
#End If

        Public Function Assignment(left As BoundExpression, right As BoundExpression) As BoundExpressionStatement
            Return ExpressionStatement(AssignmentExpression(left, right))
        End Function

        Public Function ExpressionStatement(expr As BoundExpression) As BoundExpressionStatement
            Dim boundNode = New BoundExpressionStatement(_syntax, expr)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Assignment expressions in lowered form should always have suppressObjectClone = True
        ''' </summary>
        Public Function AssignmentExpression(left As BoundExpression, right As BoundExpression) As BoundAssignmentOperator
            Debug.Assert(left.Type.IsSameTypeIgnoringAll(right.Type) OrElse right.Type.IsErrorType() OrElse left.Type.IsErrorType())
            Dim boundNode = New BoundAssignmentOperator(_syntax, left, right, True)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function ReferenceAssignment(byRefLocal As LocalSymbol, lValue As BoundExpression) As BoundReferenceAssignment
            Debug.Assert(TypeSymbol.Equals(byRefLocal.Type, lValue.Type, TypeCompareKind.ConsiderEverything))
            Debug.Assert(byRefLocal.IsByRef)

            Dim boundNode = New BoundReferenceAssignment(_syntax, Local(byRefLocal, isLValue:=True), lValue, isLValue:=True, type:=lValue.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Block(statements As ImmutableArray(Of BoundStatement)) As BoundBlock
            Return Block(ImmutableArray(Of LocalSymbol).Empty, statements)
        End Function

        Public Function Block(locals As ImmutableArray(Of LocalSymbol), statements As ImmutableArray(Of BoundStatement)) As BoundBlock
            Dim boundNode = New BoundBlock(_syntax, Nothing, locals, statements)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Block() As BoundBlock
            Return Block(ImmutableArray(Of BoundStatement).Empty)
        End Function

        Public Function Block(ParamArray statements As BoundStatement()) As BoundBlock
            Return Block(ImmutableArray.Create(Of BoundStatement)(statements))
        End Function

        Public Function Block(locals As ImmutableArray(Of LocalSymbol), ParamArray statements As BoundStatement()) As BoundBlock
            Return Block(locals, ImmutableArray.Create(Of BoundStatement)(statements))
        End Function

        Public Function StatementList() As BoundStatementList
            Return StatementList(ImmutableArray(Of BoundStatement).Empty)
        End Function

        Public Function StatementList(statements As ImmutableArray(Of BoundStatement)) As BoundStatementList
            Dim boundNode As New BoundStatementList(Syntax, statements)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function StatementList(first As BoundStatement, second As BoundStatement) As BoundStatementList
            Dim boundNode As New BoundStatementList(Syntax, ImmutableArray.Create(first, second))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [Return](Optional expression As BoundExpression = Nothing) As BoundReturnStatement
            If expression IsNot Nothing Then
                ' If necessary, add a conversion on the return expression.
                Dim useSiteInfo As New CompoundUseSiteInfo(Of AssemblySymbol)(Diagnostics, Me.Compilation.Assembly)
                Dim conversion = Conversions.ClassifyDirectCastConversion(expression.Type, Me.CurrentMethod.ReturnType, useSiteInfo)
                Debug.Assert(Conversions.IsWideningConversion(conversion))
                Diagnostics.Add(expression, useSiteInfo)

                If Not Conversions.IsIdentityConversion(conversion) Then
                    expression = New BoundDirectCast(Me.Syntax, expression, conversion, Me.CurrentMethod.ReturnType)
                End If
            End If

            Dim boundNode = New BoundReturnStatement(_syntax, expression, Nothing, Nothing)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

#If False Then

        Public Sub Generate(body As BoundStatement)
            Debug.Assert(Me.CurrentMethod IsNot Nothing)
            If body.Kind <> BoundKind.Block Then
                body = Block(body)
            End If

            Me.compilationState.AddGeneratedMethod(Me.CurrentMethod, body)
            Me.CurrentMethod = Nothing
        End Sub

        Public Function SynthesizedImplementation(type As NamedTypeSymbol, methodName As String, Optional debuggerHidden As Boolean = False) As SynthesizedImplementationMethod
            ' TODO: use WellKnownMembers instead of strings.
            Dim methodToImplement = DirectCast(type.GetMembers(methodName).[Single](), MethodSymbol)
            Dim result = New SynthesizedImplementationMethod(methodToImplement, CurrentClass, debuggerHidden:=debuggerHidden)
            EmitModule.AddCompilerGeneratedDefinition(_currentClass, result)
            Me.MethodImplementations.Add(New MethodImplementation(result, methodToImplement))
            Me.CurrentMethod = result
            Return result
        End Function

        Public Function SynthesizedPropertyImplementation(type As NamedTypeSymbol, propertyName As String, Optional debuggerHidden As Boolean = False) As SynthesizedImplementationMethod
            ' TODO: use WellKnownMembers instead of strings.
            ' TODO: share code with SynthesizedImplementation(...)
            Dim methodToImplement = (DirectCast(type.GetMembers(propertyName).[Single](), PropertySymbol)).GetMethod
            Dim result = New SynthesizedImplementationMethod(methodToImplement, CurrentClass, debuggerHidden:=debuggerHidden)
            EmitModule.AddCompilerGeneratedDefinition(_currentClass, result)
            Me.MethodImplementations.Add(New MethodImplementation(result, methodToImplement))
            Me.CurrentMethod = result
            Return result
        End Function
#End If
        Public Function SynthesizedLocal(type As TypeSymbol, Optional kind As SynthesizedLocalKind = SynthesizedLocalKind.LoweringTemp, Optional syntax As SyntaxNode = Nothing) As LocalSymbol
            Return New SynthesizedLocal(Me.CurrentMethod, type, kind, syntax)
        End Function

        Public Function SynthesizedParameter(type As TypeSymbol, name As String, Optional container As MethodSymbol = Nothing, Optional ordinal As Integer = 0) As ParameterSymbol
            Return New SynthesizedParameterSymbol(container, type, ordinal, False, name)
        End Function

#If False Then

        Public Function Binary(kind As BinaryOperatorKind, type As TypeSymbol, left As BoundExpression, right As BoundExpression, isChecked As Boolean) As BoundBinaryOperator
            Dim boundNode = New BoundBinaryOperator(Me._syntax, kind, left, right, isChecked, type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function IntNotEqual(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Return Binary(BinaryOperatorKind.NotEquals, SpecialType(Roslyn.Compilers.SpecialType.System_Boolean), left, right, False)
        End Function

#End If

        Public Function LogicalAndAlso(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Return Binary(BinaryOperatorKind.AndAlso, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right)
        End Function

        Public Function LogicalOrElse(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Return Binary(BinaryOperatorKind.OrElse, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right)
        End Function

        Public Function IntEqual(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Return Binary(BinaryOperatorKind.Equals, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right)
        End Function

        Public Function IntLessThan(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Return Binary(BinaryOperatorKind.LessThan, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right)
        End Function

        Public Function Literal(value As Boolean) As BoundLiteral
            Dim boundNode = New BoundLiteral(_syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Literal(value As Integer) As BoundLiteral
            Dim boundNode = New BoundLiteral(_syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Literal(value As StateMachineState) As BoundLiteral
            Return Literal(CType(value, Integer))
        End Function

        Public Function BadExpression(ParamArray subExpressions As BoundExpression()) As BoundExpression
            Dim boundNode = New BoundBadExpression(_syntax, LookupResultKind.Empty, ImmutableArray(Of Symbol).Empty, ImmutableArray.Create(subExpressions), ErrorTypeSymbol.UnknownResultType, hasErrors:=True)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [New](type As NamedTypeSymbol) As BoundObjectCreationExpression
            ' TODO: add diagnostics for when things fall apart
            Dim ctor = type.InstanceConstructors.Single(Function(c) c.ParameterCount = 0)
            Return [New](ctor)
        End Function

        Public Function [New](ctor As MethodSymbol, ParamArray args As BoundExpression()) As BoundObjectCreationExpression
            Dim boundNode = New BoundObjectCreationExpression(_syntax, ctor, ImmutableArray.Create(args), Nothing, ctor.ContainingType)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [New](ctor As MethodSymbol) As BoundObjectCreationExpression
            Dim boundNode = New BoundObjectCreationExpression(_syntax,
                                                              ctor,
                                                              ImmutableArray(Of BoundExpression).Empty,
                                                              Nothing,
                                                              ctor.ContainingType)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

#If False Then
        ' These function do their own overload resolution. Probably will remove them or replace with helpers that use correct overload resolution.

        Public Function StaticCall(receiver As TypeSymbol, name As String, ParamArray args As BoundExpression()) As BoundExpression
            Return StaticCall(receiver, name, ImmutableArray(Of TypeSymbol).Empty, args)
        End Function

        Public Function StaticCall(receiver As TypeSymbol, name As String, typeArgs As ImmutableArray(Of TypeSymbol), ParamArray args As BoundExpression()) As BoundExpression
            Dim m As MethodSymbol = FindMethod(receiver, name, typeArgs, args)
            If m Is Nothing Then
                Return New BoundBadExpression(_syntax, Nothing, ImmutableArray(Of Symbol).Empty, ImmutableArray(Of BoundNode).CreateFrom(Of BoundExpression)(args), receiver)
            End If

            Return [Call](Nothing, m, args)
        End Function

        Function FindMethod(receiver As TypeSymbol, name As String, typeArgs As ImmutableArray(Of TypeSymbol), args As BoundExpression()) As MethodSymbol
            Dim found As MethodSymbol = Nothing
            Dim ambiguous As Boolean = False
            Dim candidates As ImmutableArray(Of Symbol) = receiver.GetMembers(name)
            For Each m In candidates
                Dim method = TryCast(m, MethodSymbol)
                If method Is Nothing OrElse method.Arity <> typeArgs.Count OrElse method.ParameterCount <> args.Length Then
                    Continue For
                End If

                If method.Arity <> 0 Then
                    method = method.Construct(typeArgs)
                End If

                Dim parameters = method.Parameters
                Dim exact As Boolean = True
                For i = 0 To args.Length - 1
                    If parameters(i).IsByRef OrElse Not _compilation.ClassifyConversion(args(i).Type, parameters(i).Type).IsWidening Then
                        GoTo nextm
                    End If

                    exact = exact AndAlso args(i).Type = parameters(i).Type
                Next

                If exact Then
                    Return method
                End If

                If found IsNot Nothing Then
                    ambiguous = True
                End If

                found = method
nextm:
            Next

            ' TODO: (EXPRTREE) These error codes are probably not correct. Fix them.
            If ambiguous Then
                ReportLibraryProblem(ERRID.ERR_MissingRuntimeHelper, receiver, name, typeArgs, args) ' C#: ERR_LibraryMethodNotUnique
            ElseIf found Is Nothing Then
                ReportLibraryProblem(ERRID.ERR_MissingRuntimeHelper, receiver, name, typeArgs, args) ' C#: ERR_LibraryMethodNotFound
            End If

            Return found
        End Function

        Function ReportLibraryProblem(code As ERRID, receiver As TypeSymbol, name As String, typeArgs As ImmutableArray(Of TypeSymbol), args As BoundExpression()) As MethodSymbol
            Dim methodSig = New StringBuilder()
            Dim wasFirst As Boolean
            methodSig.Append(name)
            If Not typeArgs.IsNullOrEmpty Then
                methodSig.Append("(Of ")
                wasFirst = True
                For Each t In typeArgs
                    If Not wasFirst Then
                        methodSig.Append(", ")
                    End If

                    methodSig.Append(t.ToDisplayString())
                    wasFirst = False
                Next

                methodSig.Append(")")
            End If

            methodSig.Append("(")
            wasFirst = True
            For Each a In args
                If Not wasFirst Then
                    methodSig.Append(", ")
                End If

                methodSig.Append(a.Type.ToDisplayString())
                wasFirst = False
            Next

            methodSig.Append(")")
            _diagnostics.Add(code, _syntax.GetLocation(), receiver, methodSig.ToString())
            Return Nothing
        End Function

#End If

        Public Function [Call](receiver As BoundExpression, method As MethodSymbol) As BoundCall
            Return [Call](receiver, method, ImmutableArray(Of BoundExpression).Empty)
        End Function

        Public Function [Call](receiver As BoundExpression, method As MethodSymbol, ParamArray args As BoundExpression()) As BoundCall
            Return [Call](receiver, method, ImmutableArray.Create(Of BoundExpression)(args))
        End Function

        Public Function [Call](receiver As BoundExpression, method As MethodSymbol, args As ImmutableArray(Of BoundExpression)) As BoundCall
            Debug.Assert(method.ParameterCount = args.Length)
            Dim boundNode = New BoundCall(
                Syntax,
                method,
                Nothing,
                receiver,
                args,
                Nothing,
                suppressObjectClone:=True,
                type:=method.ReturnType)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [If](condition As BoundExpression, thenClause As BoundStatement, elseClause As BoundStatement) As BoundStatement
            Debug.Assert(thenClause IsNot Nothing AndAlso elseClause IsNot Nothing)
            Dim afterif = New GeneratedLabelSymbol("afterif")
            Dim alt = New GeneratedLabelSymbol("alternative")
            Dim boundCondGoto = New BoundConditionalGoto(_syntax, condition, False, alt)
            boundCondGoto.SetWasCompilerGenerated()
            Return Block(boundCondGoto, thenClause, [Goto](afterif), Label(alt), elseClause, Label(afterif))
        End Function

        Public Function TernaryConditionalExpression(condition As BoundExpression, ifTrue As BoundExpression, ifFalse As BoundExpression) As BoundTernaryConditionalExpression
            Debug.Assert(ifTrue IsNot Nothing)
            Debug.Assert(ifFalse IsNot Nothing)
            Return New BoundTernaryConditionalExpression(Me.Syntax, condition, ifTrue, ifFalse, Nothing, ifTrue.Type).MakeCompilerGenerated()
        End Function

        Public Function [TryCast](expression As BoundExpression, type As TypeSymbol) As BoundTryCast
            Debug.Assert(expression IsNot Nothing)
            Debug.Assert(Not expression.IsNothingLiteral) ' Not supported yet
            Debug.Assert(expression.Type.IsReferenceType) 'Others are not supported yet
            Debug.Assert(type.IsReferenceType) 'Others are not supported yet
            Debug.Assert(Not expression.Type.IsErrorType)
            Debug.Assert(Not type.IsErrorType)

            Return New BoundTryCast(Me.Syntax, expression, Conversions.ClassifyTryCastConversion(expression.Type, type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded), type)
        End Function

        Public Function [DirectCast](expression As BoundExpression, type As TypeSymbol) As BoundDirectCast
            Debug.Assert(expression IsNot Nothing)
            Debug.Assert(expression.IsNothingLiteral OrElse expression.Type.IsReferenceType OrElse expression.Type.IsTypeParameter()) 'Others are not supported yet
            Debug.Assert(type.IsReferenceType OrElse (type.IsTypeParameter AndAlso expression.IsNothingLiteral)) 'Others are not supported yet
            Debug.Assert(expression.Type Is Nothing OrElse Not expression.Type.IsErrorType)
            Debug.Assert(Not type.IsErrorType)

            Return New BoundDirectCast(Me.Syntax,
                                       expression,
                                       If(expression.IsNothingLiteral,
                                          ConversionKind.WideningNothingLiteral,
                                          Conversions.ClassifyDirectCastConversion(expression.Type, type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded)),
                                       type)
        End Function

        Public Function [If](condition As BoundExpression, thenClause As BoundStatement) As BoundStatement
            Return [If](condition, thenClause, Block())
        End Function

        Public Function [Throw](Optional e As BoundExpression = Nothing) As BoundThrowStatement
            Dim boundNode = New BoundThrowStatement(_syntax, e)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Local(localSym As LocalSymbol, isLValue As Boolean) As BoundLocal
            Dim boundNode = New BoundLocal(_syntax, localSym, isLValue, localSym.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Sequence(temps As ImmutableArray(Of LocalSymbol), ParamArray parts As BoundExpression()) As BoundExpression
            Debug.Assert(parts IsNot Nothing AndAlso parts.Length > 0)

            Dim statements(parts.Length - 1 - 1) As BoundExpression
            For i = 0 To parts.Length - 1 - 1
                statements(i) = parts(i)
            Next

            Dim lastExpression = parts(parts.Length - 1)
            Return Sequence(temps, statements.AsImmutableOrNull, lastExpression)
        End Function

        Public Function Sequence(temp As LocalSymbol, ParamArray parts As BoundExpression()) As BoundExpression
            Return Sequence(ImmutableArray.Create(Of LocalSymbol)(temp), parts)
        End Function

        Public Function Sequence(ParamArray parts As BoundExpression()) As BoundExpression
            Return Sequence(ImmutableArray(Of LocalSymbol).Empty, parts)
        End Function

        Public Function Sequence(locals As ImmutableArray(Of LocalSymbol), sideEffects As ImmutableArray(Of BoundExpression), result As BoundExpression) As BoundExpression
            Dim boundNode = New BoundSequence(_syntax, locals, sideEffects, result, result.Type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [Select](ex As BoundExpression, sections As IEnumerable(Of BoundCaseBlock)) As BoundStatement
            Dim sectionsArray = ImmutableArray.CreateRange(Of BoundCaseBlock)(sections)

            If sectionsArray.Length = 0 Then
                Return Me.ExpressionStatement(ex)
            End If

            Dim breakLabel As GeneratedLabelSymbol = New GeneratedLabelSymbol("break")
            CheckSwitchSections(sectionsArray)

            Dim boundNode = New BoundSelectStatement(_syntax, Me.ExpressionStatement(ex), Nothing, sectionsArray, True, breakLabel)

            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary> Check for (and assert that there are no) duplicate case labels in the switch. </summary>
        <Conditional("DEBUG")>
        Private Sub CheckSwitchSections(sections As ImmutableArray(Of BoundCaseBlock))
            Dim labels = New HashSet(Of Integer)()
            For Each s In sections
                For Each l As BoundSimpleCaseClause In s.CaseStatement.CaseClauses
                    Dim v1 = l.ValueOpt.ConstantValueOpt.Int32Value
                    Debug.Assert(Not labels.Contains(v1))
                    labels.Add(v1)
                Next
            Next
        End Sub

        'Public Function SwitchSection(value As Integer, ParamArray statements As BoundStatement()) As BoundCaseBlock
        '    Dim boundCaseClause = New BoundSimpleCaseClause(_syntax, Literal(value), Nothing)
        '    boundCaseClause.SetWasCompilerGenerated()
        '    Dim boundCaseStatement = New BoundCaseStatement(_syntax, ImmutableArray(Of BoundCaseClause).CreateFrom(boundCaseClause), Nothing)
        '    boundCaseStatement.SetWasCompilerGenerated()
        '    Dim boundCaseBlock = New BoundCaseBlock(_syntax, boundCaseStatement, Block(statements))
        '    boundCaseBlock.SetWasCompilerGenerated()
        '    Return boundCaseBlock
        'End Function

        Public Function SwitchSection(values As List(Of Integer), ParamArray statements As BoundStatement()) As BoundCaseBlock
            Dim builder = ArrayBuilder(Of BoundCaseClause).GetInstance()
            For Each i In values
                Dim boundCaseClause = New BoundSimpleCaseClause(_syntax, Literal(i), Nothing)
                boundCaseClause.SetWasCompilerGenerated()
                builder.Add(boundCaseClause)
            Next

            Dim boundCaseStatement = New BoundCaseStatement(_syntax, builder.ToImmutableAndFree(), Nothing)
            boundCaseStatement.SetWasCompilerGenerated()
            Dim boundCaseBlock = New BoundCaseBlock(_syntax, boundCaseStatement, Block(ImmutableArray.Create(Of BoundStatement)(statements)))
            boundCaseBlock.SetWasCompilerGenerated()
            Return boundCaseBlock
        End Function

        Public Function [Goto](label As LabelSymbol, Optional setWasCompilerGenerated As Boolean = True) As BoundGotoStatement
            Dim boundNode = New BoundGotoStatement(_syntax, label, Nothing)

            If setWasCompilerGenerated Then
                boundNode.SetWasCompilerGenerated()
            End If

            Return boundNode
        End Function

        Public Function Label(labelSym As LabelSymbol) As BoundLabelStatement
            Dim boundNode = New BoundLabelStatement(_syntax, labelSym)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Literal(value As String) As BoundLiteral
            Dim boundNode = New BoundLiteral(_syntax, ConstantValue.Create(value), SpecialType(Microsoft.CodeAnalysis.SpecialType.System_String))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function StringLiteral(value As ConstantValue) As BoundLiteral
            Debug.Assert(value.IsString OrElse value.IsNull)
            Dim boundNode = New BoundLiteral(_syntax, value, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_String))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

#If False Then
        Public Function ArrayLength(array As BoundExpression) As BoundArrayLength
            Debug.Assert(array.Type IsNot Nothing AndAlso array.Type.IsArrayType())
            Return New BoundArrayLength(_syntax, array, SpecialType(Roslyn.Compilers.SpecialType.System_Int32))
        End Function

        Public Function ArrayAccessFirstElement(array As BoundExpression) As BoundArrayAccess
            Debug.Assert(array.Type IsNot Nothing AndAlso array.Type.IsArrayType())
            Dim rank As Integer = (DirectCast(array.Type, ArrayTypeSymbol)).Rank
            Dim firstElementIndices As ImmutableArray(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance(rank, Literal(0)).ToReadOnlyAndFree()
            Return ArrayAccess(array, firstElementIndices)
        End Function
#End If

        Public Function ArrayAccess(array As BoundExpression, isLValue As Boolean, ParamArray indices As BoundExpression()) As BoundArrayAccess
            Return ArrayAccess(array, isLValue, indices.AsImmutableOrNull())
        End Function

        Public Function ArrayAccess(array As BoundExpression, isLValue As Boolean, indices As ImmutableArray(Of BoundExpression)) As BoundArrayAccess
            Debug.Assert(array.Type IsNot Nothing AndAlso array.Type.IsArrayType())
            Dim boundNode = New BoundArrayAccess(_syntax, array, indices, isLValue, (DirectCast(array.Type, ArrayTypeSymbol)).ElementType)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

#If False Then
        Public Function ThrowNull() As BoundStatement
            Return [Throw](Null(_compilation.GetWellKnownType(Compilers.WellKnownType.System_Exception)))
        End Function
#End If

        Public Function BaseInitialization(ParamArray args As BoundExpression()) As BoundStatement
            ' TODO: add diagnostics for when things fall apart
            Dim ctor = Me.CurrentMethod.MeParameter.Type.BaseTypeNoUseSiteDiagnostics.InstanceConstructors.Single(Function(c) c.ParameterCount = args.Length)
            Dim boundNode = New BoundExpressionStatement(_syntax, [Call](Base(), ctor, args))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Shared Function HiddenSequencePoint(Optional statementOpt As BoundStatement = Nothing) As BoundStatement
            Return New BoundSequencePoint(Nothing, statementOpt).MakeCompilerGenerated
        End Function

        Public Function Null() As BoundExpression
            Dim nullLiteral As BoundExpression = New BoundLiteral(_syntax, ConstantValue.Null, Nothing)
            nullLiteral.SetWasCompilerGenerated()
            Return nullLiteral
        End Function

        Public Function Null(type As TypeSymbol) As BoundExpression
            If Not type.IsTypeParameter() AndAlso type.IsReferenceType() Then
                Dim nullLiteral As BoundExpression = New BoundLiteral(_syntax, ConstantValue.Null, type)
                nullLiteral.SetWasCompilerGenerated()
                Return nullLiteral

            Else
                Dim nullLiteral As BoundExpression = New BoundLiteral(_syntax, ConstantValue.Null, Nothing)
                nullLiteral.SetWasCompilerGenerated()
                Return Me.Convert(type, nullLiteral)
            End If
        End Function

        Public Function Type(typeSym As TypeSymbol) As BoundTypeExpression
            Dim boundNode = New BoundTypeExpression(_syntax, typeSym)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [Typeof](type As WellKnownType) As BoundExpression
            Return [Typeof](WellKnownType(type))
        End Function

        Public Function [Typeof](typeSym As TypeSymbol) As BoundExpression
            Dim boundNode = New BoundGetType(_syntax, Type(typeSym), WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Type))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function TypeArguments(typeArgs As ImmutableArray(Of TypeSymbol)) As BoundTypeArguments
            Dim boundNode = New BoundTypeArguments(_syntax, typeArgs)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function MethodInfo(meth As WellKnownMember) As BoundExpression
            Dim method = WellKnownMember(Of MethodSymbol)(meth)
            If method Is Nothing Then
                Return BadExpression()
            Else
                Return MethodInfo(method)
            End If
        End Function

        Public Function MethodInfo(meth As SpecialMember) As BoundExpression
            Dim method = DirectCast(SpecialMember(meth), MethodSymbol)
            If method Is Nothing Then
                Return BadExpression()
            Else
                Return MethodInfo(method)
            End If
        End Function

        Public Function MethodInfo(method As MethodSymbol) As BoundExpression
            Dim boundNode = New BoundMethodInfo(Syntax, method, WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_MethodInfo))
            ' Touch the method to be used to report use site diagnostics
            WellKnownMember(Of MethodSymbol)(If(method.ContainingType.IsGenericType,
                                                Microsoft.CodeAnalysis.WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle2,
                                                Microsoft.CodeAnalysis.WellKnownMember.System_Reflection_MethodBase__GetMethodFromHandle))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function ConstructorInfo(meth As WellKnownMember) As BoundExpression
            Dim method = WellKnownMember(Of MethodSymbol)(meth)
            If method Is Nothing Then
                Return BadExpression()
            Else
                Return ConstructorInfo(method)
            End If
        End Function

        Public Function ConstructorInfo(meth As SpecialMember) As BoundExpression
            Dim method = DirectCast(SpecialMember(meth), MethodSymbol)
            If method Is Nothing Then
                Return BadExpression()
            Else
                Return ConstructorInfo(method)
            End If
        End Function

        Public Function ConstructorInfo(meth As MethodSymbol) As BoundExpression
            Dim boundNode = New BoundMethodInfo(Syntax, meth, WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_ConstructorInfo))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

#If False Then
        Friend Function ConstructorInfo(ctor As MethodSymbol) As BoundExpression
            Dim boundNode = New BoundMethodInfo(Syntax, ctor, WellKnownType(Compilers.WellKnownType.System_Reflection_ConstructorInfo))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function
#End If

        Public Function FieldInfo(field As FieldSymbol) As BoundExpression
            Dim boundNode = New BoundFieldInfo(_syntax, field, WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Reflection_FieldInfo))
            ' Touch the method to be used to report use site diagnostics
            WellKnownMember(Of MethodSymbol)(If(field.ContainingType.IsGenericType,
                                                Microsoft.CodeAnalysis.WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle2,
                                                Microsoft.CodeAnalysis.WellKnownMember.System_Reflection_FieldInfo__GetFieldFromHandle))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to the index portion of a method's metadata token.
        ''' </summary>
        Public Function MethodDefIndex(method As MethodSymbol) As BoundExpression
            Dim boundNode As New BoundMethodDefIndex(Syntax, method, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to the maximum value of the index portions of all method definition metadata tokens in current module.
        ''' </summary>
        Public Function MaximumMethodDefIndex() As BoundExpression
            Dim boundNode As New BoundMaximumMethodDefIndex(Syntax, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to the current module's MVID.
        ''' </summary>
        Public Function ModuleVersionId(isLValue As Boolean) As BoundExpression
            Dim boundNode As New BoundModuleVersionId(Syntax, isLValue, WellKnownType(Microsoft.CodeAnalysis.WellKnownType.System_Guid))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to a text representation of the current module/s MVID.
        ''' </summary>
        Public Function ModuleVersionIdString() As BoundExpression
            Dim boundNode As New BoundModuleVersionIdString(Syntax, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_String))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to the root of the dynamic analysis payloads for a particular kind of dynamic analysis.
        ''' </summary>
        ''' <param name="analysisKind">Uniquely identifies the kind of dynamic analysis.</param>
        ''' <param name="payloadType">Type of an analysis payload cell for the particular analysis kind.</param>
        Public Function InstrumentationPayloadRoot(analysisKind As Integer, payloadType As TypeSymbol, isLValue As Boolean) As BoundExpression
            Dim boundNode As New BoundInstrumentationPayloadRoot(Syntax, analysisKind, isLValue, payloadType)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        ''' <summary>
        ''' Synthesizes an expression that evaluates to the index of a source document in the table of debug source documents.
        ''' </summary>
        Public Function SourceDocumentIndex(document As Cci.DebugSourceDocument) As BoundExpression
            Dim boundNode As New BoundSourceDocumentIndex(Syntax, document, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Int32))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Convert(type As TypeSymbol, arg As BoundExpression, Optional isChecked As Boolean = False) As BoundConversion
            If arg.IsNothingLiteral() Then
                Return Convert(type, arg, ConversionKind.WideningNothingLiteral, isChecked)
            ElseIf type.IsErrorType() OrElse arg.Type.IsErrorType() Then
                Return Convert(type, arg, ConversionKind.WideningReference, isChecked) ' will abort before code gen due to error, so doesn't matter if conversion kind is wrong.
            Else
                Return Convert(type, arg, Conversions.ClassifyConversion(arg.Type, type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded).Key, isChecked)
            End If
        End Function

        Public Function Convert(type As TypeSymbol, arg As BoundExpression, convKind As ConversionKind, Optional isChecked As Boolean = False) As BoundConversion
            Debug.Assert((convKind And ConversionKind.UserDefined) = 0)
            Dim boundNode = New BoundConversion(_syntax, arg, convKind, isChecked, True, ConstantValue.NotAvailable, type)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function Array(elementType As TypeSymbol, ParamArray elements As BoundExpression()) As BoundExpression
            Return Array(elementType, elements.AsImmutableOrNull())
        End Function

        Public Function Array(elementType As TypeSymbol, elements As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim arrayType = Me.Compilation.CreateArrayTypeSymbol(elementType)
            Dim boundArrayInit = New BoundArrayInitialization(_syntax, elements, arrayType)
            boundArrayInit.SetWasCompilerGenerated()
            Return New BoundArrayCreation(_syntax, ImmutableArray.Create(Of BoundExpression)(Literal(elements.Length)), boundArrayInit, arrayType)
        End Function

        Public Function Array(elementType As TypeSymbol, bounds As ImmutableArray(Of BoundExpression), elements As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim arrayType = Me.Compilation.CreateArrayTypeSymbol(elementType)
            Dim arrayInitialization As BoundArrayInitialization = If(Not elements.IsDefaultOrEmpty, New BoundArrayInitialization(_syntax, elements, arrayType), Nothing)
            arrayInitialization?.SetWasCompilerGenerated()
            Dim arrayCreation As New BoundArrayCreation(_syntax, bounds, arrayInitialization, arrayType)
            arrayCreation.SetWasCompilerGenerated()
            Return arrayCreation
        End Function

        Public Function Conditional(condition As BoundExpression, consequence As BoundExpression, alternative As BoundExpression, type As TypeSymbol) As BoundTernaryConditionalExpression
            Return New BoundTernaryConditionalExpression(Syntax, condition, consequence, alternative, Nothing, type)
        End Function

        Public Function BinaryConditional(left As BoundExpression, right As BoundExpression) As BoundBinaryConditionalExpression
            Return New BoundBinaryConditionalExpression(Syntax, left, Nothing, Nothing, right, Nothing, left.Type)
        End Function

        Public Function Binary(kind As BinaryOperatorKind, type As TypeSymbol, left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Dim binOp = New BoundBinaryOperator(Syntax, kind, left, right, False, type)
            binOp.SetWasCompilerGenerated()
            Return binOp
        End Function

        Public Function ObjectReferenceEqual(left As BoundExpression, right As BoundExpression) As BoundBinaryOperator
            Dim boundNode = Binary(BinaryOperatorKind.Is, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), left, right)
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function ReferenceIsNothing(operand As BoundExpression) As BoundBinaryOperator
            Debug.Assert(operand.Type.IsReferenceType)
            Dim boundNode = Binary(BinaryOperatorKind.Is, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), operand, Me.Null(operand.Type))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function ReferenceIsNotNothing(operand As BoundExpression) As BoundBinaryOperator
            Debug.Assert(operand.Type.IsReferenceType)
            Dim boundNode = Binary(BinaryOperatorKind.IsNot, SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Boolean), operand, Me.Null(operand.Type))
            boundNode.SetWasCompilerGenerated()
            Return boundNode
        End Function

        Public Function [Not](expression As BoundExpression) As BoundExpression
            Return New BoundUnaryOperator(expression.Syntax, UnaryOperatorKind.Not, expression, False, expression.Type)
        End Function

        Public Function [Try](tryBlock As BoundBlock,
                              catchBlocks As ImmutableArray(Of BoundCatchBlock),
                              Optional finallyBlock As BoundBlock = Nothing,
                              Optional exitLabel As LabelSymbol = Nothing) As BoundStatement

            Return New BoundTryStatement(Syntax, tryBlock, catchBlocks, finallyBlock, exitLabel)
        End Function

        Public Function CatchBlocks(ParamArray blocks() As BoundCatchBlock) As ImmutableArray(Of BoundCatchBlock)
            Return blocks.AsImmutableOrNull()
        End Function

        Public Function [Catch](local As LocalSymbol, block As BoundBlock, Optional isSynthesizedAsyncCatchAll As Boolean = False) As BoundCatchBlock
            Dim m1 = WellKnownMember(Of MethodSymbol)(Microsoft.CodeAnalysis.WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__SetProjectError)
            Dim m2 = WellKnownMember(Of MethodSymbol)(Microsoft.CodeAnalysis.WellKnownMember.Microsoft_VisualBasic_CompilerServices_ProjectData__ClearProjectError)
            Return New BoundCatchBlock(Syntax, local, Me.Local(local, False), Nothing, Nothing, block,
                                       hasErrors:=m1 Is Nothing OrElse m2 Is Nothing,
                                       isSynthesizedAsyncCatchAll:=isSynthesizedAsyncCatchAll)
        End Function

        Public Function SequencePoint(syntax As SyntaxNode, statement As BoundStatement) As BoundStatement
            Return New BoundSequencePoint(syntax, statement)
        End Function

        Public Function SequencePoint(syntax As SyntaxNode) As BoundStatement
            Return New BoundSequencePoint(syntax, Nothing).MakeCompilerGenerated
        End Function

        Public Function SequencePointWithSpan(syntax As SyntaxNode, textSpan As TextSpan, boundStatement As BoundStatement) As BoundStatement
            Return New BoundSequencePointWithSpan(syntax, boundStatement, textSpan)
        End Function

        Public Function NoOp(Optional flavor As NoOpStatementFlavor = NoOpStatementFlavor.Default) As BoundStatement
            Return New BoundNoOpStatement(Me.Syntax, flavor).MakeCompilerGenerated
        End Function

        Public Sub CloseMethod(body As BoundStatement)
            Debug.Assert(Me.CurrentMethod IsNot Nothing)
            If body.Kind <> BoundKind.Block Then
                body = Me.Block(body)
            End If
            CompilationState.AddSynthesizedMethod(Me.CurrentMethod, body, stateMachineType:=Nothing, ImmutableArray(Of StateMachineStateDebugInfo).Empty)
            Me.CurrentMethod = Nothing
        End Sub

        Public Function SpillSequence(locals As ImmutableArray(Of LocalSymbol), fields As ImmutableArray(Of FieldSymbol), statements As ImmutableArray(Of BoundStatement), valueOpt As BoundExpression) As BoundSpillSequence
            Return New BoundSpillSequence(Me.Syntax, locals, fields, statements, valueOpt,
                                          If(valueOpt Is Nothing,
                                             Me.SpecialType(Microsoft.CodeAnalysis.SpecialType.System_Void),
                                             valueOpt.Type)).MakeCompilerGenerated()
        End Function

    End Class
End Namespace
