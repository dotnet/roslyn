var prTitle = System.Environment.GetEnvironmentVariable("ghprbPullTitle");
var escaped = String.Join("", prTitle.Where(c => char.IsLetterOrDigit(c) || c == ' '));
Console.Write(escaped);
