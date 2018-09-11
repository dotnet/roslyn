// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used for annotating internal types that are used in other assemblies that have InternalsVisibleTo.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false, AllowMultiple = true)]
    internal class InternalsUsedInAttribute : Attribute
    {
        public InternalsUsedInAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}
