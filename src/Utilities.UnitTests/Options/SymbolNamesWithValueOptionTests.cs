// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var options = SymbolNamesWithValueOption<Unit>.Create(ImmutableArray<string>.Empty, GetCompilation(), null, null);

            // Assert
            Assert.Equal(SymbolNamesWithValueOption<Unit>.Empty, options);
        }

        [Fact]
        public void SymbolNamePartsFuncIsCalledForEachSymbol()
        {
            // Arrange
            var callCount = 0;
            var symbolNames = ImmutableArray.Create("a", "b");
            Func<string, SymbolNamesWithValueOption<Unit>.NameParts> func = symbolName =>
            {
                if (symbolNames.Contains(symbolName))
                {
                    callCount++;
                }
                return new SymbolNamesWithValueOption<Unit>.NameParts(symbolName);
            };

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
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Equal(symbolNames.Length, options._names.Count);
            Assert.Empty(options._symbols);
            Assert.Empty(options._wildcardNamesBySymbolKind);
        }

        [Fact]
        public void QualifiedSymbolNamesWithoutPrefixAreIgnored()
        {
            // Arrange
            var compilation = GetCompilation(@"
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
}");
            var symbolNames = ImmutableArray.Create(
                "MyNamespace.MySubNamespace",
                "MyNamespace.MyClass",
                "MyNamespace.MyClass.MyField",
                "MyNamespace.MyClass.MyProperty",
                "MyNamespace.MyClass.MyEvent",
                "MyNamespace.MyClass.MyMethod()");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Empty(options._symbols);
            Assert.Empty(options._wildcardNamesBySymbolKind);
        }

        [Fact]
        public void QualifiedSymbolNamesWithPrefixAreProcessedAsSymbols()
        {
            // Arrange
            var compilation = GetCompilation(@"
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
}");
            var symbolNames = ImmutableArray.Create(
                "N:MyNamespace",
                "T:MyNamespace.MyClass",
                "F:MyNamespace.MyClass.MyField",
                "P:MyNamespace.MyClass.MyProperty",
                "E:MyNamespace.MyClass.MyEvent",
                "M:MyNamespace.MyClass.MyMethod()");

            // Act
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Equal(symbolNames.Length, options._symbols.Count);
            Assert.Empty(options._wildcardNamesBySymbolKind);
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
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Empty(options._symbols);
            Assert.Empty(options._wildcardNamesBySymbolKind);
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
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Empty(options._symbols);
            Assert.Empty(options._wildcardNamesBySymbolKind);
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
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Empty(options._symbols);
            Assert.Single(options._wildcardNamesBySymbolKind);
            Assert.Equal(symbolNames.Length, options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].Count);
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyField"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyProperty"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyEvent"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds].ContainsKey("MyNamespace.MyClass.MyMethod("));
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
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Assert
            Assert.Empty(options._names);
            Assert.Empty(options._symbols);
            Assert.Equal(symbolNames.Length, options._wildcardNamesBySymbolKind.Count);
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.Namespace].ContainsKey("MyNamespace"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.NamedType].ContainsKey("MyNamespace.MyClass"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.Field].ContainsKey("MyNamespace.MyClass.MyField"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.Property].ContainsKey("MyNamespace.MyClass.MyProperty"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.Event].ContainsKey("MyNamespace.MyClass.MyEvent"));
            Assert.True(options._wildcardNamesBySymbolKind[SymbolKind.Method].ContainsKey("MyNamespace.MyClass.MyMethod("));
        }

        [Fact]
        public void ValueCanBeAssociatedWithAllSymbolNames()
        {
            // Arrange
            var compilation = GetCompilation(@"
using System;

public namespace MyNamespace
{
    public class MyClass {}
}");
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
                    var split = symbolName.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                    return new SymbolNamesWithValueOption<string>.NameParts(split[0], split[1]);
                });

            // Assert
            Assert.Single(options._names);
            Assert.Equal("SomeValue1", options._names["MyClass"]);
            Assert.Single(options._symbols);
            Assert.Equal("SomeValue2", options._symbols[namedTypeSymbol]);
            Assert.Equal(2, options._wildcardNamesBySymbolKind.Count);
            Assert.Single(options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds]);
            Assert.Equal("SomeValue3", options._wildcardNamesBySymbolKind[SymbolNamesWithValueOption<Unit>.AllKinds]["MyClass"]);
            Assert.Single(options._wildcardNamesBySymbolKind[SymbolKind.NamedType]);
            Assert.Equal("SomeValue4", options._wildcardNamesBySymbolKind[SymbolKind.NamedType]["MyClass"]);
        }

        [Theory]
        // Fully qualified name without prefix
        [InlineData("MyCompany*", "MyCompany")]
        [InlineData("MyCompany.MyProduct*", "MyProduct")]
        [InlineData("MyCompany.MyProduct.MyFeature*", "MyFeature")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass*", "MyOuterClass")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass*", "MyInnerClass")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyField*", "MyField")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.ctor*", ".ctor")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyProperty*", "MyProperty")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.this[]*", "this[]")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyEvent*", "MyEvent")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod(*", "MyMethod")]
        [InlineData("MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod2(System.Str*", "MyMethod2")]
        // Fully qualified name with prefix
        [InlineData("N:MyCompany*", "MyCompany")]
        [InlineData("N:MyCompany.MyProduct*", "MyProduct")]
        [InlineData("N:MyCompany.MyProduct.MyFeature*", "MyFeature")]
        [InlineData("T:MyCompany.MyProduct.MyFeature.MyOuterClass*", "MyOuterClass")]
        [InlineData("T:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass*", "MyInnerClass")]
        [InlineData("F:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyField*", "MyField")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.ctor*", ".ctor")]
        [InlineData("P:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyProperty*", "MyProperty")]
        [InlineData("P:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.this[]*", "this[]")]
        [InlineData("E:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyEvent*", "MyEvent")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod(*", "MyMethod")]
        [InlineData("M:MyCompany.MyProduct.MyFeature.MyOuterClass.MyInnerClass.MyMethod2(System.Str*", "MyMethod2")]
        // Partial match
        [InlineData("MyCompany.MyProduct*", "MyFeature")]
        [InlineData("N:MyCompany.MyProduct*", "MyFeature")]
        [InlineData("MyFeature*", "MyFeature")]
        [InlineData("MyInnerClass*", "MyInnerClass")]
        [InlineData("MyOuterClass*", "MyInnerClass")]
        [InlineData("My*", "MyCompany")]
        [InlineData("My*", "MyOuterClass")]
        [InlineData("My*", "MyInnerClass")]
        [InlineData("My*", "MyField")]
        [InlineData("My*", "MyProperty")]
        [InlineData("My*", "MyEvent")]
        [InlineData("My*", "MyMethod")]
        [InlineData("My*", "MyMethod2")]
        public void WildcardMatch(string patternName, string symbolName)
        {
            // Arrange
            var compilation = GetCompilation(@"
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
}");
            var symbolNames = ImmutableArray.Create(patternName);
            var symbol = FindSymbol(compilation, symbolName);
            var options = SymbolNamesWithValueOption<Unit>.Create(symbolNames, compilation, null, null);

            // Act
            var isFound = options.Contains(symbol);

            // Assert
            Assert.True(isFound);
            Assert.True(options._wildcardMatchResult.ContainsKey(symbol));

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
                .WithProjectParseOptions(projectId, null)
                .GetProject(projectId);

            int count = 0;
            foreach (var source in sources)
            {
                string newFileName = $"Test{count++}.cs";
                DocumentId documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                project = project.AddDocument(newFileName, SourceText.From(source)).Project;
            }

            return project.GetCompilationAsync(cancellationToken).Result;
        }
    }
}
