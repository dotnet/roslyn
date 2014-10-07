using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Extensions
{
    internal static class SyntaxMetadataExtensions
    {
        public static Func<CommonSyntaxNode, List<TExtension>> CreateNodeExtensionGetter<TExtension>(
            this IEnumerable<TExtension> imports, IExtensionManager extensionManager) where TExtension : ISyntaxMetadata
        {
            var map = new ConcurrentDictionary<Type, List<TExtension>>();
            Func<Type, List<TExtension>> getter =
                t1 =>
                {
                    var query = from i in imports
                                let types = extensionManager.PerformFunction(i, () => i.SyntaxNodeTypes)
                                where types != null
                                where !types.Any() || types.Any(t2 => t1 == t2 || t1.IsSubclassOf(t2))
                                select i;

                    return query.ToList();
                };

            return n => map.GetOrAdd(n.GetType(), getter);
        }

        public static Func<CommonSyntaxToken, List<TExtension>> CreateTokenExtensionGetter<TExtension>(
            this IEnumerable<TExtension> imports, IExtensionManager extensionManager) where TExtension : ISyntaxMetadata
        {
            var map = new ConcurrentDictionary<int, List<TExtension>>();
            Func<int, List<TExtension>> getter =
                k =>
                {
                    var query = from i in imports
                                let kinds = extensionManager.PerformFunction(i, () => i.SyntaxTokenKinds)
                                where kinds != null
                                where !kinds.Any() || kinds.Contains(k)
                                select i;

                    return query.ToList();
                };

            return t => map.GetOrAdd(t.Kind, getter);
        }
    }
}