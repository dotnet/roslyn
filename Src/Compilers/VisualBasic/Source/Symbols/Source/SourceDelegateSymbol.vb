'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading
Imports Roslyn.Compilers.Common

Namespace Roslyn.Compilers.VisualBasic

    ''' <summary>
    ''' This class represents a synthesized delegate type derived from a delegate statement
    ''' </summary>
    Friend NotInheritable Class SourceDelegateSymbol
        Inherits SourceNamedTypeSymbol

        ' method flags for the synthesized delegate methods
        Public Const DelegateConstructorMethodFlags As SourceMemberFlags = SourceMemberFlags.MethodKindConstructor
        Public Const DelegateCommonMethodFlags As SourceMemberFlags = SourceMemberFlags.Overridable

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SourceDelegateSymbol" /> class.
        ''' </summary>
        ''' <param name="decl">The decl.</param>
        ''' <param name="containingSymbol">The containing symbol.</param>
        ''' <param name="containingModule">The containing module.</param>
        Public Sub New(decl As MergedTypeDeclaration,
                      containingSymbol As NamespaceOrTypeSymbol,
                      containingModule As SourceModuleSymbol)
            MyBase.New(decl, containingSymbol, containingModule)
        End Sub

        ''' <summary>
        ''' Adds the delegate members to this source delegate symbol
        ''' </summary>
        ''' <param name="members">The member collections where to add the member to</param>
        ''' <param name="tree">The syntax tree</param>
        ''' <param name="syntax">The syntax of the delegate declaration (sub or function delegate statement)</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        Protected Overrides Sub AddDelegateMembers(
            members As Dictionary(Of String, ArrayBuilder(Of Symbol)),
            tree As SyntaxTree,
            syntax As DelegateStatementSyntax,
            binder As Binder,
            diagnostics As DiagnosticBag)

            ' --- bind return type -------------------------------------------------
            Dim voidType As NamedTypeSymbol = binder.GetSpecialType(Compilers.SpecialType.System_Void, syntax, diagnostics)
            Dim iAsyncResult As NamedTypeSymbol = binder.GetSpecialType(Compilers.SpecialType.System_IAsyncResult, syntax, diagnostics)

            Dim returnType As TypeSymbol
            Dim returnTypeAttributes As SyntaxList(Of AttributeBlockSyntax) = Nothing

            If syntax.Kind = SyntaxKind.DelegateSubStatement Then
                returnType = voidType
            Else
                Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

                If binder.OptionStrict = OptionStrict.On Then
                    getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowsImplicitProc
                ElseIf binder.OptionStrict = OptionStrict.Custom Then
                    getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinFunction
                End If

                Dim asClause = DirectCast(syntax.AsClause, SimpleAsClauseSyntax)
                returnType = binder.DecodeIdentifierType(syntax.Identifier, asClause, getErrorInfo, diagnostics)
                If asClause IsNot Nothing Then
                    returnTypeAttributes = DirectCast(syntax.AsClause, SimpleAsClauseSyntax).Attributes
                End If
            End If

            ' A delegate has the following members: (see CLI spec 13.6)
            ' (1) a method named Invoke with the specified signature
            Dim delegateInvoke = New SynthesizedDelegateMethodSymbol(CommonMemberNames.DelegateInvokeName,
                                                                     Me,
                                                                     DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindDelegateInvoke,
                                                                     returnTypeAttributes,
                                                                     returnType,
                                                                     syntax.ParameterList,
                                                                     binder,
                                                                     diagnostics)

            AddSymbolToMembers(delegateInvoke, members)

            ' (2) a constructor with argument types (object, System.IntPtr)
            Dim delegateCtor = New SynthesizedDelegateMethodSymbol(CommonMemberNames.InstanceConstructorName, Me, DelegateConstructorMethodFlags, voidType)
            delegateCtor.SetParameters(
                ReadOnlyArray(Of ParameterSymbol).CreateFrom(
                       New SynthesizedParameterSymbol(delegateCtor, binder.GetSpecialType(Compilers.SpecialType.System_Object, syntax, diagnostics), 0, False, StringConstants.DelegateConstructorInstanceParameterName),
                       New SynthesizedParameterSymbol(delegateCtor, binder.GetSpecialType(Compilers.SpecialType.System_IntPtr, syntax, diagnostics), 1, False, StringConstants.DelegateConstructorMethodParameterName)
                       ))
            AddSymbolToMembers(delegateCtor, members)

            ' (3) BeginInvoke
            Dim delegateBeginInvoke = New SynthesizedDelegateMethodSymbol(CommonMemberNames.DelegateBeginInvokeName, Me,
                                                                          DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                                          iAsyncResult)

            Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance()

            Dim ordinal As Integer = 0
            For Each parameter In delegateInvoke.Parameters
                parameters.Add(New SynthesizedParameterSymbol(delegateBeginInvoke, parameter.Type, ordinal, parameter.IsByRef(), parameter.Name))
                ordinal += 1
            Next
            parameters.Add(New SynthesizedParameterSymbol(delegateBeginInvoke, binder.GetSpecialType(Compilers.SpecialType.System_AsyncCallback, syntax, diagnostics), ordinal, False, StringConstants.DelegateMethodCallbackParameterName))
            ordinal += 1
            parameters.Add(New SynthesizedParameterSymbol(delegateBeginInvoke, binder.GetSpecialType(Compilers.SpecialType.System_Object, syntax, diagnostics), ordinal, False, StringConstants.DelegateMethodInstanceParameterName))
            delegateBeginInvoke.SetParameters(parameters.ToReadOnly())
            AddSymbolToMembers(delegateBeginInvoke, members)
            parameters.Clear()

            ' and (4) EndInvoke methods
            Dim delegateEndInvoke = New SynthesizedDelegateMethodSymbol(CommonMemberNames.DelegateEndInvokeName, Me,
                                                                        DelegateCommonMethodFlags Or SourceMemberFlags.MethodKindOrdinary,
                                                                        returnType)

            ordinal = 0
            For Each parameter In delegateInvoke.Parameters

                If parameter.IsByRef Then
                    parameters.Add(New SynthesizedParameterSymbol(delegateEndInvoke, parameter.Type, ordinal, parameter.IsByRef(), parameter.Name))
                    ordinal += 1
                End If
            Next

            parameters.Add(New SynthesizedParameterSymbol(delegateEndInvoke, iAsyncResult, parameters.Count, False, StringConstants.DelegateMethodResultParameterName))
            delegateEndInvoke.SetParameters(parameters.ToReadOnlyAndFree())
            AddSymbolToMembers(delegateEndInvoke, members)
        End Sub

        Friend Overrides Function GetAttributesBag() As CustomAttributesBag(Of AttributeData)
            If (m_lazyCustomAttributesBag Is Nothing OrElse Not m_lazyCustomAttributesBag.IsSealed) Then
                Dim syntaxRef = TypeDeclaration.SyntaxReferences(0)
                Dim sourceModule = DirectCast(Me.ContainingModule, SourceModuleSymbol)
                Dim binder As Binder = BinderBuilder.CreateBinderForAttribute(sourceModule, syntaxRef.SyntaxTree, Me, Me.ContainingNamespace)
                LoadAndValidateAttributes(binder, DirectCast(syntaxRef.GetSyntax(), DelegateStatementSyntax).Attributes, m_lazyCustomAttributesBag, AttributeTargets.Delegate)
            End If
            Return m_lazyCustomAttributesBag
        End Function

        Friend Overrides ReadOnly Property DefaultPropertyName As String
            Get
                Debug.Assert(MyBase.DefaultPropertyName Is Nothing)
                Return Nothing
            End Get
        End Property
    End Class
End Namespace