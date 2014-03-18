// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// There are many ways to compare XML documents.  This class aims to provide functionality somewhere
    /// between a straight string comparison and a fully-configurable XML tree comparison.  In particular,
    /// given a shallow comparer (i.e. one that does not consider children), it will compare the root elements
    /// and, if they are equal, match up children by shallow equality, recursing on each pair.
    /// </summary>
    public static class XmlElementDiff
    {
        /// <summary>
        /// Compare two XElements.  Assumed to be non-null.
        /// </summary>
        public static void AssertEqual(XElement expectedRoot, XElement actualRoot, IEqualityComparer<XElement> shallowComparer)
        {
            Tuple<XElement, XElement> firstMismatch;
            if (!CheckEqual(expectedRoot, actualRoot, shallowComparer, out firstMismatch))
            {
                Assert.True(false, GetAssertText(expectedRoot.ToString(), actualRoot.ToString(), expectedRoot, firstMismatch, expectedAndActualAsCSharpString: false));
            }
        }

        /// <summary>
        /// Parse two strings as XElements and compare the results.  Assumed to be well-formed XML.
        /// </summary>
        public static void AssertEqual(string expected, string actual, IEqualityComparer<XElement> shallowComparer)
        {
            Assert.False(string.IsNullOrEmpty(expected));
            Assert.False(string.IsNullOrEmpty(actual));

            XElement expectedRoot = XElement.Parse(expected);
            XElement actualRoot = XElement.Parse(actual);

            Tuple<XElement, XElement> firstMismatch;
            if (!CheckEqual(expectedRoot, actualRoot, shallowComparer, out firstMismatch))
            {
                Assert.True(false, GetAssertText(expected, actual, expectedRoot, firstMismatch, expectedAndActualAsCSharpString: true));
            }
        }

        /// <summary>
        /// Helpful diff output message.  Can be printed as either an XML literal (VB) or a string literal (C#).
        /// </summary>
        private static string GetAssertText(string expected, string actual, XElement expectedRoot, Tuple<XElement, XElement> firstMismatch, bool expectedAndActualAsCSharpString)
        {
            StringBuilder assertText = new StringBuilder();
            
            assertText.AppendLine("Expected");
            assertText.AppendLine("====");
            assertText.AppendLine(expectedAndActualAsCSharpString ? string.Format("@\"{0}\"", expected.Replace("\"", "\"\"")) : expected);
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
            assertText.AppendLine(expectedAndActualAsCSharpString ? string.Format("@\"{0}\"", actual.Replace("\"", "\"\"")) : actual);
            assertText.AppendLine();

            return assertText.ToString();
        }

        /// <summary>
        /// Compare the root elements and, if they are equal, match up children by shallow equality, recursing on each pair.
        /// </summary>
        /// <returns>True if the elements are equal, false otherwise (in which case, firstMismatch will try to indicate a point of disagreement).</returns>
        public static bool CheckEqual(XElement expectedRoot, XElement actualRoot, IEqualityComparer<XElement> shallowComparer, out Tuple<XElement, XElement> firstMismatch)
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
                    IEnumerable<XElement> candidates2;
                    if (children2Dict.TryGetMultipleValues(child1, out candidates2))
                    {
                        XElement child2 = candidates2.FirstOrDefault(candidate => !children2Used.Contains(candidate));
                        if (child2 == null)
                        {
                            return false;
                        }
                        children2Used.Add(child2);
                        stack.Push(new Tuple<XElement, XElement>(child1, child2));
                    }
                    else
                    {
                        return false;
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

        /// <summary>
        /// Convenience shallow element comparer.  Checks names and attribute name-value pairs (ignoring order).
        /// </summary>
        public class NameAndAttributeComparer : IEqualityComparer<XElement>
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