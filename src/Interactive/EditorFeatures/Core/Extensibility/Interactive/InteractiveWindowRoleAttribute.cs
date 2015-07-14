// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Interactive
{
    // TODO: Make public and unify with Python Tools InteractiveWindowRoleAttribute.
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class InteractiveWindowRoleAttribute : Attribute
    {
        public InteractiveWindowRoleAttribute(string name)
        {
            if ((name != null) && name.Contains(","))
            {
                throw new ArgumentException($"{nameof(InteractiveWindowRoleAttribute)} name cannot contain commas. Apply multiple attributes if you want to support multiple roles.", nameof(name));
            }

            Name = name;
        }

        public string Name { get; }
    }
}
