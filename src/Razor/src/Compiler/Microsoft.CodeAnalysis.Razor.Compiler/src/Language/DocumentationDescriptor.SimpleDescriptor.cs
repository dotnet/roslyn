// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language;

internal abstract partial class DocumentationDescriptor
{
    private sealed class SimpleDescriptor : DocumentationDescriptor
    {
        public override object?[] Args => Array.Empty<object>();

        public SimpleDescriptor(DocumentationId id)
            : base(id)
        {
        }

        public override string GetText()
            => GetDocumentationText();

        public override bool Equals(DocumentationDescriptor? other)
            => other is SimpleDescriptor { Id: var id } && Id == id;

        protected override int ComputeHashCode()
            => Id.GetHashCode();
    }
}
