// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Input
{
    public static class ButtonBaseExtensions
    {
        private static readonly MethodInfo s_executeCoreMethod;

        static ButtonBaseExtensions()
        {
            var methodInfo = typeof(RoutedCommand).GetMethod("ExecuteCore", BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(IInputElement), typeof(bool) }, null);
            s_executeCoreMethod = methodInfo;
            //s_executeCore = (Action<RoutedCommand, object, IInputElement, bool>)Delegate.CreateDelegate(typeof(Action<RoutedCommand, object, IInputElement, bool>), firstArgument: null, methodInfo);
        }

        public static async Task<bool> SimulateClickAsync(this ButtonBase button, JoinableTaskFactory joinableTaskFactory)
        {
            await joinableTaskFactory.SwitchToMainThreadAsync();

            if (!button.IsEnabled || !button.IsVisible)
            {
                return false;
            }

            if (button is RadioButton radioButton)
            {
                ISelectionItemProvider peer = new RadioButtonAutomationPeer(radioButton);
                peer.Select();
            }
            else if (button is Button button2)
            {
                IInvokeProvider peer = new ButtonAutomationPeer(button2);
                peer.Invoke();
            }
            else
            {
                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                ExecuteCommandSource(button, true);
            }

            // Wait for changes to propagate
            await Task.Yield();

            return true;
        }

        private static void ExecuteCommandSource(ICommandSource commandSource, bool userInitiated)
        {
            var command = commandSource.Command;
            if (command is null)
            {
                return;
            }

            var commandParameter = commandSource.CommandParameter;
            var commandTarget = commandSource.CommandTarget;
            if (command is RoutedCommand routedCommand)
            {
                if (commandTarget is null)
                {
                    commandTarget = commandSource as IInputElement;
                }

                if (routedCommand.CanExecute(commandParameter, commandTarget))
                {
                    s_executeCoreMethod.Invoke(routedCommand, new[] { commandParameter, commandTarget, userInitiated });
                    //s_executeCore(routedCommand, commandParameter, commandTarget, userInitiated);
                }
            }
            else if (command.CanExecute(commandParameter))
            {
                command.Execute(commandParameter);
            }
        }
    }
}
