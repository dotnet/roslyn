// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Rebuild.UnitTests
{
    public sealed class DeterministicKeyBuilderTests
    {
        /// <summary>
        /// This check monitors the set of properties and fields on the various option types
        /// that contribute to the deterministic checksum of a <see cref="Compilation"/>. When
        /// any of these tests change that means the new property or field needs to be evaluated
        /// for inclusion into the checksum
        /// </summary>
        [Fact]
        public void VerifyUpToDate()
        {
            verifyCount<ParseOptions>(11);
            verifyCount<CSharpParseOptions>(10);
            verifyCount<VisualBasicParseOptions>(10);
            verifyCount<CompilationOptions>(62);
            verifyCount<CSharpCompilationOptions>(9);
            verifyCount<VisualBasicCompilationOptions>(22);

            static void verifyCount<T>(int expected)
            {
                var type = typeof(T);
                var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
                var fields = type.GetFields(flags);
                var properties = type.GetProperties(flags);
                var count = fields.Length + properties.Length;
                Assert.Equal(expected, count);
            }


        }

        [Fact]
        public void Simple()
        {
            var compilation = CSharpTestBase.CreateCompilation(
                @"System.Console.WriteLine(""Hello World"");",
                targetFramework: TargetFramework.NetCoreApp);

            var builder = new CSharpDeterministicKeyBuilder();
            builder.WriteCompilation(compilation);
            var key = builder.GetKey();
            Assert.Equal("", key);
        }
    }
}
