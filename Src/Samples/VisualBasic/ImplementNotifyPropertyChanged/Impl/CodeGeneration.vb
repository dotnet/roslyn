' *********************************************************
'
' Copyright © Microsoft Corporation
'
' Licensed under the Apache License, Version 2.0 (the
' "License"); you may not use this file except in
' compliance with the License. You may obtain a copy of
' the License at
'
' http://www.apache.org/licenses/LICENSE-2.0 
'
' THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
' OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
' INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
' OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
' PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
'
' See the Apache 2 License for the specific language
' governing permissions and limitations under the License.
'
' *********************************************************

Imports System.Runtime.CompilerServices
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
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
            .Any(Function(i) i.IsKind(SyntaxKind.MembersImportsClause) AndAlso DirectCast(i, MembersImportsClauseSyntax).Name.ToString() = name) Then

            Dim clause As ImportsClauseSyntax = SyntaxFactory.MembersImportsClause(SyntaxFactory.ParseName(name).NormalizeWhitespace(elasticTrivia:=True))
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
            getter = SyntaxFactory.AccessorBlock(SyntaxKind.PropertyGetBlock,
                                        SyntaxFactory.AccessorStatement(SyntaxKind.GetAccessorStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)),
                                        SyntaxFactory.EndBlockStatement(SyntaxKind.EndGetStatement, SyntaxFactory.Token(SyntaxKind.GetKeyword)))
        End If

        Dim returnFieldStatement = SyntaxFactory.ParseExecutableStatement(String.Format("Return {0}", backingFieldName))
        getter = getter.WithStatements(SyntaxFactory.SingletonList(returnFieldStatement))

        If setter Is Nothing Then
            Dim propertyTypeText = DirectCast(propertyStatement.AsClause, SimpleAsClauseSyntax).Type.ToString()
            Dim parameterList = SyntaxFactory.ParseParameterList(String.Format("(value As {0})", propertyTypeText))
            setter = SyntaxFactory.AccessorBlock(SyntaxKind.PropertySetBlock,
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
            Dim newField = CodeGenerationSymbolFactory.CreateFieldSymbol(
                attributes:=Nothing,
                accessibility:=Microsoft.CodeAnalysis.Accessibility.Private,
                modifiers:=New SymbolModifiers(),
                type:=propertyInfo.Type,
                name:=propertyInfo.BackingFieldName)

            node = CodeGenerator.AddFieldDeclaration(node, newField, workspace)
        Next

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

            ' Add an annoatation to format properly.
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
            node = CodeGenerator.AddEventDeclaration(
                node,
                GeneratePropertyChangedEvent(model.Compilation),
                workspace)
        End If

        Return node
    End Function

    Friend Function GeneratePropertyChangedEvent(compilation As Compilation) As IEventSymbol
        Dim notifyPropertyChangedInterface = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged")
        Dim propertyChangedEventHandlerType = compilation.GetTypeByMetadataName("System.ComponentModel.PropertyChangedEventHandler")

        Return CodeGenerationSymbolFactory.CreateEventSymbol(
            attributes:=Nothing,
            accessibility:=Microsoft.CodeAnalysis.Accessibility.Public,
            modifiers:=New SymbolModifiers(),
            type:=propertyChangedEventHandlerType,
            explicitInterfaceSymbol:=DirectCast(notifyPropertyChangedInterface.GetMembers("PropertyChanged").Single(), IEventSymbol),
            name:="PropertyChanged")
    End Function

    <Extension>
    Private Function WithSetPropertyMethod(node As TypeBlockSyntax, original As TypeBlockSyntax, model As SemanticModel, workspace As Workspace) As TypeBlockSyntax
        Dim classSymbol = DirectCast(model.GetDeclaredSymbol(original), INamedTypeSymbol)
        Dim interfaceSymbol = model.Compilation.GetTypeByMetadataName(InterfaceName)
        Dim propertyChangedEventSymbol = DirectCast(interfaceSymbol.GetMembers("PropertyChanged").Single(), IEventSymbol)
        Dim propertyChangedEvent = classSymbol.FindImplementationForInterfaceMember(propertyChangedEventSymbol)

        Dim setPropertyMethod = classSymbol.FindSetPropertyMethod(model.Compilation)
        If setPropertyMethod Is Nothing Then
            node = CodeGenerator.AddMethodDeclaration(
                node,
                GenerateSetPropertyMethod(model.Compilation),
                workspace)
        End If

        Return node
    End Function

    Friend Function GenerateSetPropertyMethod(compilation As Compilation) As IMethodSymbol
        Dim body = SyntaxFactory.ParseExecutableStatement(
"If Not EqualityComparer(Of T).Default.Equals(field, value) Then" & vbCrLf &
"    field = value" & vbCrLf &
"" & vbCrLf &
"    RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(name))" & vbCrLf &
"End If")

        body = body.WithAdditionalAnnotations(Simplifier.Annotation)

        Dim stringType = compilation.GetSpecialType(SpecialType.System_String)
        Dim voidType = compilation.GetSpecialType(SpecialType.System_Void)

        Dim typeParameter = CodeGenerationSymbolFactory.CreateTypeParameterSymbol("T")

        Dim parameter1 = CodeGenerationSymbolFactory.CreateParameterSymbol(
            attributes:=Nothing,
            refKind:=RefKind.Ref,
            isParams:=False,
            type:=typeParameter,
            name:="field")

        Dim parameter2 = CodeGenerationSymbolFactory.CreateParameterSymbol(typeParameter, "value")
        Dim parameter3 = CodeGenerationSymbolFactory.CreateParameterSymbol(stringType, "name")

        Return CodeGenerationSymbolFactory.CreateMethodSymbol(
            attributes:=Nothing,
            accessibility:=Microsoft.CodeAnalysis.Accessibility.Private,
            modifiers:=New SymbolModifiers(),
            returnType:=voidType,
            explicitInterfaceSymbol:=Nothing,
            name:="SetProperty",
            typeParameters:={typeParameter},
            parameters:={parameter1, parameter2, parameter3},
            statements:={body})

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
