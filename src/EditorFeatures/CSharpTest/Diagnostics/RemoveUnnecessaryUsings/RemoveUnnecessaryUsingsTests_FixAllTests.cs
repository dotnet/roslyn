// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.RemoveUnnecessaryUsings
{
    public partial class RemoveUnnecessaryUsingsTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
{|FixAllInDocument:using System;
using System.Collections.Generic;|}

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;
using System.Collections.Generic;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;
using System.Collections.Generic;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
{|FixAllInProject:using System;
using System.Collections.Generic;|}

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;
using System.Collections.Generic;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnnecessaryImports)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
{|FixAllInSolution:using System;
using System.Collections.Generic;|}

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;
using System.Collections.Generic;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;

class Program
{
    public Int32 x;
}
        </Document>
        <Document>
using System;

class Program2
{
    public Int32 x;
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
using System;

class Program3
{
    public Int32 x;
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: null);
        }

        #endregion
    }
}
