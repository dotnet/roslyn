string file = Args[0];
string hexSearch = Args[1];
string hexReplace = Args[2];

File.WriteAllBytes(
    Args[0],
    BitConverter.ToString(File.ReadAllBytes(Args[0]))
                .Replace(hexSearch, hexReplace)
                .Split('-')
                .Select(s => Convert.ToByte(s, 16))
                .ToArray());
            


