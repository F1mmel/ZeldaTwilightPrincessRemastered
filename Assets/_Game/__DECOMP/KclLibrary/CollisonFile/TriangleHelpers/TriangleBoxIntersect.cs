using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace KclLibrary
{
    public static class TriangleBoxIntersect
    {

        private const int X = 0;
        private const int Y = 1;
        private const int Z = 2;


        public static bool TriBoxOverlap(Triangle triangle, Vector3 cubeCenter, Vector3 cubeHalfSize)
        {
            Vector3 v0, v1, v2;
            double min, max, p0, p1, p2, rad, fex, fey, fez;
            Vector3 normal, e0, e1, e2;

            bool AXISTEST_X01(double a, double b, double fa, double fb)
            {
                p0 = (a * v0.Y - b * v0.Z);
                p2 = (a * v2.Y - b * v2.Z);
                if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
                rad = (fa * cubeHalfSize.Y + fb * cubeHalfSize.Z);
                if (min > rad || max < -rad) return false;
                return true;
            }

            bool AXISTEST_Y02(double a, double b, double fa, double fb)
            {
                p0 = -a * v0.X + b * v0.Z;
                p2 = -a * v2.X + b * v2.Z;
                if (p0 < p2) { min = p0; max = p2; } else { min = p2; max = p0; }
                rad = fa * cubeHalfSize.X + fb * cubeHalfSize.Z;
                if (min > rad || max < -rad) return false;
                return true;
            }

            bool AXISTEST_Z12(double a, double b, double fa, double fb)
            {
                p1 = a * v1.X - b * v1.Y;
                p2 = a * v2.X - b * v2.Y;
                if (p2 < p1) { min = p2; max = p1; } else { min = p1; max = p2; }
                rad = fa * cubeHalfSize.X + fb * cubeHalfSize.Y;
                return min <= rad && max >= -rad;
            }

            bool AXISTEST_Z0(double a, double b, double fa, double fb)
            {
                p0 = a * v0.X - b * v0.Y;
                p1 = a * v1.X - b * v1.Y;
                if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
                rad = fa * cubeHalfSize.X + fb * cubeHalfSize.Y;
                if (min > rad || max < -rad) return false;
                return true;
            }

            bool AXISTEST_X2(double a, double b, double fa, double fb)
            {
                p0 = a * v0.Y - b * v0.Z;
                p1 = a * v1.Y - b * v1.Z;
                if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
                rad = fa * cubeHalfSize.Y + fb * cubeHalfSize.Z;
                if (min > rad || max < -rad) return false;
                return true;
            }

            bool AXISTEST_Y1(double a, double b, double fa, double fb)
            {
                p0 = -a * v0.X + b * v0.Z;
                p1 = -a * v1.X + b * v1.Z;
                if (p0 < p1) { min = p0; max = p1; } else { min = p1; max = p0; }
                rad = fa * cubeHalfSize.X + fb * cubeHalfSize.Z;
                if (min > rad || max < -rad) return false;
                return true;
            }


            v0 = triangle.Vertices[0] - cubeCenter;
            v1 = triangle.Vertices[1] - cubeCenter;
            v2 = triangle.Vertices[2] - cubeCenter;
            e0 = v1 - v0;
            e1 = v2 - v1;
            e2 = v0 - v2;
            fex = Math.Abs((float)e0.X);
            fey = Math.Abs((float)e0.Y);
            fez = Math.Abs((float)e0.Z);

            if (!AXISTEST_X01((float)e0.Z, (float)e0.Y, fez, fey)) return false;
            if (!AXISTEST_Y02(e0.Z, e0.X, fez, fex)) return false;
            if (!AXISTEST_Z12(e0.Y, e0.X, fey, fex)) return false;

            fex = Math.Abs(e1.X);
            fey = Math.Abs(e1.Y);
            fez = Math.Abs(e1.Z);
            if (!AXISTEST_X01(e1.Z, e1.Y, fez, fey)) return false;
            if (!AXISTEST_Y02(e1.Z, e1.X, fez, fex)) return false;
            if (!AXISTEST_Z0(e1.Y, e1.X, fey, fex)) return false;

            fex = Math.Abs(e2.X);
            fey = Math.Abs(e2.Y);
            fez = Math.Abs(e2.Z);
            if (!AXISTEST_X2(e2.Z, e2.Y, fez, fey)) return false;
            if (!AXISTEST_Y1(e2.Z, e2.X, fez, fex)) return false;
            if (!AXISTEST_Z12(e2.Y, e2.X, fey, fex)) return false;


            void FINDMINMAX(double x0, double x1, double x2, out double m_min, out double m_max)
            {
                m_min = m_max = x0;
                if (x1 < m_min) m_min = x1;
                if (x1 > m_max) m_max = x1;
                if (x2 < m_min) m_min = x2;
                if (x2 > m_max) m_max = x2;
            }

            FINDMINMAX(v0.X, v1.X, v2.X, out min, out max);
            if (min > cubeHalfSize.X || max < -cubeHalfSize.X) return false;


            FINDMINMAX(v0.Y, v1.Y, v2.Y, out min, out max);
            if (min > cubeHalfSize.Y || max < -cubeHalfSize.Y) return false;

            FINDMINMAX(v0.Z, v1.Z, v2.Z, out min, out max);
            if (min > cubeHalfSize.Z || max < -cubeHalfSize.Z) return false;

            normal = Vector3.Cross(e0, e1);
            if (!planeBoxOverlap(normal, v0, cubeHalfSize)) return false;
            return true;    
        }


        private static bool planeBoxOverlap(Vector3 normal, Vector3 vert, Vector3 maxbox)
        {
            int q;
            Vector3 vmin = new Vector3(), vmax = new Vector3();
            float v;
            for (q = X; q <= Z; q++)
            {
                v = Get(vert, q);
                if (Get(normal, q) > 0.0f)
                {
                    Set(vmin, q, -Get(maxbox, q) - v);
                    Set(vmax, q, Get(maxbox, q) - v);
                }
                else
                {
                    Set(vmin, q, Get(maxbox, q) - v);
                    Set(vmax, q, -Get(maxbox, q) - v);
                }
            }
            if (Vector3.Dot(normal, vmin) > 0.0f) return false;
            if (Vector3.Dot(normal, vmax) >= 0.0f) return true;
            return false;
        }

        private static float Get(Vector3 v, int id)
        {
            if (id == 0) return v.X;
            else if (id == 1) return v.Y;
            else if (id == 2) return v.Z;
            else return 0;
        }

        private static void Set(Vector3 v, int id, float val)
        {
            if (id == 0) v.X = val;
            else if (id == 1) v.Y = val;
            else if (id == 2) v.Z = val;
        }


        private static float fmin(double a, double b, double c)
        {
            return (float)Math.Min(a, Math.Min(b, c));
        }

        private static float fmax(double a, double b, double c)
        {
            return (float)Math.Max(a, Math.Max(b, c));
        }
    }
}