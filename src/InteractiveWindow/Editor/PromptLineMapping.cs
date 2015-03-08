// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.VisualStudio.InteractiveWindow
{
    /// <summary>
    /// Maps line numbers in projection buffer to indices of projection spans corresponding to primary and stdin prompts.
    /// </summary>
    internal sealed class PromptLineMapping
    {
        private List<KeyValuePair<int, int>> _map = new List<KeyValuePair<int, int>>();

        /// <summary>
        /// If true the map might not be consistent with projection spans.
        /// Used to work around the lack of an editor feature that would allow us 
        /// to edit a subject buffer and projection spans atomically.
        /// </summary>
        public bool IsInconsistentWithProjections { get; set; }

        public PromptLineMapping()
        {
        }

        public void Clear()
        {
            _map = new List<KeyValuePair<int, int>>();
        }

        public int Count
        {
            get { return _map.Count; }
        }

        public void Add(int lineNumber, int projectionIndex)
        {
            _map.Add(new KeyValuePair<int, int>(lineNumber, projectionIndex));
        }

        public void RemoveLast()
        {
            _map.RemoveAt(_map.Count - 1);
        }

        public KeyValuePair<int, int> this[int index]
        {
            get { return _map[index]; }
            set { _map[index] = value; }
        }

        /// <summary>
        /// Binary search for a prompt located on given line number. 
        /// If no prompt is on the given line number returns the closest preceding prompt.
        /// </summary>
        /// <returns>An index in the prompt line map.</returns>
        internal int GetMappingIndexByLineNumber(int lineNumber)
        {
            int start = 0;
            int end = _map.Count - 1;
            while (true)
            {
                Debug.Assert(start <= end);

                int mid = start + ((end - start) >> 1);
                int key = _map[mid].Key;

                if (lineNumber == key)
                {
                    return mid;
                }

                if (mid == start)
                {
                    Debug.Assert(start == end || start == end - 1);
                    return (lineNumber >= _map[end].Key) ? end : mid;
                }

                if (lineNumber > key)
                {
                    start = mid;
                }
                else
                {
                    end = mid;
                }
            }
        }

        [Conditional("DEBUG")]
        public void Dump(string name)
        {
            Debug.Write("PLM (" + name + "): ");
            foreach (var plm in _map)
            {
                Debug.Write(string.Format("{0} -> {1}; ", plm.Key, plm.Value));
            }

            Debug.WriteLine("");
        }
    }
}
