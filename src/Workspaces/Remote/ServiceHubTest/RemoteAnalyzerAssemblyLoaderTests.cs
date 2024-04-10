// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP

using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Remote.UnitTests
{
    public class RemoteAnalyzerAssemblyLoaderTests
    {
        [Fact]
        public void NonIdeAnalyzerAssemblyShouldBeLoadedInSeparateALC()
        {
            using var testFixture = new AssemblyLoadTestFixture();
            var remoteAssemblyInCurrentAlc = typeof(RemoteAnalyzerAssemblyLoader).GetTypeInfo().Assembly;
            var remoteAssemblyLocation = remoteAssemblyInCurrentAlc.Location;

            var loader = new RemoteAnalyzerAssemblyLoader(Path.GetDirectoryName(remoteAssemblyLocation)!);

            // Try to load MS.CA.Remote.ServiceHub.dll as an analyzer assembly via RemoteAnalyzerAssemblyLoader
            // since it's not one of the special assemblies listed in RemoteAnalyzerAssemblyLoader,
            // RemoteAnalyzerAssemblyLoader should loaded in a spearate DirectoryLoadContext. 
            loader.AddDependencyLocation(testFixture.Delta1);
            var remoteAssemblyLoadedViaRemoteLoader = loader.LoadFromPath(testFixture.Delta1);

            var alc1 = AssemblyLoadContext.GetLoadContext(remoteAssemblyInCurrentAlc);
            var alc2 = AssemblyLoadContext.GetLoadContext(remoteAssemblyLoadedViaRemoteLoader);
            Assert.NotEqual(alc1, alc2);
        }

        [Fact]
        public void IdeAnalyzerAssemblyShouldBeLoadedInLoaderALC()
        {
            var featuresAssemblyInCurrentAlc = typeof(Microsoft.CodeAnalysis.Completion.CompletionProvider).GetTypeInfo().Assembly;
            var featuresAssemblyLocation = featuresAssemblyInCurrentAlc.Location;

            // Try to load MS.CA.Features.dll as an analyzer assembly via RemoteAnalyzerAssemblyLoader
            // since it's listed as one of the special assemblies in RemoteAnalyzerAssemblyLoader,
            // RemoteAnalyzerAssemblyLoader should loaded in its own ALC. 
            var loader = new RemoteAnalyzerAssemblyLoader(Path.GetDirectoryName(featuresAssemblyLocation)!);
            loader.AddDependencyLocation(featuresAssemblyLocation);

            var featuresAssemblyLoadedViaRemoteLoader = loader.LoadFromPath(featuresAssemblyLocation);

            var alc1 = AssemblyLoadContext.GetLoadContext(featuresAssemblyInCurrentAlc);
            var alc2 = AssemblyLoadContext.GetLoadContext(featuresAssemblyLoadedViaRemoteLoader);
            Assert.Equal(alc1, alc2);
        }

        [Fact]
        public void CompilerAssemblyShouldBeLoadedInLoaderALC()
        {
            var compilerAssemblyInCurrentAlc = typeof(SyntaxNode).GetTypeInfo().Assembly;
            var compilerAssemblyLocation = compilerAssemblyInCurrentAlc.Location;

            var loader = new RemoteAnalyzerAssemblyLoader(Path.GetDirectoryName(compilerAssemblyLocation)!);
            loader.AddDependencyLocation(compilerAssemblyLocation);

            var compilerAssemblyLoadedViaRemoteLoader = loader.LoadFromPath(compilerAssemblyLocation);

            var alc1 = AssemblyLoadContext.GetLoadContext(compilerAssemblyInCurrentAlc);
            var alc2 = AssemblyLoadContext.GetLoadContext(compilerAssemblyLoadedViaRemoteLoader);
            Assert.Equal(alc1, alc2);
        }
    }
}
#endif
