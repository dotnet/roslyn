// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// There are many ways to compare XML documents.  This class aims to provide functionality somewhere
    /// between a straight string comparison and a fully-configurable XML tree comparison.  In particular,
    /// given a shallow comparer (i.e. one that does not consider children), it will compare the root elements
    /// and, if they are equal, match up children by shallow equality, recursing on each pair.
    /// </summary>
    public static class AssertXml
    {
        public static void Equal(string expected, string actual)
        {
            Equal(XElement.Parse(expected), XElement.Parse(actual), message: null, expectedValueSourcePath: null, expectedValueSourceLine: 0, expectedIsXmlLiteral: true);
        }

        public static void Equal(XElement expected, XElement actual)
        {
            Equal(expected, actual, message: null, expectedValueSourcePath: null, expectedValueSourceLine: 0, expectedIsXmlLiteral: false);
        }

        /// <summary>
        /// Compare two XElements.  Assumed to be non-null.
        /// </summary>
        public static void Equal(
            XElement expectedRoot,
            XElement actualRoot,
            string message,
            string expectedValueSourcePath,
            int expectedValueSourceLine,
            bool expectedIsXmlLiteral)
        {
            if (!CheckEqual(expectedRoot, actualRoot, ShallowElementComparer.Instance, out var firstMismatch))
            {
                Assert.True(false, message +
                    GetAssertText(
                        GetXmlString(expectedRoot, expectedIsXmlLiteral),
                        GetXmlString(actualRoot, expectedIsXmlLiteral),
                        expectedRoot,
                        firstMismatch,
                        expectedValueSourcePath,
                        expectedValueSourceLine,
                        expectedIsXmlLiteral));
            }
        }

        private static string GetXmlString(XElement node, bool expectedIsXmlLiteral)
        {
            using (var sw = new StringWriter(CultureInfo.InvariantCulture))
            {
                var ws = new XmlWriterSettings()
                {
                    IndentChars = expectedIsXmlLiteral ? "    " : "  ",
                    OmitXmlDeclaration = true,
                    Indent = true
                };

                using (var w = XmlWriter.Create(sw, ws))
                {
                    node.WriteTo(w);
                }

                return sw.ToString();
            }
        }

        /// <summary>
        /// Helpful diff output message.  Can be printed as either an XML literal (VB) or a string literal (C#).
        /// </summary>
        private static string GetAssertText(
            string expected,
            string actual,
            XElement expectedRoot,
            Tuple<XElement, XElement> firstMismatch,
            string expectedValueSourcePath,
            int expectedValueSourceLine,
            bool expectedIsXmlLiteral)
        {
            StringBuilder assertText = new StringBuilder();

            string actualString = expectedIsXmlLiteral ? actual.Replace(" />\r\n", "/>\r\n") : string.Format("@\"{0}\"", actual.Replace("\"", "\"\""));
            string expectedString = expectedIsXmlLiteral ? expected.Replace(" />\r\n", "/>\r\n") : string.Format("@\"{0}\"", expected.Replace("\"", "\"\""));

            if (AssertEx.TryGenerateExpectedSourceFileAndGetDiffLink(actualString, expectedString.Count(c => c == '\n') + 1, expectedValueSourcePath, expectedValueSourceLine, out var link))
            {
                assertText.AppendLine(link);
            }
            else
            {
                assertText.AppendLine("Expected");
                assertText.AppendLine("====");
                assertText.AppendLine(expectedString);
                assertText.AppendLine();

                if (firstMismatch.Item1 != expectedRoot)
                {
                    assertText.AppendLine("First Difference");
                    assertText.AppendLine("====");
                    assertText.AppendLine("Expected Fragment");
                    assertText.AppendLine("----");
                    assertText.AppendLine(firstMismatch.Item1.ToString());
                    assertText.AppendLine();
                    assertText.AppendLine("Actual Fragment");
                    assertText.AppendLine("----");
                    assertText.AppendLine(firstMismatch.Item2.ToString());
                    assertText.AppendLine();
                }

                assertText.AppendLine("Actual");
                assertText.AppendLine("====");
                assertText.AppendLine(actualString);
                assertText.AppendLine();
            }

            return assertText.ToString();
        }

        /// <summary>
        /// Compare the root elements and, if they are equal, match up children by shallow equality, recursing on each pair.
        /// </summary>
        /// <returns>True if the elements are equal, false otherwise (in which case, firstMismatch will try to indicate a point of disagreement).</returns>
        private static bool CheckEqual(XElement expectedRoot, XElement actualRoot, IEqualityComparer<XElement> shallowComparer, out Tuple<XElement, XElement> firstMismatch)
        {
            Assert.NotNull(expectedRoot);
            Assert.NotNull(actualRoot);
            Assert.NotNull(shallowComparer);

            Tuple<XElement, XElement> rootPair = new Tuple<XElement, XElement>(expectedRoot, actualRoot);

            if (!shallowComparer.Equals(expectedRoot, actualRoot))
            {
                firstMismatch = rootPair;
                return false;
            }

            Stack<Tuple<XElement, XElement>> stack = new Stack<Tuple<XElement, XElement>>();
            stack.Push(rootPair);

            while (stack.Count > 0)
            {
                Tuple<XElement, XElement> pair = stack.Pop();
                firstMismatch = pair; // Will be overwritten if this pair is a match.
                Debug.Assert(shallowComparer.Equals(pair.Item1, pair.Item2)); // Shouldn't have been pushed otherwise.

                XElement[] children1 = pair.Item1.Elements().ToArray();
                MultiDictionary<XElement, XElement> children2Dict = new MultiDictionary<XElement, XElement>(shallowComparer);

                int children2Count = 0;
                foreach (XElement child in pair.Item2.Elements())
                {
                    children2Dict.Add(child, child);
                    children2Count++;
                }

                if (children1.Length != children2Count)
                {
                    return false;
                }


                HashSet<XElement> children2Used = new HashSet<XElement>(ReferenceEqualityComparer.Instance);
                foreach (XElement child1 in children1)
                {
                    XElement child2 = null;
                    foreach (var candidate in children2Dict[child1])
                    {
                        if (!children2Used.Contains(candidate))
                        {
                            child2 = candidate;
                            break;
                        }
                    }

                    if (child2 == null)
                    {
                        return false;
                    }
                    else
                    {
                        children2Used.Add(child2);
                        stack.Push(new Tuple<XElement, XElement>(child1, child2));
                    }
                }

                if (children2Used.Count < children1.Length)
                {
                    return false;
                }
            }

            firstMismatch = null;
            return true;
        }

        private class ShallowElementComparer : IEqualityComparer<XElement>
        {
            public static readonly IEqualityComparer<XElement> Instance = new ShallowElementComparer();

            private ShallowElementComparer() { }

            public bool Equals(XElement element1, XElement element2)
            {
                Assert.NotNull(element1);
                Assert.NotNull(element2);

                return element1.Name == "customDebugInfo"
                    ? element1.ToString() == element2.ToString()
                    : AssertXml.NameAndAttributeComparer.Instance.Equals(element1, element2);
            }

            public int GetHashCode(XElement element)
            {
                return element.Name.GetHashCode();
            }
        }

        /// <summary>
        /// Convenience shallow element comparer.  Checks names and attribute name-value pairs (ignoring order).
        /// </summary>
        private class NameAndAttributeComparer : IEqualityComparer<XElement>
        {
            public static readonly IEqualityComparer<XElement> Instance = new NameAndAttributeComparer();

            private NameAndAttributeComparer() { }

            public bool Equals(XElement element1, XElement element2)
            {
                Assert.NotNull(element1);
                Assert.NotNull(element2);

                if (element1.Name != element2.Name)
                {
                    return false;
                }

                IEnumerable<Tuple<XName, string>> attributes1 = element1.Attributes().Select(MakeAttributeTuple);
                IEnumerable<Tuple<XName, string>> attributes2 = element2.Attributes().Select(MakeAttributeTuple);

                return attributes1.SetEquals(attributes2);
            }

            public int GetHashCode(XElement element)
            {
                return element.Name.GetHashCode();
            }

            private static Tuple<XName, string> MakeAttributeTuple(XAttribute attribute)
            {
                return new Tuple<XName, string>(attribute.Name, attribute.Value);
            }
        }
    }
}
