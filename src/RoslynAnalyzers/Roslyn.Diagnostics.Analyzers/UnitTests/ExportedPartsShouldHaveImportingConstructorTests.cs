// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ExportedPartsShouldHaveImportingConstructor,
    Roslyn.Diagnostics.Analyzers.ExportedPartsShouldHaveImportingConstructorCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ExportedPartsShouldHaveImportingConstructor,
    Roslyn.Diagnostics.Analyzers.ExportedPartsShouldHaveImportingConstructorCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ExportedPartsShouldHaveImportingConstructorTests
    {
        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task SingleExpectedConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task SingleExpectedConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition", true)]
        [InlineData("System.Composition", false)]
        [InlineData("System.ComponentModel.Composition", true)]
        [InlineData("System.ComponentModel.Composition", false)]
        public async Task NotInheritedAttribute_CSharpAsync(string mefNamespace, bool reflectionInherited)
        {
            var source = $$"""
                using {{mefNamespace}};

                [System.AttributeUsage(System.AttributeTargets.All, Inherited = {{(reflectionInherited ? "true" : "false")}})]
                class NotInheritedExportAttribute : System.Attribute { }

                [NotInheritedExport]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }

                class D : C {
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition", true)]
        [InlineData("System.Composition", false)]
        [InlineData("System.ComponentModel.Composition", true)]
        [InlineData("System.ComponentModel.Composition", false)]
        public async Task NotInheritedAttribute_VisualBasicAsync(string mefNamespace, bool reflectionInherited)
        {
            var source = $"""
                Imports {mefNamespace}

                <System.AttributeUsage(System.AttributeTargets.All, Inherited:={reflectionInherited})>
                Class NotInheritedExportAttribute
                    Inherits System.Attribute
                End Class

                <NotInheritedExport>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class

                Class D
                    Inherits C
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2490")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InheritedExportAttribute_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [InheritedExport]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }

                class D : C {
                }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [InheritedExport]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }

                class D : C {
                    [ImportingConstructor]
                    public D()
                    {
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(4, 2, 4, 17).WithArguments("D") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2490")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InheritedExportAttribute_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <InheritedExport>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class

                Class D
                    Inherits C
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <InheritedExport>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class

                Class D
                    Inherits C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(4, 2, 4, 17).WithArguments("D") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ExportAttributeNotInherited_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }

                class D : C { }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ExportAttributeNotInherited_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class

                Class D
                    Inherits C
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndImplicitStaticConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    private static readonly object _gate = new object();

                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndImplicitStaticConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    Private Shared ReadOnly _gate As Object = New Object()

                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndExplicitStaticConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    private static readonly object _gate;

                    static C() { _gate = new object(); }

                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndExplicitStaticConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    Private Shared ReadOnly _gate As Object

                    Shared Sub New()
                        _gate = New Object()
                    End Sub

                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C { }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C()
                    {
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructorAddImport_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                [{{mefNamespace}}.Export]
                class C { }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [{{mefNamespace}}.Export]
                class C {
                    [ImportingConstructor]
                    public C()
                    {
                    }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(1, 2, 1, mefNamespace.Length + 9).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructorAddImport_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                <{mefNamespace}.Export>
                Class C
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <{mefNamespace}.Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(1, 2, 1, mefNamespace.Length + 9).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructorPlacement_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    private readonly int _value = 0;

                    private int Value => _value;
                }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    private readonly int _value = 0;

                    [ImportingConstructor]
                    public C()
                    {
                    }

                    private int Value => _value;
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructorPlacement_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    Private ReadOnly _value1 As Integer = 0

                    Private ReadOnly Property Value
                        Get
                            return _value1
                        End Get
                    End Property
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    Private ReadOnly _value1 As Integer = 0

                    <ImportingConstructor>
                    Public Sub New()
                    End Sub

                    Private ReadOnly Property Value
                        Get
                            return _value1
                        End Get
                    End Property
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(3, 2, 3, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MissingAttributeConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    public C() { }
                }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 5, 5, 19).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MissingAttributeConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    Public Sub New()
                    End Sub
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 5, 5, 21).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MissingAttributeConstructorAddImport_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                [{{mefNamespace}}.Export]
                class C {
                    public C() { }
                }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [{{mefNamespace}}.Export]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(3, 5, 3, 19).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MissingAttributeConstructorAddImport_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                <{mefNamespace}.Export>
                Class C
                    Public Sub New()
                    End Sub
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <{mefNamespace}.Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(3, 5, 3, 21).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task NonPublicConstructor_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    internal C() { }
                }
                """;
            var fixedSource = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C() { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 6, 5, 26).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task NonPublicConstructor_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Friend Sub New()
                    End Sub
                End Class
                """;
            var fixedSource = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 6, 5, 26).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MultipleConstructors_CSharpAsync(string mefNamespace)
        {
            var source = $$"""
                using {{mefNamespace}};

                [Export]
                class C {
                    [ImportingConstructor]
                    public C() { }

                    internal C(string x) { }

                    private C(int x) { }
                }
                """;

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic().WithSpan(8, 5, 8, 29).WithArguments("C"),
                        VerifyCS.Diagnostic().WithSpan(10, 5, 10, 25).WithArguments("C"),
                    },
                },
                FixedState =
                {
                    // No code fix is offered for this case
                    Sources = { source },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MultipleConstructors_VisualBasicAsync(string mefNamespace)
        {
            var source = $"""
                Imports {mefNamespace}

                <Export>
                Class C
                    <ImportingConstructor>
                    Public Sub New()
                    End Sub

                    Friend Sub New(x as String)
                    End Sub

                    Private Sub New(x as Integer)
                    End Sub
                End Class
                """;

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics =
                    {
                        VerifyVB.Diagnostic().WithSpan(9, 5, 9, 32).WithArguments("C"),
                        VerifyVB.Diagnostic().WithSpan(12, 5, 12, 34).WithArguments("C"),
                    },
                },
                FixedState =
                {
                    // No code fix is offered for this case
                    Sources = { source },
                },
            }.RunAsync();
        }
    }
}
