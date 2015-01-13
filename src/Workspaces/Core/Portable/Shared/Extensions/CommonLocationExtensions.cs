// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonLocationExtensions
    {
        public static SyntaxToken FindToken(this Location location, CancellationToken cancellationToken)
        {
            return location.SourceTree.GetRoot(cancellationToken).FindToken(location.SourceSpan.Start);
        }

        public static bool IsVisibleSourceLocation(this Location loc)
        {
            if (!loc.IsInSource)
            {
                return false;
            }

            var tree = loc.SourceTree;
            return !(tree == null || tree.IsHiddenPosition(loc.SourceSpan.Start));
        }
    }
}