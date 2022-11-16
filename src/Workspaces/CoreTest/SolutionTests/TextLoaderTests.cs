﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class TextLoaderTests
{
    private class LoaderNoOverride1 : TextLoader
    {
    }

    private class LoaderNoOverride2 : TextLoader
    {
        public new virtual Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);
    }

    private class LoaderNoOverrideBase : TextLoader
    {
        // newslot
        public new virtual Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);

        // newslot
        public virtual Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId)
            => Task.FromResult((TextAndVersion?)null!);

        // newslot
        public virtual Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, ref DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);

        // newslot
        public virtual Task<TextAndVersion> LoadTextAndVersionAsync<T>(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);
    }

    private class LoaderNoOverride3 : LoaderNoOverrideBase
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);
    }

    private class LoaderNoOverride4 : LoaderNoOverrideBase
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId)
            => base.LoadTextAndVersionAsync(workspace, documentId);
    }

    private class LoaderNoOverride5 : LoaderNoOverrideBase
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, ref DocumentId? documentId, CancellationToken cancellationToken)
           => Task.FromResult((TextAndVersion?)null!);
    }

    private class LoaderNoOverride6 : LoaderNoOverrideBase
    {
        public override Task<TextAndVersion> LoadTextAndVersionAsync<T>(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult((TextAndVersion?)null!);
    }

    public static IEnumerable<object[]> GetNoOverideLoaders()
    {
        yield return new[] { new LoaderNoOverride1() };
        yield return new[] { new LoaderNoOverride2() };
        yield return new[] { new LoaderNoOverrideBase() };
        yield return new[] { new LoaderNoOverride3() };
        yield return new[] { new LoaderNoOverride4() };
        yield return new[] { new LoaderNoOverride5() };
        yield return new[] { new LoaderNoOverride6() };
    }

    private class LoaderOverridesObsolete : TextLoader
    {
        public static readonly TextAndVersion Value = TextAndVersion.Create(SourceText.From(""), VersionStamp.Default);

        [Obsolete]
        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult(Value);
    }

    private class LoaderOverridesObsolete2 : LoaderOverridesObsolete
    {
        public static new readonly TextAndVersion Value = TextAndVersion.Create(SourceText.From(""), VersionStamp.Default);

        [Obsolete]
        public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
            => Task.FromResult(Value);
    }

    private class LoaderOverridesNew : TextLoader
    {
        public static readonly TextAndVersion Value = TextAndVersion.Create(SourceText.From(""), VersionStamp.Default);

        public override Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
            => Task.FromResult(Value);
    }

    [Theory, Obsolete]
    [MemberData(nameof(GetNoOverideLoaders))]
    public async Task NoOverride(TextLoader loader)
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => loader.LoadTextAndVersionAsync(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None));
        await Assert.ThrowsAsync<NotImplementedException>(() => loader.LoadTextAndVersionAsync(workspace: null, documentId: null, CancellationToken.None));
    }

    [Fact, Obsolete]
    public async Task OverridesObsolete()
    {
        var loader = new LoaderOverridesObsolete();
        Assert.Same(LoaderOverridesObsolete.Value, await loader.LoadTextAndVersionAsync(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None));
        Assert.Same(LoaderOverridesObsolete.Value, await loader.LoadTextAndVersionAsync(workspace: null, documentId: null, CancellationToken.None));
    }

    [Fact, Obsolete]
    public async Task OverridesObsolete2()
    {
        var loader = new LoaderOverridesObsolete2();
        Assert.Same(LoaderOverridesObsolete2.Value, await loader.LoadTextAndVersionAsync(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None));
        Assert.Same(LoaderOverridesObsolete2.Value, await loader.LoadTextAndVersionAsync(workspace: null, documentId: null, CancellationToken.None));
    }

    [Fact, Obsolete]
    public async Task OverridesNew()
    {
        var loader = new LoaderOverridesNew();
        Assert.Same(LoaderOverridesNew.Value, await loader.LoadTextAndVersionAsync(new LoadTextOptions(SourceHashAlgorithms.Default), CancellationToken.None));
        Assert.Same(LoaderOverridesNew.Value, await loader.LoadTextAndVersionAsync(workspace: null, documentId: null, CancellationToken.None));
    }
}
