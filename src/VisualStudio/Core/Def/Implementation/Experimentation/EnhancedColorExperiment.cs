// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using EnvDTE;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    internal class EnhancedColorExperiment : ForegroundThreadAffinitizedObject, IWpfTextViewConnectionListener, IDisposable
    {
        private const string UseEnhancedColorsSetting = "WindowManagement.Options.UseEnhancedColorsForManagedLanguages";

        private readonly IServiceProvider _serviceProvider;

        private EnhancedColorApplier _colorApplier;
        private ISettingsManager _settingsManager;

        private bool _isDisposed = false;
        private bool _hasTextViewOpened;

        [ImportingConstructor]
        [Obsolete]
        public EnhancedColorExperiment(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            // Dispose is invoked when the MEF container is disposed. This will be our
            // signal that VS is shutting down and we shouldn't try and perform any work.
            _isDisposed = true;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            AssertIsForeground();

            if (!_hasTextViewOpened)
            {
                _hasTextViewOpened = true;

                _colorApplier = new EnhancedColorApplier(_serviceProvider);
                _settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));

                // We need to update the theme whenever the Preview Setting changes or the VS Theme changes.
                _settingsManager.GetSubset(UseEnhancedColorsSetting).SettingChangedAsync += UseEnhancedColorsSettingChangedAsync;
                VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;

                VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
        }

        private async Task UseEnhancedColorsSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            await VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
        }

        private void UpdateThemeColors()
        {
            AssertIsForeground();

            // Simply return if we were queued to run during shutdown.
            if (_isDisposed)
            {
                return;
            }

            // We do not want to make any changes while in high contrast mode.
            if (SystemParameters.HighContrast)
            {
                return;
            }

            var currentThemeId = GetThemeId();

            // Get the preview feature flag value.
            var useEnhancedColorsSetting = _settingsManager.GetValueOrDefault(UseEnhancedColorsSetting, defaultValue: 0);

            // useEnhancedColorsSetting
            //  0 -> use enhanced colors.
            //  1 -> use enhanced colors.
            // -1 -> don't use enhanced colors.

            // Try to set colors appropriately. We will only set colors if the user
            // has not customized colors and we consider ourselves the color owner.
            if (useEnhancedColorsSetting != -1)
            {
                _colorApplier.TrySetEnhancedColors(currentThemeId);
            }
            else
            {
                _colorApplier.TrySetDefaultColors(currentThemeId);
            }
        }

        private Guid GetThemeId()
        {
            const string CurrentThemeValueName = "Microsoft.VisualStudio.ColorTheme";
            const string CurrentThemeValueNameNew = "Microsoft.VisualStudio.ColorThemeNew";

            // Look up the value from the new roamed theme property first and
            // fallback to the original roamed theme property if that fails.
            var themeIdString = _settingsManager.GetValueOrDefault<string>(CurrentThemeValueNameNew, null)
                ?? _settingsManager.GetValueOrDefault<string>(CurrentThemeValueName, null);

            return Guid.TryParse(themeIdString, out var themeId) ? themeId : Guid.Empty;
        }

        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };

        private sealed class EnhancedColorApplier
        {
            private readonly DTE _dte;

            private const uint DefaultForegroundColor = 0x01000000u;
            private const uint DefaultBackgroundColor = 0x01000001u;

            private const uint AutomaticForegroundColor = 0x02000000u;
            private const uint AutomaticBackgroundColor = 0x02000001u;

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

            private const uint ExtraContrastThemeClass = 0x556506;

            public const string PlainTextClassificationTypeName = "plain text";

            // Dark Theme
            // We also check OperatorOverloaded and ControlKeyword for whether they are the PlainText color.
            // This is because when the "Use Defaults" is invoked from the Fonts and Colors options page the
            // color reported back will be the PlainText color since these Classifications do not currently have
            // colors defined in the PKGDEF. The other identifier types do not either but the Identifier color
            // happens to be the same as PlainText so an extra check isn't necessary. StructName doesn't need
            // an additional check because it has a color defined in the PKGDEF. The Editor is smart enough
            // to follow the BaseClassification hierarchy and render the colors appropriately.
            private static readonly ImmutableDictionary<string, ImmutableArray<uint>> DarkThemeDefaultForeground =
                new Dictionary<string, ImmutableArray<uint>>()
                {
                    [PlainTextClassificationTypeName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.ClassName] = ImmutableArray.Create(DarkThemeClass),
                    [ClassificationTypeNames.ConstantName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.ControlKeyword] = ImmutableArray.Create(DarkThemePlainText, DarkThemeKeyword),
                    [ClassificationTypeNames.DelegateName] = ImmutableArray.Create(DarkThemeClass),
                    [ClassificationTypeNames.EnumMemberName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.EnumName] = ImmutableArray.Create(DarkThemeEnum),
                    [ClassificationTypeNames.EventName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.ExtensionMethodName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.FieldName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.Identifier] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.InterfaceName] = ImmutableArray.Create(DarkThemeEnum),
                    [ClassificationTypeNames.Keyword] = ImmutableArray.Create(DarkThemeKeyword),
                    [ClassificationTypeNames.LabelName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.LocalName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.MethodName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.ModuleName] = ImmutableArray.Create(DarkThemeClass),
                    [ClassificationTypeNames.NamespaceName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.Operator] = ImmutableArray.Create(DarkThemeOperator),
                    [ClassificationTypeNames.OperatorOverloaded] = ImmutableArray.Create(DarkThemePlainText, DarkThemeOperator),
                    [ClassificationTypeNames.ParameterName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.PropertyName] = ImmutableArray.Create(DarkThemePlainText),
                    [ClassificationTypeNames.StructName] = ImmutableArray.Create(DarkThemeClass),
                    [ClassificationTypeNames.TypeParameterName] = ImmutableArray.Create(DarkThemeEnum),
                }.ToImmutableDictionary();

            // Light or Blue themes
            // Same as above, we also check ControlKeyword for whether it is the PlainText color. OperatorOverload and
            // the other Identifier types do not need an additional check because their default color is the same
            // as PlainText.
            private static readonly ImmutableDictionary<string, ImmutableArray<uint>> LightThemeDefaultForeground =
                new Dictionary<string, ImmutableArray<uint>>()
                {
                    [PlainTextClassificationTypeName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.ClassName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.ConstantName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.ControlKeyword] = ImmutableArray.Create(LightThemePlainText, LightThemeKeyword),
                    [ClassificationTypeNames.DelegateName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.EnumMemberName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.EnumName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.EventName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.ExtensionMethodName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.FieldName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.Identifier] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.InterfaceName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.Keyword] = ImmutableArray.Create(LightThemeKeyword),
                    [ClassificationTypeNames.LabelName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.LocalName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.MethodName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.ModuleName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.NamespaceName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.Operator] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.OperatorOverloaded] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.ParameterName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.PropertyName] = ImmutableArray.Create(LightThemePlainText),
                    [ClassificationTypeNames.StructName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                    [ClassificationTypeNames.TypeParameterName] = ImmutableArray.Create(LightThemeClass, ExtraContrastThemeClass),
                }.ToImmutableDictionary();

            // When we build our classification map we will need to look at all the classifications with foreground color as
            // well as the static symbol classification that does not have a foreground.
            private static readonly ImmutableArray<string> Classifications =
                DarkThemeDefaultForeground.Keys.Concat(new[] { ClassificationTypeNames.StaticSymbol }).ToImmutableArray();

            public EnhancedColorApplier(IServiceProvider serviceProvider)
            {
                _dte = (DTE)serviceProvider.GetService(typeof(DTE));
            }

            public void TrySetDefaultColors(Guid themeId)
            {
                var colorItemMap = GetColorItemMap();

                // We consider ourselves the owner of the colors and set
                // default colors only when every classification we are
                // updating matches our enhanced color for the current theme.
                if (!AreColorsEnhanced(colorItemMap, themeId))
                {
                    return;
                }

                // Since we only enhance certain classifications, we only return the colors that we enhanced back to their defaults.
                if (themeId == KnownColorThemes.Dark)
                {
                    // Dark Theme
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.LocalName, DarkThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ParameterName, DarkThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.MethodName, DarkThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, DarkThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.OperatorOverloaded, DarkThemeOperator);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ControlKeyword, DarkThemeKeyword);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StructName, DarkThemeClass);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StaticSymbol, DefaultForegroundColor, DefaultBackgroundColor);
                }
                else
                {
                    // Light or Blue themes
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.LocalName, LightThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ParameterName, LightThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.MethodName, LightThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, LightThemeIdentifier);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.OperatorOverloaded, LightThemeOperator);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemeKeyword);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StaticSymbol, DefaultForegroundColor, DefaultBackgroundColor);
                }
            }

            public void TrySetEnhancedColors(Guid themeId)
            {
                var colorItemMap = GetColorItemMap();

                // We consider ourselves the owner of the colors and set
                // enhanced colors only when every classification we are
                // updating matches their default color for the current theme.
                if (!AreColorsDefaulted(colorItemMap, themeId))
                {
                    return;
                }

                if (themeId == KnownColorThemes.Dark)
                {
                    // Dark Theme
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.LocalName, DarkThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ParameterName, DarkThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.MethodName, DarkThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, DarkThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.OperatorOverloaded, DarkThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ControlKeyword, DarkThemeControlKeywordPurple);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StructName, DarkThemeStructMint);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StaticSymbol, DefaultForegroundColor, DefaultBackgroundColor);
                }
                else
                {
                    // Light or Blue themes
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.LocalName, LightThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ParameterName, LightThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.MethodName, LightThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, LightThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.OperatorOverloaded, LightThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemeControlKeywordPurple);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.StaticSymbol, DefaultForegroundColor, DefaultBackgroundColor);
                }
            }

            private Dictionary<string, ColorableItems> GetColorItemMap()
            {
                var props = _dte.Properties["FontsAndColors", "TextEditor"];
                var prop = props.Item("FontsAndColorsItems");
                var fontsAndColorsItems = (FontsAndColorsItems)prop.Object;

                var colorItemMap = Classifications.ToDictionary(
                    classification => classification,
                    classification => fontsAndColorsItems.Item(classification));

                return colorItemMap;
            }

            private void UpdateColorItem(IDictionary<string, ColorableItems> colorItemMap, string classification, uint foreground, uint background = DefaultBackgroundColor, bool isBold = false)
            {
                colorItemMap[classification].Foreground = foreground;
                colorItemMap[classification].Background = background;
                colorItemMap[classification].Bold = isBold;
            }

            /// <summary>
            /// Determines if the default colors are applied for the current theme. This is how we determine
            /// if the default colors are applied for the current theme.
            /// </summary>
            private bool AreColorsDefaulted(Dictionary<string, ColorableItems> colorItemMap, Guid themeId)
            {
                var themeDefaultForeground = themeId == KnownColorThemes.Dark
                    ? DarkThemeDefaultForeground   // Dark Theme
                    : LightThemeDefaultForeground; // Light or Blue themes

                // For all theme classifications ensure the foreground color matches one of the defaults.
                return themeDefaultForeground.Keys.All(
                    classification => themeDefaultForeground[classification].Any(
                        themeColor => IsDefaultColor(colorItemMap, classification, themeColor)));
            }

            private bool IsDefaultColor(Dictionary<string, ColorableItems> colorItemMap, string classification, uint themeColor)
            {
                // Without visiting the Font and Colors options dialog, the reported colors for
                // classifications that do not export a color, have a color defined in the PKGDEF,
                // or have a custom color set will be Black (0x00000000) for the Foreground and
                // White (0x00FFFFF) for the Background. We will additionally check the foreground
                // against Black and DefaultForegroundColor for completeness.

                var foreground = colorItemMap[classification].Foreground;
                return foreground == themeColor ||
                    foreground == 0 ||
                    foreground == DefaultForegroundColor;
            }

            /// <summary>
            /// Determines if our enhanced colors are applied for the current theme. This is how we determine
            /// if we are the color owner when trying to set default colors.
            /// </summary>
            private bool AreColorsEnhanced(Dictionary<string, ColorableItems> colorItemMap, Guid themeId)
            {
                if (themeId == KnownColorThemes.Dark)
                {
                    // Dark Theme
                    return colorItemMap[ClassificationTypeNames.LocalName].Foreground == DarkThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.ParameterName].Foreground == DarkThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.MethodName].Foreground == DarkThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ExtensionMethodName].Foreground == DarkThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.OperatorOverloaded].Foreground == DarkThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ControlKeyword].Foreground == DarkThemeControlKeywordPurple &&
                        colorItemMap[ClassificationTypeNames.StructName].Foreground == DarkThemeStructMint;
                }
                else
                {
                    // Light or Blue themes
                    return colorItemMap[ClassificationTypeNames.LocalName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.ParameterName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.MethodName].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ExtensionMethodName].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.OperatorOverloaded].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ControlKeyword].Foreground == LightThemeControlKeywordPurple;
                }
            }
        }
    }
}
