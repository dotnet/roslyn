// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A source text created by an <see cref="ISourceGenerator"/>
    /// </summary>
    internal readonly struct GeneratedSourceText
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
