// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Test.Utilities;
using Xunit;

namespace Analyzer.Utilities.UnitTests.Options
{
    public sealed class SymbolNamesWithValueOptionTests
    {
        [Fact]
        public void WhenNoSymbolNames_ReturnsEmpty()
        {
            // Arrange & act
            var options = SymbolNamesWithValueOption<Unit>.Create(ImmutableArray<string>.Empty, GetCompilation(), null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Equal(SymbolNamesWithValueOption<Unit>.Empty, options);
        }

        [Fact]
        public void SymbolNamePartsFuncIsCalledForEachSymbol()
        {
            // Arrange
            var callCount = 0;
            var symbolNames = ImmutableArray.Create("a", "b");
            SymbolNamesWithValueOption<Unit>.NameParts func(string symbolName)
            {
                if (symbolNames.Contains(symbolName))
                {
                    callCount++;
                }

                return new SymbolNamesWithValueOption<Unit>.NameParts(symbolName, Unit.Default);
            }

            // Act
            SymbolNamesWithValueOption<Unit>.Create(symbolNames, GetCompilation(), null, func);

            // Assert
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void UnqualifiedSymbolNamesAreProcessedAsNames()
        {
            // Arrange
            // Note that there is no check for the existence of the member
            var compilation = GetCompilation();
            var symbolNames = ImmutableArray.Create("MyNamespace", "MyClass", "MyMethod()", "MyProperty", "MyEvent", "MyField");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Equal(symbolNames.Length, options.GetTestAccessor().Names.Count);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Empty(options.GetTestAccessor().WildcardNamesBySymbolKind);
        }

        [Fact]
        public void QualifiedSymbolNamesWithoutPrefixAreIgnored()
        {
            // Arrange
            var compilation = GetCompilation("""
                using System;

                public namespace MyNamespace
                {
                    public class MyClass
                    {
                        public int MyField;
                        public int MyProperty { get; set; }
                        public event EventHandler<EventArgs> MyEvent;
                        public void MyMethod() {}
                    }
                }
                """);
            var symbolNames = ImmutableArray.Create(
                "MyNamespace.MySubNamespace",
                "MyNamespace.MyClass",
                "MyNamespace.MyClass.MyField",
                "MyNamespace.MyClass.MyProperty",
                "MyNamespace.MyClass.MyEvent",
                "MyNamespace.MyClass.MyMethod()");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Empty(options.GetTestAccessor().WildcardNamesBySymbolKind);
        }

        [Fact]
        public void QualifiedSymbolNamesWithPrefixAreProcessedAsSymbols()
        {
            // Arrange
            var compilation = GetCompilation("""
                using System;

                public namespace MyNamespace
                {
                    public class MyClass
                    {
                        public int MyField;
                        public int MyProperty { get; set; }
                        public event EventHandler<EventArgs> MyEvent;
                        public void MyMethod() {}
                    }
                }
                """);
            var symbolNames = ImmutableArray.Create(
                "N:MyNamespace",
                "T:MyNamespace.MyClass",
                "F:MyNamespace.MyClass.MyField",
                "P:MyNamespace.MyClass.MyProperty",
                "E:MyNamespace.MyClass.MyEvent",
                "M:MyNamespace.MyClass.MyMethod()");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Equal(symbolNames.Length, options.GetTestAccessor().Symbols.Count);
            Assert.Empty(options.GetTestAccessor().WildcardNamesBySymbolKind);
        }

        [Fact]
        public void UnfoundQualifiedSymbolNamesWithPrefixAreExcluded()
        {
            // Arrange
            var compilation = GetCompilation();
            var symbolNames = ImmutableArray.Create(
                "N:MyNamespace",
                "T:MyNamespace.MyClass",
                "F:MyNamespace.MyClass.MyField",
                "P:MyNamespace.MyClass.MyProperty",
                "E:MyNamespace.MyClass.MyEvent",
                "M:MyNamespace.MyClass.MyMethod()");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Empty(options.GetTestAccessor().WildcardNamesBySymbolKind);
        }

        [Fact]
        public void UnsupportedWildcardConstructionsAreIgnored()
        {
            // Arrange
            var compilation = GetCompilation();
            var symbolNames = ImmutableArray.Create(
                "*",        // only wildcard symbol
                "*a",       // wildcard symbol is not last
                "a*a",      // wildcard symbol is not last
                "*a*");     // more than one wildcard symbol

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Empty(options.GetTestAccessor().WildcardNamesBySymbolKind);
        }

        [Fact]
        public void WildcardConstructionsWithoutPrefixAreCorrectlyClassified()
        {
            // Arrange
            var compilation = GetCompilation();
            var symbolNames = ImmutableArray.Create(
                "MyNamespace*",
                "MyNamespace.MyClass*",
                "MyNamespace.MyClass.MyField*",
                "MyNamespace.MyClass.MyProperty*",
                "MyNamespace.MyClass.MyEvent*",
                "MyNamespace.MyClass.MyMethod(*");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Single(options.GetTestAccessor().WildcardNamesBySymbolKind);
            Assert.Equal(symbolNames.Length, options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].Count);
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyField"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyProperty"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyEvent"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyMethod("));
        }

