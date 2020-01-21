// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.GenerateDefaultConstructors;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.GenerateDefaultConstructors
{
    public class GenerateDefaultConstructorsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new GenerateDefaultConstructorsCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestProtectedBase()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    protected B(int x)
    {
    }
}",
@"class C : B
{
    protected C(int x) : base(x)
    {
    }
}

class B
{
    protected B(int x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestPublicBase()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    public B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

class B
{
    public B(int x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestInternalBase()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(int x)
    {
    }
}",
@"class C : B
{
    internal C(int x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestPrivateBase()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    private B(int x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestRefOutParams()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(ref int x, out string s, params bool[] b)
    {
    }
}",
@"class C : B
{
    internal C(ref int x, out string s, params bool[] b) : base(ref x, out s, b)
    {
    }
}

class B
{
    internal B(ref int x, out string s, params bool[] b)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFix1()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
@"class C : B
{
    internal C(int x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFix2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
@"class C : B
{
    protected C(string x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
index: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestRefactoring1()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
@"class C : B
{
    public C(bool x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
@"class C : B
{
    public C(bool x) : base(x)
    {
    }

    protected C(string x) : base(x)
    {
    }

    internal C(int x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
    public C(bool x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
@"class C : B
{
    public C(bool x)
    {
    }

    protected C(string x) : base(x)
    {
    }

    internal C(int x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }

    protected B(string x)
    {
    }

    public B(bool x)
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixAll_WithTuples()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
    public C((bool, bool) x)
    {
    }
}

class B
{
    internal B((int, int) x)
    {
    }

    protected B((string, string) x)
    {
    }

    public B((bool, bool) x)
    {
    }
}",
@"class C : B
{
    public C((bool, bool) x)
    {
    }

    protected C((string, string) x) : base(x)
    {
    }

    internal C((int, int) x) : base(x)
    {
    }
}

class B
{
    internal B((int, int) x)
    {
    }

    protected B((string, string) x)
    {
    }

    public B((bool, bool) x)
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestMissing1()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C : [||]B
{
    public C(int x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestDefaultConstructorGeneration_1()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
    public C(int y)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int y)
    {
    }

    internal C(int x) : base(x)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(889349, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/889349")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestDefaultConstructorGeneration_2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
    private C(int y)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}",
@"class C : B
{
    internal C(int x) : base(x)
    {
    }

    private C(int y)
    {
    }
}

class B
{
    internal B(int x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestFixCount1()
        {
            await TestActionCountAsync(
@"class C : [||]B
{
}

class B
{
    public B(int x)
    {
    }
}",
count: 1);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        [WorkItem(544070, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544070")]
        public async Task TestException1()
        {
            await TestInRegularAndScriptAsync(
@"using System;
class Program : Excep[||]tion
{
}",
@"using System;
using System.Runtime.Serialization;

class Program : Exception
{
    public Program()
    {
    }

    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}",
index: 4);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException2()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program : [||]Exception
{
    public Program()
    {
    }

    static void Main(string[] args)
    {
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

class Program : Exception
{
    public Program()
    {
    }

    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    static void Main(string[] args)
    {
    }
}",
index: 3);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException3()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program : [||]Exception
{
    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }

    static void Main(string[] args)
    {
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program : Exception
{
    public Program()
    {
    }

    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }

    static void Main(string[] args)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestException4()
        {
            await TestInRegularAndScriptAsync(
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program : [||]Exception
{
    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }

    static void Main(string[] args)
    {
    }
}",
@"using System;
using System.Collections.Generic;
using System.Linq;

class Program : Exception
{
    public Program()
    {
    }

    public Program(string message) : base(message)
    {
    }

    public Program(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected Program(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context)
    {
    }

    static void Main(string[] args)
    {
    }
}",
index: 2);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task Tuple()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    public B((int, string) x)
    {
    }
}",
@"class C : B
{
    public C((int, string) x) : base(x)
    {
    }
}

class B
{
    public B((int, string) x)
    {
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors), Test.Utilities.CompilerTrait(Test.Utilities.CompilerFeature.Tuples)]
        public async Task TupleWithNames()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

class B
{
    public B((int a, string b) x)
    {
    }
}",
@"class C : B
{
    public C((int a, string b) x) : base(x)
    {
    }
}

class B
{
    public B((int a, string b) x)
    {
    }
}");
        }

        [WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestGenerateFromDerivedClass()
        {
            await TestInRegularAndScriptAsync(
@"class Base
{
    public Base(string value)
    {
    }
}

class [||]Derived : Base
{
}",
@"class Base
{
    public Base(string value)
    {
    }
}

class Derived : Base
{
    public Derived(string value) : base(value)
    {
    }
}");
        }

        [WorkItem(6541, "https://github.com/dotnet/Roslyn/issues/6541")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateConstructor)]
        public async Task TestGenerateFromDerivedClass2()
        {
            await TestInRegularAndScriptAsync(
@"class Base
{
    public Base(int a, string value = null)
    {
    }
}

class [||]Derived : Base
{
}",
@"class Base
{
    public Base(int a, string value = null)
    {
    }
}

class Derived : Base
{
    public Derived(int a, string value = null) : base(a, value)
    {
    }
}");
        }

        [WorkItem(19953, "https://github.com/dotnet/roslyn/issues/19953")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestNotOnEnum()
        {
            await TestMissingInRegularAndScriptAsync(
@"enum [||]E
{
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    protected B(int x)
    {
    }
}",
@"abstract class C : B
{
    protected C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    protected B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPublicConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    public B(int x)
    {
    }
}",
@"abstract class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    public B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPublicConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    public B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    public B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromInternalConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    internal B(int x)
    {
    }
}",
@"abstract class C : B
{
    internal C(int x) : base(x)
    {
    }
}

abstract class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromInternalConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedInternalConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    protected internal B(int x)
    {
    }
}",
@"abstract class C : B
{
    protected internal C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromProtectedInternalConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    protected internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    protected internal B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPrivateProtectedConstructor()
        {
            await TestInRegularAndScriptAsync(
@"abstract class C : [||]B
{
}

abstract class B
{
    private protected B(int x)
    {
    }
}",
@"abstract class C : B
{
    private protected C(int x) : base(x)
    {
    }
}

abstract class B
{
    private protected B(int x)
    {
    }
}");
        }

        [WorkItem(25238, "https://github.com/dotnet/roslyn/issues/25238")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsGenerateDefaultConstructors)]
        public async Task TestGenerateConstructorFromPrivateProtectedConstructor2()
        {
            await TestInRegularAndScriptAsync(
@"class C : [||]B
{
}

abstract class B
{
    private protected internal B(int x)
    {
    }
}",
@"class C : B
{
    public C(int x) : base(x)
    {
    }
}

abstract class B
{
    private protected internal B(int x)
    {
    }
}");
        }
    }
}
