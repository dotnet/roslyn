// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal partial class ControlFlowGraphBuilder
    {
        /// <summary>
        /// Holds the current object being initialized if we're visiting an object initializer.
        /// Or the current anonymous type object being initialized if we're visiting an anonymous type object initializer.
        /// Or the target of a VB With statement.
        /// </summary>
        private struct ImplicitInstanceInfo
        {
            /// <summary>
            /// Holds the current object instance being initialized if we're visiting an object initializer.
            /// </summary>
            public IOperation ImplicitInstance { get; }

            /// <summary>
            /// Holds the current anonymous type instance being initialized if we're visiting an anonymous object initializer.
            /// </summary>
            public INamedTypeSymbol AnonymousType { get; }

            /// <summary>
            /// Holds the captured values for initialized anonymous type properties in an anonymous object initializer.
            /// </summary>
            public PooledDictionary<IPropertySymbol, IOperation> AnonymousTypePropertyValues { get; }

            public ImplicitInstanceInfo(IOperation currentImplicitInstance)
            {
                Debug.Assert(currentImplicitInstance != null);
                ImplicitInstance = currentImplicitInstance;
                AnonymousType = null;
                AnonymousTypePropertyValues = null;
            }

            public ImplicitInstanceInfo(INamedTypeSymbol currentInitializedAnonymousType)
            {
                Debug.Assert(currentInitializedAnonymousType.IsAnonymousType);

                ImplicitInstance = null;
                AnonymousType = currentInitializedAnonymousType;
                AnonymousTypePropertyValues = PooledDictionary<IPropertySymbol, IOperation>.GetInstance();
            }

            public ImplicitInstanceInfo(in Context context)
            {
                Debug.Assert(context.ImplicitInstance == null || context.AnonymousType == null);

                if (context.ImplicitInstance != null)
                {
                    ImplicitInstance = context.ImplicitInstance;
                    AnonymousType = null;
                    AnonymousTypePropertyValues = null;
                }
                else if (context.AnonymousType != null)
                {
                    ImplicitInstance = null;
                    AnonymousType = context.AnonymousType;
                    AnonymousTypePropertyValues = PooledDictionary<IPropertySymbol, IOperation>.GetInstance();

                    foreach (KeyValuePair<IPropertySymbol, IOperation> pair in context.AnonymousTypePropertyValues)
                    {
                        AnonymousTypePropertyValues.Add(pair.Key, pair.Value);
                    }
                }
                else
                {
                    ImplicitInstance = null;
                    AnonymousType = null;
                    AnonymousTypePropertyValues = null;
                }
            }

            public void Free()
            {
                AnonymousTypePropertyValues?.Free();
            }
        }
    }
}
