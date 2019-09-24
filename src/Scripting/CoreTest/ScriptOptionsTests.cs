// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
        public void Imports_Are_AppliedTo_CompilationOption()
        {
            var scriptOptions = ScriptOptions.Default.WithImports(new[] { "System", "System.IO" });
            var compilation = CSharpScript.Create(string.Empty, scriptOptions).GetCompilation();
            Assert.Equal(scriptOptions.Imports, compilation.Options.GetImports());
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

        [Fact]
        public void WithAllowUnsafe_SetsAllowUnsafe()
        {
            Assert.True(ScriptOptions.Default.WithAllowUnsafe(true).AllowUnsafe);
            Assert.False(ScriptOptions.Default.WithAllowUnsafe(false).AllowUnsafe);
            Assert.True(ScriptOptions.Default.AllowUnsafe);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithAllowUnsafe_SameValueTwice_DoesNotCreateNewInstance(bool allowUnsafe)
        {
            var options = ScriptOptions.Default.WithAllowUnsafe(allowUnsafe);
            Assert.Same(options, options.WithAllowUnsafe(allowUnsafe));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AllowUnsafe_Is_AppliedTo_CompilationOption(bool allowUnsafe)
        {
            var scriptOptions = ScriptOptions.Default.WithAllowUnsafe(allowUnsafe);
            var compilation = (CSharpCompilation)CSharpScript.Create(string.Empty, scriptOptions).GetCompilation();
            Assert.Equal(scriptOptions.AllowUnsafe, compilation.Options.AllowUnsafe);
        }

        [Fact]
        public void WithCheckOverflow_SetsCheckOverflow()
        {
            Assert.True(ScriptOptions.Default.WithCheckOverflow(true).CheckOverflow);
            Assert.False(ScriptOptions.Default.WithCheckOverflow(false).CheckOverflow);
            Assert.False(ScriptOptions.Default.CheckOverflow);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WithCheckOverflow_SameValueTwice_DoesNotCreateNewInstance(bool checkOverflow)
        {
            var options = ScriptOptions.Default.WithCheckOverflow(checkOverflow);
            Assert.Same(options, options.WithCheckOverflow(checkOverflow));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CheckOverflow_Is_AppliedTo_CompilationOption(bool checkOverflow)
        {
            var scriptOptions = ScriptOptions.Default.WithCheckOverflow(checkOverflow);
            var compilation = CSharpScript.Create(string.Empty, scriptOptions).GetCompilation();
            Assert.Equal(scriptOptions.CheckOverflow, compilation.Options.CheckOverflow);
        }

        [Theory]
        [InlineData(OptimizationLevel.Debug)]
        [InlineData(OptimizationLevel.Release)]
        public void WithOptimizationLevel_SetsOptimizationLevel(OptimizationLevel optimizationLevel)
        {
            Assert.Equal(ScriptOptions.Default.WithOptimizationLevel(optimizationLevel).OptimizationLevel, optimizationLevel);
            Assert.Equal(OptimizationLevel.Debug, ScriptOptions.Default.OptimizationLevel);
        }

        [Theory]
        [InlineData(OptimizationLevel.Debug)]
        [InlineData(OptimizationLevel.Release)]
        public void WithOptimizationLevel_SameValueTwice_DoesNotCreateNewInstance(OptimizationLevel optimizationLevel)
        {
            var options = ScriptOptions.Default.WithOptimizationLevel(optimizationLevel);
            Assert.Same(options, options.WithOptimizationLevel(optimizationLevel));
        }

        [Theory]
        [InlineData(OptimizationLevel.Debug)]
        [InlineData(OptimizationLevel.Release)]
        public void OptimizationLevel_Is_AppliedTo_CompilationOption(OptimizationLevel optimizationLevel)
        {
            var scriptOptions = ScriptOptions.Default.WithOptimizationLevel(optimizationLevel);
            var compilation = CSharpScript.Create(string.Empty, scriptOptions).GetCompilation();
            Assert.Equal(scriptOptions.OptimizationLevel, compilation.Options.OptimizationLevel);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void WithWarningLevel_SetsWarningLevel(int warningLevel)
        {
            Assert.Equal(ScriptOptions.Default.WithWarningLevel(warningLevel).WarningLevel, warningLevel);
            Assert.Equal(4, ScriptOptions.Default.WarningLevel);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void WithWarningLevel_SameValueTwice_DoesNotCreateNewInstance(int warningLevel)
        {
            var options = ScriptOptions.Default.WithWarningLevel(warningLevel);
            Assert.Same(options, options.WithWarningLevel(warningLevel));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void WarningLevel_Is_AppliedTo_CompilationOption(int warningLevel)
        {
            var scriptOptions = ScriptOptions.Default.WithWarningLevel(warningLevel);
            var compilation = CSharpScript.Create(string.Empty, scriptOptions).GetCompilation();
            Assert.Equal(scriptOptions.WarningLevel, compilation.Options.WarningLevel);
        }
    }
}
