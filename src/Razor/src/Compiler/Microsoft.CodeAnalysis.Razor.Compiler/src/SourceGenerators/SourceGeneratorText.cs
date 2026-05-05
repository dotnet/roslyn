// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators
{
    public readonly record struct SourceGeneratorText(SourceText Text)
    {
        public bool Equals(SourceGeneratorText other)
        {
            if (ReferenceEquals(Text, other.Text))
            {
                return true;
            }

            return Text.ContentEquals(other.Text);
        }

        public override int GetHashCode()
            => Text.GetHashCode();
    }
}
