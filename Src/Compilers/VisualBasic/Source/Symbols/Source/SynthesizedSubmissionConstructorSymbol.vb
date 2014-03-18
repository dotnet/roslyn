' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represents a compiler generated parameterless constructor 
    ''' </summary>
    Friend NotInheritable Class SynthesizedSubmissionConstructorSymbol
        Inherits SynthesizedConstructorBase

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedConstructorSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing type for the synthesized constructor.</param>
        ''' <param name="isShared">if set to <c>true</c> if this is a shared constructor.</param>
        Friend Sub New(
            syntaxNode As VisualBasicSyntaxNode,
            container As NamedTypeSymbol,
            isShared As Boolean,
            binder As Binder,
            diagnostics As DiagnosticBag
        )
            MyBase.New(syntaxNode, container, isShared, binder, diagnostics)
            Debug.Assert(container.TypeKind = TypeKind.Submission)

            ' In interactive code the constructor of the Script class takes the InteractiveSession object 
            ' from which it can retrieve references to previous submissions.
            Dim compilation = container.DeclaringCompilation

            Dim interactiveSessionType = binder.GetWellKnownType(WellKnownType.Microsoft_CSharp_RuntimeHelpers_Session, syntaxNode, diagnostics)

            ' resolve return type:
            ' TODO(tomat): compilation.GetTypeByReflectionType(compilation.SubmissionReturnType, diagnostics)
            Dim returnType As TypeSymbol = compilation.GetSpecialType(SpecialType.System_Object)

            _parameters = ImmutableArray.Create(Of ParameterSymbol)(
                New SynthesizedParameterSymbol(Me, interactiveSessionType, 0, isByRef:=False, Name:="session"),
                New SynthesizedParameterSymbol(Me, returnType, 1, isByRef:=True, Name:="submissionResult"))
        End Sub

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Return New BoundBlock(
                Me.m_SyntaxNode,
                Nothing,
                Nothing,
                ImmutableArray.Create(Of BoundStatement)(New BoundReturnStatement(Me.m_SyntaxNode, Nothing, Nothing, Nothing)))
        End Function

        Friend Shared Function MakeSubmissionInitialization(
            syntax As VisualBasicSyntaxNode,
            constructor As MethodSymbol,
            synthesizedFields As SynthesizedSubmissionFields,
            compilation As VisualBasicCompilation,
            diagnostics As DiagnosticBag) As ImmutableArray(Of BoundStatement)

            Debug.Assert(constructor.ParameterCount = 2)
            Dim result = New BoundStatement(1 + synthesizedFields.Count - 1) {}

            Dim sessionReference = New BoundParameter(syntax, constructor.Parameters(0), isLValue:=False, type:=constructor.Parameters(0).Type)

            Dim submissionGetter = DirectCast(compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_CSharp_RuntimeHelpers_SessionHelpers__GetSubmission), MethodSymbol)
            Dim submissionAdder = DirectCast(compilation.GetWellKnownTypeMember(WellKnownMember.Microsoft_CSharp_RuntimeHelpers_SessionHelpers__SetSubmission), MethodSymbol)

            ' TODO: report erroneous adder/getter
            Debug.Assert(submissionAdder IsNot Nothing AndAlso submissionGetter IsNot Nothing)

            ' TODO: report erroneous Int32
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim meReference = New BoundMeReference(syntax, constructor.ContainingType)

            Dim i As Integer = 0

            ' hostObject = DirectCast(SessionHelpers.SetSubmission(<session>, <slot index>, this), THostObject)
            Dim slotIndex = compilation.GetSubmissionSlotIndex()
            Debug.Assert(slotIndex >= 0)

            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
            Dim setSubmission As BoundExpression = New BoundCall(syntax,
                method:=submissionAdder,
                methodGroup:=Nothing,
                receiver:=Nothing,
                arguments:=ImmutableArray.Create(Of BoundExpression)(
                    sessionReference,
                    New BoundLiteral(syntax, ConstantValue.Create(slotIndex), intType),
                    New BoundDirectCast(meReference.Syntax, meReference,
                                        Conversions.ClassifyDirectCastConversion(meReference.Type, submissionAdder.Parameters(2).Type, useSiteDiagnostics),
                                        submissionAdder.Parameters(2).Type)),
                constantValueOpt:=Nothing,
                Type:=submissionAdder.ReturnType).MakeCompilerGenerated()

            diagnostics.Add(syntax, useSiteDiagnostics)

            Dim hostObjectField = synthesizedFields.GetHostObjectField()
            If hostObjectField IsNot Nothing Then
                setSubmission = New BoundAssignmentOperator(
                    syntax,
                    New BoundFieldAccess(syntax, meReference, hostObjectField, isLValue:=True, Type:=hostObjectField.Type),
                    New BoundDirectCast(syntax, setSubmission, ConversionKind.Reference, Type:=hostObjectField.Type),
                    suppressObjectClone:=True,
                    Type:=hostObjectField.Type).MakeCompilerGenerated()
            End If

            result(i) = New BoundExpressionStatement(syntax, setSubmission)
            i = i + 1

            For Each field In synthesizedFields.FieldSymbols
                Dim targetScriptClass = DirectCast(field.Type, ImplicitNamedTypeSymbol)
                Dim targetSubmissionId = targetScriptClass.DeclaringCompilation.GetSubmissionSlotIndex()
                Debug.Assert(targetSubmissionId >= 0)

                ' constructor.<field> = DirectCast(SessionHelpers.GetSubmission(<session>, <i>), <FieldType>);
                result(i) = New BoundExpressionStatement(syntax,
                    New BoundAssignmentOperator(syntax,
                        New BoundFieldAccess(syntax,
                            receiverOpt:=meReference,
                            FieldSymbol:=field,
                            isLValue:=True,
                            Type:=targetScriptClass),
                        New BoundDirectCast(syntax,
                            New BoundCall(syntax,
                                    method:=submissionGetter,
                                    methodGroup:=Nothing,
                                    receiver:=Nothing,
                                    arguments:=ImmutableArray.Create(Of BoundExpression)(
                                        sessionReference,
                                        New BoundLiteral(syntax, ConstantValue.Create(targetSubmissionId), intType)),
                                    constantValueOpt:=Nothing,
                                    Type:=submissionGetter.ReturnType),
                            ConversionKind.Reference,
                            Type:=targetScriptClass),
                        suppressObjectClone:=True,
                        Type:=targetScriptClass)).MakeCompilerGenerated()

                i = i + 1
            Next

            Debug.Assert(i = result.Length)
            Return result.AsImmutableOrNull()
        End Function


    End Class
End Namespace
