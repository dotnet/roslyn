// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig
{
    [EditorConfigGenerator(LanguageNames.CSharp), Shared]
    internal class CSharpEditorConfigFileGenerator
        : IEditorConfigOptionsCollection
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpEditorConfigFileGenerator()
        {
        }

        public ImmutableArray<(string feature, ImmutableArray<IOption2> options)> GetEditorConfigOptions()
        {
            var builder = ArrayBuilder<(string, ImmutableArray<IOption2>)>.GetInstance();
            builder.AddRange(EditorConfigFileGenerator.GetLanguageAgnosticEditorConfigOptions());
            builder.Add((WorkspacesResources.CSharp_Coding_Conventions, CSharpCodeStyleOptions.AllOptions));
            builder.Add((WorkspacesResources.CSharp_Formatting_Rules, CSharpFormattingOptions2.AllOptions));
            return builder.ToImmutableAndFree();
        }
    }
}
