// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Analyzer.Utilities.Options
{
    public enum EnumValuesPrefixTrigger
    {
        // NOTE: Below fields names are used in the .editorconfig specification.
        //       Hence the names should *not* be modified, as that would be a breaking
        //       change for .editorconfig specification.
        AnyEnumValue,
        AllEnumValues,
        Heuristic,
    }
}
