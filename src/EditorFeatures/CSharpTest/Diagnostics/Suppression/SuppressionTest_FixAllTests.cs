// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Suppression
{
    public abstract partial class CSharpSuppressionTests : AbstractSuppressionDiagnosticTest
    {
        #region "Fix all occurrences tests"

        #region "Pragma disable tests"

        public abstract partial class CSharpPragmaWarningDisableSuppressionTests : CSharpSuppressionTests
        {
            public partial class UserInfoDiagnosticSuppressionTests : CSharpPragmaWarningDisableSuppressionTests
            {
                private string FixAllActionEquivalenceKey => FeaturesResources.SuppressWithPragma + UserDiagnosticAnalyzer.Decsciptor.Id;

                [Fact]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInDocument()
                {
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
class Class3 { }
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
#pragma warning disable InfoDiagnostic // InfoDiagnostic Title
class Class3 { }
#pragma warning restore InfoDiagnostic // InfoDiagnostic Title
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

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }

                [Fact]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInProject()
                {
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

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }

                [Fact]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInSolution()
                {
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

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }
            }
        }

        #endregion

        #region "SuppressMessageAttribute tests"

        public abstract partial class CSharpGlobalSuppressMessageSuppressionTests : CSharpSuppressionTests
        {
            public partial class UserInfoDiagnosticSuppressionTests : CSharpGlobalSuppressMessageSuppressionTests
            {
                private string FixAllActionEquivalenceKey => FeaturesResources.SuppressWithGlobalSuppressMessage + UserDiagnosticAnalyzer.Descriptor.Id;

                [Fact]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInDocument()
                {
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

                    var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]"
    .Replace("<", "&lt;").Replace(">", "&gt;");

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
        <Document FilePath=""GlobalSuppressions.cs"">" + addedGlobalSuppressions +
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
    </Project>
</Workspace>";

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }

                [Fact]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInProject()
                {
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

                    var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]"
    .Replace("<", "&lt;").Replace(">", "&gt;");

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
        <Document FilePath=""GlobalSuppressions.cs"">" + addedGlobalSuppressions +
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
    </Project>
</Workspace>";

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }

                [Fact(Skip ="TODO: File a GitHubIssue for test framework unable to handle multiple projects in solution with same file name.")]
                [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
                [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
                public void TestFixAllInSolution()
                {
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

                    var addedGlobalSuppressionsProject1 = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class3"")]

".Replace("<", "&lt;").Replace(">", "&gt;");

                    var addedGlobalSuppressionsProject2 = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""member"", Target = ""~M:Class1.Method~System.Int32"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class1"")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""InfoDiagnostic"", ""InfoDiagnostic:InfoDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"", Scope = ""type"", Target = ""~T:Class2"")]

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
        <Document FilePath=""GlobalSuppressions.cs"">" + addedGlobalSuppressionsProject1 +
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
        <Document FilePath=""GlobalSuppressions.cs"">" + addedGlobalSuppressionsProject2 +
        @"</Document>
    </Project>
</Workspace>";

                    Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
                }
            }
        }

        public partial class CSharpDiagnosticWithoutLocationSuppressionTests : CSharpSuppressionTests
        {
            private string FixAllActionEquivalenceKey => FeaturesResources.SuppressWithGlobalSuppressMessage + UserDiagnosticAnalyzer.Descriptor.Id;

            [Fact]
            [Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)]
            [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
            public void TestFixAllInProject()
            {
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

                var addedGlobalSuppressions = $@"
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""NoLocationDiagnostic"", ""NoLocationDiagnostic:NoLocationDiagnostic"", Justification = ""{FeaturesResources.SuppressionPendingJustification}"")]"
    .Replace("<", "&lt;").Replace(">", "&gt;");

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
        <Document FilePath=""GlobalSuppressions.cs"">" + addedGlobalSuppressions +
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
    </Project>
</Workspace>";

                Test(input, expected, compareTokens: false, fixAllActionEquivalenceKey: FixAllActionEquivalenceKey);
            }
        }

        #endregion

        #endregion
    }
}