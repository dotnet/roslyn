// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.Scripting.Test
{
    internal sealed class AssemblyLoader : Microsoft.CodeAnalysis.Scripting.AssemblyLoader
    {
        private readonly RuntimeAssemblyManager _manager;

        public AssemblyLoader(RuntimeAssemblyManager manager)
        {
            _manager = manager;
        }

        public override Assembly Load(AssemblyIdentity identity, string location = null)
        {
            return _manager.GetAssembly(identity.GetDisplayName(), reflectionOnly: false);
        }
    }
}
