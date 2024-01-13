' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Linq

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Represents an interactive code entry point that is inserted into the compilation if there is not an existing one.
    ''' </summary>
    Friend MustInherit Class SynthesizedEntryPointSymbol
        Inherits SynthesizedMethodBase

        Friend Const MainName = "<Main>"
        Friend Const FactoryName = "<Factory>"

        Private ReadOnly _containingType As NamedTypeSymbol
        Private ReadOnly _returnType As TypeSymbol

        Friend Shared Function Create(initializerMethod As SynthesizedInteractiveInitializerMethod, diagnostics As BindingDiagnosticBag) As SynthesizedEntryPointSymbol
            Dim containingType = initializerMethod.ContainingType
            Dim compilation = ContainingType.DeclaringCompilation
            If compilation.IsSubmission Then
                Dim submissionArrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Object))
                ReportUseSiteInfo(submissionArrayType, diagnostics)
                Return New SubmissionEntryPoint(containingType, initializerMethod.ReturnType, submissionArrayType)
            Else
                Dim taskType = compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)

                Debug.Assert(taskType.IsErrorType() OrElse initializerMethod.ReturnType.IsOrDerivedFrom(taskType, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))

                ReportUseSiteInfo(taskType, diagnostics)
                Dim getAwaiterMethod = If(taskType.IsErrorType(),
                    Nothing,
                    GetRequiredMethod(taskType, WellKnownMemberNames.GetAwaiter, diagnostics))
                Dim getResultMethod = If(getAwaiterMethod Is Nothing,
                    Nothing,
                    GetRequiredMethod(getAwaiterMethod.ReturnType, WellKnownMemberNames.GetResult, diagnostics))
                Return New ScriptEntryPoint(
                    containingType,
                    compilation.GetSpecialType(SpecialType.System_Void),
                    getAwaiterMethod,
                    getResultMethod)
            End If
        End Function

        Private Sub New(containingType As NamedTypeSymbol, returnType As TypeSymbol)
            MyBase.New(containingType)

            Debug.Assert(containingType IsNot Nothing)
            Debug.Assert(returnType IsNot Nothing)

            _containingType = containingType
            _returnType = returnType
        End Sub

        Friend MustOverride Function CreateBody() As BoundBlock

        Public MustOverride Overrides ReadOnly Property Name As String

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Return _returnType.SpecialType = SpecialType.System_Void
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return True
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                ' the method is Shared
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return Accessibility.Private
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            Return LexicalSortKey.NotInSource
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return ImmutableArray(Of Location).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Return ImmutableArray(Of TypeSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return MethodKind.Ordinary
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

        Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
            Return False
        End Function

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Throw ExceptionUtilities.Unreachable
        End Function

        Private Function GetSyntax() As VisualBasicSyntaxNode
            Return VisualBasicSyntaxTree.Dummy.GetRoot()
        End Function

        Private Shared Sub ReportUseSiteInfo(symbol As Symbol, diagnostics As BindingDiagnosticBag)
            diagnostics.Add(symbol.GetUseSiteInfo(), NoLocation.Singleton)
        End Sub

        Private Shared Function GetRequiredMethod(type As TypeSymbol, methodName As String, diagnostics As BindingDiagnosticBag) As MethodSymbol
            Dim method = TryCast(type.GetMembers(methodName).SingleOrDefault(), MethodSymbol)
            If method Is Nothing Then
                diagnostics.Add(
                    ErrorFactory.ErrorInfo(ERRID.ERR_MissingRuntimeHelper, type.MetadataName & "." & methodName),
                    NoLocation.Singleton)
            End If
            Return method
        End Function

        Private Shared Function CreateParameterlessCall(syntax As VisualBasicSyntaxNode, receiver As BoundExpression, method As MethodSymbol) As BoundCall
            Return New BoundCall(
                syntax,
                method,
                methodGroupOpt:=Nothing,
                receiverOpt:=receiver.MakeRValue(),
                arguments:=ImmutableArray(Of BoundExpression).Empty,
                constantValueOpt:=Nothing,
                suppressObjectClone:=False,
                type:=method.ReturnType).MakeCompilerGenerated()
        End Function

        Private NotInheritable Class ScriptEntryPoint
            Inherits SynthesizedEntryPointSymbol

            Private ReadOnly _getAwaiterMethod As MethodSymbol
            Private ReadOnly _getResultMethod As MethodSymbol

            Friend Sub New(containingType As NamedTypeSymbol, returnType As TypeSymbol, getAwaiterMethod As MethodSymbol, getResultMethod As MethodSymbol)
                MyBase.New(containingType, returnType)

                Debug.Assert(containingType.IsScriptClass)
                Debug.Assert(returnType.SpecialType = SpecialType.System_Void)

                _getAwaiterMethod = getAwaiterMethod
                _getResultMethod = getResultMethod
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return MainName
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return ImmutableArray(Of ParameterSymbol).Empty
                End Get
            End Property

            ' Private Shared Sub <Main>()
            '     Dim script As New Script()
            '     script.<Initialize>().GetAwaiter().GetResult()
            ' End Sub
            Friend Overrides Function CreateBody() As BoundBlock
                ' CreateBody should only be called if no errors.
                Debug.Assert(_getAwaiterMethod IsNot Nothing)
                Debug.Assert(_getResultMethod IsNot Nothing)

                Dim syntax = GetSyntax()

                Dim ctor = _containingType.GetScriptConstructor()
                Debug.Assert(ctor.ParameterCount = 0)

                Dim initializer = _containingType.GetScriptInitializer()
                Debug.Assert(initializer.ParameterCount = 0)

                Dim scriptLocal = New BoundLocal(
                    syntax,
                    New SynthesizedLocal(Me, _containingType, SynthesizedLocalKind.LoweringTemp),
                    _containingType).MakeCompilerGenerated()

                ' Dim script As New Script()
                Dim scriptAssignment = New BoundExpressionStatement(
                    syntax,
                    New BoundAssignmentOperator(
                        syntax,
                        scriptLocal,
                        New BoundObjectCreationExpression(
                            syntax,
                            ctor,
                            ImmutableArray(Of BoundExpression).Empty,
                            initializerOpt:=Nothing,
                            type:=_containingType).MakeCompilerGenerated(),
                        suppressObjectClone:=False).MakeCompilerGenerated()).MakeCompilerGenerated()

                ' script.<Initialize>().GetAwaiter().GetResult()
                Dim scriptInitialize = New BoundExpressionStatement(
                    syntax,
                    CreateParameterlessCall(
                        syntax,
                        CreateParameterlessCall(
                            syntax,
                            CreateParameterlessCall(
                                syntax,
                                scriptLocal,
                                initializer),
                            _getAwaiterMethod),
                        _getResultMethod)).MakeCompilerGenerated()

                ' Return
                Dim returnStatement = New BoundReturnStatement(
                    syntax,
                    Nothing,
                    Nothing,
                    Nothing).MakeCompilerGenerated()

                Return New BoundBlock(
                    syntax,
                    Nothing,
                    ImmutableArray.Create(Of LocalSymbol)(scriptLocal.LocalSymbol),
                    ImmutableArray.Create(Of BoundStatement)(scriptAssignment, scriptInitialize, returnStatement)).MakeCompilerGenerated()
            End Function
        End Class

        Private NotInheritable Class SubmissionEntryPoint
            Inherits SynthesizedEntryPointSymbol

            Private ReadOnly _parameters As ImmutableArray(Of ParameterSymbol)

            Friend Sub New(containingType As NamedTypeSymbol, returnType As TypeSymbol, submissionArrayType As TypeSymbol)
                MyBase.New(containingType, returnType)

                Debug.Assert(containingType.IsSubmissionClass)
                _parameters = ImmutableArray.Create(Of ParameterSymbol)(New SynthesizedParameterSymbol(Me, submissionArrayType, ordinal:=0, isByRef:=False, name:="submissionArray"))
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return FactoryName
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    Return _parameters
                End Get
            End Property

            ' Private Shared Function <Factory>(submissionArray As Object()) As T
            '     Dim submission As New Submission#N(submissionArray)
            '     Return submission.<Initialize>()
            ' End Function
            Friend Overrides Function CreateBody() As BoundBlock
                Dim syntax = GetSyntax()

                Dim ctor = _containingType.GetScriptConstructor()
                Debug.Assert(ctor.ParameterCount = 1)

                Dim initializer = _containingType.GetScriptInitializer()
                Debug.Assert(initializer.ParameterCount = 0)

                Dim parameter = _parameters(0)
                Dim submissionArrayParameter = New BoundParameter(
                    syntax,
                    parameter,
                    isLValue:=False,
                    type:=parameter.Type).MakeCompilerGenerated()
                Dim submissionLocal = New BoundLocal(
                    syntax,
                    New SynthesizedLocal(Me, _containingType, SynthesizedLocalKind.LoweringTemp),
                    _containingType).MakeCompilerGenerated()

                ' Dim submission As New Submission#N(submissionArray)
                Dim submissionAssignment = New BoundExpressionStatement(
                    syntax,
                    New BoundAssignmentOperator(
                        syntax,
                        submissionLocal,
                        New BoundObjectCreationExpression(
                            syntax,
                            ctor,
                            ImmutableArray.Create(Of BoundExpression)(submissionArrayParameter),
                            initializerOpt:=Nothing,
                            type:=_containingType).MakeCompilerGenerated(),
                        suppressObjectClone:=False).MakeCompilerGenerated()).MakeCompilerGenerated()

                ' Return submission.<Initialize>()
                Dim returnStatement = New BoundReturnStatement(
                    syntax,
                    CreateParameterlessCall(
                        syntax,
                        submissionLocal,
                        initializer).MakeRValue(),
                    functionLocalOpt:=Nothing,
                    exitLabelOpt:=Nothing).MakeCompilerGenerated()

                Return New BoundBlock(
                    syntax,
                    Nothing,
                    ImmutableArray.Create(Of LocalSymbol)(submissionLocal.LocalSymbol),
                    ImmutableArray.Create(Of BoundStatement)(submissionAssignment, returnStatement)).MakeCompilerGenerated()
            End Function
        End Class

    End Class
End Namespace

