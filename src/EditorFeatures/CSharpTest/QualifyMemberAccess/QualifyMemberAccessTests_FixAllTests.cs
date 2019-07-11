// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.QualifyMemberAccess
{
    public partial class QualifyMemberAccessTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsQualifyMemberAccess)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_QualifyMemberAccess()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    int Property { get; set; }
    int OtherProperty { get; set; }

    void M()
    {
        {|FixAllInSolution:Property|} = 1;
        var x = OtherProperty;
    }
}
        </Document>
        <Document>
using System;

class D
{
    string StringProperty { get; set; }
    int field;

    void N()
    {
        StringProperty = string.Empty;
        field = 0; // ensure this doesn't get qualified
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    int Property { get; set; }
    int OtherProperty { get; set; }

    void M()
    {
        this.Property = 1;
        var x = this.OtherProperty;
    }
}
        </Document>
        <Document>
using System;

class D
{
    string StringProperty { get; set; }
    int field;

    void N()
    {
        this.StringProperty = string.Empty;
        field = 0; // ensure this doesn't get qualified
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(
                initialMarkup: input,
                expectedMarkup: expected,
                options: Option(CodeStyleOptions.QualifyPropertyAccess, true, NotificationOption.Suggestion));
        }
    }
}
