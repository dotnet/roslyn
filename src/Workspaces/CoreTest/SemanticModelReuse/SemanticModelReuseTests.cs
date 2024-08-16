// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.SemanticModelReuse
{
    [UseExportProvider]
    public class SemanticModelReuseTests
    {
        private static Document CreateDocument(string code, string language)
        {
            var solution = new AdhocWorkspace().CurrentSolution;
            var projectId = ProjectId.CreateNewId();
            var project = solution.AddProject(projectId, "Project", "Project.dll", language).GetProject(projectId);

            return project.AddMetadataReference(TestMetadata.Net40.mscorlib)
                          .AddDocument("Document", SourceText.From(code));
        }

        #region C# tests

        [Fact]
        public async Task NullBodyReturnsNormalSemanticModel1_CSharp()
        {
            var document = CreateDocument("", LanguageNames.CSharp);

            // trying to get a model for null should return a non-speculative model
            var model = await document.ReuseExistingSpeculativeModelAsync(null, CancellationToken.None);
            Assert.False(model.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task NullBodyReturnsNormalSemanticModel2_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document = CreateDocument(source, LanguageNames.CSharp);

            // Even if we've primed things with a real location, getting a semantic model for null should return a
            // non-speculative model.
            var model1 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            var model2 = await document.ReuseExistingSpeculativeModelAsync(null, CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);
            Assert.False(model2.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task SameSyntaxTreeReturnsNonSpeculativeModel_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.  The next call will also use the
            // same syntax tree, so it should get the same semantic model.
            var model1 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            var model2 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);
            Assert.False(model2.IsSpeculativeSemanticModel);

            // Should be the same models.
            Assert.Equal(model1, model2);

            // Which also should be the normal model the document provides.
            var actualModel = await document.GetSemanticModelAsync();
            Assert.Equal(model1, actualModel);
        }

        [Fact]
        public async Task InBodyEditShouldProduceCachedModel_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { void M() { return null; } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task OutOfBodyEditShouldProduceFreshModel_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { long M() { return; } }"));

            // We changed the return type, so we can't reuse the previous model.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model2.IsSpeculativeSemanticModel);

            var document3 = document2.WithText(SourceText.From("class C { long M() { return 0; } }"));

            // We are now again only editing a method body so we should be able to get a speculative model.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task MultipleBodyEditsShouldProduceFreshModel_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { void M() { return 0; } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From("class C { void M() { return 1; } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1167540")]
        public async Task MultipleBodyEditsShouldProduceFreshModel_Accessor_Property_CSharp()
        {
            var source = "class C { int M { get { return 0; } } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { int M { get { return 1; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From("class C { int M { get { return 2; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1167540")]
        public async Task MultipleBodyEditsShouldProduceFreshModel_Accessor_Event_CSharp()
        {
            var source = "class C { event System.Action E { add { return 0; } } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { event System.Action E { add { return 1; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From("class C { event System.Action E { add { return 2; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1167540")]
        public async Task MultipleBodyEditsShouldProduceFreshModel_Accessor_Indexer_CSharp()
        {
            var source = "class C { int this[int i] { get { return 0; } } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { int this[int i] { get { return 1; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From("class C { int this[int i] { get { return 2; } } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1541001")]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1587699")]
        public async Task TestOutOfBoundsInSyntaxContext1_CSharp()
        {
            var source = "class C { void M() { return; } }";
            var document1 = CreateDocument(source, LanguageNames.CSharp);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From("class C { void M() { return null; } }"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            // ensure this doesn't crash.
            CSharpSyntaxContext.CreateContext(document2, model2, source.IndexOf("void"), CancellationToken.None);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1541001")]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1587699")]
        public async Task TestOutOfBoundsInSyntaxContext2_CSharp()
        {
            // These two tree are considered equavilent at top level, but the change in trivia around the method
            // makes it tricky to decide whether it's safe to use the speculative model at a given position.

            var source1 = "class C { void M() { return; } }";
            //                                ^ this is the position used to set OriginalPositionForSpeculation when creating the speculative model.  
            var source2 = "class C {                             void M() { return null; } }";
            //                                                            ^ it's unsafe to use the speculative model at this position, even though it's after OriginalPositionForSpeculation 

            // First call will prime the cache to point at the real semantic model.
            var document1 = CreateDocument(source1, LanguageNames.CSharp);
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source1.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(source2));

            // Because the change in trivia shifted the method definition, we are not able to get a speculative model based on previous model
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source2.IndexOf("{ return"), CancellationToken.None);
            Assert.False(model2.IsSpeculativeSemanticModel);

            // ensure this doesn't crash.
            CSharpSyntaxContext.CreateContext(document2, model2, source2.IndexOf("{ return"), CancellationToken.None);
        }

        #endregion

        #region Visual Basic tests

        [Fact]
        public async Task NullBodyReturnsNormalSemanticModel1_VisualBasic()
        {
            var document = CreateDocument("", LanguageNames.VisualBasic);

            // trying to get a model for null should return a non-speculative model
            var model = await document.ReuseExistingSpeculativeModelAsync(null, CancellationToken.None);
            Assert.False(model.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task NullBodyReturnsNormalSemanticModel2_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document = CreateDocument(source, LanguageNames.VisualBasic);

            // Even if we've primed things with a real location, getting a semantic model for null should return a
            // non-speculative model.
            var model1 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            var model2 = await document.ReuseExistingSpeculativeModelAsync(null, CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);
            Assert.False(model2.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task SameSyntaxTreeReturnsNonSpeculativeModel_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.  The next call will also use the
            // same syntax tree, so it should get the same semantic model.
            var model1 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            var model2 = await document.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);
            Assert.False(model2.IsSpeculativeSemanticModel);

            // Should be the same models.
            Assert.Equal(model1, model2);

            // Which also should be the normal model the document provides.
            var actualModel = await document.GetSemanticModelAsync();
            Assert.Equal(model1, actualModel);
        }

        [Fact]
        public async Task InBodyEditShouldProduceCachedModel_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
    sub M()
        return nothing
    end sub
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task OutOfBodyEditShouldProduceFreshModel_VisualBasic()
        {
            var source1 = @"
class C
    sub M()
        return
    end sub
end class";
            var document1 = CreateDocument(source1, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source1.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var source2 = @"
class C
    function M() as long
        return
    end function
end class";
            var document2 = document1.WithText(SourceText.From(source2));

            // We changed the return type, so we can't reuse the previous model.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source2.IndexOf("return"), CancellationToken.None);
            Assert.False(model2.IsSpeculativeSemanticModel);

            var document3 = document2.WithText(SourceText.From(@"
class C
    function M() as long
        return 0
    end function
end class"));

            // We are now again only editing a method body so we should be able to get a speculative model.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source2.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task MultipleBodyEditsShouldProduceFreshModel_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
    sub M()
        return 0
    end sub
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From(@"
class C
    sub M()
        return 1
    end sub
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task MultipleBodyEditsShouldProduceFreshModel_Accessor_Property_VisualBasic()
        {
            var source = @"
class C
    readonly property M as integer
        get
            return 0
        end get
    end property
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
    readonly property M as integer
        get
            return 1
        end get
    end property
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From(@"
class C
    readonly property M as integer
        get
            return 2
        end get
    end property
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact]
        public async Task MultipleBodyEditsShouldProduceFreshModel_Accessor_Event_VisualBasic()
        {
            var source = @"
class C
    public custom event E as System.Action
        addhandler(value as System.Action)
            return 0
        end addhandler
    end event
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
    public custom event E as System.Action
        addhandler(value as System.Action)
            return 1
        end addhandler
    end event
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            var document3 = document1.WithText(SourceText.From(@"
class C
    public custom event E as System.Action
        addhandler(value as System.Action)
            return 2
        end addhandler
    end event
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model3 = await document3.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model3.IsSpeculativeSemanticModel);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1541001")]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1587699")]
        public async Task TestOutOfBoundsInSyntaxContext1_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
    sub M()
        return nothing
    end sub
end class"));

            // This should be able to get a speculative model using the original model we primed the cache with.
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.True(model2.IsSpeculativeSemanticModel);

            // Ensure this doesn't crash.
            VisualBasicSyntaxContext.CreateContext(document2, model2, source.IndexOf("sub"), CancellationToken.None);
        }

        [Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1541001")]
        [WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1587699")]
        public async Task TestOutOfBoundsInSyntaxContext2_VisualBasic()
        {
            var source = @"
class C
    sub M()
        return
    end sub
end class";
            var document1 = CreateDocument(source, LanguageNames.VisualBasic);

            // First call will prime the cache to point at the real semantic model.
            var model1 = await document1.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model1.IsSpeculativeSemanticModel);

            var document2 = document1.WithText(SourceText.From(@"
class C
class C
                                    sub M()
        return nothing
    end sub
end class"));

            // Because the change in trivia shifted the method definition, we are not able to get a speculative model based on previous model
            var model2 = await document2.ReuseExistingSpeculativeModelAsync(source.IndexOf("return"), CancellationToken.None);
            Assert.False(model2.IsSpeculativeSemanticModel);

            // Ensure this doesn't crash.
            VisualBasicSyntaxContext.CreateContext(document2, model2, source.IndexOf("return"), CancellationToken.None);
        }

        #endregion
    }
}
