// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public sealed record PropertyMetadata() : MetadataObject(MetadataKind.Property)
{
    public static PropertyMetadata Default { get; } = new();

    public string? GloballyQualifiedTypeName { get; init; }
    public bool IsChildContent { get; init; }
    public bool IsEventCallback { get; init; }
    public bool IsDelegateSignature { get; init; }
    public bool IsDelegateWithAwaitableResult { get; init; }
    public bool IsGenericTyped { get; init; }
    public bool IsInitOnlyProperty { get; init; }

    internal override bool HasDefaultValue => Equals(Default);

    private protected override void BuildChecksum(in Checksum.Builder builder)
    {
        builder.Append(GloballyQualifiedTypeName);
        builder.Append(IsChildContent);
        builder.Append(IsEventCallback);
        builder.Append(IsDelegateSignature);
        builder.Append(IsDelegateWithAwaitableResult);
        builder.Append(IsGenericTyped);
        builder.Append(IsInitOnlyProperty);
    }

    public ref struct Builder
    {
        public string? GloballyQualifiedTypeName { get; set; }
        public bool IsChildContent { get; set; }
        public bool IsEventCallback { get; set; }
        public bool IsDelegateSignature { get; set; }
        public bool IsDelegateWithAwaitableResult { get; set; }
        public bool IsGenericTyped { get; set; }
        public bool IsInitOnlyProperty { get; set; }

        public readonly PropertyMetadata Build()
            => new()
            {
                GloballyQualifiedTypeName = GloballyQualifiedTypeName,
                IsChildContent = IsChildContent,
                IsDelegateSignature = IsDelegateSignature,
                IsEventCallback = IsEventCallback,
                IsDelegateWithAwaitableResult = IsDelegateWithAwaitableResult,
                IsGenericTyped = IsGenericTyped,
                IsInitOnlyProperty = IsInitOnlyProperty,
            };
    }
}
