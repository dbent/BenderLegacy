using System;

namespace Bender.Internal
{
    public static class ColorConsole
    {
        public static void Temp(ConsoleColor foregroundColor, Action action)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = foregroundColor;

            action();

            Console.ForegroundColor = original;
        }
    }
}
