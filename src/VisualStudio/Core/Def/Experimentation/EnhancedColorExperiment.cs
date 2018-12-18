using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
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
    internal class EnhancedColorExperiment : IWpfTextViewConnectionListener
    {
        private const string UseEnhancedColorsFlight = "UseEnhancedColors";
        private const string StopEnhancedColorsFlight = "StopEnhancedColors";
        private const string UseEnhancedColorsSetting = "WindowManagement.Options.UseEnhancedColorsForManagedLanguages";

        private readonly ISettingsManager _settingsManager;
        private readonly EnhancedColorApplier _colorApplier;

        private readonly bool _inUseEnhancedColorsFlight;
        private readonly bool _inStopEnhancedColorsFlight;

        private bool _done;

        [ImportingConstructor]
        private EnhancedColorExperiment([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, ExperimentationService experimentationService)
        {
            _inUseEnhancedColorsFlight = experimentationService?.IsExperimentEnabled(UseEnhancedColorsFlight) ?? false;
            _inStopEnhancedColorsFlight = experimentationService?.IsExperimentEnabled(StopEnhancedColorsFlight) ?? false;

            _colorApplier = new EnhancedColorApplier(serviceProvider);

            _settingsManager = (ISettingsManager)serviceProvider.GetService(typeof(SVsSettingsPersistenceManager));

            // Do not hook settings changed if we have stopped the experiment
            if (_inStopEnhancedColorsFlight)
            {
                return;
            }

            // We need to update the theme whenever the Preview Setting changes or the VS Theme changes.
            _settingsManager.GetSubset(UseEnhancedColorsSetting).SettingChangedAsync += UseEnhancedColorsSettingChangedAsync;
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
        }

        private Task UseEnhancedColorsSettingChangedAsync(object sender, PropertyChangedEventArgs args)
        {
            return Task.Run(() => UpdateThemeColors());
        }

        public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
            // This needs to be scheduled after editor has been composed. Otherwise
            // it may cause UI delays by composing the editor before it is needed
            // by the rest of VS.
            if (!_done)
            {
                _done = true;
                VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.UIThreadIdlePriority, UpdateThemeColors);
            }
        }

        public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
        {
        }

        private void UpdateThemeColors()
        {
            int useEnhancedColorsSetting = 0;

            if (_settingsManager != null)
            {
                useEnhancedColorsSetting = _settingsManager.GetValueOrDefault(name: UseEnhancedColorsSetting, defaultValue: 0);
            }

            // useEnhancedColorsSetting
            //  0 -> use value from flight.
            //  1 -> always use enhanced colors (unless the kill flight is active).
            // -1 -> never use enhanced colors.
            var useFlightValue = useEnhancedColorsSetting == 0;
            var applyEnhancedColors = (useFlightValue && _inUseEnhancedColorsFlight) || useEnhancedColorsSetting == 1;
            var removeEnhancedColors = !applyEnhancedColors || _inStopEnhancedColorsFlight;

            if (removeEnhancedColors)
            {
                _colorApplier.SetDefaultColors();
            }
            else
            {
                _colorApplier.SetEnhancedColors();
            }
        }

        // NOTE: This service is not public or intended for use by teams/individuals outside of Microsoft. Any data stored is subject to deletion without warning.
        [Guid("9B164E40-C3A2-4363-9BC5-EB4039DEF653")]
        private class SVsSettingsPersistenceManager { };

        private class EnhancedColorApplier
        {
            readonly DTE dte;

            // These platform classifications aren't all lowercase
            const string IdentifierClassificationTypeName = "Identifier";
            const string KeywordClassificationTypeName = "Keyword";
            const string OperatorClassificationTypeName = "Operator";

            const uint DefaultForegroundColor = 0x01000000u;
            const uint DefaultBackgroundColor = 0x01000001u;

            const uint AutomaticForegroundColor = 0x02000000u;
            const uint AutomaticBackgroundColor = 0x02000001u;

            // Colors are in 0x00BBGGRR
            const uint DarkThemeIdentifier = 0x00DCDCDCu;
            const uint DarkThemeOperator = 0x00B4B4B4u;
            const uint DarkThemeKeyword = 0x00D69C56u;
            const uint DarkThemeClass = 0x00B0C94Eu;
            const uint DarkThemeLocalBlue = 0x00FEDC9Cu;
            const uint DarkThemeMethodYellow = 0x00AADCDCu;
            const uint DarkThemeControlKeywordPurple = 0x00DFA0D8u;
            const uint DarkThemeStructMint = 0x008CC77Eu;

            const uint LightThemeIdentifier = 0x00000000u;
            const uint LightThemeOperator = 0x00000000u;
            const uint LightThemeKeyword = 0x00FF0000u;
            const uint LightThemeClass = 0x00AF912Bu;
            const uint LightThemeLocalBlue = 0x007F371Fu;
            const uint LightThemeMethodYellow = 0x001F5374u;
            const uint LightThemeControlKeywordPurple = 0x00C4088Fu;

            public EnhancedColorApplier(IServiceProvider serviceProvider)
            {
                dte = (DTE)serviceProvider.GetService(typeof(DTE));
            }

            public void SetDefaultColors()
            {
                var colorItemMap = GetColorItemMap();

                // Only set default colors if the users hasn't customized their colors
                if (!AreColorsEnhanced(colorItemMap))
                {
                    return;
                }

                if (IsDarkTheme())
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

            public void SetEnhancedColors()
            {
                var colorItemMap = GetColorItemMap();

                // Only update colors if the users hasn't customized their colors
                if (!AreColorsDefaulted(colorItemMap))
                {
                    return;
                }

                if (IsDarkTheme())
                {
                    // Dark Theme
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.LocalName, DarkThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ParameterName, DarkThemeLocalBlue);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.MethodName, DarkThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, DefaultForegroundColor);
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
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ExtensionMethodName, DefaultForegroundColor);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.OperatorOverloaded, LightThemeMethodYellow);
                    UpdateColorItem(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemeControlKeywordPurple);
                }
            }

            void UpdateColorItem(IDictionary<string, ColorableItems> colorItemMap, string classification, uint foreground, uint background = DefaultBackgroundColor)
            {
                colorItemMap[classification].Foreground = foreground;
                colorItemMap[classification].Background = background;
            }

            private Dictionary<string, ColorableItems> GetColorItemMap()
            {
                var props = dte.Properties["FontsAndColors", "TextEditor"];
                var prop = props.Item("FontsAndColorsItems");
                var fontsAndColorsItems = (FontsAndColorsItems)prop.Object;

                return Enumerable.Range(1, fontsAndColorsItems.Count)
                    .Select(index => fontsAndColorsItems.Item(index))
                    .ToDictionary(item => item.Name);
            }

            private bool AreColorsDefaulted(Dictionary<string, ColorableItems> colorItemMap)
            {
                if (IsDarkTheme())
                {
                    // Dark Theme
                    return IsDefaultForeground(colorItemMap, IdentifierClassificationTypeName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.LocalName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ParameterName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.MethodName, DarkThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.OperatorOverloaded, DarkThemeOperator) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, DarkThemeKeyword) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.StructName, DarkThemeClass);
                }
                else
                {
                    // Light or Blue themes
                    return IsDefaultForeground(colorItemMap, IdentifierClassificationTypeName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.LocalName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ParameterName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.MethodName, LightThemeIdentifier) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.OperatorOverloaded, LightThemeOperator) &&
                        IsDefaultForeground(colorItemMap, ClassificationTypeNames.ControlKeyword, LightThemeKeyword);
                }
            }

            private bool AreColorsEnhanced(Dictionary<string, ColorableItems> colorItemMap)
            {
                if (IsDarkTheme())
                {
                    // Dark Theme
                    return IsDefaultForeground(colorItemMap, IdentifierClassificationTypeName, DarkThemeIdentifier) &&
                        colorItemMap[ClassificationTypeNames.LocalName].Foreground == DarkThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.ParameterName].Foreground == DarkThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.MethodName].Foreground == DarkThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.OperatorOverloaded].Foreground == DarkThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ControlKeyword].Foreground == DarkThemeControlKeywordPurple &&
                        colorItemMap[ClassificationTypeNames.StructName].Foreground == DarkThemeStructMint;
                }
                else
                {
                    // Light or Blue themes
                    return IsDefaultForeground(colorItemMap, IdentifierClassificationTypeName, LightThemeIdentifier) &&
                        colorItemMap[ClassificationTypeNames.LocalName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.ParameterName].Foreground == LightThemeLocalBlue &&
                        colorItemMap[ClassificationTypeNames.MethodName].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.OperatorOverloaded].Foreground == LightThemeMethodYellow &&
                        colorItemMap[ClassificationTypeNames.ControlKeyword].Foreground == LightThemeControlKeywordPurple;
                }
            }

            private bool IsDefaultForeground(Dictionary<string, ColorableItems> colorItemMap, string classification, uint themeColor)
            {
                var color = colorItemMap[classification].Foreground;
                return color == DefaultForegroundColor ||
                    color == AutomaticForegroundColor ||
                    color == 0 ||
                    color == themeColor;
            }

            private bool IsDarkTheme()
            {
                const string DarkThemeGuid = "1ded0138-47ce-435e-84ef-9ec1f439b749";
                return GetThemeId() == DarkThemeGuid;
            }

            public string GetThemeId()
            {
                try
                {
                    var currentTheme = dte.Properties["Environment", "General"].Item("SelectedTheme").Value;
                    var themeId = currentTheme.GetType().GetProperty("ThemeId").GetValue(currentTheme);
                    return themeId.ToString();
                }
                catch
                {
                    var keyName = $@"Software\Microsoft\VisualStudio\{dte.Version}\ApplicationPrivateSettings\Microsoft\VisualStudio";
                    using (var key = Registry.CurrentUser.OpenSubKey(keyName))
                    {
                        var keyText = key?.GetValue("ColorTheme", string.Empty) as string;
                        if (string.IsNullOrEmpty(keyText))
                        {
                            return null;
                        }

                        var keyTextValues = keyText.Split('*');
                        if (keyTextValues.Length < 3)
                        {
                            return null;
                        }

                        return keyTextValues[2];
                    }
                }
            }
        }
    }
}
