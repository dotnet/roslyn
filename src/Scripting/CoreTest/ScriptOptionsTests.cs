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
        }

        [Fact]
        public void AddNamespaces()
        {
            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            var options = ScriptOptions.Default.
                AddImports("").
                AddImports("blah.").
                AddImports("b\0lah").
                AddImports(".blah").
                AddImports("b\0lah").
                AddImports(".blah");

            AssertEx.Equal(new[] { "", "blah.", "b\0lah", ".blah", "b\0lah", ".blah" }, options.Imports);
        }

        [Fact]
        public void AddImports_Errors()
        {
            var options = ScriptOptions.Default;
           
            AssertEx.ThrowsArgumentNull("imports", () => options.AddImports((string[])null));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.AddImports(new string[] { null } ));

            AssertEx.ThrowsArgumentNull("imports", () => options.AddImports((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.AddImports((IEnumerable<string>)new string[] { null }));

            AssertEx.ThrowsArgumentNull("imports", () => options.AddImports(default(ImmutableArray<string>)));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.AddImports(ImmutableArray.Create((string)null)));

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            options.AddImports("");
            options.AddImports("blah.");
            options.AddImports("b\0lah");
            options.AddImports(".blah");
        }

        [Fact]
        public void WithImports_Errors()
        {
            var options = ScriptOptions.Default;

            AssertEx.ThrowsArgumentNull("imports", () => options.WithImports((string[])null));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.WithImports(new string[] { null }));

            AssertEx.ThrowsArgumentNull("imports", () => options.WithImports((IEnumerable<string>)null));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.WithImports((IEnumerable<string>)new string[] { null }));

            AssertEx.ThrowsArgumentNull("imports", () => options.WithImports(default(ImmutableArray<string>)));
            AssertEx.ThrowsArgumentNull("imports[0]", () => options.WithImports(ImmutableArray.Create((string)null)));

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            options.WithImports("");
            options.WithImports("blah.");
            options.WithImports("b\0lah");
            options.WithImports(".blah");
        }
    }
}
