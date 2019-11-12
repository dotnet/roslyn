// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private sealed class ForegroundColorDefaulter
        {
            private readonly IVsFontAndColorStorage _fontAndColorStorage;
            private readonly IVsFontAndColorStorage3 _fontAndColorStorage3;
            private readonly IVsFontAndColorUtilities _fontAndColorUtilities;

            private const uint DefaultForegroundColor = 0x01000000u;
            private const uint DefaultBackgroundColor = 0x01000001u;

            // Colors are in 0x00BBGGRR
            private const uint DarkThemePlainText = 0x00DCDCDCu;
            private const uint DarkThemeIdentifier = DarkThemePlainText;
            private const uint DarkThemeOperator = 0x00B4B4B4u;
            private const uint DarkThemeKeyword = 0x00D69C56u;
            private const uint DarkThemeClass = 0x00B0C94Eu;
            private const uint DarkThemeEnum = 0x00A3D7B8;
            private const uint DarkThemeLocalBlue = 0x00FEDC9Cu;
            private const uint DarkThemeMethodYellow = 0x00AADCDCu;
            private const uint DarkThemeControlKeywordPurple = 0x00DFA0D8u;
            private const uint DarkThemeStructMint = 0x0091C686u;

            private const uint LightThemePlainText = 0x00000000u;
            private const uint LightThemeIdentifier = LightThemePlainText;
            private const uint LightThemeOperator = LightThemePlainText;
            private const uint LightThemeKeyword = 0x00FF0000u;
            private const uint LightThemeClass = 0x00AF912Bu;
            private const uint LightThemeLocalBlue = 0x007F371Fu;
            private const uint LightThemeMethodYellow = 0x001F5374u;
            private const uint LightThemeControlKeywordPurple = 0x00C4088Fu;

            private const uint ExtraContrastThemeClass = 0x00556506u;

            private const string PlainTextClassificationTypeName = "plain text";

            private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");

            // Dark Theme
            private static ImmutableDictionary<string, uint> DarkThemeEnhancedForegroundChanges =>
                new Dictionary<string, uint>()
                {
                    [ClassificationTypeNames.ControlKeyword] = DarkThemeControlKeywordPurple,
                    [ClassificationTypeNames.ExtensionMethodName] = DarkThemeMethodYellow,
                    [ClassificationTypeNames.LocalName] = DarkThemeLocalBlue,
                    [ClassificationTypeNames.MethodName] = DarkThemeMethodYellow,
                    [ClassificationTypeNames.OperatorOverloaded] = DarkThemeMethodYellow,
                    [ClassificationTypeNames.ParameterName] = DarkThemeLocalBlue,
                    [ClassificationTypeNames.StructName] = DarkThemeStructMint,
                }.ToImmutableDictionary();

            private static ImmutableDictionary<string, uint> DarkThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = DarkThemePlainText,
                    [ClassificationTypeNames.ClassName] = DarkThemeClass,
                    [ClassificationTypeNames.ConstantName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = DarkThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = DarkThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = DarkThemeEnum,
                    [ClassificationTypeNames.EventName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = DarkThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = DarkThemeEnum,
                    [ClassificationTypeNames.Keyword] = DarkThemeKeyword,
                    [ClassificationTypeNames.LabelName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = DarkThemeClass,
                    [ClassificationTypeNames.NamespaceName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.Operator] = DarkThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = DarkThemeOperator,
                    [ClassificationTypeNames.ParameterName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = DarkThemeIdentifier,
                    [ClassificationTypeNames.StructName] = DarkThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = DarkThemeEnum,
                }.ToImmutableDictionary();

            // Light or Blue themes
            private static ImmutableDictionary<string, uint> BlueLightThemeEnhancedForegroundChanges =>
                new Dictionary<string, uint>()
                {
                    [ClassificationTypeNames.ControlKeyword] = LightThemeControlKeywordPurple,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeMethodYellow,
                    [ClassificationTypeNames.LocalName] = LightThemeLocalBlue,
                    [ClassificationTypeNames.MethodName] = LightThemeMethodYellow,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeMethodYellow,
                    [ClassificationTypeNames.ParameterName] = LightThemeLocalBlue,
                }.ToImmutableDictionary();

            private static ImmutableDictionary<string, uint> BlueLightThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = LightThemePlainText,
                    [ClassificationTypeNames.ClassName] = LightThemeClass,
                    [ClassificationTypeNames.ConstantName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = LightThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = LightThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = LightThemeClass,
                    [ClassificationTypeNames.EventName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = LightThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = LightThemeClass,
                    [ClassificationTypeNames.Keyword] = LightThemeKeyword,
                    [ClassificationTypeNames.LabelName] = LightThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = LightThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = LightThemeClass,
                    [ClassificationTypeNames.NamespaceName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Operator] = LightThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeOperator,
                    [ClassificationTypeNames.ParameterName] = LightThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = LightThemeIdentifier,
                    [ClassificationTypeNames.StructName] = LightThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = LightThemeClass,
                }.ToImmutableDictionary();

            // AdditionalContrast Theme
            private static ImmutableDictionary<string, uint> AdditionalContrastThemeClassicForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = LightThemePlainText,
                    [ClassificationTypeNames.ClassName] = ExtraContrastThemeClass,
                    [ClassificationTypeNames.ConstantName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ControlKeyword] = LightThemeKeyword,
                    [ClassificationTypeNames.DelegateName] = ExtraContrastThemeClass,
                    [ClassificationTypeNames.EnumMemberName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EnumName] = LightThemeIdentifier,
                    [ClassificationTypeNames.EventName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ExtensionMethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.FieldName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Identifier] = LightThemeIdentifier,
                    [ClassificationTypeNames.InterfaceName] = ExtraContrastThemeClass,
                    [ClassificationTypeNames.Keyword] = LightThemeKeyword,
                    [ClassificationTypeNames.LabelName] = LightThemeIdentifier,
                    [ClassificationTypeNames.LocalName] = LightThemeIdentifier,
                    [ClassificationTypeNames.MethodName] = LightThemeIdentifier,
                    [ClassificationTypeNames.ModuleName] = ExtraContrastThemeClass,
                    [ClassificationTypeNames.NamespaceName] = LightThemeIdentifier,
                    [ClassificationTypeNames.Operator] = LightThemeOperator,
                    [ClassificationTypeNames.OperatorOverloaded] = LightThemeOperator,
                    [ClassificationTypeNames.ParameterName] = LightThemeIdentifier,
                    [ClassificationTypeNames.PropertyName] = LightThemeIdentifier,
                    [ClassificationTypeNames.StructName] = ExtraContrastThemeClass,
                    [ClassificationTypeNames.TypeParameterName] = ExtraContrastThemeClass,
                }.ToImmutableDictionary();

            // When we build our classification map we will need to look at all the classifications with foreground color.
            private static ImmutableArray<string> Classifications => DarkThemeClassicForeground.Keys.ToImmutableArray();

            public ForegroundColorDefaulter(IServiceProvider serviceProvider)
            {
                _fontAndColorStorage = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                _fontAndColorStorage3 = (IVsFontAndColorStorage3)_fontAndColorStorage;
                _fontAndColorUtilities = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorUtilities>();
            }

            public bool AreClassificationsDefaulted(Guid themeId)
            {
                var themeClassicForeground = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeClassicForeground
                    : (themeId == KnownColorThemes.AdditionalContrast)
                        ? AdditionalContrastThemeClassicForeground
                        : BlueLightThemeClassicForeground;

                var themeEnhancedForegroundChanges = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeEnhancedForegroundChanges
                    : BlueLightThemeEnhancedForegroundChanges;

                // Open Text Editor category for readonly access
                _fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_READONLY);

                var allDefaulted = true;

                foreach (var classification in Classifications)
                {
                    var colorItems = new ColorableItemInfo[1];
                    _fontAndColorStorage.GetItem(classification, colorItems);

                    var colorItem = colorItems[0];

                    if (!IsForegroundDefaulted(colorItem)
                        && !IsItemDefaultable(colorItem, classification, themeClassicForeground, themeEnhancedForegroundChanges))
                    {
                        allDefaulted = false;
                        break;
                    }
                }

                _fontAndColorStorage.CloseCategory();

                return allDefaulted;
            }

            private bool IsForegroundDefaulted(ColorableItemInfo colorInfo)
            {
                // Since we are primarily concerned with the foreground color, return early
                // if the setting isn't populated or is defaulted.
                return colorInfo.bForegroundValid == 0
                    || colorInfo.crForeground == DefaultForegroundColor;
            }

            private bool IsItemDefaultable(ColorableItemInfo colorInfo,
                string classification,
                ImmutableDictionary<string, uint> themeClassicForeground,
                ImmutableDictionary<string, uint> themeEnhancedForegroundChanges)
            {
                var foregroundColorRef = colorInfo.crForeground;
                _fontAndColorUtilities.GetColorType(foregroundColorRef, out var foregroundColorType);

                // Check if the color is an RGB. If the color has been changed to a system color or other, then it can't be default.
                if (foregroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    return false;
                }

                var classicColor = themeClassicForeground[classification];
                var enhancedColor = themeEnhancedForegroundChanges.ContainsKey(classification)
                    ? themeEnhancedForegroundChanges[classification]
                    : classicColor; // Use the classic color since there is not an enhanced color for this classification

                var foregroundIsDefault = foregroundColorRef == classicColor || foregroundColorRef == enhancedColor;

                var backgroundColorRef = colorInfo.crBackground;
                var backgroundAndFontIsDefault = backgroundColorRef == DefaultBackgroundColor
                    && colorInfo.dwFontFlags == (uint)FONTFLAGS.FF_DEFAULT;

                if (foregroundIsDefault && backgroundAndFontIsDefault)
                {
                    return true;
                }

                return false;
            }

            public void SetDefaultForeground(Guid themeId)
            {
                var themeClassicForeground = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeClassicForeground
                    : (themeId == KnownColorThemes.AdditionalContrast)
                        ? AdditionalContrastThemeClassicForeground
                        : BlueLightThemeClassicForeground;

                var themeEnhancedForegroundChanges = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeEnhancedForegroundChanges
                    : BlueLightThemeEnhancedForegroundChanges;

                // Open Text Editor category for read/write.
                _fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);

                // Try to default any colors that are already a default theme color.
                foreach (var classification in Classifications)
                {
                    var colorItems = new ColorableItemInfo[1];
                    _fontAndColorStorage.GetItem(classification, colorItems);

                    var colorItem = colorItems[0];

                    if (IsForegroundDefaulted(colorItem))
                    {
                        continue;
                    }

                    if (IsItemDefaultable(colorItem, classification, themeClassicForeground, themeEnhancedForegroundChanges))
                    {
                        _fontAndColorStorage3.RevertItemToDefault(classification);
                    }
                }

                _fontAndColorStorage.CloseCategory();
            }
        }
    }
}
