using System.Collections.Generic;
using System.ComponentModel;

namespace MSBuildWorkspaceTester.Framework
{
    internal static class PropertyChangedEventArgsCache
    {
        private static readonly Dictionary<string, PropertyChangedEventArgs> s_eventArgsCache
            = new Dictionary<string, PropertyChangedEventArgs>();

        public static PropertyChangedEventArgs GetEventArgs(string propertyName)
        {
            lock (s_eventArgsCache)
            {
                if (!s_eventArgsCache.TryGetValue(propertyName, out PropertyChangedEventArgs eventArgs))
                {
                    eventArgs = new PropertyChangedEventArgs(propertyName);
                    s_eventArgsCache.Add(propertyName, eventArgs);
                }

                return eventArgs;
            }
        }
    }
}
