using OpenTK;
using System;


    public static class WMath
    {
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;

            return value;
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            if (value > max)
                value = max;

            return value;
        }

        public static float Lerp(float a, float b, float t)
        {
            return (1 - t) * a + t * b;
        }

        public static float DegreesToRadians(float degrees)
        {
            return degrees * (float)(Math.PI / 180.0);
        }

        public static float RadiansToDegrees(float radians)
        {
            return radians * (float)(180.0 / Math.PI); 
        }

        public static float RotationShortToFloat(short rotation)
        {
            return rotation * (180 / 32768f);
        }

        public static short RotationFloatToShort(float rotation)
        {
            return (short)(rotation * (32768f / 180f));
        }

        public static FRay TransformRay(FRay ray, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            FRay localRay = new FRay();
            localRay.Direction = Vector3.Transform(ray.Direction, rotation);
            localRay.Origin = Vector3.Transform(ray.Origin - position, rotation);

            localRay.Origin.X /= scale.X;
            localRay.Origin.Y /= scale.Y;
            localRay.Origin.Z /= scale.Z;

            localRay.Direction.X /= scale.X;
            localRay.Direction.Y /= scale.Y;
            localRay.Direction.Z /= scale.Z;
            localRay.Direction.Normalize();

            return localRay;
        }

        public static int Pad32Delta(long inPos)
        {
            long nextAligned = (inPos + 0x1F) & ~0x1F;

            long delta = nextAligned - inPos;
            return (int)delta;
        }

        public static bool RayIntersectsAABB(FRay ray, Vector3 aabbMin, Vector3 aabbMax, out float intersectionDistance)
        {
            Vector3 t_1 = new Vector3(), t_2 = new Vector3();

            float tNear = float.MinValue;
            float tFar = float.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                if (ray.Direction[i] == 0)
                {
                    if ((ray.Origin[i] < aabbMin[i]) || (ray.Origin[i] > aabbMax[i]))
                    {
                        intersectionDistance = float.MinValue;
                        return false;
                    }
                }
                else
                {
                    t_1[i] = (aabbMin[i] - ray.Origin[i]) / ray.Direction[i];
                    t_2[i] = (aabbMax[i] - ray.Origin[i]) / ray.Direction[i];

                    if (t_1[i] > t_2[i])
                    {
                        Vector3 temp = t_2;
                        t_2 = t_1;
                        t_1 = temp;
                    }

                    if (t_1[i] > tNear)
                        tNear = t_1[i];

                    if (t_2[i] < tFar)
                        tFar = t_2[i];

                    if ((tNear > tFar) || (tFar < 0))
                    {
                        intersectionDistance = float.MinValue;
                        return false;
                    }
                }
            }

            intersectionDistance = tNear;
            return true;
        }

        public static bool RayIntersectsTriangle(FRay ray, Vector3 v1, Vector3 v2, Vector3 v3, bool oneSided, out float intersectionDistance)
        {
            intersectionDistance = float.MinValue;

            Vector3 e1 = v2 - v1;
            Vector3 e2 = v3 - v1;

            Vector3 p;
            Vector3.Cross(ref ray.Direction, ref e2, out p);

            float det = Vector3.Dot(e1, p);

            if (oneSided)
            {
                Vector3 n;
                Vector3.Cross(ref e2, ref e1, out n);
                n.NormalizeFast();

                float dirToTri;
                Vector3.Dot(ref ray.Direction, ref n, out dirToTri);

                if (dirToTri > 0)
                    return false;
            }

            if (det > -float.Epsilon && det < float.Epsilon)
                return false;

            float inv_det = 1f / det;

            Vector3 t;
            Vector3.Subtract(ref ray.Origin, ref v1, out t);

            float u;
            Vector3.Dot(ref t, ref p, out u);
            u *= inv_det;

            if (u < 0f || u > 1f)
                return false;

            Vector3 q;
            Vector3.Cross(ref t, ref e1, out q);

            float v;
            Vector3.Dot(ref ray.Direction, ref q, out v);
            v *= inv_det;

            if (v < 0f || u + v > 1f)
                return false;

            float dist;
            Vector3.Dot(ref e2, ref q, out dist);
            dist *= inv_det;

            if (dist > float.Epsilon)
            {
                intersectionDistance = dist;
                return true;
            }

            return false;
        }

        public static int Floor(float val)
        {
            return (int)Math.Floor(val);
        }
    }