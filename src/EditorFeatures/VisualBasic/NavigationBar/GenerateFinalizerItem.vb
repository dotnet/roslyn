' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Editing

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.NavigationBar
    Friend Class GenerateFinalizerItem
        Inherits AbstractGenerateCodeItem

        Private ReadOnly _destinationTypeSymbolKey As SymbolKey

        Public Sub New(destinationTypeSymbolKey As SymbolKey)
            MyBase.New(WellKnownMemberNames.DestructorName, Glyph.MethodProtected)

            _destinationTypeSymbolKey = destinationTypeSymbolKey
        End Sub

        Protected Overrides ReadOnly Property ApplyLineAdjustmentFormattingRule As Boolean
            Get
                Return False
            End Get
        End Property

        Protected Overrides Async Function GetGeneratedDocumentCoreAsync(document As Document, codeGenerationOptions As CodeGenerationOptions, cancellationToken As CancellationToken) As Task(Of Document)
            Dim compilation = Await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(False)
            Dim destinationType = TryCast(_destinationTypeSymbolKey.Resolve(compilation).Symbol, INamedTypeSymbol)

            If destinationType Is Nothing Then
                Return Nothing
            End If

            Dim syntaxFactory = document.GetLanguageService(Of SyntaxGenerator)()
            Dim finalizeCall =
                syntaxFactory.ExpressionStatement(
                    syntaxFactory.InvocationExpression(
                        syntaxFactory.MemberAccessExpression(
                            syntaxFactory.BaseExpression(),
                            syntaxFactory.IdentifierName(WellKnownMemberNames.DestructorName))))

            Dim finalizerMethodSymbol = CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes:=Nothing,
                accessibility:=Accessibility.Protected,
                modifiers:=New DeclarationModifiers(isOverride:=True),
                returnType:=compilation.GetSpecialType(SpecialType.System_Void),
                refKind:=RefKind.None,
                explicitInterfaceImplementations:=Nothing,
                name:=WellKnownMemberNames.DestructorName,
                typeParameters:=Nothing,
                parameters:=ImmutableArray(Of IParameterSymbol).Empty,
                statements:=ImmutableArray.Create(finalizeCall))

            finalizerMethodSymbol = GeneratedSymbolAnnotation.AddAnnotationToSymbol(finalizerMethodSymbol)

            Return Await CodeGenerator.AddMethodDeclarationAsync(document.Project.Solution,
                                                                 destinationType,
                                                                 finalizerMethodSymbol,
                                                                 codeGenerationOptions,
                                                                 cancellationToken).ConfigureAwait(False)
        End Function
    End Class
End Namespace
