using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Xunit;

namespace RoslynEx.Tests
{
    public class DependencyResolverTests
    {
        private static readonly T1 T1 = new T1();
        private static readonly T2 T2 = new T2();
        private static readonly T3 T3 = new T3();

        [Fact]
        public void SimpleSort()
        {
            var transformers = ImmutableArray.CreateBuilder<ISourceTransformer>();
            transformers.AddRange(T2, T1, T3);

            var diagnostics = new List<DiagnosticInfo>();

            TransformerDependencyResolver.Sort(
                ref transformers, new[] { new[] { typeof(T1).FullName, typeof(T2).FullName, typeof(T3).FullName }.ToImmutableArray() },
                diagnostics);

            Assert.Equal(transformers.ToArray(), new ISourceTransformer[] { T1, T2, T3 });
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void NotFound()
        {
            var transformers = ImmutableArray.CreateBuilder<ISourceTransformer>();
            transformers.AddRange(T1);

            var diagnostics = new List<DiagnosticInfo>();

            TransformerDependencyResolver.Sort(
                ref transformers, new[] { new[] { typeof(T1).FullName, typeof(T2).FullName }.ToImmutableArray() }, diagnostics);

            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(RoslynExMessageProvider.ERR_TransformerNotFound, diagnostics[0].Code);
        }

        [Fact]
        public void Cycle()
        {
            var transformers = ImmutableArray.CreateBuilder<ISourceTransformer>();
            transformers.AddRange(T2, T1, T3);

            var diagnostics = new List<DiagnosticInfo>();

            TransformerDependencyResolver.Sort(
                ref transformers,
                new[] { 
                    new[] { typeof(T1).FullName, typeof(T2).FullName, typeof(T3).FullName }.ToImmutableArray(),
                    new[] { typeof(T3).FullName, typeof(T1).FullName }.ToImmutableArray()
                },
                diagnostics);

            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(RoslynExMessageProvider.ERR_TransformerCycleFound, diagnostics[0].Code);
        }

        [Fact]
        public void NotOrdered()
        {
            var transformers = ImmutableArray.CreateBuilder<ISourceTransformer>();
            transformers.AddRange(T2, T1, T3);

            var diagnostics = new List<DiagnosticInfo>();

            TransformerDependencyResolver.Sort(
                ref transformers, new[] { new[] { typeof(T1).FullName, typeof(T2).FullName }.ToImmutableArray() }, diagnostics);

            Assert.Equal(1, diagnostics.Count);
            Assert.Equal(RoslynExMessageProvider.ERR_TransformersNotOrdered, diagnostics[0].Code);
        }
    }

    class TransformerBase : ISourceTransformer
    {
        public Compilation Execute(TransformerContext context) => throw new NotImplementedException();
    }

    class T1 : TransformerBase { }
    class T2 : TransformerBase { }
    class T3 : TransformerBase { }
}
