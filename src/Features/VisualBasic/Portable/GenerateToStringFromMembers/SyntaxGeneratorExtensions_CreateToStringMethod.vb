' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Simplification

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateToStringFromMembers
    Friend Module ToStringGenerator
        Private Const ToStringName As String = "ToString"
        Private Const BuilderVariableName As String = "builder"
        Private Const AppendName As String = "Append"

        ''' <summary>
        ''' Generates a standalone override of <see cref="Object.ToString()"/> that prints
        ''' the supplied members directly. For example, for
        ''' <c>Class Program : Dim a As Integer : End Class</c>:
        ''' <code>
        ''' Public Overrides Function ToString() As String
        '''     Dim builder = New StringBuilder()
        '''     builder.Append("Program")
        '''     builder.Append(" { ")
        '''     builder.Append("a = ")
        '''     builder.Append(a.ToString())
        '''     builder.Append("}"c)
        '''     Return builder.ToString()
        ''' End Function
        ''' </code>
        ''' </summary>
        Public Function CreateStandaloneToStringMethod(
            factory As SyntaxGenerator,
            generatorInternal As SyntaxGeneratorInternal,
            compilation As Compilation,
            containingType As INamedTypeSymbol,
            members As ImmutableArray(Of ISymbol)) As IMethodSymbol

            Dim statements = CreateStandaloneToStringMethodStatements(
                factory, generatorInternal, compilation, containingType, members)

            Return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes:=Nothing,
                accessibility:=Accessibility.Public,
                modifiers:=DeclarationModifiers.Override,
                returnType:=compilation.GetSpecialType(SpecialType.System_String),
                refKind:=RefKind.None,
                explicitInterfaceImplementations:=Nothing,
                name:=ToStringName,
                typeParameters:=Nothing,
                parameters:=Nothing,
                statements:=statements)
        End Function

        Private Function CreateStandaloneToStringMethodStatements(
            factory As SyntaxGenerator,
            generatorInternal As SyntaxGeneratorInternal,
            compilation As Compilation,
            containingType As INamedTypeSymbol,
            members As ImmutableArray(Of ISymbol)) As ImmutableArray(Of SyntaxNode)

            Dim stringBuilderType = compilation.GetTypeByMetadataName("System.Text.StringBuilder")
            Contract.ThrowIfNull(stringBuilderType)

            Dim statements = ImmutableArray.CreateBuilder(Of SyntaxNode)()
            Dim builderName = factory.IdentifierName(BuilderVariableName)

            ' Dim builder = New StringBuilder();
            statements.Add(SimpleLocalDeclarationStatement(
                factory, generatorInternal, stringBuilderType, BuilderVariableName,
                factory.ObjectCreationExpression(stringBuilderType)))

            ' builder.Append("<TypeName>");
            statements.Add(CreateAppendStringCall(factory, builderName, containingType.Name))

            ' builder.Append(" { ");
            statements.Add(CreateAppendStringCall(factory, builderName, " { "))

            statements.AddRange(CreateAppendMembersStatements(factory, compilation, builderName, members))

            ' builder.Append('}');
            statements.Add(CreateAppendCharCall(factory, builderName, "}"c))

            ' return builder.ToString();
            statements.Add(factory.ReturnStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(builderName, factory.IdentifierName(ToStringName)))))

            Return statements.ToImmutable()
        End Function

        ''' <summary>
        ''' The statements that print each member in the form
        ''' <c>"&lt;name&gt; = &lt;value&gt;"</c>, separated by <c>", "</c>.
        ''' </summary>
        Private Function CreateAppendMembersStatements(
            factory As SyntaxGenerator,
            compilation As Compilation,
            builderName As SyntaxNode,
            members As ImmutableArray(Of ISymbol)) As ImmutableArray(Of SyntaxNode)

            Dim statements = ImmutableArray.CreateBuilder(Of SyntaxNode)()
            For i = 0 To members.Length - 1
                Dim member = members(i)

                ' builder.Append(", <name> = "); // if previous members exist
                ' builder.Append("<name> = "); // if it is the first member
                Dim memberHeader = If(i > 0, ", ", "") + member.Name + " = "
                statements.Add(CreateAppendStringCall(factory, builderName, memberHeader))

                Dim memberAccess = factory.MemberAccessExpression(
                        factory.ThisExpression(), factory.IdentifierName(member.Name)).
                    WithAdditionalAnnotations(Simplifier.Annotation)

                ' builder.Append(<value>.ToString()); // for value types
                ' builder.Append((object)<value>); // otherwise
                '
                ' Value types are printed via ToString() as there is no
                ' StringBuilder.Append overload for arbitrary value types, while
                ' reference types go through Append(object).
                Dim memberType = member.GetSymbolType()
                If memberType IsNot Nothing AndAlso memberType.IsValueType Then
                    statements.Add(CreateAppendCall(factory, builderName,
                        factory.InvocationExpression(
                            factory.MemberAccessExpression(memberAccess, factory.IdentifierName(ToStringName)))))
                Else
                    statements.Add(CreateAppendCall(factory, builderName,
                        factory.CastExpression(compilation.GetSpecialType(SpecialType.System_Object), memberAccess)))
                End If
            Next

            Return statements.ToImmutable()
        End Function

        ''' <summary>
        ''' In VB it's more idiomatic to write things like <c>Dim t = TryCast(obj, SomeType)</c>
        ''' instead of <c>Dim t As SomeType = TryCast(obj, SomeType)</c>, so we just elide the type
        ''' from the decl. For C# we don't want to do this though. We want to always include the
        ''' type and let the simplifier decide if it should be <c>var</c> or not.
        ''' </summary>
        Private Function SimpleLocalDeclarationStatement(
            generator As SyntaxGenerator,
            generatorInternal As SyntaxGeneratorInternal,
            namedTypeSymbol As INamedTypeSymbol,
            name As String,
            initializer As SyntaxNode) As SyntaxNode

            If generatorInternal.RequiresLocalDeclarationType() Then
                Return generator.LocalDeclarationStatement(namedTypeSymbol, name, initializer)
            Else
                Return generator.LocalDeclarationStatement(name, initializer)
            End If
        End Function

        Private Function CreateAppendStringCall(factory As SyntaxGenerator, builderName As SyntaxNode, value As String) As SyntaxNode
            Return factory.ExpressionStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(builderName, AppendName),
                    factory.LiteralExpression(value)))
        End Function

        Private Function CreateAppendCharCall(factory As SyntaxGenerator, builderName As SyntaxNode, value As Char) As SyntaxNode
            Return factory.ExpressionStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(builderName, AppendName),
                    factory.LiteralExpression(value)))
        End Function

        Private Function CreateAppendCall(factory As SyntaxGenerator, builderName As SyntaxNode, value As SyntaxNode) As SyntaxNode
            Return factory.ExpressionStatement(
                factory.InvocationExpression(
                    factory.MemberAccessExpression(builderName, AppendName),
                    value))
        End Function
    End Module
End Namespace
