using System.Collections.Generic;

namespace Bender.Apis.WolframAlpha
{
    internal static class Reference
    {
        private static readonly IDictionary<Format, string> FormatToString = new Dictionary<Format, string>
        {
            {Format.Html, "html"},
            {Format.Image, "image"},
            {Format.MathematicaCells, "cell"},
            {Format.MathematicaInput, "minput"},
            {Format.PlainText, "plaintext"},
            {Format.Sound, "sound"}
        };

        public static string GetFormatString(Format format)
        {
            return FormatToString[format];
        }
    }
}
