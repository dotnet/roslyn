using System;
using Roslyn.Compilers.Common;
using Roslyn.Services.Shared.Extensions;

namespace Roslyn.Services.Shared.Utilities
{
    internal struct CommonSyntaxHelper
    {
        public static readonly Func<CommonSyntaxToken, bool> NonZeroWidth = t => t.Width() > 0;

        public static readonly Func<CommonSyntaxToken, bool> Any = t => true;
    }
}