        [Fact]
        public void WildcardConstructionsWithPrefixAreCorrectlyClassified()
        {
            // Arrange
            var compilation = GetCompilation();
            var symbolNames = ImmutableArray.Create(
                "N:MyNamespace*",
                "T:MyNamespace.MyClass*",
                "F:MyNamespace.MyClass.MyField*",
                "P:MyNamespace.MyClass.MyProperty*",
                "E:MyNamespace.MyClass.MyEvent*",
                "M:MyNamespace.MyClass.MyMethod(*");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Assert
            Assert.Empty(options.GetTestAccessor().Names);
            Assert.Empty(options.GetTestAccessor().Symbols);
            Assert.Equal(symbolNames.Length, options.GetTestAccessor().WildcardNamesBySymbolKind.Count);
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.Namespace].ContainsKey("MyNamespace"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.NamedType].ContainsKey("MyNamespace.MyClass"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.Field].ContainsKey("MyNamespace.MyClass.MyField"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.Property].ContainsKey("MyNamespace.MyClass.MyProperty"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.Event].ContainsKey("MyNamespace.MyClass.MyEvent"));
            Assert.True(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.Method].ContainsKey("MyNamespace.MyClass.MyMethod("));
        }

        [Fact]
        public void ValueCanBeAssociatedWithAllSymbolNames()
        {
            // Arrange
            var compilation = GetCompilation("""
                using System;

                public namespace MyNamespace
                {
                    public class MyClass {}
                }
                """);
            var symbolNames = ImmutableArray.Create(
                "MyClass->SomeValue1",
                "T:MyNamespace.MyClass->SomeValue2",
                "MyClass*->SomeValue3",
                "T:MyClass*->SomeValue4");

            var namedTypeSymbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("MyClass").Single();

            // Act
            var options = SymbolNamesWithValueOption<string>.Create(symbolNames, compilation, null,
                getSymbolNamePartsFunc: symbolName =>
                {
                    var split = symbolName.Split(["->"], StringSplitOptions.RemoveEmptyEntries);
                    return new SymbolNamesWithValueOption<string>.NameParts(split[0], split[1]);
                });

            // Assert
            Assert.Single(options.GetTestAccessor().Names);
            Assert.Equal("SomeValue1", options.GetTestAccessor().Names["MyClass"]);
            Assert.Single(options.GetTestAccessor().Symbols);
            Assert.Equal("SomeValue2", options.GetTestAccessor().Symbols[namedTypeSymbol]);
            Assert.Equal(2, options.GetTestAccessor().WildcardNamesBySymbolKind.Count);
            Assert.Single(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds]);
            Assert.Equal("SomeValue3", options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds]["MyClass"]);
            Assert.Single(options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.NamedType]);
            Assert.Equal("SomeValue4", options.GetTestAccessor().WildcardNamesBySymbolKind[SymbolKind.NamedType]["MyClass"]);
        }

        [Fact]
        [WorkItem(1242125, "https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1242125")]
        public void FastPathDoesNotCache()
        {
            var compilation = GetCompilation("""
                using System;

                public namespace MyNamespace
                {
                    public class MyClass {}
                }
                """);

            var namedTypeSymbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("MyClass").Single();

            var options = SymbolNamesWithValueOption<Unit>.Empty;

            // Check for containment
            var contained = options.Contains(namedTypeSymbol);
            Assert.False(contained);
            Assert.Empty(options.GetTestAccessor().WildcardMatchResult);
            Assert.Empty(options.GetTestAccessor().SymbolToDeclarationId);
        }

        [Theory]
        // Symbol name
        [InlineData("My*", "MyCompany")]
        [InlineData("My*", "MyOuterClass")]
        [InlineData("My*", "MyInnerClass")]
        [InlineData("My*", "MyField")]
        [InlineData("My*", "MyProperty")]
        [InlineData("My*", "MyEvent")]
        [InlineData("My*", "MyMethod")]
        [InlineData("My*", "MyMethod2")]
        // Fully qualified name with prefix
        [InlineData("N:MyCompany*", "MyCompany")]
        [InlineData("N:MyCompany.MyProduct*", "MyProduct")]
        [InlineData("N:MyCompany.MyProduct.MyFeature*", "MyFeature")]
        [InlineData("T:MyCompany.MyProduct.MyFeature.MyOuterClass*", "MyOuterClass")]
        [InlineData("T:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass*", "MyInnerClass")]
        [InlineData("F:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyField*", "MyField")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass..ctor*", ".ctor")]
        [InlineData("P:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyProperty*", "MyProperty")]
        [InlineData("P:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.Item*", "this[]")]
        [InlineData("E:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyEvent*", "MyEvent")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod*", "MyMethod")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod2(System.Str*", "MyMethod2")]
        // Fully qualified name without prefix
        [InlineData("MyCompany*", "MyCompany")]
        [InlineData("MyCompany.MyProduct*", "MyProduct")]
        [InlineData("MyCompany.MyProduct.MyFeature*", "MyFeature")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass*", "MyOuterClass")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass*", "MyInnerClass")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyField*", "MyField")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass..ctor*", ".ctor")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyProperty*", "MyProperty")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.Item*", "this[]")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyEvent*", "MyEvent")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod*", "MyMethod")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod2(System.Str*", "MyMethod2")]
        public void WildcardMatch(string patternName, string symbolName)
        {
            // Arrange
            var compilation = GetCompilation("""
                public namespace MyCompany.MyProduct.MyFeature
                {
                    public class MyOuterClass
                    {
                        public class MyInnerClass
                        {
                            public int MyField;
                            public MyInnerClass() {}
                            public MyInnerClass(int i) {}
                            public int MyProperty { get; set; }
                            public int this[]
                            {
                                get { return 42; }
                                set {}
                            }
                            public int this[string s]
                            {
                                get { return 42; }
                                set {}
                            }
                            public event EventHandler<EventArgs> MyEvent;
                            public void MyMethod() {}
                            public void MyMethod2(string s) {}
                        }
                    }
                }
                """);
            var symbolNames = ImmutableArray.Create(patternName);
            var symbol = FindSymbol(compilation, symbolName);
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

            // Act
            var isFound = options.Contains(symbol);

            // Assert
            Assert.True(isFound);
            Assert.True(options.GetTestAccessor().WildcardMatchResult.ContainsKey(symbol));

            static ISymbol FindSymbol(Compilation compilation, string symbolName)
            {
                var innerClassSymbol = (INamedTypeSymbol)compilation.GetSymbolsWithName("MyInnerClass", SymbolFilter.Type).Single();

                var currentType = innerClassSymbol;
                while (currentType != null)
                {
                    if (currentType.Name == symbolName)
                    {
                        return currentType;
                    }

                    currentType = currentType.ContainingType;
                }

                var currentNamespace = innerClassSymbol.ContainingNamespace;
                while (currentNamespace != null)
                {
                    if (currentNamespace.Name == symbolName)
                    {
                        return currentNamespace;
                    }

                    currentNamespace = currentNamespace.ContainingNamespace;
                }

                foreach (var member in innerClassSymbol.GetMembers())
                {
                    if (member.Name == symbolName)
                    {
                        return member;
                    }
                }

                throw new InvalidOperationException("Cannot find symbol name: " + symbolName);
            }
        }

        private static Compilation GetCompilation(params string[] sources)
        {
            var cancellationToken = CancellationToken.None;
            const string TestProjectName = "SymbolNamesTestProject";
            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            var references = Task.Run(() => AdditionalMetadataReferences.Default.ResolveAsync(LanguageNames.CSharp, cancellationToken)).GetAwaiter().GetResult();

#pragma warning disable CA2000 // Dispose objects before losing scope - Current solution/project takes the dispose ownership of the created AdhocWorkspace
            var project = new AdhocWorkspace().CurrentSolution
#pragma warning restore CA2000 // Dispose objects before losing scope
                .AddProject(projectId, TestProjectName, TestProjectName, LanguageNames.CSharp)
                .AddMetadataReferences(projectId, references)
                .WithProjectCompilationOptions(projectId, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithProjectParseOptions(projectId, new CSharpParseOptions())
                .GetProject(projectId)!;

            int count = 0;
            foreach (var source in sources)
            {
                string newFileName = $"Test{count++}.cs";
                DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                project = project.AddDocument(newFileName, SourceText.From(source)).Project;
            }

            Assert.True(project.SupportsCompilation);
            return project.GetCompilationAsync(cancellationToken).Result!;
        }
    }
}
