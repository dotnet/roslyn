// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    public class ScriptOptionsTests : TestBase
    {
        [Fact]
        public void AddReferences()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(int).GetTypeInfo().Assembly).
                AddReferences(typeof(int).GetTypeInfo().Assembly).
                AddReferences(MetadataReference.CreateFromAssemblyInternal(typeof(int).GetTypeInfo().Assembly)).
                AddReferences("System.Linq").
                AddReferences("System.Linq");

            Assert.Equal(5, options.MetadataReferences.Length);
        }

        [Fact]
        public void AddReferences_Errors()
        {
            var moduleRef = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference();

            var options = ScriptOptions.Default;
            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((MetadataReference[])null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences(new MetadataReference[] { null }));

            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((IEnumerable<MetadataReference>)null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences((IEnumerable<MetadataReference>)new MetadataReference[] { null }));

            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((Assembly[])null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences(new Assembly[] { null }));

            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((IEnumerable<Assembly>)null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences((IEnumerable<Assembly>)new Assembly[] { null }));

            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((string[])null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences(new string[] { null }));

            AssertEx.ThrowsArgumentNull("references", () => options.AddReferences((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.AddReferences((IEnumerable<string>)new string[] { null }));

            AssertEx.ThrowsArgumentException("references", () => options.AddReferences(moduleRef));
        }

        [Fact]
        public void WithReferences()
        {
            var options = ScriptOptions.Default.WithReferences("System.Linq", "system.linq");
            Assert.Equal(2, options.MetadataReferences.Length);

            options = ScriptOptions.Default.WithReferences(typeof(int).GetTypeInfo().Assembly, typeof(int).GetTypeInfo().Assembly);
            Assert.Equal(2, options.MetadataReferences.Length);

            var assemblyRef = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods).GetReference();

            options = ScriptOptions.Default.WithReferences(assemblyRef, assemblyRef);
            Assert.Equal(2, options.MetadataReferences.Length);
        }

        [Fact]
        public void WithReferences_Errors()
        {
            var moduleRef = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference();

            var options = ScriptOptions.Default;
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((MetadataReference[])null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((IEnumerable<MetadataReference>)null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences(default(ImmutableArray<MetadataReference>)));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(new MetadataReference[] { null }));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(ImmutableArray.Create((MetadataReference)null)));

            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((Assembly[])null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((IEnumerable<Assembly>)null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences(default(ImmutableArray<Assembly>)));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(new Assembly[] { null }));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(ImmutableArray.Create((Assembly)null)));

            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((string[])null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("references", () => options.WithReferences(default(ImmutableArray<string>)));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(new string[] { null }));
            AssertEx.ThrowsArgumentNull("references[0]", () => options.WithReferences(ImmutableArray.Create((string)null)));

            AssertEx.ThrowsArgumentException("references", () => options.WithReferences(moduleRef));
        }

        [Fact]
        public void AddNamespaces()
        {
            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            var options = ScriptOptions.Default.
                AddNamespaces("").
                AddNamespaces("blah.").
                AddNamespaces("b\0lah").
                AddNamespaces(".blah").
                AddNamespaces("b\0lah").
                AddNamespaces(".blah");

            AssertEx.Equal(new[] { "", "blah.", "b\0lah", ".blah", "b\0lah", ".blah" }, options.Namespaces);
        }

        [Fact]
        public void AddNamespaces_Errors()
        {
            var options = ScriptOptions.Default;
           
            AssertEx.ThrowsArgumentNull("namespaces", () => options.AddNamespaces((string[])null));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.AddNamespaces(new string[] { null } ));

            AssertEx.ThrowsArgumentNull("namespaces", () => options.AddNamespaces((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.AddNamespaces((IEnumerable<string>)new string[] { null }));

            AssertEx.ThrowsArgumentNull("namespaces", () => options.AddNamespaces(default(ImmutableArray<string>)));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.AddNamespaces(ImmutableArray.Create((string)null)));

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            options.AddNamespaces("");
            options.AddNamespaces("blah.");
            options.AddNamespaces("b\0lah");
            options.AddNamespaces(".blah");
        }

        [Fact]
        public void WithNamespaces_Errors()
        {
            var options = ScriptOptions.Default;

            AssertEx.ThrowsArgumentNull("namespaces", () => options.WithNamespaces((string[])null));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.WithNamespaces(new string[] { null }));

            AssertEx.ThrowsArgumentNull("namespaces", () => options.WithNamespaces((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.WithNamespaces((IEnumerable<string>)new string[] { null }));

            AssertEx.ThrowsArgumentNull("namespaces", () => options.WithNamespaces(default(ImmutableArray<string>)));
            AssertEx.ThrowsArgumentNull("namespaces[0]", () => options.WithNamespaces(ImmutableArray.Create((string)null)));

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            options.WithNamespaces("");
            options.WithNamespaces("blah.");
            options.WithNamespaces("b\0lah");
            options.WithNamespaces(".blah");
        }

#if TODO // provide simple resolver APIs
        [Fact]
        public void AddSearchPaths()
        {
            var options = ScriptOptions.Default;

            AssertEx.ThrowsArgumentNull("searchPaths", () => options.AddSearchPaths((string[])null));
            AssertEx.ThrowsArgumentNull("searchPaths[0]", () => options.AddSearchPaths(new string[] { null }));

            AssertEx.ThrowsArgumentNull("searchPaths", () => options.AddSearchPaths((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("searchPaths[0]", () => options.AddSearchPaths((IEnumerable<string>)new string[] { null }));
        }

        [Fact]
        public void WithSearchPaths()
        {
            var options = ScriptOptions.Default;

            AssertEx.ThrowsArgumentNull("searchPaths", () => options.WithSearchPaths((string[])null));
            AssertEx.ThrowsArgumentNull("searchPaths[0]", () => options.WithSearchPaths(new string[] { null }));

            AssertEx.ThrowsArgumentNull("searchPaths", () => options.WithSearchPaths((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("searchPaths[0]", () => options.WithSearchPaths((IEnumerable<string>)new string[] { null }));
        }
#endif
    }
}
