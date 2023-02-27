// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    internal interface IEditorConfigValueSerializer
    {
        bool TryParse(string value, out object? result);

        /// <summary>
        /// Gets the editorconfig string representation for the specified <paramref name="value"/>. 
        /// </summary>
        string Serialize(object? value);
    }
}
