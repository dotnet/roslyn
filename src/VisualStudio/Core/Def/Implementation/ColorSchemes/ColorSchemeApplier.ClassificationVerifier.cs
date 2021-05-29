﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Permissions;
using System.Windows;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.ColorSchemes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.ColorSchemes
{
    internal partial class ColorSchemeApplier
    {
        private sealed class ClassificationVerifier : ForegroundThreadAffinitizedObject
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly ImmutableDictionary<SchemeName, ImmutableDictionary<Guid, ImmutableDictionary<string, uint>>> _colorSchemes;

            private static readonly Guid TextEditorMEFItemsColorCategory = new("75a05685-00a8-4ded-bae5-e7a50bfa929a");

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

            private IVsFontAndColorStorage? _fontAndColorStorage;
            private IVsFontAndColorUtilities? _fontAndColorUtilities;

            private ImmutableArray<string> Classifications { get; }

            public ClassificationVerifier(IThreadingContext threadingContext, IServiceProvider serviceProvider, ImmutableDictionary<SchemeName, ColorScheme> colorSchemes)
                : base(threadingContext)
            {
                _serviceProvider = serviceProvider;

                _colorSchemes = colorSchemes.ToImmutableDictionary(
                    nameAndScheme => nameAndScheme.Key,
                    nameAndScheme => nameAndScheme.Value.Themes.ToImmutableDictionary(
                        theme => theme.Guid,
                        theme => theme.Category.Colors
                            .Where(color => color.Foreground.HasValue)
                            .ToImmutableDictionary(color => color.Name, color => color.Foreground!.Value)));

                // Gather all the classifications from the core and scheme dictionaries.
                var coreClassifications = DarkThemeForeground.Keys.Concat(BlueLightThemeForeground.Keys).Distinct();
                var colorSchemeClassifications = _colorSchemes.Values.SelectMany(scheme => scheme.Values.SelectMany(theme => theme.Keys)).Distinct();
                Classifications = coreClassifications.Concat(colorSchemeClassifications).ToImmutableArray();
            }

            /// <summary>
            /// Determines if any Classification foreground colors have been customized in Fonts and Colors.
            /// </summary>
            public bool AreForegroundColorsCustomized(SchemeName schemeName, Guid themeId)
            {
                AssertIsForeground();

                // Ensure we are initialized
                if (_fontAndColorStorage is null)
                {
                    _fontAndColorStorage = _serviceProvider.GetService<SVsFontAndColorStorage, IVsFontAndColorStorage>();
                    _fontAndColorUtilities = (IVsFontAndColorUtilities)_fontAndColorStorage!;
                }

                // Make no changes when in high contast mode or in unknown theme.
                if (SystemParameters.HighContrast ||
                    !_colorSchemes.TryGetValue(schemeName, out var colorScheme) ||
                    !colorScheme.TryGetValue(themeId, out var colorSchemeTheme))
                {
                    return false;
                }

                var coreThemeColors = (themeId == KnownColorThemes.Dark)
                    ? DarkThemeForeground
                    : BlueLightThemeForeground;

                // Open Text Editor category for readonly access and do not load items if they are defaulted.
                if (_fontAndColorStorage.OpenCategory(TextEditorMEFItemsColorCategory, (uint)__FCSTORAGEFLAGS.FCSF_READONLY) != VSConstants.S_OK)
                {
                    // We were unable to access color information.
                    return false;
                }

                try
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

                        if (IsClassificationCustomized(coreThemeColors, colorSchemeTheme, colorItem, classification))
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

            /// <summary>
            /// Determines if the ColorableItemInfo's Foreground has been customized to a color that doesn't match the
            /// selected scheme.
            /// </summary>
            private bool IsClassificationCustomized(
                ImmutableDictionary<string, uint> coreThemeColors,
                ImmutableDictionary<string, uint> schemeThemeColors,
                ColorableItemInfo colorItem,
                string classification)
            {
                AssertIsForeground();
                Contract.ThrowIfNull(_fontAndColorUtilities);

                var foregroundColorRef = colorItem.crForeground;

                if (_fontAndColorUtilities.GetColorType(foregroundColorRef, out var foregroundColorType) != VSConstants.S_OK)
                {
                    // Without being able to check color type, we cannot make a determination.
                    return false;
                }

                // If the color is defaulted then it isn't customized.
                if (foregroundColorType == (int)__VSCOLORTYPE.CT_AUTOMATIC)
                {
                    return false;
                }

                // Since the color type isn't default then it has been customized, we will
                // perform an additional check for RGB colors to see if the customized color
                // matches the color scheme color.
                if (foregroundColorType != (int)__VSCOLORTYPE.CT_RAW)
                {
                    return true;
                }

                if (coreThemeColors.TryGetValue(classification, out var coreColor))
                {
                    return foregroundColorRef != coreColor;
                }

                if (schemeThemeColors.TryGetValue(classification, out var schemeColor))
                {
                    return foregroundColorRef != schemeColor;
                }

                // Since Classification inheritance isn't represented in the scheme files,
                // this switch case will handle the 3 cases we expect.
                var fallbackColor = classification switch
                {
                    ClassificationTypeNames.OperatorOverloaded => coreThemeColors[ClassificationTypeNames.Operator],
                    ClassificationTypeNames.ControlKeyword => coreThemeColors[ClassificationTypeNames.Keyword],
                    _ => coreThemeColors[ClassificationTypeNames.Identifier]
                };

                return foregroundColorRef != fallbackColor;
            }
        }
    }
}
