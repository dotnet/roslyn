' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class LocalRewriter

        Public Overrides Function VisitXmlComment(node As BoundXmlComment) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlDocument(node As BoundXmlDocument) As BoundNode
            If Me._inExpressionLambda AndAlso Not node.HasErrors Then
                Return VisitXmlContainerInExpressionLambda(node.RewriterInfo)
            Else
                Return VisitXmlContainer(node.RewriterInfo)
            End If
        End Function

        Public Overrides Function VisitXmlDeclaration(node As BoundXmlDeclaration) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlProcessingInstruction(node As BoundXmlProcessingInstruction) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlAttribute(node As BoundXmlAttribute) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlElement(node As BoundXmlElement) As BoundNode
            Dim rewriterInfo = node.RewriterInfo
            Dim previousImportedNamespaces = Me._xmlImportedNamespaces

            If rewriterInfo.IsRoot Then
                Debug.Assert(Not rewriterInfo.ImportedNamespaces.IsDefault)
                Me._xmlImportedNamespaces = rewriterInfo.ImportedNamespaces
            End If

            Dim result As BoundNode
            If Me._inExpressionLambda AndAlso Not node.HasErrors Then
                result = VisitXmlContainerInExpressionLambda(rewriterInfo)
            Else
                result = VisitXmlContainer(rewriterInfo)
            End If

            If rewriterInfo.IsRoot Then
                Me._xmlImportedNamespaces = previousImportedNamespaces
            End If

            Return result
        End Function

        Public Overrides Function VisitXmlEmbeddedExpression(node As BoundXmlEmbeddedExpression) As BoundNode
            Return Visit(node.Expression)
        End Function

        Public Overrides Function VisitXmlMemberAccess(node As BoundXmlMemberAccess) As BoundNode
            Return Visit(node.MemberAccess)
        End Function

        Public Overrides Function VisitXmlName(node As BoundXmlName) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlNamespace(node As BoundXmlNamespace) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Public Overrides Function VisitXmlCData(node As BoundXmlCData) As BoundNode
            Return Visit(node.ObjectCreation)
        End Function

        Private Function VisitXmlContainer(rewriterInfo As BoundXmlContainerRewriterInfo) As BoundExpression
            Debug.Assert(Not Me._inExpressionLambda)

            Dim expr = VisitExpressionNode(rewriterInfo.ObjectCreation)

            If rewriterInfo.SideEffects.Length = 0 Then
                Debug.Assert(rewriterInfo.XmlnsAttributesPlaceholder Is Nothing)
                Return expr
            End If

            Dim syntax = expr.Syntax
            Dim type = expr.Type

            Dim locals = ArrayBuilder(Of LocalSymbol).GetInstance()
            Dim sideEffects = ArrayBuilder(Of BoundExpression).GetInstance()

            Dim local = CreateTempLocal(syntax, type, expr, sideEffects)
            locals.Add(local.LocalSymbol)
            AddPlaceholderReplacement(rewriterInfo.Placeholder, local)

            Dim attributes As BoundLocal = Nothing

            If rewriterInfo.XmlnsAttributesPlaceholder IsNot Nothing Then
                attributes = CreateTempLocal(syntax, rewriterInfo.XmlnsAttributesPlaceholder.Type, VisitExpressionNode(rewriterInfo.XmlnsAttributes), sideEffects)
                locals.Add(attributes.LocalSymbol)
                AddPlaceholderReplacement(rewriterInfo.XmlnsAttributesPlaceholder, attributes)
            End If

            If rewriterInfo.PrefixesPlaceholder IsNot Nothing Then
                Dim prefixesArray As BoundExpression = Nothing
                Dim namespacesArray As BoundExpression = Nothing
                CreatePrefixesAndNamespacesArrays(rewriterInfo, syntax, prefixesArray, namespacesArray)

                Dim prefixes = CreateTempLocal(syntax, rewriterInfo.PrefixesPlaceholder.Type, prefixesArray, sideEffects)
                locals.Add(prefixes.LocalSymbol)
                AddPlaceholderReplacement(rewriterInfo.PrefixesPlaceholder, prefixes)

                Dim namespaces = CreateTempLocal(syntax, rewriterInfo.NamespacesPlaceholder.Type, namespacesArray, sideEffects)
                locals.Add(namespaces.LocalSymbol)
                AddPlaceholderReplacement(rewriterInfo.NamespacesPlaceholder, namespaces)
            End If

            VisitList(rewriterInfo.SideEffects, sideEffects)

            If rewriterInfo.PrefixesPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(rewriterInfo.PrefixesPlaceholder)
                RemovePlaceholderReplacement(rewriterInfo.NamespacesPlaceholder)
            End If

            If rewriterInfo.XmlnsAttributesPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(rewriterInfo.XmlnsAttributesPlaceholder)
            End If

            RemovePlaceholderReplacement(rewriterInfo.Placeholder)

            Return New BoundSequence(syntax,
                                     locals:=locals.ToImmutableAndFree(),
                                     sideEffects:=sideEffects.ToImmutableAndFree(),
                                     valueOpt:=local,
                                     type:=type)
        End Function

        ''' <summary>
        ''' Create a temp local of the given type and initial value.
        ''' The resulting local is treated as an rvalue, and the
        ''' initialization assignment is added to 'sideEffects'.
        ''' </summary>
        Private Function CreateTempLocal(syntax As SyntaxNode, type As TypeSymbol, expr As BoundExpression, sideEffects As ArrayBuilder(Of BoundExpression)) As BoundLocal
            Dim local = New BoundLocal(syntax, New SynthesizedLocal(Me._currentMethodOrLambda, type, SynthesizedLocalKind.LoweringTemp), type)
            sideEffects.Add(New BoundAssignmentOperator(syntax, local, expr, suppressObjectClone:=True, type:=type))
            Return local.MakeRValue()
        End Function

        Private Function VisitXmlContainerInExpressionLambda(rewriterInfo As BoundXmlContainerRewriterInfo) As BoundExpression
            Debug.Assert(Me._inExpressionLambda)

            ' NOTE: When we convert Xml container expression in context of expression lambda we rewrite it into 
            '       one of the following: New XElement(XName, Object), New XElement(XName, Object()) or
            '       New XDocument(XName, Object()) where the second parameter, an object or object array, contains all 
            '       expressions from rewriterInfo.SideEffects (which are supposed to be in a form of <l>.Add(<e>));
            '
            '       In case there are any extra locals introduced by rewriterInfo.XmlnsAttributes, rewriterInfo.Prefixes
            '       or rewriterInfo.Namespaces, those need to be put into preamble for the method so that they are 
            '       initialized outside expression tree lambda and captured properly

            Dim origSideEffects As ImmutableArray(Of BoundExpression) = rewriterInfo.SideEffects

            Debug.Assert(rewriterInfo.ObjectCreation.Kind = BoundKind.ObjectCreationExpression)
            Dim objCreation = DirectCast(rewriterInfo.ObjectCreation, BoundObjectCreationExpression)

            ' if there are no side effects just rewrite object creation expression
            If origSideEffects.Length = 0 Then
                Debug.Assert(rewriterInfo.XmlnsAttributesPlaceholder Is Nothing)
                Return VisitExpressionNode(objCreation)
            End If

            Dim origArgument As BoundExpression = objCreation.Arguments(0)

            Debug.Assert(objCreation.InitializerOpt Is Nothing)
            Debug.Assert(objCreation.ConstructorOpt IsNot Nothing)

            ' NOTE: if we are here, original bound node does not have errors, thus we 
            '       can assume the symbols used in this node are OK
            Dim constructor As MethodSymbol = Nothing
            If objCreation.Arguments.Length = 1 Then
                ' This is a branch where XElement::.ctor(XName) lands, we need to get XElement::.ctor(XName, Object()) 
                Debug.Assert(TypeSymbol.Equals(objCreation.ConstructorOpt.ContainingType, Me.Compilation.GetWellKnownType(WellKnownType.System_Xml_Linq_XElement), TypeCompareKind.ConsiderEverything))

                constructor = DirectCast(Me.Compilation.GetWellKnownTypeMember(If(origSideEffects.Length = 1,
                                                                                  WellKnownMember.System_Xml_Linq_XElement__ctor,
                                                                                  WellKnownMember.System_Xml_Linq_XElement__ctor2)), MethodSymbol)

                If ReportMissingOrBadRuntimeHelper(objCreation, WellKnownMember.System_Xml_Linq_XElement__ctor2, constructor) Then
                    Return VisitExpressionNode(objCreation)
                End If

            Else
                ' This is a branch where XDocument::.ctor(XDeclaration, Object()) lands
                Debug.Assert(objCreation.Arguments.Length = 2)
                constructor = objCreation.ConstructorOpt
                Debug.Assert(TypeSymbol.Equals(constructor.ContainingType, Me.Compilation.GetWellKnownType(WellKnownType.System_Xml_Linq_XDocument), TypeCompareKind.ConsiderEverything))
            End If

            Dim syntax = objCreation.Syntax
            Dim type = objCreation.Type

            ' NOTE: rewriterInfo.Placeholder should never be used in rewriting below, because it is supposed
            '       to be on the receiver side of side-effect calls and we never rewrite those
            'AddPlaceholderReplacement(rewriterInfo.Placeholder, local)

            Dim attributes As BoundLocal = Nothing

            If rewriterInfo.XmlnsAttributesPlaceholder IsNot Nothing Then
                attributes = CreateTempLocalInExpressionLambda(syntax, rewriterInfo.XmlnsAttributesPlaceholder.Type, VisitExpressionNode(rewriterInfo.XmlnsAttributes))
                AddPlaceholderReplacement(rewriterInfo.XmlnsAttributesPlaceholder, attributes)
            End If

            If rewriterInfo.PrefixesPlaceholder IsNot Nothing Then
                Dim prefixesArray As BoundExpression = Nothing
                Dim namespacesArray As BoundExpression = Nothing
                CreatePrefixesAndNamespacesArrays(rewriterInfo, syntax, prefixesArray, namespacesArray)

                Dim prefixes = CreateTempLocalInExpressionLambda(syntax, rewriterInfo.PrefixesPlaceholder.Type, prefixesArray)
                AddPlaceholderReplacement(rewriterInfo.PrefixesPlaceholder, prefixes)

                Dim namespaces = CreateTempLocalInExpressionLambda(syntax, rewriterInfo.NamespacesPlaceholder.Type, namespacesArray)
                AddPlaceholderReplacement(rewriterInfo.NamespacesPlaceholder, namespaces)
            End If

            Dim rewrittenCallArguments(origSideEffects.Length - 1) As BoundExpression
            For i = 0 To origSideEffects.Length - 1
                Debug.Assert(origSideEffects(i).Kind = BoundKind.Call)
                Dim [call] = DirectCast(origSideEffects(i), BoundCall)
                Debug.Assert([call].Arguments.Length = 1)
                rewrittenCallArguments(i) = VisitExpressionNode([call].Arguments(0))
            Next

            If rewriterInfo.PrefixesPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(rewriterInfo.PrefixesPlaceholder)
                RemovePlaceholderReplacement(rewriterInfo.NamespacesPlaceholder)
            End If

            If rewriterInfo.XmlnsAttributesPlaceholder IsNot Nothing Then
                RemovePlaceholderReplacement(rewriterInfo.XmlnsAttributesPlaceholder)
            End If

            ' See comment above
            'RemovePlaceholderReplacement(rewriterInfo.Placeholder)

            Dim secondArgumentType As TypeSymbol = constructor.Parameters(1).Type
            Dim secondArgument As BoundExpression
            If secondArgumentType.IsArrayType Then
                ' Second parameter is an object array
                Dim arrayType = DirectCast(secondArgumentType, ArrayTypeSymbol)
                secondArgument = New BoundArrayCreation(
                                        objCreation.Syntax,
                                        ImmutableArray.Create(Of BoundExpression)(
                                            New BoundLiteral(
                                                objCreation.Syntax,
                                                ConstantValue.Create(rewrittenCallArguments.Length),
                                                Me.GetSpecialType(SpecialType.System_Int32))),
                                        New BoundArrayInitialization(
                                            objCreation.Syntax, rewrittenCallArguments.AsImmutableOrNull, arrayType),
                                        arrayType)

            Else
                ' Second parameter is an object; there must be only one argument
                Debug.Assert(rewrittenCallArguments.Length = 1)
                secondArgument = rewrittenCallArguments(0)
            End If

            Return objCreation.Update(constructor,
                                      ImmutableArray.Create(Of BoundExpression)(
                                          VisitExpression(origArgument), secondArgument),
                                      defaultArguments:=Nothing,
                                      Nothing,
                                      objCreation.Type)
        End Function

        Private Function CreateTempLocalInExpressionLambda(syntax As SyntaxNode, type As TypeSymbol, expr As BoundExpression) As BoundLocal
            Dim local As New SynthesizedLocal(Me._topMethod, type, SynthesizedLocalKind.XmlInExpressionLambda, Me._currentMethodOrLambda.Syntax)
            Dim boundLocal = New BoundLocal(syntax, local, type)
            Me._xmlFixupData.AddLocal(local, New BoundAssignmentOperator(syntax, boundLocal, expr, suppressObjectClone:=True, type:=type))
            Return boundLocal.MakeRValue()
        End Function

        Private Sub CreatePrefixesAndNamespacesArrays(
                                                         rewriterInfo As BoundXmlContainerRewriterInfo,
                                                         syntax As SyntaxNode,
                                                         <Out()> ByRef prefixes As BoundExpression,
                                                         <Out()> ByRef namespaces As BoundExpression)

            Dim prefixesBuilder = ArrayBuilder(Of BoundExpression).GetInstance()
            Dim namespacesBuilder = ArrayBuilder(Of BoundExpression).GetInstance()

            For Each pair In Me._xmlImportedNamespaces
                prefixesBuilder.Add(CreateCompilerGeneratedXmlnsPrefix(syntax, pair.Key))
                namespacesBuilder.Add(CreateCompilerGeneratedXmlNamespace(syntax, pair.Value))
            Next

            For Each pair In rewriterInfo.InScopeXmlNamespaces
                prefixesBuilder.Add(CreateCompilerGeneratedXmlnsPrefix(syntax, pair.Key))
                namespacesBuilder.Add(CreateCompilerGeneratedXmlNamespace(syntax, pair.Value))
            Next

            prefixes = VisitExpressionNode(CreateCompilerGeneratedArray(syntax, rewriterInfo.PrefixesPlaceholder.Type, prefixesBuilder.ToImmutableAndFree()))
            namespaces = VisitExpressionNode(CreateCompilerGeneratedArray(syntax, rewriterInfo.NamespacesPlaceholder.Type, namespacesBuilder.ToImmutableAndFree()))
        End Sub

        Private Function BindXmlNamespace(syntax As SyntaxNode, [namespace] As BoundExpression) As BoundExpression
            ' XNamespace.Get must exist since we only add XNamespaces in
            ' lowering if corresponding XNamespaces were created when binding
            ' the containing XElement. (See Binder.BindXmlContainerRewriterInfo
            ' where we add xmlns attributes for any prefixes required from Imports.)
            Dim method = DirectCast(Compilation.GetWellKnownTypeMember(WellKnownMember.System_Xml_Linq_XNamespace__Get), MethodSymbol)
            Return New BoundCall(syntax,
                                     method,
                                     methodGroupOpt:=Nothing,
                                     receiverOpt:=Nothing,
                                     arguments:=ImmutableArray.Create([namespace]),
                                     constantValueOpt:=Nothing,
                                     type:=method.ReturnType).MakeCompilerGenerated()
        End Function

        Private Function CreateStringLiteral(syntax As SyntaxNode, str As String) As BoundLiteral
            Return New BoundLiteral(syntax, ConstantValue.Create(str), GetSpecialType(SpecialType.System_String)).MakeCompilerGenerated()
        End Function

        Private Function CreateCompilerGeneratedXmlnsPrefix(syntax As SyntaxNode, prefix As String) As BoundExpression
            Return CreateStringLiteral(syntax, If(prefix = StringConstants.DefaultXmlnsPrefix, StringConstants.XmlnsPrefix, prefix))
        End Function

        Private Function CreateCompilerGeneratedXmlNamespace(syntax As SyntaxNode, [namespace] As String) As BoundExpression
            Return BindXmlNamespace(syntax, CreateStringLiteral(syntax, [namespace]))
        End Function

        ''' <summary>
        ''' Create a BoundExpression representing an array creation initialized with the given items.
        ''' If there are zero items, the result is a BoundLiteral Nothing, otherwise, a BoundArrayCreation.
        ''' </summary>
        Private Function CreateCompilerGeneratedArray(
                                                     syntax As SyntaxNode,
                                                     arrayType As TypeSymbol,
                                                     items As ImmutableArray(Of BoundExpression)) As BoundExpression
            Dim result As BoundExpression

            If items.Length = 0 Then
                result = New BoundLiteral(syntax, ConstantValue.Nothing, arrayType)
            Else
                Dim size = (New BoundLiteral(syntax, ConstantValue.Create(items.Length), GetSpecialType(SpecialType.System_Int32))).MakeCompilerGenerated()
                Dim initializer = (New BoundArrayInitialization(syntax, items, arrayType)).MakeCompilerGenerated()
                result = New BoundArrayCreation(syntax, ImmutableArray.Create(Of BoundExpression)(size), initializer, arrayType)
            End If

            result.SetWasCompilerGenerated()
            Return result
        End Function

    End Class
End Namespace
