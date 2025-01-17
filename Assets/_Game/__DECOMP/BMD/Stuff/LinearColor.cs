using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace ZeldaTesting
{
    [Serializable] [StructLayout(LayoutKind.Sequential)]
    public struct WLinearColor
    {
        public float R;

        public float G;

        public float B;

        public float A;

        public WLinearColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public WLinearColor(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
            A = 1f;
        }

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return R;
                    case 1: return G;
                    case 2: return B;
                    case 3: return A;
                    default: throw new ArgumentOutOfRangeException("index", "Invalid WLinearColor Index!");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: R = value; break;
                    case 1: G = value; break;
                    case 2: B = value; break;
                    case 3: A = value; break;
                    default: throw new ArgumentOutOfRangeException("index", "Invalid WLinearColor Index!");
                }
            }
        }

        public static bool operator ==(WLinearColor lhs, WLinearColor rhs)
        {
            return lhs.R == rhs.R && lhs.G == rhs.G && lhs.B == rhs.B && lhs.A == rhs.A;
        }

        public static bool operator !=(WLinearColor lhs, WLinearColor rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is WLinearColor))
                return false;

            WLinearColor other = (WLinearColor)obj;
            return this == other;
        }

        public override int GetHashCode()
        {
            return ((byte)(R * 255) << 24 | (byte)(R * 255) << 16 | (byte)(G * 255) << 8 | (byte)(A * 255));
        }

        public static WLinearColor FromHexString(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                throw new ArgumentException("Empty/Null hex string!", "hexString");

            if (hexString.StartsWith("0x"))
                hexString = hexString.Substring(2);

            uint WLinearColorVal;
            bool bSuccess = uint.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out WLinearColorVal);
            if (!bSuccess)
                throw new ArgumentException("Not a valid hex number!", "hexString");

            WLinearColor outputWLinearColor = new WLinearColor();
            byte[] bytes = BitConverter.GetBytes(WLinearColorVal);
            for (int i = 0; i < 4; i++)
                outputWLinearColor[3-i] = bytes[i] / 255f;

            return outputWLinearColor;
        }

        public override string ToString()
        {
            return string.Format("R: {0} G: {1} B: {2} A: {3}", (byte)(R*255f), (byte)(G*255f), (byte)(B*255f), (byte)(A*255f));
        }

        public string ToHexString()
        {
            StringBuilder sb = new StringBuilder(10);
            sb.Append("0x");
            sb.Append(R.ToString("X2"));
            sb.Append(G.ToString("X2"));
            sb.Append(B.ToString("X2"));
            sb.Append(A.ToString("X2"));
            return sb.ToString();
        }

        #region Static Preset WLinearColors
        public static WLinearColor White
        {
            get { return new WLinearColor(1f, 1f, 1f, 1f); }
        }

        public static WLinearColor Black
        {
            get { return new WLinearColor(0f, 0f, 0f, 1f); }
        }

        public static WLinearColor Red
        {
            get { return new WLinearColor(1f, 0f, 0f, 1f); }
        }

        public static WLinearColor Green
        {
            get { return new WLinearColor(0f, 1f, 0f, 1f); }
        }

        public static WLinearColor Blue
        {
            get { return new WLinearColor(0f, 0f, 1f, 1f); }
        }

        public static WLinearColor Orange
        {
            get { return new WLinearColor(1f, 0.647f, 0f, 1f); }
        }

        public static WLinearColor Coral
        {
            get { return new WLinearColor(1f, 0.5f, 0.314f, 1f); }
        }

        public static WLinearColor Yellow
        {
            get { return new WLinearColor(1f, 1f, 0f, 1f); }
        }

        public static WLinearColor Purple
        {
            get { return new WLinearColor(0.542f, 0.169f, 0.886f, 1f); }
        }

        public static WLinearColor Seagreen
        {
            get { return new WLinearColor(0.18f, 0.545f, 341f, 1f); }
        }

        public static WLinearColor Pink
        {
            get { return new WLinearColor(1f, 0.412f, 0.71f, 1f); }
        }

        public static WLinearColor DarkGrey
        {
            get { return new WLinearColor(0.4f, 0.4f, 0.4f, 1f); }
        }

        public static WLinearColor Grey
        {
            get { return new WLinearColor(0.7f, 0.7f, 0.7f, 1f); }
        }

        public static WLinearColor Transparent
        {
            get { return new WLinearColor(0f, 0f, 0f, 0f); }
        }
        #endregion
    }
}
