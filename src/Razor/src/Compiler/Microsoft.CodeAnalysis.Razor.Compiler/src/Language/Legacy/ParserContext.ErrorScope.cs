// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

internal partial class ParserContext
{
    public readonly ref struct ErrorScope(ParserContext context)
    {
        private readonly ParserContext _context = context;

        public void Dispose()
        {
            _context._errorSinkStack.Pop();
        }
    }
}
