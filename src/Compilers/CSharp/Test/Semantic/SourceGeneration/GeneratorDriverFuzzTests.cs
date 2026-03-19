// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Roslyn.Test.Utilities.TestGenerators;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable RS0062 // Do not implicitly capture primary constructor parameters

namespace Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.SourceGeneration
{
    public class GeneratorDriverFuzzTests
         : CSharpTestBase
    {
        private readonly ITestOutputHelper _output;

        public GeneratorDriverFuzzTests(ITestOutputHelper output)
        {
            _output = output;
        }

        enum InputChangeKind
        {
            Cached,
            Modified,
            Deleted,
            MaxValue
        }

        enum OperatorKind
        {
            Select,
            SelectMany,
            Where,
            Combine,
            MaxValue
        }

        class HintNameProvider(int nextHintNameId)
        {
            public void Reset(int id) => nextHintNameId = id;
            public string GetNextHintName()
            {
                var name = nextHintNameId.ToString();
                nextHintNameId++;
                return name;
            }
        }

        /// <summary>
        /// Isomorphic to the IncrementalValueProvider tree.
        /// Used to generate code which reconstructs the IncrementalValueProvider tree.
        /// </summary>
        abstract class Operator
        {
            public required HintNameProvider HintNameProvider { protected get; init; }

            /// <summary>Creates an IncrementalValuesProvider which is the result of applying this operator to <paramref name="provider"/>.</summary>
            public abstract IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider);

            /// <summary>Generates source equivalent to application of this operator.</summary>
            public abstract void AppendTo(StringBuilder builder);
        }

        class SelectOperator(Operator Source, bool TransformAs, bool TransformCs) : Operator
        {
            public override IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider)
            {
                var provider1 = provider.Select((additionalText, _) => (AdditionalText)new InMemoryAdditionalText(additionalText.Path, additionalText.GetText()!.ToString() switch
                {
                    "a" when TransformAs => "b",
                    "c" when TransformCs => "d",
                    var other => other
                }));
                return Source.Apply(provider1);
            }

