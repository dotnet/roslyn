using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Experimentation
{
    [Export(typeof(IWpfTextViewConnectionListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Analyzable)]
    internal class EnhancedColorExperiment : ForegroundThreadAffinitizedObject, IWpfTextViewConnectionListener
    {
        private const string UseEnhancedColorsFlight = "UseEnhancedColors";
        private const string StopEnhancedColorsFlight = "StopEnhancedColors";
        private const string UseEnhancedColorsSetting = "WindowManagement.Options.UseEnhancedColorsForManagedLanguages";

        private readonly IExperimentationService _experimentationService;
        private readonly IServiceProvider _serviceProvider;

        private EnhancedColorApplier _colorApplier;
        private ISettingsManager _settingsManager;
        private RegistryKey _vsRegistryRoot;

        private bool _inUseEnhancedColorsFlight;
        private bool _inStopEnhancedColorsFlight;
        private bool _hasTextViewOpened;

        [ImportingConstructor]
        private EnhancedColorExperiment(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, VisualStudioExperimentationService experimentationService)
            : base(threadingContext)
        {
            _serviceProvider = serviceProvider;
            _experimentationService = experimentationService;
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            AssertIsForeground();

            if (!_hasTextViewOpened)
            {
                _hasTextViewOpened = true;

                _colorApplier = new EnhancedColorApplier(_serviceProvider);
                _vsRegistryRoot = VSRegistry.RegistryRoot(_serviceProvider, __VsLocalRegistryType.RegType_UserSettings, writable: true);

                // Check which experimental flights we are in
                _inUseEnhancedColorsFlight = _experimentationService?.IsExperimentEnabled(UseEnhancedColorsFlight) ?? false;
                _inStopEnhancedColorsFlight = _experimentationService?.IsExperimentEnabled(StopEnhancedColorsFlight) ?? false;

                // Do not hook settings changed if we have stopped the experiment. 
                // We will simply remove the enhanced colors if they are applied.
                if (!_inStopEnhancedColorsFlight)
                {
                    _settingsManager = (ISettingsManager)_serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));

                    // We need to update the theme whenever the Preview Setting changes or the VS Theme changes.
                    _settingsManager.GetSubset(UseEnhancedColorsSetting).SettingChangedAsync += UseEnhancedColorsSettingChangedAsync;
                    VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
                }

                VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            AssertIsForeground();

            // Wait until things have settled down from the theme change, since we will potentially be changing theme colors.
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
        }

        private Task UseEnhancedColorsSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            AssertIsForeground();

            UpdateThemeColors();

            return Task.CompletedTask;
        }

        private void UpdateThemeColors()
        {
            AssertIsForeground();

            var currentThemeId = GetThemeId();

            // Get the preview feature flag value.
            var useEnhancedColorsSetting = _settingsManager?.GetValueOrDefault(name: UseEnhancedColorsSetting, defaultValue: 0) ?? 0;

            // useEnhancedColorsSetting
            //  0 -> use value from flight.
            //  1 -> always use enhanced colors (unless the kill flight is active).
            // -1 -> never use enhanced colors.
            var useFlightValue = useEnhancedColorsSetting == 0;
            var applyEnhancedColors = (useFlightValue && _inUseEnhancedColorsFlight) || useEnhancedColorsSetting == 1;
            var removeEnhancedColors = !applyEnhancedColors || _inStopEnhancedColorsFlight;

            if (removeEnhancedColors)
            {
                _colorApplier.SetDefaultColors(currentThemeId);
            }
            else
            {
                _colorApplier.SetEnhancedColors(currentThemeId);
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
            private const uint DarkThemeLocalBlue = 0x00FEDC9Cu;
            private const uint DarkThemeMethodYellow = 0x00AADCDCu;
            private const uint DarkThemeControlKeywordPurple = 0x00DFA0D8u;
            private const uint DarkThemeStructMint = 0x008CC77Eu;

            private const uint LightThemePlainText = 0x00000000u;
            private const uint LightThemeIdentifier = LightThemePlainText;
            private const uint LightThemeOperator = LightThemePlainText;
            private const uint LightThemeKeyword = 0x00FF0000u;
            private const uint LightThemeClass = 0x00AF912Bu;
            private const uint LightThemeLocalBlue = 0x007F371Fu;
            private const uint LightThemeMethodYellow = 0x001F5374u;
            private const uint LightThemeControlKeywordPurple = 0x00C4088Fu;

            public EnhancedColorApplier(IServiceProvider serviceProvider)
            {
                _dte = (DTE)serviceProvider.GetService(typeof(DTE));
            }

            public void SetDefaultColors(Guid themeId)
            {
                var colorItemMap = GetColorItemMap();

                // Only set default colors if the users hasn't customized their colors
                if (!AreColorsEnhanced(colorItemMap, themeId))
                {
                    return;
                }

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
                }
            }

            public void SetEnhancedColors(Guid themeId)
            {
                var colorItemMap = GetColorItemMap();

                // Only update colors if the users hasn't customized their colors
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
                }
            }

            private Dictionary<string, ColorableItems> GetColorItemMap()
            {
                var props = _dte.Properties["FontsAndColors", "TextEditor"];
                var prop = props.Item("FontsAndColorsItems");
                var fontsAndColorsItems = (FontsAndColorsItems)prop.Object;

                var colorItemMap = new Dictionary<string, ColorableItems>
                {
                    [ClassificationTypeNames.LocalName] = fontsAndColorsItems.Item(ClassificationTypeNames.LocalName),
                    [ClassificationTypeNames.ParameterName] = fontsAndColorsItems.Item(ClassificationTypeNames.ParameterName),
                    [ClassificationTypeNames.MethodName] = fontsAndColorsItems.Item(ClassificationTypeNames.MethodName),
                    [ClassificationTypeNames.ExtensionMethodName] = fontsAndColorsItems.Item(ClassificationTypeNames.ExtensionMethodName),
                    [ClassificationTypeNames.OperatorOverloaded] = fontsAndColorsItems.Item(ClassificationTypeNames.OperatorOverloaded),
                    [ClassificationTypeNames.ControlKeyword] = fontsAndColorsItems.Item(ClassificationTypeNames.ControlKeyword),
                    [ClassificationTypeNames.StructName] = fontsAndColorsItems.Item(ClassificationTypeNames.StructName),
                };

                return colorItemMap;
            }

            private void UpdateColorItem(IDictionary<string, ColorableItems> colorItemMap, string classification, uint foreground, uint background = DefaultBackgroundColor)
            {
                colorItemMap[classification].Foreground = foreground;
                colorItemMap[classification].Background = background;
                colorItemMap[classification].Bold = false;
            }

            private bool AreColorsDefaulted(Dictionary<string, ColorableItems> colorItemMap, Guid themeId)
            {
                bool areColorsDefaulted;

                if (themeId == KnownColorThemes.Dark)
                {
                    // Dark Theme
                    // We also check OperatorOverloaded and ControlKeyword for whether they are the PlainText color.
                    // This is because when the "Use Defaults" is invoked from the Fonts and Colors options page the
                    // color reported back will be the PlainText color since these Classifications do not currently have
                    // colors defined in the PKGDEF. The other identifier types do not either but the Identifier color
                    // happens to be the same as PlainText so an extra check isn't necessary. StructName doesn't need
                    // an additional check because it has a color defined in the PKGDEF. The Editor is smart enough
                    // to follow the BaseClassification hierarchy and render the colors appropriately.
                    areColorsDefaulted = IsDefaultForeground(colorItemMap, ClassificationTypeNames.LocalName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ParameterName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.MethodName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ExtensionMethodName, DarkThemeIdentifier) &&
                        (IsDefaultForeground(colorItemMap, ClassificationTypeNames.OperatorOverloaded, DarkThemePlainText) ||
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.OperatorOverloaded, DarkThemeOperator)) &&
                        (IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, DarkThemePlainText) ||
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, DarkThemeKeyword)) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.StructName, DarkThemeClass);
                }
                else
                {
                    // Light or Blue themes
                    // Same as above, we also scheck ControlKeyword for whether it is the PlainText color. OperatorOverload and
                    // the other Identifier types do not need an additional check because their default color is the same
                    // as PlainText.
                    areColorsDefaulted = IsDefaultForeground(colorItemMap, ClassificationTypeNames.LocalName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ParameterName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.MethodName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ExtensionMethodName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.OperatorOverloaded, LightThemeOperator) &&
                        (IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemePlainText) ||
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemeKeyword));
                }

                return areColorsDefaulted;
            }

            private bool IsDefaultForeground(Dictionary<string, ColorableItems> colorItemMap, string classification, uint themeColor)
            {
                var color = colorItemMap[classification].Foreground;

                // The color value isn't always consistent. Sometimes it'll come back as DefaultColor
                // other times it'll come back as the theme color. Just covering all the bases.
                return color == DefaultForegroundColor ||
                    color == AutomaticForegroundColor ||
                    color == themeColor;
            }

            private bool AreColorsEnhanced(Dictionary<string, ColorableItems> colorItemMap, Guid themeId)
            {
                bool areColorsEnhanced;

                if (themeId == KnownColorThemes.Dark)
                {
                    // Dark Theme
                    areColorsEnhanced = colorItemMap[ClassificationTypeNames.LocalName].Foreground == DarkThemeLocalBlue &&
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
                    areColorsEnhanced = colorItemMap[ClassificationTypeNames.LocalName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.ParameterName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.MethodName].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ExtensionMethodName].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.OperatorOverloaded].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ControlKeyword].Foreground == LightThemeControlKeywordPurple;
                }

                return areColorsEnhanced;
            }
        }
    }
}
