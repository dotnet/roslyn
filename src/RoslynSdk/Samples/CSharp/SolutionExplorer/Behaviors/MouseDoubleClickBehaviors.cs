using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MSBuildWorkspaceTester.Behaviors
{
    public static class MouseDoubleClickBehaviors
    {
        public static DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                name: "Command",
                propertyType: typeof(ICommand),
                ownerType: typeof(MouseDoubleClickBehaviors),
                defaultMetadata: new UIPropertyMetadata(
                    propertyChangedCallback: CommandChanged));

        public static ICommand GetCommand(DependencyObject obj)
            => (ICommand)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand value)
            => obj.SetValue(CommandProperty, value);

        public static DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                name: "CommandParameter",
                propertyType: typeof(object),
                ownerType: typeof(MouseDoubleClickBehaviors),
                defaultMetadata: new UIPropertyMetadata(
                    defaultValue: null,
                    propertyChangedCallback: CommandParameterChanged));

        private static void CommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public static object GetCommandParameter(DependencyObject obj)
            => obj.GetValue(CommandParameterProperty);

        public static void SetCommandParameter(DependencyObject obj, object value)
            => obj.SetValue(CommandParameterProperty, value);

        private static void CommandChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is Control control)
            {
                if (e.NewValue != null && e.OldValue == null)
                {
                    control.MouseDoubleClick += OnMouseDoubleClick;
                }
                else if (e.NewValue == null && e.OldValue != null)
                {
                    control.MouseDoubleClick -= OnMouseDoubleClick;
                }

                void OnMouseDoubleClick(object sender, RoutedEventArgs args)
                {
                    ICommand command = (ICommand)control.GetValue(CommandProperty);
                    object commandParameter = control.GetValue(CommandParameterProperty);
                    command.Execute(commandParameter);
                }
            }
        }
    }
}
