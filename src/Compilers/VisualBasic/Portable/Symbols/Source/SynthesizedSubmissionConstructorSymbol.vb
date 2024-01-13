' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend NotInheritable Class SynthesizedSubmissionConstructorSymbol
        Inherits SynthesizedConstructorBase

        Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SynthesizedConstructorSymbol" /> class.
        ''' </summary>
        ''' <param name="container">The containing type for the synthesized constructor.</param>
        ''' <param name="isShared">if set to <c>true</c> if this is a shared constructor.</param>
        Friend Sub New(
            syntaxReference As SyntaxReference,
            container As NamedTypeSymbol,
            isShared As Boolean,
            binder As Binder,
            diagnostics As BindingDiagnosticBag
        )
            MyBase.New(syntaxReference, container, isShared, binder, diagnostics)
            Debug.Assert(container.TypeKind = TypeKind.Submission)

            ' In interactive code the constructor of the Script class takes the InteractiveSession object 
            ' from which it can retrieve references to previous submissions.
            Dim compilation = container.DeclaringCompilation

            Dim submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object))
            diagnostics.Add(submissionArrayType.GetUseSiteInfo(), NoLocation.Singleton)

            _parameters = ImmutableArray.Create(Of ParameterSymbol)(
                New SynthesizedParameterSymbol(Me, submissionArrayType, 0, isByRef:=False, name:="submissionArray"))
        End Sub

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As BindingDiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock
            Dim node As SyntaxNode = Me.Syntax
            Return New BoundBlock(
                node,
                Nothing,
                ImmutableArray(Of LocalSymbol).Empty,
                ImmutableArray.Create(Of BoundStatement)(New BoundReturnStatement(node, Nothing, Nothing, Nothing)))
        End Function

        Friend Shared Function MakeSubmissionInitialization(
            syntax As SyntaxNode,
            constructor As MethodSymbol,
            synthesizedFields As SynthesizedSubmissionFields,
            compilation As VisualBasicCompilation) As ImmutableArray(Of BoundStatement)

            Debug.Assert(constructor.ParameterCount = 1)
            Dim result = New List(Of BoundStatement)()

            Dim submissionArrayReference = New BoundParameter(syntax, constructor.Parameters(0), isLValue:=False, type:=constructor.Parameters(0).Type)
            Dim submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object))

            ' TODO: report erroneous Int32
            Dim intType = compilation.GetSpecialType(SpecialType.System_Int32)
            Dim objectType = compilation.GetSpecialType(SpecialType.System_Object)
            Dim meReference = New BoundMeReference(syntax, constructor.ContainingType)

            ' <submission_array>(<slot_index>) = Me
            Dim slotIndex = compilation.GetSubmissionSlotIndex()
            Debug.Assert(slotIndex >= 0)

            result.Add(
                New BoundExpressionStatement(syntax,
                    New BoundAssignmentOperator(syntax,
                        New BoundArrayAccess(syntax, submissionArrayReference, ImmutableArray.Create(Of BoundExpression)(New BoundLiteral(syntax, ConstantValue.Create(slotIndex), intType)), isLValue:=True, type:=objectType),
                        New BoundDirectCast(syntax, meReference, ConversionKind.Reference, type:=objectType),
                        suppressObjectClone:=True,
                        type:=objectType)).MakeCompilerGenerated())

            ' hostObject = DirectCast(<submission_array>(0), <THostObject>)
            Dim hostObjectField = synthesizedFields.GetHostObjectField()
            If hostObjectField IsNot Nothing Then
                result.Add(
                    New BoundExpressionStatement(syntax,
                        New BoundAssignmentOperator(
                            syntax,
                            New BoundFieldAccess(syntax, meReference, hostObjectField, isLValue:=True, type:=hostObjectField.Type),
                            New BoundDirectCast(syntax,
                                New BoundArrayAccess(syntax, submissionArrayReference, ImmutableArray.Create(Of BoundExpression)(New BoundLiteral(syntax, ConstantValue.Create(0), intType)), isLValue:=False, type:=objectType),
                                ConversionKind.Reference, type:=hostObjectField.Type),
                            suppressObjectClone:=True,
                            type:=hostObjectField.Type).MakeCompilerGenerated()))
            End If

            For Each field In synthesizedFields.FieldSymbols
                Dim targetScriptType = DirectCast(field.Type, ImplicitNamedTypeSymbol)
                Dim targetSubmissionId = targetScriptType.DeclaringCompilation.GetSubmissionSlotIndex()
                Debug.Assert(targetSubmissionId >= 0)

                ' Me.<field> = DirectCast(<submission_array>(<i>), <FieldType>);
                result.Add(New BoundExpressionStatement(syntax,
                    New BoundAssignmentOperator(syntax,
                        New BoundFieldAccess(syntax,
                            receiverOpt:=meReference,
                            fieldSymbol:=field,
                            isLValue:=True,
                            type:=targetScriptType),
                        New BoundDirectCast(syntax,
                            New BoundArrayAccess(syntax, submissionArrayReference, ImmutableArray.Create(Of BoundExpression)(New BoundLiteral(syntax, ConstantValue.Create(targetSubmissionId), intType)), isLValue:=False, type:=objectType),
                            ConversionKind.Reference,
                            type:=targetScriptType),
                        suppressObjectClone:=True,
                        type:=targetScriptType)).MakeCompilerGenerated())
            Next

            Return result.AsImmutableOrNull()
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Dim containingType = DirectCast(Me.ContainingType, SourceMemberContainerTypeSymbol)
            Return containingType.CalculateSyntaxOffsetInSynthesizedConstructor(localPosition, localTree, IsShared)
        End Function
    End Class
End Namespace
