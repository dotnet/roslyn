' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.GraphModel
Imports Microsoft.VisualStudio.GraphModel.Schemas
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Progression
Imports Moq
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Progression
    <Trait(Traits.Feature, Traits.Features.Progression)>
    Public Class GraphProviderTests
        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078048")>
        Public Sub TestGetContainsGraphQueries()
            Dim context = CreateGraphContext(GraphContextDirection.Contains, Array.Empty(Of GraphCategory)())
            Dim queries = RoslynGraphProvider.GetGraphQueries(context, asyncListener:=Nothing)
            Assert.Equal(queries.Single().GetType(), GetType(ContainsGraphQuery))
        End Sub

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078048")>
        Public Sub TestGetContainsGraphQueriesWithTarget()
            Dim context = CreateGraphContext(GraphContextDirection.Target, {CodeLinkCategories.Contains})
            Dim queries = RoslynGraphProvider.GetGraphQueries(context, asyncListener:=Nothing)
            Assert.Equal(queries.Single().GetType(), GetType(ContainsGraphQuery))
        End Sub

        Private Shared Function CreateGraphContext(direction As GraphContextDirection, linkCategories As IEnumerable(Of GraphCategory)) As IGraphContext
            Dim context = New Mock(Of IGraphContext)(MockBehavior.Strict)
            context.Setup(Function(x) x.Direction).Returns(direction)
            context.Setup(Function(x) x.LinkCategories).Returns(linkCategories)
            Return context.Object
        End Function
    End Class

End Namespace
