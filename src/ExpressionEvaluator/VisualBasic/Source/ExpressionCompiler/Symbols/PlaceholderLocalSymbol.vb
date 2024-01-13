' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend MustInherit Class PlaceholderLocalSymbol
        Inherits EELocalSymbolBase

        Private ReadOnly _name As String

        Friend ReadOnly DisplayName As String

        Friend Sub New(method As MethodSymbol, name As String, displayName As String, type As TypeSymbol)
            MyBase.New(method, type)
            _name = name
            Me.DisplayName = displayName
        End Sub

        Friend Overloads Shared Function Create(
            typeNameDecoder As TypeNameDecoder(Of PEModuleSymbol, TypeSymbol),
            containingMethod As MethodSymbol,
            [alias] As [Alias]) As PlaceholderLocalSymbol

            Dim typeName = [alias].Type
            Debug.Assert(typeName.Length > 0)

            Dim type = typeNameDecoder.GetTypeSymbolForSerializedType(typeName)
            Debug.Assert(type IsNot Nothing)

            Dim dynamicFlags As ReadOnlyCollection(Of Byte) = Nothing
            Dim tupleElementNames As ReadOnlyCollection(Of String) = Nothing
            CustomTypeInfo.Decode([alias].CustomTypeInfoId, [alias].CustomTypeInfo, dynamicFlags, tupleElementNames)
            type = TupleTypeDecoder.DecodeTupleTypesIfApplicable(type, tupleElementNames.AsImmutableOrNull())

            Dim name = [alias].FullName
            Dim displayName = [alias].Name
            Select Case [alias].Kind
                Case DkmClrAliasKind.Exception
                    Return New ExceptionLocalSymbol(containingMethod, name, displayName, type, ExpressionCompilerConstants.GetExceptionMethodName)
                Case DkmClrAliasKind.StowedException
                    Return New ExceptionLocalSymbol(containingMethod, name, displayName, type, ExpressionCompilerConstants.GetStowedExceptionMethodName)
                Case DkmClrAliasKind.ReturnValue
                    Dim index As Integer = 0
                    PseudoVariableUtilities.TryParseReturnValueIndex(name, index)
                    Return New ReturnValueLocalSymbol(containingMethod, name, displayName, type, index)
                Case DkmClrAliasKind.ObjectId
                    Return New ObjectIdLocalSymbol(containingMethod, type, name, displayName, isReadOnly:=True)
                Case DkmClrAliasKind.Variable
                    Return New ObjectIdLocalSymbol(containingMethod, type, name, displayName, isReadOnly:=False)
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue([alias].Kind)
            End Select
        End Function

        Friend Overrides ReadOnly Property DeclarationKind As LocalDeclarationKind
            Get
                Return LocalDeclarationKind.Variable
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierToken As SyntaxToken
            Get
                Return Nothing
            End Get
        End Property

        Friend Overrides ReadOnly Property IdentifierLocation As Location
            Get
                Throw ExceptionUtilities.Unreachable
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return NoLocations
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _name
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return ImmutableArray(Of SyntaxReference).Empty
            End Get
        End Property

        Friend Overrides Function ToOtherMethod(method As MethodSymbol, typeMap As TypeSubstitution) As EELocalSymbolBase
            ' Pseudo-variables should be rewritten in PlaceholderLocalRewriter
            ' rather than copied as locals to the target method.
            Throw ExceptionUtilities.Unreachable
        End Function

        Friend MustOverride Overrides ReadOnly Property IsReadOnly As Boolean

        Friend MustOverride Function RewriteLocal(
            compilation As VisualBasicCompilation,
            container As EENamedTypeSymbol,
            syntax As SyntaxNode,
            isLValue As Boolean,
            diagnostics As DiagnosticBag) As BoundExpression

        Friend Shared Function ConvertToLocalType(
            compilation As VisualBasicCompilation,
            expr As BoundExpression,
            type As TypeSymbol,
            diagnostics As DiagnosticBag) As BoundExpression

            Dim syntax = expr.Syntax
            Dim exprType = expr.Type

            Dim conversionKind As ConversionKind
            If type.IsErrorType() Then
                diagnostics.Add(type.GetUseSiteInfo().DiagnosticInfo, syntax.GetLocation())
                conversionKind = Nothing
            ElseIf exprType.IsErrorType() Then
                diagnostics.Add(exprType.GetUseSiteInfo().DiagnosticInfo, syntax.GetLocation())
                conversionKind = Nothing
            Else
                Dim useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol) = Nothing
                Dim pair = Conversions.ClassifyConversion(exprType, type, useSiteInfo)
                Debug.Assert(useSiteInfo.Diagnostics Is Nothing, "If this happens, please add a test")

                diagnostics.Add(syntax, useSiteInfo.Diagnostics)

                Debug.Assert(pair.Value Is Nothing) ' Conversion method.
                conversionKind = pair.Key
            End If

            Return New BoundDirectCast(
                syntax,
                expr,
                conversionKind,
                type,
                hasErrors:=Not Conversions.ConversionExists(conversionKind)).MakeCompilerGenerated()
        End Function

        Friend Shared Function GetIntrinsicMethod(compilation As VisualBasicCompilation, methodName As String) As MethodSymbol
            Dim type = compilation.GetTypeByMetadataName(ExpressionCompilerConstants.IntrinsicAssemblyTypeMetadataName)
            Dim members = type.GetMembers(methodName)
            Debug.Assert(members.Length = 1)
            Return DirectCast(members(0), MethodSymbol)
        End Function

    End Class

End Namespace
