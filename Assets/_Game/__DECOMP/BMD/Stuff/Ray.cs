using OpenTK;


    public struct FRay
    {
        public Vector3 Origin;
        public Vector3 Direction;

        public FRay(Vector3 origin, Vector3 direction)
        {
            Origin = origin;
            Direction = direction;
        }

        public Vector3 GetPoint(float distance)
        {
            return Origin + (Direction * distance);
        }

        public override string ToString()
        {
            return string.Format("Origin: {0:F3}, {1:F3}, {2:F3}, Direction: {3:F3}, {4:F3}, {5:F3}", Origin.X, Origin.Y, Origin.Z, Direction.X, Direction.Y, Direction.Z);
        }
    }