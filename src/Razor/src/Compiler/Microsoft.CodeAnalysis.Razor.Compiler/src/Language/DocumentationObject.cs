// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
///  Helper struct that wraps a <see cref="DocumentationDescriptor"/>, <see cref="string"/>, or <see langword="null"/>.
/// </summary>
internal readonly record struct DocumentationObject
{
    public readonly object? Object;

    public DocumentationObject(object? obj)
    {
        if (obj is not (DocumentationDescriptor or string or null))
        {
            throw new ArgumentException(
                Resources.FormatA_documentation_object_can_only_be_a_0_instance_string_or_null(nameof(DocumentationDescriptor)),
                paramName: nameof(obj));
        }

        Object = obj;
    }

    public void AppendToChecksum(in Checksum.Builder builder)
    {
        switch (Object)
        {
            case DocumentationDescriptor descriptor:
                builder.Append((int)descriptor.Id);

                foreach (var arg in descriptor.Args)
                {
                    switch (arg)
                    {
                        case string s:
                            builder.Append(s);
                            break;

                        case int i:
                            builder.Append(i);
                            break;

                        case bool b:
                            builder.Append(b);
                            break;

                        case null:
                            builder.AppendNull();
                            break;

                        default:
                            throw new NotSupportedException();
                    }
                }

                break;

            case string s:
                builder.Append(s);
                break;

            case null:
                builder.AppendNull();
                break;
        }
    }

    public readonly string? GetText()
        => Object switch
        {
            DocumentationDescriptor d => d.GetText(),
            string s => s,
            null => null,
            _ => Assumed.Unreachable<string>()
        };

    public override int GetHashCode()
        => Object switch
        {
            DocumentationDescriptor d => d.GetHashCode(),
            string s => s.GetHashCode(),
            null => 0,
            _ => Assumed.Unreachable<int>()
        };

    public bool Equals(DocumentationObject other)
        => (Object, other.Object) switch
        {
            (DocumentationDescriptor d1, DocumentationDescriptor d2) => d1.Equals(d2),
            (string s1, string s2) => s1 == s2,
            (null, null) => true,
            (DocumentationDescriptor or string or null, DocumentationDescriptor or string or null) => false,
            _ => Assumed.Unreachable<bool>()
        };

    public static implicit operator DocumentationObject(string text)
        => new(text);

    public static implicit operator DocumentationObject(DocumentationDescriptor descriptor)
        => new(descriptor);
}
