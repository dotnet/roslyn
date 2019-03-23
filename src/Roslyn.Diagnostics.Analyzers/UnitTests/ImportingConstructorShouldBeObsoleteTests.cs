// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;
using VerifyCS = Test.Utilities.CSharpCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsolete,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;
using VerifyVB = Test.Utilities.VisualBasicCodeFixVerifier<
    Roslyn.Diagnostics.Analyzers.ImportingConstructorShouldBeObsolete,
    Microsoft.CodeAnalysis.Testing.EmptyCodeFixProvider>;

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

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
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

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
                },
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

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(5, 2, 5, 8).WithArguments("C") },
                },
            }.RunAsync();
        }
    }
}
