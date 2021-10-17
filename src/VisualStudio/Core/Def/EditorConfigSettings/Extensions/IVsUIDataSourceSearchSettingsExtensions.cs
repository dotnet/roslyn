// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.SearchSettings
{
    internal static class IVsUIDataSourceSearchSettingsExtensions
    {
        /// <summary>
        /// Sets whether the search control will display MRU items in the drop-down popup.
        /// Default=True.
        /// </summary>
        public static void UseMRU(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.SearchUseMRU, value);

        /// <summary>
        /// Sets the Thickness of the padding around the search box.
        /// Default="0".
        /// </summary>
        public static void ControlBorderThickness(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.ControlBorderThickness, value);

        /// <summary>
        /// Sets the Thickness of the search control's border.
        /// Default="1".
        /// </summary>
        public static void ControlPaddingThickness(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.ControlPaddingThickness, value);

        /// <summary>
        /// Sets The guid of the theme to use as default/fixed theme when UseDefaultThemeColors is set.
        /// This is usually necessary when the search control is hosted in a dialog whose colors don't change when the IDE theme changes.
        /// Default="{DE3DBBCD-F642-433C-8353-8F1DF4370ABA}"
        /// </summary>
        public static void DefaultTheme(this IVsUIDataSource source, Guid value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.DefaultTheme, value.ToString());

        /// <summary>
        /// Sets whether the search control should only use the colors of the default theme.
        /// This is usually set to true when the search control is hosted in a dialog whose colors don't change when the IDE theme changes.
        /// Default=False.
        /// </summary>
        public static void UseDefaultThemeColors(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.UseDefaultThemeColors, value);

        /// <summary>
        /// Sets whether the search control forwards the enter key event after search is started. 
        /// Default=False.
        /// </summary>
        public static void ForwardEnterKeyOnSearchStart(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.ForwardEnterKeyOnSearchStart, value);
        /// <summary>
        /// Sets whether the search button is visible in the search control. 
        /// Default=True.
        /// </summary>
        public static void SearchButtonVisible(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.SearchButtonVisible, value);

        /// <summary>
        /// Sets the minimum width of the search control's popup.
        /// Default=200.
        /// </summary>
        public static void ControlMinPopupWidth(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.ControlMinPopupWidth, value);

        /// <summary>
        /// Sets the maximum width of the search control.
        /// Default=400.
        /// </summary>
        public static void ControlMaxWidth(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, value);

        /// <summary>
        /// Sets the minimum width of the search control.
        /// Default=100.
        /// </summary>
        public static void ControlMinWidth(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.ControlMinWidth, value);

        /// <summary>
        /// Sets the tooltip for the search button after a search is complete.
        /// Default="Clear search".
        /// </summary>
        public static void SearchClearTooltip(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.SearchClearTooltip, value);

        /// <summary>
        /// Sets the tooltip for the search button while the search is performed.
        /// Default="Stop search".
        /// </summary>
        public static void SearchStopTooltip(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.SearchStopTooltip, value);

        /// <summary>
        /// Sets the tooltip for the search button before starting the search.
        /// Default="Search".
        /// </summary>
        public static void SearchStartTooltip(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.SearchStartTooltip, value);

        /// <summary>
        /// Sets an ARGB background color for the HwndSource.
        /// This setting is ignored if it is 0, or if the search control is parented under a WPF element.
        /// Default=0.
        /// </summary>
        public static void HwndSourceBackgroundColor(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.HwndSourceBackgroundColor, value);

        /// <summary>
        /// Sets the tooltip for the search edit box.
        /// Default="Type words to search for".
        /// </summary>
        public static void SearchTooltip(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.SearchTooltip, value);

        /// <summary>
        /// Sets whether the search watermark remains even when the control is focused.
        /// Default=False.
        /// </summary>
        public static void SearchWatermarkWhenFocused(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.SearchWatermarkWhenFocused, value);

        /// <summary>
        /// the string displayed in the search box when it's empty and doesn't have the focus.
        /// Default="Search".
        /// </summary>
        public static void SearchWatermark(this IVsUIDataSource source, string value)
            => SetStringValue(source, SearchSettingsDataSource.PropertyNames.SearchWatermark, value);

        /// <summary>
        /// the delay in milliseconds after a search is automatically started after which the search popup is automatically closed.
        /// Default=4000.
        /// </summary>
        public static void SearchPopupCloseDelay(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchPopupCloseDelay, value);

        /// <summary>
        /// Sets whether the search popup is automatically shown on typing (for delayed and on-demand searches only).
        /// Default=True.
        /// </summary>
        public static void SearchPopupAutoDropdown(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.SearchPopupAutoDropdown, value);

        /// <summary>
        /// Sets the delay in milliseconds from the search start after which the progress indicator is displayed.
        /// This allows fast searches to complete without showing progress. 
        /// Default=200.
        /// </summary>
        public static void SearchProgressShowDelay(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchProgressShowDelay, value);

        /// <summary>
        /// Sets the progress type supported by the window search.
        /// Default=SPT_INDETERMINATE.
        /// </summary>
        public static void SearchProgressType(this IVsUIDataSource source, VSSEARCHPROGRESSTYPE value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchProgressType, (int)value);

        /// <summary>
        /// Sets whether the search string has whitespaces trimmed from beginning and end before starting a search or adding the item to MRU list.
        /// Default=True.
        /// </summary>
        public static void SearchTrimsWhitespaces(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.SearchTrimsWhitespaces, value);

        /// <summary>
        /// Sets whether the search will be restarted on pressing Enter or selecting MRU item from the list, even if the search string is not changed.
        /// Default=False.
        /// </summary>
        public static void RestartSearchIfUnchanged(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.RestartSearchIfUnchanged, value);

        /// <summary>
        /// Sets the minimum number of characters that have relevance for the window search.
        /// The window host will wait for the user to type at least the min number of characters before calling IVsWindowSearch to start a new search. 
        /// Default=1.
        /// </summary>
        public static void SearchStartMinChars(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchStartMinChars, value);

        /// <summary>
        /// Sets the delay in milliseconds after which a search starts automatically (for delayed search type).
        /// Default=1000.
        /// </summary>
        public static void SearchStartDelay(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchStartDelay, value);

        /// <summary>
        /// Sets the search start type (instant/delayed/ondemand).
        /// Default=SST_DELAYED.
        /// </summary>
        public static void SearchStartType(this IVsUIDataSource source, VSSEARCHSTARTTYPE value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.SearchStartType, (int)value);

        /// <summary>
        /// Sets the maximum number of MRU items to show in the popup.
        /// Default=5.
        /// </summary>
        public static void MaximumMRUItems(this IVsUIDataSource source, int value)
            => SetIntValue(source, SearchSettingsDataSource.PropertyNames.MaximumMRUItems, value);

        /// <summary>
        /// Sets whether or not the search MRU list is filtered by prefix based on what's currently typed in the search box.
        /// Default=True.
        /// </summary>
        public static void PrefixFilterMRUItems(this IVsUIDataSource source, bool value)
            => SetBoolValue(source, SearchSettingsDataSource.PropertyNames.PrefixFilterMRUItems, value);

        private static void SetBoolValue(IVsUIDataSource source, string property, bool value)
        {
            var valueProp = BuiltInPropertyValue.FromBool(value);
            _ = source.SetValue(property, valueProp);
        }

        private static void SetIntValue(IVsUIDataSource source, string property, int value)
        {
            var valueProp = BuiltInPropertyValue.Create(value);
            _ = source.SetValue(property, valueProp);
        }

        private static void SetStringValue(IVsUIDataSource source, string property, string value)
        {
            var valueProp = BuiltInPropertyValue.Create(value);
            _ = source.SetValue(property, valueProp);
        }
    }
}