            public override void AppendTo(StringBuilder builder)
            {
                Source.AppendTo(builder);
                builder.AppendLine("""
                                        .Select((additionalText, _) => (AdditionalText)new InMemoryAdditionalText(additionalText.Path, additionalText.GetText()!.ToString() switch
                                        {
                        """);
                if (TransformAs)
                {
                    builder.AppendLine("""
                                            "a" => "b",
                        """);
                }
                if (TransformCs)
                {
                    builder.AppendLine("""
                                            "c" => "d",
                        """);
                }
                builder.AppendLine("""
                                            var other => other
                                        }))
                        """);
            }
        }

        class SelectManyOperator(Operator Source, ImmutableArray<(bool TransformAs, bool TransformCs)> Logics) : Operator
        {
            public override IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider)
            {
                return Source.Apply(provider).SelectMany((additionalText, _) =>
                    Logics.Select(logic => (AdditionalText)new InMemoryAdditionalText(HintNameProvider.GetNextHintName(), additionalText.GetText()!.ToString() switch
                    {
                        "a" when logic.TransformAs => "b",
                        "c" when logic.TransformCs => "d",
                        var other => other
                    })));
            }

            public override void AppendTo(StringBuilder builder)
            {
                Source.AppendTo(builder);
                builder.AppendLine("""
                                        .SelectMany((additionalText, _) => new (bool TransformAs, bool TransformCs)[] {
                        """);
                foreach (var logic in Logics)
                {
                    builder.AppendLine($"""
                                            ({logic.TransformAs.ToString().ToLowerInvariant()}, {logic.TransformCs.ToString().ToLowerInvariant()}),
                        """);
                }

                builder.AppendLine("""
                                        }.Select(logic => (AdditionalText)new InMemoryAdditionalText(hintNameProvider.GetNextHintName(), additionalText.GetText()!.ToString() switch
                                        {
                                            "a" when logic.TransformAs => "b",
                                            "c" when logic.TransformCs => "d",
                                            var other => other
                                        })))
                        """);

            }
        }

        class WhereOperator(Operator Source, bool IncludeAs, bool IncludeBs, bool IncludeCs, bool IncludeDs) : Operator
        {
            public override IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider)
            {
                var provider1 = provider.Where(additionalText => additionalText.GetText()!.ToString() is var textString
                    && ((IncludeAs && textString == "a")
                        || (IncludeBs && textString == "b")
                        || (IncludeCs && textString == "c")
                        || (IncludeDs && textString == "d")));
                return Source.Apply(provider1);
            }

            public override void AppendTo(StringBuilder builder)
            {
                Source.AppendTo(builder);
                builder.AppendLine("""
                                        .Where(additionalText => additionalText.GetText()!.ToString() is var textString &&
                                            (false
                        """);
                if (IncludeAs)
                {
                    builder.AppendLine("""
                                                || textString == "a"
                            """);
                }
                if (IncludeBs)
                {
                    builder.AppendLine("""
                                                || textString == "b"
                            """);
                }
                if (IncludeCs)
                {
                    builder.AppendLine("""
                                                || textString == "c"
                            """);
                }
                if (IncludeDs)
                {
                    builder.AppendLine("""
                                                || textString == "d"
                            """);
                }
                builder.AppendLine("""
                                        ))
                        """);
            }
        }

        class CombineOperator(Operator Source1, Operator Source2) : Operator
        {
            public override IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider)
            {
                var provider4_1 = Source1.Apply(provider);
                var provider4_2 = Source2.Apply(provider);
                var provider4 = provider4_1.Combine(provider4_2.Collect()).Select((pair, _)
                        => (AdditionalText)new InMemoryAdditionalText(
                            pair.Left.Path,
                            string.Join("", pair.Right.Select(text => text.GetText()!.ToString()))));
                return provider4;
            }

            public override void AppendTo(StringBuilder builder)
            {
                Source1.AppendTo(builder);
                builder.Append("""
                                        .Combine(
                        """);

                Source2.AppendTo(builder);
                builder.AppendLine("""
                                        .Collect()
                                        )
                                        .Select((pair, _)
                                            => (AdditionalText)new InMemoryAdditionalText(
                                                pair.Left.Path,
                                                string.Join("", pair.Right.Select(text => text.GetText()!.ToString()))))
                        """);
            }
        }

        // Represents the original input.
        class LeafOperator : Operator
        {
            public override IncrementalValuesProvider<AdditionalText> Apply(IncrementalValuesProvider<AdditionalText> provider) => provider;

            public override void AppendTo(StringBuilder builder)
            {
                builder.AppendLine("context.AdditionalTextsProvider");
            }
        }

        [Fact]
        public void Fuzz_Generate()
        {
            var random = new Random();
            for (var iteration = 0; iteration < 1000; iteration++)
            {
                Fuzz_Iteration(iteration, random);
            }
        }

        private void Fuzz_Iteration(int iteration, Random random)
        {
            var depth = 5; // adjust as needed for a simpler reproducer
            var hintNameProvider = new HintNameProvider(nextHintNameId: 0);
            var rootOperator = makeOperatorTree(new LeafOperator() { HintNameProvider = hintNameProvider }, depth);

            var source = "";
            var comp = CreateCompilation(source);
            var generators = new[] { new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(registerPipeline)) };

            // original input
            var originalInputsLength = 1 + random.Next(4); // adjust as needed for simpler repro
            var originalInputs = new List<InMemoryAdditionalText>(originalInputsLength);
            for (var i = 0; i < originalInputsLength; i++)
            {
                originalInputs.Add(new InMemoryAdditionalText(hintNameProvider.GetNextHintName(), getRandomLetter()));
            }

            // make edits to original input
            var editedInputs = new List<InMemoryAdditionalText>();
            for (var i = 0; i < originalInputsLength; i++)
            {
                var changeKind = (InputChangeKind)random.Next((int)InputChangeKind.MaxValue);
                switch (changeKind)
                {
                    case InputChangeKind.Cached:
                        editedInputs.Add(originalInputs[i]);
                        break;
                    case InputChangeKind.Modified:
                        editedInputs.Add(new InMemoryAdditionalText(originalInputs[i].Path, getRandomLetter()));
                        break;
                    case InputChangeKind.Deleted:
                        continue;
                }
            }

            // add some new documents in random positions
            var newDocumentsCount = random.Next(3); // adjust as needed for simpler repro
            for (int i = 0; i < newDocumentsCount; i++)
            {
                editedInputs.Insert(random.Next(editedInputs.Count), new InMemoryAdditionalText(hintNameProvider.GetNextHintName(), getRandomLetter()));
            }

            // Uncomment to printf-debug output of the reproducer test itself.
            // writeReproducerTest();

            try
            {
                // run from scratch on original
                GeneratorDriver driver1 = CSharpGeneratorDriver.Create(generators, originalInputs);
                driver1.RunGenerators(comp);

                // incremental update from edited input
                driver1 = driver1.ReplaceAdditionalTexts(editedInputs.ToImmutableArray<AdditionalText>());
                hintNameProvider.Reset(originalInputsLength + newDocumentsCount);
                driver1 = driver1.RunGenerators(comp);
                var result1 = driver1.GetRunResult();

                // run from scratch on edited
                GeneratorDriver driver2 = CSharpGeneratorDriver.Create(generators, editedInputs);
                hintNameProvider.Reset(originalInputsLength + newDocumentsCount);
                driver2 = driver2.RunGenerators(comp);
                var result2 = driver2.GetRunResult();

                Assert.Equal(result2.GeneratedTrees, result1.GeneratedTrees, SyntaxTreeComparer.Instance);
                Assert.Equal(result2.Diagnostics, result1.Diagnostics, CommonDiagnosticComparer.Instance);
            }
            catch
            {
                writeReproducerTest();
                throw;
            }

            string getRandomLetter()
            {
                return random.Next(4) switch
                {
                    0 => "a",
                    1 => "b",
                    2 => "c",
                    3 => "d",
                    var num => throw ExceptionUtilities.UnexpectedValue(num)
                };
            }

            void registerPipeline(IncrementalGeneratorInitializationContext context)
            {
                // generate a random tree of operations
                var provider = context.AdditionalTextsProvider;
                var finalProvider = rootOperator.Apply(provider);

                context.RegisterSourceOutput(finalProvider, (context, text) =>
                {
                    context.AddSource(((InMemoryAdditionalText)text).Path, ((InMemoryAdditionalText)text).GetText().ToString());
                });
            }

            Operator makeOperatorTree(Operator @operator, int depth)
            {
                if (depth == 0)
                {
                    return @operator;
                }

                switch ((OperatorKind)random.Next((int)OperatorKind.MaxValue))
                {
                    case OperatorKind.Select:
                        bool transformAs = random.Next(2) == 0 ? true : false;
                        bool transformCs = random.Next(2) == 0 ? true : false;
                        var operator1 = new SelectOperator(@operator, transformAs, transformCs) { HintNameProvider = hintNameProvider };
                        return makeOperatorTree(operator1, random.Next(depth));

                    case OperatorKind.SelectMany:
                        // generate a random number of Select-like transformations
                        // yield a transformed version of the document for each
                        var count = random.Next(depth);
                        var sources = ArrayBuilder<(bool TransformAs, bool TransformBs)>.GetInstance(count);
                        for (int i = 0; i < count; i++)
                        {
                            sources.Add((
                                TransformAs: random.Next(2) == 0 ? true : false,
                                TransformBs: random.Next(2) == 0 ? true : false
                                ));
                        }
                        var operator2 = new SelectManyOperator(@operator, sources.ToImmutableAndFree()) { HintNameProvider = hintNameProvider };
                        return makeOperatorTree(operator2, random.Next(depth));

                    case OperatorKind.Where:
                        bool includeAs = random.Next(2) == 0 ? true : false;
                        bool includeBs = random.Next(2) == 0 ? true : false;
                        bool includeCs = random.Next(2) == 0 ? true : false;
                        bool includeDs = random.Next(2) == 0 ? true : false;
                        var operator3 = new WhereOperator(@operator, includeAs, includeBs, includeCs, includeDs) { HintNameProvider = hintNameProvider };
                        return makeOperatorTree(operator3, random.Next(depth));

                    case OperatorKind.Combine:
                        // Forks off two subtrees from 'provider' and joins them.
                        //     .
                        //    / \
                        //    | |
                        //    \ /
                        //     .
                        var operator4_1 = makeOperatorTree(@operator, random.Next(depth));
                        var operator4_2 = makeOperatorTree(@operator, random.Next(depth));
                        var operator4 = new CombineOperator(operator4_1, operator4_2) { HintNameProvider = hintNameProvider };
                        return makeOperatorTree(operator4, random.Next(depth));
                }

                throw ExceptionUtilities.Unreachable();
            }

            void writeReproducerTest()
            {
                var builder = new StringBuilder();
                builder.AppendLine($$"""
                    [Fact]
                    public void Fuzz_{{iteration}}()
                    {
                        var source = "";
                        var comp = CreateCompilation(source);
                        var hintNameProvider = new HintNameProvider({{originalInputsLength + newDocumentsCount}});
                        var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(registerPipeline));

                        // original input
                        var originalInputs = new[]
                        {
                """);

                foreach (var text in originalInputs)
                {
                    builder.AppendLine($"""
                            new InMemoryAdditionalText("{text.Path}", "{text.GetText()}"),
                """);
                }

                builder.AppendLine($$"""
                        };

                        // from scratch on original
                        GeneratorDriver driver1 = CSharpGeneratorDriver.Create(new[] { generator }, originalInputs);
                        driver1.RunGenerators(comp);

                        // make edits to original input
                        var editedInputs = ImmutableArray.Create(new AdditionalText[]
                        {
                """);

                foreach (var text in editedInputs)
                {
                    builder.AppendLine($"""
                            new InMemoryAdditionalText("{text.Path}", "{text.GetText()}"),
                """);
                }

                builder.AppendLine($$"""
                        });

                        // incremental update from edited input
                        driver1 = driver1.ReplaceAdditionalTexts(editedInputs);
                        hintNameProvider.Reset({{originalInputsLength + newDocumentsCount}});
                        driver1 = driver1.RunGenerators(comp);
                        var result1 = driver1.GetRunResult();

                        // from scratch on edited
                        GeneratorDriver driver2 = CSharpGeneratorDriver.Create(new[] { generator }, editedInputs);
                        hintNameProvider.Reset({{originalInputsLength + newDocumentsCount}});
                        driver2 = driver2.RunGenerators(comp);
                        var result2 = driver2.GetRunResult();

                        Assert.Equal(result2.GeneratedTrees, result1.GeneratedTrees, SyntaxTreeComparer.Instance);
                        Assert.Equal(result2.Diagnostics, result1.Diagnostics, CommonDiagnosticComparer.Instance);
                """);

                builder.AppendLine($$"""

                        void registerPipeline(IncrementalGeneratorInitializationContext context)
                        {
                """);

                builder.Append("""
                            var provider = 
                """);
                rootOperator.AppendTo(builder);
                builder.AppendLine("""
                            ;
                """);

                builder.Append("""
                            context.RegisterSourceOutput(provider, (context, text) =>
                            {
                                context.AddSource(text.Path, text.GetText()!.ToString());
                            });
                        }
                    }
                """);

                _output.WriteLine(builder.ToString());
            }
        }

        [Fact]
        public void Fuzz_5()
        {
            var source = "";
            var comp = CreateCompilation(source);
            var hintNameProvider = new HintNameProvider(4);
            var generator = new IncrementalGeneratorWrapper(new PipelineCallbackGenerator(registerPipeline));

            // original input
            var originalInputs = new[]
            {
                new InMemoryAdditionalText("0", "c"),
                new InMemoryAdditionalText("1", "a"),
                new InMemoryAdditionalText("2", "d"),
            };

            // from scratch on original

            GeneratorDriver driver1 = CSharpGeneratorDriver.Create(new[] { generator }, originalInputs);
            hintNameProvider = new HintNameProvider(4);
            driver1.RunGenerators(comp);

            // make edits to original input
            var editedInputs = ImmutableArray.Create(new AdditionalText[]
            {
                new InMemoryAdditionalText("3", "c"),
                new InMemoryAdditionalText("0", "c"),
                new InMemoryAdditionalText("1", "b"),
                new InMemoryAdditionalText("2", "b"),
            });

            // incremental update from edited input
            hintNameProvider = new HintNameProvider(4);
            driver1 = driver1.ReplaceAdditionalTexts(editedInputs);
            driver1 = driver1.RunGenerators(comp);
            var result1 = driver1.GetRunResult();

            // from scratch on edited
            GeneratorDriver driver2 = CSharpGeneratorDriver.Create(new[] { generator }, editedInputs);
            hintNameProvider = new HintNameProvider(4);
            driver2 = driver2.RunGenerators(comp);
            var result2 = driver2.GetRunResult();

            Assert.Equal(result2.GeneratedTrees, result1.GeneratedTrees, SyntaxTreeComparer.Instance);
            Assert.Equal(result2.Diagnostics, result1.Diagnostics, CommonDiagnosticComparer.Instance);

            void registerPipeline(IncrementalGeneratorInitializationContext context)
            {
                var provider = context.AdditionalTextsProvider
                    .SelectMany((additionalText, _) => new (bool TransformAs, bool TransformCs)[] {
                    (false, false),
                    }.Select(logic => (AdditionalText)new InMemoryAdditionalText(hintNameProvider.GetNextHintName(), additionalText.GetText()!.ToString() switch
                    {
                        "a" when logic.TransformAs => "b",
                        "c" when logic.TransformCs => "d",
                        var other => other
                    })))
                ;
                context.RegisterSourceOutput(provider, (context, text) =>
                {
                    context.AddSource(text.Path, text.GetText()!.ToString());
                });
            }
        }
    }
}
