// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        // Now that we are updating the theme's default color for classifications instead of updating the applied classification color, we need to 
        // update the classifications whose applied color matches the theme's color. These need to be reverted to the default color so that when we
        // change theme colors it will be reflected in the editor.
        private sealed class ForegroundColorDefaulter : ForegroundThreadAffinitizedObject
        {
            private readonly ColorSchemeSettings _settings;
            private readonly ImmutableArray<ImmutableDictionary<Guid, ImmutableDictionary<string, uint?>>> _colorSchemes;

            private readonly IVsFontAndColorStorage _fontAndColorStorage;
            private readonly IVsFontAndColorStorage3 _fontAndColorStorage3;
            private readonly IVsFontAndColorUtilities _fontAndColorUtilities;

            private static readonly Guid TextEditorMEFItemsColorCategory = new Guid("75a05685-00a8-4ded-bae5-e7a50bfa929a");

            // These classification colors (0x00BBGGRR) should match the VS\EditorColors.xml file.
            // They are not in the scheme files because they are core classifications.
            private const uint DarkThemePlainText = 0x00DCDCDCu;
            private const uint DarkThemeIdentifier = DarkThemePlainText;
            private const uint DarkThemeOperator = 0x00B4B4B4u;
            private const uint DarkThemeKeyword = 0x00D69C56u;

            private const uint LightThemePlainText = 0x00000000u;
            private const uint LightThemeIdentifier = LightThemePlainText;
            private const uint LightThemeOperator = LightThemePlainText;
            private const uint LightThemeKeyword = 0x00FF0000u;

            private const string PlainTextClassificationTypeName = "plain text";

            // Dark Theme Core Classifications
            private static ImmutableDictionary<string, uint> DarkThemeForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = DarkThemePlainText,
                    [ClassificationTypeNames.Identifier] = DarkThemeIdentifier,
                    [ClassificationTypeNames.Keyword] = DarkThemeKeyword,
                    [ClassificationTypeNames.Operator] = DarkThemeOperator,
                }.ToImmutableDictionary();

            // Light, Blue, or AdditionalContrast Theme Core Classifications
            private static ImmutableDictionary<string, uint> BlueLightThemeForeground =>
                new Dictionary<string, uint>()
                {
                    [PlainTextClassificationTypeName] = LightThemePlainText,
                    [ClassificationTypeNames.Identifier] = LightThemeIdentifier,
                    [ClassificationTypeNames.Keyword] = LightThemeKeyword,
                    [ClassificationTypeNames.Operator] = LightThemeOperator,
                }.ToImmutableDictionary();


            // The High Contrast theme is not included because we do not want to make changes when the user is in High Contrast mode.

            private ImmutableArray<string> Classifications { get; }

            public ForegroundColorDefaulter(IThreadingContext threadingContext, IServiceProvider serviceProvider, ColorSchemeSettings settings, ImmutableDictionary<string, ColorScheme> colorSchemes)
                : base(threadingContext, assertIsForeground: true)
            {
                _settings = settings;

                // Convert colors schemes into an array of theme dictionaries which contain classification dictionaries of colors.
                _colorSchemes = colorSchemes.Values.Select(
                    scheme => scheme.Themes.ToImmutableDictionary(
                        theme => theme.Guid,
                        theme => theme.Category.Colors.ToImmutableDictionary(
                            color => color.Name,
                            color => color.Foreground)))
                    .ToImmutableArray();

                // Gather all the classifications from the core and scheme dictionaries.
                var coreClassifications = DarkThemeForeground.Keys.Concat(BlueLightThemeForeground.Keys).Distinct();
                var colorSchemeClassifications = _colorSchemes.SelectMany(scheme => scheme.Values.SelectMany(theme => theme.Keys)).Distinct();
                Classifications = coreClassifications.Concat(colorSchemeClassifications).ToImmutableArray();

                _fontAndColorStorage = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                // IVsFontAndColorStorage3 has methods to default classifications but does not include the methods defined in IVsFontAndColorStorage
                _fontAndColorStorage3 = (IVsFontAndColorStorage3)_fontAndColorStorage;
                _fontAndColorUtilities = serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorUtilities>();
            }

            /// <summary>
            /// Determines if all Classification foreground colors are DefaultColor or can be safely reverted to DefaultColor.
            /// </summary>
            public bool AreClassificationsDefaultable(Guid themeId)
            {
                AssertIsForeground();

                // Make no changes when in high contast mode or in unknown theme.
                if (SystemParameters.HighContrast || !IsSupportedTheme(themeId))
                {
                    return false;
                }

                // Open Text Editor category for readonly access and do not load items if they are defaulted.
                if (_fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_READONLY) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return false;
                }

                try
                {
                    foreach (var scheme in _colorSchemes)
                    {
                        var schemeThemeColors = scheme[themeId];

                        if (AreClassificationsDefaultableToScheme(themeId, schemeThemeColors))
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    _fontAndColorStorage.CloseCategory();
                }

                return false;
            }

            private bool IsSupportedTheme(Guid themeId)
            {
                return _colorSchemes.Any(scheme => scheme.ContainsKey(themeId));
            }

            private bool AreClassificationsDefaultableToScheme(Guid themeId, ImmutableDictionary<string, uint?> schemeThemeColors)
            {
                foreach (var classification in Classifications)
                {
                    var colorItems = new ColorableItemInfo[1];

                    if (_fontAndColorStorage.GetItem(classification, colorItems) != VSConstants.S_OK)
                    {
                        // Classifications that are still defaulted will not have entries.
                        continue;
                    }

                    var colorItem = colorItems[0];

                    if (!IsClassificationDefaultable(themeId, schemeThemeColors, colorItem, classification))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool IsClassificationDefaultable(Guid themeId, ImmutableDictionary<string, uint?> schemeThemeColors, ColorableItemInfo colorItem, string classification)
            {
                if (_fontAndColorUtilities.GetColorType(colorItem.crForeground, out var foregroundColorType) != VSConstants.S_OK)
                {
                    // Without being able to check color type, we cannot make a determination.
                    return false;
                }

                return foregroundColorType switch
                {
                    (int)__VSCOLORTYPE.CT_AUTOMATIC => true,
                    (int)__VSCOLORTYPE.CT_RAW => CanItemBeDefaulted(themeId, schemeThemeColors, colorItem, classification),
                    _ => false
                };
            }

            /// <summary>
            /// Determines if the ColorableItemInfo can be reverted to its default state. This requires checking both color and font configuration,
            /// since reverting will reset all information for the item.
            /// </summary>
            private bool CanItemBeDefaulted(Guid themeId, ImmutableDictionary<string, uint?> schemeThemeColors, ColorableItemInfo colorItem, string classification)
            {
                if (_fontAndColorUtilities.GetColorType(colorItem.crBackground, out var backgroundColorType) != VSConstants.S_OK)
                {
                    // Without being able to check color type, we cannot make a determination.
                    return false;
                }

                return IsForegroundTheSchemeColor(themeId, schemeThemeColors, classification, colorItem.crForeground)
                    && backgroundColorType == (int)__VSCOLORTYPE.CT_AUTOMATIC
                    && colorItem.dwFontFlags == (uint)FONTFLAGS.FF_DEFAULT;
            }

            private bool IsForegroundTheSchemeColor(Guid themeId, ImmutableDictionary<string, uint?> schemeThemeColors, string classification, uint foregroundColorRef)
            {
                var coreThemeColors = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeForeground
                    : BlueLightThemeForeground;

                if (coreThemeColors.TryGetValue(classification, out var coreColor))
                {
                    return foregroundColorRef == coreColor;
                }

                if (schemeThemeColors.TryGetValue(classification, out var schemeColor) && schemeColor.HasValue)
                {
                    return foregroundColorRef == schemeColor.Value;
                }

                // Since Classification inheritance isn't represented in the scheme files,
                // this switch case will handle the 3 cases we expect.
                var fallbackColor = classification switch
                {
                    ClassificationTypeNames.OperatorOverloaded => coreThemeColors[ClassificationTypeNames.Operator],
                    ClassificationTypeNames.ControlKeyword => coreThemeColors[ClassificationTypeNames.Keyword],
                    _ => coreThemeColors[ClassificationTypeNames.Identifier]
                };

                return foregroundColorRef == fallbackColor;
            }

            /// <summary>
            /// Reverts Classifications to their default state.
            /// </summary>
            public void DefaultClassifications()
            {
                AssertIsForeground();

                var themeId = _settings.GetThemeId();

                // Make no changes when in high contast mode, in unknown theme, or if theme has been defaulted.
                if (SystemParameters.HighContrast || !IsSupportedTheme(themeId) || _settings.HasThemeBeenDefaulted[themeId])
                {
                    return;
                }

                // Open Text Editor category for read/write.
                if (_fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return;
                }

                try
                {
                    foreach (var classification in Classifications)
                    {
                        DefaultClassification(classification);
                    }
                }
                finally
                {
                    _fontAndColorStorage.CloseCategory();
                }

                _settings.HasThemeBeenDefaulted[themeId] = true;
            }

            private void DefaultClassification(string classification)
            {
                var colorItems = new ColorableItemInfo[1];
                if (_fontAndColorStorage.GetItem(classification, colorItems) != VSConstants.S_OK)
                {
                    // Classifications that are still defaulted will not have entries.
                    return;
                }

                var colorItem = colorItems[0];

                // If the foreground is the automatic color then no need to default the classification,
                // since it will pull in the theme's color.
                if (_fontAndColorUtilities.GetColorType(colorItem.crForeground, out var foregroundColorType) == VSConstants.S_OK
                    && foregroundColorType == (int)__VSCOLORTYPE.CT_AUTOMATIC)
                {
                    return;
                }

                _fontAndColorStorage3.RevertItemToDefault(classification);
            }
        }
    }
}
