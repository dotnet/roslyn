' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' Binder used for interiors of documentation comment for binding 'cref' attribute value
    ''' </summary>
    Partial Friend NotInheritable Class DocumentationCommentCrefBinder
        Inherits DocumentationCommentBinder

        Public Sub New(containingBinder As Binder, commentedSymbol As Symbol)
            MyBase.New(containingBinder, commentedSymbol)
        End Sub

        Private _typeParameterBinder As TypeParametersBinder

        Private Function GetOrCreateTypeParametersAwareBinder(typeParameters As Dictionary(Of String, CrefTypeParameterSymbol)) As Binder
            If Me._typeParameterBinder Is Nothing Then
                Interlocked.CompareExchange(Me._typeParameterBinder, New TypeParametersBinder(Me, typeParameters), Nothing)
            End If

#If DEBUG Then
            ' Make sure the type parameter symbols are the same
            Debug.Assert(typeParameters.Count = Me._typeParameterBinder._typeParameters.Count)
            For Each kvp In typeParameters
                Debug.Assert(kvp.Value.Equals(Me._typeParameterBinder._typeParameters(kvp.Key)))
            Next
#End If

            Return Me._typeParameterBinder
        End Function

        Private Shared Function HasTrailingSkippedTokensAndShouldReportError(reference As CrefReferenceSyntax) As Boolean
            Dim triviaList As SyntaxTriviaList = reference.GetTrailingTrivia()
            For Each trivia In triviaList
                If trivia.Kind = SyntaxKind.SkippedTokensTrivia Then
                    ' ignore those, representing VB intrinsic types
                    Dim name As TypeSyntax = reference.Name
                    If name.Kind = SyntaxKind.IdentifierName Then
                        Dim identifier As SyntaxToken = DirectCast(name, IdentifierNameSyntax).Identifier
                        If Not identifier.IsBracketed AndAlso IsIntrinsicTypeForDocumentationComment(SyntaxFacts.GetKeywordKind(identifier.ValueText)) Then
                            ' special case to be ignored, also see description 
                            ' in ParseXml.vb::TryParseXmlCrefAttributeValue(...)
                            Continue For
                        End If

                    ElseIf name.Kind = SyntaxKind.PredefinedType Then
                        Continue For
                    End If

                    ' Otherwise report an error
                    Return True
                End If
            Next

            Return False
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(reference As CrefReferenceSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            ' If the node has trailing syntax nodes, it should report error, unless the name 
            ' is a VB intrinsic type (which ensures compatibility with Dev11)
            If HasTrailingSkippedTokensAndShouldReportError(reference) Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            If reference.Signature Is Nothing Then
                Return BindNameInsideCrefReferenceInLegacyMode(reference.Name, preserveAliases, useSiteInfo)
            End If

            ' Extended 'cref' attribute syntax should not contain complex generic arguments 
            ' such as in [cref="List(Of Action(Of A))"], because these generic arguments actually 
            ' define type parameters to be used in signature part and return value
            If NameSyntaxHasComplexGenericArguments(reference.Name) Then
                Return ImmutableArray(Of Symbol).Empty
            End If

            Dim symbols = ArrayBuilder(Of Symbol).GetInstance

            ' Bind the name part and collect type parameters
            Dim typeParameters As New Dictionary(Of String, CrefTypeParameterSymbol)(CaseInsensitiveComparison.Comparer)
            CollectCrefNameSymbolsStrict(reference.Name, reference.Signature.ArgumentTypes.Count, typeParameters, symbols, preserveAliases, useSiteInfo)
            If symbols.Count = 0 Then
                symbols.Free()
                Return ImmutableArray(Of Symbol).Empty
            End If

            RemoveOverriddenMethodsAndProperties(symbols)

            ' Bind signature and return type if present
            Dim signatureTypes As ArrayBuilder(Of SignatureElement) = Nothing
            Dim returnType As TypeSymbol = Nothing
            BindSignatureAndReturnValue(reference, typeParameters, signatureTypes, returnType, diagnosticBag)

            ' Create only if needed
            Debug.Assert(signatureTypes Is Nothing OrElse signatureTypes.Count > 0)
            Dim signatureParameterCount As Integer = If(signatureTypes Is Nothing, 0, signatureTypes.Count)

            '  Choose between symbols those with matching signatures
            Dim candidatePointer As Integer = 0
            Dim goodPointer As Integer = 0

            While candidatePointer < symbols.Count
                Dim candidateSymbol As Symbol = symbols(candidatePointer)

                ' NOTE: we do a very simple signature check and 
                '       avoid using signature comparer

                Select Case candidateSymbol.Kind
                    Case SymbolKind.Method
                        Dim candidateMethod = DirectCast(candidateSymbol, MethodSymbol)

                        If candidateMethod.ParameterCount <> signatureParameterCount Then
                            ' Signature does not match
                            Exit Select
                        End If

                        Dim parameters As ImmutableArray(Of ParameterSymbol) = candidateMethod.Parameters
                        For i = 0 To signatureParameterCount - 1
                            Dim parameter As ParameterSymbol = parameters(i)
                            If parameter.IsByRef <> signatureTypes(i).IsByRef OrElse
                                    Not parameter.Type.IsSameTypeIgnoringAll(signatureTypes(i).Type) Then

                                ' Signature does not match
                                Exit Select
                            End If
                        Next

                        If returnType IsNot Nothing Then
                            If candidateMethod.IsSub OrElse Not candidateMethod.ReturnType.IsSameTypeIgnoringAll(returnType) Then
                                ' Return type does not match
                                Exit Select
                            End If
                        End If

                        ' Good candidate
                        symbols(goodPointer) = candidateSymbol
                        goodPointer += 1
                        candidatePointer += 1
                        Continue While

                    Case SymbolKind.Property
                        Dim candidateProperty = DirectCast(candidateSymbol, PropertySymbol)
                        Dim parameters As ImmutableArray(Of ParameterSymbol) = candidateProperty.Parameters

                        If parameters.Length <> signatureParameterCount Then
                            ' Signature does not match
                            Exit Select
                        End If

                        For i = 0 To signatureParameterCount - 1
                            Dim parameter As ParameterSymbol = parameters(i)
                            If parameter.IsByRef <> signatureTypes(i).IsByRef OrElse
                                    Not parameter.Type.IsSameTypeIgnoringAll(signatureTypes(i).Type) Then

                                ' Signature does not match
                                Exit Select
                            End If
                        Next

                        Debug.Assert(returnType Is Nothing,
                                     "Return type is only allowed for Operator CType, why we found a property here?")

                        ' Good candidate
                        symbols(goodPointer) = candidateSymbol
                        goodPointer += 1
                        candidatePointer += 1
                        Continue While

                    Case Else
                        ' All other symbols are ignored
                End Select

                ' Ignore symbol
                candidatePointer += 1
            End While

            If signatureTypes IsNot Nothing Then
                signatureTypes.Free()
            End If

            If goodPointer < candidatePointer Then
                symbols.Clip(goodPointer)
            End If

            Return symbols.ToImmutableAndFree()
        End Function

        Friend Overrides Function BindInsideCrefAttributeValue(name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            Dim isPartOfSignatureOrReturnType As Boolean = False
            Dim crefReference As CrefReferenceSyntax = GetEnclosingCrefReference(name, isPartOfSignatureOrReturnType)

            If crefReference Is Nothing Then
                Debug.Assert(False, "Speculative binding??")
                Return ImmutableArray(Of Symbol).Empty
            End If

            If crefReference.Signature Is Nothing Then
                Debug.Assert(Not isPartOfSignatureOrReturnType)
                Return BindNameInsideCrefReferenceInLegacyMode(name, preserveAliases, useSiteInfo)
            End If

            If isPartOfSignatureOrReturnType Then
                Return BindInsideCrefSignatureOrReturnType(crefReference, name, preserveAliases, diagnosticBag)
            Else
                Return BindInsideCrefReferenceName(name, crefReference.Signature.ArgumentTypes.Count, preserveAliases, useSiteInfo)
            End If
        End Function

        Private Function BindInsideCrefSignatureOrReturnType(crefReference As CrefReferenceSyntax, name As TypeSyntax, preserveAliases As Boolean, diagnosticBag As BindingDiagnosticBag) As ImmutableArray(Of Symbol)
            Dim typeParameterAwareBinder As Binder = Me.GetOrCreateTypeParametersAwareBinder(crefReference)

            Dim result As Symbol = typeParameterAwareBinder.BindNamespaceOrTypeOrAliasSyntax(name, If(diagnosticBag, BindingDiagnosticBag.Discarded))
            result = typeParameterAwareBinder.BindNamespaceOrTypeOrAliasSyntax(name, If(diagnosticBag, BindingDiagnosticBag.Discarded))

            If result IsNot Nothing AndAlso result.Kind = SymbolKind.Alias AndAlso Not preserveAliases Then
                result = DirectCast(result, AliasSymbol).Target
            End If

            Return If(result Is Nothing,
                      ImmutableArray(Of Symbol).Empty,
                      ImmutableArray.Create(Of Symbol)(result))
        End Function

        Private Function GetOrCreateTypeParametersAwareBinder(crefReference As CrefReferenceSyntax) As Binder
            ' To create type-param-aware binder we need to have type parameters, 
            ' but we don't want to do so if the binder is already created
            If Me._typeParameterBinder IsNot Nothing Then
                Return Me._typeParameterBinder
            End If

            Dim typeParameters As New Dictionary(Of String, CrefTypeParameterSymbol)(IdentifierComparison.Comparer)

            Dim crefName As TypeSyntax = crefReference.Name
            Dim genericName As GenericNameSyntax = Nothing

            While crefName IsNot Nothing

                Select Case crefName.Kind
                    Case SyntaxKind.GenericName
                        genericName = DirectCast(crefName, GenericNameSyntax)
                        crefName = Nothing

                    Case SyntaxKind.QualifiedName
                        Dim qName = DirectCast(crefName, QualifiedNameSyntax)
                        crefName = qName.Left

                        If qName.Right.Kind = SyntaxKind.GenericName Then
                            genericName = DirectCast(qName.Right, GenericNameSyntax)
                        End If

                    Case SyntaxKind.IdentifierName,
                         SyntaxKind.CrefOperatorReference,
                         SyntaxKind.GlobalName,
                         SyntaxKind.PredefinedType
                        Exit While

                    Case SyntaxKind.QualifiedCrefOperatorReference
                        crefName = DirectCast(crefName, QualifiedCrefOperatorReferenceSyntax).Left

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(crefName.Kind)
                End Select

                ' Fall back to the next name part, if we need to collect type 
                ' parameters genericName is supposed to be not Nothing
                If genericName IsNot Nothing Then
                    Dim arguments As SeparatedSyntaxList(Of TypeSyntax) = genericName.TypeArgumentList.Arguments

                    For i = 0 To arguments.Count - 1
                        Dim typeSyntax As TypeSyntax = arguments(i)

                        Select Case typeSyntax.Kind
                            Case SyntaxKind.IdentifierName
                                Dim identifier = DirectCast(typeSyntax, IdentifierNameSyntax)
                                Dim typeParameterName As String = identifier.Identifier.ValueText

                                ' As we go 'left-to-right' don't override right-most parameters with the same name
                                If Not typeParameters.ContainsKey(typeParameterName) Then
                                    typeParameters(typeParameterName) = New CrefTypeParameterSymbol(i, typeParameterName, identifier)
                                End If

                            Case Else
                                ' An error case
                        End Select
                    Next
                End If

            End While

            Return GetOrCreateTypeParametersAwareBinder(typeParameters)
        End Function

        Private Function BindInsideCrefReferenceName(name As TypeSyntax, argCount As Integer, preserveAliases As Boolean, <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol)) As ImmutableArray(Of Symbol)
            ' NOTE: in code here and below 'parent' may be Nothing in 
            '       case of speculative binding (which is NYI)
            Dim parent As VisualBasicSyntaxNode = name.Parent

            ' Type parameter
            If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.TypeArgumentList Then
                Dim ordinal As Integer = DirectCast(parent, TypeArgumentListSyntax).Arguments.IndexOf(name)

                If name.Kind = SyntaxKind.IdentifierName Then
                    Dim identifier = DirectCast(name, IdentifierNameSyntax)
                    Return ImmutableArray.Create(Of Symbol)(New CrefTypeParameterSymbol(ordinal, identifier.Identifier.ValueText, identifier))
                End If

                ' An error case
                Return ImmutableArray.Create(Of Symbol)(New CrefTypeParameterSymbol(ordinal, StringConstants.NamedSymbolErrorName, name))
            End If

            ' Names considered to be checked for color-color case are Identifier or Generic names which 
            ' are the left part of the qualified name parent 
            Dim checkForColorColor As Boolean = False
            Dim nameText As String = Nothing
            Dim arity As Integer = -1

lAgain:
            Select Case name.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName

                    If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.QualifiedName Then
                        Dim qualified = DirectCast(parent, QualifiedNameSyntax)
                        If qualified.Right Is name Then
                            name = qualified
                            parent = name.Parent
                            GoTo lAgain
                        End If
                    End If

                    ' color-color info
                    checkForColorColor = True
                    If name.Kind = SyntaxKind.IdentifierName Then
                        nameText = DirectCast(name, IdentifierNameSyntax).Identifier.ValueText
                        arity = 0

                    Else
                        Dim generic = DirectCast(name, GenericNameSyntax)
                        nameText = generic.Identifier.ValueText
                        arity = generic.TypeArgumentList.Arguments.Count
                    End If

                ' Fall through

                Case SyntaxKind.CrefOperatorReference
                    If parent IsNot Nothing AndAlso parent.Kind = SyntaxKind.QualifiedCrefOperatorReference Then
                        name = DirectCast(parent, QualifiedCrefOperatorReferenceSyntax)
                        parent = name.Parent
                        GoTo lAgain
                    End If
                ' Fall through

                Case SyntaxKind.QualifiedName,
                     SyntaxKind.QualifiedCrefOperatorReference
                    ' Fall through

                Case SyntaxKind.GlobalName
                    Return ImmutableArray.Create(Of Symbol)(Me.Compilation.GlobalNamespace)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(name.Kind)
            End Select

            Dim symbols = ArrayBuilder(Of Symbol).GetInstance
            CollectCrefNameSymbolsStrict(name, argCount, New Dictionary(Of String, CrefTypeParameterSymbol)(IdentifierComparison.Comparer), symbols, preserveAliases, useSiteInfo)

            RemoveOverriddenMethodsAndProperties(symbols)

            If symbols.Count = 1 AndAlso checkForColorColor Then
                Dim symbol As Symbol = symbols(0)
                Dim type As TypeSymbol = Nothing

                Select Case symbol.Kind
                    Case SymbolKind.Field
                        type = DirectCast(symbol, FieldSymbol).Type

                    Case SymbolKind.Method
                        type = DirectCast(symbol, MethodSymbol).ReturnType

                    Case SymbolKind.Property
                        type = DirectCast(symbol, PropertySymbol).Type
                End Select

                Dim replaceWithType As Boolean = False
                If type IsNot Nothing Then
                    If IdentifierComparison.Equals(type.Name, nameText) Then
                        Dim namedType = TryCast(type, NamedTypeSymbol)

                        If namedType IsNot Nothing Then
                            replaceWithType = namedType.Arity = arity
                        Else
                            replaceWithType = arity = 0
                        End If
                    End If
                End If

                If replaceWithType Then
                    symbols(0) = type
                End If
            End If

            Return symbols.ToImmutableAndFree()
        End Function

        Private Shared Function GetEnclosingCrefReference(nameFromCref As TypeSyntax, <Out> ByRef partOfSignatureOrReturnType As Boolean) As CrefReferenceSyntax
            partOfSignatureOrReturnType = False

            Dim node As VisualBasicSyntaxNode = nameFromCref
            While node IsNot Nothing

                Select Case node.Kind
                    Case SyntaxKind.CrefReference
                        Exit While

                    Case SyntaxKind.SimpleAsClause
                        partOfSignatureOrReturnType = True

                    Case SyntaxKind.CrefSignature
                        partOfSignatureOrReturnType = True
                End Select

                node = node.Parent
            End While

            Return DirectCast(node, CrefReferenceSyntax)
        End Function

        Private Structure SignatureElement
            Public ReadOnly Type As TypeSymbol
            Public ReadOnly IsByRef As Boolean

            Public Sub New(type As TypeSymbol, isByRef As Boolean)
                Me.Type = type
                Me.IsByRef = isByRef
            End Sub
        End Structure

        Private Sub BindSignatureAndReturnValue(reference As CrefReferenceSyntax,
                                                typeParameters As Dictionary(Of String, CrefTypeParameterSymbol),
                                                <Out> ByRef signatureTypes As ArrayBuilder(Of SignatureElement),
                                                <Out> ByRef returnType As TypeSymbol,
                                                diagnosticBag As BindingDiagnosticBag)

            signatureTypes = Nothing
            returnType = Nothing

            Dim typeParameterAwareBinder As Binder = Me.GetOrCreateTypeParametersAwareBinder(typeParameters)
            Dim diagnostic = If(diagnosticBag, BindingDiagnosticBag.Discarded)

            Dim signature As CrefSignatureSyntax = reference.Signature
            Debug.Assert(signature IsNot Nothing)

            If signature.ArgumentTypes.Count > 0 Then
                signatureTypes = ArrayBuilder(Of SignatureElement).GetInstance

                For Each part In signature.ArgumentTypes
                    signatureTypes.Add(
                        New SignatureElement(
                            typeParameterAwareBinder.BindTypeSyntax(part.Type, diagnostic),
                            part.Modifier.Kind = SyntaxKind.ByRefKeyword))
                Next
            End If

            If reference.AsClause IsNot Nothing Then
                returnType = typeParameterAwareBinder.BindTypeSyntax(reference.AsClause.Type, diagnostic)
            End If
        End Sub

        Private Sub CollectCrefNameSymbolsStrict(nameFromCref As TypeSyntax,
                                                 argsCount As Integer,
                                                 typeParameters As Dictionary(Of String, CrefTypeParameterSymbol),
                                                 symbols As ArrayBuilder(Of Symbol),
                                                 preserveAlias As Boolean,
                                                 <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))

            ' This binding mode is used for extended cref-reference syntax with mandatory
            ' signature specified, we enforce more strict rules in this case

            Select Case nameFromCref.Kind
                Case SyntaxKind.QualifiedCrefOperatorReference
                    ' 'A.B.Operator+' or 'C(Of T).Operator CType'
                    CollectQualifiedOperatorReferenceSymbolsStrict(
                        DirectCast(nameFromCref, QualifiedCrefOperatorReferenceSyntax), argsCount, typeParameters, symbols, useSiteInfo)

                Case SyntaxKind.CrefOperatorReference
                    ' 'Operator+' or 'Operator CType'
                    CollectTopLevelOperatorReferenceStrict(
                        DirectCast(nameFromCref, CrefOperatorReferenceSyntax), argsCount, symbols, useSiteInfo)

                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName
                    ' 'New', 'A', or 'B(Of T)'
                    CollectSimpleNameSymbolsStrict(
                        DirectCast(nameFromCref, SimpleNameSyntax), typeParameters, symbols, preserveAlias, useSiteInfo, False)

                Case SyntaxKind.QualifiedName
                    ' 'A(Of T).B.M(Of E)'
                    CollectQualifiedNameSymbolsStrict(
                        DirectCast(nameFromCref, QualifiedNameSyntax), typeParameters, symbols, preserveAlias, useSiteInfo)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(nameFromCref.Kind)
            End Select
        End Sub

        Private Sub CollectTopLevelOperatorReferenceStrict(reference As CrefOperatorReferenceSyntax, argCount As Integer, symbols As ArrayBuilder(Of Symbol), <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            CollectOperatorsAndConversionsInType(reference, argCount, Me.ContainingType, symbols, useSiteInfo)
        End Sub

        Private Sub CollectSimpleNameSymbolsStrict(node As SimpleNameSyntax,
                                                   typeParameters As Dictionary(Of String, CrefTypeParameterSymbol),
                                                   symbols As ArrayBuilder(Of Symbol),
                                                   preserveAlias As Boolean,
                                                   <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                                   typeOrNamespaceOnly As Boolean)

            ' Name syntax of Cref should not have diagnostics
            If node.ContainsDiagnostics Then
                Return
            End If

            If node.Kind = SyntaxKind.GenericName Then
                ' Generic name
                Dim genericName = DirectCast(node, GenericNameSyntax)

                ' Search exact arity only
                CollectSimpleNameSymbolsStrict(genericName.Identifier.ValueText,
                                               genericName.TypeArgumentList.Arguments.Count,
                                               symbols,
                                               preserveAlias,
                                               useSiteInfo,
                                               typeOrNamespaceOnly)

                CreateTypeParameterSymbolsAndConstructSymbols(genericName, symbols, typeParameters)

            Else
                ' Simple identifier name
                Debug.Assert(node.Kind = SyntaxKind.IdentifierName)

                Dim identifier = DirectCast(node, IdentifierNameSyntax)
                Dim token As SyntaxToken = identifier.Identifier

                If IdentifierComparison.Equals(identifier.Identifier.ValueText, SyntaxFacts.GetText(SyntaxKind.NewKeyword)) AndAlso Not token.IsBracketed Then
                    CollectConstructorsSymbolsStrict(symbols)

                Else
                    ' Search 0-arity only
                    CollectSimpleNameSymbolsStrict(identifier.Identifier.ValueText, 0, symbols, preserveAlias, useSiteInfo, typeOrNamespaceOnly)
                End If
            End If
        End Sub

        Private Sub CollectQualifiedNameSymbolsStrict(node As QualifiedNameSyntax,
                                                      typeParameters As Dictionary(Of String, CrefTypeParameterSymbol),
                                                      symbols As ArrayBuilder(Of Symbol),
                                                      preserveAlias As Boolean,
                                                      <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))

            ' Name syntax of Cref should not have diagnostics
            If node.ContainsDiagnostics Then
                Return
            End If

            Dim allowColorColor As Boolean = True

            Dim left As NameSyntax = node.Left
            Select Case left.Kind
                Case SyntaxKind.IdentifierName
                    CollectSimpleNameSymbolsStrict(DirectCast(left, SimpleNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo, typeOrNamespaceOnly:=True)

                Case SyntaxKind.GenericName
                    CollectSimpleNameSymbolsStrict(DirectCast(left, SimpleNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo, typeOrNamespaceOnly:=True)

                Case SyntaxKind.QualifiedName
                    CollectQualifiedNameSymbolsStrict(DirectCast(left, QualifiedNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo)
                    allowColorColor = False

                Case SyntaxKind.GlobalName
                    symbols.Add(Me.Compilation.GlobalNamespace)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(left.Kind)
            End Select

            If symbols.Count <> 1 Then
                ' Stop resolving
                typeParameters.Clear()
                symbols.Clear()
                Return
            End If

            Dim singleSymbol As Symbol = symbols(0)
            symbols.Clear()

            ' We found one single symbol, we need to search for the 'right' 
            ' name in the context of this symbol
            Dim right As SimpleNameSyntax = node.Right

            If right.Kind = SyntaxKind.GenericName Then
                ' Generic name
                Dim genericName = DirectCast(right, GenericNameSyntax)

                ' Search exact arity only
                CollectSimpleNameSymbolsStrict(singleSymbol,
                                               allowColorColor,
                                               genericName.Identifier.ValueText,
                                               genericName.TypeArgumentList.Arguments.Count,
                                               symbols,
                                               preserveAlias,
                                               useSiteInfo)

                CreateTypeParameterSymbolsAndConstructSymbols(genericName, symbols, typeParameters)

            Else
                ' Simple identifier name
                Debug.Assert(right.Kind = SyntaxKind.IdentifierName)

                Dim identifier = DirectCast(right, IdentifierNameSyntax)
                Dim token As SyntaxToken = identifier.Identifier

                If IdentifierComparison.Equals(identifier.Identifier.ValueText, SyntaxFacts.GetText(SyntaxKind.NewKeyword)) AndAlso Not token.IsBracketed Then
                    CollectConstructorsSymbolsStrict(singleSymbol, symbols)

                Else
                    ' Search 0-arity only
                    CollectSimpleNameSymbolsStrict(singleSymbol, allowColorColor, identifier.Identifier.ValueText, 0, symbols, preserveAlias, useSiteInfo)
                End If
            End If
        End Sub

        Private Sub CollectQualifiedOperatorReferenceSymbolsStrict(node As QualifiedCrefOperatorReferenceSyntax,
                                                                   argCount As Integer,
                                                                   typeParameters As Dictionary(Of String, CrefTypeParameterSymbol),
                                                                   symbols As ArrayBuilder(Of Symbol),
                                                                   <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))

            ' Name syntax of Cref should not have diagnostics
            If node.ContainsDiagnostics Then
                Return
            End If

            Dim allowColorColor As Boolean = True

            Dim left As NameSyntax = node.Left
            Select Case left.Kind
                Case SyntaxKind.IdentifierName
                    CollectSimpleNameSymbolsStrict(DirectCast(left, SimpleNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo, typeOrNamespaceOnly:=True)

                Case SyntaxKind.GenericName
                    CollectSimpleNameSymbolsStrict(DirectCast(left, SimpleNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo, typeOrNamespaceOnly:=True)

                Case SyntaxKind.QualifiedName
                    CollectQualifiedNameSymbolsStrict(DirectCast(left, QualifiedNameSyntax), typeParameters, symbols, preserveAlias:=False, useSiteInfo:=useSiteInfo)
                    allowColorColor = False

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(left.Kind)
            End Select

            If symbols.Count <> 1 Then
                ' Stop resolving
                typeParameters.Clear()
                symbols.Clear()
                Return
            End If

            Dim singleSymbol As Symbol = symbols(0)
            symbols.Clear()

            If singleSymbol.Kind = SymbolKind.Alias Then
                singleSymbol = DirectCast(singleSymbol, AliasSymbol).Target
            End If

            CollectOperatorsAndConversionsInType(node.Right, argCount, TryCast(singleSymbol, TypeSymbol), symbols, useSiteInfo)
        End Sub

        Private Sub CollectConstructorsSymbolsStrict(symbols As ArrayBuilder(Of Symbol))
            Dim containingSymbol As Symbol = Me.ContainingMember
            If containingSymbol Is Nothing Then
                Return
            End If

            If containingSymbol.Kind <> SymbolKind.NamedType Then
                containingSymbol = containingSymbol.ContainingType
            End If

            Dim type = DirectCast(containingSymbol, NamedTypeSymbol)
            If type IsNot Nothing Then
                symbols.AddRange(type.InstanceConstructors)
            End If
            Return
        End Sub

        Private Shared Sub CollectConstructorsSymbolsStrict(containingSymbol As Symbol, symbols As ArrayBuilder(Of Symbol))
            Debug.Assert(symbols.Count = 0)
            If containingSymbol.Kind = SymbolKind.NamedType Then
                symbols.AddRange(DirectCast(containingSymbol, NamedTypeSymbol).InstanceConstructors)
            End If
        End Sub

        Private Sub CollectSimpleNameSymbolsStrict(name As String,
                                                   arity As Integer,
                                                   symbols As ArrayBuilder(Of Symbol),
                                                   preserveAlias As Boolean,
                                                   <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                                   typeOrNamespaceOnly As Boolean)

            Debug.Assert(Not String.IsNullOrEmpty(name))
            Debug.Assert(arity >= 0)

            Const options As LookupOptions =
                                LookupOptions.UseBaseReferenceAccessibility Or
                                LookupOptions.MustNotBeReturnValueVariable Or
                                LookupOptions.IgnoreExtensionMethods Or
                                LookupOptions.MustNotBeLocalOrParameter Or
                                LookupOptions.NoSystemObjectLookupForInterfaces

            Dim result As LookupResult = LookupResult.GetInstance()

            Me.Lookup(result, name, arity, If(typeOrNamespaceOnly, options Or LookupOptions.NamespacesOrTypesOnly, options), useSiteInfo)

            If Not result.IsGoodOrAmbiguous OrElse Not result.HasSymbol Then
                result.Free()
                Return
            End If

            CollectGoodOrAmbiguousFromLookupResult(result, symbols, preserveAlias)
            result.Free()
        End Sub

        Private Sub CollectSimpleNameSymbolsStrict(containingSymbol As Symbol,
                                                   allowColorColor As Boolean,
                                                   name As String,
                                                   arity As Integer,
                                                   symbols As ArrayBuilder(Of Symbol),
                                                   preserveAlias As Boolean,
                                                   <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))

            Debug.Assert(Not String.IsNullOrEmpty(name))
            Debug.Assert(arity >= 0)

            Dim lookupResult As LookupResult = lookupResult.GetInstance()

            Dim options As LookupOptions = LookupOptions.UseBaseReferenceAccessibility Or
                                           LookupOptions.MustNotBeReturnValueVariable Or
                                           LookupOptions.IgnoreExtensionMethods Or
                                           LookupOptions.MustNotBeLocalOrParameter Or
                                           LookupOptions.NoSystemObjectLookupForInterfaces
lAgain:
            Select Case containingSymbol.Kind
                Case SymbolKind.Namespace
                    LookupMember(lookupResult, DirectCast(containingSymbol, NamespaceSymbol), name, arity, options, useSiteInfo)

                Case SymbolKind.Alias
                    containingSymbol = DirectCast(containingSymbol, AliasSymbol).Target
                    GoTo lAgain

                Case SymbolKind.NamedType, SymbolKind.ArrayType
                    LookupMember(lookupResult, DirectCast(containingSymbol, TypeSymbol), name, arity, options, useSiteInfo)

                Case SymbolKind.Property
                    If allowColorColor Then
                        ' Check for Color Color case
                        Dim [property] = DirectCast(containingSymbol, PropertySymbol)
                        Dim propertyType As TypeSymbol = [property].Type
                        If IdentifierComparison.Equals([property].Name, propertyType.Name) Then
                            containingSymbol = propertyType
                            GoTo lAgain
                        End If
                    End If

                Case SymbolKind.Field
                    If allowColorColor Then
                        ' Check for Color Color case
                        Dim field = DirectCast(containingSymbol, FieldSymbol)
                        Dim fieldType As TypeSymbol = field.Type
                        If IdentifierComparison.Equals(field.Name, fieldType.Name) Then
                            containingSymbol = fieldType
                            GoTo lAgain
                        End If
                    End If

                Case SymbolKind.Method
                    ' Check for Color Color case
                    If allowColorColor Then
                        Dim method = DirectCast(containingSymbol, MethodSymbol)
                        If Not method.IsSub Then
                            Dim returnType As TypeSymbol = method.ReturnType
                            If IdentifierComparison.Equals(method.Name, returnType.Name) Then
                                containingSymbol = returnType
                                GoTo lAgain
                            End If
                        End If
                    End If

                Case Else
                    ' Nothing can be found in context of these symbols

            End Select

            If Not lookupResult.IsGoodOrAmbiguous OrElse Not lookupResult.HasSymbol Then
                lookupResult.Free()
                Return
            End If

            CollectGoodOrAmbiguousFromLookupResult(lookupResult, symbols, preserveAlias)
            lookupResult.Free()
        End Sub

        Private Shared Sub CreateTypeParameterSymbolsAndConstructSymbols(genericName As GenericNameSyntax,
                                                                  symbols As ArrayBuilder(Of Symbol),
                                                                  typeParameters As Dictionary(Of String, CrefTypeParameterSymbol))

            Dim arguments As SeparatedSyntaxList(Of TypeSyntax) = genericName.TypeArgumentList.Arguments
            Dim typeParameterSymbols(arguments.Count - 1) As TypeSymbol

            For i = 0 To arguments.Count - 1
                Dim typeSyntax As TypeSyntax = arguments(i)
                Dim created As CrefTypeParameterSymbol = Nothing

                Select Case typeSyntax.Kind
                    Case SyntaxKind.IdentifierName
                        Dim identifier = DirectCast(typeSyntax, IdentifierNameSyntax)
                        created = New CrefTypeParameterSymbol(i, identifier.Identifier.ValueText, identifier)
                        typeParameterSymbols(i) = created

                    Case Else
                        ' An error case
                        created = New CrefTypeParameterSymbol(i, StringConstants.NamedSymbolErrorName, typeSyntax)
                        typeParameterSymbols(i) = created
                End Select

                typeParameters(created.Name) = created
            Next

            For i = 0 To symbols.Count - 1
                Dim symbol As Symbol = symbols(i)
lAgain:
                Select Case symbol.Kind
                    Case SymbolKind.Method
                        Dim method = DirectCast(symbol, MethodSymbol)
                        Debug.Assert(method.Arity = genericName.TypeArgumentList.Arguments.Count)
                        symbols(i) = method.Construct(typeParameterSymbols.AsImmutableOrNull.As(Of TypeSymbol))

                    Case SymbolKind.NamedType, SymbolKind.ErrorType
                        Dim type = DirectCast(symbol, NamedTypeSymbol)
                        Debug.Assert(type.Arity = genericName.TypeArgumentList.Arguments.Count)
                        symbols(i) = type.Construct(typeParameterSymbols.AsImmutableOrNull.As(Of TypeSymbol))

                    Case SymbolKind.Alias
                        symbol = DirectCast(symbol, AliasSymbol).Target
                        GoTo lAgain
                End Select
            Next
        End Sub

        Private Shared Sub CollectGoodOrAmbiguousFromLookupResult(lookupResult As LookupResult, symbols As ArrayBuilder(Of Symbol), preserveAlias As Boolean)
            Dim di As DiagnosticInfo = lookupResult.Diagnostic

            If TypeOf di Is AmbiguousSymbolDiagnostic Then
                ' Several ambiguous symbols wrapped in 'AmbiguousSymbolDiagnostic', return 
                ' unwrapped symbols in 'ambiguousSymbols' and return Nothing as a result
                Debug.Assert(lookupResult.Kind = LookupResultKind.Ambiguous)

                Dim ambiguousSymbols As ImmutableArray(Of Symbol) = DirectCast(di, AmbiguousSymbolDiagnostic).AmbiguousSymbols
                Debug.Assert(ambiguousSymbols.Length > 1)

                For Each sym In ambiguousSymbols
                    symbols.Add(If(preserveAlias, sym, UnwrapAlias(sym)))
                Next

            Else
                ' Return result as a single good symbol
                For Each sym In lookupResult.Symbols
                    symbols.Add(If(preserveAlias, sym, UnwrapAlias(sym)))
                Next
            End If
        End Sub

        Private Shared Sub CollectOperatorsAndConversionsInType(crefOperator As CrefOperatorReferenceSyntax, argCount As Integer, type As TypeSymbol, symbols As ArrayBuilder(Of Symbol),
                                                         <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol))
            If type Is Nothing Then
                Return
            End If

            If argCount > 2 OrElse argCount < 1 Then
                Return
            End If

            Select Case crefOperator.OperatorToken.Kind
                Case SyntaxKind.IsTrueKeyword
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(UnaryOperatorKind.IsTrue)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.TrueOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.IsFalseKeyword
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(UnaryOperatorKind.IsFalse)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.FalseOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.NotKeyword
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(UnaryOperatorKind.Not)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator,
                                                             WellKnownMemberNames.OnesComplementOperatorName, opInfo,
                                                             useSiteInfo,
                                                             WellKnownMemberNames.LogicalNotOperatorName, opInfo)
                    End If

                Case SyntaxKind.PlusToken
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(UnaryOperatorKind.Plus)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.UnaryPlusOperatorName, opInfo, useSiteInfo)
                    Else
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Add)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.AdditionOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.MinusToken
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(UnaryOperatorKind.Minus)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.UnaryNegationOperatorName, opInfo, useSiteInfo)
                    Else
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Subtract)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.SubtractionOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.AsteriskToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Multiply)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.MultiplyOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.SlashToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Divide)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.DivisionOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.BackslashToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.IntegerDivide)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.IntegerDivisionOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.ModKeyword
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Modulo)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.ModulusOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.CaretToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Power)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.ExponentOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.EqualsToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Equals)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.EqualityOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.LessThanGreaterThanToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.NotEquals)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.InequalityOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.LessThanToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.LessThan)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.LessThanOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.GreaterThanToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.GreaterThan)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.GreaterThanOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.LessThanEqualsToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.LessThanOrEqual)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.LessThanOrEqualOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.GreaterThanEqualsToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.GreaterThanOrEqual)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.GreaterThanOrEqualOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.LikeKeyword
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Like)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.LikeOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.AmpersandToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Concatenate)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.ConcatenateOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.AndKeyword
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.And)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator,
                                                             WellKnownMemberNames.BitwiseAndOperatorName, opInfo,
                                                             useSiteInfo,
                                                             WellKnownMemberNames.LogicalAndOperatorName, opInfo)
                    End If

                Case SyntaxKind.OrKeyword
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Or)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator,
                                                             WellKnownMemberNames.BitwiseOrOperatorName, opInfo,
                                                             useSiteInfo,
                                                             WellKnownMemberNames.LogicalOrOperatorName, opInfo)
                    End If

                Case SyntaxKind.XorKeyword
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.Xor)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator, WellKnownMemberNames.ExclusiveOrOperatorName, opInfo, useSiteInfo)
                    End If

                Case SyntaxKind.LessThanLessThanToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.LeftShift)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator,
                                                             WellKnownMemberNames.LeftShiftOperatorName, opInfo,
                                                             useSiteInfo,
                                                             WellKnownMemberNames.UnsignedLeftShiftOperatorName, opInfo)
                    End If

                Case SyntaxKind.GreaterThanGreaterThanToken
                    If argCount = 2 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.RightShift)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.UserDefinedOperator,
                                                             WellKnownMemberNames.RightShiftOperatorName, opInfo,
                                                             useSiteInfo,
                                                             WellKnownMemberNames.UnsignedRightShiftOperatorName, opInfo)
                    End If

                Case SyntaxKind.CTypeKeyword
                    If argCount = 1 Then
                        Dim opInfo As New OverloadResolution.OperatorInfo(BinaryOperatorKind.RightShift)
                        CollectOperatorsAndConversionsInType(type, symbols, MethodKind.Conversion,
                                                             WellKnownMemberNames.ImplicitConversionName, New OverloadResolution.OperatorInfo(UnaryOperatorKind.Implicit),
                                                             useSiteInfo,
                                                             WellKnownMemberNames.ExplicitConversionName, New OverloadResolution.OperatorInfo(UnaryOperatorKind.Explicit))
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(crefOperator.OperatorToken.Kind)
            End Select
        End Sub

        Private Shared Sub CollectOperatorsAndConversionsInType(type As TypeSymbol,
                                                         symbols As ArrayBuilder(Of Symbol),
                                                         kind As MethodKind,
                                                         name1 As String,
                                                         info1 As OverloadResolution.OperatorInfo,
                                                         <[In], Out> ByRef useSiteInfo As CompoundUseSiteInfo(Of AssemblySymbol),
                                                         Optional name2 As String = Nothing,
                                                         Optional info2 As OverloadResolution.OperatorInfo = Nothing)

            Dim methods = ArrayBuilder(Of MethodSymbol).GetInstance()
            OverloadResolution.CollectUserDefinedOperators(type, Nothing, kind, name1, info1, name2, info2, methods, useSiteInfo)
            symbols.AddRange(methods)
            methods.Free()
        End Sub

        Private Shared Function NameSyntaxHasComplexGenericArguments(name As TypeSyntax) As Boolean
            Select Case name.Kind
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.CrefOperatorReference,
                     SyntaxKind.GlobalName,
                     SyntaxKind.PredefinedType
                    Return False

                Case SyntaxKind.QualifiedCrefOperatorReference
                    Return NameSyntaxHasComplexGenericArguments(DirectCast(name, QualifiedCrefOperatorReferenceSyntax).Left)

                Case SyntaxKind.QualifiedName
                    Dim qualified = DirectCast(name, QualifiedNameSyntax)
                    Return NameSyntaxHasComplexGenericArguments(qualified.Left) OrElse
                           NameSyntaxHasComplexGenericArguments(qualified.Right)

                Case SyntaxKind.GenericName
                    Dim genericArguments = DirectCast(name, GenericNameSyntax).TypeArgumentList.Arguments
                    For i = 0 To genericArguments.Count - 1
                        If genericArguments(i).Kind <> SyntaxKind.IdentifierName Then
                            Return True
                        End If
                    Next
                    Return False

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(name.Kind)
            End Select
        End Function

    End Class

End Namespace

