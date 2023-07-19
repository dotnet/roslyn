// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class GetSemanticInfoTests : SemanticModelTestBase
    {
        #region helpers

        internal List<string> GetLookupNames(string testSrc)
        {
            var parseOptions = TestOptions.Regular;
            var compilation = CreateCompilationWithMscorlib45(testSrc, parseOptions: parseOptions);
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);
            var position = testSrc.Contains("/*<bind>*/") ? GetPositionForBinding(tree) : GetPositionForBinding(testSrc);
            return model.LookupNames(position);
        }

        internal List<ISymbol> GetLookupSymbols(string testSrc, NamespaceOrTypeSymbol container = null, string name = null, int? arity = null, bool isScript = false, IEnumerable<string> globalUsings = null)
        {
            var tree = Parse(testSrc, options: isScript ? TestOptions.Script : TestOptions.Regular);
            var compilation = CreateCompilationWithMscorlib45(new[] { tree }, options: TestOptions.ReleaseDll.WithUsings(globalUsings));
            var model = compilation.GetSemanticModel(tree);
            var position = testSrc.Contains("/*<bind>*/") ? GetPositionForBinding(tree) : GetPositionForBinding(testSrc);
            return model.LookupSymbols(position, container.GetPublicSymbol(), name).Where(s => !arity.HasValue || arity == s.GetSymbol().GetMemberArity()).ToList();
        }

        #endregion helpers

        #region tests

        [Fact]
        public void LookupExpressionBodyProp01()
        {
            var text = @"
class C
{
    public int P => /*<bind>*/10/*</bind>*/;
}";
            var actual = GetLookupNames(text).ListToSortedString();

            var expected_lookupNames = new List<string>
            {
                "C",
                "Equals",
                "Finalize",
                "GetHashCode",
                "GetType",
                "MemberwiseClone",
                "Microsoft",
                "P",
                "ReferenceEquals",
                "System",
                "ToString"
            };

            Assert.Equal(expected_lookupNames.ListToSortedString(), actual);
        }

        [Fact]
        public void LookupExpressionBodiedMethod01()
        {
            var text = @"
class C
{
    public int M() => /*<bind>*/10/*</bind>*/;
}";
            var actual = GetLookupNames(text).ListToSortedString();

            var expected_lookupNames = new List<string>
            {
                "C",
                "Equals",
                "Finalize",
                "GetHashCode",
                "GetType",
                "MemberwiseClone",
                "Microsoft",
                "M",
                "ReferenceEquals",
                "System",
                "ToString"
            };

            Assert.Equal(expected_lookupNames.ListToSortedString(), actual);
        }

        [WorkItem(538262, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538262")]
        [Fact]
        public void LookupCompilationUnitSyntax()
        {
            var testSrc = @"
/*<bind>*/
class Test
{
}
/*</bind>*/
";

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupSymbols(testSrc);
        }

        [WorkItem(527476, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527476")]
        [Fact]
        public void LookupConstrAndDestr()
        {
            var testSrc = @"
class Test
{
    Test()
    {
    }

    Test(int i)
    {
    }

    ~Test()
    {
    }

    static /*<bind>*/void/*</bind>*/Main()
    {
    }
}
";
            List<string> expected_lookupNames = new List<string>
            {
                "Equals",
                "Finalize",
                "GetHashCode",
                "GetType",
                "Main",
                "MemberwiseClone",
                "Microsoft",
                "ReferenceEquals",
                "System",
                "Test",
                "ToString"
            };

            List<string> expected_lookupSymbols = new List<string>
            {
                "Microsoft",
                "System",
                "System.Boolean System.Object.Equals(System.Object obj)",
                "System.Boolean System.Object.Equals(System.Object objA, System.Object objB)",
                "System.Boolean System.Object.ReferenceEquals(System.Object objA, System.Object objB)",
                "System.Int32 System.Object.GetHashCode()",
                "System.Object System.Object.MemberwiseClone()",
                "void System.Object.Finalize()",
                "System.String System.Object.ToString()",
                "System.Type System.Object.GetType()",
                "void Test.Finalize()",
                "void Test.Main()",
                "Test"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc);

            Assert.Equal(expected_lookupNames.ListToSortedString(), actual_lookupNames.ListToSortedString());
            Assert.Equal(expected_lookupSymbols.ListToSortedString(), actual_lookupSymbols.ListToSortedString());
        }

        [WorkItem(527477, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527477")]
        [Fact]
        public void LookupNotYetDeclLocalVar()
        {
            var testSrc = @"
class Test
{
    static void Main()
    {
        int j = /*<bind>*/9/*</bind>*/ ;
        int k = 45;
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "j",
                "k"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "j",
                "k"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
        }

        [WorkItem(538301, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538301")]
        [Fact]
        public void LookupByNameIncorrectArity()
        {
            var testSrc = @"
class Test
{
    public static void Main()
    {
        int i = /*<bind>*/10/*</bind>*/;
    }
}
";

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupSymbols(testSrc, name: "i", arity: 1);

            var actual_lookupSymbols = GetLookupSymbols(testSrc, name: "i", arity: 1);

            Assert.Empty(actual_lookupSymbols);
        }

        [WorkItem(538310, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538310")]
        [Fact]
        public void LookupInProtectedNonNestedType()
        {
            var testSrc = @"
protected class MyClass {
    /*<bind>*/public static void Main()/*</bind>*/ {}	
}
";

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupSymbols(testSrc);
        }

        [WorkItem(538311, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538311")]
        [Fact]
        public void LookupClassContainsVolatileEnumField()
        {
            var testSrc = @"
enum E{} 
class Test {
    static volatile E x;
    static /*<bind>*/int/*</bind>*/ Main() { 
        return 1;
    }
}
";

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            GetLookupSymbols(testSrc);
        }

        [WorkItem(538312, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538312")]
        [Fact]
        public void LookupUsingAlias()
        {
            var testSrc = @"
using T2 = System.IO;

namespace T1
{
    class Test
    {
        static /*<bind>*/void/*</bind>*/ Main()
        {
        }
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "T1",
                "T2"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "T1",
                "T2"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
        }

        [WorkItem(538313, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538313")]
        [Fact]
        public void LookupUsingNameSpaceContSameTypeNames()
        {
            var testSrc = @"
namespace T1
{
    using T2;
    public class Test
    {
        static /*<bind>*/int/*</bind>*/ Main()
        {
            return 1;
        }
    }
}

namespace T2
{
    public class Test
    {
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "T1",
                "T2",
                "Test"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "T1",
                "T2",
                "T1.Test",
                //"T2.Test" this is hidden by T1.Test
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[2], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[2], actual_lookupSymbols_as_string);
        }

        [WorkItem(527489, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527489")]
        [Fact]
        public void LookupMustNotBeNonInvocableMember()
        {
            var testSrc = @"
class Test
{
    public void TestMeth(int i, int j)
    {
        int m = /*<bind>*/10/*</bind>*/;
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "TestMeth",
                "i",
                "j",
                "m",
                "System",
                "Microsoft",
                "Test"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "void Test.TestMeth(System.Int32 i, System.Int32 j)",
                "System.Int32 i",
                "System.Int32 j",
                "System.Int32 m",
                "System",
                "Microsoft",
                "Test"
            };

            var comp = CreateCompilation(testSrc);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var position = GetPositionForBinding(tree);
            var binder = ((CSharpSemanticModel)model).GetEnclosingBinder(position);

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var info = LookupSymbolsInfo.GetInstance();
            binder.AddLookupSymbolsInfo(info, LookupOptions.MustBeInvocableIfMember);
            var actual_lookupNames = info.Names;

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = actual_lookupNames.SelectMany(name =>
            {
                var lookupResult = LookupResult.GetInstance();
                HashSet<DiagnosticInfo> useSiteDiagnostics = null;
                binder.LookupSymbolsSimpleName(
                    lookupResult,
                    qualifierOpt: null,
                    plainName: name,
                    arity: 0,
                    basesBeingResolved: null,
                    options: LookupOptions.MustBeInvocableIfMember,
                    diagnose: false,
                    useSiteDiagnostics: ref useSiteDiagnostics);
                Assert.Null(useSiteDiagnostics);
                Assert.True(lookupResult.IsMultiViable || lookupResult.Kind == LookupResultKind.NotReferencable);
                var result = lookupResult.Symbols.ToArray();
                lookupResult.Free();
                return result;
            });
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[2], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[3], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[4], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[5], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[2], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[3], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[4], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[5], actual_lookupSymbols_as_string);

            info.Free();
        }

        [WorkItem(538365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538365")]
        [Fact]
        public void LookupWithNameZeroArity()
        {
            var testSrc = @"
class Test
{
    private void F<T>(T i)
    {
    }

    private void F<T, U>(T i, U j)
    {
    }

    private void F(int i)
    {
    }

    private void F(int i, int j)
    {
    }

    public static /*<bind>*/void/*</bind>*/ Main()
    {
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "F"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "void Test.F(System.Int32 i)",
                "void Test.F(System.Int32 i, System.Int32 j)"
            };

            List<string> not_expected_in_lookupSymbols = new List<string>
            {
                "void Test.F<T>(T i)",
                "void Test.F<T, U>(T i, U j)"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc, name: "F", arity: 0);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);

            Assert.Equal(2, actual_lookupSymbols.Count);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.DoesNotContain(not_expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.DoesNotContain(not_expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
        }

        [WorkItem(538365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538365")]
        [Fact]
        public void LookupWithNameZeroArityAndLookupOptionsAllMethods()
        {
            var testSrc = @"
class Test
{
    public void F<T>(T i)
    {
    }

    public void F<T, U>(T i, U j)
    {
    }

    public void F(int i)
    {
    }

    public void F(int i, int j)
    {
    }

    public void Main()
    {
        return;
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "F"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "void Test.F(System.Int32 i)",
                "void Test.F(System.Int32 i, System.Int32 j)",
                "void Test.F<T>(T i)",
                "void Test.F<T, U>(T i, U j)"
            };

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var comp = CreateCompilation(testSrc);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var position = testSrc.IndexOf("return", StringComparison.Ordinal);
            var binder = ((CSharpSemanticModel)model).GetEnclosingBinder(position);
            var lookupResult = LookupResult.GetInstance();
            HashSet<DiagnosticInfo> useSiteDiagnostics = null;
            binder.LookupSymbolsSimpleName(lookupResult, qualifierOpt: null, plainName: "F", arity: 0, basesBeingResolved: null, options: LookupOptions.AllMethodsOnArityZero, diagnose: false, useSiteDiagnostics: ref useSiteDiagnostics);
            Assert.Null(useSiteDiagnostics);
            Assert.True(lookupResult.IsMultiViable);
            var actual_lookupSymbols_as_string = lookupResult.Symbols.Select(e => e.ToTestDisplayString()).ToArray();
            lookupResult.Free();

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = model.LookupNames(position);

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);

            Assert.Equal(4, actual_lookupSymbols_as_string.Length);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[2], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[3], actual_lookupSymbols_as_string);
        }

        [WorkItem(539160, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539160")]
        [Fact]
        public void LookupExcludeInAppropriateNS()
        {
            var testSrc = @"
class Test
{
   public static /*<bind>*/void/*</bind>*/ Main()
   {
   }
}
";
            var srcTrees = new SyntaxTree[] { Parse(testSrc) };
            var refs = new MetadataReference[] { SystemDataRef };
            CSharpCompilation compilation = CSharpCompilation.Create("Test.dll", srcTrees, refs);

            var tree = srcTrees[0];
            var model = compilation.GetSemanticModel(tree);

            List<string> not_expected_in_lookup = new List<string>
            {
                "<CrtImplementationDetails>",
                "<CppImplementationDetails>"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = model.LookupNames(GetPositionForBinding(tree), null).ToList();
            var actual_lookupNames_ignoreAcc = model.LookupNames(GetPositionForBinding(tree), null).ToList();

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = model.LookupSymbols(GetPositionForBinding(tree));
            var actual_lookupSymbols_ignoreAcc = model.LookupSymbols(GetPositionForBinding(tree));
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());
            var actual_lookupSymbols_ignoreAcc_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.DoesNotContain(not_expected_in_lookup[0], actual_lookupNames);
            Assert.DoesNotContain(not_expected_in_lookup[1], actual_lookupNames);
            Assert.DoesNotContain(not_expected_in_lookup[0], actual_lookupNames_ignoreAcc);
            Assert.DoesNotContain(not_expected_in_lookup[1], actual_lookupNames_ignoreAcc);

            Assert.DoesNotContain(not_expected_in_lookup[0], actual_lookupSymbols_as_string);
            Assert.DoesNotContain(not_expected_in_lookup[1], actual_lookupSymbols_as_string);
            Assert.DoesNotContain(not_expected_in_lookup[0], actual_lookupSymbols_ignoreAcc_as_string);
            Assert.DoesNotContain(not_expected_in_lookup[1], actual_lookupSymbols_ignoreAcc_as_string);
        }

        /// <summary>
        /// Verify that there's a way to look up only the members of the base type that are visible
        /// from the current type.
        /// </summary>
        [Fact]
        [WorkItem(539814, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539814")]
        public void LookupProtectedInBase()
        {
            var testSrc = @"
class A
{
    private void Hidden() { }
    protected void Goo() { }
}
 
class B : A
{
    void Bar()
    {
        /*<bind>*/base/*</bind>*/.Goo();
    }
}
";
            var srcTrees = new SyntaxTree[] { Parse(testSrc) };
            var refs = new MetadataReference[] { SystemDataRef };
            CSharpCompilation compilation = CSharpCompilation.Create("Test.dll", srcTrees, refs);

            var tree = srcTrees[0];
            var model = compilation.GetSemanticModel(tree);

            var baseExprNode = GetSyntaxNodeForBinding(GetSyntaxNodeList(tree));
            Assert.Equal("base", baseExprNode.ToString());

            var baseExprLocation = baseExprNode.SpanStart;
            Assert.NotEqual(0, baseExprLocation);

            var baseExprInfo = model.GetTypeInfo((ExpressionSyntax)baseExprNode);
            Assert.NotEqual(default, baseExprInfo);

            var baseExprType = (INamedTypeSymbol)baseExprInfo.Type;
            Assert.NotNull(baseExprType);
            Assert.Equal("A", baseExprType.Name);

            var symbols = model.LookupBaseMembers(baseExprLocation);
            Assert.Equal("void A.Goo()", symbols.Single().ToTestDisplayString());

            var names = model.LookupNames(baseExprLocation, useBaseReferenceAccessibility: true);
            Assert.Equal("Goo", names.Single());
        }

        [WorkItem(528263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528263")]
        [Fact]
        public void LookupStartOfScopeMethodBody()
        {
            var testSrc = @"public class start
{
       static public void Main()
/*pos*/{
          int num=10;
       } 
";
            List<string> expected_in_lookupNames = new List<string>
            {
                "Main",
                "start",
                "num"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "void start.Main()",
                "start",
                "System.Int32 num"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Equal('{', testSrc[GetPositionForBinding(testSrc)]);

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[2], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[2], actual_lookupSymbols_as_string);
        }

        [WorkItem(528263, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528263")]
        [Fact]
        public void LookupEndOfScopeMethodBody()
        {
            var testSrc = @"public class start
{
       static public void Main()
       {
          int num=10;
/*pos*/} 
";
            List<string> expected_in_lookupNames = new List<string>
            {
                "Main",
                "start"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "void start.Main()",
                "start"
            };

            List<string> not_expected_in_lookupNames = new List<string>
            {
                "num"
            };

            List<string> not_expected_in_lookupSymbols = new List<string>
            {
                "System.Int32 num"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Equal('}', testSrc[GetPositionForBinding(testSrc)]);

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.DoesNotContain(not_expected_in_lookupNames[0], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.DoesNotContain(not_expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(540888, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540888")]
        [Fact]
        public void LookupLambdaParamInConstructorInitializer()
        {
            var testSrc = @"
using System;

class MyClass
{
    public MyClass(Func<int, int> x)
    {
    }

    public MyClass(int j, int k)
        : this(lambdaParam => /*pos*/lambdaParam)
    {
    }
}
";
            List<string> expected_in_lookupNames = new List<string>
            {
                "j",
                "k",
                "lambdaParam"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "System.Int32 j",
                "System.Int32 k",
                "System.Int32 lambdaParam"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[2], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[2], actual_lookupSymbols_as_string);
        }

        [WorkItem(540893, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540893")]
        [Fact]
        public void TestForLocalVarDeclLookupAtForKeywordInForStmt()
        {
            var testSrc = @"
class MyClass
{
    static void Main()
    {
        /*pos*/for (int forVar = 10; forVar < 10; forVar++)
        {
        }
    }
}
";
            List<string> not_expected_in_lookupNames = new List<string>
            {
                "forVar"
            };

            List<string> not_expected_in_lookupSymbols = new List<string>
            {
                "System.Int32 forVar",
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.DoesNotContain(not_expected_in_lookupNames[0], actual_lookupNames);

            Assert.DoesNotContain(not_expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(540894, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540894")]
        [Fact]
        public void TestForeachIterVarLookupAtForeachKeyword()
        {
            var testSrc = @"
class MyClass
{
    static void Main()
    {
        System.Collections.Generic.List<int> listOfNumbers = new System.Collections.Generic.List<int>();

        /*pos*/foreach (int number in listOfNumbers)
        {
        }
    }
}
";
            List<string> not_expected_in_lookupNames = new List<string>
            {
                "number"
            };

            List<string> not_expected_in_lookupSymbols = new List<string>
            {
                "System.Int32 number",
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.DoesNotContain(not_expected_in_lookupNames[0], actual_lookupNames);

            Assert.DoesNotContain(not_expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(540912, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540912")]
        [Fact]
        public void TestLookupInConstrInitIncompleteConstrDecl()
        {
            var testSrc = @"
class MyClass
{
    public MyClass(int x)
    {
    }

    public MyClass(int j, int k) :this(/*pos*/k)
";
            List<string> expected_in_lookupNames = new List<string>
            {
                "j",
                "k"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "System.Int32 j",
                "System.Int32 k",
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupNames[1], actual_lookupNames);

            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
            Assert.Contains(expected_in_lookupSymbols[1], actual_lookupSymbols_as_string);
        }

        [WorkItem(541060, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541060")]
        [Fact]
        public void TestLookupInsideIncompleteNestedLambdaBody()
        {
            var testSrc = @"
class C
{
    C()
    {
        D(() =>
        {
            D(() =>
            {
            }/*pos*/
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "C"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "C"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.NotEmpty(actual_lookupNames);
            Assert.NotEmpty(actual_lookupSymbols);

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(541611, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541611")]
        [Fact]
        public void LookupLambdaInsideAttributeUsage()
        {
            var testSrc = @"
using System;

class Program
{
    [ObsoleteAttribute(x=>x/*pos*/
    static void Main(string[] args)
    {       
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "x"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "? x"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [Fact]
        public void LookupInsideLocalFunctionAttribute()
        {
            var testSrc = @"
using System;

class Program
{
    const int w = 0451;

    void M()
    {
        int x = 42;
        const int y = 123;
        [ObsoleteAttribute(/*pos*/
        static void local1(int z)
        {
        }
    }
}
";

            var lookupNames = GetLookupNames(testSrc);
            var lookupSymbols = GetLookupSymbols(testSrc).Select(e => e.ToTestDisplayString()).ToList();

            Assert.Contains("w", lookupNames);
            Assert.Contains("y", lookupNames);
            Assert.Contains("System.Int32 Program.w", lookupSymbols);
            Assert.Contains("System.Int32 y", lookupSymbols);
        }

        [Fact]
        public void LookupInsideLambdaAttribute()
        {
            var testSrc = @"
using System;

class Program
{
    const int w = 0451;

    void M()
    {
        int x = 42;
        const int y = 123;
        Action<int> a =
            [ObsoleteAttribute(/*pos*/
            (int z) => { };
    }
}
";

            var lookupNames = GetLookupNames(testSrc);
            var lookupSymbols = GetLookupSymbols(testSrc).Select(e => e.ToTestDisplayString()).ToList();

            Assert.Contains("w", lookupNames);
            Assert.Contains("y", lookupNames);
            Assert.Contains("System.Int32 Program.w", lookupSymbols);
            Assert.Contains("System.Int32 y", lookupSymbols);
        }

        [Fact]
        public void LookupInsideIncompleteStatementAttribute()
        {
            var testSrc = @"
using System;

class Program
{
    const int w = 0451;

    void M()
    {
        int x = 42;
        const int y = 123;
        [ObsoleteAttribute(/*pos*/
        int
    }
}
";

            var lookupNames = GetLookupNames(testSrc);
            var lookupSymbols = GetLookupSymbols(testSrc).Select(e => e.ToTestDisplayString()).ToList();

            Assert.Contains("w", lookupNames);
            Assert.Contains("y", lookupNames);
            Assert.Contains("System.Int32 Program.w", lookupSymbols);
            Assert.Contains("System.Int32 y", lookupSymbols);
        }

        [WorkItem(541909, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541909")]
        [Fact]
        public void LookupFromRangeVariableAfterFromClause()
        {
            var testSrc = @"
class Program
{
    static void Main(string[] args)
    {
        var q = from i in new int[] { 4, 5 } where /*pos*/

    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "i"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "? i"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(541921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541921")]
        [Fact]
        public void LookupFromRangeVariableInsideNestedFromClause()
        {
            var testSrc = @"
class Program
{
    static void Main(string[] args)
    {
        string[] strings = { };

        var query = from s in strings 
                    from s1 in /*pos*/
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "s"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "? s"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(541919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541919")]
        [Fact]
        public void LookupLambdaVariableInQueryExpr()
        {
            var testSrc = @"
class Program
{
    static void Main(string[] args)
    {
        Func<int, IEnumerable<int>> f1 = (x) => from n in /*pos*/
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "x"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "x"
            };

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.Name);

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [WorkItem(541910, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541910")]
        [Fact]
        public void LookupInsideQueryExprOutsideTypeDecl()
        {
            var testSrc = @"var q = from i in/*pos*/ f";

            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.NotEmpty(actual_lookupNames);
            Assert.NotEmpty(actual_lookupSymbols_as_string);
        }

        [WorkItem(542203, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542203")]
        [Fact]
        public void LookupInsideQueryExprInMalformedFromClause()
        {
            var testSrc = @"
using System;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        int[] numbers = new int[] { 4, 5 };

        var q1 = from I<x/*pos*/ in numbers.Where(x1 => x1 > 2) select x;
    }
}
";
            // Get the list of LookupNames at the location at the end of the /*pos*/ tag
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location at the end of the /*pos*/ tag
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToTestDisplayString());

            Assert.NotEmpty(actual_lookupNames);
            Assert.NotEmpty(actual_lookupSymbols_as_string);
        }

        [WorkItem(543295, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543295")]
        [Fact]
        public void MultipleOverlappingInterfaceConstraints()
        {
            var testSrc =
@"public interface IEntity
{
    object Key { get; }
}

public interface INumberedProjectChild
 : IEntity
{ }

public interface IAggregateRoot : IEntity
{
}

public interface ISpecification<TCandidate>
{
    void IsSatisfiedBy(TCandidate candidate);
}

public abstract class Specification<TCandidate> : ISpecification<TCandidate>
{
    public abstract void IsSatisfiedBy(TCandidate candidate);
}

public class NumberSpecification<TCandidate>
    : Specification<TCandidate> where TCandidate : IAggregateRoot,
    INumberedProjectChild
{
    public override void IsSatisfiedBy(TCandidate candidate)
    {
        var key = candidate.Key;
    }
}";
            CreateCompilation(testSrc).VerifyDiagnostics();
        }

        [WorkItem(529406, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529406")]
        [Fact]
        public void FixedPointerInitializer()
        {
            var testSrc = @"
class Program
{
    static int num = 0;
    unsafe static void Main(string[] args)
    {
        fixed(int* p1 = /*pos*/&num, p2 = &num)
        {
        }
    }
}
";

            List<string> expected_in_lookupNames = new List<string>
            {
                "p2"
            };

            List<string> expected_in_lookupSymbols = new List<string>
            {
                "p2"
            };

            // Get the list of LookupNames at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupNames = GetLookupNames(testSrc);

            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode enclosed within the <bind> </bind> tags
            var actual_lookupSymbols = GetLookupSymbols(testSrc);
            var actual_lookupSymbols_as_string = actual_lookupSymbols.Select(e => e.ToString()).ToList();

            Assert.Contains(expected_in_lookupNames[0], actual_lookupNames);
            Assert.Contains(expected_in_lookupSymbols[0], actual_lookupSymbols_as_string);
        }

        [Fact]
        public void LookupSymbolsAtEOF()
        {
            var source =
@"class
{
}";
            var tree = Parse(source);
            var comp = CreateCompilationWithMscorlib40(new[] { tree });
            var model = comp.GetSemanticModel(tree);
            var eof = tree.GetCompilationUnitRoot().FullSpan.End;
            Assert.NotEqual(0, eof);
            var symbols = model.LookupSymbols(eof);
            CompilationUtils.CheckISymbols(symbols, "System", "Microsoft");
        }

        [Fact, WorkItem(546523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546523")]
        public void TestLookupSymbolsNestedNamespacesNotImportedByUsings_01()
        {
            var source =
@"
using System;
 
class Program
{
    static void Main(string[] args)
    {
        /*pos*/
    }
}
";
            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode
            var actual_lookupSymbols = GetLookupSymbols(source);

            // Verify nested namespaces *are not* imported.
            var systemNS = (INamespaceSymbol)actual_lookupSymbols.Where((sym) => sym.Name.Equals("System") && sym.Kind == SymbolKind.Namespace).Single();
            INamespaceSymbol systemXmlNS = systemNS.GetNestedNamespace("Xml");
            Assert.DoesNotContain(systemXmlNS, actual_lookupSymbols);
        }

        [Fact, WorkItem(546523, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546523")]
        public void TestLookupSymbolsNestedNamespacesNotImportedByUsings_02()
        {
            var usings = "using X;";

            var source =
@"
using aliasY = X.Y;

namespace X
{
    namespace Y
    {
        public class InnerZ
        {
        }
    }

    public class Z
    {
    }

    public static class StaticZ
    {
    }
}

public class A
{
    public class B
    {
    }
}
 
class Program
{
    public static void Main()
    {
        /*pos*/
    }
}
";
            // Get the list of LookupSymbols at the location of the CSharpSyntaxNode
            var actual_lookupSymbols = GetLookupSymbols(usings + source, isScript: false);
            TestLookupSymbolsNestedNamespaces(actual_lookupSymbols);

            actual_lookupSymbols = GetLookupSymbols(source, isScript: true, globalUsings: new[] { usings });
            TestLookupSymbolsNestedNamespaces(actual_lookupSymbols);

            Action<ModuleSymbol> validator = (module) =>
            {
                NamespaceSymbol globalNS = module.GlobalNamespace;

                Assert.Equal(1, globalNS.GetMembers("X").Length);
                Assert.Equal(1, globalNS.GetMembers("A").Length);
                Assert.Equal(1, globalNS.GetMembers("Program").Length);

                Assert.Empty(globalNS.GetMembers("Y"));
                Assert.Empty(globalNS.GetMembers("Z"));
                Assert.Empty(globalNS.GetMembers("StaticZ"));
                Assert.Empty(globalNS.GetMembers("B"));
            };

            CompileAndVerify(source, sourceSymbolValidator: validator, symbolValidator: validator);
        }

        [Fact]
        [WorkItem(530826, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530826")]
        public void TestAmbiguousInterfaceLookup()
        {
            var source =
@"delegate void D();
interface I1
{
    void M();
}

interface I2
{
    event D M;
}

interface I3 : I1, I2 { }
public class P : I3
{
    event D I2.M { add { } remove { } }
    void I1.M() { }
}

class Q : P
{
    static int Main(string[] args)
    {
        Q p = new Q();
        I3 m = p;
        if (m.M is object) {}
        return 0;
    }
}";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().Where(n => n.ToString() == "m.M").Single();
            var symbolInfo = model.GetSymbolInfo(node);
            Assert.Equal("void I1.M()", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
            var node2 = (ExpressionSyntax)SyntaxFactory.SyntaxTree(node).GetRoot();
            symbolInfo = model.GetSpeculativeSymbolInfo(node.Position, node2, SpeculativeBindingOption.BindAsExpression);
            Assert.Equal("void I1.M()", symbolInfo.CandidateSymbols.Single().ToTestDisplayString());
            Assert.Equal(CandidateReason.OverloadResolutionFailure, symbolInfo.CandidateReason);
        }

        [Fact]
        public void TestLookupVerbatimVar()
        {
            var source = "class C { public static void Main() { @var v = 1; } }";
            CreateCompilation(source).VerifyDiagnostics(
                // (1,39): error CS0246: The type or namespace name 'var' could not be found (are you missing a using directive or an assembly reference?)
                // class C { public static void Main() { @var v = 1; } }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "@var").WithArguments("var").WithLocation(1, 39)
                );
        }

        private void TestLookupSymbolsNestedNamespaces(List<ISymbol> actual_lookupSymbols)
        {
            var namespaceX = (INamespaceSymbol)actual_lookupSymbols.Where((sym) => sym.Name.Equals("X") && sym.Kind == SymbolKind.Namespace).Single();

            // Verify nested namespaces within namespace X *are not* present in lookup symbols.
            INamespaceSymbol namespaceY = namespaceX.GetNestedNamespace("Y");
            Assert.DoesNotContain(namespaceY, actual_lookupSymbols);
            INamedTypeSymbol typeInnerZ = namespaceY.GetTypeMembers("InnerZ").Single();
            Assert.DoesNotContain(typeInnerZ, actual_lookupSymbols);

            // Verify nested types *are not* present in lookup symbols.
            var typeA = (INamedTypeSymbol)actual_lookupSymbols.Where((sym) => sym.Name.Equals("A") && sym.Kind == SymbolKind.NamedType).Single();
            INamedTypeSymbol typeB = typeA.GetTypeMembers("B").Single();
            Assert.DoesNotContain(typeB, actual_lookupSymbols);

            // Verify aliases to nested namespaces within namespace X *are* present in lookup symbols.
            var aliasY = (IAliasSymbol)actual_lookupSymbols.Where((sym) => sym.Name.Equals("aliasY") && sym.Kind == SymbolKind.Alias).Single();
            Assert.Contains(aliasY, actual_lookupSymbols);
        }

        [Fact]
        public void ExtensionMethodCall()
        {
            var source =
@"static class E
{
    internal static void F(this object o)
    {
    }
}
class C
{
    void M()
    {
        /*<bind>*/this.F/*</bind>*/();
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            compilation.VerifyDiagnostics();
            var exprs = GetExprSyntaxList(tree);
            var expr = GetExprSyntaxForBinding(exprs);
            var method = (IMethodSymbol)model.GetSymbolInfo(expr).Symbol;
            Assert.Equal("object.F()", method.ToDisplayString());
            var reducedFrom = method.ReducedFrom;
            Assert.NotNull(reducedFrom);
            Assert.Equal("E.F(object)", reducedFrom.ToDisplayString());
        }

        [WorkItem(3651, "https://github.com/dotnet/roslyn/issues/3651")]
        [Fact]
        public void ExtensionMethodDelegateCreation()
        {
            var source =
@"static class E
{
    internal static void F(this object o)
    {
    }
}
class C
{
    void M()
    {
        (new System.Action<object>(/*<bind>*/E.F/*</bind>*/))(this);
        (new System.Action(/*<bind1>*/this.F/*</bind1>*/))();
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            compilation.VerifyDiagnostics();
            var exprs = GetExprSyntaxList(tree);

            var expr = GetExprSyntaxForBinding(exprs, index: 0);
            var method = (IMethodSymbol)model.GetSymbolInfo(expr).Symbol;
            Assert.Null(method.ReducedFrom);
            Assert.Equal("E.F(object)", method.ToDisplayString());

            expr = GetExprSyntaxForBinding(exprs, index: 1);
            method = (IMethodSymbol)model.GetSymbolInfo(expr).Symbol;
            Assert.Equal("object.F()", method.ToDisplayString());
            var reducedFrom = method.ReducedFrom;
            Assert.NotNull(reducedFrom);
            Assert.Equal("E.F(object)", reducedFrom.ToDisplayString());
        }

        [WorkItem(7493, "https://github.com/dotnet/roslyn/issues/7493")]
        [Fact]
        public void GenericNameLookup()
        {
            var source = @"using A = List<int>;";
            var compilation = CreateCompilation(source).VerifyDiagnostics(
                // (1,11): error CS0246: The type or namespace name 'List<>' could not be found (are you missing a using directive or an assembly reference?)
                // using A = List<int>;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List<int>").WithArguments("List<>").WithLocation(1, 11),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using A = List<int>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using A = List<int>;").WithLocation(1, 1));
        }

        #endregion tests

        #region regressions

        [Fact]
        [WorkItem(552472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552472")]
        public void BrokenCode01()
        {
            var source =
@"Dele<Str> d3 = delegate (Dele<Str> d2 = delegate ()
{
    returne<double> d1 = delegate () { return 1; };
    {
        int result = 0;
        Dels Test : Base";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SemanticModel imodel = model;
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "returne<double>").First();
            imodel.GetSymbolInfo(node, default(CancellationToken));
        }

        [Fact]
        [WorkItem(552472, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552472")]
        public void BrokenCode02()
        {
            var source =
@"public delegate D D(D d);

class Program
{
    public D d3 = delegate(D d2 = delegate
        {
            System.Object x = 3;
            return null;
        }) {};
    public static void Main(string[] args)
    {
    }
}
";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            SemanticModel imodel = model;
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "System.Object").First();
            imodel.GetSymbolInfo(node, default(CancellationToken));
        }

        [Fact]
        public void InterfaceDiamondHiding()
        {
            var source = @"
interface T
{
    int P { get; set; }
    int Q { get; set; }
}

interface L : T
{
    new int P { get; set; }
}

interface R : T
{
    new int Q { get; set; }
}

interface B : L, R
{
}

class Test
{
    int M(B b)
    {
        return b.P + b.Q;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var global = comp.GlobalNamespace;

            var interfaceT = global.GetMember<NamedTypeSymbol>("T");
            var interfaceL = global.GetMember<NamedTypeSymbol>("L");
            var interfaceR = global.GetMember<NamedTypeSymbol>("R");
            var interfaceB = global.GetMember<NamedTypeSymbol>("B");

            var propertyTP = interfaceT.GetMember<PropertySymbol>("P");
            var propertyTQ = interfaceT.GetMember<PropertySymbol>("Q");
            var propertyLP = interfaceL.GetMember<PropertySymbol>("P");
            var propertyRQ = interfaceR.GetMember<PropertySymbol>("Q");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var syntaxes = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToArray();
            Assert.Equal(2, syntaxes.Length);

            // The properties in T are hidden - we bind to the properties on more-derived interfaces
            Assert.Equal(propertyLP.GetPublicSymbol(), model.GetSymbolInfo(syntaxes[0]).Symbol);
            Assert.Equal(propertyRQ.GetPublicSymbol(), model.GetSymbolInfo(syntaxes[1]).Symbol);

            int position = source.IndexOf("return", StringComparison.Ordinal);

            // We do the right thing with diamond inheritance (i.e. member is hidden along all paths
            // if it is hidden along any path) because we visit base interfaces in topological order.
            Assert.Equal(propertyLP.GetPublicSymbol(), model.LookupSymbols(position, interfaceB.GetPublicSymbol(), "P").Single());
            Assert.Equal(propertyRQ.GetPublicSymbol(), model.LookupSymbols(position, interfaceB.GetPublicSymbol(), "Q").Single());
        }

        [Fact]
        public void SemanticModel_OnlyInvalid()
        {
            var source = @"
public class C
{
    void M()
    {
        return;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            int position = source.IndexOf("return", StringComparison.Ordinal);

            var symbols = model.LookupNamespacesAndTypes(position, name: "M");
            Assert.Equal(0, symbols.Length);
        }

        [Fact]
        public void SemanticModel_InvalidHidingValid()
        {
            var source = @"
public class C<T>
{
    public class Inner
    {
        void T()
        {
            return;
        }
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var classC = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var methodT = classC.GetMember<NamedTypeSymbol>("Inner").GetMember<MethodSymbol>("T");

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            int position = source.IndexOf("return", StringComparison.Ordinal);

            var symbols = model.LookupSymbols(position, name: "T");
            Assert.Equal(methodT.GetPublicSymbol(), symbols.Single()); // Hides type parameter.

            symbols = model.LookupNamespacesAndTypes(position, name: "T");
            Assert.Equal(classC.TypeParameters.Single().GetPublicSymbol(), symbols.Single()); // Ignore intervening method.
        }

        [Fact]
        public void SemanticModel_MultipleValid()
        {
            var source = @"
public class Outer
{
    void M(int x)
    {
    }

    void M()
    {
        return;
    }
}
";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            int position = source.IndexOf("return", StringComparison.Ordinal);

            var symbols = model.LookupSymbols(position, name: "M");
            Assert.Equal(2, symbols.Length);
        }

        [Fact, WorkItem(1078958, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078958")]
        public void Bug1078958()
        {
            const string source = @"
class C
{
    static void Goo<T>()
    {
        /*<bind>*/T/*</bind>*/();
    }
 
    static void T() { }
}";

            var symbols = GetLookupSymbols(source);
            Assert.True(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961()
        {
            const string source = @"
class C
{
    const int T = 42;
    static void Goo<T>(int x = /*<bind>*/T/*</bind>*/)
    {
        System.Console.Write(x);
    }

    static void Main()
    {
        Goo<object>();
    }
}";

            var symbols = GetLookupSymbols(source);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_2()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    static void Goo<T>([A(/*<bind>*/T/*</bind>*/)] int x)
    {
    }
}";

            var symbols = GetLookupSymbols(source);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_3()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    [A(/*<bind>*/T/*</bind>*/)]
    static void Goo<T>(int x)
    {
    }
}";

            var symbols = GetLookupSymbols(source);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_4()
        {
            const string source = @"
class A : System.Attribute
{
    public A(int i) { }
}

class C
{
    const int T = 42;

    static void Goo<[A(/*<bind>*/T/*</bind>*/)] T>(int x)
    {
    }
}";

            var symbols = GetLookupSymbols(source);
            Assert.False(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_5()
        {
            const string source = @"
class C
{
    class T { }

    static void Goo<T>(T x = default(/*<bind>*/T/*</bind>*/))
    {
        System.Console.Write((object)x == null);
    }

    static void Main()
    {
        Goo<object>();
    }
}";

            var symbols = GetLookupSymbols(source);
            Assert.True(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1078961, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1078961")]
        public void Bug1078961_6()
        {
            const string source = @"
class C
{
    class T { }

    static void Goo<T>(T x = default(/*<bind>*/T/*</bind>*/))
    {
        System.Console.Write((object)x == null);
    }

    static void Main()
    {
        Goo<object>();
    }
}";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var position = GetPositionForBinding(tree);

            var symbols = model.LookupNamespacesAndTypes(position);
            Assert.True(symbols.Any(s => s.Kind == SymbolKind.TypeParameter));
        }

        [Fact, WorkItem(1091936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091936")]
        public void Bug1091936_1()
        {
            const string source = @"
class Program
{
    static object M(long l) { return null; }
    static object M(int i) { return null; }

    static void Main(string[] args)
    {
        (M(0))?.ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var ms = comp.GlobalNamespace.GetTypeMembers("Program").Single().GetMembers("M").OfType<MethodSymbol>();
            var m = ms.Where(mm => mm.Parameters[0].Type.SpecialType == SpecialType.System_Int32).Single();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var call = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();

            var symbolInfo = model.GetSymbolInfo(call.Expression);
            Assert.NotEqual(default, symbolInfo);
            Assert.Equal(symbolInfo.Symbol.GetSymbol(), m);
        }

        [Fact, WorkItem(1091936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091936")]
        public void Bug1091936_2()
        {
            const string source = @"
class Program
{
    static object M = null;

    static void Main(string[] args)
    {
        M?.ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var m = comp.GlobalNamespace.GetTypeMembers("Program").Single().GetMembers("M").Single();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.NotEqual(default, symbolInfo);
            Assert.Equal(symbolInfo.Symbol.GetSymbol(), m);
        }

        [Fact, WorkItem(1091936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091936")]
        public void Bug1091936_3()
        {
            const string source = @"
class Program
{
    object M = null;

    static void Main(string[] args)
    {
        (new Program()).M?.ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var m = comp.GlobalNamespace.GetTypeMembers("Program").Single().GetMembers("M").Single();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single().Expression;

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.NotEqual(default, symbolInfo);
            Assert.Equal(symbolInfo.Symbol.GetSymbol(), m);
        }

        [Fact, WorkItem(1091936, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1091936")]
        public void Bug1091936_4()
        {
            const string source = @"
class Program
{
    static void Main(string[] args)
    {
        var y = (System.Linq.Enumerable.Select<string, int>(args, s => int.Parse(s)))?.ToString();
    }
}
";

            var comp = CreateCompilationWithMscorlib40AndSystemCore(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            var node = tree.GetRoot().DescendantNodes().OfType<GenericNameSyntax>().Single();

            var symbolInfo = model.GetSymbolInfo(node);
            Assert.NotEqual(default, symbolInfo);
            Assert.NotNull(symbolInfo.Symbol);
        }

        [Fact]
        public void GenericAttribute_LookupSymbols_01()
        {
            var source = @"
using System;
class Attr1<T> : Attribute { public Attr1(T t) { } }

[Attr1<string>(""a"")]
class C { }";

            var comp = CreateCompilation(source);
            comp.VerifyDiagnostics();

            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();
            var symbol = model.GetSymbolInfo(node);
            Assert.Equal("Attr1<System.String>..ctor(System.String t)", symbol.Symbol.ToTestDisplayString());
        }

        [Fact]
        public void GenericAttribute_LookupSymbols_02()
        {
            var source = @"
using System;
class Attr1<T> : Attribute { public Attr1(T t) { } }

[Attr1</*<bind>*/string/*</bind>*/>]
class C { }";

            var names = GetLookupNames(source);
            Assert.Contains("C", names);
            Assert.Contains("Attr1", names);
        }

        #endregion
    }
}
