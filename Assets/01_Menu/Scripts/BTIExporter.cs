using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using GameFormatReader.Common;
using UnityEngine;
using WiiExplorer;

public class BTIExporter : MonoBehaviour
{
    public string archiveDirectory = "Assets/GameFiles/res/FieldMap";
    public string exportDirectory = @"C:\Users\finne\Desktop\ExportedUI";

    void Start()
    {
        ExportBTIFiles();
    }

    void ExportBTIFiles()
    {
        string[] arcFiles = Directory.GetFiles(archiveDirectory, "*.arc", SearchOption.AllDirectories);
        foreach (string arcFile in arcFiles)
        {
            Archive archive = ArcReader.Read(arcFile);
            foreach (ArcFile file in archive.Files)
            {
                if (file.Name.EndsWith(".bti"))
                {
                    byte[] btiBuffer = file.Buffer;
                    Debug.Log("Exporting... " + archive.Name + " ::: " + file.Name);
                    
                    if(file.Name.Equals("im_zelda_+button_text_00.bti")) continue;
                    
                    MemoryStream FS = new MemoryStream(btiBuffer);
                    bool compressed = MemoryStreamEx.ReadString(FS, 4) == "Yaz0";
                    FS.Close();

                    if (compressed) btiBuffer = YAZ0.Decompress(btiBuffer);

                    BinaryTextureImage compressedTex = new BinaryTextureImage();
                    using (EndianBinaryReader reader = new EndianBinaryReader(btiBuffer, Endian.Big))
                    {
                        compressedTex.Load(reader, 0, 0);
                    }

                    string path = exportDirectory +"/" + archive.Name + "_" + file.Name + ".png";
                    path = path.Trim();
                    compressedTex.SaveImageToDisk(path);
                }
            }
            
            /*byte[] arcBuffer = ArcReader.GetBuffer(ArcReader.Read(arcFile));
            List<string> btiFiles = ArcReader.GetFileNamesWithExtension(arcBuffer, ".bti");
            
            foreach (string btiFile in btiFiles)
            {
                byte[] btiBuffer = ArcReader.GetBuffer(ArcReader.Read(arcBuffer, btiFile));
                string exportPath = Path.Combine(exportDirectory, Path.GetFileName(btiFile));
                File.WriteAllBytes(exportPath, btiBuffer);
            }*/
        }

        Debug.Log("Export completed. All BTI files have been exported to: " + exportDirectory);
    }
}
