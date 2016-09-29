' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Samples.ImplementNotifyPropertyChangedVB

Friend Module CodeGeneration

    Friend Function ImplementINotifyPropertyChanged(
            root As CompilationUnitSyntax,
            model As SemanticModel,
            properties As IEnumerable(Of ExpandablePropertyInfo),
            Workspace As Workspace) As CompilationUnitSyntax

        Dim typeDeclaration = properties.First().PropertyDeclaration.FirstAncestorOrSelf(Of TypeBlockSyntax)
        Dim backingFieldLookup = properties.ToDictionary(Function(info) info.PropertyDeclaration, Function(info) info.BackingFieldName)
        Dim allProperties = properties.Select(Function(p) DirectCast(p.PropertyDeclaration, SyntaxNode)).Concat({typeDeclaration})

        root = root.ReplaceNodes(
            allProperties,
            Function(original, updated) ReplaceNode(original, updated, backingFieldLookup, properties, model, Workspace))

        Return root _
            .WithImport("System.Collections.Generic") _
            .WithImport("System.ComponentModel")
    End Function

    Private Function ReplaceNode(
        original As SyntaxNode,
        updated As SyntaxNode,
        backingFieldLookup As Dictionary(Of DeclarationStatementSyntax, String),
        properties As IEnumerable(Of ExpandablePropertyInfo),
        model As SemanticModel,
        workspace As Workspace) As SyntaxNode

        Return If(TypeOf original Is TypeBlockSyntax,
            ExpandType(DirectCast(original, TypeBlockSyntax),
                        DirectCast(updated, TypeBlockSyntax),
                        properties.Where(Function(p) p.NeedsBackingField),
                        model,
                        workspace),
            DirectCast(ExpandProperty(DirectCast(original, DeclarationStatementSyntax), backingFieldLookup(DirectCast(original, DeclarationStatementSyntax))), SyntaxNode))
    End Function

    <Extension>
    Private Function WithImport(root As CompilationUnitSyntax, name As String) As CompilationUnitSyntax
        If Not root.Imports _
            .SelectMany(Function(i) i.ImportsClauses) _
            .Any(Function(i) i.IsKind(SyntaxKind.SimpleImportsClause) AndAlso DirectCast(i, SimpleImportsClauseSyntax).Name.ToString() = name) Then

            Dim clause As ImportsClauseSyntax = SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName(name).NormalizeWhitespace(elasticTrivia:=True))
            Dim clauseList = SyntaxFactory.SeparatedList({clause})
            Dim statement = SyntaxFactory.ImportsStatement(clauseList)
            statement = statement.WithAdditionalAnnotations(Formatter.Annotation)

            root = root.AddImports(statement)
        End If

        Return root
    End Function

    Private Function ExpandProperty(propertyDeclaration As DeclarationStatementSyntax, backingFieldName As String) As SyntaxNode
        Dim getter As AccessorBlockSyntax = Nothing
        Dim setter As AccessorBlockSyntax = Nothing
        Dim propertyStatement As PropertyStatementSyntax = Nothing
        Dim propertyBlock As PropertyBlockSyntax = Nothing

        If propertyDeclaration.IsKind(SyntaxKind.PropertyStatement) Then
            propertyStatement = DirectCast(propertyDeclaration, PropertyStatementSyntax)
        ElseIf propertyDeclaration.IsKind(SyntaxKind.PropertyBlock) Then
            propertyBlock = DirectCast(propertyDeclaration, PropertyBlockSyntax)
            propertyStatement = propertyBlock.PropertyStatement

            If Not ExpansionChecker.TryGetAccessors(propertyBlock, getter, setter) Then
                Throw New ArgumentException()
            End If
        Else
            Debug.Fail("Unexpected declaration kind.")
        End If

        If getter Is Nothing Then
            getter = SyntaxFactory.AccessorBlock(SyntaxKind.GetAccessorBlock,
                                        SyntaxFactory.AccessorStatement(SyntaxKind.GetAccessorStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)),
                                        SyntaxFactory.EndBlockStatement(SyntaxKind.EndGetStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)))
        End If

        Dim returnFieldStatement = SyntaxFactory.ParseExecutableStatement(String.Format("Return {0}", backingFieldName))
            getter = getter.WithStatements(SyntaxFactory.SingletonList(returnFieldStatement))

            If setter Is Nothing Then
                Dim propertyTypeText = DirectCast(propertyStatement.AsClause, SimpleAsClauseSyntax).Type.ToString()
                Dim parameterList = SyntaxFactory.ParseParameterList(String.Format("(value As {0})", propertyTypeText))
                setter = SyntaxFactory.AccessorBlock(SyntaxKind.SetAccessorBlock,
                                            SyntaxFactory.AccessorStatement(SyntaxKind.SetAccessorStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)).
                                                                     WithParameterList(parameterList),
                                            SyntaxFactory.EndBlockStatement(SyntaxKind.EndSetStatement, SyntaxFactory.Token(SyntaxKind.SetKeyword)))
            End If

            Dim setPropertyStatement = SyntaxFactory.ParseExecutableStatement(String.Format("SetProperty({0}, value, ""{1}"")", backingFieldName, propertyStatement.Identifier.ValueText))
            setter = setter.WithStatements(SyntaxFactory.SingletonList(setPropertyStatement))

            Dim newPropertyBlock As PropertyBlockSyntax = propertyBlock
            If newPropertyBlock Is Nothing Then
                newPropertyBlock = SyntaxFactory.PropertyBlock(propertyStatement, SyntaxFactory.List(Of AccessorBlockSyntax)())
            End If

            newPropertyBlock = newPropertyBlock.WithAccessors(SyntaxFactory.List({getter, setter}))

            Return newPropertyBlock
    End Function

    Private Function ExpandType(
        original As TypeBlockSyntax,
        updated As TypeBlockSyntax,
        properties As IEnumerable(Of ExpandablePropertyInfo),
        model As SemanticModel,
        workspace As Workspace) As TypeBlockSyntax

        Debug.Assert(original IsNot updated)

        Return updated _
            .WithBackingFields(properties, workspace) _
            .WithBaseType(original, model) _
            .WithPropertyChangedEvent(original, model, workspace) _
            .WithSetPropertyMethod(original, model, workspace) _
            .NormalizeWhitespace(elasticTrivia:=True) _
            .WithAdditionalAnnotations(Formatter.Annotation)
    End Function

    <Extension>
    Private Function WithBackingFields(node As TypeBlockSyntax, properties As IEnumerable(Of ExpandablePropertyInfo), workspace As Workspace) As TypeBlockSyntax

        For Each propertyInfo In properties
            Dim newField = GenerateBackingField(propertyInfo, workspace)
            Dim currentProp = GetProperty(node, GetPropertyName(propertyInfo.PropertyDeclaration))
            node = node.InsertNodesBefore(currentProp, {newField})
        Next

        Return node
    End Function

    Private Function GetPropertyName(node As DeclarationStatementSyntax) As String
        Dim block = TryCast(node, PropertyBlockSyntax)
        If block IsNot Nothing Then
            Return block.PropertyStatement.Identifier.Text
        End If
        Dim prop = TryCast(node, PropertyStatementSyntax)
        If prop IsNot Nothing Then
            Return prop.Identifier.Text
        End If
        Return Nothing
    End Function

    Private Function GetProperty(node As TypeBlockSyntax, name As String) As DeclarationStatementSyntax
        Return node.DescendantNodes().OfType(Of DeclarationStatementSyntax).FirstOrDefault(Function(n) GetPropertyName(n) = name)
    End Function

    Private Function GenerateBackingField(propertyInfo As ExpandablePropertyInfo, workspace As Workspace) As StatementSyntax
        Dim g = SyntaxGenerator.GetGenerator(workspace, LanguageNames.VisualBasic)
        Dim fieldType = g.TypeExpression(propertyInfo.Type)

        Dim fieldDecl = DirectCast(ParseMember(String.Format("Private {0} As _fieldType_", propertyInfo.BackingFieldName)), FieldDeclarationSyntax)
        Return fieldDecl.ReplaceNode(fieldDecl.Declarators(0).AsClause.Type, fieldType).WithAdditionalAnnotations(Formatter.Annotation)
    End Function

    Private Function ParseMember(source As String) As StatementSyntax
        Dim cu = SyntaxFactory.ParseCompilationUnit("Class x" & vbCrLf & source & vbCrLf & "End Class")
        Return DirectCast(cu.Members(0), ClassBlockSyntax).Members(0)
    End Function

    <Extension>
    Private Function AddMembers(node As TypeBlockSyntax, ParamArray members As StatementSyntax()) As TypeBlockSyntax
        Return AddMembers(node, DirectCast(members, IEnumerable(Of StatementSyntax)))
    End Function

    <Extension>
    Private Function AddMembers(node As TypeBlockSyntax, members As IEnumerable(Of StatementSyntax)) As TypeBlockSyntax
        Dim classBlock = TryCast(node, ClassBlockSyntax)
        If classBlock IsNot Nothing Then
            Return classBlock.WithMembers(classBlock.Members.AddRange(members))
        End If

        Dim structBlock = TryCast(node, StructureBlockSyntax)
        If structBlock IsNot Nothing Then
            Return structBlock.WithMembers(structBlock.Members.AddRange(members))
        End If

        Return node
    End Function

    <Extension>
    Private Function WithBaseType(node As TypeBlockSyntax, original As TypeBlockSyntax, model As SemanticModel) As TypeBlockSyntax
        Dim classSymbol = DirectCast(model.GetDeclaredSymbol(original), INamedTypeSymbol)
        Dim interfaceSymbol = model.Compilation.GetTypeByMetadataName(InterfaceName)

        ' Does this class already implement INotifyPropertyChanged? If not, add it to the base list.
        If Not classSymbol.AllInterfaces.Any(Function(i) i.Equals(interfaceSymbol)) Then
            ' Add an annotation to simplify the name
            Dim baseTypeName = SyntaxFactory.ParseTypeName(InterfaceName) _
                .WithAdditionalAnnotations(Simplifier.Annotation)

            ' Add an annotation to format properly.
            Dim implementsStatement = SyntaxFactory.ImplementsStatement(baseTypeName).
                WithAdditionalAnnotations(Formatter.Annotation)

            node = If(node.IsKind(SyntaxKind.ClassBlock),
                DirectCast(DirectCast(node, ClassBlockSyntax).AddImplements(implementsStatement), TypeBlockSyntax),
                DirectCast(node, StructureBlockSyntax).AddImplements(implementsStatement))
        End If

        Return node
    End Function

    Private Const InterfaceName As String = "System.ComponentModel.INotifyPropertyChanged"

    <Extension>
    Private Function WithPropertyChangedEvent(node As TypeBlockSyntax, original As TypeBlockSyntax, model As SemanticModel, workspace As Workspace) As TypeBlockSyntax
        Dim classSymbol = DirectCast(model.GetDeclaredSymbol(original), INamedTypeSymbol)
        Dim interfaceSymbol = model.Compilation.GetTypeByMetadataName(InterfaceName)
        Dim propertyChangedEventSymbol = DirectCast(interfaceSymbol.GetMembers("PropertyChanged").Single(), IEventSymbol)
        Dim propertyChangedEvent = classSymbol.FindImplementationForInterfaceMember(propertyChangedEventSymbol)

        ' Does this class contain an implementation for the PropertyChanged event? If not, add it.
        If propertyChangedEvent Is Nothing Then
            node = AddMembers(node, GeneratePropertyChangedEvent())
        End If

        Return node
    End Function

    Friend Function GeneratePropertyChangedEvent() As StatementSyntax
        Dim decl = ParseMember("Public Event PropertyChanged As System.ComponentModel.PropertyChangedEventHandler Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged")
        Return decl.WithAdditionalAnnotations(Simplifier.Annotation)
    End Function

    <Extension>
    Private Function WithSetPropertyMethod(node As TypeBlockSyntax, original As TypeBlockSyntax, model As SemanticModel, workspace As Workspace) As TypeBlockSyntax
        Dim classSymbol = DirectCast(model.GetDeclaredSymbol(original), INamedTypeSymbol)
        Dim interfaceSymbol = model.Compilation.GetTypeByMetadataName(InterfaceName)
        Dim propertyChangedEventSymbol = DirectCast(interfaceSymbol.GetMembers("PropertyChanged").Single(), IEventSymbol)
        Dim propertyChangedEvent = classSymbol.FindImplementationForInterfaceMember(propertyChangedEventSymbol)

        Dim setPropertyMethod = classSymbol.FindSetPropertyMethod(model.Compilation)
        If setPropertyMethod Is Nothing Then
            node = AddMembers(node, GenerateSetPropertyMethod())
        End If

        Return node
    End Function

    Friend Function GenerateSetPropertyMethod() As StatementSyntax
        Return ParseMember(<x>
Private Sub SetProperty(Of T)(ByRef field As T, value As T, name As String)
    If Not EqualityComparer(Of T).Default.Equals(field, value) Then
        field = value
        RaiseEvent PropertyChanged(Me, New System.ComponentModel.PropertyChangedEventArgs(name))
    End If
End Sub
</x>.Value).WithAdditionalAnnotations(Simplifier.Annotation)

    End Function

    <Extension>
    Private Function FindSetPropertyMethod(classSymbol As INamedTypeSymbol, compilation As Compilation) As IMethodSymbol
        ' Find SetProperty(Of T)(ByRef T, T, string) method.
        Dim setPropertyMethod = classSymbol.
            GetMembers("SetProperty").OfType(Of IMethodSymbol)().
            FirstOrDefault(Function(m) m.Parameters.Count = 3 AndAlso m.TypeParameters.Count = 1)

        If setPropertyMethod IsNot Nothing Then
            Dim parameters = setPropertyMethod.Parameters
            Dim typeParameter = setPropertyMethod.TypeParameters(0)

            Dim stringType = compilation.GetSpecialType(SpecialType.System_String)

            If (setPropertyMethod.ReturnsVoid AndAlso
                parameters(0).RefKind = RefKind.Ref AndAlso
                parameters(0).Type.Equals(typeParameter) AndAlso
                parameters(1).Type.Equals(typeParameter) AndAlso
                parameters(2).Type.Equals(stringType)) Then

                Return setPropertyMethod
            End If
        End If

        Return Nothing
    End Function
End Module

