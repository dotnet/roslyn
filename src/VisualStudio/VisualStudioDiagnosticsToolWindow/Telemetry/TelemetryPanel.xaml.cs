// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Telemetry
{
    /// <summary>
    /// Interaction logic for TelemetryPanel.xaml
    /// </summary>
    public partial class TelemetryPanel : UserControl
    {
        public TelemetryPanel()
        {
            InitializeComponent();
        }

        private async void OnDump(object sender, RoutedEventArgs e)
        {
            using (Disable(DumpButton))
            using (Disable(CopyButton))
            {
                GenerationProgresBar.IsIndeterminate = true;

                var text = await Task.Run(() => GetTelemetryString()).ConfigureAwait(true);
                this.Result.Text = text;

                GenerationProgresBar.IsIndeterminate = false;
            }
        }

        private void OnCopy(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(this.Result.Text);
        }

        private string GetTelemetryString()
        {
            var fixAllScopeValues = Enum.GetValues(typeof(FixAllScope));

            var sb = new StringBuilder();
            var seenType = new HashSet<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var result = ScanAssembly(assembly);
                if (result.Length > 0)
                {
                    sb.AppendLine($"Searching: {assembly.FullName}");
                    sb.AppendLine(result);
                }
            }

            return sb.ToString();

            string ScanAssembly(Assembly assembly)
            {
                var typeDiscovered = new StringBuilder();
                try
                {
                    foreach (var module in assembly.GetModules())
                    {
                        foreach (var type in module.GetTypes())
                        {
                            ScanType(type, typeDiscovered);
                        }
                    }
                }
                catch
                {
                    // ignore
                }

                return typeDiscovered.ToString();
            }

            void ScanType(Type type, StringBuilder typeDiscovered)
            {
                type = type.GetTypeForTelemetry();

                if (!seenType.Add(type))
                {
                    return;
                }

                RecordIfCodeAction(type, typeDiscovered);

                foreach (var nestedTypeInfo in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    ScanType(nestedTypeInfo, typeDiscovered);
                }
            }

            void RecordIfCodeAction(Type type, StringBuilder typeDiscovered)
            {
                if (!IsCodeAction(type))
                {
                    return;
                }

                var telemetryId = type.GetTelemetryId();

                typeDiscovered.AppendLine($"Found: {type.FullName}: {telemetryId.ToString()}");
            }

            bool IsCodeAction(Type type)
            {
                if (type == null)
                {
                    return false;
                }

                var codeActionType = typeof(CodeAction);
                return codeActionType.IsAssignableFrom(type);
            }
        }

        private IDisposable Disable(UIElement control)
        {
            control.IsEnabled = false;
            return new RAII(() => control.IsEnabled = true);
        }

        private sealed class RAII : IDisposable
        {
            private readonly Action _action;

            public RAII(Action disposeAction)
            {
                _action = disposeAction;
            }
            public void Dispose()
            {
                _action?.Invoke();
            }
        }
    }
}
