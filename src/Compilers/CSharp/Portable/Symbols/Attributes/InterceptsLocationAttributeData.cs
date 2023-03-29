// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    /// <summary>
    /// Information decoded from InterceptsLocationAttribute.
    /// </summary>
    /// <param name="Line">The 0-indexed line number.</param>
    /// <param name="Character">The 0-indexed character number.</param>
    // PROTOTYPE(ic): move away from records
    internal sealed record InterceptsLocationAttributeData(string FilePath, int Line, int Character, Location AttributeLocation);
}
