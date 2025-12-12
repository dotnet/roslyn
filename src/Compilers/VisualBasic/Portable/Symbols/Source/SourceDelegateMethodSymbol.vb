' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' This class represent a compiler generated method of a delegate type that is based upon source delegate or event delegate declaration.
    ''' </summary>
    Friend Class SourceDelegateMethodSymbol
        Inherits SourceMethodSymbol

        ' Parameters.
        Private _parameters As ImmutableArray(Of ParameterSymbol)

        ' Return type. Void for a Sub.
        Private ReadOnly _returnType As TypeSymbol

        Protected Sub New(delegateType As NamedTypeSymbol,
                          syntax As VisualBasicSyntaxNode,
                          binder As Binder,
                          flags As SourceMemberFlags,
                          returnType As TypeSymbol)
            MyBase.New(delegateType, flags, binder.GetSyntaxReference(syntax), delegateType.Locations)

            Debug.Assert(TypeOf syntax Is DelegateStatementSyntax OrElse
                         TypeOf syntax Is EventStatementSyntax)
            Debug.Assert(returnType IsNot Nothing)
            _returnType = returnType
        End Sub

        Protected Sub InitializeParameters(parameters As ImmutableArray(Of ParameterSymbol))
            Debug.Assert(_parameters.IsDefault)
            Debug.Assert(Not parameters.IsDefault)
            _parameters = parameters
        End Sub

        Public Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                Return _returnType
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return _parameters
            End Get
        End Property

        Friend Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
            Get
                Return OverriddenMembersResult(Of MethodSymbol).Empty
            End Get
        End Property

        Friend Shared Sub MakeDelegateMembers(delegateType As NamedTypeSymbol,
                                              syntax As VisualBasicSyntaxNode,
                                              parameterListOpt As ParameterListSyntax,
                                              binder As Binder,
                                              <Out> ByRef constructor As MethodSymbol,
                                              <Out> ByRef beginInvoke As MethodSymbol,
                                              <Out> ByRef endInvoke As MethodSymbol,
                                              <Out> ByRef invoke As MethodSymbol,
                                              diagnostics As BindingDiagnosticBag)

            Debug.Assert(TypeOf syntax Is DelegateStatementSyntax OrElse
                         TypeOf syntax Is EventStatementSyntax)

            Dim returnType As TypeSymbol = BindReturnType(syntax, binder, diagnostics)

            ' reuse types to avoid reporting duplicate errors if missing:
            Dim voidType = binder.GetSpecialType(SpecialType.System_Void, syntax, diagnostics)
            Dim iAsyncResultType = binder.GetSpecialType(SpecialType.System_IAsyncResult, syntax, diagnostics)
            Dim objectType = binder.GetSpecialType(SpecialType.System_Object, syntax, diagnostics)
            Dim intPtrType = binder.GetSpecialType(SpecialType.System_IntPtr, syntax, diagnostics)
            Dim asyncCallbackType = binder.GetSpecialType(SpecialType.System_AsyncCallback, syntax, diagnostics)

            ' A delegate has the following members: (see CLI spec 13.6)
            ' (1) a method named Invoke with the specified signature
            Dim invokeMethod = New InvokeMethod(delegateType, returnType, syntax, binder, parameterListOpt, diagnostics)
            invoke = invokeMethod

            ' (2) a constructor with argument types (object, System.IntPtr)
            constructor = New Constructor(delegateType, voidType, objectType, intPtrType, syntax, binder)

            ' If this is a winmd compilation we don't want to add the begin/endInvoke members to the symbol
            If delegateType.IsCompilationOutputWinMdObj() Then
                beginInvoke = Nothing
                endInvoke = Nothing
            Else
                ' (3) BeginInvoke
                beginInvoke = New BeginInvokeMethod(invokeMethod, iAsyncResultType, objectType, asyncCallbackType, syntax, binder)

                ' and (4) EndInvoke methods
                endInvoke = New EndInvokeMethod(invokeMethod, iAsyncResultType, syntax, binder)
            End If
        End Sub

        Private Shared Function BindReturnType(syntax As VisualBasicSyntaxNode, binder As Binder, diagnostics As BindingDiagnosticBag) As TypeSymbol
            If syntax.Kind = SyntaxKind.DelegateFunctionStatement Then
                Dim delegateSyntax = DirectCast(syntax, DelegateStatementSyntax)

                Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

                If binder.OptionStrict = OptionStrict.On Then
                    getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowsImplicitProc
                ElseIf binder.OptionStrict = OptionStrict.Custom Then
                    getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinFunction
                End If

                Dim asClause = DirectCast(delegateSyntax.AsClause, SimpleAsClauseSyntax)
                Return binder.DecodeIdentifierType(delegateSyntax.Identifier, asClause, getErrorInfo, diagnostics)
            Else
                Return binder.GetSpecialType(SpecialType.System_Void, syntax, diagnostics)
            End If
        End Function

        ''' <summary>
        ''' Returns true if this method is an extension method.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        Friend Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean
            Get
                Return False
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether this instance is external method.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if this instance is external method; otherwise, <c>false</c>.
        ''' </value>
        Public NotOverridable Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                Return True
            End Get
        End Property

        Public NotOverridable Overrides Function GetDllImportData() As DllImportData
            Return Nothing
        End Function

        Friend NotOverridable Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                Return Reflection.MethodImplAttributes.Runtime
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        ''' <summary>
        ''' Get the type parameters on this method. If the method has not generic,
        ''' returns an empty list.
        ''' </summary>
        Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
            Get
                Return ImmutableArray(Of TypeParameterSymbol).Empty
            End Get
        End Property

        ''' <summary>
        ''' Gets a value indicating whether the symbol was generated by the compiler
        ''' rather than declared explicitly.
        ''' </summary>
        Public NotOverridable Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return False
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return Me.MethodKind = MethodKind.Constructor
            End Get
        End Property

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Return ImmutableArray(Of String).Empty
        End Function

        Protected NotOverridable Overrides Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            ' delegate methods don't inherit attributes from the delegate type, only parameters and return types do
            Return Nothing
        End Function

        Private NotInheritable Class Constructor
            Inherits SourceDelegateMethodSymbol

            Public Sub New(delegateType As NamedTypeSymbol,
                    voidType As TypeSymbol,
                    objectType As TypeSymbol,
                    intPtrType As TypeSymbol,
                    syntax As VisualBasicSyntaxNode,
                    binder As Binder)

                MyBase.New(delegateType,
                           syntax,
                           binder,
                           flags:=SourceMemberFlags.MethodKindConstructor Or SourceMemberFlags.AccessibilityPublic Or SourceMemberFlags.MethodIsSub,
                           returnType:=voidType)

                InitializeParameters(ImmutableArray.Create(Of ParameterSymbol)(
                    New SynthesizedParameterSymbol(Me, objectType, 0, False, StringConstants.DelegateConstructorInstanceParameterName),
                    New SynthesizedParameterSymbol(Me, intPtrType, 1, False, StringConstants.DelegateConstructorMethodParameterName)))
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return WellKnownMemberNames.InstanceConstructorName
                End Get
            End Property

            Protected Overrides Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
                ' Constructor doesn't have return type attributes
                Return Nothing
            End Function
        End Class

        Private NotInheritable Class InvokeMethod
            Inherits SourceDelegateMethodSymbol

            Public Sub New(delegateType As NamedTypeSymbol,
                    returnType As TypeSymbol,
                    syntax As VisualBasicSyntaxNode,
                    binder As Binder,
                    parameterListOpt As ParameterListSyntax,
                    diagnostics As BindingDiagnosticBag)

                MyBase.New(delegateType,
                           syntax,
                           binder,
                           flags:=SourceMemberFlags.MethodKindDelegateInvoke Or SourceMemberFlags.AccessibilityPublic Or SourceMemberFlags.Overridable Or
                                  If(returnType.SpecialType = SpecialType.System_Void, SourceMemberFlags.MethodIsSub, Nothing),
                           returnType:=returnType)

                InitializeParameters(binder.DecodeParameterListOfDelegateDeclaration(Me, parameterListOpt, diagnostics))
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return WellKnownMemberNames.DelegateInvokeName
                End Get
            End Property
        End Class

        Private NotInheritable Class BeginInvokeMethod
            Inherits SourceDelegateMethodSymbol

            Public Sub New(invoke As InvokeMethod,
                    iAsyncResultType As TypeSymbol,
                    objectType As TypeSymbol,
                    asyncCallbackType As TypeSymbol,
                    syntax As VisualBasicSyntaxNode,
                    binder As Binder)

                MyBase.New(invoke.ContainingType,
                           syntax,
                           binder,
                           flags:=SourceMemberFlags.MethodKindOrdinary Or SourceMemberFlags.AccessibilityPublic Or SourceMemberFlags.Overridable,
                           returnType:=iAsyncResultType)

                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance()

                Dim ordinal As Integer = 0
                For Each parameter In invoke.Parameters
                    ' Delegate parameters cannot be optional in VB.
                    ' In error cases where the parameter is specified optional in source, the parameter
                    ' will not have a True .IsOptional.
                    ' This is ensured by 'CheckDelegateParameterModifier'
                    Debug.Assert(Not parameter.IsOptional)
                    parameters.Add(New SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(DirectCast(parameter, SourceParameterSymbol), Me, ordinal))
                    ordinal += 1
                Next

                parameters.Add(New SynthesizedParameterSymbol(Me, asyncCallbackType, ordinal, False, StringConstants.DelegateMethodCallbackParameterName))
                ordinal += 1
                parameters.Add(New SynthesizedParameterSymbol(Me, objectType, ordinal, False, StringConstants.DelegateMethodInstanceParameterName))

                InitializeParameters(parameters.ToImmutableAndFree())
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return WellKnownMemberNames.DelegateBeginInvokeName
                End Get
            End Property

            Protected Overrides Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
                ' BeginInvoke doesn't have return type attributes
                Return Nothing
            End Function
        End Class

        Private NotInheritable Class EndInvokeMethod
            Inherits SourceDelegateMethodSymbol

            Public Sub New(invoke As InvokeMethod,
                    iAsyncResultType As TypeSymbol,
                    syntax As VisualBasicSyntaxNode,
                    binder As Binder)

                MyBase.New(invoke.ContainingType,
                           syntax,
                           binder,
                           flags:=SourceMemberFlags.MethodKindOrdinary Or SourceMemberFlags.AccessibilityPublic Or SourceMemberFlags.Overridable Or
                                  If(invoke.ReturnType.SpecialType = SpecialType.System_Void, SourceMemberFlags.MethodIsSub, Nothing),
                           returnType:=invoke.ReturnType)

                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance()

                Dim ordinal = 0
                For Each parameter In invoke.Parameters
                    ' Delegate parameters cannot be optional in VB.
                    ' In error cases where the parameter is specified optional in source, the parameter
                    ' will not have a True .IsOptional.
                    ' This is ensured by 'CheckDelegateParameterModifier'
                    Debug.Assert(Not parameter.IsOptional)
                    If parameter.IsByRef Then
                        parameters.Add(New SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(DirectCast(parameter, SourceParameterSymbol), Me, ordinal))
                        ordinal += 1
                    End If
                Next

                parameters.Add(New SynthesizedParameterSymbol(Me, iAsyncResultType, parameters.Count, False, StringConstants.DelegateMethodResultParameterName))

                InitializeParameters(parameters.ToImmutableAndFree())
            End Sub

            Public Overrides ReadOnly Property Name As String
                Get
                    Return WellKnownMemberNames.DelegateEndInvokeName
                End Get
            End Property

            Protected Overrides Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
                ' EndInvoke doesn't have return type attributes
                Return Nothing
            End Function
        End Class
    End Class
End Namespace
