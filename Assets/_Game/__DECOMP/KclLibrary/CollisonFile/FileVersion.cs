using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KclLibrary
{
    public enum FileVersion
    {
        VersionGC = 0x00000000,
        VersionWII = 0x01000000,
        VersionDS = 0x01020000,
        Version2 = 0x02020000,
    }
}