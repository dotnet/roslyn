// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.AddExplicitCast
{
    public partial class AddExplicitCastTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C {}
class D : C {}
class Program
{
    public void F()
    {
        C c;
        D d = {|FixAllInDocument:c|};
        D d2 = c;
    }
}
        </Document>
    </Project>
</Workspace>";

            var expected = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
class C {}
class D : C {}
class Program
{
    public void F()
    {
        C c;
        D d = (D)c;
        D d2 = (D)c;
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        #endregion
    }
}
