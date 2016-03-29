// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.ImplementAbstractClass;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ImplementAbstractClass
{
    public partial class ImplementAbstractClassTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "global::A1");

            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class {|FixAllInDocument:B1|} : A1
{
    class C1 : A1, I1
    {
    }
}
        </Document>
        <Document>
class B2 : A1
{
    class C2 : A1, I1
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class B3 : A1
{
    class C3 : A1, I1
    {
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

public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class B1 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : A1
{
    class C2 : A1, I1
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class B3 : A1
{
    class C3 : A1, I1
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "global::A1");

            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class {|FixAllInProject:B1|} : A1
{
    class C1 : A1, I1
    {
    }
}
        </Document>
        <Document>
class B2 : A1
{
    class C2 : A1, I1
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class B3 : A1
{
    class C3 : A1, I1
    {
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

public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class B1 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class B3 : A1
{
    class C3 : A1, I1
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "global::A1");

            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class {|FixAllInSolution:B1|} : A1
{
    class C1 : A1, I1
    {
    }
}
        </Document>
        <Document>
class B2 : A1
{
    class C2 : A1, I1
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
class B3 : A1
{
    class C3 : A1, I1
    {
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

public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class B1 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
using System;

class B3 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C3 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_DifferentAssemblyWithSameTypeName()
        {
            var fixAllActionId = ImplementAbstractClassCodeFixProvider.GetCodeActionId("Assembly1", "global::A1");

            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class {|FixAllInSolution:B1|} : A1
{
    class C1 : A1, I1
    {
    }
}
        </Document>
        <Document>
class B2 : A1
{
    class C2 : A1, I1
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F2();
}

class B3 : A1
{
    class C3 : A1, I1
    {
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

public abstract class A1
{
    public abstract void F1();
}

public interface I1
{
    void F2();
}

class B1 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : A1
{
    public override void F1()
    {
        throw new NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public abstract class A1
{
    public abstract void F2();
}

class B3 : A1
{
    class C3 : A1, I1
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionId);
        }

        #endregion
    }
}
