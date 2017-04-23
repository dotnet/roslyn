// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal class SerializableDefinitionItem
    {
        public DefinitionItem Rehydrate(Workspace workspace)
        {
            throw new NotImplementedException();
        }

        internal static SerializableDefinitionItem Dehydrate(DefinitionItem definition)
        {
            throw new NotImplementedException();
        }
    }
}