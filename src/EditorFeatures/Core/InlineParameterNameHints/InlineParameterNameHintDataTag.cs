// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.InlineParameterNameHints
{
    /// <summary>
    /// The simple tag that only holds information regarding the associated parameter name
    /// for the argument
    /// </summary>
    internal class InlineParameterNameHintDataTag : ITag
    {
        public readonly string ParameterName;

        public InlineParameterNameHintDataTag(string parameterName)
        {
            if (parameterName.Length == 0)
            {
                throw new ArgumentException("Must have a length greater than 0", nameof(parameterName));
            }

            ParameterName = parameterName;
        }
    }
}
