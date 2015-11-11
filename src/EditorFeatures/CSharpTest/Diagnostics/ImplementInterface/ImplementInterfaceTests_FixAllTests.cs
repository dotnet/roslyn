// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using ImplementInterfaceCodeAction = Microsoft.CodeAnalysis.ImplementInterface.AbstractImplementInterfaceService.ImplementInterfaceCodeAction;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.ImplementInterface
{
    public partial class ImplementInterfaceTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        #region "Fix all occurrences tests"

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInDocument()
        {
            var fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "global::I1", explicitly: false, abstractly: false, throughMember: null, codeActionTypeName: typeof(ImplementInterfaceCodeAction).FullName);

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
using System;

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
        throw new NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new NotImplementedException();
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

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInProject()
        {
            var fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "global::I1", explicitly: false, abstractly: false, throughMember: null, codeActionTypeName: typeof(ImplementInterfaceCodeAction).FullName);

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
using System;

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
        throw new NotImplementedException();
    }

    class C1 : I1, I2
    {
        public void F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : I1, I2
{
    public void F1()
    {
        throw new NotImplementedException();
    }

    class C2 : I1, I2
    {
        public void F1()
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
class B3 : I1, I2
{
    class C3 : I1, I2
    {
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution()
        {
            var fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "global::I2", explicitly: true, abstractly: false, throughMember: null, codeActionTypeName: typeof(ImplementInterfaceCodeAction).FullName);

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
using System;

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
        throw new NotImplementedException();
    }

    class C1 : I1, I2
    {
        void I2.F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : I1, I2
{
    void I2.F1()
    {
        throw new NotImplementedException();
    }

    class C2 : I1, I2
    {
        void I2.F1()
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

class B3 : I1, I2
{
    void I2.F1()
    {
        throw new NotImplementedException();
    }

    class C3 : I1, I2
    {
        void I2.F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
    </Project>
</Workspace>";

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        [WpfFact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsImplementAbstractClass)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task TestFixAllInSolution_DifferentAssemblyWithSameTypeName()
        {
            var fixAllActionEquivalenceKey = ImplementInterfaceCodeAction.GetCodeActionEquivalenceKey("Assembly1", "global::I2", explicitly: true, abstractly: false, throughMember: null, codeActionTypeName: typeof(ImplementInterfaceCodeAction).FullName);

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
using System;

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
        throw new NotImplementedException();
    }

    class C1 : I1, I2
    {
        void I2.F1()
        {
            throw new NotImplementedException();
        }
    }
}
        </Document>
        <Document>
using System;

class B2 : I1, I2
{
    void I2.F1()
    {
        throw new NotImplementedException();
    }

    class C2 : I1, I2
    {
        void I2.F1()
        {
            throw new NotImplementedException();
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

            await TestAsync(input, expected, compareTokens: false, fixAllActionEquivalenceKey: fixAllActionEquivalenceKey);
        }

        #endregion
    }
}
