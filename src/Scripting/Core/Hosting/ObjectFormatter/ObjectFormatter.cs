// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Object pretty printer.
    /// </summary>
    public abstract class ObjectFormatter
    {
        public string FormatObject(object obj) => FormatObject(obj, new PrintOptions());

        public abstract string FormatObject(object obj, PrintOptions options);

        public abstract string FormatException(Exception e);
    }
}
