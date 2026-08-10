// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Completion.Html;

internal static partial class LocalHtmlCompletionProvider
{
    /// <summary>
    /// Provides HTML character entity completion (e.g., &amp;amp;, &amp;nbsp;).
    /// </summary>
    private static class EntityCompletion
    {
        /// <summary>
        /// Determines whether the cursor is in an entity reference context (i.e., preceded by '&amp;'
        /// with no intervening whitespace or semicolons) and computes the replacement range
        /// from '&amp;' through the end of the entity reference (including trailing ';' if present).
        /// </summary>
        public static bool TryGetReplacementRange(SourceText sourceText, int position, [NotNullWhen(true)] out LspRange? range)
        {
            range = default;

            if (position <= 0 || position > sourceText.Length)
            {
                return false;
            }

            // Scan backwards from the cursor for '&'. If we hit whitespace, ';', or '<'/'>' first, it's not an entity.
            var start = -1;
            for (var i = position - 1; i >= 0; i--)
            {
                var ch = sourceText[i];
                if (ch == '&')
                {
                    start = i;
                    break;
                }

                if (char.IsWhiteSpace(ch) || ch == ';' || ch == '<' || ch == '>' || ch == '"' || ch == '\'')
                {
                    return false;
                }
            }

            if (start < 0)
            {
                return false;
            }

            // Find the end: scan forward from cursor for ';' (include it) or stop at word boundary
            var end = position;
            while (end < sourceText.Length)
            {
                var ch = sourceText[end];
                if (ch == ';')
                {
                    end++; // include the semicolon
                    break;
                }

                if (char.IsWhiteSpace(ch) || ch == '<' || ch == '>' || ch == '"' || ch == '\'' || ch == '&')
                {
                    break;
                }

                end++;
            }

            range = sourceText.GetRange(start, end);
            return true;
        }

        /// <summary>
        /// Returns a completion list with entity items.
        /// </summary>
        public static RazorVSInternalCompletionList BuildCompletionList(LspRange range)
        {
            var entities = s_htmlEntities;
            var items = new VSInternalCompletionItem[entities.Length];

            for (var i = 0; i < entities.Length; i++)
            {
                var (name, character) = entities[i];
                var label = "&" + name + "; (" + character + ")";
                var newText = "&" + name + ";";

                items[i] = new VSInternalCompletionItem
                {
                    Label = label,
                    Kind = CompletionItemKind.Constant,
                    FilterText = name,
                    InsertTextFormat = InsertTextFormat.Plaintext,
                    TextEdit = new TextEdit { Range = range, NewText = newText },
                };
            }

            return new RazorVSInternalCompletionList
            {
                Items = items,
                IsIncomplete = false,
            };
        }

