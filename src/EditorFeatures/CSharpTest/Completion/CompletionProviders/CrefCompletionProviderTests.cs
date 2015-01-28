// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.CompletionProviders
{
    public class CrefCompletionProviderTests : AbstractCSharpCompletionProviderTests
    {
        internal override ICompletionProvider CreateCompletionProvider()
        {
            return new CrefCompletionProvider();
        }

        protected override void VerifyWorker(string code, int position, string expectedItemOrNull, string expectedDescriptionOrNull, SourceCodeKind sourceCodeKind, bool usePreviousCharAsTrigger, bool checkForAbsence, bool experimental, int? glyph)
        {
            VerifyAtPosition(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            VerifyAtEndOfFile(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);

            // Items cannot be partially written if we're checking for their absence,
            // or if we're verifying that the list will show up (without specifying an actual item)
            if (!checkForAbsence && expectedItemOrNull != null)
            {
                VerifyAtPosition_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
                VerifyAtEndOfFile_ItemPartiallyWritten(code, position, usePreviousCharAsTrigger, expectedItemOrNull, expectedDescriptionOrNull, sourceCodeKind, checkForAbsence, experimental, glyph);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NameCref()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref=""$$""/> 
    class Program
    {
    }
}";
            VerifyItemExists(text, "AccessViolationException");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void QualifiedCref()
        {
            var text = @"using System;
namespace Foo
{

    class Program
    {
        /// <see cref=""Program.$$""/> 
        void foo() { }
    }
}";
            VerifyItemExists(text, "foo");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrefArgumentList()
        {
            var text = @"using System;
namespace Foo
{

    class Program
    {
        /// <see cref=""Program.foo($$""/> 
        void foo(int i) { }
    }
}";
            VerifyItemIsAbsent(text, "foo(int)");
            VerifyItemExists(text, "int");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CrefTypeParameterInArgumentList()
        {
            var text = @"using System;
namespace Foo
{

    class Program<T>
    {
        /// <see cref=""Program{Q}.foo($$""/> 
        void foo(T i) { }
    }
}";
            VerifyItemExists(text, "Q");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion), WorkItem(530887)]
        public void PrivateMember()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref=""C.$$""/> 
    class Program<T>
    {
    }

    class C
    {
        private int Private;
        public int Public;
    }
}";
            VerifyItemExists(text, "Private");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void AfterSingleQuote()
        {
            var text = @"using System;
namespace Foo
{
    /// <see cref='$$'/> 
    class Program
    {
    }
}";
            VerifyItemExists(text, "Exception");
        }

        [WorkItem(531315)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void EscapePredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";
            VerifyItemExists(text, "@void");
        }

        [WorkItem(531345)]
        [WorkItem(598159)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowParameterNames()
        {
            var text = @"/// <see cref=""C.$$""/>
class C
{
    void M(int x) { }
    void M(ref long x) { }
    void M<T>(T x) { }
}

";
            VerifyItemExists(text, "M(int)");
            VerifyItemExists(text, "M(ref long)");
            VerifyItemExists(text, "M{T}(T)");
        }

        [WorkItem(531345)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowTypeParameterNames()
        {
            var text = @"/// <see cref=""C$$""/>
class C<TFoo>
{
    void M(int x) { }
    void M(long x) { }
    void M(string x) { }
}

";
            VerifyItemExists(text, "C{TFoo}");
        }

        [WorkItem(531156)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void ShowConstructors()
        {
            var text = @"using System;

/// <see cref=""C.$$""/>
class C<T>
{
    public C(int x) { }

    public C() { }

    public C(T x) { }
}

";
            VerifyItemExists(text, "C");
            VerifyItemExists(text, "C(T)");
            VerifyItemExists(text, "C(int)");
        }

        [WorkItem(598679)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NoParamsModifier()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
        {
            void M(int x) { }
            void M(params long[] x) { }
        }


";
            VerifyItemExists(text, "M(long[])");
        }

        [WorkItem(607773)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void UnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.$$""/>
class C { }
";
            VerifyItemExists(text, "Enumerator");
        }

        [WorkItem(607773)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitUnqualifiedTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List{T}.$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List{T}.Enumerator""/>
class C { }
";
            VerifyProviderCommit(text, "Enumerator", expected, ' ', "Enum");
        }

        [WorkItem(642285)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestOperators()
        {
            var text = @"
class Test
{
    /// <see cref=""$$""/>
    public static Test operator !(Test t)
    {
        return new Test();
    }
    public static int operator +(Test t1, Test t2) // Invoke FAR here on operator
    {
        return 1;
    }
    public static bool operator true(Test t)
    {
        return true;
    }
    public static bool operator false(Test t)
    {
        return false;
    }
}
";
            VerifyItemExists(text, "operator !(Test)");
            VerifyItemExists(text, "operator +(Test, Test)");
            VerifyItemExists(text, "operator true(Test)");
            VerifyItemExists(text, "operator false(Test)");
        }

        [WorkItem(641096)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void SuggestIndexers()
        {
            var text = @"
/// <see cref=""thi$$""/>
class Program
{
    int[] arr;

    public int this[int i]
    {
        get { return arr[i]; }
    }
}
";
            VerifyItemExists(text, "this[int]");
        }

        [WorkItem(531315)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void CommitEscapedPredefinedTypeName()
        {
            var text = @"using System;
/// <see cref=""@vo$$""/>
class @void { }
";

            var expected = @"using System;
/// <see cref=""@void""/>
class @void { }
";
            VerifyProviderCommit(text, "@void", expected, ' ', "@vo");
        }

        [WorkItem(598159)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void RefOutModifiers()
        {
            var text = @"/// <summary>
/// <see cref=""C.$$""/>
/// </summary>
class C
{
    void M(ref int x) { }
    void M(out long x) { }
}

";
            VerifyItemExists(text, "M(ref int)");
            VerifyItemExists(text, "M(out long)");
        }

        [WorkItem(673587)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void NestedNamespaces()
        {
            var text = @"namespace N
{
    class C
    {
        void sub() { }
    }
    namespace N
    {
        class C
        { }
    }
}
class Program
{
    /// <summary>
    /// <see cref=""N.$$""/> // type N. here
    /// </summary>
    static void Main(string[] args)
    {

    }
}";
            VerifyItemExists(text, "N");
            VerifyItemExists(text, "C");
        }

        [WorkItem(730338)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PermitTypingTypeParameters()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""List$$""/>
class C { }
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""List""/>
class C { }
";
            VerifyProviderCommit(text, "List{T}", expected, '{', "List");
        }

        [WorkItem(730338)]
        [Fact, Trait(Traits.Feature, Traits.Features.Completion)]
        public void PermitTypingParameterTypes()
        {
            var text = @"
using System.Collections.Generic;
/// <see cref=""foo$$""/>
class C 
{ 
    public void foo(int x) { }
}
";

            var expected = @"
using System.Collections.Generic;
/// <see cref=""foo""/>
class C 
{ 
    public void foo(int x) { }
}
";
            VerifyProviderCommit(text, "foo(int)", expected, '(', "foo");
        }
    }
}
