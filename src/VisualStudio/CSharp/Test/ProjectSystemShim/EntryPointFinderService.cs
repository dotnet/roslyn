// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.UnitTests;
using Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel;
using Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.CSharp.UnitTests.ProjectSystemShim
{
    [UseExportProvider]
    public class EntryPointFinderService
    {
        private static async Task<IEnumerable<INamedTypeSymbol>> FindEntryPointsAsync(string file)
        {
            using var vsWorkspace = TestWorkspace.CreateCSharp(file, composition: VisualStudioTestCompositions.LanguageServices);
            var entryPointFinder = vsWorkspace.Projects.Single().LanguageServiceProvider.GetRequiredService<IEntryPointFinderService>();
            var workspace = vsWorkspace.Projects.Single().LanguageServiceProvider.WorkspaceServices.Workspace;
            var compilation = await workspace.CurrentSolution.Projects.Single().GetCompilationAsync();
            AssertEx.NotNull(compilation);
            return entryPointFinder.FindEntryPoints(compilation, false);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public static async Task FindMainEntryPointForStatic()
        {
            var file = """
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static void Main(string[] args)
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            var entryPoint = Assert.Single(entryPoints);
            Assert.Equal("Program", entryPoint.Name);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public static async Task DoNotFindMainEntryPointForNonStatic()
        {
            var file = """
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public void Main(string[] args)
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            Assert.Empty(entryPoints);
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData("void Main(string[] args)")]
        [InlineData("Task Main(string[] args)")]
        [InlineData("int Main(string[] args)")]
        [InlineData("Task<int> Main(string[] args)")]
        public static async Task FindMainEntryPointForDifferentReturnTypes(string methodDefinition)
        {
            var file = $$"""
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static {{methodDefinition}}
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            var entryPoint = Assert.Single(entryPoints);
            Assert.Equal("Program", entryPoint.Name);
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData("bool Main(string[] args)")]
        [InlineData("float Main(string[] args)")]
        [InlineData("Task<bool> Main(string[] args)")]
        public static async Task DoNotFindMainEntryPointForDifferentReturnTypes(string methodDefinition)
        {
            var file = $$"""
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static {{methodDefinition}}
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            Assert.Empty(entryPoints);
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData("int main(string[] args)")]
        [InlineData("void MaiN(string[] args)")]
        public static async Task DoNotFindMainEntryPointForDifferentCapitalization(string methodDefinition)
        {
            var file = $$"""
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static {{methodDefinition}}
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            Assert.Empty(entryPoints);
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData("void Main()")]
        [InlineData("Task Main()")]
        [InlineData("int Main()")]
        [InlineData("Task<int> Main()")]
        public static async Task FindMainEntryPointForDifferentArguments(string methodDefinition)
        {
            var file = $$"""
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static {{methodDefinition}}
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            var entryPoint = Assert.Single(entryPoints);
            Assert.Equal("Program", entryPoint.Name);
        }

        [WpfTheory]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        [InlineData("void Main(string args)")]
        [InlineData("void Main(char[] args)")]
        [InlineData("void Main<T>(T[] args)")]
        public static async Task DoNotFindMainEntryPointForDifferentArgumentTypes(string methodDefinition)
        {
            var file = $$"""
                using System;
                using System.Threading.Tasks;
                public class Program
                {
                    public static {{methodDefinition}}
                    {
                    } 
                }
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            Assert.Empty(entryPoints);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.ProjectSystemShims)]
        public static async Task FindTopLevelFunction()
        {
            var file = """
                using System;
                using System.Threading.Tasks;
                
                Console.WriteLine("Hello World");
                """;
            var entryPoints = await FindEntryPointsAsync(file);
            var entryPoint = Assert.Single(entryPoints);
            Assert.Equal(WellKnownMemberNames.TopLevelStatementsEntryPointTypeName, entryPoint.Name);
        }
    }
}
