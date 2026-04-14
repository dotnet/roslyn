// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class MiscTests
    {
        /// <summary>
        /// The version of OOB dependencies that MSBuild depends on. When bumping MSBuild's dependencies,
        /// this should be updated to fix test failures. This number is obtained from MSBuild's app.config:
        /// <see href="https://github.com/dotnet/msbuild/blob/main/src/MSBuild/app.config"/>
        /// </summary>
        private static readonly Version s_msBuildOobDependencyVersion = new Version(10, 0, 0, 1);

        /// <summary>
        /// The build task very deliberately does not depend on any of our shipping binaries.  This is to avoid
        /// potential load conflicts for dependencies when loading custom versions of our task.
        /// </summary>
        [Theory]
        [InlineData("Microsoft.CodeAnalysis")]
        [InlineData("Microsoft.CodeAnalysis.CSharp")]
        [InlineData("Microsoft.CodeAnalysis.VisualBasic")]
        [WorkItem(1183, "https://github.com/Microsoft/msbuild/issues/1183")]
        public void EnsureDependenciesNotPresent(string refName)
        {
            var assembly = typeof(ManagedCompiler).Assembly;
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name == refName);
        }

        /// <summary>
        /// <para>
        /// On .NET Framework, the build task might depend on some OOB assemblies that
        /// are transitively obtained via MSBuild. Ensure that we don't accidentally
        /// depend on newer versions of these assemblies than what MSBuild depends on,
        /// to avoid potential load conflicts when loading custom versions of our task.
        /// </para>
        /// <para>
        /// On .NET, these dependencies are part of the shared framework and always available.
        /// Their versions are pinned to X.0.0.0, so the check still ensures that we don't accidentally
        /// bring in a newer version.
        /// </para>
        /// </summary>
        [Fact]
        public void EnsureDependenciesNotNewerThanMSBuild()
        {
            var assembly = typeof(ManagedCompiler).Assembly;
            var refs = assembly
                .GetReferencedAssemblies()
                .Where(x => x.Name is "System.Collections.Immutable" or "System.Reflection.Metadata")
                .ToArray();
            Assert.NotEmpty(refs);
            Assert.All(refs, x =>
            {
                Assert.True(x.Version <= s_msBuildOobDependencyVersion, $"Reference {x.Name} has version {x.Version} which is newer than the maximum allowed {s_msBuildOobDependencyVersion}");
            });
        }
    }
}
