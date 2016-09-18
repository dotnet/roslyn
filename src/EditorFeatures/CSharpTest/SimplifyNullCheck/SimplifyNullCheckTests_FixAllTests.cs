// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.SimplifyNullCheck;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.SimplifyNullCheck
{
    public partial class SimplifyNullCheckTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInDocument1()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        {|FixAllInDocument:if|} (s == null) { throw new ArgumentNullException(nameof(s)); }
        if (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = s;
        _t = t;
    }
}",
@"
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.IfStatementEquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInDocument2()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        {|FixAllInDocument:if|} (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = s;
        _t = t;
    }
}",
@"
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.IfStatementEquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInDocument3()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        if (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = {|FixAllInDocument:s|};
        _t = t;
    }
}",
@"
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.ExpressionStatementEquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInDocument4()
        {
            await TestAsync(
@"
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        if (t == null) { throw new ArgumentNullException(nameof(t)); }
        _s = s;
        _t = {|FixAllInDocument:t|};
    }
}",
@"
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.ExpressionStatementEquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInDocumentDoNotTouchOtherDocuments()
        {
            await TestAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        {|FixAllInDocument:if|} (s == null) { throw new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}
        </Document>
        <Document>
using System;

class D
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>
",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}
        </Document>
        <Document>
using System;

class D
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.IfStatementEquivalenceKey);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyNullCheck)]
        public async Task FixAllInProject1()
        {
            await TestAsync(
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        {|FixAllInProject:if|} (s == null) { throw new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}
        </Document>
        <Document>
using System;

class D
{
    void M(string s, string t)
    {
        if (s == null) { throw new ArgumentNullException(nameof(s)); }
        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>
",
@"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}
        </Document>
        <Document>
using System;

class D
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
    }
}
        </Document>
    </Project>
</Workspace>", fixAllActionEquivalenceKey: SimplifyNullCheckCodeFixProvider.IfStatementEquivalenceKey);
        }
    }
}