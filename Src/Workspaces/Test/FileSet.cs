// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class FileSet : IEnumerable<KeyValuePair<string, object>>
    {
        private readonly Dictionary<string, object> nameToContentMap;

        public FileSet(IEnumerable<KeyValuePair<string, object>> nameToContentMap)
        {
            this.nameToContentMap = nameToContentMap.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return this.nameToContentMap.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public FileSet WithFile(string fileName, object content)
        {
            return new FileSet(this.nameToContentMap.Where(kvp => kvp.Key != fileName).Concat(new KeyValuePair<string, object>(fileName, content)));
        }

        public FileSet ReplaceFileElement(string fileName, string elementName, string elementValue)
        {
            object content;
            if (this.nameToContentMap.TryGetValue(fileName, out content))
            {
                var textContent = content as string;
                if (textContent != null)
                {
                    var elementStartTag = "<" + elementName;
                    var elementEndTag = "</" + elementName;
                    var startTagStart = textContent.IndexOf(elementStartTag);
                    if (startTagStart >= -1)
                    {
                        var startTagEnd = textContent.IndexOf('>', startTagStart + 1);
                        if (startTagEnd >= startTagStart)
                        {
                            var endTagStart = textContent.IndexOf(elementEndTag, startTagEnd + 1);
                            if (endTagStart >= startTagEnd)
                            {
                                var newContent = textContent.Substring(0, startTagEnd + 1) + elementValue + textContent.Substring(endTagStart);
                                return this.WithFile(fileName, newContent);
                            }
                        }
                    }
                }
            }

            return this;
        }
    }
}