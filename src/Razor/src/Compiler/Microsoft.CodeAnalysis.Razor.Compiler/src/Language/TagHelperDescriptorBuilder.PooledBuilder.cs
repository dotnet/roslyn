// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;

namespace Microsoft.AspNetCore.Razor.Language;

public partial class TagHelperDescriptorBuilder
{
    public struct PooledBuilder : IDisposable
    {
        private readonly TagHelperDescriptorBuilder _builder;
        private bool _disposed;

        internal PooledBuilder(TagHelperDescriptorBuilder builder)
        {
            _builder = builder;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                s_pool.Return(_builder);
                _disposed = true;
            }
        }
    }
}
