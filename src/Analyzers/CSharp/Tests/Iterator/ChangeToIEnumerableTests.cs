// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics.Iterator;

[Trait(Traits.Feature, Traits.Features.CodeActionsChangeToIEnumerable)]
public class ChangeToIEnumerableTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest_NoEditor
{
    public ChangeToIEnumerableTests(ITestOutputHelper logger)
       : base(logger)
    {
    }

    internal override (DiagnosticAnalyzer?, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
        => (null, new CSharpChangeToIEnumerableCodeFixProvider());

    [Fact]
    public async Task TestChangeToIEnumerableObjectMethod()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static object [|M|]()
                {
                    yield return 0;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<object> M()
                {
                    yield return 0;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestChangeToIEnumerableTupleMethod()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static Tuple<int> [|M|]()
                {
                    yield return 0;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<Tuple<int>> M()
                {
                    yield return 0;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestChangeToIEnumerableListMethod()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IList<int> [|M|]()
                {
                    yield return 0;
                }
            }
            """;

        var expected =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<int> M()
                {
                    yield return 0;
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestChangeToIEnumerableWithListReturningMethodWithNullableArgument()
    {
        var initial =
            """
            #nullable enable

            using System;
            using System.Collections.Generic;

            class Program
            {
                static IList<string?> [|M|]()
                {
                    yield return "";
                }
            }
            """;

        var expected =
            """
            #nullable enable

            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<string?> M()
                {
                    yield return "";
                }
            }
            """;
        await TestInRegularAndScriptAsync(initial, expected);
    }

    [Fact]
    public async Task TestChangeToIEnumerableGenericIEnumerableMethod()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerable<int> [|M|]()
                {
                    yield return 0;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestChangeToIEnumerableGenericIEnumeratorMethod()
    {
        var initial =
            """
            using System;
            using System.Collections.Generic;

            class Program
            {
                static IEnumerator<int> [|M|]()
                {
                    yield return 0;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestChangeToIEnumerableIEnumeratorMethod()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerator [|M|]()
                {
                    yield return 0;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestChangeToIEnumerableIEnumerableMethod()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static IEnumerable [|M|]()
                {
                    yield return 0;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact]
    public async Task TestChangeToIEnumerableVoidMethod()
    {
        var initial =
            """
            using System;
            using System.Collections;

            class Program
            {
                static void [|M|]()
                {
                    yield return 0;
                }
            }
            """;
        await TestMissingInRegularAndScriptAsync(initial);
    }

    [Fact, WorkItem(7087, @"https://github.com/dotnet/roslyn/issues/7087")]
    public async Task TestChangeToIEnumerableProperty()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;

            namespace Asdf
            {
                public class Test
                {
                    public ISet<IMyInterface> Test
                    {
                        [|get|]
                        {
                            yield return TestFactory.Create<float>("yada yada yada");
                        } ;
                    }
                }

                public static class TestFactory
                {
                    public static IMyInterface Create<T>(string someIdentifier)
                    {
                        return new MyClass<T>();
                    }
                }

                public interface IMyInterface : IEquatable<IMyInterface>
                {
                }

                public class MyClass<T> : IMyInterface
                {
                    public bool Equals(IMyInterface other)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;

            namespace Asdf
            {
                public class Test
                {
                    public IEnumerable<IMyInterface> Test
                    {
                        get
                        {
                            yield return TestFactory.Create<float>("yada yada yada");
                        } ;
                    }
                }

                public static class TestFactory
                {
                    public static IMyInterface Create<T>(string someIdentifier)
                    {
                        return new MyClass<T>();
                    }
                }

                public interface IMyInterface : IEquatable<IMyInterface>
                {
                }

                public class MyClass<T> : IMyInterface
                {
                    public bool Equals(IMyInterface other)
                    {
                        throw new NotImplementedException();
                    }
                }
            }
            """);
    }

    [Fact, WorkItem(7087, @"https://github.com/dotnet/roslyn/issues/7087")]
    public async Task TestChangeToIEnumerableOperator()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace Asdf
            {
                public class T
                {
                    public static ISet<int> operator [|=|] (T left, T right)
                    {
                        yield return 0;
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace Asdf
            {
                public class T
                {
                    public static IEnumerable<int> operator = (T left, T right)
                    {
                        yield return 0;
                    }
                }
            }
            """);
    }

    [Fact, WorkItem(7087, @"https://github.com/dotnet/roslyn/issues/7087")]
    public async Task TestChangeToIEnumerableIndexer()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class T
            {
                public T[] this[int i]
                {
                    [|get|]
                    {
                        yield return new T();
                    }
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;

            class T
            {
                public IEnumerable<T> this[int i]
                {
                    get
                    {
                        yield return new T();
                    }
                }
            }
            """);
    }
}
