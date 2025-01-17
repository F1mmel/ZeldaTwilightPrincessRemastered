using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using WiiExplorer;
using ArcFile = WiiExplorer.ArcFile;

public class ArcReader
{
    public static byte[] GetBuffer(Archive archive, string file, ArchiveDirectory root = null)
    {
        /*if (root == null) root = archive.Root;
        
        foreach (KeyValuePair<string, object> c in root.Items)
        {
            if (c.Value is ArchiveDirectory dir)
            {
                foreach (KeyValuePair<string, object> subChild in dir.Items)
                {
                    if (subChild.Value is ArchiveFile f)
                    {
                        if (f.Name.Equals(file))
                        {
                            return f.FileData;
                        }
                    }
                }
            }
        }

        return null;*/

        foreach (ArcFile f in archive.Files)
        {
            if (f.Name.Equals(file))
            {
                return f.Buffer;
            }
        }

        return null;
    }

    public static ArcFile GetFile(Archive archive, string file)
    {
        foreach (ArcFile f in archive.Files)
        {
            if (f.Name.Equals(file))
            {
                return f;
            }
        }

        return null;
    }

    public static Archive ReadObject(string arcFile)
    {
        return Read("Assets/GameFiles/res/Object/" + arcFile + ".arc");
    }

    public static Dictionary<string, Archive> CachedArchives = new Dictionary<string, Archive>();
    public static Archive Read(string arcFile)
    {
        if (CachedArchives.ContainsKey(arcFile))
        {
            return CachedArchives[arcFile];
        }
        
            bool IsYaz0 = YAZ0.Check(arcFile);
            bool IsYay0 = YAY0.Check(arcFile);
            Archive Archive;

            if (IsU8()) {
                Archive = IsYaz0 ? new U8(YAZ0.DecompressToMemoryStream(arcFile), arcFile) : (IsYay0 ? new U8(YAY0.DecompressToMemoryStream(arcFile), arcFile) : new U8(arcFile));
            } else if (IsRARC())
            {
                Archive = IsYaz0 ? new RARC(YAZ0.DecompressToMemoryStream(arcFile), arcFile) : (IsYay0 ? new RARC(YAY0.DecompressToMemoryStream(arcFile), arcFile) : new RARC(arcFile));
            }
            else
            {
                Debug.LogError("NO VALID ARC FILE: " + arcFile);
                return new U8("ERROR");
            }

            //MemoryStream stream = new MemoryStream();
            //Archive.Save(stream);
            
            foreach (KeyValuePair<string, object> c in Archive.Root.Items)
            {
                if (c.Value is ArchiveDirectory dir)
                {
                    foreach (KeyValuePair<string, object> subChild in dir.Items)
                    {
                        if (subChild.Value is RARC.File f)
                        {
                            Archive.Files.Add(new ArcFile(f.Name, dir.Name, f.FileData));
                        }
                    }
                }
                else
                {
                    if (c.Value is RARC.File f)
                    {
                        Archive.Files.Add(new ArcFile(f.Name, "", f.FileData));
                    }
                }
            }

            CachedArchives.Add(arcFile, Archive);
            return Archive;

            int Count = Archive.TotalFileCount; //do it here so we don't need to do it twice, as that would be taxing for large archives
            
            bool IsU8()
            {
                Stream arc;
                if (IsYaz0)
                {
                    arc = YAZ0.DecompressToMemoryStream(arcFile);
                }
                else if (IsYay0)
                {
                    arc = YAY0.DecompressToMemoryStream(arcFile);
                }
                else
                {
                    arc = new FileStream(arcFile, FileMode.Open);
                }
                bool Check = arc.ReadString(4) == U8.Magic;
                arc.Close();
                return Check;
            }
            bool IsRARC()
            {
                System.IO.Stream arc;
                if (IsYaz0)
                {
                    arc = YAZ0.DecompressToMemoryStream(arcFile);
                }
                else if (IsYay0)
                {
                    arc = YAY0.DecompressToMemoryStream(arcFile);
                }
                else
                {
                    arc = new FileStream(arcFile, FileMode.Open);
                }
                bool Check = arc.ReadString(4) == RARC.Magic;
                arc.Close();
                return Check;
            }
    }

    public static List<RARC.File> GetFullFileData(Archive archive)
    {
        List<RARC.File> files = new List<RARC.File>();
        foreach (KeyValuePair<string, object> c in archive.Root.Items)
        {
            if (c.Value is ArchiveDirectory dir)
            {
                foreach (KeyValuePair<string, object> subChild in dir.Items)
                {
                    if (subChild.Value is RARC.File f)
                    {
                        files.Add(f);
                    }
                }
            }
            else
            {
                if (c.Value is RARC.File f)
                {
                    files.Add(f);
                }
            }
        }

        return files;
    }

    public static RARC.File GetFileById(Archive archive, int id)
    {
        List<RARC.File> files = GetFullFileData(archive);
        foreach (var a in files)
        {
            if (a.ID == id) return a;
        }

        return null;
    }
}