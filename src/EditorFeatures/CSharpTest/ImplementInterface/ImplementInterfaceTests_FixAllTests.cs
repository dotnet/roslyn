// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using ImplementInterfaceCodeAction = Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService.ImplementInterfaceCodeAction;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ImplementInterface
{
    public partial class ImplementInterfaceTests
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|FixAllInDocument:I1|}, I2
{
    class C1 : I1, I2
    {
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    class C2 : I1, I2
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    class C2 : I1, I2
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : {|FixAllInProject:I1|}, I2
{
    class C1 : I1, I2
    {
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    class C2 : I1, I2
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>        
        <Document>
class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    public void F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : I1, I2
    {
        public void F1()
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
class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, {|FixAllInSolution:I2|}
{
    class C1 : I1, I2
    {
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    class C2 : I1, I2
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <ProjectReference>Assembly1</ProjectReference>
        <Document>
class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : I1, I2
    {
        void I2.F1()
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
class B3 : I1, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C3 : I1, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, index: 1);
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, {|FixAllInSolution:I2|}
{
    class C1 : I1, I2
    {
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    class C2 : I1, I2
    {
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B3 : I1, I2
{
    class C3 : I1, I2
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
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B1 : I1, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C1 : I1, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
        <Document>
class B2 : I1, I2
{
    void I2.F1()
    {
        throw new System.NotImplementedException();
    }

    class C2 : I1, I2
    {
        void I2.F1()
        {
            throw new System.NotImplementedException();
        }
    }
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public interface I1
{
    void F1();
}

public interface I2
{
    void F1();
}

class B3 : I1, I2
{
    class C3 : I1, I2
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected, index: 1);
        }

        #endregion
    }
}
