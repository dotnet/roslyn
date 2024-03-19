// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Utilities;

internal readonly struct ParameterName : IEquatable<ParameterName>
{
    /// <summary>
    /// The name the underlying naming system came up with based on the argument itself.
    /// This might be a name like "_value".  We pass this along because it can help
    /// later parts of the GenerateConstructor process when doing things like field hookup.
    /// </summary>
    public readonly string NameBasedOnArgument;

    /// <summary>
    /// The name we think should actually be used for this parameter.  This will include
    /// stripping the name of things like underscores.
    /// </summary>
    public readonly string BestNameForParameter;

    public ParameterName(string nameBasedOnArgument, bool isFixed)
    {
        NameBasedOnArgument = nameBasedOnArgument;

        if (isFixed)
        {
            // If the parameter name is fixed, we have to accept it as is.
            BestNameForParameter = NameBasedOnArgument;
        }
        else
        {
            // Otherwise, massage it a bit to be a more suitable match for
            // how people actually writing parameters.
            var trimmed = nameBasedOnArgument.TrimStart('_');
            BestNameForParameter = trimmed.Length > 0 ? trimmed.ToCamelCase() : nameBasedOnArgument;
        }
    }

    public ParameterName(string nameBasedOnArgument, bool isFixed, NamingRule parameterNamingRule)
    {
        NameBasedOnArgument = nameBasedOnArgument;

        if (isFixed)
        {
            // If the parameter name is fixed, we have to accept it as is.
            BestNameForParameter = NameBasedOnArgument;
        }
        else
        {
            // Otherwise, massage it a bit to be a more suitable match for
            // how people actually writing parameters.
            BestNameForParameter = parameterNamingRule.NamingStyle.MakeCompliant(nameBasedOnArgument).First();
        }
    }

    public override bool Equals(object obj)
        => Equals((ParameterName)obj);

    public bool Equals(ParameterName other)
        => NameBasedOnArgument.Equals(other.NameBasedOnArgument);

    public override int GetHashCode()
        => NameBasedOnArgument.GetHashCode();
}
