using AuxiliaryLibraries.Tools;
using AuxiliaryLibraries.WPF.Wrapper;
using PersonaEditorLib;
using PersonaEditorLib.Sprite;
using System;
using System.Collections.Generic;
using System.IO;

namespace Persona5Rus.Common
{
    internal sealed class TextureImporter
    {
        private string texturePath;

        private Dictionary<string, string> cpkPathToFullPathPNG = new Dictionary<string, string>();

        public TextureImporter(string texturePath)
        {
            if (Directory.Exists(texturePath))
            {
                foreach (var file in Directory.EnumerateFiles(texturePath, "*.png", SearchOption.AllDirectories))
                {
                    var cpkPath = IOTools.RelativePath(file, texturePath);
                    cpkPathToFullPathPNG[cpkPath] = file;
                }
            }
            this.texturePath = texturePath;
        }

        public bool Import(GameFile file, string cpkPath)
        {
            if (file.GameData is DDS dds)
            {
                var cpkDdsPath = Path.ChangeExtension(cpkPath, ".png");

                if(!cpkPathToFullPathPNG.TryGetValue(cpkDdsPath, out string pngPath))
                {
                    return false;
                }

                var bitmapSource = AuxiliaryLibraries.WPF.Tools.ImageTools.OpenPNG(pngPath);
                var bitmap = bitmapSource.GetBitmap();
                dds.SetBitmap(bitmap);

                return true;
            }

            return false;
        }
    }
}