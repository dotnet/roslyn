// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Test.Utilities;
using Xunit;

namespace Roslyn.Diagnostics.Analyzers.UnitTests;

using VerifyCS = CSharpCodeFixVerifier<
    ImportingConstructorShouldBeObsolete,
    CSharpImportingConstructorShouldBeObsoleteCodeFixProvider>;
using VerifyVB = VisualBasicCodeFixVerifier<
    ImportingConstructorShouldBeObsolete,
    BasicImportingConstructorShouldBeObsoleteCodeFixProvider>;

public sealed class ImportingConstructorShouldBeObsoleteTests
{
    [Theory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task SingleExpectedConstructor_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
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
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
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
    public async Task ObsoleteButNotError_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [[|Obsolete(MefConstruction.ImportingConstructorMessage, error: false)|]]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task ObsoleteButNotError_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <[|Obsolete(MefConstruction.ImportingConstructorMessage, False)|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
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
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task NotMarkedObsolete_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};

            [Export]
            class C {
                [[|ImportingConstructor|]]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task NotMarkedObsolete_PrimaryConstructor_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};

            [Export]
            [method: [|ImportingConstructor|]]
            class C() {
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            [method: ImportingConstructor]
            [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            class C() {
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
            LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task NotMarkedObsolete_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports System
            Imports {mefNamespace}

            <Export>
            Class C
                <[|ImportingConstructor|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <Obsolete(ImportingConstructorMessage, True)>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task NotMarkedObsoleteAddImports_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using {{mefNamespace}};

            [Export]
            class C {
                [[|ImportingConstructor|]]
                public C() { }
            }
            """;
        var helperSource = $$"""
            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        var fixedSource = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }
            """;

        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source, helperSource },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedState =
            {
                Sources = { fixedSource, helperSource },
            },
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task NotMarkedObsoleteAddImports_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports {mefNamespace}

            <Export>
            Class C
                <[|ImportingConstructor|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <Obsolete(ImportingConstructorMessage, True)>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task MessageArgumentOmitted_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};

            [Export]
            class C {
                [ImportingConstructor]
                [[|Obsolete|]]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task MessageArgumentOmitted_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports System
            Imports {mefNamespace}

            <Export>
            Class C
                <ImportingConstructor>
                <[|Obsolete|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <Obsolete(ImportingConstructorMessage, True)>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task ErrorArgumentOmitted_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [[|Obsolete(MefConstruction.ImportingConstructorMessage)|]]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
        }.RunAsync();
    }

    [Theory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task ErrorArgumentOmitted_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <[|Obsolete(MefConstruction.ImportingConstructorMessage)|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef.MefConstruction
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
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
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task IncorrectMessage_CSharpAsync(string mefNamespace)
    {
        var source = $$"""
            using System;
            using {{mefNamespace}};

            [Export]
            class C {
                [ImportingConstructor]
                [[|Obsolete("INCORRECT MESSAGE")|]]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $$"""
            using System;
            using {{mefNamespace}};
            using Microsoft.CodeAnalysis.Host.Mef;

            [Export]
            class C {
                [ImportingConstructor]
                [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
                public C() { }
            }

            namespace Microsoft.CodeAnalysis.Host.Mef {
                static class MefConstruction {
                    internal const string ImportingConstructorMessage = "This exported object must be obtained through the MEF export provider.";
                }
            }
            """,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 2,
        }.RunAsync();
    }

    [WindowsOnlyTheory]
    [InlineData("System.Composition")]
    [InlineData("System.ComponentModel.Composition")]
    public async Task IncorrectMessage_VisualBasicAsync(string mefNamespace)
    {
        var source = $"""
            Imports System
            Imports {mefNamespace}

            <Export>
            Class C
                <ImportingConstructor>
                <[|Obsolete("INCORRECT MESSAGE")|]>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """;
        await new VerifyVB.Test
        {
            TestState =
            {
                Sources = { source },
                AdditionalReferences = { AdditionalMetadataReferences.SystemComponentModelCompositionReference },
            },
            FixedCode = $"""
            Imports System
            Imports {mefNamespace}
            Imports Microsoft.CodeAnalysis.Host.Mef

            <Export>
            Class C
                <ImportingConstructor>
                <Obsolete(ImportingConstructorMessage, True)>
                Public Sub New()
                End Sub
            End Class

            Namespace Global.Microsoft.CodeAnalysis.Host.Mef
                Module MefConstruction
                    Friend Const ImportingConstructorMessage As String = "This exported object must be obtained through the MEF export provider."
                End Module
            End Namespace
            """,
            NumberOfIncrementalIterations = 2,
            NumberOfFixAllIterations = 2,
        }.RunAsync();
    }
}
