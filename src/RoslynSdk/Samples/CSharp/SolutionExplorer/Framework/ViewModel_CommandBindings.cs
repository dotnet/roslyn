using System;
using System.Windows;
using System.Windows.Input;

namespace MSBuildWorkspaceTester.Framework
{
    internal partial class ViewModel
    {
        private CommandBindingCollection _commandBindings;
        private readonly object _gate = new object();

        private void AddCommandBinding(CommandBinding binding)
        {
            CommandManager.RegisterClassCommandBinding(GetType(), binding);

            lock (_gate)
            {
                if (_commandBindings == null)
                {
                    _commandBindings = new CommandBindingCollection();
                }

                _commandBindings.Add(binding);
            }
        }

        private ICommand RegisterCommand(
            string text, string name, InputGesture[] inputGestures,
            ExecutedRoutedEventHandler executed,
            CanExecuteRoutedEventHandler canExecute)
        {
            RoutedUICommand command = new RoutedUICommand(text, name, GetType(), new InputGestureCollection(inputGestures));
            CommandBinding binding = new CommandBinding(command, executed, canExecute);

            AddCommandBinding(binding);

            return command;
        }

        protected ICommand RegisterCommand(string text, string name, Action executed, Func<bool> canExecute, params InputGesture[] inputGestures)
        {
            return RegisterCommand(text, name, inputGestures,
                executed: (s, e) => executed(),
                canExecute: (s, e) => e.CanExecute = canExecute());
        }

        protected ICommand RegisterCommand<T>(string text, string name, Action<T> executed, Func<T, bool> canExecute, params InputGesture[] inputGestures)
        {
            T cast(object x) => x != null ? (T)x : default;

            return RegisterCommand(text, name, inputGestures,
                executed: (s, e) => executed(cast(e.Parameter)),
                canExecute: (s, e) => e.CanExecute = canExecute(cast(e.Parameter)));
        }

        public static readonly DependencyProperty RegisterCommandsProperty =
            DependencyProperty.RegisterAttached(
                name: "RegisterCommands",
                propertyType: typeof(ViewModel),
                ownerType: typeof(ViewModel),
                defaultMetadata: new PropertyMetadata(
                    defaultValue: null,
                    propertyChangedCallback: (dp, e) =>
                    {
                        if (dp is UIElement element && e.NewValue is ViewModel viewModel)
                        {
                            lock (viewModel._gate)
                            {
                                if (viewModel._commandBindings != null)
                                {
                                    element.CommandBindings.AddRange(viewModel._commandBindings);
                                }
                            }
                        }
                    }));

        public static ViewModel GetRegisterCommands(UIElement element)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            return element.GetValue(RegisterCommandsProperty) as ViewModel;
        }

        public static void SetRegisterCommands(UIElement element, ViewModel value)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            element.SetValue(RegisterCommandsProperty, value);
        }
    }
}
