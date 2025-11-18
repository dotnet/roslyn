// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    public sealed class MiscTests
    {
        /// <summary>
        /// The build task very deliberately does not depend on any of our shipping binaries.  This is to avoid
        /// potential load conflicts for dependencies when loading custom versions of our task.
        /// </summary>
        [Fact]
        [WorkItem(1183, "https://github.com/Microsoft/msbuild/issues/1183")]
        public void EnsureDependencies()
        {
            var assembly = typeof(ManagedCompiler).Assembly;
            foreach (var name in assembly.GetReferencedAssemblies())
            {
                var isBadRef =
                    name.Name == typeof(Compilation).Assembly.GetName().Name ||
                    name.Name == typeof(CSharpCompilation).Assembly.GetName().Name ||
                    name.Name == typeof(ImmutableArray<string>).Assembly.GetName().Name;
                Assert.False(isBadRef);
            }
        }
    }
}
