using AuxiliaryLibraries.Tools;
using AuxiliaryLibraries.WPF.Wrapper;
using PersonaEditorLib;
using PersonaEditorLib.Sprite;
using PersonaEditorLib.SpriteContainer;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Persona5Rus.Common
{
    internal sealed class TextureImporter
    {
        private string texturePath;

        private Dictionary<string, string> cpkPathToFullPathPNG = new Dictionary<string, string>();

        private Dictionary<string, string> cpkPathToFullPathXML = new Dictionary<string, string>();

        public TextureImporter(string texturePath)
        {
            if (Directory.Exists(texturePath))
            {
                foreach (var file in Directory.EnumerateFiles(texturePath, "*.png", SearchOption.AllDirectories))
                {
                    var cpkPath = IOTools.RelativePath(file, texturePath).ToLower();
                    cpkPathToFullPathPNG[cpkPath] = file;
                }

                foreach (var file in Directory.EnumerateFiles(texturePath, "*.xml", SearchOption.AllDirectories))
                {
                    var cpkPath = IOTools.RelativePath(file, texturePath).ToLower();
                    cpkPathToFullPathXML[cpkPath] = file;
                }
            }
            this.texturePath = texturePath;
        }

        public bool Import(GameFile file, string cpkPath)
        {
            var updated = false;

            var ddss = file.GetAllObjectFiles(FormatEnum.DDS).ToArray();
            foreach (var ddsGF in ddss)
            {
                var dds = ddsGF.GameData as DDS;                
                var cpkDdsPath = Path.Combine(Path.GetDirectoryName(cpkPath), Path.ChangeExtension(ddsGF.Name.Replace('/', '+'), ".png")).ToLower();

                if (!cpkPathToFullPathPNG.TryGetValue(cpkDdsPath, out string pngPath))
                {
                    continue;
                }

                var bitmapSource = AuxiliaryLibraries.WPF.Tools.ImageTools.OpenPNG(pngPath);
                var bitmap = bitmapSource.GetBitmap();
                dds.SetBitmap(bitmap);

                updated |= true;
            }

            var spds = file.GetAllObjectFiles(FormatEnum.SPD).ToArray();
            foreach (var spdGF in spds)
            {
                var spd = spdGF.GameData as SPD;
                var cpkDdsPath = Path.Combine(Path.GetDirectoryName(cpkPath), Path.ChangeExtension(spdGF.Name.Replace('/', '+'), ".xml")).ToLower();

                if (!cpkPathToFullPathXML.TryGetValue(cpkDdsPath, out string xmlPath))
                {
                    continue;
                }

                var xml = XDocument.Load(xmlPath);
                spd.SetTable(xml);

                updated |= true;
            }

            return updated;
        }
    }
}