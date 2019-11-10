// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Roslyn.Utilities
{
    internal static class XmlUtilities
    {
        internal static TNode Copy<TNode>(this TNode node, bool copyAttributeAnnotations)
            where TNode : XNode
        {
            XNode copy;

            // Documents can't be added to containers, so our usual copy trick won't work.
            if (node.NodeType == XmlNodeType.Document)
            {
                copy = new XDocument(((XDocument)(object)node));
            }
            else
            {
                XContainer temp = new XElement("temp");
                temp.Add(node);
                copy = temp.LastNode;
                temp.RemoveNodes();
            }

            Debug.Assert(copy != node);
            Debug.Assert(copy.Parent == null); // Otherwise, when we give it one, it will be copied.

            // Copy annotations, the above doesn't preserve them.
            // We need to preserve Location annotations as well as line position annotations.
            CopyAnnotations(node, copy);

            // We also need to preserve line position annotations for all attributes
            // since we report errors with attribute locations.
            if (copyAttributeAnnotations && node.NodeType == XmlNodeType.Element)
            {
                var sourceElement = ((XElement)(object)node);
                var targetElement = ((XElement)copy);

                IEnumerator<XAttribute> sourceAttributes = sourceElement.Attributes().GetEnumerator();
                IEnumerator<XAttribute> targetAttributes = targetElement.Attributes().GetEnumerator();
                while (sourceAttributes.MoveNext() && targetAttributes.MoveNext())
                {
                    Debug.Assert(sourceAttributes.Current.Name == targetAttributes.Current.Name);
                    CopyAnnotations(sourceAttributes.Current, targetAttributes.Current);
                }
            }

            return (TNode)copy;
        }

        private static void CopyAnnotations(XObject source, XObject target)
        {
            foreach (var annotation in source.Annotations<object>())
            {
                target.AddAnnotation(annotation);
            }
        }

        internal static XElement[]? TrySelectElements(XNode node, string xpath, out string? errorMessage, out bool invalidXPath)
        {
            errorMessage = null;
            invalidXPath = false;

            try
            {
                var xpathResult = System.Xml.XPath.Extensions.XPathSelectElements(node, xpath);

                // Throws InvalidOperationException if the result of the XPath is an XDocument:
                return xpathResult?.ToArray();
            }
            catch (InvalidOperationException e)
            {
                errorMessage = e.Message;
                return null;
            }
            catch (Exception e) when (e.GetType().FullName == "System.Xml.XPath.XPathException")
            {
                errorMessage = e.Message;
                invalidXPath = true;
                return null;
            }
        }
    }
}
