' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.GraphModel.Schemas
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression

    Public Class GraphProviderTests
        <WorkItem(1078048)>
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub TestGetContainsGraphQueries()
            Dim context = CreateGraphContext(GraphContextDirection.Contains, Array.Empty(Of GraphCategory)())
            Dim queries = AbstractGraphProvider.GetGraphQueries(context)
            Assert.Equal(queries.Single().GetType(), GetType(ContainsGraphQuery))
        End Sub

        <WorkItem(1078048)>
        <Fact, Trait(Traits.Feature, Traits.Features.Progression)>
        Public Sub TestGetContainsGraphQueriesWithTarget()
            Dim context = CreateGraphContext(GraphContextDirection.Target, {CodeLinkCategories.Contains})
            Dim queries = AbstractGraphProvider.GetGraphQueries(context)
            Assert.Equal(queries.Single().GetType(), GetType(ContainsGraphQuery))
        End Sub

        Private Shared Function CreateGraphContext(direction As GraphContextDirection, linkCategories As IEnumerable(Of GraphCategory)) As IGraphContext
            Dim context = New Mock(Of IGraphContext)()
            context.Setup(Function(x) x.Direction).Returns(direction)
            context.Setup(Function(x) x.LinkCategories).Returns(linkCategories)
            Return context.Object
        End Function
    End Class

End Namespace
