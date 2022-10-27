// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests.Persistence;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TestCompositionTests
    {
        [Fact]
        public void FactoryReuse()
        {
            var composition1 = FeaturesTestCompositions.Features.AddParts(typeof(TestErrorReportingService), typeof(TestTemporaryStorageServiceFactory));
            var composition2 = FeaturesTestCompositions.Features.AddParts(typeof(TestTemporaryStorageServiceFactory), typeof(TestErrorReportingService));
            Assert.Same(composition1.ExportProviderFactory, composition2.ExportProviderFactory);
        }

        [Fact]
        public void Assemblies()
        {
            var assembly1 = typeof(Workspace).Assembly;
            var assembly2 = typeof(object).Assembly;

            var composition1 = TestComposition.Empty;
            var composition2 = composition1.AddAssemblies(assembly1);
            AssertEx.SetEqual(new[] { assembly1 }, composition2.Assemblies);
            Assert.Empty(composition2.RemoveAssemblies(assembly1).Assemblies);

            var composition3 = composition2.WithAssemblies(ImmutableHashSet.Create(assembly2));
            AssertEx.SetEqual(new[] { assembly2 }, composition3.Assemblies);
        }

        [Fact]
        public void Parts()
        {
            var type1 = typeof(int);
            var type2 = typeof(bool);

            var composition1 = TestComposition.Empty;
            var composition2 = composition1.AddParts(type1);
            var composition3 = composition2.RemoveParts(type1);

            AssertEx.SetEqual(new[] { type1 }, composition2.Parts);
            Assert.Empty(composition3.Parts);
            Assert.Empty(composition3.ExcludedPartTypes);

            var composition4 = composition2.WithParts(ImmutableHashSet.Create(type2));
            AssertEx.SetEqual(new[] { type2 }, composition4.Parts);
            Assert.Empty(composition3.ExcludedPartTypes);
        }

        [Fact]
        public void ExcludedPartTypes()
        {
            var type1 = typeof(int);
            var type2 = typeof(bool);

            var composition1 = TestComposition.Empty;
            var composition2 = composition1.AddExcludedPartTypes(type1);
            var composition3 = composition2.RemoveExcludedPartTypes(type1);

            AssertEx.SetEqual(new[] { type1 }, composition2.ExcludedPartTypes);
            Assert.Empty(composition3.Parts);

            Assert.Empty(composition3.ExcludedPartTypes);
            Assert.Empty(composition3.Parts);

            var composition4 = composition2.WithExcludedPartTypes(ImmutableHashSet.Create(type2));
            AssertEx.SetEqual(new[] { type2 }, composition4.ExcludedPartTypes);
            Assert.Empty(composition4.Parts);
        }

        [Fact]
        public void Composition()
        {
            var assembly1 = typeof(Workspace).Assembly;
            var assembly2 = typeof(object).Assembly;
            var type1 = typeof(int);
            var type2 = typeof(long);
            var excluded1 = typeof(bool);
            var excluded2 = typeof(byte);

            var composition1 = TestComposition.Empty.AddAssemblies(assembly1).AddParts(type1).AddExcludedPartTypes(excluded1);
            var composition2 = TestComposition.Empty.AddAssemblies(assembly2).AddParts(type1, type2).AddExcludedPartTypes(excluded2);
            var composition3 = composition1.Add(composition2);

            AssertEx.SetEqual(new[] { assembly1, assembly2 }, composition3.Assemblies);
            AssertEx.SetEqual(new[] { type1, type2 }, composition3.Parts);
            AssertEx.SetEqual(new[] { excluded1, excluded2 }, composition3.ExcludedPartTypes);

            var composition4 = composition3.Remove(composition1);

            AssertEx.SetEqual(new[] { assembly2 }, composition4.Assemblies);
            AssertEx.SetEqual(new[] { type2 }, composition4.Parts);
            AssertEx.SetEqual(new[] { excluded2 }, composition4.ExcludedPartTypes);
        }
    }
}
