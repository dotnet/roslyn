// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class SymbolCompletionFormat : AbstractSymbolCompletionFormat
    {
        public static readonly SymbolCompletionFormat Default =
            new SymbolCompletionFormat(SymbolDisplayFormat.MinimallyQualifiedFormat);

        public SymbolCompletionFormat(SymbolDisplayFormat format) : base(format, format, '<')
        {
        }

        protected sealed override string Escape(string identifier, SyntaxContext context)
        {
            return identifier.EscapeIdentifier(context);
        }
    }
}
