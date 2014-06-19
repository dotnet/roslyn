using System;

namespace Roslyn.Compilers.Common
{
    internal struct CommonSyntaxHelper
    {
        public static readonly Func<CommonSyntaxToken, bool> NonZeroWidth = t => t.Width > 0;

        public static readonly Func<CommonSyntaxToken, bool> Any = t => true;
    }
}
