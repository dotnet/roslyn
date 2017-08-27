﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Progression;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Progression
{
    internal static class IconHelper
    {
        private static string GetIconName(string groupName, string itemName)
        {
            return string.Format("Microsoft.VisualStudio.{0}.{1}", groupName, itemName);
        }

        public static string GetIconName(string groupName, Accessibility symbolAccessibility)
        {
            switch (symbolAccessibility)
            {
                case Accessibility.Private:
                    return GetIconName(groupName, "Private");

                case Accessibility.Protected:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.ProtectedOrInternal:
                    return GetIconName(groupName, "Protected");

                case Accessibility.Internal:
                    return GetIconName(groupName, "Internal");

                case Accessibility.Public:
                case Accessibility.NotApplicable:
                    return GetIconName(groupName, "Public");

                default:
                    throw new ArgumentException();
            }
        }

        public static void Initialize(IGlyphService glyphService, IIconService iconService)
        {
            var supportedGlyphGroups = new Dictionary<StandardGlyphGroup, string>
            {
                { StandardGlyphGroup.GlyphGroupError, "Error" },
                { StandardGlyphGroup.GlyphGroupDelegate, "Delegate" },
                { StandardGlyphGroup.GlyphGroupEnum, "Enum" },
                { StandardGlyphGroup.GlyphGroupStruct, "Struct" },
                { StandardGlyphGroup.GlyphGroupClass, "Class" },
                { StandardGlyphGroup.GlyphGroupInterface, "Interface" },
                { StandardGlyphGroup.GlyphGroupModule, "Module" },
                { StandardGlyphGroup.GlyphGroupConstant, "Constant" },
                { StandardGlyphGroup.GlyphGroupEnumMember, "EnumMember" },
                { StandardGlyphGroup.GlyphGroupEvent, "Event" },
                { StandardGlyphGroup.GlyphExtensionMethodPrivate, "ExtensionMethodPrivate" },
                { StandardGlyphGroup.GlyphExtensionMethodProtected, "ExtensionMethodProtected" },
                { StandardGlyphGroup.GlyphExtensionMethodInternal, "ExtensionMethodInternal" },
                { StandardGlyphGroup.GlyphExtensionMethod, "ExtensionMethod" },
                { StandardGlyphGroup.GlyphGroupMethod, "Method" },
                { StandardGlyphGroup.GlyphGroupProperty, "Property" },
                { StandardGlyphGroup.GlyphGroupField, "Field" },
                { StandardGlyphGroup.GlyphGroupOperator, "Operator" },
                { StandardGlyphGroup.GlyphReference, "Reference" }
            };

            var supportedGlyphItems = new Dictionary<StandardGlyphItem, string>
            {
                { StandardGlyphItem.GlyphItemPrivate, "Private" },
                { StandardGlyphItem.GlyphItemProtected, "Protected" },
                { StandardGlyphItem.GlyphItemInternal, "Internal" },
                { StandardGlyphItem.GlyphItemPublic, "Public" },
                { StandardGlyphItem.GlyphItemFriend, "Friend" }
            };

            foreach (var groupKvp in supportedGlyphGroups)
            {
                foreach (var itemKvp in supportedGlyphItems)
                {
                    string iconName = GetIconName(groupKvp.Value, itemKvp.Value);
                    StandardGlyphGroup localGroup = groupKvp.Key;
                    StandardGlyphItem localItem = itemKvp.Key;
                    iconService.AddIcon(iconName, iconName, () => glyphService.GetGlyph(localGroup, localItem));
                }
            }
        }
    }
}
