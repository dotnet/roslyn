// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A source text created by an <see cref="ISourceGenerator"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public readonly struct GeneratedSourceText
    {
        public SourceText Text { get; }

        public string HintName { get; }

        public GeneratedSourceText(string hintName, SourceText text)
        {
            this.Text = text;
            this.HintName = hintName;
        }
    }
}
