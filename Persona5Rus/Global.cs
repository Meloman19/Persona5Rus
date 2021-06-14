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

        private static PersonaEncoding ps3P5EngEncoding = null;
        private static PersonaEncoding ps3P5JapEncoding = null;
        private static PersonaEncoding ps3P5RusEncoding = null;
        private static PersonaEncoding ps4P5RusEncoding = null;

        private static PersonaFont ps3P5Font = null;
        private static PersonaFont ps4P5Font = null;

        private const string PS3_P5_ENG = "PS3_P5_ENG";
        private const string PS3_P5_JAP = "PS3_P5_JAP";
        private const string PS3_P5_RUS = "PS3_P5_RUS";
        private const string PS4_P5_RUS = "PS4_P5_RUS";

        public static PersonaEncoding PS3_P5EngEncoding()
        {
            if (ps3P5EngEncoding == null)
                ps3P5EngEncoding = new PersonaEncoding(Path.Combine(FontDirectory, PS3_P5_ENG + ".fntmap"));
            return ps3P5EngEncoding;
        }

        public static PersonaEncoding PS3_P5JapEncoding()
        {
            if (ps3P5JapEncoding == null)
                ps3P5JapEncoding = new PersonaEncoding(Path.Combine(FontDirectory, PS3_P5_JAP + ".fntmap"));
            return ps3P5JapEncoding;
        }

        public static PersonaEncoding PS3_P5RusEncoding()
        {
            if (ps3P5RusEncoding == null)
                ps3P5RusEncoding = new PersonaEncoding(Path.Combine(FontDirectory, PS3_P5_RUS + ".fntmap"));
            return ps3P5RusEncoding;
        }

        public static PersonaEncoding PS4_P5RusEncoding()
        {
            if (ps4P5RusEncoding == null)
                ps4P5RusEncoding = new PersonaEncoding(Path.Combine(FontDirectory, PS4_P5_RUS + ".fntmap"));
            return ps4P5RusEncoding;
        }

        public static PersonaFont PS3_P5RusFont()
        {
            if (ps3P5Font == null)
                ps3P5Font = new PersonaFont(Path.Combine(FontDirectory, PS3_P5_RUS + ".fnt"));
            return ps3P5Font;
        }

        public static PersonaFont PS4_P5RusFont()
        {
            if (ps4P5Font == null)
                ps4P5Font = new PersonaFont(Path.Combine(FontDirectory, PS4_P5_RUS + ".fnt"));
            return ps4P5Font;
        }
    }
}