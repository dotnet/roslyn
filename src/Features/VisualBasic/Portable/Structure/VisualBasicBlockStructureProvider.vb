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

        <ImportingConstructor>
        Public Sub New()
        End Sub

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

        Public Shared Function CreateDefaultNodeStructureProviderMap() As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))()

            builder.Add(Of AccessorStatementSyntax, AccessorDeclarationStructureProvider)()
            builder.Add(Of ClassStatementSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider)()
            builder.Add(Of CollectionInitializerSyntax, CollectionInitializerStructureProvider)
            builder.Add(Of CompilationUnitSyntax, CompilationUnitStructureProvider)()
            builder.Add(Of SubNewStatementSyntax, ConstructorDeclarationStructureProvider, MetadataAsSource.MetadataConstructorDeclarationStructureProvider)()
            builder.Add(Of DelegateStatementSyntax, DelegateDeclarationStructureProvider, MetadataAsSource.MetadataDelegateDeclarationStructureProvider)()
            builder.Add(Of DocumentationCommentTriviaSyntax, DocumentationCommentStructureProvider)()
            builder.Add(Of DoLoopBlockSyntax, DoLoopBlockStructureProvider)
            builder.Add(Of EnumStatementSyntax, EnumDeclarationStructureProvider, MetadataAsSource.MetadataEnumDeclarationStructureProvider)()
            builder.Add(Of EnumMemberDeclarationSyntax, MetadataAsSource.MetadataEnumMemberDeclarationStructureProvider)()
            builder.Add(Of EventStatementSyntax, EventDeclarationStructureProvider, MetadataAsSource.MetadataEventDeclarationStructureProvider)()
            builder.Add(Of DeclareStatementSyntax, ExternalMethodDeclarationStructureProvider)()
            builder.Add(Of FieldDeclarationSyntax, FieldDeclarationStructureProvider, MetadataAsSource.MetadataFieldDeclarationStructureProvider)()
            builder.Add(Of ForBlockSyntax, ForBlockStructureProvider)
            builder.Add(Of ForEachBlockSyntax, ForEachBlockStructureProvider)
            builder.Add(Of InterfaceStatementSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider)()
            builder.Add(Of MethodStatementSyntax, MethodDeclarationStructureProvider, MetadataAsSource.MetadataMethodDeclarationStructureProvider)()
            builder.Add(Of ModuleStatementSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider)()
            builder.Add(Of MultiLineIfBlockSyntax, MultiLineIfBlockStructureProvider)()
            builder.Add(Of MultiLineLambdaExpressionSyntax, MultilineLambdaStructureProvider)()
            builder.Add(Of NamespaceStatementSyntax, NamespaceDeclarationStructureProvider)()
            builder.Add(Of ObjectCollectionInitializerSyntax, ObjectCreationInitializerStructureProvider)
            builder.Add(Of ObjectMemberInitializerSyntax, ObjectCreationInitializerStructureProvider)
            builder.Add(Of OperatorStatementSyntax, OperatorDeclarationStructureProvider, MetadataAsSource.MetadataOperatorDeclarationStructureProvider)()
            builder.Add(Of PropertyStatementSyntax, PropertyDeclarationStructureProvider, MetadataAsSource.MetadataPropertyDeclarationStructureProvider)()
            builder.Add(Of RegionDirectiveTriviaSyntax, RegionDirectiveStructureProvider, MetadataAsSource.MetadataRegionDirectiveStructureProvider)()
            builder.Add(Of SelectBlockSyntax, SelectBlockStructureProvider)
            builder.Add(Of StructureStatementSyntax, TypeDeclarationStructureProvider, MetadataAsSource.MetadataTypeDeclarationStructureProvider)()
            builder.Add(Of SyncLockBlockSyntax, SyncLockBlockStructureProvider)
            builder.Add(Of TryBlockSyntax, TryBlockStructureProvider)
            builder.Add(Of UsingBlockSyntax, UsingBlockStructureProvider)
            builder.Add(Of WhileBlockSyntax, WhileBlockStructureProvider)
            builder.Add(Of WithBlockSyntax, WithBlockStructureProvider)
            builder.Add(Of XmlCDataSectionSyntax, XmlExpressionStructureProvider)()
            builder.Add(Of XmlCommentSyntax, XmlExpressionStructureProvider)()
            builder.Add(Of XmlDocumentSyntax, XmlExpressionStructureProvider)()
            builder.Add(Of XmlElementSyntax, XmlExpressionStructureProvider)()
            builder.Add(Of XmlProcessingInstructionSyntax, XmlExpressionStructureProvider)()
            builder.Add(Of LiteralExpressionSyntax, StringLiteralExpressionStructureProvider)()
            builder.Add(Of InterpolatedStringExpressionSyntax, InterpolatedStringExpressionStructureProvider)()

            Return builder.ToImmutable()
        End Function

        Public Shared Function CreateDefaultTriviaStructureProviderMap() As ImmutableDictionary(Of Integer, ImmutableArray(Of AbstractSyntaxStructureProvider))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Integer, ImmutableArray(Of AbstractSyntaxStructureProvider))()

            builder.Add(SyntaxKind.DisabledTextTrivia, ImmutableArray.Create(Of AbstractSyntaxStructureProvider)(New DisabledTextTriviaStructureProvider()))

            Return builder.ToImmutable()
        End Function

        Friend Sub New()
            MyBase.New(CreateDefaultNodeStructureProviderMap(), CreateDefaultTriviaStructureProviderMap())
        End Sub
    End Class
End Namespace
