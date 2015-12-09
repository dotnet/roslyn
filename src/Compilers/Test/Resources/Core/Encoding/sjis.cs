using System.IO;
using System.Text;

class プラネテス
{
    public static void Main()
    {
        File.WriteAllText("output.txt", "星野 八郎太", Encoding.GetEncoding(932));
    }
}
