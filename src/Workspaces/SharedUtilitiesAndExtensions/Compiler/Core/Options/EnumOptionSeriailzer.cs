// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    internal class EnumOptionSeriailzer<T> : IEditorConfigValueSerializer where T : struct, Enum
    {
        public string Serialize(object? value)
        {
            Contract.ThrowIfNull(value);
            return ((T)value).ToString();
        }

        public bool TryParse(string value, out object? result)
        {
            if (Enum.TryParse<T>(value, out var parsedValue))
            {
                result = parsedValue;
                return true;
            }

            result = null;
            return false;
        }
    }
}
