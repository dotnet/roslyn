// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public abstract class CSharpRemoveSuppressionTests : CSharpSuppressionTests
    {
        protected override bool IncludeSuppressedDiagnostics => true;
        protected override bool IncludeUnsuppressedDiagnostics => false;
        protected override int CodeActionIndex => 0;
        private string FixAllActionEquivalenceKey => FeaturesResources.RemoveSuppressionEquivalenceKeyPrefix + UserDiagnosticAnalyzer.Decsciptor.Id;

        protected class UserDiagnosticAnalyzer : DiagnosticAnalyzer
        {
            private readonly bool _reportDiagnosticsWithoutLocation;
            public static readonly DiagnosticDescriptor Decsciptor =
                new DiagnosticDescriptor("InfoDiagnostic", "InfoDiagnostic Title", "InfoDiagnostic", "InfoDiagnostic", DiagnosticSeverity.Info, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(Decsciptor);
                }
            }

            public UserDiagnosticAnalyzer(bool reportDiagnosticsWithoutLocation = false)
            {
                _reportDiagnosticsWithoutLocation = reportDiagnosticsWithoutLocation;
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration);
            }

            public void AnalyzeNode(SyntaxNodeAnalysisContext context)
            {
                var classDecl = (ClassDeclarationSyntax)context.Node;
                var location = _reportDiagnosticsWithoutLocation ? Location.None : classDecl.Identifier.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(Decsciptor, location));
            }
        }

        public class CSharpDiagnosticWithLocationRemoveSuppressionTests : CSharpRemoveSuppressionTests
        {
            internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                    new UserDiagnosticAnalyzer(), new CSharpSuppressionCodeFixProvider());
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemovePragmaSuppression()
            {
                await TestAsync(
        @"
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
[|class Class|]
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}",
        @"
using System;

class Class
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemovePragmaSuppression_AdjacentTrivia()
            {
                await TestAsync(
        @"
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class1 { }
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
[|class Class2|]
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}",
        @"
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class1 { }
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
class Class2
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemovePragmaSuppression_TriviaWithMultipleIDs()
            {
                await TestAsync(
        @"
using System;

#pragma warning disable InfoDiagnostic, SomeOtherDiagnostic
[|class Class|]
#pragma warning restore InfoDiagnostic, SomeOtherDiagnostic
{
    int Method()
    {
        int x = 0;
    }
}",
        @"
using System;

#pragma warning disable InfoDiagnostic, SomeOtherDiagnostic
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
class Class
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
#pragma warning restore InfoDiagnostic, SomeOtherDiagnostic
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemovePragmaSuppression_WithEnclosingSuppression()
            {
                await TestAsync(
        @"
#pragma warning disable InfoDiagnostic
using System;

#pragma warning disable InfoDiagnostic
[|class Class|]
#pragma warning restore InfoDiagnostic
{
    int Method()
    {
        int x = 0;
    }
}",
        @"
#pragma warning disable InfoDiagnostic
using System;

#pragma warning restore InfoDiagnostic
class Class
#pragma warning disable InfoDiagnostic
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemoveLocalAttributeSuppression()
            {
                await TestAsync(
        $@"
using System;

[System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"")]
[|class Class|]
{{
    int Method()
    {{
        int x = 0;
    }}
}}",
        @"
using System;

class Class
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemoveLocalAttributeSuppression2()
            {
                await TestAsync(
        $@"
using System;

class Class1
{{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"")]           
    [|class Class2|]
    {{
        int Method()
        {{
            int x = 0;
        }}
    }}
}}",
        @"
using System;

class Class1
{
    class Class2
    {
        int Method()
        {
            int x = 0;
        }
    }
}");
            }

            [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            public async Task TestRemoveGlobalAttributeSuppression()
            {
                await TestAsync(
        $@"
using System;

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class"")]

[|class Class|]
{{
    int Method()
    {{
        int x = 0;
    }}
}}",
        @"
using System;

class Class
{
    int Method()
    {
        int x = 0;
    }
}");
            }

            #region "Fix all occurrences tests"

            #region "Pragma disable tests"

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInDocument_RemovePragmaSuppressions()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
{|FixAllInDocument:class Class1|}
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class2
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>";

                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInProject_RemovePragmaSuppressions()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
{|FixAllInProject:class Class1|}
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class2
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
        <Document>
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class3
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>";

                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInSolution()
            {
                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
{|FixAllInSolution:class Class1|}
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class2
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
        <Document>
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class3
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class1
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
    int Method()
    {
        int x = 0;
    }
}

#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class2
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
{
}
        </Document>
    </Project>
</Workspace>";

                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
    </Project>
</Workspace>";

                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }

            #endregion

            #region "SuppressMessageAttribute tests"

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInDocument_RemoveAttributeSuppressions()
            {
                var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

{|FixAllInDocument:class Class1|}
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";

                var newGlobalSuppressionsFile = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]

".Replace("<", "&lt;").Replace(">", "&gt;");
                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + newGlobalSuppressionsFile +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";

                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInProject_RemoveAttributeSuppressions()
            {
                var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

{|FixAllInProject:class Class1|}
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";

                var newGlobalSuppressionsFile = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.


";
                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + newGlobalSuppressionsFile +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";



                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInSolution_RemoveAttributeSuppression()
            {
                var addedGlobalSuppressionsProject1 = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                var addedGlobalSuppressionsProject2 = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

{|FixAllInSolution:class Class1|}
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + addedGlobalSuppressionsProject1 +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressionsProject2 +
    @"</Document>
    </Project>
</Workspace>";

                var newGlobalSuppressionsFile = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.


";
                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + newGlobalSuppressionsFile +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + newGlobalSuppressionsFile +
    @"</Document>
    </Project>
</Workspace>";

                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }
        }

        public class CSharpDiagnosticWithoutLocationRemoveSuppressionTests : CSharpRemoveSuppressionTests
        {
            internal override Tuple<DiagnosticAnalyzer, ISuppressionFixProvider> CreateDiagnosticProviderAndFixer(Workspace workspace)
            {
                return new Tuple<DiagnosticAnalyzer, ISuppressionFixProvider>(
                    new UserDiagnosticAnalyzer(reportDiagnosticsWithoutLocation: true), new CSharpSuppressionCodeFixProvider());
            }

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public async Task TestFixAllInProject_RemoveAttributeSuppressions()
            {
                var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>{|FixAllInProject:|}
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";

                var newGlobalSuppressionsFile = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.


";
                var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document>
class Class3
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly1.cs"">" + newGlobalSuppressionsFile +
    @"</Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Class1
{
    int Method()
    {
        int x = 0;
    }
}

class Class2
{
}
        </Document>
        <Document FilePath=""GlobalSuppressions_Assembly2.cs"">" + addedGlobalSuppressions +
    @"</Document>
    </Project>
</Workspace>";



                await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }
        }

        #endregion

        #endregion
    }
}