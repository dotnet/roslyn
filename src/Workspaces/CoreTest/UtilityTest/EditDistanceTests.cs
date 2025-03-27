// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class EditDistanceTests
{
    private static void VerifyEditDistance(string s, string t, int expectedEditDistance)
    {
        // We want the full edit distance, without bailing out early because we crossed the
        // threshold.
        var editDistance1 = EditDistance.GetEditDistance(s, t);
        Assert.Equal(expectedEditDistance, editDistance1);

        // Edit distances are symmetric.
        var editDistance2 = EditDistance.GetEditDistance(t, s);
        Assert.Equal(editDistance1, editDistance2);

        // If we set hte edit distance as our threshold, we should still find the value.
        var editDistance3 = EditDistance.GetEditDistance(s, t, editDistance1);
        Assert.Equal(editDistance1, editDistance3);

        if (editDistance1 > 0)
        {
            var editDistance4 = EditDistance.GetEditDistance(s, t, editDistance1 - 1);
            Assert.Equal(EditDistance.BeyondThreshold, editDistance4);
        }
    }

    [Fact]
    public void EditDistance0()
    {
        VerifyEditDistance("", "", 0);
        VerifyEditDistance("a", "a", 0);
    }

    [Fact]
    public void EditDistance1()
    {
        VerifyEditDistance("", "a", 1);
        VerifyEditDistance("a", "", 1);
        VerifyEditDistance("a", "b", 1);
        VerifyEditDistance("ab", "a", 1);
        VerifyEditDistance("a", "ab", 1);
        VerifyEditDistance("aabb", "abab", 1);
    }

    [Fact]
    public void EditDistance2()
    {
        VerifyEditDistance("", "aa", 2);
        VerifyEditDistance("aa", "", 2);
        VerifyEditDistance("aa", "bb", 2);
        VerifyEditDistance("aab", "a", 2);
        VerifyEditDistance("a", "aab", 2);
        VerifyEditDistance("aababb", "ababab", 2);
    }

    [Fact]
    public void EditDistance3()
    {
        VerifyEditDistance("", "aaa", 3);
        VerifyEditDistance("aaa", "", 3);
        VerifyEditDistance("aaa", "bbb", 3);
        VerifyEditDistance("aaab", "a", 3);
        VerifyEditDistance("a", "aaab", 3);
        VerifyEditDistance("aababbab", "abababaa", 3);
    }

    [Fact]
    public void EditDistance4()
        => VerifyEditDistance("XlmReade", "XmlReader", 2);

    [Fact]
    public void EditDistance5()
        => VerifyEditDistance("Zeil", "trials", 4);

    [Fact]
    public void EditDistance6()
        => VerifyEditDistance("barking", "corkliness", 6);

    [Fact]
    public void EditDistance7()
        => VerifyEditDistance("kitten", "sitting", 3);

    [Fact]
    public void EditDistance8()
        => VerifyEditDistance("sunday", "saturday", 3);

    [Fact]
    public void EditDistance9()
        => VerifyEditDistance("meilenstein", "levenshtein", 4);

    [Fact]
    public void EditDistance10()
        => VerifyEditDistance("rosettacode", "raisethysword", 8);

    [Fact]
    public void EditDistance11()
    {
        var editDistance = EditDistance.GetEditDistance("book", "moons", 1);
        Assert.Equal(EditDistance.BeyondThreshold, editDistance);
        VerifyEditDistance("book", "moons", 3);
    }

    [Fact]
    public void EditDistance12()
    {
        VerifyEditDistance("aaaab", "aaabc", 2);
        VerifyEditDistance("aaaab", "aabcc", 3);
        VerifyEditDistance("aaaab", "abccc", 4);
        VerifyEditDistance("aaaab", "bcccc", 5);

        VerifyEditDistance("aaaabb", "aaabbc", 2);
        VerifyEditDistance("aaaabb", "aabbcc", 4);
        VerifyEditDistance("aaaabb", "abbccc", 5);
        VerifyEditDistance("aaaabb", "bbcccc", 6);

        VerifyEditDistance("aaaabbb", "aaabbbc", 2);
        VerifyEditDistance("aaaabbb", "aabbbcc", 4);
        VerifyEditDistance("aaaabbb", "abbbccc", 6);
        VerifyEditDistance("aaaabbb", "bbbcccc", 7);

        VerifyEditDistance("aaaabbbb", "aaabbbbc", 2);
        VerifyEditDistance("aaaabbbb", "aabbbbcc", 4);
        VerifyEditDistance("aaaabbbb", "abbbbccc", 6);
        VerifyEditDistance("aaaabbbb", "bbbbcccc", 8);
    }

    public static readonly string[] Top1000 =
#pragma warning disable format // https://github.com/dotnet/roslyn/issues/70711 tracks removing this suppression.
        [
            "a","able","about","above","act","add","afraid","after","again","against","age","ago","agree","air","all",
            "allow","also","always","am","among","an","and","anger","animal","answer","any","appear","apple","are",
            "area","arm","arrange","arrive","art","as","ask","at","atom","baby","back","bad","ball","band","bank",
            "bar","base","basic","bat","be","bear","beat","beauty","bed","been","before","began","begin","behind",
            "believe","bell","best","better","between","big","bird","bit","black","block","blood","blow","blue","board",
            "boat","body","bone","book","born","both","bottom","bought","box","boy","branch","bread","break","bright",
            "bring","broad","broke","brother","brought","brown","build","burn","busy","but","buy","by","call","came",
            "camp","can","capital","captain","car","card","care","carry","case","cat","catch","caught","cause","cell",
            "cent","center","century","certain","chair","chance","change","character","charge","chart","check","chick",
            "chief","child","children","choose","chord","circle","city","claim","class","clean","clear","climb","clock",
            "close","clothe","cloud","coast","coat","cold","collect","colony","color","column","come","common","company",
            "compare","complete","condition","connect","consider","consonant","contain","continent","continue","control",
            "cook","cool","copy","corn","corner","correct","cost","cotton","could","count","country","course","cover",
            "cow","crease","create","crop","cross","crowd","cry","current","cut","dad","dance","danger","dark","day",
            "dead","deal","dear","death","decide","decimal","deep","degree","depend","describe","desert","design",
            "determine","develop","dictionary","did","die","differ","difficult","direct","discuss","distant","divide",
            "division","do","doctor","does","dog","dollar","done","dont","door","double","down","draw","dream","dress",
            "drink","drive","drop","dry","duck","during","each","ear","early","earth","ease","east","eat","edge",
            "effect","egg","eight","either","electric","element","else","end","enemy","energy","engine","enough",
            "enter","equal","equate","especially","even","evening","event","ever","every","exact","example","except",
            "excite","exercise","expect","experience","experiment","eye","face","fact","fair","fall","family","famous",
            "far","farm","fast","fat","father","favor","fear","feed","feel","feet","fell","felt","few","field","fig",
            "fight","figure","fill","final","find","fine","finger","finish","fire","first","fish","fit","five","flat",
            "floor","flow","flower","fly","follow","food","foot","for","force","forest","form","forward","found",
            "four","fraction","free","fresh","friend","from","front","fruit","full","fun","game","garden","gas","gather",
            "gave","general","gentle","get","girl","give","glad","glass","go","gold","gone","good","got","govern",
            "grand","grass","gray","great","green","grew","ground","group","grow","guess","guide","gun","had","hair",
            "half","hand","happen","happy","hard","has","hat","have","he","head","hear","heard","heart","heat","heavy",
            "held","help","her","here","high","hill","him","his","history","hit","hold","hole","home","hope","horse",
            "hot","hour","house","how","huge","human","hundred","hunt","hurry","i","ice","idea","if","imagine","in",
            "inch","include","indicate","industry","insect","instant","instrument","interest","invent","iron","is",
            "island","it","job","join","joy","jump","just","keep","kept","key","kill","kind","king","knew","know",
            "lady","lake","land","language","large","last","late","laugh","law","lay","lead","learn","least","leave",
            "led","left","leg","length","less","let","letter","level","lie","life","lift","light","like","line","liquid",
            "list","listen","little","live","locate","log","lone","long","look","lost","lot","loud","love","low",
            "machine","made","magnet","main","major","make","man","many","map","mark","market","mass","master","match",
            "material","matter","may","me","mean","meant","measure","meat","meet","melody","men","metal","method",
            "middle","might","mile","milk","million","mind","mine","minute","miss","mix","modern","molecule","moment",
            "money","month","moon","more","morning","most","mother","motion","mount","mountain","mouth","move","much",
            "multiply","music","must","my","name","nation","natural","nature","near","necessary","neck","need","neighbor",
            "never","new","next","night","nine","no","noise","noon","nor","north","nose","note","nothing","notice",
            "noun","now","number","numeral","object","observe","occur","ocean","of","off","offer","office","often",
            "oh","oil","old","on","once","one","only","open","operate","opposite","or","order","organ","original",
            "other","our","out","over","own","oxygen","page","paint","pair","paper","paragraph","parent","part","particular",
            "party","pass","past","path","pattern","pay","people","perhaps","period","person","phrase","pick","picture",
            "piece","pitch","place","plain","plan","plane","planet","plant","play","please","plural","poem","point",
            "poor","populate","port","pose","position","possible","post","pound","power","practice","prepare","present",
            "press","pretty","print","probable","problem","process","produce","product","proper","property","protect",
            "prove","provide","pull","push","put","quart","question","quick","quiet","quite","quotient","race","radio",
            "rail","rain","raise","ran","range","rather","reach","read","ready","real","reason","receive","record",
            "red","region","remember","repeat","reply","represent","require","rest","result","rich","ride","right",
            "ring","rise","river","road","rock","roll","room","root","rope","rose","round","row","rub","rule","run",
            "safe","said","sail","salt","same","sand","sat","save","saw","say","scale","school","science","score",
            "sea","search","season","seat","second","section","see","seed","seem","segment","select","self","sell",
            "send","sense","sent","sentence","separate","serve","set","settle","seven","several","shall","shape",
            "share","sharp","she","sheet","shell","shine","ship","shoe","shop","shore","short","should","shoulder",
            "shout","show","side","sight","sign","silent","silver","similar","simple","since","sing","single","sister",
            "sit","six","size","skill","skin","sky","slave","sleep","slip","slow","small","smell","smile","snow",
            "so","soft","soil","soldier","solution","solve","some","son","song","soon","sound","south","space","speak",
            "special","speech","speed","spell","spend","spoke","spot","spread","spring","square","stand","star","start",
            "state","station","stay","stead","steam","steel","step","stick","still","stone","stood","stop","store",
            "story","straight","strange","stream","street","stretch","string","strong","student","study","subject",
            "substance","subtract","success","such","sudden","suffix","sugar","suggest","suit","summer","sun","supply",
            "support","sure","surface","surprise","swim","syllable","symbol","system","table","tail","take","talk",
            "tall","teach","team","teeth","tell","temperature","ten","term","test","than","thank","that","the","their",
            "them","then","there","these","they","thick","thin","thing","think","third","this","those","though","thought",
            "thousand","three","through","throw","thus","tie","time","tiny","tire","to","together","told","tone",
            "too","took","tool","top","total","touch","toward","town","track","trade","train","travel","tree","triangle",
            "trip","trouble","truck","true","try","tube","turn","twenty","two","type","under","unit","until","up",
            "us","use","usual","valley","value","vary","verb","very","view","village","visit","voice","vowel","wait",
            "walk","wall","want","war","warm","was","wash","watch","water","wave","way","we","wear","weather","week",
            "weight","well","went","were","west","what","wheel","when","where","whether","which","while","white",
            "who","whole","whose","why","wide","wife","wild","will","win","wind","window","wing","winter","wire",
            "wish","with","woman","women","wonder","wont","wood","word","work","world","would","write","written",
            "wrong","wrote","yard","year","yellow","yes","yet","you","young","your",
        ];
#pragma warning restore format

    [Fact]
    public void Top1000Test()
    {
        for (var i = 0; i < Top1000.Length; i++)
        {
            var source = Top1000[i];
            for (var j = 0; j < Top1000.Length; j++)
            {
                var target = Top1000[j];
                var editDistance1 = EditDistance.GetEditDistance(source, target);

                if (i == j)
                {
                    Assert.Equal(0, editDistance1);
                }

                if (editDistance1 == 0)
                {
                    Assert.Equal(i, j);
                }

                Assert.True(editDistance1 >= 0);

                var editDistance2 = EditDistance.GetEditDistance(source, target, editDistance1);
                Assert.Equal(editDistance1, editDistance2);
            }
        }
    }

    [Fact]
    public void TestSpecificMetric()
    {
        // If our edit distance is a metric then ED(CA,ABC) = 2 because CA -> AC -> ABC
        // In this case.  This then satisfies the triangle inequality because 
        // ED(CA, AC) + ED(AC, ABC) >= ED(CA, ABC)   ...   1 + 1 >= 2
        //
        // If it's not implemented with a metric (like if we used the Optimal String Alignment
        // algorithm), then the we could get an edit distance of 3 "CA -> A -> AB -> ABC".  
        // This violates the triangle inequality rule because: 
        // 
        // OSA(CA,AC) + OSA(AC,ABC) >= OSA(CA,ABC)  ...   1 + 1 >= 3    is not true.
        //
        // Being a metric is important so that we can properly use this with BKTrees.
        VerifyEditDistance("CA", "ABC", 2);
    }

    [Fact]
    public void TestTriangleInequality()
    {
        var top = Top1000.Take(50).ToArray();

        for (var i = 0; i < top.Length; i++)
        {
            for (var j = 0; j < top.Length; j++)
            {
                if (j == i)
                {
                    continue;
                }

                for (var k = 0; k < top.Length; k++)
                {
                    if (k == i || k == j)
                    {
                        continue;
                    }

                    var string1 = top[i];
                    var string2 = top[j];
                    var string3 = top[k];

                    var editDistance12 = EditDistance.GetEditDistance(string1, string2);
                    var editDistance13 = EditDistance.GetEditDistance(string1, string3);
                    var editDistance23 = EditDistance.GetEditDistance(string2, string3);

                    Assert.True(editDistance13 <= editDistance12 + editDistance23);
                }
            }
        }
    }
}
