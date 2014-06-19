//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

namespace Roslyn.Compilers
{
    public interface ICompiler
    {
        ISyntaxTree Parse(IText text, IParseOptions options);

        // eventually add method for creating an ICompilation...
    }
}