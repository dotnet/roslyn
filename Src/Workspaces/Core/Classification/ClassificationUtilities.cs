using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Classification
{
    internal static class ClassificationUtilities
    {
        public static Func<CommonSyntaxNode, List<ISyntaxClassifier>> CreateNodeExtensionGetter(
            this IEnumerable<ISyntaxClassifier> imports)
        {
            var map = new ConcurrentDictionary<Type, List<ISyntaxClassifier>>();
            Func<Type, List<ISyntaxClassifier>> getter =
                t1 =>
                {
                    var query = from i in imports
                                where i.SyntaxNodeTypes != null
                                where !i.SyntaxNodeTypes.Any() || i.SyntaxNodeTypes.Any(t2 => t1 == t2 || t1.IsSubclassOf(t2))
                                select i;

                    return query.ToList();
                };

            return n => map.GetOrAdd(n.GetType(), getter);
        }

        public static Func<CommonSyntaxToken, List<ISyntaxClassifier>> CreateTokenExtensionGetter(
            this IEnumerable<ISyntaxClassifier> imports)
        {
            var map = new ConcurrentDictionary<int, List<ISyntaxClassifier>>();
            Func<int, List<ISyntaxClassifier>> getter =
                k =>
                {
                    var query = from i in imports
                                where i.SyntaxTokenKinds != null
                                where !i.SyntaxTokenKinds.Any() || i.SyntaxTokenKinds.Contains(k)
                                select i;

                    return query.ToList();
                };

            return t => map.GetOrAdd(t.Kind, getter);
        }
    }
}