        private static readonly (string Name, char Character)[] s_htmlEntities =
        [
            ("quot", '"'),
            ("amp", '&'),
            ("lt", '<'),
            ("gt", '>'),
            ("nbsp", '\u00A0'),
            ("iexcl", (char)161),
            ("cent", (char)162),
            ("pound", (char)163),
            ("curren", (char)164),
            ("yen", (char)165),
            ("brvbar", (char)166),
            ("sect", (char)167),
            ("uml", (char)168),
            ("copy", (char)169),
            ("ordf", (char)170),
            ("laquo", (char)171),
            ("not", (char)172),
            ("shy", (char)173),
            ("reg", (char)174),
            ("macr", (char)175),
            ("deg", (char)176),
            ("plusmn", (char)177),
            ("sup2", (char)178),
            ("sup3", (char)179),
            ("acute", (char)180),
            ("micro", (char)181),
            ("para", (char)182),
            ("middot", (char)183),
            ("cedil", (char)184),
            ("sup1", (char)185),
            ("ordm", (char)186),
            ("raquo", (char)187),
            ("frac14", (char)188),
            ("frac12", (char)189),
            ("frac34", (char)190),
            ("iquest", (char)191),
            ("Agrave", (char)192),
            ("Aacute", (char)193),
            ("Acirc", (char)194),
            ("Atilde", (char)195),
            ("Auml", (char)196),
            ("Aring", (char)197),
            ("AElig", (char)198),
            ("Ccedil", (char)199),
            ("Egrave", (char)200),
            ("Eacute", (char)201),
            ("Ecirc", (char)202),
            ("Euml", (char)203),
            ("Igrave", (char)204),
            ("Iacute", (char)205),
            ("Icirc", (char)206),
            ("Iuml", (char)207),
            ("ETH", (char)208),
            ("Ntilde", (char)209),
            ("Ograve", (char)210),
            ("Oacute", (char)211),
            ("Ocirc", (char)212),
            ("Otilde", (char)213),
            ("Ouml", (char)214),
            ("times", (char)215),
            ("Oslash", (char)216),
            ("Ugrave", (char)217),
            ("Uacute", (char)218),
            ("Ucirc", (char)219),
            ("Uuml", (char)220),
            ("Yacute", (char)221),
            ("THORN", (char)222),
            ("szlig", (char)223),
            ("agrave", (char)224),
            ("aacute", (char)225),
            ("acirc", (char)226),
            ("atilde", (char)227),
            ("auml", (char)228),
            ("aring", (char)229),
            ("aelig", (char)230),
            ("ccedil", (char)231),
            ("egrave", (char)232),
            ("eacute", (char)233),
            ("ecirc", (char)234),
            ("euml", (char)235),
            ("igrave", (char)236),
            ("iacute", (char)237),
            ("icirc", (char)238),
            ("iuml", (char)239),
            ("eth", (char)240),
            ("ntilde", (char)241),
            ("ograve", (char)242),
            ("oacute", (char)243),
            ("ocirc", (char)244),
            ("otilde", (char)245),
            ("ouml", (char)246),
            ("divide", (char)247),
            ("oslash", (char)248),
            ("ugrave", (char)249),
            ("uacute", (char)250),
            ("ucirc", (char)251),
            ("uuml", (char)252),
            ("yacute", (char)253),
            ("thorn", (char)254),
            ("yuml", (char)255),
            ("OElig", (char)338),
            ("oelig", (char)339),
            ("Scaron", (char)352),
            ("scaron", (char)353),
            ("Yuml", (char)376),
            ("fnof", (char)402),
            ("circ", (char)710),
            ("tilde", (char)732),
            ("Alpha", (char)913),
            ("Beta", (char)914),
            ("Gamma", (char)915),
            ("Delta", (char)916),
            ("Epsilon", (char)917),
            ("Zeta", (char)918),
            ("Eta", (char)919),
            ("Theta", (char)920),
            ("Iota", (char)921),
            ("Kappa", (char)922),
            ("Lambda", (char)923),
            ("Mu", (char)924),
            ("Nu", (char)925),
            ("Xi", (char)926),
            ("Omicron", (char)927),
            ("Pi", (char)928),
            ("Rho", (char)929),
            ("Sigma", (char)931),
            ("Tau", (char)932),
            ("Upsilon", (char)933),
            ("Phi", (char)934),
            ("Chi", (char)935),
            ("Psi", (char)936),
            ("Omega", (char)937),
            ("alpha", (char)945),
            ("beta", (char)946),
            ("gamma", (char)947),
            ("delta", (char)948),
            ("epsilon", (char)949),
            ("zeta", (char)950),
            ("eta", (char)951),
            ("theta", (char)952),
            ("iota", (char)953),
            ("kappa", (char)954),
            ("lambda", (char)955),
            ("mu", (char)956),
            ("nu", (char)957),
            ("xi", (char)958),
            ("omicron", (char)959),
            ("pi", (char)960),
            ("rho", (char)961),
            ("sigmaf", (char)962),
            ("sigma", (char)963),
            ("tau", (char)964),
            ("upsilon", (char)965),
            ("phi", (char)966),
            ("chi", (char)967),
            ("psi", (char)968),
            ("omega", (char)969),
            ("thetasym", (char)977),
            ("upsih", (char)978),
            ("piv", (char)982),
            ("ensp", (char)8194),
            ("emsp", (char)8195),
            ("thinsp", (char)8201),
            ("zwsp", (char)8203),
            ("zwnj", (char)8204),
            ("zwj", (char)8205),
            ("lrm", (char)8206),
            ("rlm", (char)8207),
            ("ndash", (char)8211),
            ("mdash", (char)8212),
            ("lsquo", (char)8216),
            ("rsquo", (char)8217),
            ("sbquo", (char)8218),
            ("ldquo", (char)8220),
            ("rdquo", (char)8221),
            ("bdquo", (char)8222),
            ("dagger", (char)8224),
            ("Dagger", (char)8225),
            ("bull", (char)8226),
            ("hellip", (char)8230),
            ("lre", (char)8234),
            ("rle", (char)8235),
            ("pdf", (char)8236),
            ("lro", (char)8237),
            ("rlo", (char)8238),
            ("permil", (char)8240),
            ("prime", (char)8242),
            ("Prime", (char)8243),
            ("lsaquo", (char)8249),
            ("rsaquo", (char)8250),
            ("oline", (char)8254),
            ("frasl", (char)8260),
            ("iss", (char)8298),
            ("ass", (char)8299),
            ("iafs", (char)8300),
            ("aafs", (char)8301),
            ("nads", (char)8302),
            ("nods", (char)8303),
            ("euro", (char)8364),
            ("image", (char)8465),
            ("weierp", (char)8472),
            ("real", (char)8476),
            ("trade", (char)8482),
            ("alefsym", (char)8501),
            ("larr", (char)8592),
            ("uarr", (char)8593),
            ("rarr", (char)8594),
            ("darr", (char)8595),
            ("harr", (char)8596),
            ("crarr", (char)8629),
            ("lArr", (char)8656),
            ("uArr", (char)8657),
            ("rArr", (char)8658),
            ("dArr", (char)8659),
            ("hArr", (char)8660),
            ("forall", (char)8704),
            ("part", (char)8706),
            ("exist", (char)8707),
            ("empty", (char)8709),
            ("nabla", (char)8711),
            ("isin", (char)8712),
            ("notin", (char)8713),
            ("ni", (char)8715),
            ("prod", (char)8719),
            ("sum", (char)8721),
            ("minus", (char)8722),
            ("lowast", (char)8727),
            ("radic", (char)8730),
            ("prop", (char)8733),
            ("infin", (char)8734),
            ("ang", (char)8736),
            ("and", (char)8743),
            ("or", (char)8744),
            ("cap", (char)8745),
            ("cup", (char)8746),
            ("int", (char)8747),
            ("there4", (char)8756),
            ("sim", (char)8764),
            ("cong", (char)8773),
            ("asymp", (char)8776),
            ("ne", (char)8800),
            ("equiv", (char)8801),
            ("le", (char)8804),
            ("ge", (char)8805),
            ("sub", (char)8834),
            ("sup", (char)8835),
            ("nsub", (char)8836),
            ("sube", (char)8838),
            ("supe", (char)8839),
            ("oplus", (char)8853),
            ("otimes", (char)8855),
            ("perp", (char)8869),
            ("sdot", (char)8901),
            ("lceil", (char)8968),
            ("rceil", (char)8969),
            ("lfloor", (char)8970),
            ("rfloor", (char)8971),
            ("lang", (char)9001),
            ("rang", (char)9002),
            ("loz", (char)9674),
            ("spades", (char)9824),
            ("clubs", (char)9827),
            ("hearts", (char)9829),
            ("diams", (char)9830),
        ];
    }
}
