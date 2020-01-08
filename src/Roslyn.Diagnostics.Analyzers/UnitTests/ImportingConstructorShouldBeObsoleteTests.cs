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
using Microsoft.CodeAnalysis.Host.Mef;

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
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
Imports Microsoft.CodeAnalysis.Host.Mef

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
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
using Microsoft.CodeAnalysis.Host.Mef;

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: false)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};
using Microsoft.CodeAnalysis.Host.Mef;

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(9, 6, 9, 73).WithArguments("C") },
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
Imports Microsoft.CodeAnalysis.Host.Mef

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, False)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}
Imports Microsoft.CodeAnalysis.Host.Mef

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(9, 6, 9, 66).WithArguments("C") },
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

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(Microsoft.CodeAnalysis.Host.Mef.MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
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

        [Theory(Skip = "https://github.com/dotnet/roslyn/issues/31720")]
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

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(Microsoft.CodeAnalysis.Host.Mef.MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
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

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(Microsoft.CodeAnalysis.Host.Mef.MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
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

Namespace Global.Microsoft.CodeAnalysis.Host.Mef
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(Microsoft.CodeAnalysis.Host.Mef.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
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
using Microsoft.CodeAnalysis.Host.Mef;

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};
using Microsoft.CodeAnalysis.Host.Mef;

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(9, 6, 9, 59).WithArguments("C") },
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
Imports Microsoft.CodeAnalysis.Host.Mef

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}
Imports Microsoft.CodeAnalysis.Host.Mef

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(9, 6, 9, 59).WithArguments("C") },
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
    [Obsolete(""INCORRECT MESSAGE"")]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";
            var fixedSource = $@"
using System;
using {mefNamespace};

[Export]
class C {{
    [ImportingConstructor]
    [Obsolete(Microsoft.CodeAnalysis.Host.Mef.MefConstruction.ImportingConstructorMessage, error: true)]
    public C() {{ }}
}}

namespace Microsoft.CodeAnalysis.Host.Mef {{
    static class MefConstruction {{
        internal const string ImportingConstructorMessage = ""This exported object must be obtained through the MEF export provider."";
    }}
}}
";

            await new VerifyCS.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyCS.Diagnostic().WithSpan(8, 6, 8, 35).WithArguments("C") },
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
    <Obsolete(""INCORRECT MESSAGE"")>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";
            var fixedSource = $@"
Imports System
Imports {mefNamespace}

<Export>
Class C
    <ImportingConstructor>
    <Obsolete(Microsoft.CodeAnalysis.Host.Mef.ImportingConstructorMessage, True)>
    Public Sub New()
    End Sub
End Class

Namespace Global.Microsoft.CodeAnalysis.Host.Mef
    Module MefConstruction
        Friend Const ImportingConstructorMessage As String = ""This exported object must be obtained through the MEF export provider.""
    End Module
End Namespace
";

            await new VerifyVB.Test
            {
                TestState =
                {
                    Sources = { source },
                    AdditionalReferences = { AdditionalMetadataReferences.SystemCompositionReference, AdditionalMetadataReferences.SystemComponentModelCompositionReference },
                    ExpectedDiagnostics = { VerifyVB.Diagnostic().WithSpan(8, 6, 8, 35).WithArguments("C") },
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
