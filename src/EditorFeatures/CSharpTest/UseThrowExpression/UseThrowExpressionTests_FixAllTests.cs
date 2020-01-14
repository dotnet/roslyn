// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseThrowExpression
{
    public partial class UseThrowExpressionTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInDocument1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            {|FixAllInDocument:throw|} new ArgumentNullException(nameof(s));
        }

        if (t == null)
        {
            throw new ArgumentNullException(nameof(t));
        }

        _s = s;
        _t = t;
    }
}",
@"using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInDocument2()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        if (t == null)
        {
            {|FixAllInDocument:throw|} new ArgumentNullException(nameof(t));
        }

        _s = s;
        _t = t;
    }
}",
@"using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInDocument3()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            {|FixAllInDocument:throw new ArgumentNullException(nameof(s));|}
        }

        if (t == null)
        {
            throw new ArgumentNullException(nameof(t));
        }

        _s = s;
        _t = t;
    }
}",
@"using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInDocument4()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        if (t == null)
        {
            {|FixAllInDocument:throw new ArgumentNullException(nameof(t));|}
        }

        _s = s;
        _t = t;
    }
}",
@"using System;

class C
{
    void M(string s, string t)
    {
        _s = s ?? throw new ArgumentNullException(nameof(s));
        _t = t ?? throw new ArgumentNullException(nameof(t));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInDocumentDoNotTouchOtherDocuments()
        {
            await TestInRegularAndScriptAsync(
@"<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            {|FixAllInDocument:throw|} new ArgumentNullException(nameof(s));
        }

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
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseThrowExpression)]
        public async Task FixAllInProject1()
        {
            await TestInRegularAndScriptAsync(
@"<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class C
{
    void M(string s, string t)
    {
        if (s == null)
        {
            {|FixAllInProject:throw|} new ArgumentNullException(nameof(s));
        }

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
        if (s == null)
        {
            throw new ArgumentNullException(nameof(s));
        }

        _s = s;
    }
}
        </Document>
    </Project>
</Workspace>",
@"<Workspace>
    <Project Language = ""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
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
</Workspace>");
        }
    }
}
