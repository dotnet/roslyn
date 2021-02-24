// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using System;
using System.Xml.Linq;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A program location in an XML file.
    /// </summary>
    internal class XmlLocation : Location, IEquatable<XmlLocation?>
    {
        private readonly FileLinePositionSpan _positionSpan;

        private XmlLocation(string path, int lineNumber, int columnNumber)
        {
            LinePosition start = new LinePosition(lineNumber, columnNumber);
            LinePosition end = new LinePosition(lineNumber, columnNumber + 1);
            _positionSpan = new FileLinePositionSpan(path, start, end);
        }

        public static XmlLocation Create(XmlException exception, string path)
        {
            // Convert to 0-indexed (special case - sometimes 0,0).
            int lineNumber = Math.Max(exception.LineNumber - 1, 0);
            int columnNumber = Math.Max(exception.LinePosition - 1, 0);

            return new XmlLocation(path, lineNumber, columnNumber);
        }

        public static XmlLocation Create(XObject obj, string path)
        {
            IXmlLineInfo xmlLineInfo = obj;
            Debug.Assert(xmlLineInfo.LinePosition != 0);

            // Convert to 0-indexed (special case - sometimes 0,0).
            int lineNumber = Math.Max(xmlLineInfo.LineNumber - 1, 0);
            int columnNumber = Math.Max(xmlLineInfo.LinePosition - 1, 0);

            return new XmlLocation(path, lineNumber, columnNumber);
        }

        public override LocationKind Kind
        {
            get
            {
                return LocationKind.XmlFile;
            }
        }

        public override FileLinePositionSpan GetLineSpan()
        {
            return _positionSpan;
        }

        public bool Equals(XmlLocation? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other != null && other._positionSpan.Equals(_positionSpan);
        }

        public override bool Equals(object? obj)
        {
            return this.Equals(obj as XmlLocation);
        }

        public override int GetHashCode()
        {
            return _positionSpan.GetHashCode();
        }
    }
}
