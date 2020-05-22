// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// Collection of <see cref="InvocationMapper"/>s.
    /// </summary>
    internal class InvocationMapperCollection
    {
        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="invocationMappers">The <see cref="InvocationMapper"/>s for this collection.</param>
        /// <remarks>
        /// If a single method matches with multiple <see cref="InvocationMapper"/>s, the first one wins.
        /// </remarks>
        public InvocationMapperCollection(IEnumerable<InvocationMapper> invocationMappers)
        {
            foreach (InvocationMapper invocationMapper in invocationMappers)
            {
                if (!this.InvocationMappers.TryGetValue(invocationMapper.MethodMetadataName, out List<InvocationMapper> list))
                {
                    list = new List<InvocationMapper>();
                    this.InvocationMappers.Add(invocationMapper.MethodMetadataName, list);
                }

                list.Add(invocationMapper);
                this.RequiresValueContentAnalysis |= invocationMapper.RequiresValueContentAnalysis;
            }
        }

        /// <summary>
        /// Constructs.
        /// </summary>
        /// <param name="invocationMappers">The <see cref="InvocationMapper"/>s for this collection.</param>
        /// <remarks>
        /// If a single method matches with multiple <see cref="InvocationMapper"/>s, the first one wins.
        /// </remarks>
        public InvocationMapperCollection(params InvocationMapper[] invocationMappers)
            : this((IEnumerable<InvocationMapper>)invocationMappers)
        {
        }

        /// <summary>
        /// An empty collection, just so you don't have to new one up yourself.
        /// </summary>
        public static InvocationMapperCollection Empty { get; } = new InvocationMapperCollection();

        /// <summary>
        /// Indicates that at least one <see cref="InvocationMapper"/> requires ValueContentAnalysis.
        /// </summary>
        internal bool RequiresValueContentAnalysis { get; }

        /// <summary>
        /// Keys are method names, values are lists of InvocationMappers.
        /// The lists are linearly searched to find a matching method signature. First match wins.
        /// </summary>
        private readonly Dictionary<string, List<InvocationMapper>> InvocationMappers =
            new Dictionary<string, List<InvocationMapper>>(StringComparer.Ordinal);

        /// <summary>
        /// Tries to find the first matching <see cref="InvocationMapper"/>.
        /// </summary>
        /// <param name="method">Method symbol to find a matching <see cref="InvocationMapper"/> for.</param>
        /// <param name="invocationMapper">Matched <see cref="InvocationMapper"/> if found.</param>
        /// <returns>True if a matching <see cref="InvocationMapper"/> was found in this collection, false otherwise.</returns>
        internal bool TryGetInvocationMapper(
            IMethodSymbol method,
            [NotNullWhen(returnValue: true)] out InvocationMapper? invocationMapper)
        {
            invocationMapper = null;

            if (this.InvocationMappers.TryGetValue(method.MetadataName, out List<InvocationMapper> list))
            {
                foreach (InvocationMapper m in list)
                {
                    if (m.SignatureMatcher(method))
                    {
                        invocationMapper = m;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
