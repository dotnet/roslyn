// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting.Hosting;
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

            Assert.Equal(GacFileResolver.IsAvailable ? 5 : 30, options.MetadataReferences.Length);
        }

        [Fact]
        public void AddReferences_Errors()
        {
            var moduleRef = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference();

            var options = ScriptOptions.Default.WithReferences(ImmutableArray<MetadataReference>.Empty);
            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((MetadataReference[])null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences(new MetadataReference[] { null }));

            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((IEnumerable<MetadataReference>)null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences((IEnumerable<MetadataReference>)new MetadataReference[] { null }));

            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((Assembly[])null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences(new Assembly[] { null }));

            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((IEnumerable<Assembly>)null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences((IEnumerable<Assembly>)new Assembly[] { null }));

            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((string[])null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences(new string[] { null }));

            Assert.Throws<ArgumentNullException>("references", () => options.AddReferences((IEnumerable<string>)null));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.AddReferences((IEnumerable<string>)new string[] { null }));
        }

        [Fact]
        public void WithReferences()
        {
            var empty = ScriptOptions.Default.WithReferences(ImmutableArray<MetadataReference>.Empty);

            var options = empty.WithReferences("System.Linq", "system.linq");
            Assert.Equal(2, options.MetadataReferences.Length);

            options = empty.WithReferences(typeof(int).GetTypeInfo().Assembly, typeof(int).GetTypeInfo().Assembly);
            Assert.Equal(2, options.MetadataReferences.Length);

            var assemblyRef = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods).GetReference();

            options = empty.WithReferences(assemblyRef, assemblyRef);
            Assert.Equal(2, options.MetadataReferences.Length);
        }

        [Fact]
        public void WithReferences_Errors()
        {
            var moduleRef = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference();

            var options = ScriptOptions.Default.WithReferences(ImmutableArray<MetadataReference>.Empty);
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((MetadataReference[])null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((IEnumerable<MetadataReference>)null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences(default(ImmutableArray<MetadataReference>)));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(new MetadataReference[] { null }));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(ImmutableArray.Create((MetadataReference)null)));

            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((Assembly[])null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((IEnumerable<Assembly>)null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences(default(ImmutableArray<Assembly>)));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(new Assembly[] { null }));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(ImmutableArray.Create((Assembly)null)));

            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((string[])null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences((IEnumerable<string>)null));
            Assert.Throws<ArgumentNullException>("references", () => options.WithReferences(default(ImmutableArray<string>)));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(new string[] { null }));
            Assert.Throws<ArgumentNullException>("references[0]", () => options.WithReferences(ImmutableArray.Create((string)null)));
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

            Assert.Throws<ArgumentNullException>("imports", () => options.AddImports((string[])null));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.AddImports(new string[] { null }));

            Assert.Throws<ArgumentNullException>("imports", () => options.AddImports((IEnumerable<string>)null));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.AddImports((IEnumerable<string>)new string[] { null }));

            Assert.Throws<ArgumentNullException>("imports", () => options.AddImports(default(ImmutableArray<string>)));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.AddImports(ImmutableArray.Create((string)null)));

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

            Assert.Throws<ArgumentNullException>("imports", () => options.WithImports((string[])null));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.WithImports(new string[] { null }));

            Assert.Throws<ArgumentNullException>("imports", () => options.WithImports((IEnumerable<string>)null));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.WithImports((IEnumerable<string>)new string[] { null }));

            Assert.Throws<ArgumentNullException>("imports", () => options.WithImports(default(ImmutableArray<string>)));
            Assert.Throws<ArgumentNullException>("imports[0]", () => options.WithImports(ImmutableArray.Create((string)null)));

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            options.WithImports("");
            options.WithImports("blah.");
            options.WithImports("b\0lah");
            options.WithImports(".blah");
        }

        [Fact]
        public void WithEmitDebugInformation_SetsEmitDebugInformation()
        {
            Assert.True(ScriptOptions.Default.WithEmitDebugInformation(true).EmitDebugInformation);
            Assert.False(ScriptOptions.Default.WithEmitDebugInformation(false).EmitDebugInformation);
            Assert.False(ScriptOptions.Default.EmitDebugInformation);
        }

        [Fact]
        public void WithEmitDebugInformation_SameValueTwice_DoesNotCreateNewInstance()
        {
            var options = ScriptOptions.Default.WithEmitDebugInformation(true);
            Assert.Same(options, options.WithEmitDebugInformation(true));
        }

        [Fact]
        public void WithFileEncoding_SetsWithFileEncoding()
        {
            var options = ScriptOptions.Default.WithFileEncoding(Encoding.ASCII);
            Assert.Equal(Encoding.ASCII, options.FileEncoding);
        }

        [Fact]
        public void WithFileEncoding_SameValueTwice_DoesNotCreateNewInstance()
        {
            var options = ScriptOptions.Default.WithFileEncoding(Encoding.ASCII);
            Assert.Same(options, options.WithFileEncoding(Encoding.ASCII));
        }
    }
}
