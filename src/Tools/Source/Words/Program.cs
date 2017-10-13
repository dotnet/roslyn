// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

internal delegate void Handler(Location location, string word);

internal sealed class Location
{
    internal readonly string Path;
    internal readonly int Line;
    internal readonly int Column;

    internal Location(string path, int line, int column)
    {
        Path = path;
        Line = line;
        Column = column;
    }
}

internal struct CountAndLocation
{
    internal readonly int Count;
    internal readonly Location Location;

    internal CountAndLocation(int count, Location location)
    {
        Count = count;
        Location = location;
    }
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: Words <source-path>");
            return;
        }

        var path = Path.GetFullPath(args[0]);
        var words = new Dictionary<string, CountAndLocation> ();
        ReadFiles(
            path,
            path.Length + 1,
            new[] { ".cs", ".vb" },
            (location, word) =>
            {
                if (word.Length < 3)
                {
                    return;
                }
                word = word.ToLower();
                CountAndLocation value;
                value = words.TryGetValue(word, out value) ?
                    new CountAndLocation(value.Count + 1, Merge(value.Location, location)) :
                    new CountAndLocation(1, location);
                words[word] = value;
            });

        var list = words.ToArray();
        Array.Sort(list, (pair1, pair2) => pair1.Key.CompareTo(pair2.Key));
        WriteResults(list);
    }

    // If the file is the same, keep the file but drop the (line, column).
    // If the file is different, drop the location.
    private static Location Merge(Location a, Location b)
    {
        return (a != null && a.Path == b.Path) ? new Location(a.Path, 0, 0) : null;
    }

    private static void WriteResults(KeyValuePair<string, CountAndLocation>[] results)
    {
        foreach (var item in results)
        {
            var word = item.Key;
            var count = item.Value.Count;
            var location = item.Value.Location;
            Console.Write("{0} {1}", word, count);
            if (location != null)
            {
                Console.Write("\t\t\t{0}", location.Path);
                if (location.Line != 0)
                {
                    Console.Write(" ({0}, {1})", location.Line, location.Column);
                }
            }
            Console.WriteLine();
        }
    }

    private static void ReadFiles(string path, int pathLength, string[] extensions, Handler handler)
    {
        foreach (var file in Directory.GetFiles(path))
        {
            var extension = Path.GetExtension(file);
            if (extensions.Contains(extension))
            {
                var text = File.ReadAllText(file);
                ReadWords(file.Substring(pathLength), text, handler);
            }
        }

        foreach (var dir in Directory.GetDirectories(path))
        {
            ReadFiles(dir, pathLength, extensions, handler);
        }
    }

    private static void ReadWords(string path, string text, Handler handler)
    {
        int line = 1;
        int column = 0;
        int i = 0;
        int n = text.Length;
        while (i < n)
        {
            char c = text[i++];
            switch (c)
            {
                case '\n':
                    line++;
                    column = 0;
                    continue;
                case '\r':
                    column = 0;
                    continue;
            }
            column++;
            if (!char.IsLetter(c))
            {
                continue;
            }
            int start = i - 1;
            while (i < n)
            {
                c = text[i];
                if (!char.IsLetter(c))
                {
                    break;
                }
                if (char.IsUpper(c))
                {
                    break;
                }
                i++;
            }
            int length = i - start;
            handler(new Location(path, line, column), text.Substring(start, length));
            column += length - 1;
        }
    }
}
