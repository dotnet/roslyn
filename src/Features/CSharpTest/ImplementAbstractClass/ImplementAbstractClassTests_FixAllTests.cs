// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementAbstractClass;

public partial class ImplementAbstractClassTests
{
    #region "Fix all occurrences tests"

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInDocument()
    {
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
        throw new System.NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
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

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInProject()
    {
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
        throw new System.NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : A1
{
    public override void F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
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

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInSolution()
    {
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
        throw new System.NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : A1
{
    public override void F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
class B3 : A1
{
    public override void F1()
    {
        throw new System.NotImplementedException();
    }

    class C3 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
    </Project>
</Workspace>";

        await TestInRegularAndScriptAsync(input, expected);
    }

    [Fact]
    [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
    [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
    public async Task TestFixAllInSolution_DifferentAssemblyWithSameTypeName()
    {
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
        throw new System.NotImplementedException();
    }

    class C1 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : A1
{
    public override void F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : A1, I1
    {
        public override void F1()
        {
            throw new System.NotImplementedException();
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

        await TestInRegularAndScriptAsync(input, expected);
    }

    #endregion
}
