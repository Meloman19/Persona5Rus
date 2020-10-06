using PersonaEditorLib;
using System.IO;

namespace Persona5Rus
{
    internal static class Global
    {
        public static string ApplicationDirectory { get; } = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

        public static string TempDirectory { get; } = Path.Combine(ApplicationDirectory, "Temp");
        public static string OutputDirectory { get; } = Path.Combine(ApplicationDirectory, "Output");
        public static string DataDirectory = Path.Combine(ApplicationDirectory, "Data");
        public static string FontDirectory { get; } = Path.Combine(ApplicationDirectory, "Font");

        private static PersonaEncoding p5EngEncoding = null;
        private static PersonaEncoding p5JapEncoding = null;
        private static PersonaEncoding p5RusEncoding = null;

        private static PersonaEncoding oldEncoding = null;
        private static PersonaEncoding newEncoding = null;
        private static PersonaFont newFont = null;

        public static string OldFontName { get; set; } = "P5_ENG";
        public static string NewFontName { get; set; } = "P5_RUS";

        public static PersonaEncoding P5EngEncoding()
        {
            if (p5EngEncoding == null)
                p5EngEncoding = new PersonaEncoding(Path.Combine(FontDirectory, "P5_ENG.fntmap"));
            return p5EngEncoding;
        }

        public static PersonaEncoding P5JapEncoding()
        {
            if (p5JapEncoding == null)
                p5JapEncoding = new PersonaEncoding(Path.Combine(FontDirectory, "P5_JAP.fntmap"));
            return p5JapEncoding;
        }

        public static PersonaEncoding P5RusEncoding()
        {
            if (p5RusEncoding == null)
                p5RusEncoding = new PersonaEncoding(Path.Combine(FontDirectory, "P5_RUS.fntmap"));
            return p5RusEncoding;
        }

        public static PersonaEncoding OldEncoding()
        {
            if (oldEncoding == null)
                oldEncoding = new PersonaEncoding(Path.Combine(FontDirectory, OldFontName + ".fntmap"));
            return oldEncoding;
        }

        public static PersonaEncoding NewEncoding()
        {
            if (newEncoding == null)
                newEncoding = new PersonaEncoding(Path.Combine(FontDirectory, NewFontName + ".fntmap"));
            return newEncoding;
        }

        public static PersonaFont NewFont()
        {
            if (newFont == null)
                newFont = new PersonaFont(Path.Combine(FontDirectory, NewFontName + ".fnt"));
            return newFont;
        }
    }
}