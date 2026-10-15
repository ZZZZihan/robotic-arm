using System;

namespace CSharpDemo
{
    // Pure geometry only. No SDK, controller reference, or assumption about robot topology.
    internal static class CartesianNumbers
    {
        internal static double Finite(double value, string name)
        {
            if (Double.IsNaN(value) || Double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "坐标参数必须为有限数值。");
            return value;
        }

        internal static double Positive(double value, string name)
        {
            Finite(value, name);
            if (value <= 0) throw new ArgumentOutOfRangeException(name, "标定比例必须大于零。");
            return value;
        }

        internal static string FrameName(string name)
        {
            if (String.IsNullOrEmpty(name) || name.Trim().Length == 0 || name != name.Trim())
                throw new ArgumentException("坐标系名称不能为空或带首尾空格。", "name");
            return name;
        }
    }

    // A mathematical point, never an implicit live encoder measurement.
    internal sealed class CartesianPointMm
    {
        public readonly double X, Y, Z;

        public CartesianPointMm(double x, double y, double z)
        {
            X = CartesianNumbers.Finite(x, "x");
            Y = CartesianNumbers.Finite(y, "y");
            Z = CartesianNumbers.Finite(z, "z");
        }
    }

    // Dimensionless direction. FromAxes requires three orthonormal, right-handed axes.
    internal sealed class CartesianDirection
    {
        public readonly double X, Y, Z;

        public CartesianDirection(double x, double y, double z)
        {
            X = CartesianNumbers.Finite(x, "x");
            Y = CartesianNumbers.Finite(y, "y");
            Z = CartesianNumbers.Finite(z, "z");
        }

        internal double Dot(CartesianDirection other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }
    }

    // p_to = R * p_from + t. Columns of R are the FROM axes expressed in TO.
    // Valid numerical transforms do not establish physical calibration.
    internal sealed class CartesianTransform
    {
        private const double Tolerance = 1e-9;
        public readonly string FromFrame, ToFrame;
        public readonly CartesianPointMm Origin;
        public readonly CartesianDirection XAxis, YAxis, ZAxis;

        private CartesianTransform(string fromFrame, string toFrame, CartesianPointMm origin,
            CartesianDirection xAxis, CartesianDirection yAxis, CartesianDirection zAxis)
        {
            FromFrame = CartesianNumbers.FrameName(fromFrame);
            ToFrame = CartesianNumbers.FrameName(toFrame);
            if (origin == null || xAxis == null || yAxis == null || zAxis == null)
                throw new ArgumentNullException("origin/axes");
            CheckUnit(xAxis); CheckUnit(yAxis); CheckUnit(zAxis);
            if (Math.Abs(xAxis.Dot(yAxis)) > Tolerance || Math.Abs(xAxis.Dot(zAxis)) > Tolerance ||
                Math.Abs(yAxis.Dot(zAxis)) > Tolerance)
                throw new ArgumentException("坐标轴必须两两正交。");
            double determinant = (xAxis.Y * yAxis.Z - xAxis.Z * yAxis.Y) * zAxis.X +
                (xAxis.Z * yAxis.X - xAxis.X * yAxis.Z) * zAxis.Y +
                (xAxis.X * yAxis.Y - xAxis.Y * yAxis.X) * zAxis.Z;
            if (Math.Abs(determinant - 1) > Tolerance)
                throw new ArgumentException("坐标轴必须满足右手系 X × Y = Z。");
            Origin = origin; XAxis = xAxis; YAxis = yAxis; ZAxis = zAxis;
        }

        private static void CheckUnit(CartesianDirection axis)
        {
            if (Math.Abs(axis.Dot(axis) - 1) > Tolerance)
                throw new ArgumentException("坐标轴必须是单位向量，不能用零向量或缩放代替旋转。");
        }

        public static CartesianTransform FromAxes(string fromFrame, string toFrame, CartesianPointMm origin,
            CartesianDirection xAxis, CartesianDirection yAxis, CartesianDirection zAxis)
        {
            return new CartesianTransform(fromFrame, toFrame, origin, xAxis, yAxis, zAxis);
        }

        public static CartesianTransform Identity(string frame)
        {
            return FromYawDegrees(frame, frame, new CartesianPointMm(0, 0, 0), 0);
        }

        public static CartesianTransform FromYawDegrees(string fromFrame, string toFrame,
            CartesianPointMm origin, double degrees)
        {
            CartesianNumbers.Finite(degrees, "degrees");
            double angle = (degrees % 360) * Math.PI / 180;
            double c = Math.Cos(angle), s = Math.Sin(angle);
            return FromAxes(fromFrame, toFrame, origin, new CartesianDirection(c, s, 0),
                new CartesianDirection(-s, c, 0), new CartesianDirection(0, 0, 1));
        }

        public CartesianPointMm TransformPoint(CartesianPointMm point)
        {
            if (point == null) throw new ArgumentNullException("point");
            return new CartesianPointMm(
                XAxis.X * point.X + YAxis.X * point.Y + ZAxis.X * point.Z + Origin.X,
                XAxis.Y * point.X + YAxis.Y * point.Y + ZAxis.Y * point.Z + Origin.Y,
                XAxis.Z * point.X + YAxis.Z * point.Y + ZAxis.Z * point.Z + Origin.Z);
        }

