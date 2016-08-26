' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    <ExportLanguageServiceFactory(GetType(BlockStructureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicBlockStructureServiceFactory
        Implements ILanguageServiceFactory

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicBlockStructureService(languageServices.WorkspaceServices.Workspace)
        End Function
    End Class

    Friend Class VisualBasicBlockStructureService
        Inherits BlockStructureServiceWithProviders

        Friend Sub New(workspace As Workspace)
            MyBase.New(workspace)
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of BlockStructureProvider)
            Return ImmutableArray.Create(Of BlockStructureProvider)(New VisualBasicBlockStructureProvider())
        End Function
    End Class

    Friend Class VisualBasicBlockStructureProvider
        Inherits AbstractBlockStructureProvider

        Private Shared ReadOnly s_defaultNodeOutlinerMap As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider)) = CreateDefaultNodeOutlinerMap()
        Private Shared ReadOnly s_defaultTriviaOutlinerMap As ImmutableDictionary(Of Integer, ImmutableArray(Of AbstractSyntaxStructureProvider)) = CreateDefaultTriviaOutlinerMap()

        Public Shared Function CreateDefaultNodeOutlinerMap() As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))()

            builder.Add(Of AccessorStatementSyntax, AccessorDeclarationOutliner)()
            builder.Add(Of ClassStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of CompilationUnitSyntax, CompilationUnitOutliner)()
            builder.Add(Of SubNewStatementSyntax, ConstructorDeclarationOutliner, MetadataAsSource.ConstructorDeclarationOutliner)()
            builder.Add(Of DelegateStatementSyntax, DelegateDeclarationOutliner, MetadataAsSource.DelegateDeclarationOutliner)()
            builder.Add(Of DocumentationCommentTriviaSyntax, DocumentationCommentOutliner)()
            builder.Add(Of EnumStatementSyntax, EnumDeclarationOutliner, MetadataAsSource.EnumDeclarationOutliner)()
            builder.Add(Of EnumMemberDeclarationSyntax, MetadataAsSource.EnumMemberDeclarationOutliner)()
            builder.Add(Of EventStatementSyntax, EventDeclarationOutliner, MetadataAsSource.EventDeclarationOutliner)()
            builder.Add(Of DeclareStatementSyntax, ExternalMethodDeclarationOutliner)()
            builder.Add(Of FieldDeclarationSyntax, FieldDeclarationOutliner, MetadataAsSource.FieldDeclarationOutliner)()
            builder.Add(Of InterfaceStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of MethodStatementSyntax, MethodDeclarationOutliner, MetadataAsSource.MethodDeclarationOutliner)()
            builder.Add(Of ModuleStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of MultiLineLambdaExpressionSyntax, MultilineLambdaOutliner)()
            builder.Add(Of NamespaceStatementSyntax, NamespaceDeclarationOutliner)()
            builder.Add(Of OperatorStatementSyntax, OperatorDeclarationOutliner, MetadataAsSource.OperatorDeclarationOutliner)()
            builder.Add(Of PropertyStatementSyntax, PropertyDeclarationOutliner, MetadataAsSource.PropertyDeclarationOutliner)()
            builder.Add(Of RegionDirectiveTriviaSyntax, RegionDirectiveOutliner, MetadataAsSource.RegionDirectiveOutliner)()
            builder.Add(Of StructureStatementSyntax, TypeDeclarationOutliner, MetadataAsSource.TypeDeclarationOutliner)()
            builder.Add(Of XmlCDataSectionSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlCommentSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlDocumentSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlElementSyntax, XmlExpressionOutliner)()
            builder.Add(Of XmlProcessingInstructionSyntax, XmlExpressionOutliner)()

            Return builder.ToImmutable()
        End Function

        Public Shared Function CreateDefaultTriviaOutlinerMap() As ImmutableDictionary(Of Integer, ImmutableArray(Of AbstractSyntaxStructureProvider))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Integer, ImmutableArray(Of AbstractSyntaxStructureProvider))()

            builder.Add(SyntaxKind.DisabledTextTrivia, ImmutableArray.Create(Of AbstractSyntaxStructureProvider)(New DisabledTextTriviaOutliner()))

            Return builder.ToImmutable()
        End Function

        Friend Sub New()
            MyBase.New(s_defaultNodeOutlinerMap, s_defaultTriviaOutlinerMap)
        End Sub
    End Class
End Namespace