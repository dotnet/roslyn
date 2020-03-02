﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.GenericParameter)]
    internal sealed class NonDefaultableAttribute : Attribute
    {
    }
}
