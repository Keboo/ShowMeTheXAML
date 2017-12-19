namespace ShowMeTheXAML.Tests
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndings(this string @string)
        {
            return @string.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }
}