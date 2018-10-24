// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.PushMemberUp
{
    public class PushMemberUpToIntefaceTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new PullMemberUpCodeRefactoringProvider();

        #region interface
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushMethodUpToInterface()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public void TestM[||]ethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}";
            var expected = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        void TestMethod();
    }

    public class TestClass : IInterface
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushMultipleEventsToInterface()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1, Eve[||]nt2, Event3;
    }
}";

            var expected = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        event EventHandler Event2;
    }

    public class TestClass : IInterface
    {
        public event EventHandler Event1, Event2, Event3;
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushPropertyToInterface()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        public int TestPr[||]operty { get; private set; }
    }
}";

            var expected = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int TestProperty { get; }
    }

    public class TestClass : IInterface
    {
        public int TestProperty { get; private set; }
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }

        
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushIndexerToInterface()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
    }

    public class TestClass : IInterface
    {
        private int j;
        public int th[||]is[int i]
        {
           get => j;
           set => j = value;
        }
    }
}";

            var expected = @"
using System;
namespace PushUpTest
{
    interface IInterface
    {
        int this[int i] { get; set; }
    }

    public class TestClass : IInterface
    {
        private int j;
        public int this[int i]
        {
           get => j;
           set => j = value;
        }
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }
        #endregion interface

        #region class
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushMethodToClass()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public void TestMeth[||]od()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }
}";

            var expected = @"
namespace PushUpTest
{
    public class Base
    {
        public void TestMethod()
        {
            System.Console.WriteLine(""Hello World"");
        }
    }

    public class TestClass : Base
    {
    }
}";

            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushFieldsToClass()
        {
            var testText = @"
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int you, a[||]nd, someone = 10086;
    }
}";

            var expected = @"
namespace PushUpTest
{
    public class Base
    {
        public int and;
    }

    public class TestClass : Base
    {
        public int you, someone = 10086;
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushPropertyToClass()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    public class Base
    {
    }

    public class TestClass : Base
    {
        public int TestPr[||]operty { get; private set; }
    }
}";

            var expected = @"
using System;
namespace PushUpTest
{
    public class Base
    {
        public int TestProperty { get; private set; }
    }

    public class TestClass : Base
    {
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsPushMemberUp)]
        public async Task PushIndexerToClass()
        {
            var testText = @"
using System;
namespace PushUpTest
{
    public class Base 
    {
    }

    public class TestClass : Base
    {
        private int j;
        public int th[||]is[int i]
        {
            get => j;
            set => j = value;
        }
    }
}";

            var expected = @"
using System;
namespace PushUpTest
{
    public class Base 
    {
        public int this[int i]
        {
            get => j;
            set => j = value;
        }
    }

    public class TestClass : Base
    {
        private int j;
    }
}";
            await TestInRegularAndScriptAsync(testText, expected);
        }
        #endregion class
    }
}
