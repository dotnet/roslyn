// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class INamedTypeSymbolExtensions
    {
        public static StandardGlyphGroup GetTypeGlyphGroup(this INamedTypeSymbol symbol)
        {
            switch (symbol.TypeKind)
            {
                case TypeKind.Class:
                case TypeKind.Submission: // TODO (tomat): this shouldn't be here, submission shouldn't ever be in completion list
                    return StandardGlyphGroup.GlyphGroupClass;
                case TypeKind.Delegate:
                    return StandardGlyphGroup.GlyphGroupDelegate;
                case TypeKind.Enum:
                    return StandardGlyphGroup.GlyphGroupEnum;
                case TypeKind.Module:
                    return StandardGlyphGroup.GlyphGroupModule;
                case TypeKind.Interface:
                    return StandardGlyphGroup.GlyphGroupInterface;
                case TypeKind.Struct:
                    return StandardGlyphGroup.GlyphGroupStruct;
                case TypeKind.Error:
                    return StandardGlyphGroup.GlyphGroupError;
                default:
                    return Contract.FailWithReturn<StandardGlyphGroup>("Unknown named type symbol kind!");
            }
        }
    }
}
