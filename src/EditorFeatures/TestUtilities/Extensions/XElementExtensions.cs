// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
{
    public static class XElementExtensions
    {
        public static string NormalizedValue(this XElement element)
        {
            return element.Value.Replace("\n", "\r\n");
        }
    }
}
