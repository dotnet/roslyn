// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class DictionaryAnalyzerConfigOptionsTests
    {
        [Fact]
        public void BackwardCompatibility()
        {
            // Note: Older versions of analyzers access this field via reflection.
            // https://github.com/dotnet/roslyn/blob/8e3d62a30b833631baaa4e84c5892298f16a8c9e/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Options/EditorConfig/EditorConfigStorageLocationExtensions.cs#L21
            Assert.Equal(
                typeof(ImmutableDictionary<string, string>),
                typeof(DictionaryAnalyzerConfigOptions).GetField("Options", BindingFlags.Instance | BindingFlags.NonPublic)?.FieldType);
        }
    }
}
