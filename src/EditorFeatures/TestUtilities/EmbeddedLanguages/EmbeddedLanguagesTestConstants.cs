// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Test.Utilities.EmbeddedLanguages
{
    internal static class EmbeddedLanguagesTestConstants
    {
        public static readonly string StringSyntaxAttributeCode = @"
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class StringSyntaxAttribute : Attribute
    {
        public StringSyntaxAttribute(string syntax)
        {
            Syntax = syntax;
This conversation was marked as resolved by deeprobin
            Arguments = Array.Empty<object?>();
        }

        public StringSyntaxAttribute(string syntax, params object?[] arguments)
        {
            Syntax = syntax;
            Arguments = arguments;
        }

        public string Syntax { get; }
        public object?[] Arguments { get; }

        public const string DateTimeFormat = nameof(DateTimeFormat);
        public const string Json = nameof(Json);
        public const string Regex = nameof(Regex);
    }
}";
    }
}
