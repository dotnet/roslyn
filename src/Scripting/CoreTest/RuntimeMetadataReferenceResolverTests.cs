// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

extern alias Scripting;

using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Interactive
{
    using RelativePathResolver = Scripting::Microsoft.CodeAnalysis.RelativePathResolver;

    public class RuntimeMetadataReferenceResolverTests : TestBase
    {
        [Fact]
        public void Resolve()
        {
            using (var directory = new DisposableDirectory(Temp))
            {
                var assembly1 = directory.CreateFile("_1.dll");
                var assembly2 = directory.CreateFile("_2.dll");

                // With NuGetPackageResolver.
                var resolver = new RuntimeMetadataReferenceResolver(
                    new RelativePathResolver(ImmutableArray.Create(directory.Path), baseDirectory: directory.Path),
                    new PackageResolver(ImmutableDictionary<string, ImmutableArray<string>>.Empty.Add("nuget:N/1.0", ImmutableArray.Create(assembly1.Path, assembly2.Path))),
                    gacFileResolver: null);
                // Recognized NuGet reference.
                var actualReferences = resolver.ResolveReference("nuget:N/1.0", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                AssertEx.SetEqual(actualReferences.SelectAsArray(r => r.FilePath), assembly1.Path, assembly2.Path);
                // Unrecognized NuGet reference.
                actualReferences = resolver.ResolveReference("nuget:N/2.0", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                Assert.True(actualReferences.IsEmpty);
                // Recognized file path. 
                actualReferences = resolver.ResolveReference("_2.dll", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                AssertEx.SetEqual(actualReferences.SelectAsArray(r => r.FilePath), assembly2.Path);
                // Unrecognized file path. 
                actualReferences = resolver.ResolveReference("_3.dll", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                Assert.True(actualReferences.IsEmpty);

                // Without NuGetPackageResolver.
                resolver = new RuntimeMetadataReferenceResolver(
                    new RelativePathResolver(ImmutableArray.Create(directory.Path), baseDirectory: directory.Path),
                    packageResolver: null,
                    gacFileResolver: null);
                // Unrecognized NuGet reference.
                actualReferences = resolver.ResolveReference("nuget:N/1.0", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                Assert.True(actualReferences.IsEmpty);
                // Recognized file path. 
                actualReferences = resolver.ResolveReference("_2.dll", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                AssertEx.SetEqual(actualReferences.SelectAsArray(r => r.FilePath), assembly2.Path);
                // Unrecognized file path. 
                actualReferences = resolver.ResolveReference("_3.dll", baseFilePath: null, properties: MetadataReferenceProperties.Assembly);
                Assert.True(actualReferences.IsEmpty);
            }
        }

        private sealed class PackageResolver : NuGetPackageResolver
        {
            private const string Prefix = "nuget:";
            private readonly IImmutableDictionary<string, ImmutableArray<string>> _map;

            internal PackageResolver(IImmutableDictionary<string, ImmutableArray<string>> map)
            {
                _map = map;
            }

            internal override ImmutableArray<string> ResolveNuGetPackage(string packageName, string packageVersion)
            {
                var reference = $"{Prefix}{packageName}/{packageVersion}";
                ImmutableArray<string> paths;
                if (_map.TryGetValue(reference, out paths))
                {
                    return paths;
                }
                return ImmutableArray<string>.Empty;
            }
        }
    }
}
