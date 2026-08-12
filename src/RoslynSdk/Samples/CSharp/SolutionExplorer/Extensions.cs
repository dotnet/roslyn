using System.Windows;

namespace MSBuildWorkspaceTester
{
    internal static class Extensions
    {
        public static T FindName<T>(this FrameworkElement element, string name)
        {
            return (T)element.FindName(name);
        }
    }
}
