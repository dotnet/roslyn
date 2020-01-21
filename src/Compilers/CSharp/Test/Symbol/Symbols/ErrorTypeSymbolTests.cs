// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class ErrorTypeSymbolTests : CSharpTestBase
    {
        [WorkItem(546143, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546143")]
        [Fact]
        public void ConstructedErrorTypes()
        {
            var source1 =
@"public class A<T>
{
    public class B<U> { }
}";
            var compilation1 = CreateCompilation(source1, assemblyName: "91AB32B7-DDDF-4E50-87EF-4E8B0A664A41");
            compilation1.VerifyDiagnostics();
            var reference1 = MetadataReference.CreateFromImage(compilation1.EmitToArray(options: new EmitOptions(metadataOnly: true)));

            // Binding types in source, no missing types.
            var source2 =
@"class C1<T, U> : A<T>.B<U> { }
class C2<T, U> : A<T>.B<U> { }
class C3<T> : A<T>.B<object> { }
class C4<T> : A<object>.B<T> { }
class C5 : A<object>.B<int> { }
class C6 : A<string>.B<object> { }
class C7 : A<string>.B<object> { }";
            var compilation2 = CreateCompilation(source2, references: new[] { reference1 }, assemblyName: "91AB32B7-DDDF-4E50-87EF-4E8B0A664A42");
            compilation2.VerifyDiagnostics();
            CompareConstructedErrorTypes(compilation2, missingTypes: false, fromSource: true);
            var reference2 = MetadataReference.CreateFromImage(compilation2.EmitToArray(options: new EmitOptions(metadataOnly: true)));

            // Loading types from metadata, no missing types.
            var source3 =
@"";
            var compilation3 = CreateCompilation(source3, references: new[] { reference1, reference2 });
            compilation3.VerifyDiagnostics();
            CompareConstructedErrorTypes(compilation3, missingTypes: false, fromSource: false);

            // Binding types in source, missing types, resulting inExtendedErrorTypeSymbols.
            var compilation4 = CreateCompilation(source2);
            CompareConstructedErrorTypes(compilation4, missingTypes: true, fromSource: true);

            // Loading types from metadata, missing types, resulting in ErrorTypeSymbols.
            var source5 =
@"";
            var compilation5 = CreateCompilation(source5, references: new[] { reference2 });
            CompareConstructedErrorTypes(compilation5, missingTypes: true, fromSource: false);
        }

        private void CompareConstructedErrorTypes(CSharpCompilation compilation, bool missingTypes, bool fromSource)
        {
            // Get all root types.
            var allTypes = compilation.GlobalNamespace.GetTypeMembers();

            // Get base class for each type named "C?".
            var types = new[] { "C1", "C2", "C3", "C4", "C5", "C6", "C7" }.Select(name => allTypes.First(t => t.Name == name).BaseType()).ToArray();
            foreach (var type in types)
            {
                var constructedFrom = type.ConstructedFrom;
                Assert.NotEqual(type, constructedFrom);
                if (missingTypes)
                {
                    Assert.True(type.IsErrorType());
                    Assert.True(constructedFrom.IsErrorType());
                    var extendedError = constructedFrom as ExtendedErrorTypeSymbol;
                    if (fromSource)
                    {
                        Assert.NotNull(extendedError);
                    }
                    else
                    {
                        Assert.Null(extendedError);
                    }
                }
                else
                {
                    Assert.False(type.IsErrorType());
                    Assert.False(constructedFrom.IsErrorType());
                }
            }

            // Compare pairs of types. The only error types that
            // should compare equal are C6 and C7.
            const int n = 7;
            for (int i = 0; i < n - 1; i++)
            {
                var typeA = types[i];
                for (int j = i + 1; j < n; j++)
                {
                    var typeB = types[j];
                    bool expectedEqual = (i == 5) && (j == 6);
                    Assert.Equal(TypeSymbol.Equals(typeA, typeB, TypeCompareKind.ConsiderEverything2), expectedEqual);
                }
            }
        }
    }
}
