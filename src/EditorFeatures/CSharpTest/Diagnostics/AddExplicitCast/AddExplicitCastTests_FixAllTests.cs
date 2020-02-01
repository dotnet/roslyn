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
        public async Task CS0266TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return b;
	}

	Derived ReturnDerived2(Base b)
	{
		return ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = {|FixAllInDocument:b|};
		d = new Base() { };

		Derived d2 = ReturnBase();

        Test t = new Test();
		t.D = b;
		t.d = b;
		d = t.B;

		<![CDATA[ Func<Base, Derived> foo = d => d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
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
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return (Derived)b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return (Derived)b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return (Derived)b;
	}

	Derived ReturnDerived2(Base b)
	{
		return (Derived)ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return (Derived)func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = (Derived)b;
		d = (Derived)new Base() { };

		Derived d2 = (Derived)ReturnBase();

        Test t = new Test();
		t.D = (Derived)b;
		t.d = b;
		d = (Derived)t.B;

		<![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = (Derived)foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }


        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task CS0266TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return b;
	}

	Derived ReturnDerived2(Base b)
	{
		return ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = {|FixAllInProject:b|};
		d = new Base() { };

		Derived d2 = ReturnBase();

        Test t = new Test();
		t.D = b;
		t.d = b;
		d = t.B;

		<![CDATA[ Func<Base, Derived> foo = d => d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
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
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return (Derived)b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return (Derived)b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return (Derived)b;
	}

	Derived ReturnDerived2(Base b)
	{
		return (Derived)ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return (Derived)func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = (Derived)b;
		d = (Derived)new Base() { };

		Derived d2 = (Derived)ReturnBase();

        Test t = new Test();
		t.D = (Derived)b;
		t.d = b;
		d = (Derived)t.B;

		<![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = (Derived)foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = (Derived2)b1;
		derived2 = (Derived2)b3;
		Base2 base2 = (Base2)b1;
		derived2 = (Derived2)d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task CS0266TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return b;
	}

	Derived ReturnDerived2(Base b)
	{
		return ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = {|FixAllInSolution:b|};
		d = new Base() { };

		Derived d2 = ReturnBase();

        Test t = new Test();
		t.D = b;
		t.d = b;
		d = t.B;

		<![CDATA[ Func<Base, Derived> foo = d => d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = b1;
		derived2 = b3;
		Base2 base2 = b1;
		derived2 = d1;
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
using System.Collections.Generic;
using System.Threading.Tasks;

public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}
	Derived ReturnDerived(Base b)
	{
		return (Derived)b;
	}

	<![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		yield return (Derived)b;
	}

    <![CDATA[ IEnumerable<Derived> ReturnDerived() ]]>
	{
		Base b;
		return b;
	}

	<![CDATA[ async Task<Derived> M() ]]>
	{
		Base b;
		return (Derived)b;
	}

	Derived ReturnDerived2(Base b)
	{
		return (Derived)ReturnBase();
	}

	Derived Foo()
	{
		<![CDATA[ Func<Base, Base> func = d => d; ]]>
		Base b;
		return (Derived)func(b);
	}
	public Program1()
	{
		Base b;
		Derived d = (Derived)b;
		d = (Derived)new Base() { };

		Derived d2 = (Derived)ReturnBase();

        Test t = new Test();
		t.D = (Derived)b;
		t.d = b;
		d = (Derived)t.B;

		<![CDATA[ Func<Base, Derived> foo = d => (Derived)d; ]]>

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		d2 = (Derived)foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = (Derived2)b1;
		derived2 = (Derived2)b3;
		Base2 base2 = (Base2)b1;
		derived2 = (Derived2)d1;
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
public class Program3
{
    interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Derived2 derived2 = (Derived2)b1;
		derived2 = (Derived2)b3;
		Base2 base2 = (Base2)b1;
		derived2 = (Derived2)d1;
	}
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task CS1503TestFixAllInDocument()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base(b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this(b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived({|FixAllInDocument:b|});
		PassDerived(ReturnBase());
		PassDerived(1, b);
		PassDerived(1, ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add(b);

		Test t = new Test();
		t.testing(b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2(b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
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
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base((Derived)b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this((Derived)b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived((Derived)b);
		PassDerived((Derived)ReturnBase());
		PassDerived(1, (Derived)b);
		PassDerived(1, (Derived)ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add((Derived)b);

		Test t = new Test();
		t.testing((Derived)b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2((Derived)b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task CS1503TestFixAllInProject()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base(b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this(b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived({|FixAllInProject:b|});
		PassDerived(ReturnBase());
		PassDerived(1, b);
		PassDerived(1, ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add(b);

		Test t = new Test();
		t.testing(b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2(b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
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
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base((Derived)b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this((Derived)b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived((Derived)b);
		PassDerived((Derived)ReturnBase());
		PassDerived(1, (Derived)b);
		PassDerived(1, (Derived)ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add((Derived)b);

		Test t = new Test();
		t.testing((Derived)b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2((Derived)b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1((Derived2)b1);
		Foo1((Derived2)d1);
		Foo2((Base2)b1);
		Foo3((Derived2)b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
</Workspace>";

            await TestInRegularAndScriptAsync(input, expected);
        }

        [Fact]
        [Trait(Traits.Feature, Traits.Features.CodeActionsAddExplicitCast)]
        [Trait(Traits.Feature, Traits.Features.CodeActionsFixAllOccurrences)]
        public async Task CS1503TestFixAllInSolution()
        {
            var input = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base(b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this(b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived({|FixAllInSolution:b|});
		PassDerived(ReturnBase());
		PassDerived(1, b);
		PassDerived(1, ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add(b);

		Test t = new Test();
		t.testing(b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2(b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1(b1);
		Foo1(d1);
		Foo2(b1);
		Foo3(b1);
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
using System.Collections.Generic;
using System.Threading.Tasks;
public class Program1
{
	class Base { }
	class Derived : Base { }

	class Test
	{
		private Derived d;
		private Base b;
		public Test(Derived derived)
		{
			d = derived;
			B = derived;
		}

		public Derived D { get => d; set => d = value; }
		public Base B { get => b; set => b = value; }

		public void testing(Derived d) { }
		private void testing(Base b) { }
	}

	class Test2 : Test
	{
		public Test2(Base b) : base((Derived)b) { }
	}

	class Test3
	{
		public Test3(Derived b) { }
		public Test3(int i, Base b) : this((Derived)b) { }

		public void testing(int i, Derived d) { }
		private void testing(int i, Base d) { }
	}

	Base ReturnBase()
	{
		Base b = new Base();
		return b;
	}

	void PassDerived(Derived d) { }
	void PassDerived(int i, Derived d) { }

	public Program1()
	{
		Base b;
		Derived d = b;

		PassDerived((Derived)b);
		PassDerived((Derived)ReturnBase());
		PassDerived(1, (Derived)b);
		PassDerived(1, (Derived)ReturnBase());

		<![CDATA[ List<Derived> list = new List<Derived>(); ]]>
		list.Add((Derived)b);

		Test t = new Test();
		t.testing((Derived)b);

		<![CDATA[ Func<Derived, Base> foo2 = d => d; ]]>
		Derived d2 = foo2((Derived)b);
		d2 = foo2(d);
	}
}
        </Document>
        <Document>
public class Program2
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1((Derived2)b1);
		Foo1((Derived2)d1);
		Foo2((Base2)b1);
		Foo3((Derived2)b1);
	}
}
        </Document>
    </Project>
    <Project Language=""C#"" AssemblyName=""Assembly2"" CommonReferences=""true"">
        <Document>
class Program3
{
	interface Base1 { }
	interface Base2 : Base1 { }
	interface Base3 { }
	class Derived1 : Base2, Base3 { }
	class Derived2 : Derived1 { }

	void Foo1(Derived2 b) { }
	void Foo2(Base2 b) { }

	void Foo3(Derived2 b1) { }
	void Foo3(int i) { }

	private void M2(Base1 b1, Base2 b2, Base3 b3, Derived1 d1, Derived2 d2)
	{
		Foo1((Derived2)b1);
		Foo1((Derived2)d1);
		Foo2((Base2)b1);
		Foo3((Derived2)b1);
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
