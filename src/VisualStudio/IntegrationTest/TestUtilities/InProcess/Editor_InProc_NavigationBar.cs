// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal partial class Editor_InProc
    {
        public string GetSelectedNavBarItem(int comboBoxIndex)
            =>  ExecuteOnActiveView(v => GetNavigationBarComboBoxes(v)[comboBoxIndex].SelectedItem?.ToString());

        public string[] GetNavBarItems(int comboBoxIndex)
            => ExecuteOnActiveView(v =>
                GetNavigationBarComboBoxes(v)[comboBoxIndex]
                .Items
                .OfType<object>()
                .Select(i => i?.ToString() ?? "")
                .ToArray());

        public int GetNavbarItemIndex(int index, string itemText)
        {
            int FindItem(ComboBox comboBox)
            {
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    if (comboBox.Items[i].ToString() == itemText)
                    {
                        return i;
                    }
                }

                return -1;
            }

            return ExecuteOnActiveView(v => FindItem(GetNavigationBarComboBoxes(v)[index]));
        }

        public void ExpandNavigationBar(int index)
        {
            ExecuteOnActiveView(v =>
            {
                var combobox = GetNavigationBarComboBoxes(v)[index];
                combobox.Focus();
                combobox.IsDropDownOpen = true;
            });
        }

        public void SelectNavBarItem(int comboboxIndex, string selection)
        {
            var itemIndex = GetNavbarItemIndex(comboboxIndex, selection);
            if (itemIndex < 0)
            {
                throw new ArgumentException($"Could not find {selection} in combobox");
            }

            ExpandNavigationBar(comboboxIndex);
            System.Windows.Forms.SendKeys.SendWait("{HOME}");
            for (int i = 0; i < itemIndex; i++)
            {
                System.Windows.Forms.SendKeys.SendWait("{DOWN}");
            }
            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
        }

        public bool IsNavBarEnabled()
            => ExecuteOnActiveView(v => GetNavbar(v) != null);

        private List<ComboBox> GetNavigationBarComboBoxes(IWpfTextView textView)
        {
            var margin = GetNavbar(textView);
            List<ComboBox> combos = margin.GetFieldValue<List<ComboBox>>("_combos");
            return combos;
        }

        private static UIElement GetNavbar(IWpfTextView textView)
        {
            var control = textView.VisualElement;
            while (control != null)
            {
                if (control.GetType().Name == "WpfMultiViewHost")
                {
                    break;
                }

                control = VisualTreeHelper.GetParent(control) as FrameworkElement;
            }

            var topMarginControl = control.GetPropertyValue<ContentControl>("TopMarginControl");
            var vsDropDownBarAdapterMargin = topMarginControl.Content as UIElement;
            return vsDropDownBarAdapterMargin;
        }
    }

    internal static class ReflectionExtensions
    {
        public static PropertyType GetPropertyValue<PropertyType>(this object instance, string propertyName)
        {
            return (PropertyType)GetPropertyValue(instance, propertyName);
        }

        public static object GetPropertyValue(this object instance, string propertyName)
        {
            Type type = instance.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo == null)
            {
                throw new ArgumentException("Property " + propertyName + " was not found on type " + type.ToString());
            }
            object result = propertyInfo.GetValue(instance, null);
            return result;
        }

        public static object GetFieldValue(this object instance, string fieldName)
        {
            Type type = instance.GetType();
            FieldInfo fieldInfo = null;
            while (type != null)
            {
                fieldInfo = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    break;
                }
                type = type.BaseType;
            }

            if (fieldInfo == null)
            {
                throw new FieldAccessException("Field " + fieldName + " was not found on type " + type.ToString());
            }
            object result = fieldInfo.GetValue(instance);
            return result; // you can place a breakpoint here (for debugging purposes)
        }

        public static FieldType GetFieldValue<FieldType>(this object instance, string fieldName)
        {
            return (FieldType)GetFieldValue(instance, fieldName);
        }
    }
}
