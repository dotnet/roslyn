' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class VisualBasicBlockStructureProvider
        Inherits AbstractBlockStructureProvider

        Public Shared Function CreateDefaultNodeStructureProviderMap() As ImmutableDictionary(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))
            Dim builder = ImmutableDictionary.CreateBuilder(Of Type, ImmutableArray(Of AbstractSyntaxStructureProvider))()

            builder.Add(Of AccessorStatementSyntax, AccessorDeclarationStructureProvider)()
            builder.Add(Of ClassStatementSyntax, TypeDeclarationStructureProvider)()
            builder.Add(Of CollectionInitializerSyntax, CollectionInitializerStructureProvider)
            builder.Add(Of CompilationUnitSyntax, CompilationUnitStructureProvider)()
            builder.Add(Of SubNewStatementSyntax, ConstructorDeclarationStructureProvider)()
            builder.Add(Of DelegateStatementSyntax, DelegateDeclarationStructureProvider)()
            builder.Add(Of DocumentationCommentTriviaSyntax, DocumentationCommentStructureProvider)()
            builder.Add(Of DoLoopBlockSyntax, DoLoopBlockStructureProvider)
            builder.Add(Of EnumStatementSyntax, EnumDeclarationStructureProvider)()
            builder.Add(Of EnumMemberDeclarationSyntax, EnumMemberDeclarationStructureProvider)()
            builder.Add(Of EventStatementSyntax, EventDeclarationStructureProvider)()
            builder.Add(Of DeclareStatementSyntax, ExternalMethodDeclarationStructureProvider)()
            builder.Add(Of FieldDeclarationSyntax, FieldDeclarationStructureProvider)()
            builder.Add(Of ForBlockSyntax, ForBlockStructureProvider)
            builder.Add(Of ForEachBlockSyntax, ForEachBlockStructureProvider)
            builder.Add(Of InterfaceStatementSyntax, TypeDeclarationStructureProvider)()
            builder.Add(Of MethodStatementSyntax, MethodDeclarationStructureProvider)()
            builder.Add(Of ModuleStatementSyntax, TypeDeclarationStructureProvider)()
            builder.Add(Of MultiLineIfBlockSyntax, MultiLineIfBlockStructureProvider)()
            builder.Add(Of MultiLineLambdaExpressionSyntax, MultilineLambdaStructureProvider)()
            builder.Add(Of NamespaceStatementSyntax, NamespaceDeclarationStructureProvider)()
            builder.Add(Of ObjectCollectionInitializerSyntax, ObjectCreationInitializerStructureProvider)
            builder.Add(Of ObjectMemberInitializerSyntax, ObjectCreationInitializerStructureProvider)
            builder.Add(Of OperatorStatementSyntax, OperatorDeclarationStructureProvider)()
            builder.Add(Of PropertyStatementSyntax, PropertyDeclarationStructureProvider)()
            builder.Add(Of RegionDirectiveTriviaSyntax, RegionDirectiveStructureProvider)()
            builder.Add(Of SelectBlockSyntax, SelectBlockStructureProvider)
            builder.Add(Of StructureStatementSyntax, TypeDeclarationStructureProvider)()
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
