# NiceIO 
Windows: [![Build status](https://ci.appveyor.com/api/projects/status/63fq5clxy264xb80?svg=true)](https://ci.appveyor.com/project/LucasMeijer/niceio)
OSX: [![Build status](https://travis-ci.org/lucasmeijer/NiceIO.svg?branch=master)](https://travis-ci.org/lucasmeijer/NiceIO)


For when you've had to use System.IO one time too many. 

I need to make c# juggle files & directories around a lot. It has to work on osx, linux and windows. It always hurts, and I've never enjoyed it. NiceIO is an attempt to fix that. It's a single file library, no binaries, no .csproj's, no nuget specs, or any of that. .NET Framework 3.5. Whenever dealing with files makes you cringe, just grab NiceIO.cs, throw it in your project and get on with your business.

This project is in a very early state and the API is very far from stable.

Basic usage:
```c#
//paths are immutable
NPath path1 = new NPath(@"/var/folders/something");
// /var/folders/something

//use back,forward,or trailing slashes,  doesnt matter
NPath path2 = new NPath(@"/var\folders/something///");
// /var/folders/something

//semantically the same
path1 == path2;
// true

// ..'s that are not at the beginning of the path get collapsed
new NPath("/mydir/../myfile.exe");
// /myfile.exe

//build paths
path1.Combine("dir1/dir2");
// /var/folders/something/dir1/dir2

//handy accessors
NPath.HomeDirectory;
// /Users/lucas

//all operations return their destination, so they fluently daisychain
NPath myfile = NPath.HomeDirectory.CreateDirectory("mysubdir").CreateFile("myfile.txt");
// /Users/lucas/mysubdir/myfile.txt

//common operations you know and expect
myfile.Exists();
// true

//you will never again have to look up if .Extension includes the dot or not
myfile.ExtensionWithDot;
// ".txt"

//getting parent directory
NPath dir = myfile.Parent;
// /User/lucas/mysubdir

//copying files,
myfile.Copy("myfile2");
// /Users/lucas/mysubdir/myfile2

//into not-yet-existing directories
myfile.Copy("hello/myfile3");
// /Users/lucas/mysubdir/hello/myfile3

//listing files
dir.Files(recurse:true);
// { /Users/lucas/mysubdir/myfile.txt, 
//   /Users/lucas/mysubdir/myfile2, 
//   /Users/lucas/mysubdir/hello/myfile3 }

//or directories
dir.Directories();
// { /Users/lucas/mysubdir/hello }

//or both
dir.Contents(recurse:true);
// { /Users/lucas/mysubdir/myfile.txt, 
//   /Users/lucas/mysubdir/myfile2, 
//   /Users/lucas/mysubdir/hello/myfile3, 
//   /Users/lucas/mysubdir/hello }

//copy entire directory, and listing everything in the copy
myfile.Parent.Copy("anotherdir").Files(recurse:true);
// { /Users/lucas/anotherdir/myfile, 
//   /Users/lucas/anotherdir/myfile.txt, 
//   /Users/lucas/anotherdir/myfile2, 
//   /Users/lucas/anotherdir/hello/myfile3 }

//easy accesors for common operations:
string text = myfile.ReadAllText();
string[] lines = myfile.ReadAllLines();
myFile.WriteAllText("hello");
myFile.WriteAllLines(new[] { "one", "two"});
```

NiceIO is MIT Licensed.
