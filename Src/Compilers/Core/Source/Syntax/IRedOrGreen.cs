using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    interface IRedOrGreen
    {
        SyntaxNode AsRed();
        GreenNode AsGreen();
    }
}