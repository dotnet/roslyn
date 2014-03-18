// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Composition
{
    /// <summary>
    /// Represents access to a set of lazily constructed object instances, grouped by type.
    /// </summary>
    internal abstract class ExportSource
    {
        public abstract IEnumerable<Lazy<T>> GetExports<T>() where T : class;
        public abstract IEnumerable<Lazy<T, TMetadata>> GetExports<T, TMetadata>() where T : class;
    }
}