        public CartesianPointMm InverseTransformPoint(CartesianPointMm point)
        {
            if (point == null) throw new ArgumentNullException("point");
            CartesianDirection delta = new CartesianDirection(point.X - Origin.X, point.Y - Origin.Y, point.Z - Origin.Z);
            return new CartesianPointMm(XAxis.Dot(delta), YAxis.Dot(delta), ZAxis.Dot(delta));
        }

        public CartesianTransform Inverse()
        {
            CartesianPointMm inverseOrigin = InverseTransformPoint(new CartesianPointMm(0, 0, 0));
            return FromAxes(ToFrame, FromFrame, inverseOrigin,
                new CartesianDirection(XAxis.X, YAxis.X, ZAxis.X),
                new CartesianDirection(XAxis.Y, YAxis.Y, ZAxis.Y),
                new CartesianDirection(XAxis.Z, YAxis.Z, ZAxis.Z));
        }

        private CartesianDirection Rotate(CartesianDirection direction)
        {
            return new CartesianDirection(
                XAxis.X * direction.X + YAxis.X * direction.Y + ZAxis.X * direction.Z,
                XAxis.Y * direction.X + YAxis.Y * direction.Y + ZAxis.Y * direction.Z,
                XAxis.Z * direction.X + YAxis.Z * direction.Y + ZAxis.Z * direction.Z);
        }

        // Apply this transform first, then next. Frame order is checked before composing.
        public CartesianTransform Then(CartesianTransform next)
        {
            if (next == null) throw new ArgumentNullException("next");
            if (!String.Equals(ToFrame, next.FromFrame, StringComparison.Ordinal))
                throw new ArgumentException("坐标变换链不匹配：前一目标系必须等于后一来源系。", "next");
            return FromAxes(FromFrame, next.ToFrame, next.TransformPoint(Origin),
                next.Rotate(XAxis), next.Rotate(YAxis), next.Rotate(ZAxis));
        }
    }

    internal enum CartesianJointKind { Rotary, Linear }

    // One calibrated encoder -> one joint coordinate; this is NOT robot forward kinematics.
    // Effective encoder counts and motor/output ratio must be measured for each joint.
    internal sealed class JointCalibration
    {
        public readonly CartesianJointKind Kind;
        public readonly int ReferenceEncoderCount, PositiveDirection;
        public readonly double ReferenceJointPosition;
        private readonly double unitsPerCount;

        private JointCalibration(CartesianJointKind kind, double countsPerMotorRevolution,
            double motorRevolutionsPerOutputRevolution, double unitsPerOutputRevolution,
            int referenceEncoderCount, int positiveDirection, double referencePosition)
        {
            CartesianNumbers.Positive(countsPerMotorRevolution, "countsPerMotorRevolution");
            CartesianNumbers.Positive(motorRevolutionsPerOutputRevolution, "motorRevolutionsPerOutputRevolution");
            CartesianNumbers.Positive(unitsPerOutputRevolution, "unitsPerOutputRevolution");
            if (positiveDirection != 1 && positiveDirection != -1)
                throw new ArgumentOutOfRangeException("positiveDirection", "方向只能为 +1 或 -1。");
            Kind = kind; ReferenceEncoderCount = referenceEncoderCount; PositiveDirection = positiveDirection;
            ReferenceJointPosition = CartesianNumbers.Finite(referencePosition, "referencePosition");
            unitsPerCount = CartesianNumbers.Positive(
                (unitsPerOutputRevolution / motorRevolutionsPerOutputRevolution) / countsPerMotorRevolution,
                "unitsPerCount");
        }

        public static JointCalibration Rotary(double countsPerMotorRevolution,
            double motorRevolutionsPerJointRevolution, int referenceEncoderCount,
            int positiveDirection, double referenceDegrees)
        {
            return new JointCalibration(CartesianJointKind.Rotary, countsPerMotorRevolution,
                motorRevolutionsPerJointRevolution, 360, referenceEncoderCount, positiveDirection, referenceDegrees);
        }

        public static JointCalibration Linear(double countsPerMotorRevolution,
            double motorRevolutionsPerScrewRevolution, double leadMmPerScrewRevolution,
            int referenceEncoderCount, int positiveDirection, double referenceMm)
        {
            return new JointCalibration(CartesianJointKind.Linear, countsPerMotorRevolution,
                motorRevolutionsPerScrewRevolution, leadMmPerScrewRevolution,
                referenceEncoderCount, positiveDirection, referenceMm);
        }

        public double ConvertEncoder(int encoder)
        {
            long delta = (long)encoder - ReferenceEncoderCount;
            return CartesianNumbers.Finite(ReferenceJointPosition + PositiveDirection * delta * unitsPerCount,
                "jointPosition");
        }

        // Missing calibration and numerical failure always clear the output; zero is a valid coordinate.
        public static bool TryConvertEncoder(JointCalibration calibration, int encoder,
            out double? jointPosition, out string reason)
        {
            jointPosition = null;
            if (calibration == null) { reason = "未标定：缺少该轴的比例、方向或参考零点。"; return false; }
            try { jointPosition = calibration.ConvertEncoder(encoder); reason = null; return true; }
            catch (ArgumentOutOfRangeException) { reason = "换算超出有限数值范围。"; return false; }
        }
    }
}
