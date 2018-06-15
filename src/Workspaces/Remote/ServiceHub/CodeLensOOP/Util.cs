// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.CodeAnalysis.Remote.CodeLensOOP
{
    /// <summary>
    /// ported from original reference code lens provider. keep exact same behavior
    /// </summary>
    internal static class Util
    {
        public static string GetCodeElementKindsString(CodeElementKinds kind)
        {
            switch (kind)
            {
                case CodeElementKinds.Method:
                    return ServiceHubResources.method;

                case CodeElementKinds.Type:
                    return ServiceHubResources.type;

                case CodeElementKinds.Property:
                    return ServiceHubResources.property;

                default:
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Unsupported type {0}", kind), nameof(kind));
            }
        }
    }
}
