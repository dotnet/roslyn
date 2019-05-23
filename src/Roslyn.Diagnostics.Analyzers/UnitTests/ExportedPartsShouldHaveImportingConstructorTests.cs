// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public async Task SingleExpectedConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task SingleExpectedConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition", true)]
        [InlineData("System.Composition", false)]
        [InlineData("System.ComponentModel.Composition", true)]
        [InlineData("System.ComponentModel.Composition", false)]
        public async Task NotInheritedAttribute_CSharp(string mefNamespace, bool reflectionInherited)
        {
            var source = $@"
using {mefNamespace};

[System.AttributeUsage(System.AttributeTargets.All, Inherited = {(reflectionInherited ? "true" : "false")})]
class NotInheritedExportAttribute : System.Attribute {{ }}

[NotInheritedExport]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}

class D : C {{
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition", true)]
        [InlineData("System.Composition", false)]
        [InlineData("System.ComponentModel.Composition", true)]
        [InlineData("System.ComponentModel.Composition", false)]
        public async Task NotInheritedAttribute_VisualBasic(string mefNamespace, bool reflectionInherited)
        {
            var source = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn-analyzers/issues/2490")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InheritedExportAttribute_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[InheritedExport]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}

class D : C {{
}}
";
            var fixedSource = $@"
using {mefNamespace};

[InheritedExport]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}

class D : C {{
    [ImportingConstructor]
    public D()
    {{
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
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
        public async Task InheritedExportAttribute_VisualBasic(string mefNamespace)
        {
            var source = $@"
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
";
            var fixedSource = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
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
        public async Task ExportAttributeNotInherited_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}

class D : C {{ }}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ExportAttributeNotInherited_VisualBasic(string mefNamespace)
        {
            var source = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndImplicitStaticConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    private static readonly object _gate = new object();

    [ImportingConstructor]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndImplicitStaticConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
    Private Shared ReadOnly _gate As Object = New Object()

    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndExplicitStaticConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    private static readonly object _gate;

    static C() {{ _gate = new object(); }}

    [ImportingConstructor]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task InstanceAndExplicitStaticConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                },
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{ }}
";
            var fixedSource = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C()
    {{
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/31720")]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
End Class
";
            var fixedSource = $@"
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
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
        public async Task ImplicitConstructorPlacement_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    private readonly int _value = 0;

    private int Value => _value;
}}
";
            var fixedSource = $@"
using {mefNamespace};

[Export]
class C {{
    private readonly int _value = 0;

    [ImportingConstructor]
    public C()
    {{
    }}

    private int Value => _value;
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/31720")]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task ImplicitConstructorPlacement_VisualBasic(string mefNamespace)
        {
            var source = $@"
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
";
            var fixedSource = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(4, 2, 4, 8).WithArguments("C") },
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
        public async Task MissingAttributeConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    public C() {{ }}
}}
";
            var fixedSource = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(6, 5, 6, 19).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
            }.RunAsync();
        }

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/31720")]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task MissingAttributeConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
    Public Sub New()
    End Sub
End Class
";
            var fixedSource = $@"
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(6, 5, 6, 21).WithArguments("C") },
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
        public async Task NonPublicConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    internal C() {{ }}
}}
";
            var fixedSource = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(6, 6, 6, 26).WithArguments("C") },
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
        public async Task NonPublicConstructor_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Friend Sub New()
    End Sub
End Class
";
            var fixedSource = $@"
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(6, 6, 6, 26).WithArguments("C") },
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
        public async Task MultipleConstructors_CSharp(string mefNamespace)
        {
            var source = $@"
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}

    internal C(string x) {{ }}

    private C(int x) {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics =
                    {
                        VerifyCS.Diagnostic().WithSpan(9, 5, 9, 29).WithArguments("C"),
                        VerifyCS.Diagnostic().WithSpan(11, 5, 11, 25).WithArguments("C"),
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
        public async Task MultipleConstructors_VisualBasic(string mefNamespace)
        {
            var source = $@"
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
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics =
                    {
                        VerifyVB.Diagnostic().WithSpan(10, 5, 10, 32).WithArguments("C"),
                        VerifyVB.Diagnostic().WithSpan(13, 5, 13, 34).WithArguments("C"),
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
