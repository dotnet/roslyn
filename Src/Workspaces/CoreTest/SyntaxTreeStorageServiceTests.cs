// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SyntaxTreeStorageServiceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestCanRetrieve()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var root = CSharp.SyntaxFactory.CompilationUnit();
            var tree = CSharp.SyntaxFactory.SyntaxTree(root);

            Assert.False(syntaxTreeStorageService.CanRetrieve(tree));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async Task TestEnqueueStoreAsync()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var root = CSharp.SyntaxFactory.CompilationUnit();
            var tree = CSharp.SyntaxFactory.SyntaxTree(root);

            // EnqueueStore and EnqueueStoreAsync is basically same thing. only difference is EnqueueStoreAsync returns Task that can be waited.
            await syntaxTreeStorageService.EnqueueStoreAsync(tree, root, tempStorageService, CancellationToken.None);

            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestStore()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var root = CSharp.SyntaxFactory.CompilationUnit();
            var tree = CSharp.SyntaxFactory.SyntaxTree(root);

            syntaxTreeStorageService.Store(tree, root, tempStorageService, CancellationToken.None);
            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async Task TestStoreAsync()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var root = CSharp.SyntaxFactory.CompilationUnit();
            var tree = CSharp.SyntaxFactory.SyntaxTree(root);

            await syntaxTreeStorageService.StoreAsync(tree, root, tempStorageService, CancellationToken.None);

            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestRetrieve()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var tree = CSharp.SyntaxFactory.ParseSyntaxTree("namespace A { class C {");
            var root = tree.GetRoot();

            syntaxTreeStorageService.Store(tree, root, tempStorageService, CancellationToken.None);
            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));

            var syntaxFactoryService = new CSharp.CSharpSyntaxTreeFactoryServiceFactory().CreateLanguageService(provider: null) as ISyntaxTreeFactoryService;
            var newRoot = syntaxTreeStorageService.Retrieve(tree, syntaxFactoryService, CancellationToken.None);

            Assert.True(root.IsEquivalentTo(newRoot));

            // we can't directly compare diagnostics since location in the diagnostic will point to two different trees
            AssertEx.SetEqual(root.GetDiagnostics().Select(d => d.ToString()), newRoot.GetDiagnostics().Select(d => d.ToString()));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async Task TestRetrieveAsync()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var tree = CSharp.SyntaxFactory.ParseSyntaxTree("namespace A { class C {");
            var root = tree.GetRoot();

            syntaxTreeStorageService.Store(tree, root, tempStorageService, CancellationToken.None);
            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));

            var syntaxFactoryService = new CSharp.CSharpSyntaxTreeFactoryServiceFactory().CreateLanguageService(provider: null) as ISyntaxTreeFactoryService;
            var newRoot = await syntaxTreeStorageService.RetrieveAsync(tree, syntaxFactoryService, CancellationToken.None);

            Assert.True(root.IsEquivalentTo(newRoot));

            // we can't directly compare diagnostics since location in the diagnostic will point to two different trees
            AssertEx.SetEqual(root.GetDiagnostics().Select(d => d.ToString()), newRoot.GetDiagnostics().Select(d => d.ToString()));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async Task TestRemoveAsync()
        {
            var textFactory = new TextFactoryServiceFactory.TextFactoryService();
            var tempStorageService = new TemporaryStorageServiceFactory.TemporaryStorageService(textFactory);
            var syntaxTreeStorageService = new SyntaxTreeStorageService();

            var root = CSharp.SyntaxFactory.CompilationUnit();
            var tree = CSharp.SyntaxFactory.SyntaxTree(root);

            var weakReference = new WeakReference(tree);

            syntaxTreeStorageService.Store(tree, root, tempStorageService, CancellationToken.None);
            Assert.True(syntaxTreeStorageService.CanRetrieve(tree));

            // let tree and root go.
            root = null;
            tree = null;

            var count = 0;
            while (weakReference.IsAlive && count < 100)
            {
                GC.Collect();

                await Task.Delay(5);
                count++;
            }

            // tree should go away before it reached 100
            Assert.False(count == 100);
        }
    }
}
