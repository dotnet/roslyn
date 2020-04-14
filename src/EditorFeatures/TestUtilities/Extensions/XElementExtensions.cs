// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class XElementExtensions
    {
        public static string NormalizedValue(this XElement element)
            => element.Value.Replace("\n", "\r\n");
    }
}
