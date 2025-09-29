// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.ImplementationIsObsoleteAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<Microsoft.CodeAnalysis.Analyzers.ImplementationIsObsoleteAnalyzer, Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Analyzers.UnitTests
{
    public class ImplementationIsObsoleteAnalyzerTests
    {
        private const string AttributeStringCSharp = """

            namespace System.Runtime.CompilerServices
            {
                internal class ImplementationIsObsoleteAttribute : System.Attribute { public ImplementationIsObsoleteAttribute(string url) { } }
            }

            """;
        [Fact]
        public async Task CSharp_VerifySameAssemblyAsync()
        {
            string source = $$"""
                {{AttributeStringCSharp}}

                [System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")]
                public interface IMyInterface { }

                class SomeClass : IMyInterface { }
                """;

            // Verify no diagnostic since interface is in the same assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyDifferentAssemblyAsync()
        {
            string source1 = AttributeStringCSharp + """


                [System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")]
                public interface IMyInterface { }

                public interface IMyOtherInterface : IMyInterface { }
                """;

            var source2 = """

                class SomeClass : IMyInterface { }

                class SomeOtherClass : IMyOtherInterface { }
                """;

            // Verify errors since interface is not in a friend assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { source1 },
                        },
                    },
                    AdditionalProjectReferences = { "DependencyProject" },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(2,7): error RS1042: Type SomeClass cannot implement interface IMyInterface because IMyInterface is obsolete for implementation. See https://example.com for more details.
                        VerifyCS.Diagnostic().WithSpan(2, 7, 2, 16).WithArguments("SomeClass", "IMyInterface", "https://example.com"),
                        // /0/Test0.cs(4,7): error RS1042: Type SomeOtherClass cannot implement interface IMyInterface because IMyInterface is obsolete for implementation. See https://example.com for more details.
                        VerifyCS.Diagnostic().WithSpan(4, 7, 4, 21).WithArguments("SomeOtherClass", "IMyInterface", "https://example.com"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task CSharp_VerifyDifferentFriendAssemblyAsync()
        {
            string source1 = $$"""

                [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestProject")]

                {{AttributeStringCSharp}}


                [System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")]
                public interface IMyInterface { }

                public interface IMyOtherInterface : IMyInterface { }
                """;

            var source2 = """

                class SomeClass : IMyInterface { }

                class SomeOtherClass : IMyOtherInterface { }
                """;

            // Verify no diagnostic since interface is in a friend assembly.
            await new VerifyCS.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { source1 },
                        },
                    },
                    AdditionalProjectReferences = { "DependencyProject" },
                },
            }.RunAsync();
        }

        private const string AttributeStringBasic = """

            Namespace System.Runtime.CompilerServices
                Friend Class ImplementationIsObsoleteAttribute 
                    Inherits System.Attribute

                    Public Sub New(url As String)
                    End Sub
                End Class
            End Namespace

            """;

        [Fact]
        public async Task Basic_VerifySameAssemblyAsync()
        {
            string source = $"""
                {AttributeStringBasic}

                <System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")>
                Public Interface IMyInterface
                End Interface

                Class SomeClass 
                    Implements IMyInterface 
                End Class
                """;

            // Verify no diagnostic since interface is in the same assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState = { Sources = { source } },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyDifferentAssemblyAsync()
        {
            string source1 = $"""
                {AttributeStringBasic}

                <System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")>
                Public Interface IMyInterface
                End Interface

                Public Interface IMyOtherInterface
                    Inherits IMyInterface
                End Interface

                """;

            var source2 = """

                Class SomeClass 
                    Implements IMyInterface 
                End Class

                Class SomeOtherClass
                    Implements IMyOtherInterface
                End Class

                """;

            // Verify errors since interface is not in a friend assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { source1 },
                        },
                    },
                    AdditionalProjectReferences = { "DependencyProject" },
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.vb(2,7): error RS1042: Type SomeClass cannot implement interface IMyInterface because IMyInterface is obsolete for implementation. See https://example.com for more details.
                        VerifyVB.Diagnostic().WithSpan(2, 7, 2, 16).WithArguments("SomeClass", "IMyInterface", "https://example.com"),
                        // /0/Test0.vb(6,7): error RS1042: Type SomeOtherClass cannot implement interface IMyInterface because IMyInterface is obsolete for implementation. See https://example.com for more details.
                        VerifyVB.Diagnostic().WithSpan(6, 7, 6, 21).WithArguments("SomeOtherClass", "IMyInterface", "https://example.com"),
                    },
                },
            }.RunAsync();
        }

        [Fact]
        public async Task Basic_VerifyDifferentFriendAssemblyAsync()
        {
            string source1 = $"""
                <Assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TestProject")>
                {AttributeStringBasic}

                <System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute("https://example.com")>
                Public Interface IMyInterface
                End Interface

                Public Interface IMyOtherInterface
                    Inherits IMyInterface
                End Interface
                """;

            var source2 = """
                Class SomeClass 
                    Implements IMyInterface 
                End Class

                Class SomeOtherClass
                    Implements IMyOtherInterface
                End Class
                """;

            // Verify no diagnostic since interface is in a friend assembly.
            await new VerifyVB.Test
            {
                ReferenceAssemblies = AdditionalMetadataReferences.DefaultWithoutRoslynSymbols,
                TestState =
                {
                    Sources = { source2 },
                    AdditionalProjects =
                    {
                        ["DependencyProject"] =
                        {
                            Sources = { source1 },
                        },
                    },
                    AdditionalProjectReferences = { "DependencyProject" },
                },
            }.RunAsync();
        }
    }
}
