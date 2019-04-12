// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsolete,
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsoleteCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsolete,
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsoleteCodeFixProvider>;

namespace Roslyn.Diagnostics.Analyzers.UnitTests
{
    public class ImportingConstructorShouldBeObsoleteTests
    {
        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task SingleExpectedConstructor_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
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
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
End Module
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
        public async Task ObsoleteButNotError_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: false)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(8, 6, 8, 73).WithArguments("C") },
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
        public async Task ObsoleteButNotError_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, False)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
End Module
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
End Module
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(8, 6, 8, 66).WithArguments("C") },
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
        public async Task NotMarkedObsolete_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    public C() {{ }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(""This exported object must be obtained through the MEF export provider."", error: true)]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(7, 6, 7, 26).WithArguments("C") },
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
        public async Task NotMarkedObsolete_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    Public Sub New()
    End Sub
End Class
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(""This exported object must be obtained through the MEF export provider."", True)>
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
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(7, 6, 7, 26).WithArguments("C") },
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
        public async Task MessageArgumentOmitted_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete]
    public C() {{ }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(""This exported object must be obtained through the MEF export provider."", error: true)]
    public C() {{ }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(8, 6, 8, 14).WithArguments("C") },
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
        public async Task MessageArgumentOmitted_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete>
    Public Sub New()
    End Sub
End Class
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(""This exported object must be obtained through the MEF export provider."", True)>
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
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(8, 6, 8, 14).WithArguments("C") },
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
        public async Task ErrorArgumentOmitted_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(8, 6, 8, 59).WithArguments("C") },
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
        public async Task ErrorArgumentOmitted_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
End Module
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
End Module
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(8, 6, 8, 59).WithArguments("C") },
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
        public async Task IncorrectMessage_CSharp(string mefNamespace)
        {
            var source = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""INCORRECT MESSAGE"";
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(""This exported object must be obtained through the MEF export provider."", error: true)]
    public C() {{ }}
}}

static class MefConstruction {{
    internal const string ImportingConstructorMessage = ""INCORRECT MESSAGE"";
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(8, 6, 8, 59).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }

        [Theory]
        [InlineData("System.Composition")]
        [InlineData("System.ComponentModel.Composition")]
        public async Task IncorrectMessage_VisualBasic(string mefNamespace)
        {
            var source = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""INCORRECT MESSAGE""
End Module
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(""This exported object must be obtained through the MEF export provider."", True)>
    Public Sub New()
    End Sub
End Class

Module MefConstruction
    Friend Const ImportingConstructorMessage As String = ""INCORRECT MESSAGE""
End Module
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(8, 6, 8, 59).WithArguments("C") },
                },
                FixedState =
                {
                    Sources = { fixedSource },
                },
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2,
            }.RunAsync();
        }
    }
}
