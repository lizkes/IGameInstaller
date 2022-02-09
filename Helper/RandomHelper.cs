using System;
using System.Linq;

namespace IGameInstaller.Helper
{
    public static class RandomHelper
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        static readonly Random rnd = new();
        public static string GetFixLengthRandomString(int length = 6, string extraChars = "")
        {
            string newChars = chars + extraChars;
            var randomString = new string(Enumerable.Range(0, length).Select(x => newChars[rnd.Next(newChars.Length)]).ToArray());
            return randomString;
        }

        public static string GetRandomVariableLengthString(int min = 4, int max = 10, string extraChars = "")
        {
            string newChars = chars + extraChars;
            var randomString = new string(Enumerable.Range(0, rnd.Next(min, max)).Select(x => newChars[rnd.Next(newChars.Length)]).ToArray());
            return randomString;
        }
    }
}
