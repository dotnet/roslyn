// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language;

internal abstract partial class DocumentationDescriptor
{
    private sealed class FormattedDescriptor : DocumentationDescriptor
    {
        public override object?[] Args { get; }

        private string? _formattedString;

        public FormattedDescriptor(DocumentationId id, object?[] args)
            : base(id)
        {
#if DEBUG
            foreach (var arg in args)
            {
                Debug.Assert(
                    arg is string or int or bool or null,
                    "Only string, int, bool, or null arguments are allowed.");
            }
#endif

            Args = args;
        }

        public override string GetText()
            => _formattedString ??= string.Format(CultureInfo.CurrentCulture, GetDocumentationText(), Args);

        public override bool Equals(DocumentationDescriptor? other)
        {
            if (other is not FormattedDescriptor { Id: var id, Args: var args })
            {
                return false;
            }

            if (Id != id)
            {
                return false;
            }

            var length = Args.Length;

            if (length != args.Length)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                var thisArg = Args[i];
                var otherArg = args[i];

                var areEqual = (thisArg, otherArg) switch
                {
                    (string s1, string s2) => s1 == s2,
                    (int i1, int i2) => i1 == i2,
                    (bool b1, bool b2) => b1 == b2,
                    (null, null) => true,
                    _ => false
                };

                if (!areEqual)
                {
                    return false;
                }
            }

            return true;
        }

        protected override int ComputeHashCode()
        {
            var result = HashCodeCombiner.Start();

            result.Add(Id);

            foreach (var arg in Args)
            {
                result.Add(arg);
            }

            return result.CombinedHash;
        }
    }
}
