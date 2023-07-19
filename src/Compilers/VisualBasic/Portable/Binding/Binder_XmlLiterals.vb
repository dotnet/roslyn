' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class Binder
        Private Function BindXmlComment(
                                       syntax As XmlCommentSyntax,
                                       rootInfoOpt As XmlElementRootInfo,
                                       diagnostics As BindingDiagnosticBag) As BoundExpression
            If rootInfoOpt Is Nothing Then
                diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)
            End If

            Dim str = CreateStringLiteral(syntax, GetXmlString(syntax.TextTokens), compilerGenerated:=True, diagnostics:=diagnostics)
            Dim objectCreation = BindObjectCreationExpression(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XComment, syntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(str),
                diagnostics)
            Return New BoundXmlComment(syntax, str, objectCreation, objectCreation.Type)
        End Function

        Private Function BindXmlDocument(syntax As XmlDocumentSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)

            Dim declaration = BindXmlDeclaration(syntax.Declaration, diagnostics)

            ' Match the native compiler and invoke the XDocument(XDeclaration, Params Object())
            ' .ctor, with Nothing for the Params array argument.
            Dim objectCreation = BindObjectCreationExpression(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XDocument, syntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(declaration, New BoundLiteral(syntax, ConstantValue.Nothing, Nothing)),
                diagnostics)

            Dim childNodeBuilder = ArrayBuilder(Of BoundExpression).GetInstance()

            BindXmlContent(syntax.PrecedingMisc, childNodeBuilder, rootInfoOpt:=Nothing, diagnostics:=diagnostics)
            childNodeBuilder.Add(BindXmlContent(syntax.Root, rootInfoOpt:=Nothing, diagnostics:=diagnostics))
            BindXmlContent(syntax.FollowingMisc, childNodeBuilder, rootInfoOpt:=Nothing, diagnostics:=diagnostics)

            Dim childNodes = childNodeBuilder.ToImmutableAndFree()
            Dim rewriterInfo = BindXmlContainerRewriterInfo(syntax, objectCreation, childNodes, rootInfoOpt:=Nothing, diagnostics:=diagnostics)
            Return New BoundXmlDocument(syntax, declaration, childNodes, rewriterInfo, objectCreation.Type, rewriterInfo.HasErrors)
        End Function

        Private Function BindXmlDeclaration(syntax As XmlDeclarationSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim version = BindXmlDeclarationOption(syntax, syntax.Version, diagnostics)
            Dim encoding = BindXmlDeclarationOption(syntax, syntax.Encoding, diagnostics)
            Dim standalone = BindXmlDeclarationOption(syntax, syntax.Standalone, diagnostics)
            Dim objectCreation = BindObjectCreationExpression(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XDeclaration, syntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(version, encoding, standalone),
                diagnostics)
            Return New BoundXmlDeclaration(syntax, version, encoding, standalone, objectCreation, objectCreation.Type)
        End Function

        Private Function BindXmlDeclarationOption(syntax As XmlDeclarationSyntax, optionSyntax As XmlDeclarationOptionSyntax, diagnostics As BindingDiagnosticBag) As BoundLiteral
            If optionSyntax Is Nothing Then
                Return CreateStringLiteral(syntax, Nothing, compilerGenerated:=True, diagnostics:=diagnostics)
            Else
                Dim value = optionSyntax.Value
                Return CreateStringLiteral(value, GetXmlString(value.TextTokens), compilerGenerated:=False, diagnostics:=diagnostics)
            End If
        End Function

        Private Function BindXmlProcessingInstruction(syntax As XmlProcessingInstructionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim target = CreateStringLiteral(syntax, GetXmlName(syntax.Name), compilerGenerated:=True, diagnostics:=diagnostics)
            Dim data = CreateStringLiteral(syntax, GetXmlString(syntax.TextTokens), compilerGenerated:=True, diagnostics:=diagnostics)
            Dim objectCreation = BindObjectCreationExpression(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XProcessingInstruction, syntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(target, data),
                diagnostics)
            Return New BoundXmlProcessingInstruction(syntax, target, data, objectCreation, objectCreation.Type)
        End Function

        Private Function BindXmlEmptyElement(
                                            syntax As XmlEmptyElementSyntax,
                                            rootInfoOpt As XmlElementRootInfo,
                                            diagnostics As BindingDiagnosticBag) As BoundExpression
            Return BindXmlElement(syntax, syntax.Name, syntax.Attributes, Nothing, rootInfoOpt, diagnostics)
        End Function

        Private Function BindXmlElement(
                                       syntax As XmlElementSyntax,
                                       rootInfoOpt As XmlElementRootInfo,
                                       diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim startTag = syntax.StartTag
            Return BindXmlElement(syntax, startTag.Name, startTag.Attributes, syntax.Content, rootInfoOpt, diagnostics)
        End Function

        Private Function BindXmlElement(
                                       syntax As XmlNodeSyntax,
                                       nameSyntax As XmlNodeSyntax,
                                       attributes As SyntaxList(Of XmlNodeSyntax),
                                       content As SyntaxList(Of XmlNodeSyntax),
                                       rootInfoOpt As XmlElementRootInfo,
                                       diagnostics As BindingDiagnosticBag) As BoundExpression
            If rootInfoOpt Is Nothing Then
                diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)

                ' 'importedNamespaces' is the set of Imports statements that are referenced
                ' within the XmlElement, represented as { prefix, namespace } pairs. The set is
                ' used to ensure the necessary xmlns attributes are added to the resulting XML at
                ' runtime. The set is represented as a flat list rather than a dictionary so that the
                ' order of the xmlns attributes matches the XML generated by the native compiler.
                Dim importedNamespaces = ArrayBuilder(Of KeyValuePair(Of String, String)).GetInstance()
                Dim binder = New XmlRootElementBinder(Me)
                Dim result = binder.BindXmlElement(syntax, nameSyntax, attributes, content, New XmlElementRootInfo(Me, syntax, importedNamespaces), diagnostics)
                importedNamespaces.Free()

                Return result
            Else
                Dim allAttributes As Dictionary(Of XmlName, BoundXmlAttribute) = Nothing
                Dim xmlnsAttributes = ArrayBuilder(Of BoundXmlAttribute).GetInstance()
                Dim otherAttributes = ArrayBuilder(Of XmlNodeSyntax).GetInstance()
                Dim namespaces = BindXmlnsAttributes(attributes, allAttributes, xmlnsAttributes, otherAttributes, rootInfoOpt.ImportedNamespaces, diagnostics)
                Debug.Assert((namespaces Is Nothing) OrElse (namespaces.Count > 0))

                Dim binder = If(namespaces Is Nothing, Me, New XmlElementBinder(Me, namespaces))
                Dim result = binder.BindXmlElementWithoutAddingNamespaces(syntax, nameSyntax, allAttributes, xmlnsAttributes, otherAttributes, content, rootInfoOpt, diagnostics)

                otherAttributes.Free()
                xmlnsAttributes.Free()

                Return result
            End If
        End Function

        Private Function BindXmlElementWithoutAddingNamespaces(
                                                              syntax As XmlNodeSyntax,
                                                              nameSyntax As XmlNodeSyntax,
                                                              <Out()> ByRef allAttributes As Dictionary(Of XmlName, BoundXmlAttribute),
                                                              xmlnsAttributes As ArrayBuilder(Of BoundXmlAttribute),
                                                              otherAttributes As ArrayBuilder(Of XmlNodeSyntax),
                                                              content As SyntaxList(Of XmlNodeSyntax),
                                                              rootInfo As XmlElementRootInfo,
                                                              diagnostics As BindingDiagnosticBag) As BoundExpression
            ' Any expression type is allowed as long as there is an appropriate XElement
            ' constructor for that argument type. In particular, this allows expressions of type
            ' XElement since XElement includes New(other As XElement). This is consistent with
            ' the native compiler, but contradicts the spec (11.23.4: "... the embedded expression
            ' must be a value of a type implicitly convertible to System.Xml.Linq.XName").
            Dim argument As BoundExpression
            If nameSyntax.Kind = SyntaxKind.XmlEmbeddedExpression Then
                argument = BindXmlEmbeddedExpression(DirectCast(nameSyntax, XmlEmbeddedExpressionSyntax), diagnostics:=diagnostics)
            Else
                Dim fromImports = False
                Dim prefix As String = Nothing
                Dim localName As String = Nothing
                Dim [namespace] As String = Nothing
                argument = BindXmlName(DirectCast(nameSyntax, XmlNameSyntax), forElement:=True, rootInfoOpt:=rootInfo, fromImports:=fromImports, prefix:=prefix, localName:=localName, [namespace]:=[namespace], diagnostics:=diagnostics)

                If fromImports Then
                    AddImportedNamespaceIfNecessary(rootInfo.ImportedNamespaces, prefix, [namespace], forElement:=True)
                End If
            End If

            ' Expression* .Semantics::InterpretXmlName( [ ParseTree::Expression* Expr ] [ BCSym.[Alias]** ResolvedPrefix ] )
            ' . . . 
            'If NameExpr.ResultType Is GetFXSymbolProvider().GetObjectType() AndAlso Not m_UsingOptionTypeStrict Then
            '    NameExpr = AllocateExpression( _
            '        BILOP.SX_DIRECTCAST, _
            '        m_XmlSymbols.GetXName(), _
            '        NameExpr, _
            '        NameExpr.Loc)
            'End If

            If argument.Type.IsObjectType AndAlso OptionStrict <> VisualBasic.OptionStrict.On Then
                Dim xnameType = GetWellKnownType(WellKnownType.System_Xml_Linq_XName, syntax, diagnostics)
                argument = ApplyDirectCastConversion(syntax, argument, xnameType, diagnostics:=diagnostics)
            End If

            Dim objectCreation = BindObjectCreationExpression(
                nameSyntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XElement, nameSyntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(argument),
                diagnostics)

            Dim childNodeBuilder = ArrayBuilder(Of BoundExpression).GetInstance()

            For Each xmlnsAttribute In xmlnsAttributes
                childNodeBuilder.Add(xmlnsAttribute)
            Next

            Debug.Assert(otherAttributes.All(Function(a) (a.Kind = SyntaxKind.XmlAttribute) OrElse (a.Kind = SyntaxKind.XmlEmbeddedExpression)))
            BindXmlAttributes(allAttributes, otherAttributes, childNodeBuilder, rootInfo, diagnostics)

            If syntax.Kind <> SyntaxKind.XmlEmptyElement Then
                If content.Count > 0 Then
                    BindXmlContent(content, childNodeBuilder, rootInfo, diagnostics)
                Else
                    ' An XElement with a start and end tag but no content. Include a compiler-
                    ' generated empty string as content for consistency with the native compiler.
                    ' (This also ensures <x></x> is serialized as <x></x> rather than <x/>.)
                    childNodeBuilder.Add(CreateStringLiteral(syntax, String.Empty, compilerGenerated:=True, diagnostics:=diagnostics))
                End If
            End If

            Dim childNodes = childNodeBuilder.ToImmutableAndFree()
            Dim rewriterInfo = BindXmlContainerRewriterInfo(syntax, objectCreation, childNodes, rootInfo, diagnostics)
            Return New BoundXmlElement(syntax, argument, childNodes, rewriterInfo, objectCreation.Type, rewriterInfo.HasErrors)
        End Function

        Private Function BindXmlContainerRewriterInfo(
                                                     syntax As XmlNodeSyntax,
                                                     objectCreation As BoundExpression,
                                                     childNodes As ImmutableArray(Of BoundExpression),
                                                     rootInfoOpt As XmlElementRootInfo,
                                                     diagnostics As BindingDiagnosticBag) As BoundXmlContainerRewriterInfo
            If (childNodes.Length = 0) AndAlso
                ((rootInfoOpt Is Nothing) OrElse (rootInfoOpt.ImportedNamespaces.Count = 0)) Then
                Return New BoundXmlContainerRewriterInfo(objectCreation)
            End If

            Dim placeholder = (New BoundRValuePlaceholder(syntax, objectCreation.Type)).MakeCompilerGenerated()
            Dim sideEffectBuilder = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim addGroup = GetXmlMethodOrPropertyGroup(syntax,
                                                        GetWellKnownType(WellKnownType.System_Xml_Linq_XContainer, syntax, diagnostics),
                                                        StringConstants.XmlAddMethodName,
                                                        placeholder,
                                                        diagnostics)
            ' The following are arguments to
            ' InternalXmlHelper.RemoveNamespaceAttributes(prefixes As String(),
            '     namespaces As XNamespace(), attributes As List(Of XAttribute), arg As XElement) As XElement
            Dim inScopeXmlNamespaces As ImmutableArray(Of KeyValuePair(Of String, String)) = Nothing
            Dim prefixesPlaceholder As BoundRValuePlaceholder = Nothing
            Dim namespacesPlaceholder As BoundRValuePlaceholder = Nothing

            For Each childNode In childNodes
                ' Skip attributes that match imports since those are redundant.
                If (childNode.Kind = BoundKind.XmlAttribute) AndAlso DirectCast(childNode, BoundXmlAttribute).MatchesImport Then
                    Continue For
                End If

                ' Any namespaces from Imports <xmlns:p="..."> that were used within an
                ' XElement require xmlns:p="..." attributes to be added to the root of the XML.
                ' (The xmlns declarations are added to the root to generate the simplest XML.)
                ' If the XML contains embedded expressions, those embedded expressions may
                ' also reference namespaces. For instance, the XML generated at runtime for
                ' <x <%= <p:y/> %>/> should be <x xmlns:p="..."><p:y/></x>. In general, these
                ' cases of embedded expressions are handled by adding xmlns attributes
                ' to the root of the XML within the embedded expression, then using methods
                ' at runtime (when stitching together the XML) to move any attributes to the
                ' containing XML. For instance, for <x <%= e %>/> we generate the following code:
                '
                '   Dim prefixes As New String() From { ... } ' prefixes in use at 'x'
                '   Dim namespaces As New XNamespace() From { ... } ' namespaces in use at 'x'
                '   Dim attribs As New List(Of XAttribute) ' attributes on 'x' plus any removed from 'e'
                '   e = InternalXmlHelper.RemoveNamespaceAttributes(prefixes, namespaces, attribs, e)
                '   x.Add(attribs)
                '   x.Add(e)
                '
                ' In a handful of cases where the namespaces on the embedded expression can
                ' be determined at compile time, the native compiler avoids the cost of calling
                ' RemoveNamespaceAttributes by moving namespaces at compile time. (See
                ' XmlSemantics::TraceLinqExpression.) That is an optimization only and for simplicity
                ' is not included. (If we do add static analysis, we'll need to handle cases where
                ' embedded expressions contain both namespaces that can be determined at compile
                ' time and namespaces that may be included at runtime. For instance, the expression
                ' <p:y> in <x <%= <p:y><%= e %></p:y> %>/> uses "p" but may also use namespaces
                ' in <%= e %>.)

                Dim expr = childNode
                Debug.Assert(expr.Type IsNot Nothing)

                ' If rootInfoOpt Is Nothing, we're binding an XDocument, not an XElement,
                ' in which case it's not possible to remove embedded xmlns attributes.
                If (rootInfoOpt IsNot Nothing) AndAlso
                    (expr.Kind = BoundKind.XmlEmbeddedExpression) AndAlso
                    HasImportedXmlNamespaces AndAlso
                    Not expr.Type.IsIntrinsicOrEnumType() Then

                    If inScopeXmlNamespaces.IsDefault Then
                        Dim builder = ArrayBuilder(Of KeyValuePair(Of String, String)).GetInstance()
                        GetInScopeXmlNamespaces(builder)
                        inScopeXmlNamespaces = builder.ToImmutableAndFree()
                    End If

                    If prefixesPlaceholder Is Nothing Then
                        Dim prefixesType = CreateArrayType(GetSpecialType(SpecialType.System_String, syntax, diagnostics))
                        prefixesPlaceholder = (New BoundRValuePlaceholder(syntax, prefixesType)).MakeCompilerGenerated()

                        Dim namespacesType = CreateArrayType(GetWellKnownType(WellKnownType.System_Xml_Linq_XNamespace, syntax, diagnostics))
                        namespacesPlaceholder = (New BoundRValuePlaceholder(syntax, namespacesType)).MakeCompilerGenerated()
                    End If

                    ' Generate update to child using RemoveNamespaceAttributes.
                    expr = rootInfoOpt.BindRemoveNamespaceAttributesInvocation(expr, prefixesPlaceholder, namespacesPlaceholder, diagnostics)
                End If

                sideEffectBuilder.Add(BindInvocationExpressionIfGroupNotNothing(syntax, addGroup, ImmutableArray.Create(Of BoundExpression)(expr), diagnostics))
            Next

            Dim xmlnsAttributesPlaceholder As BoundRValuePlaceholder = Nothing
            Dim xmlnsAttributes As BoundExpression = Nothing

            ' At the root element, add any xmlns attributes required from Imports,
            ' and any xmlns attributes removed from embedded expressions.
            Dim isRoot = (rootInfoOpt IsNot Nothing) AndAlso (rootInfoOpt.Syntax Is syntax)
            If isRoot Then
                ' Imports declarations are added in the reverse order that the
                ' prefixes were discovered, to match the native compiler.
                Dim importedNamespaces = rootInfoOpt.ImportedNamespaces
                For i = importedNamespaces.Count - 1 To 0 Step -1
                    Dim pair = importedNamespaces(i)
                    Dim attribute = BindXmlnsAttribute(syntax, pair.Key, pair.Value, diagnostics)
                    sideEffectBuilder.Add(BindInvocationExpressionIfGroupNotNothing(syntax, addGroup, ImmutableArray.Create(Of BoundExpression)(attribute), diagnostics))
                Next

                ' Add any xmlns attributes from embedded expressions. (If there were
                ' any embedded expressions, XmlnsAttributesPlaceholder will be set.)
                xmlnsAttributesPlaceholder = rootInfoOpt.XmlnsAttributesPlaceholder
                If xmlnsAttributesPlaceholder IsNot Nothing Then
                    xmlnsAttributes = BindObjectCreationExpression(syntax, xmlnsAttributesPlaceholder.Type, ImmutableArray(Of BoundExpression).Empty, diagnostics).MakeCompilerGenerated()
                    sideEffectBuilder.Add(BindInvocationExpressionIfGroupNotNothing(syntax, addGroup, ImmutableArray.Create(Of BoundExpression)(xmlnsAttributesPlaceholder), diagnostics))
                End If
            End If

            Return New BoundXmlContainerRewriterInfo(
                isRoot,
                placeholder,
                objectCreation,
                xmlnsAttributesPlaceholder:=xmlnsAttributesPlaceholder,
                xmlnsAttributes:=xmlnsAttributes,
                prefixesPlaceholder:=prefixesPlaceholder,
                namespacesPlaceholder:=namespacesPlaceholder,
                importedNamespaces:=If(isRoot, rootInfoOpt.ImportedNamespaces.ToImmutable(), Nothing),
                inScopeXmlNamespaces:=inScopeXmlNamespaces,
                sideEffects:=sideEffectBuilder.ToImmutableAndFree())
        End Function

        Private Function BindRemoveNamespaceAttributesInvocation(
                                                                syntax As VisualBasicSyntaxNode,
                                                                expr As BoundExpression,
                                                                prefixesPlaceholder As BoundRValuePlaceholder,
                                                                namespacesPlaceholder As BoundRValuePlaceholder,
                                                                <Out()> ByRef xmlnsAttributesPlaceholder As BoundRValuePlaceholder,
                                                                <Out()> ByRef removeNamespacesGroup As BoundMethodOrPropertyGroup,
                                                                diagnostics As BindingDiagnosticBag) As BoundExpression

            If xmlnsAttributesPlaceholder Is Nothing Then
                Dim listType = GetWellKnownType(WellKnownType.System_Collections_Generic_List_T, syntax, diagnostics).Construct(
                    GetWellKnownType(WellKnownType.System_Xml_Linq_XAttribute, syntax, diagnostics))
                xmlnsAttributesPlaceholder = (New BoundRValuePlaceholder(syntax, listType)).MakeCompilerGenerated()

                ' Generate method group and arguments for RemoveNamespaceAttributes.
                removeNamespacesGroup = GetXmlMethodOrPropertyGroup(syntax,
                                                                    GetInternalXmlHelperType(syntax, diagnostics),
                                                                    StringConstants.XmlRemoveNamespaceAttributesMethodName,
                                                                    Nothing,
                                                                    diagnostics)
            End If

            ' Invoke RemoveNamespaceAttributes.
            Return BindInvocationExpressionIfGroupNotNothing(expr.Syntax,
                                                             removeNamespacesGroup,
                                                             ImmutableArray.Create(Of BoundExpression)(prefixesPlaceholder, namespacesPlaceholder, xmlnsAttributesPlaceholder, expr),
                                                             diagnostics).MakeCompilerGenerated()
        End Function

        Private Function CreateArrayType(elementType As TypeSymbol) As ArrayTypeSymbol
            Return ArrayTypeSymbol.CreateSZArray(elementType, ImmutableArray(Of CustomModifier).Empty, compilation:=Compilation)
        End Function

        Private Shared Function GetXmlnsXmlName(prefix As String) As XmlName
            Dim localName = If(String.IsNullOrEmpty(prefix), StringConstants.XmlnsPrefix, prefix)
            Dim [namespace] = If(String.IsNullOrEmpty(prefix), StringConstants.DefaultXmlNamespace, StringConstants.XmlnsNamespace)
            Return New XmlName(localName, [namespace])
        End Function

        Private Function BindXmlnsAttribute(
                                           syntax As XmlNodeSyntax,
                                           prefix As String,
                                           namespaceName As String,
                                           diagnostics As BindingDiagnosticBag) As BoundXmlAttribute
            Dim name = BindXmlnsName(syntax, prefix, compilerGenerated:=True, diagnostics:=diagnostics)
            Dim [namespace] = BindXmlNamespace(syntax,
                                               CreateStringLiteral(syntax, namespaceName, compilerGenerated:=True, diagnostics:=diagnostics),
                                               diagnostics)
            Return BindXmlnsAttribute(syntax, name, [namespace], useConstructor:=False, matchesImport:=False, compilerGenerated:=True, hasErrors:=False, diagnostics:=diagnostics)
        End Function

        Private Function BindXmlnsName(
                                      syntax As XmlNodeSyntax,
                                      prefix As String,
                                      compilerGenerated As Boolean,
                                      diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim name = GetXmlnsXmlName(prefix)
            Return BindXmlName(syntax,
                                   CreateStringLiteral(syntax,
                                                       name.LocalName,
                                                       compilerGenerated,
                                                       diagnostics),
                                   CreateStringLiteral(syntax,
                                                       name.XmlNamespace,
                                                       compilerGenerated,
                                                       diagnostics),
                                   diagnostics)
        End Function

        Private Function BindXmlnsAttribute(
                                           syntax As XmlNodeSyntax,
                                           prefix As BoundExpression,
                                           [namespace] As BoundExpression,
                                           useConstructor As Boolean,
                                           matchesImport As Boolean,
                                           compilerGenerated As Boolean,
                                           hasErrors As Boolean,
                                           diagnostics As BindingDiagnosticBag) As BoundXmlAttribute
            Dim objectCreation As BoundExpression
            If useConstructor Then
                objectCreation = BindObjectCreationExpression(syntax,
                                                              GetWellKnownType(WellKnownType.System_Xml_Linq_XAttribute, syntax, diagnostics),
                                                              ImmutableArray.Create(Of BoundExpression)(prefix, [namespace]),
                                                              diagnostics)
            Else
                Dim type = GetInternalXmlHelperType(syntax, diagnostics)
                Dim group = GetXmlMethodOrPropertyGroup(syntax, type, StringConstants.XmlCreateNamespaceAttributeMethodName, Nothing, diagnostics)
                objectCreation = BindInvocationExpressionIfGroupNotNothing(syntax, group, ImmutableArray.Create(Of BoundExpression)(prefix, [namespace]), diagnostics)
            End If

            Dim result = New BoundXmlAttribute(syntax, prefix, [namespace], matchesImport, objectCreation, objectCreation.Type, hasErrors)
            If compilerGenerated Then
                result.SetWasCompilerGenerated()
            End If
            Return result
        End Function

        Private Function BindXmlAttribute(
                                         syntax As XmlAttributeSyntax,
                                         rootInfo As XmlElementRootInfo,
                                         <Out()> ByRef xmlName As XmlName,
                                         diagnostics As BindingDiagnosticBag) As BoundXmlAttribute
            Dim nameSyntax = syntax.Name
            Dim name As BoundExpression

            If nameSyntax.Kind = SyntaxKind.XmlEmbeddedExpression Then
                name = BindXmlEmbeddedExpression(DirectCast(nameSyntax, XmlEmbeddedExpressionSyntax), diagnostics:=diagnostics)
                xmlName = Nothing
            Else
                Dim fromImports = False
                Dim prefix As String = Nothing
                Dim localName As String = Nothing
                Dim [namespace] As String = Nothing
                name = BindXmlName(
                    DirectCast(nameSyntax, XmlNameSyntax),
                    forElement:=False,
                    rootInfoOpt:=rootInfo,
                    fromImports:=fromImports,
                    prefix:=prefix,
                    localName:=localName,
                    [namespace]:=[namespace],
                    diagnostics:=diagnostics)

                If fromImports Then
                    AddImportedNamespaceIfNecessary(rootInfo.ImportedNamespaces, prefix, [namespace], forElement:=False)
                End If

                xmlName = New XmlName(localName, [namespace])
            End If

            Dim value As BoundExpression
            Dim objectCreation As BoundExpression
            Dim valueSyntax = syntax.Value
            Dim matchesImport As Boolean = False

            If valueSyntax.Kind = SyntaxKind.XmlEmbeddedExpression Then
                ' Use InternalXmlHelper.CreateAttribute rather than 'New XAttribute()' for attributes
                ' with embedded expression values since CreateAttribute handles Nothing values.
                value = BindXmlEmbeddedExpression(DirectCast(valueSyntax, XmlEmbeddedExpressionSyntax), diagnostics)
                Dim group = GetXmlMethodOrPropertyGroup(valueSyntax,
                                                        GetInternalXmlHelperType(syntax, diagnostics),
                                                        StringConstants.XmlCreateAttributeMethodName,
                                                        Nothing,
                                                        diagnostics)
                objectCreation = BindInvocationExpressionIfGroupNotNothing(valueSyntax,
                                                                           group,
                                                                           ImmutableArray.Create(Of BoundExpression)(name, value),
                                                                           diagnostics)
            Else
                Dim str = GetXmlString(DirectCast(valueSyntax, XmlStringSyntax).TextTokens)

                ' Mark if this attribute is an xmlns declaration that matches an Imports,
                ' since xmlns declarations from Imports will be added directly at the
                ' XmlElement root, and this attribute can be dropped then.
                matchesImport = (nameSyntax.Kind = SyntaxKind.XmlName) AndAlso MatchesXmlnsImport(DirectCast(nameSyntax, XmlNameSyntax), str)

                value = CreateStringLiteral(valueSyntax, str, compilerGenerated:=False, diagnostics:=diagnostics)
                objectCreation = BindObjectCreationExpression(nameSyntax,
                                                              GetWellKnownType(WellKnownType.System_Xml_Linq_XAttribute, syntax, diagnostics),
                                                              ImmutableArray.Create(Of BoundExpression)(name, value),
                                                              diagnostics)
            End If

            Return New BoundXmlAttribute(syntax, name, value, matchesImport, objectCreation, objectCreation.Type)
        End Function

        ''' <summary>
        ''' Returns True if the xmlns { prefix, namespace } pair matches
        ''' an Imports declaration and there aren't any xmlns declarations
        ''' for the same prefix on any outer XElement scopes.
        ''' </summary>
        Private Function MatchesXmlnsImport(prefix As String, [namespace] As String) As Boolean
            Dim fromImports As Boolean = False
            Dim otherNamespace As String = Nothing
            Return LookupXmlNamespace(prefix, False, otherNamespace, fromImports) AndAlso
                fromImports AndAlso
                ([namespace] = otherNamespace)
        End Function

        Private Function MatchesXmlnsImport(name As XmlNameSyntax, value As String) As Boolean
            Dim prefix As String = Nothing
            TryGetXmlnsPrefix(name, prefix, BindingDiagnosticBag.Discarded)

            If prefix Is Nothing Then
                Return False
            End If

            ' Match using containing Binder since this Binder
            ' contains this xmlns attribute in XmlNamespaces.
            Return ContainingBinder.MatchesXmlnsImport(prefix, value)
        End Function

        ''' <summary>
        ''' Bind the expression within the XmlEmbeddedExpressionSyntax,
        ''' and wrap in a BoundXmlEmbeddedExpression.
        ''' </summary>
        Private Function BindXmlEmbeddedExpression(syntax As XmlEmbeddedExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim binder = New XmlEmbeddedExpressionBinder(Me)
            Dim expr = binder.BindRValue(syntax.Expression, diagnostics)
            Debug.Assert(expr IsNot Nothing)
            Debug.Assert(expr.Type IsNot Nothing)
            Return New BoundXmlEmbeddedExpression(syntax, expr, expr.Type)
        End Function

        Private Sub BindXmlAttributes(
                                      <Out()> ByRef allAttributes As Dictionary(Of XmlName, BoundXmlAttribute),
                                     attributes As ArrayBuilder(Of XmlNodeSyntax),
                                     childNodeBuilder As ArrayBuilder(Of BoundExpression),
                                     rootInfo As XmlElementRootInfo,
                                     diagnostics As BindingDiagnosticBag)
            For Each childSyntax In attributes
                If childSyntax.Kind = SyntaxKind.XmlAttribute Then
                    Dim attributeSyntax = DirectCast(childSyntax, XmlAttributeSyntax)
                    Dim name As XmlName = Nothing
                    Dim attribute = BindXmlAttribute(attributeSyntax, rootInfo, name, diagnostics)
                    childNodeBuilder.Add(attribute)

                    ' Name may be Nothing for embedded expressions.
                    ' Otherwise, check for duplicates.
                    If name.LocalName IsNot Nothing Then
                        AddXmlAttributeIfNotDuplicate(attributeSyntax.Name, name, attribute, allAttributes, diagnostics)
                    End If
                Else
                    Dim child = BindXmlContent(childSyntax, rootInfo, diagnostics)
                    childNodeBuilder.Add(child)
                End If
            Next
        End Sub

        Private Structure XmlName
            Public Sub New(localName As String, [namespace] As String)
                Me.LocalName = localName
                Me.XmlNamespace = [namespace]
            End Sub

            Public ReadOnly LocalName As String
            Public ReadOnly XmlNamespace As String
        End Structure

        Private NotInheritable Class XmlNameComparer
            Implements IEqualityComparer(Of XmlName)

            Public Shared ReadOnly Instance As New XmlNameComparer()

            Private Function IEqualityComparer_Equals(x As XmlName, y As XmlName) As Boolean Implements IEqualityComparer(Of XmlName).Equals
                Return String.Equals(x.LocalName, y.LocalName, StringComparison.Ordinal) AndAlso String.Equals(x.XmlNamespace, y.XmlNamespace, StringComparison.Ordinal)
            End Function

            Private Function IEqualityComparer_GetHashCode(obj As XmlName) As Integer Implements IEqualityComparer(Of XmlName).GetHashCode
                Dim result = obj.LocalName.GetHashCode()
                If obj.XmlNamespace IsNot Nothing Then
                    result = Hash.Combine(result, obj.XmlNamespace.GetHashCode())
                End If
                Return result
            End Function
        End Class

        Private Sub BindXmlContent(content As SyntaxList(Of XmlNodeSyntax), childNodeBuilder As ArrayBuilder(Of BoundExpression), rootInfoOpt As XmlElementRootInfo, diagnostics As BindingDiagnosticBag)
            For Each childSyntax In content
                childNodeBuilder.Add(BindXmlContent(childSyntax, rootInfoOpt, diagnostics))
            Next
        End Sub

        Private Function BindXmlContent(syntax As XmlNodeSyntax, rootInfoOpt As XmlElementRootInfo, diagnostics As BindingDiagnosticBag) As BoundExpression
            Select Case syntax.Kind
                Case SyntaxKind.XmlProcessingInstruction
                    Return BindXmlProcessingInstruction(DirectCast(syntax, XmlProcessingInstructionSyntax), diagnostics)

                Case SyntaxKind.XmlComment
                    Return BindXmlComment(DirectCast(syntax, XmlCommentSyntax), rootInfoOpt, diagnostics)

                Case SyntaxKind.XmlElement
                    Return BindXmlElement(DirectCast(syntax, XmlElementSyntax), rootInfoOpt, diagnostics)

                Case SyntaxKind.XmlEmptyElement
                    Return BindXmlEmptyElement(DirectCast(syntax, XmlEmptyElementSyntax), rootInfoOpt, diagnostics)

                Case SyntaxKind.XmlEmbeddedExpression
                    Return BindXmlEmbeddedExpression(DirectCast(syntax, XmlEmbeddedExpressionSyntax), diagnostics)

                Case SyntaxKind.XmlCDataSection
                    Return BindXmlCData(DirectCast(syntax, XmlCDataSectionSyntax), rootInfoOpt, diagnostics)

                Case SyntaxKind.XmlText
                    Return BindXmlText(DirectCast(syntax, XmlTextSyntax), diagnostics)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(syntax.Kind)
            End Select
        End Function

        Private Function BindXmlAttributeAccess(syntax As XmlMemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)

            Dim receiver = BindXmlMemberAccessReceiver(syntax, diagnostics)
            Dim nameSyntax = If(syntax.Name.Kind = SyntaxKind.XmlName,
                                DirectCast(syntax.Name, XmlNameSyntax),
                                DirectCast(syntax.Name, XmlBracketedNameSyntax).Name)
            Dim name = BindXmlName(nameSyntax, forElement:=False, diagnostics:=diagnostics)
            Dim receiverType = receiver.Type
            Debug.Assert(receiverType IsNot Nothing)
            Dim memberAccess As BoundExpression = Nothing

            If receiverType.SpecialType = SpecialType.System_Object Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_NoXmlAxesLateBinding)
            ElseIf Not receiverType.IsErrorType() Then
                Dim group As BoundMethodOrPropertyGroup = Nothing
                ' Determine the appropriate overload, allowing
                ' XElement or IEnumerable(Of XElement) argument.
                Dim xmlType = GetWellKnownType(WellKnownType.System_Xml_Linq_XElement, syntax, diagnostics)
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
                If receiverType.IsOrDerivedFrom(xmlType, useSiteInfo) OrElse receiverType.IsCompatibleWithGenericIEnumerableOfType(xmlType, useSiteInfo) Then
                    group = GetXmlMethodOrPropertyGroup(syntax,
                                                            GetInternalXmlHelperType(syntax, diagnostics),
                                                            StringConstants.XmlAttributeValueMethodName,
                                                            Nothing,
                                                            diagnostics)
                End If

                diagnostics.Add(syntax, useSiteInfo)

                If group IsNot Nothing Then
                    memberAccess = BindInvocationExpressionIfGroupNotNothing(syntax, group, ImmutableArray.Create(Of BoundExpression)(receiver, name), diagnostics)
                    memberAccess = MakeValue(memberAccess, diagnostics)
                Else
                    ReportDiagnostic(diagnostics, syntax, ERRID.ERR_TypeDisallowsAttributes, receiverType)
                End If
            End If

            If memberAccess Is Nothing Then
                memberAccess = BadExpression(syntax, ImmutableArray.Create(receiver, name), Compilation.GetSpecialType(SpecialType.System_String))
            End If

            Return New BoundXmlMemberAccess(syntax, memberAccess, memberAccess.Type)
        End Function

        Private Function BindXmlElementAccess(syntax As XmlMemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Return BindXmlElementAccess(syntax, StringConstants.XmlElementsMethodName, ERRID.ERR_TypeDisallowsElements, diagnostics)
        End Function

        Private Function BindXmlDescendantAccess(syntax As XmlMemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            Return BindXmlElementAccess(syntax, StringConstants.XmlDescendantsMethodName, ERRID.ERR_TypeDisallowsDescendants, diagnostics)
        End Function

        Private Function BindXmlElementAccess(syntax As XmlMemberAccessExpressionSyntax, memberName As String, typeDisallowsError As ERRID, diagnostics As BindingDiagnosticBag) As BoundExpression
            diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)

            Dim receiver = BindXmlMemberAccessReceiver(syntax, diagnostics)
            Dim name = BindXmlName(DirectCast(syntax.Name, XmlBracketedNameSyntax).Name, forElement:=True, diagnostics:=diagnostics)
            Dim receiverType = receiver.Type
            Debug.Assert(receiverType IsNot Nothing)
            Dim memberAccess As BoundExpression = Nothing

            If receiverType.SpecialType = SpecialType.System_Object Then
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_NoXmlAxesLateBinding)
            ElseIf Not receiverType.IsErrorType() Then
                Dim group As BoundMethodOrPropertyGroup = Nothing
                Dim arguments As ImmutableArray(Of BoundExpression) = Nothing
                ' Determine the appropriate overload, allowing
                ' XContainer or IEnumerable(Of XContainer) argument.
                Dim xmlType = GetWellKnownType(WellKnownType.System_Xml_Linq_XContainer, syntax, diagnostics)
                Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)

                If receiverType.IsOrDerivedFrom(xmlType, useSiteInfo) Then
                    group = GetXmlMethodOrPropertyGroup(syntax,
                                                            xmlType,
                                                            memberName,
                                                            receiver,
                                                            diagnostics)
                    arguments = ImmutableArray.Create(Of BoundExpression)(name)
                ElseIf receiverType.IsCompatibleWithGenericIEnumerableOfType(xmlType, useSiteInfo) Then
                    group = GetXmlMethodOrPropertyGroup(syntax,
                                                            GetWellKnownType(WellKnownType.System_Xml_Linq_Extensions, syntax, diagnostics),
                                                            memberName,
                                                            Nothing,
                                                            diagnostics)
                    arguments = ImmutableArray.Create(Of BoundExpression)(receiver, name)
                End If

                diagnostics.Add(syntax, useSiteInfo)

                If group IsNot Nothing Then
                    memberAccess = BindInvocationExpressionIfGroupNotNothing(syntax, group, arguments, diagnostics)
                    memberAccess = MakeRValue(memberAccess, diagnostics)
                Else
                    ReportDiagnostic(diagnostics, syntax, typeDisallowsError, receiverType)
                End If
            End If

            If memberAccess Is Nothing Then
                memberAccess = BadExpression(syntax, ImmutableArray.Create(receiver, name), ErrorTypeSymbol.UnknownResultType)
            End If

            Return New BoundXmlMemberAccess(syntax, memberAccess, memberAccess.Type)
        End Function

        Private Function BindXmlMemberAccessReceiver(syntax As XmlMemberAccessExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            If syntax.Base Is Nothing Then
                Dim receiver As BoundExpression

                Dim conditionalAccess As ConditionalAccessExpressionSyntax = syntax.GetCorrespondingConditionalAccessExpression()

                If conditionalAccess IsNot Nothing Then
                    receiver = GetConditionalAccessReceiver(conditionalAccess)
                Else
                    receiver = TryBindOmittedLeftForXmlMemberAccess(syntax, diagnostics, Me)
                End If

                If receiver Is Nothing Then
                    Return ReportDiagnosticAndProduceBadExpression(diagnostics, syntax, ERRID.ERR_BadWithRef)
                End If
                Return receiver
            Else
                Dim receiver = BindValue(syntax.Base, diagnostics)
                Debug.Assert(receiver IsNot Nothing)
                Return AdjustReceiverValue(receiver, syntax, diagnostics)
            End If
        End Function

        Private Function BindXmlName(
                                    syntax As XmlNameSyntax,
                                    forElement As Boolean,
                                    diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim fromImports = False
            Dim prefix As String = Nothing
            Dim localName As String = Nothing
            Dim [namespace] As String = Nothing
            Return BindXmlName(syntax, forElement, Nothing, fromImports, prefix, localName, [namespace], diagnostics)
        End Function

        Private Function BindXmlName(
                                    syntax As XmlNameSyntax,
                                    forElement As Boolean,
                                    rootInfoOpt As XmlElementRootInfo,
                                    <Out()> ByRef fromImports As Boolean,
                                    <Out()> ByRef prefix As String,
                                    <Out()> ByRef localName As String,
                                    <Out()> ByRef [namespace] As String,
                                    diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim prefixSyntax = syntax.Prefix
            Dim namespaceExpr As BoundLiteral

            fromImports = False
            localName = GetXmlName(syntax.LocalName)
            [namespace] = Nothing

            If prefixSyntax IsNot Nothing Then
                Dim prefixToken = prefixSyntax.Name
                prefix = GetXmlName(prefixToken)

                If forElement AndAlso (prefix = StringConstants.XmlnsPrefix) Then
                    ' "Element names cannot use the 'xmlns' prefix."
                    ReportDiagnostic(diagnostics, prefixToken, ERRID.ERR_IllegalXmlnsPrefix)
                    Return BadExpression(syntax, Compilation.GetSpecialType(SpecialType.System_String))
                End If

                If Not LookupXmlNamespace(prefix, False, [namespace], fromImports) Then
                    Return ReportXmlNamespacePrefixNotDefined(syntax, prefixSyntax.Name, prefix, compilerGenerated:=False, diagnostics:=diagnostics)
                End If

                namespaceExpr = CreateStringLiteral(prefixSyntax, [namespace], compilerGenerated:=False, diagnostics:=diagnostics)
            Else
                prefix = StringConstants.DefaultXmlnsPrefix

                If forElement Then
                    Dim found = LookupXmlNamespace(prefix, False, [namespace], fromImports)
                    Debug.Assert(found)
                Else
                    [namespace] = StringConstants.DefaultXmlNamespace
                End If

                namespaceExpr = CreateStringLiteral(syntax, [namespace], compilerGenerated:=True, diagnostics:=diagnostics)
            End If

            ' LocalName is marked as CompilerGenerated to avoid confusing the semantic model
            ' with two BoundNodes (LocalName and entire XName) for the same syntax node.
            Dim localNameExpr = CreateStringLiteral(syntax, localName, compilerGenerated:=True, diagnostics:=diagnostics)
            Return BindXmlName(syntax, localNameExpr, namespaceExpr, diagnostics)
        End Function

        Private Shared Sub AddImportedNamespaceIfNecessary(
                                                          importedNamespaces As ArrayBuilder(Of KeyValuePair(Of String, String)),
                                                          prefix As String,
                                                          [namespace] As String,
                                                          forElement As Boolean)
            Debug.Assert(prefix IsNot Nothing)
            Debug.Assert([namespace] IsNot Nothing)

            ' If the namespace is the default, create an xmlns="" attribute
            ' if the reference was for an XmlElement name. Otherwise,
            ' avoid adding an attribute for the default namespace.
            If [namespace] = StringConstants.DefaultXmlNamespace Then
                If Not forElement OrElse (prefix = StringConstants.DefaultXmlnsPrefix) Then
                    Return
                End If
                prefix = StringConstants.DefaultXmlnsPrefix
            End If

            For Each pair In importedNamespaces
                If pair.Key = prefix Then
                    Return
                End If
            Next

            importedNamespaces.Add(New KeyValuePair(Of String, String)(prefix, [namespace]))
        End Sub

        Private Function BindXmlName(syntax As VisualBasicSyntaxNode, localName As BoundExpression, [namespace] As BoundExpression, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim group = GetXmlMethodOrPropertyGroup(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XName, syntax, diagnostics),
                StringConstants.XmlGetMethodName,
                Nothing,
                diagnostics)
            Dim objectCreation = BindInvocationExpressionIfGroupNotNothing(syntax, group, ImmutableArray.Create(Of BoundExpression)(localName, [namespace]), diagnostics)
            Return New BoundXmlName(syntax, [namespace], localName, objectCreation, objectCreation.Type)
        End Function

        Private Function BindGetXmlNamespace(syntax As GetXmlNamespaceExpressionSyntax, diagnostics As BindingDiagnosticBag) As BoundExpression
            diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)

            Dim nameSyntax = syntax.Name
            Dim [namespace] As String = Nothing
            Dim fromImports = False
            Dim expr As BoundExpression

            If nameSyntax IsNot Nothing Then
                Dim prefixToken = nameSyntax.Name
                Dim prefix = GetXmlName(prefixToken)
                If LookupXmlNamespace(prefix, False, [namespace], fromImports) Then
                    expr = CreateStringLiteral(nameSyntax, [namespace], compilerGenerated:=False, diagnostics:=diagnostics)
                Else
                    expr = ReportXmlNamespacePrefixNotDefined(nameSyntax, prefixToken, prefix, compilerGenerated:=False, diagnostics:=diagnostics)
                End If
            Else
                Dim found = LookupXmlNamespace(StringConstants.DefaultXmlnsPrefix, False, [namespace], fromImports)
                Debug.Assert(found)
                expr = CreateStringLiteral(syntax, [namespace], compilerGenerated:=True, diagnostics:=diagnostics)
            End If

            Return BindXmlNamespace(syntax, expr, diagnostics)
        End Function

        Private Function BindXmlNamespace(syntax As VisualBasicSyntaxNode, [namespace] As BoundExpression, diagnostics As BindingDiagnosticBag) As BoundExpression
            Dim group = GetXmlMethodOrPropertyGroup(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XNamespace, syntax, diagnostics),
                StringConstants.XmlGetMethodName,
                Nothing,
                diagnostics)
            Dim objectCreation = BindInvocationExpressionIfGroupNotNothing(syntax, group, ImmutableArray.Create(Of BoundExpression)([namespace]), diagnostics)
            Return New BoundXmlNamespace(syntax, [namespace], objectCreation, objectCreation.Type)
        End Function

        Private Function ReportXmlNamespacePrefixNotDefined(syntax As VisualBasicSyntaxNode, prefixToken As SyntaxToken, prefix As String, compilerGenerated As Boolean, diagnostics As BindingDiagnosticBag) As BoundBadExpression
            Debug.Assert(prefix IsNot Nothing)
            Debug.Assert(prefix = GetXmlName(prefixToken))
            ' "XML namespace prefix '{0}' is not defined."
            ReportDiagnostic(diagnostics, prefixToken, ERRID.ERR_UndefinedXmlPrefix, prefix)
            Dim result = BadExpression(syntax, Compilation.GetSpecialType(SpecialType.System_String))
            If compilerGenerated Then
                result.SetWasCompilerGenerated()
            End If
            Return result
        End Function

        Private Function BindXmlCData(syntax As XmlCDataSectionSyntax, rootInfoOpt As XmlElementRootInfo, diagnostics As BindingDiagnosticBag) As BoundExpression
            If rootInfoOpt Is Nothing Then
                diagnostics = CheckXmlFeaturesAllowed(syntax, diagnostics)
            End If

            Dim value = CreateStringLiteral(syntax, GetXmlString(syntax.TextTokens), compilerGenerated:=True, diagnostics:=diagnostics)
            Dim objectCreation = BindObjectCreationExpression(
                syntax,
                GetWellKnownType(WellKnownType.System_Xml_Linq_XCData, syntax, diagnostics),
                ImmutableArray.Create(Of BoundExpression)(value),
                diagnostics)
            Return New BoundXmlCData(syntax, value, objectCreation, objectCreation.Type)
        End Function

        Private Function BindXmlText(syntax As XmlTextSyntax, diagnostics As BindingDiagnosticBag) As BoundLiteral
            Return CreateStringLiteral(syntax, GetXmlString(syntax.TextTokens), compilerGenerated:=False, diagnostics:=diagnostics)
        End Function

        Friend Shared Function GetXmlString(tokens As SyntaxTokenList) As String
            Dim n = tokens.Count
            If n = 0 Then
                Return String.Empty
            ElseIf n = 1 Then
                Return GetXmlString(tokens(0))
            Else
                Dim pooledBuilder = PooledStringBuilder.GetInstance()
                Dim builder = pooledBuilder.Builder
                For Each token In tokens
                    builder.Append(GetXmlString(token))
                Next
                Dim result = builder.ToString()
                pooledBuilder.Free()
                Return result
            End If
        End Function

        Private Shared Function GetXmlString(token As SyntaxToken) As String
            Select Case token.Kind
                Case SyntaxKind.XmlTextLiteralToken, SyntaxKind.XmlEntityLiteralToken
                    Return token.ValueText
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(token.Kind)
            End Select
        End Function

        Private Function GetXmlMethodOrPropertyGroup(syntax As VisualBasicSyntaxNode, type As NamedTypeSymbol, memberName As String, receiverOpt As BoundExpression, diagnostics As BindingDiagnosticBag) As BoundMethodOrPropertyGroup
            If type.IsErrorType() Then
                Return Nothing
            End If

            Debug.Assert((receiverOpt Is Nothing) OrElse
                         receiverOpt.Type.IsErrorType() OrElse
                         receiverOpt.Type.IsOrDerivedFrom(type, CompoundUseSiteInfo(Of AssemblySymbol).Discarded))

            Dim group As BoundMethodOrPropertyGroup = Nothing
            Dim result = LookupResult.GetInstance()

            ' Match the lookup of XML members in the native compiler: consider members
            ' on this type only, not base types, and ignore extension methods or properties
            ' from the current scope. (Extension methods and properties will be included,
            ' as shared members, if the members are defined on 'type' however.)
            Dim useSiteInfo = GetNewCompoundUseSiteInfo(diagnostics)
            LookupMember(result,
                         type,
                         memberName,
                         arity:=0,
                         options:=LookupOptions.AllMethodsOfAnyArity Or LookupOptions.NoBaseClassLookup Or LookupOptions.IgnoreExtensionMethods,
                         useSiteInfo:=useSiteInfo)

            diagnostics.Add(syntax, useSiteInfo)

            If result.IsGood Then
                Debug.Assert(result.Symbols.Count > 0)
                Dim symbol0 = result.Symbols(0)
                Select Case result.Symbols(0).Kind
                    Case SymbolKind.Method
                        group = New BoundMethodGroup(syntax,
                                                    Nothing,
                                                    result.Symbols.ToDowncastedImmutable(Of MethodSymbol),
                                                    result.Kind,
                                                    receiverOpt,
                                                    QualificationKind.QualifiedViaValue)
                    Case SymbolKind.Property
                        group = New BoundPropertyGroup(syntax,
                                                    result.Symbols.ToDowncastedImmutable(Of PropertySymbol),
                                                    result.Kind,
                                                    receiverOpt,
                                                    QualificationKind.QualifiedViaValue)
                End Select
            End If

            If group Is Nothing Then
                ReportDiagnostic(diagnostics,
                                 syntax,
                                 If(result.HasDiagnostic,
                                    result.Diagnostic,
                                    ErrorFactory.ErrorInfo(ERRID.ERR_NameNotMember2, memberName, type)))
            End If

            result.Free()
            Return group
        End Function

        ''' <summary>
        ''' If the method or property group is not Nothing, bind as an invocation expression.
        ''' Otherwise return a BoundBadExpression containing the arguments.
        ''' </summary>
        Private Function BindInvocationExpressionIfGroupNotNothing(syntax As SyntaxNode, groupOpt As BoundMethodOrPropertyGroup, arguments As ImmutableArray(Of BoundExpression), diagnostics As BindingDiagnosticBag) As BoundExpression
            If groupOpt Is Nothing Then
                Return BadExpression(syntax, arguments, ErrorTypeSymbol.UnknownResultType)
            Else
                Return BindInvocationExpression(syntax,
                                                syntax,
                                                TypeCharacter.None,
                                                groupOpt,
                                                arguments,
                                                argumentNames:=Nothing,
                                                diagnostics:=diagnostics,
                                                callerInfoOpt:=Nothing)
            End If
        End Function

        ''' <summary>
        ''' Check if XML features are allowed. If not, report an error and return a
        ''' separate DiagnosticBag that can be used for binding sub-expressions.
        ''' </summary>
        Private Function CheckXmlFeaturesAllowed(syntax As VisualBasicSyntaxNode, diagnostics As BindingDiagnosticBag) As BindingDiagnosticBag
            ' Check if XObject is available, which matches the native compiler.
            Dim type = Compilation.GetWellKnownType(WellKnownType.System_Xml_Linq_XObject)
            If type.IsErrorType() Then
                ' "XML literals and XML axis properties are not available. Add references to System.Xml, System.Xml.Linq, and System.Core."
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_XmlFeaturesNotAvailable)
                ' DiagnosticBag does not need to be created from the pool
                ' since this is an error recovery scenario only.
                Return BindingDiagnosticBag.Discarded
            Else
                Return diagnostics
            End If
        End Function

        Private Function CreateStringLiteral(
                                            syntax As VisualBasicSyntaxNode,
                                            str As String,
                                            compilerGenerated As Boolean,
                                            diagnostics As BindingDiagnosticBag,
                                            Optional hasErrors As Boolean = False) As BoundLiteral
            Debug.Assert(syntax IsNot Nothing)
            Dim result = New BoundLiteral(syntax, ConstantValue.Create(str), GetSpecialType(SpecialType.System_String, syntax, diagnostics), hasErrors:=hasErrors)
            If compilerGenerated Then
                result.SetWasCompilerGenerated()
            End If
            Return result
        End Function

        ''' <summary>
        ''' Bind any xmlns declaration attributes and return the bound nodes plus a Dictionary
        ''' of { prefix, namespace } pairs that will be used for namespace lookup at and below
        ''' the containing XmlElement. Any xmlns declarations that are redundant with Imports
        ''' in scope (same prefix and namespace) are dropped, and instead, an entry is added
        ''' to the 'importedNamespaces' collection. When the root XmlElement is generated,
        ''' xmlns attributes will be added for all entries in importedNamespaces. Any attributes
        ''' other than xmlns are added to the 'otherAttributes' collection for binding by the caller.
        ''' </summary>
        Private Function BindXmlnsAttributes(
                                      attributes As SyntaxList(Of XmlNodeSyntax),
                                      <Out()> ByRef allAttributes As Dictionary(Of XmlName, BoundXmlAttribute),
                                      xmlnsAttributes As ArrayBuilder(Of BoundXmlAttribute),
                                      otherAttributes As ArrayBuilder(Of XmlNodeSyntax),
                                      importedNamespaces As ArrayBuilder(Of KeyValuePair(Of String, String)),
                                      diagnostics As BindingDiagnosticBag) As Dictionary(Of String, String)
            Debug.Assert(xmlnsAttributes.Count = 0)
            Debug.Assert(otherAttributes.Count = 0)

            Dim namespaces As Dictionary(Of String, String) = Nothing

            For Each attribute In attributes
                Dim syntax = TryCast(attribute, XmlAttributeSyntax)

                Dim prefix As String = Nothing
                Dim namespaceName As String = Nothing
                Dim [namespace] As BoundExpression = Nothing
                Dim hasErrors As Boolean = False

                If (syntax IsNot Nothing) AndAlso
                    TryGetXmlnsAttribute(syntax, prefix, namespaceName, [namespace], hasErrors, fromImport:=False, diagnostics:=diagnostics) Then
                    Debug.Assert(prefix IsNot Nothing)
                    Debug.Assert(hasErrors OrElse (namespaceName IsNot Nothing))
                    Debug.Assert(hasErrors OrElse ([namespace] IsNot Nothing))

                    Dim matchesImport = Not hasErrors AndAlso MatchesXmlnsImport(prefix, namespaceName)

                    ' Generate a BoundXmlAttribute, even if we'll drop the
                    ' attribute, since the semantic model will need one.
                    Dim xmlnsAttribute = BindXmlnsAttribute(
                        syntax,
                        BindXmlnsName(syntax.Name, prefix, compilerGenerated:=False, diagnostics:=diagnostics),
                        [namespace],
                        useConstructor:=True,
                        matchesImport:=matchesImport,
                        compilerGenerated:=False,
                        hasErrors:=hasErrors,
                        diagnostics:=diagnostics)
                    xmlnsAttributes.Add(xmlnsAttribute)

                    If Not hasErrors Then
                        If matchesImport Then
                            AddImportedNamespaceIfNecessary(importedNamespaces, prefix, namespaceName, forElement:=False)
                        End If

                        ' Check for duplicates.
                        If AddXmlAttributeIfNotDuplicate(syntax.Name, GetXmlnsXmlName(prefix), xmlnsAttribute, allAttributes, diagnostics) Then
                            If namespaces Is Nothing Then
                                namespaces = New Dictionary(Of String, String)
                            End If

                            namespaces.Add(prefix, namespaceName)
                        End If
                    End If
                Else
                    ' Not an xmlns attribute. Defer binding to the caller, to
                    ' allow the caller to include any namespaces found here
                    ' in the lookup of the prefix on this and other attributes.
                    otherAttributes.Add(attribute)
                End If
            Next

            Return namespaces
        End Function

        Private Shared Function AddXmlAttributeIfNotDuplicate(
                                                      syntax As XmlNodeSyntax,
                                                      name As XmlName,
                                                      attribute As BoundXmlAttribute,
                                                      <Out()> ByRef allAttributes As Dictionary(Of XmlName, BoundXmlAttribute),
                                                      diagnostics As BindingDiagnosticBag) As Boolean
            If allAttributes Is Nothing Then
                allAttributes = New Dictionary(Of XmlName, BoundXmlAttribute)(XmlNameComparer.Instance)
            End If

            If allAttributes.ContainsKey(name) Then
                ' "Duplicate XML attribute '{0}'."
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_DuplicateXmlAttribute, syntax.ToString())
                Return False
            Else
                allAttributes.Add(name, attribute)
                Return True
            End If
        End Function

        ''' <summary>
        ''' If the attribute represents an xmlns declaration, populate 'prefix' and 'namespace',
        ''' and generate diagnostics and set hasErrors if there are errors. Returns True if this
        ''' is an xmlns declaration, even if there are errors. Unless this attribute is from an
        ''' Imports statement, generate the BoundExpression for the namespace as well.
        ''' (For Imports, binding is skipped, since a BoundNode is not needed, and in the
        ''' invalid case of "xmlns:p=&lt;%= expr %&gt;", expr may result in a cycle.
        ''' </summary>
        Private Function TryGetXmlnsAttribute(
                                             syntax As XmlAttributeSyntax,
                                             <Out()> ByRef prefix As String,
                                             <Out()> ByRef namespaceName As String,
                                             <Out()> ByRef [namespace] As BoundExpression,
                                             <Out()> ByRef hasErrors As Boolean,
                                             fromImport As Boolean,
                                             diagnostics As BindingDiagnosticBag) As Boolean
            prefix = Nothing
            namespaceName = Nothing
            [namespace] = Nothing
            hasErrors = False

            ' If the name is an embedded expression, it should not be treated as an
            ' "xmlns" declaration at compile-time, regardless of the expression value.
            If syntax.Name.Kind = SyntaxKind.XmlEmbeddedExpression Then
                Return False
            End If

            Debug.Assert(syntax.Name.Kind = SyntaxKind.XmlName)
            Dim nameSyntax = DirectCast(syntax.Name, XmlNameSyntax)

            If Not TryGetXmlnsPrefix(nameSyntax, prefix, diagnostics) Then
                Return False
            End If

            Debug.Assert(prefix IsNot Nothing)

            Dim valueSyntax = syntax.Value
            If valueSyntax.Kind <> SyntaxKind.XmlString Then
                Debug.Assert(valueSyntax.Kind = SyntaxKind.XmlEmbeddedExpression)
                ' "An embedded expression cannot be used here."
                ReportDiagnostic(diagnostics, valueSyntax, ERRID.ERR_EmbeddedExpression)
                hasErrors = True

                ' Avoid binding Imports since that might result in a cycle.
                If Not fromImport Then
                    [namespace] = BindXmlEmbeddedExpression(DirectCast(valueSyntax, XmlEmbeddedExpressionSyntax), diagnostics)
                End If

            Else
                namespaceName = GetXmlString(DirectCast(valueSyntax, XmlStringSyntax).TextTokens)
                Debug.Assert(namespaceName IsNot Nothing)

                If (prefix = StringConstants.XmlnsPrefix) OrElse
                    ((prefix = StringConstants.XmlPrefix) AndAlso (namespaceName <> StringConstants.XmlNamespace)) Then
                    ' "XML namespace prefix '{0}' is reserved for use by XML and the namespace URI cannot be changed."
                    ReportDiagnostic(diagnostics, nameSyntax.LocalName, ERRID.ERR_ReservedXmlPrefix, prefix)
                    hasErrors = True

                ElseIf Not fromImport AndAlso
                    String.IsNullOrEmpty(namespaceName) AndAlso
                    Not String.IsNullOrEmpty(prefix) Then
                    ' "Namespace declaration with prefix cannot have an empty value inside an XML literal."
                    ReportDiagnostic(diagnostics, nameSyntax.LocalName, ERRID.ERR_IllegalDefaultNamespace)
                    hasErrors = True

                ElseIf RedefinesReservedXmlNamespace(syntax.Value, prefix, StringConstants.XmlnsPrefix, namespaceName, StringConstants.XmlnsNamespace, diagnostics) OrElse
                    RedefinesReservedXmlNamespace(syntax.Value, prefix, StringConstants.XmlPrefix, namespaceName, StringConstants.XmlNamespace, diagnostics) Then
                    hasErrors = True

                End If

                If Not fromImport Then
                    [namespace] = CreateStringLiteral(
                        valueSyntax,
                        namespaceName,
                        compilerGenerated:=False,
                        diagnostics:=diagnostics)
                End If
            End If

            Return True
        End Function

        Private Shared Function RedefinesReservedXmlNamespace(syntax As VisualBasicSyntaxNode, prefix As String, reservedPrefix As String, [namespace] As String, reservedNamespace As String, diagnostics As BindingDiagnosticBag) As Boolean
            If ([namespace] = reservedNamespace) AndAlso (prefix <> reservedPrefix) Then
                ' "Prefix '{0}' cannot be bound to namespace name reserved for '{1}'."
                ReportDiagnostic(diagnostics, syntax, ERRID.ERR_ReservedXmlNamespace, prefix, reservedPrefix)
                Return True
            End If
            Return False
        End Function

        ''' <summary>
        ''' If name is "xmlns", set prefix to String.Empty and return True.
        ''' If name is "xmlns:p", set prefix to p and return True.
        ''' Otherwise return False.
        ''' </summary>
        Private Function TryGetXmlnsPrefix(syntax As XmlNameSyntax, <Out()> ByRef prefix As String, diagnostics As BindingDiagnosticBag) As Boolean
            Dim localName = GetXmlName(syntax.LocalName)
            Dim prefixName As String = Nothing

            If syntax.Prefix IsNot Nothing Then
                prefixName = GetXmlName(syntax.Prefix.Name)
                If prefixName = StringConstants.XmlnsPrefix Then
                    prefix = localName
                    Return True
                End If
            End If

            If localName = StringConstants.XmlnsPrefix Then
                ' Dev11 treats as p:xmlns="..." as an xmlns declaration, and ignores 'p',
                ' treating it as a declaration of the default namespace in all cases. Since
                ' the user probably intended to write xmlns:p="...", we now issue a warning.
                If Not String.IsNullOrEmpty(prefixName) Then
                    ' If 'p' maps to the empty namespace, we'll end up generating an attribute
                    ' with local name "xmlns" in the empty namespace, which the runtime will
                    ' interpret as an xmlns declaration of the default namespace. So, in that
                    ' case, we treat p:xmlns="..." as an xmlns declaration as in Dev11.
                    Dim fromImports = False
                    Dim [namespace] As String = Nothing
                    ' Lookup can ignore namespaces defined on XElements since we're interested
                    ' in the default namespace and that can only be defined with Imports.
                    If LookupXmlNamespace(prefixName, True, [namespace], fromImports) AndAlso ([namespace] = StringConstants.DefaultXmlNamespace) Then
                        ReportDiagnostic(diagnostics, syntax, ERRID.WRN_EmptyPrefixAndXmlnsLocalName)
                    Else
                        ReportDiagnostic(diagnostics, syntax, ERRID.WRN_PrefixAndXmlnsLocalName, prefixName)
                        prefix = Nothing
                        Return False
                    End If
                End If

                prefix = String.Empty
                Return True
            End If

            prefix = Nothing
            Return False
        End Function

        Private Shared Function GetXmlName(token As SyntaxToken) As String
            Select Case token.Kind
                Case SyntaxKind.XmlNameToken
                    Return token.ValueText
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(token.Kind)
            End Select
        End Function

        ''' <summary>
        ''' State tracked for the root XmlElement while binding nodes within the
        ''' tree. This state is mutable since it includes the set of namespaces from
        ''' Imports referenced within the tree. Ideally, this state would be part of the
        ''' XmlRootElementBinder, but since this state is mutable, there would be
        ''' issues caching and reusing the Binder. Instead, the state is passed
        ''' explicitly as an argument to each binding method.
        ''' </summary>
        Private NotInheritable Class XmlElementRootInfo
            Private ReadOnly _binder As Binder
            Private ReadOnly _syntax As XmlNodeSyntax
            Private ReadOnly _importedNamespaces As ArrayBuilder(Of KeyValuePair(Of String, String))
            Private _xmlnsAttributesPlaceholder As BoundRValuePlaceholder
            Private _removeNamespacesGroup As BoundMethodOrPropertyGroup

            Public Sub New(binder As Binder, syntax As XmlNodeSyntax, importedNamespaces As ArrayBuilder(Of KeyValuePair(Of String, String)))
                _binder = binder
                _syntax = syntax
                _importedNamespaces = importedNamespaces
            End Sub

            Public ReadOnly Property Syntax As XmlNodeSyntax
                Get
                    Return _syntax
                End Get
            End Property

            Public ReadOnly Property ImportedNamespaces As ArrayBuilder(Of KeyValuePair(Of String, String))
                Get
                    Return _importedNamespaces
                End Get
            End Property

            Public ReadOnly Property XmlnsAttributesPlaceholder As BoundRValuePlaceholder
                Get
                    Return _xmlnsAttributesPlaceholder
                End Get
            End Property

            Public Function BindRemoveNamespaceAttributesInvocation(
                                                                    expr As BoundExpression,
                                                                    prefixes As BoundRValuePlaceholder,
                                                                    namespaces As BoundRValuePlaceholder,
                                                                    diagnostics As BindingDiagnosticBag) As BoundExpression
                Return _binder.BindRemoveNamespaceAttributesInvocation(
                    _syntax,
                    expr,
                    prefixes,
                    namespaces,
                    _xmlnsAttributesPlaceholder,
                    _removeNamespacesGroup,
                    diagnostics)
            End Function
        End Class

    End Class

    ''' <summary>
    ''' Binding state used by the rewriter for XContainer derived types.
    ''' </summary>
    Friend NotInheritable Class BoundXmlContainerRewriterInfo
        Public Sub New(objectCreation As BoundExpression)
            Debug.Assert(objectCreation IsNot Nothing)
            Me.ObjectCreation = objectCreation
            Me.SideEffects = ImmutableArray(Of BoundExpression).Empty

            Me.HasErrors = objectCreation.HasErrors
        End Sub

        Public Sub New(isRoot As Boolean,
                       placeholder As BoundRValuePlaceholder,
                       objectCreation As BoundExpression,
                       xmlnsAttributesPlaceholder As BoundRValuePlaceholder,
                       xmlnsAttributes As BoundExpression,
                       prefixesPlaceholder As BoundRValuePlaceholder,
                       namespacesPlaceholder As BoundRValuePlaceholder,
                       importedNamespaces As ImmutableArray(Of KeyValuePair(Of String, String)),
                       inScopeXmlNamespaces As ImmutableArray(Of KeyValuePair(Of String, String)),
                       sideEffects As ImmutableArray(Of BoundExpression))
            Debug.Assert(isRoot = Not importedNamespaces.IsDefault)
            Debug.Assert(placeholder IsNot Nothing)
            Debug.Assert(objectCreation IsNot Nothing)
            Debug.Assert((xmlnsAttributesPlaceholder IsNot Nothing) = (xmlnsAttributes IsNot Nothing))
            Debug.Assert((prefixesPlaceholder IsNot Nothing) = (namespacesPlaceholder IsNot Nothing))
            Debug.Assert(Not sideEffects.IsDefault)

            Me.IsRoot = isRoot
            Me.Placeholder = placeholder
            Me.ObjectCreation = objectCreation
            Me.XmlnsAttributesPlaceholder = xmlnsAttributesPlaceholder
            Me.XmlnsAttributes = xmlnsAttributes
            Me.PrefixesPlaceholder = prefixesPlaceholder
            Me.NamespacesPlaceholder = namespacesPlaceholder
            Me.ImportedNamespaces = importedNamespaces
            Me.InScopeXmlNamespaces = inScopeXmlNamespaces
            Me.SideEffects = sideEffects

            Me.HasErrors = objectCreation.HasErrors OrElse sideEffects.Any(Function(s) s.HasErrors)
        End Sub

        Public ReadOnly IsRoot As Boolean
        Public ReadOnly Placeholder As BoundRValuePlaceholder
        Public ReadOnly ObjectCreation As BoundExpression
        Public ReadOnly XmlnsAttributesPlaceholder As BoundRValuePlaceholder
        Public ReadOnly XmlnsAttributes As BoundExpression
        Public ReadOnly PrefixesPlaceholder As BoundRValuePlaceholder
        Public ReadOnly NamespacesPlaceholder As BoundRValuePlaceholder
        Public ReadOnly ImportedNamespaces As ImmutableArray(Of KeyValuePair(Of String, String))
        Public ReadOnly InScopeXmlNamespaces As ImmutableArray(Of KeyValuePair(Of String, String))
        Public ReadOnly SideEffects As ImmutableArray(Of BoundExpression)
        Public ReadOnly HasErrors As Boolean
    End Class

    ''' <summary>
    ''' A binder to expose namespaces from Imports&lt;xmlns:...&gt; statements.
    ''' </summary>
    Friend NotInheritable Class XmlNamespaceImportsBinder
        Inherits Binder

        Private ReadOnly _namespaces As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition)

        Public Sub New(containingBinder As Binder, namespaces As IReadOnlyDictionary(Of String, XmlNamespaceAndImportsClausePosition))
            MyBase.New(containingBinder)
            Debug.Assert(namespaces IsNot Nothing)
            Debug.Assert(namespaces.Count > 0)
            _namespaces = namespaces
        End Sub

        Public Function GetImportChainData() As ImmutableArray(Of ImportedXmlNamespace)
            Return _namespaces.SelectAsArray(Function(kvp) New ImportedXmlNamespace(kvp.Value.XmlNamespace, kvp.Value.SyntaxReference))
        End Function

        Friend Overrides ReadOnly Property HasImportedXmlNamespaces As Boolean
            Get
                Return True
            End Get
        End Property

        Friend Overrides Function LookupXmlNamespace(prefix As String,
                                                     ignoreXmlNodes As Boolean,
                                                     <Out()> ByRef [namespace] As String,
                                                     <Out()> ByRef fromImports As Boolean) As Boolean
            Dim result As XmlNamespaceAndImportsClausePosition = Nothing
            If _namespaces.TryGetValue(prefix, result) Then
                [namespace] = result.XmlNamespace
                Me.Compilation.MarkImportDirectiveAsUsed(Me.SyntaxTree, result.ImportsClausePosition)
                fromImports = True
                Return True
            End If

            Return MyBase.LookupXmlNamespace(prefix, ignoreXmlNodes, [namespace], fromImports)
        End Function
    End Class

    Friend NotInheritable Class XmlRootElementBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides Sub GetInScopeXmlNamespaces(builder As ArrayBuilder(Of KeyValuePair(Of String, String)))
        End Sub
    End Class

    ''' <summary>
    ''' A binder for XmlElement declarations.
    ''' </summary>
    Friend NotInheritable Class XmlElementBinder
        Inherits Binder
        Private ReadOnly _namespaces As Dictionary(Of String, String)

        Public Sub New(containingBinder As Binder, namespaces As Dictionary(Of String, String))
            MyBase.New(containingBinder)
            Debug.Assert(namespaces IsNot Nothing)
            Debug.Assert(namespaces.Count > 0)
            _namespaces = namespaces
        End Sub

        Friend Overrides Function LookupXmlNamespace(prefix As String, ignoreXmlNodes As Boolean, <Out()> ByRef [namespace] As String, <Out()> ByRef fromImports As Boolean) As Boolean
            If Not ignoreXmlNodes Then
                If _namespaces.TryGetValue(prefix, [namespace]) Then
                    fromImports = False
                    Return True
                End If
            End If
            Return MyBase.LookupXmlNamespace(prefix, ignoreXmlNodes, [namespace], fromImports)
        End Function

        Friend Overrides Sub GetInScopeXmlNamespaces(builder As ArrayBuilder(Of KeyValuePair(Of String, String)))
            builder.AddRange(_namespaces)
            ContainingBinder.GetInScopeXmlNamespaces(builder)
        End Sub
    End Class

    Friend NotInheritable Class XmlEmbeddedExpressionBinder
        Inherits Binder

        Public Sub New(containingBinder As Binder)
            MyBase.New(containingBinder)
        End Sub

        Friend Overrides Function LookupXmlNamespace(prefix As String, ignoreXmlNodes As Boolean, <Out()> ByRef [namespace] As String, <Out()> ByRef fromImports As Boolean) As Boolean
            ' Perform further namespace lookup on the nearest containing binder
            ' that is outside any XML to avoid inheriting xmlns declarations
            ' from XML nodes outside of the embedded expression.
            Return MyBase.LookupXmlNamespace(prefix, True, [namespace], fromImports)
        End Function

        Friend Overrides Sub GetInScopeXmlNamespaces(builder As ArrayBuilder(Of KeyValuePair(Of String, String)))
        End Sub
    End Class

    ''' <summary>
    ''' An extension property in reduced form, with first parameter
    ''' removed and exposed as an explicit receiver type.
    ''' </summary>
    Friend NotInheritable Class ReducedExtensionPropertySymbol
        Inherits PropertySymbol

        Private ReadOnly _originalDefinition As PropertySymbol

        Public Sub New(originalDefinition As PropertySymbol)
            Debug.Assert(originalDefinition IsNot Nothing)
            Debug.Assert(originalDefinition.IsShared)
            Debug.Assert(originalDefinition.ParameterCount = 1)

            _originalDefinition = originalDefinition
        End Sub

        Friend Overrides ReadOnly Property ReducedFrom As PropertySymbol
            Get
                Return _originalDefinition
            End Get
        End Property

        Friend Overrides ReadOnly Property ReducedFromDefinition As PropertySymbol
            Get
                Return _originalDefinition
            End Get
        End Property

        Friend Overrides ReadOnly Property ReceiverType As TypeSymbol
            Get
                Return _originalDefinition.Parameters(0).Type
            End Get
        End Property

        Public Overrides ReadOnly Property Name As String
            Get
                Return _originalDefinition.Name
            End Get
        End Property

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Return _originalDefinition.HasSpecialName
            End Get
        End Property

        Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
            Get
                Return _originalDefinition.CallingConvention
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return _originalDefinition.ContainingSymbol
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return _originalDefinition.DeclaredAccessibility
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return _originalDefinition.DeclaringSyntaxReferences
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of PropertySymbol)
            Get
                Return ImmutableArray(Of PropertySymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property GetMethod As MethodSymbol
            Get
                Return ReduceAccessorIfAny(_originalDefinition.GetMethod)
            End Get
        End Property

        Friend Overrides ReadOnly Property AssociatedField As FieldSymbol
            Get
                Return _originalDefinition.AssociatedField
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                Return _originalDefinition.ObsoleteAttributeData
            End Get
        End Property

        Public Overrides ReadOnly Property IsDefault As Boolean
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
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsOverloads As Boolean
            Get
                Return False
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

        Public Overrides ReadOnly Property IsShared As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                Return _originalDefinition.Locations
            End Get
        End Property

        Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                Return ImmutableArray(Of ParameterSymbol).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property ParameterCount As Integer
            Get
                Return 0
            End Get
        End Property

        Public Overrides ReadOnly Property SetMethod As MethodSymbol
            Get
                Return ReduceAccessorIfAny(_originalDefinition.SetMethod)
            End Get
        End Property

        Public Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                Return _originalDefinition.ReturnsByRef
            End Get
        End Property

        Public Overrides ReadOnly Property Type As TypeSymbol
            Get
                Return _originalDefinition.Type
            End Get
        End Property

        Public Overrides ReadOnly Property TypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalDefinition.TypeCustomModifiers
            End Get
        End Property

        Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return _originalDefinition.RefCustomModifiers
            End Get
        End Property

        Private Function ReduceAccessorIfAny(methodOpt As MethodSymbol) As ReducedExtensionAccessorSymbol
            Return If(methodOpt Is Nothing, Nothing, New ReducedExtensionAccessorSymbol(Me, methodOpt))
        End Function

        Friend Overrides ReadOnly Property IsMyGroupCollectionProperty As Boolean
            Get
                Debug.Assert(Not _originalDefinition.IsMyGroupCollectionProperty)
                Return False
            End Get
        End Property

        Public Overrides ReadOnly Property IsRequired As Boolean
            Get
                Return False
            End Get
        End Property

        Private NotInheritable Class ReducedExtensionAccessorSymbol
            Inherits MethodSymbol

            Private ReadOnly _associatedProperty As ReducedExtensionPropertySymbol
            Private ReadOnly _originalDefinition As MethodSymbol
            Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

            Public Sub New(associatedProperty As ReducedExtensionPropertySymbol, originalDefinition As MethodSymbol)
                _associatedProperty = associatedProperty
                _originalDefinition = originalDefinition
            End Sub

            Friend Overrides ReadOnly Property CallsiteReducedFromMethod As MethodSymbol
                Get
                    Return _originalDefinition
                End Get
            End Property

            Public Overrides ReadOnly Property ReducedFrom As MethodSymbol
                Get
                    Return _originalDefinition
                End Get
            End Property

            Public Overrides ReadOnly Property Arity As Integer
                Get
                    Return 0
                End Get
            End Property

            Public Overrides ReadOnly Property AssociatedSymbol As Symbol
                Get
                    Return _associatedProperty
                End Get
            End Property

            Friend Overrides ReadOnly Property HasSpecialName As Boolean
                Get
                    Return _originalDefinition.HasSpecialName
                End Get
            End Property

            Friend Overrides ReadOnly Property CallingConvention As Microsoft.Cci.CallingConvention
                Get
                    Return Microsoft.Cci.CallingConvention.HasThis
                End Get
            End Property

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _originalDefinition.ContainingSymbol
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
                Get
                    Return _originalDefinition.DeclaredAccessibility
                End Get
            End Property

            Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
                Get
                    Return _originalDefinition.DeclaringSyntaxReferences
                End Get
            End Property

            Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
                Get
                    Return ImmutableArray(Of MethodSymbol).Empty
                End Get
            End Property

            Public Overrides ReadOnly Property IsExtensionMethod As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsExternalMethod As Boolean
                Get
                    Return _originalDefinition.IsExternalMethod
                End Get
            End Property

            Public Overrides Function GetDllImportData() As DllImportData
                Return _originalDefinition.GetDllImportData()
            End Function

            Friend Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
                Get
                    Return _originalDefinition.ReturnTypeMarshallingInformation
                End Get
            End Property

            Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
                Get
                    Return _originalDefinition.ImplementationAttributes
                End Get
            End Property

            Friend Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
                Get
                    Return _originalDefinition.HasDeclarativeSecurity
                End Get
            End Property

            Friend Overrides Function GetSecurityInformation() As IEnumerable(Of Microsoft.Cci.SecurityAttribute)
                Return _originalDefinition.GetSecurityInformation()
            End Function

            Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
                Get
                    Return _originalDefinition.ObsoleteAttributeData
                End Get
            End Property

            Public Overrides ReadOnly Property IsMustOverride As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsNotOverridable As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsOverloads As Boolean
                Get
                    Return False
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

            Public Overrides ReadOnly Property IsShared As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsSub As Boolean
                Get
                    Return _originalDefinition.IsSub
                End Get
            End Property

            Public Overrides ReadOnly Property IsAsync As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsIterator As Boolean
                Get
                    Return False
                End Get
            End Property

            Public Overrides ReadOnly Property IsInitOnly As Boolean
                Get
                    Return _originalDefinition.IsInitOnly
                End Get
            End Property

            Public Overrides ReadOnly Property IsVararg As Boolean
                Get
                    Return _originalDefinition.IsVararg
                End Get
            End Property

            Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
                Return _originalDefinition.GetAppliedConditionalSymbols()
            End Function

            Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
                Get
                    Return _originalDefinition.Locations
                End Get
            End Property

            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return _originalDefinition.MethodKind
                End Get
            End Property

            Friend Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
                Get
                    Return _originalDefinition.IsMethodKindBasedOnSyntax
                End Get
            End Property

            Friend Overrides ReadOnly Property ParameterCount As Integer
                Get
                    Return _originalDefinition.ParameterCount - 1
                End Get
            End Property

            Public Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
                Get
                    If _lazyParameters.IsDefault Then
                        ImmutableInterlocked.InterlockedInitialize(_lazyParameters, ReducedAccessorParameterSymbol.MakeParameters(Me, _originalDefinition.Parameters))
                    End If
                    Return _lazyParameters
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnsByRef As Boolean
                Get
                    Return _originalDefinition.ReturnsByRef
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnType As TypeSymbol
                Get
                    Return _originalDefinition.ReturnType
                End Get
            End Property

            Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _originalDefinition.ReturnTypeCustomModifiers
                End Get
            End Property

            Public Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
                Get
                    Return _originalDefinition.RefCustomModifiers
                End Get
            End Property

            Friend Overrides ReadOnly Property Syntax As SyntaxNode
                Get
                    Return _originalDefinition.Syntax
                End Get
            End Property

            Public Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
                Get
                    Return _originalDefinition.TypeArguments
                End Get
            End Property

            Public Overrides ReadOnly Property TypeParameters As ImmutableArray(Of TypeParameterSymbol)
                Get
                    Return _originalDefinition.TypeParameters
                End Get
            End Property

            Friend Overrides Function IsMetadataNewSlot(Optional ignoreInterfaceImplementationChanges As Boolean = False) As Boolean
                Return False
            End Function

            Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
                Get
                    Return _originalDefinition.GenerateDebugInfo
                End Get
            End Property

            Friend Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
                Throw ExceptionUtilities.Unreachable
            End Function

            Friend Overrides ReadOnly Property HasSetsRequiredMembers As Boolean
                Get
                    Return False
                End Get
            End Property
        End Class

        Private NotInheritable Class ReducedAccessorParameterSymbol
            Inherits ReducedParameterSymbolBase

            Public Shared Function MakeParameters(propertyOrAccessor As Symbol, originalParameters As ImmutableArray(Of ParameterSymbol)) As ImmutableArray(Of ParameterSymbol)
                Dim n = originalParameters.Length

                If n <= 1 Then
                    Debug.Assert(n = 1)
                    Return ImmutableArray(Of ParameterSymbol).Empty
                Else
                    Dim parameters(n - 2) As ParameterSymbol
                    For i = 0 To n - 2
                        parameters(i) = New ReducedAccessorParameterSymbol(propertyOrAccessor, originalParameters(i + 1))
                    Next
                    Return parameters.AsImmutableOrNull()
                End If
            End Function

            Private ReadOnly _propertyOrAccessor As Symbol

            Public Sub New(propertyOrAccessor As Symbol, underlyingParameter As ParameterSymbol)
                MyBase.New(underlyingParameter)
                _propertyOrAccessor = propertyOrAccessor
            End Sub

            Public Overrides ReadOnly Property ContainingSymbol As Symbol
                Get
                    Return _propertyOrAccessor
                End Get
            End Property

            Public Overrides ReadOnly Property Type As TypeSymbol
                Get
                    Return m_CurriedFromParameter.Type
                End Get
            End Property
        End Class
    End Class

End Namespace
