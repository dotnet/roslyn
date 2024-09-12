// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle
{
    [EditorConfigGenerator(LanguageNames.CSharp), Shared]
    internal sealed class CSharpEditorConfigFileGenerator
        : IEditorConfigOptionsCollection
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorConfigFileGenerator()
        {
        }

        public IEnumerable<(string feature, ImmutableArray<IOption2> options)> GetOptions()
        {
            var builder = ArrayBuilder<(string, ImmutableArray<IOption2>)>.GetInstance();
            builder.Add((CSharpWorkspaceResources.CSharp_Coding_Conventions, CSharpCodeStyleOptions.AllOptions));
            builder.Add((CSharpWorkspaceResources.CSharp_Formatting_Rules, CSharpFormattingOptions2.AllOptions));
            return builder.ToArrayAndFree();
        }
    }
}
