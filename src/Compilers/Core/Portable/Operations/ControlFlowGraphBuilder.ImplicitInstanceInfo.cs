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
            /// Holds the capture Ids for initialized anonymous type properties in an anonymous object initializer.
            /// </summary>
            public PooledDictionary<IPropertySymbol, int> AnonymousTypePropertyCaptureIds { get; }

            public ImplicitInstanceInfo(IOperation currentImplicitInstance)
            {
                Debug.Assert(currentImplicitInstance != null);
                ImplicitInstance = currentImplicitInstance;
                AnonymousType = null;
                AnonymousTypePropertyCaptureIds = null;
            }

            public ImplicitInstanceInfo(INamedTypeSymbol currentInitializedAnonymousType)
            {
                Debug.Assert(currentInitializedAnonymousType.IsAnonymousType);

                ImplicitInstance = null;
                AnonymousType = currentInitializedAnonymousType;
                AnonymousTypePropertyCaptureIds = PooledDictionary<IPropertySymbol, int>.GetInstance();
            }

            public ImplicitInstanceInfo(in Context context)
            {
                Debug.Assert(context.ImplicitInstance == null || context.AnonymousType == null);

                if (context.ImplicitInstance != null)
                {
                    ImplicitInstance = context.ImplicitInstance;
                    AnonymousType = null;
                    AnonymousTypePropertyCaptureIds = null;
                }
                else if (context.AnonymousType != null)
                {
                    ImplicitInstance = null;
                    AnonymousType = context.AnonymousType;
                    AnonymousTypePropertyCaptureIds = PooledDictionary<IPropertySymbol, int>.GetInstance();

                    foreach (KeyValuePair<IPropertySymbol, int> pair in context.AnonymousTypePropertyCaptureIds)
                    {
                        AnonymousTypePropertyCaptureIds.Add(pair.Key, pair.Value);
                    }
                }
                else
                {
                    ImplicitInstance = null;
                    AnonymousType = null;
                    AnonymousTypePropertyCaptureIds = null;
                }
            }

            public void Free()
            {
                AnonymousTypePropertyCaptureIds?.Free();
            }
        }
    }
}
