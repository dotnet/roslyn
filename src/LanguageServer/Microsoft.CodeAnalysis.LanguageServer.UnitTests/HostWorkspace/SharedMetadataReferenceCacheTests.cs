// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SharedMetadataReferenceCacheTests : TestBase
{
    [Fact]
    public async Task ConcurrentRequests_ReturnSameReference()
    {
        var cache = new SharedMetadataReferenceCache();
        var mscorlibPath = typeof(object).Assembly.Location;

        var references = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => Task.Run(
                () => GetReference(cache, mscorlibPath))));

        Assert.All(references, reference => Assert.Same(references[0], reference));
        Assert.NotEmpty(((AssemblyMetadata)references[0].GetMetadata()).GetModules());
    }

    [Fact]
    public void CacheHit_DoesNotInvokeProvider()
    {
        var cache = new SharedMetadataReferenceCache();
        var mscorlibPath = typeof(object).Assembly.Location;
        var creationCallCount = 0;

        var reference1 = cache.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly, CreateReference);
        var reference2 = cache.GetReference(mscorlibPath, MetadataReferenceProperties.Assembly, CreateReference);

        Assert.Same(reference1, reference2);
        Assert.Equal(1, creationCallCount);

        PortableExecutableReference CreateReference(string path, MetadataReferenceProperties properties)
        {
            creationCallCount++;
            return CreateCacheableReference(path, properties);
        }
    }

    [Fact]
    public void DifferentProperties_ShareMetadataAndDocumentationProvider()
    {
        var cache = new SharedMetadataReferenceCache();
        var mscorlibPath = typeof(object).Assembly.Location;
        var defaultProperties = MetadataReferenceProperties.Assembly;
        var aliasedProperties = defaultProperties.WithAliases(["global", "MyAlias"]).WithEmbedInteropTypes(true);
        var creationCallCount = 0;

        var defaultReference = cache.GetReference(mscorlibPath, defaultProperties, CreateReference);
        var aliasedReference = cache.GetReference(mscorlibPath, aliasedProperties, CreateReference);

        Assert.NotSame(defaultReference, aliasedReference);
        Assert.Equal(defaultProperties, defaultReference.Properties);
        Assert.Equal(aliasedProperties, aliasedReference.Properties);
        Assert.Same(defaultReference.GetMetadataId(), aliasedReference.GetMetadataId());
        Assert.Equal(1, creationCallCount);

        PortableExecutableReference CreateReference(string path, MetadataReferenceProperties properties)
        {
            creationCallCount++;
            return CreateCacheableReference(path, properties);
        }
    }

    [Fact]
    public void ChangedTimestamp_DoesNotShareReference()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = Path.Combine(Temp.CreateDirectory().Path, "reference.dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var reference1 = GetReference(cache, path);

        File.Copy(typeof(Enumerable).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        var reference2 = GetReference(cache, path);

        Assert.NotSame(reference1, reference2);
        Assert.NotSame(reference1.GetMetadataId(), reference2.GetMetadataId());
    }

    [Fact]
    public void ChangedTimestamp_InvalidatesAllPropertyVariants()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = Path.Combine(Temp.CreateDirectory().Path, "reference.dll");
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var defaultProperties = MetadataReferenceProperties.Assembly;
        var aliasedProperties = defaultProperties.WithAliases(["global", "MyAlias"]);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var defaultReference1 = GetReference(cache, path, defaultProperties);
        var aliasedReference1 = GetReference(cache, path, aliasedProperties);

        File.Copy(typeof(Enumerable).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        var defaultReference2 = GetReference(cache, path, defaultProperties);
        var aliasedReference2 = GetReference(cache, path, aliasedProperties);

        Assert.NotSame(defaultReference1.GetMetadataId(), defaultReference2.GetMetadataId());
        Assert.NotSame(aliasedReference1.GetMetadataId(), aliasedReference2.GetMetadataId());
        Assert.Same(defaultReference2.GetMetadataId(), aliasedReference2.GetMetadataId());
    }

    [Fact]
    public void CacheDoesNotKeepReferenceAlive()
    {
        var cache = new SharedMetadataReferenceCache();
        var reference = ObjectReference.CreateFromFactory(
            () => GetReference(cache, typeof(object).Assembly.Location));

        reference.AssertReleased();
    }

    [Fact]
    public void DeadReferenceIsReloaded()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = typeof(object).Assembly.Location;
        var reference = ObjectReference.CreateFromFactory(() => GetReference(cache, path));
        reference.AssertReleased();

        var reloadedReference = GetReference(cache, path);
        Assert.Same(reloadedReference, GetReference(cache, path));

        GC.KeepAlive(reloadedReference);
    }

    [Fact]
    public void CleanupRemovesDeadEntries()
    {
        var cache = new SharedMetadataReferenceCache(cleanupThreshold: 2);

        // Add an entry, then release its only strong reference so it is eligible for cleanup.
        var reference = ObjectReference.CreateFromFactory(
            () => GetReference(cache, typeof(object).Assembly.Location));
        reference.AssertReleased();

        // Adding a second, live entry reaches the cleanup threshold.
        var liveReference = GetReference(cache, typeof(Enumerable).Assembly.Location);

        // Cleanup should remove the dead entry and retain only the live entry.
        Assert.Equal(1, cache.GetTestAccessor().EntryCount);
        Assert.NotEmpty(((AssemblyMetadata)liveReference.GetMetadata()).GetModules());
    }

    [Fact]
    public void ChangedFileReplacesPreviousVersion()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = Path.Combine(Temp.CreateDirectory().Path, "reference.dll");
        var otherPath = typeof(Enumerable).Assembly.Location;
        var timestamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.Copy(typeof(object).Assembly.Location, path);
        File.SetLastWriteTimeUtc(path, timestamp);

        var firstReference = GetReference(cache, path);
        var otherReference = GetReference(cache, otherPath);
        Assert.Same(firstReference, GetReference(cache, path));

        File.Copy(typeof(Uri).Assembly.Location, path, overwrite: true);
        File.SetLastWriteTimeUtc(path, timestamp.AddSeconds(1));

        Assert.NotSame(firstReference, GetReference(cache, path));
        Assert.Same(otherReference, GetReference(cache, otherPath));
    }

    [Fact]
    public void DifferentImageKinds_DoNotShareReference()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = typeof(object).Assembly.Location;

        var assemblyReference = GetReference(cache, path, MetadataReferenceProperties.Assembly);
        var moduleReference = GetReference(cache, path, MetadataReferenceProperties.Module);

        Assert.NotSame(assemblyReference, moduleReference);
        Assert.NotSame(assemblyReference.GetMetadataId(), moduleReference.GetMetadataId());
    }

    [ConditionalFact(typeof(UnixLikeOnly))]
    public void PathsDifferingOnlyByCase_DoNotShareReferenceOnUnix()
    {
        var cache = new SharedMetadataReferenceCache();
        var directory = Temp.CreateDirectory();
        var lowerCasePath = Path.Combine(directory.Path, "reference.dll");
        var upperCasePath = Path.Combine(directory.Path, "REFERENCE.dll");
        File.Copy(typeof(object).Assembly.Location, lowerCasePath);
        File.Copy(typeof(object).Assembly.Location, upperCasePath);

        var lowerCaseReference = GetReference(cache, lowerCasePath);
        var upperCaseReference = GetReference(cache, upperCasePath);

        Assert.NotSame(lowerCaseReference, upperCaseReference);
        Assert.Equal(2, cache.GetTestAccessor().EntryCount);
    }

    [Fact]
    public void ThrowingReference_IsSharedAndInvalidatedWhenFileCreated()
    {
        var cache = new SharedMetadataReferenceCache();
        var path = Path.Combine(Temp.CreateDirectory().Path, "reference.dll");

        var firstReference = cache.GetReference(
            path, MetadataReferenceProperties.Assembly, CreateReference);
        var secondReference = cache.GetReference(
            path, MetadataReferenceProperties.Assembly, CreateReference);

        Assert.Same(firstReference, secondReference);
        Assert.Throws<FileNotFoundException>(firstReference.GetMetadata);

        File.Copy(typeof(object).Assembly.Location, path);

        var loadedReference = cache.GetReference(
            path, MetadataReferenceProperties.Assembly, CreateReference);
        Assert.NotSame(firstReference, loadedReference);
        Assert.Same(
            loadedReference,
            cache.GetReference(path, MetadataReferenceProperties.Assembly, CreateReference));
        Assert.IsType<AssemblyMetadata>(loadedReference.GetMetadata());

        static PortableExecutableReference CreateReference(
            string path, MetadataReferenceProperties properties)
        {
            try
            {
                return MetadataReference.CreateFromFile(
                    path, properties, DocumentationProvider.Default);
            }
            catch (IOException e)
            {
                return new ThrowingExecutableReference(path, properties, e);
            }
        }
    }

    private static PortableExecutableReference GetReference(
        SharedMetadataReferenceCache cache,
        string path,
        MetadataReferenceProperties properties = default)
        => cache.GetReference(path, properties, CreateCacheableReference);

    private static PortableExecutableReference CreateCacheableReference(
        string path, MetadataReferenceProperties properties)
    {
        var reference = MetadataReference.CreateFromFile(
            path, properties, DocumentationProvider.Default);
        _ = reference.GetMetadata();

        return reference;
    }

    private sealed class ThrowingExecutableReference(
        string path, MetadataReferenceProperties properties, IOException exception)
        : PortableExecutableReference(properties, path)
    {
        protected override DocumentationProvider CreateDocumentationProvider()
            => DocumentationProvider.Default;

        protected override Metadata GetMetadataImpl()
            => throw exception;

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => new ThrowingExecutableReference(FilePath!, properties, exception);
    }
}
