using System;
using CSharpDemo;

internal static class CartesianCoordinatesTests
{
    private static int passed;

    private static void Assert(bool valid, string message)
    {
        if (!valid) throw new Exception(message);
    }

    private static void Near(double actual, double expected, string message)
    {
        double tolerance = 1e-9 * Math.Max(1, Math.Abs(expected));
        Assert(!Double.IsNaN(actual) && !Double.IsInfinity(actual) && Math.Abs(actual - expected) <= tolerance,
            message + ": expected " + expected + ", actual " + actual);
    }

    private static void Point(CartesianPointMm actual, double x, double y, double z)
    {
        Assert(actual != null, "point missing");
        Near(actual.X, x, "X"); Near(actual.Y, y, "Y"); Near(actual.Z, z, "Z");
    }

    private static void Refuses(Action action)
    {
        bool refused = false;
        try { action(); }
        catch (ArgumentException) { refused = true; }
        Assert(refused, "expected argument refusal");
    }

    private static void Test(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("PASS CARTESIAN " + name); }
        catch (Exception error)
        {
            Console.Error.WriteLine("FAIL CARTESIAN " + name + ": " + error);
            Environment.Exit(1);
        }
    }

    private static CartesianPointMm P(double x, double y, double z)
    {
        return new CartesianPointMm(x, y, z);
    }

    private static CartesianDirection D(double x, double y, double z)
    {
        return new CartesianDirection(x, y, z);
    }

    private static CartesianTransform Axes(CartesianDirection x, CartesianDirection y, CartesianDirection z)
    {
        return CartesianTransform.FromAxes("Tool", "Base", P(0, 0, 0), x, y, z);
    }

    private static CartesianTransform GeneralTransform()
    {
        // Independent reference: Rodrigues rotation by +60 degrees about (1,1,1).
        return CartesianTransform.FromAxes("Tool", "Base", P(10, -20, 30),
            D(2.0 / 3, 2.0 / 3, -1.0 / 3), D(-1.0 / 3, 2.0 / 3, 2.0 / 3),
            D(2.0 / 3, -1.0 / 3, 2.0 / 3));
    }

    private static int Main()
    {
        Test("identity preserves all coordinates and frame names", delegate {
            CartesianTransform identity = CartesianTransform.Identity("Base");
            Point(identity.TransformPoint(P(12.5, -7.25, 0)), 12.5, -7.25, 0);
            Point(identity.InverseTransformPoint(P(12.5, -7.25, 0)), 12.5, -7.25, 0);
            Assert(identity.FromFrame == "Base" && identity.ToFrame == "Base", "identity frame mismatch");
        });
        Test("pure translation is in target-frame millimetres", delegate {
            CartesianTransform transform = CartesianTransform.FromYawDegrees("Work", "Base", P(100, -50, 20), 0);
            Point(transform.TransformPoint(P(2, 3, -4)), 102, -47, 16);
            Point(transform.InverseTransformPoint(P(102, -47, 16)), 2, 3, -4);
            Assert(transform.FromFrame == "Work" && transform.ToFrame == "Base", "frame direction reversed");
        });
        Test("positive Z ninety degrees maps positive X to positive Y", delegate {
            CartesianTransform transform = CartesianTransform.FromYawDegrees("Work", "Base", P(0, 0, 0), 90);
            Point(transform.TransformPoint(P(1, 0, 0)), 0, 1, 0);
            Point(transform.TransformPoint(P(0, 1, 0)), -1, 0, 0);
            Point(transform.TransformPoint(P(0, 0, 1)), 0, 0, 1);
        });
        Test("negative and multiple yaw turns keep degree convention", delegate {
            Point(CartesianTransform.FromYawDegrees("A", "B", P(0, 0, 0), -90).TransformPoint(P(2, 0, 3)), 0, -2, 3);
            Point(CartesianTransform.FromYawDegrees("A", "B", P(0, 0, 0), 810).TransformPoint(P(2, 0, 3)), 0, 2, 3);
        });
        Test("general three-dimensional axes match independent Rodrigues reference", delegate {
            CartesianTransform transform = GeneralTransform();
            Point(transform.TransformPoint(P(3, 6, 9)), 16, -17, 39);
            Point(transform.InverseTransformPoint(P(16, -17, 39)), 3, 6, 9);
            Point(transform.TransformPoint(P(0, 0, 3)), 12, -21, 32);
        });
        Test("inverse swaps frames and agrees with inverse-point operation", delegate {
            CartesianTransform transform = GeneralTransform(), inverse = transform.Inverse();
            Assert(inverse.FromFrame == "Base" && inverse.ToFrame == "Tool", "inverse frame mismatch");
            Point(inverse.TransformPoint(P(16, -17, 39)), 3, 6, 9);
            CartesianPointMm actual = inverse.TransformPoint(P(-9, 4, 27));
            CartesianPointMm expected = transform.InverseTransformPoint(P(-9, 4, 27));
            Point(actual, expected.X, expected.Y, expected.Z);
        });
        Test("general inverse round trip covers signed coordinates and nonzero origin", delegate {
            CartesianTransform transform = GeneralTransform();
            CartesianPointMm[] inputs = new CartesianPointMm[] { P(0, 0, 0), P(1, -2, 3), P(-1234.5, 678.25, -90.125) };
            foreach (CartesianPointMm input in inputs)
            {
                CartesianPointMm output = transform.InverseTransformPoint(transform.TransformPoint(input));
                Point(output, input.X, input.Y, input.Z);
                output = transform.TransformPoint(transform.InverseTransformPoint(input));
                Point(output, input.X, input.Y, input.Z);
            }
        });
        Test("Then applies this first and next second with noncommuting translations", delegate {
            CartesianTransform first = CartesianTransform.FromYawDegrees("A", "B", P(10, 0, 0), 90);
            CartesianTransform next = CartesianTransform.FromYawDegrees("B", "A", P(0, 20, 0), 90);
            CartesianTransform forward = first.Then(next), reverse = next.Then(first);
            Point(forward.TransformPoint(P(1, 0, 0)), -1, 30, 0);
            Point(reverse.TransformPoint(P(1, 0, 0)), -11, 0, 0);
            Assert(forward.FromFrame == "A" && forward.ToFrame == "A", "composition frame mismatch");
            CartesianPointMm expected = next.TransformPoint(first.TransformPoint(P(4, -5, 6)));
            Point(forward.TransformPoint(P(4, -5, 6)), expected.X, expected.Y, expected.Z);
        });
        Test("general transform followed by inverse composes to identity", delegate {
            CartesianTransform transform = GeneralTransform();
            Point(transform.Then(transform.Inverse()).TransformPoint(P(13, -8, 21)), 13, -8, 21);
            Point(transform.Inverse().Then(transform).TransformPoint(P(13, -8, 21)), 13, -8, 21);
        });
        Test("mismatched and case-different frame chains refuse", delegate {
            CartesianTransform first = CartesianTransform.FromYawDegrees("A", "Base", P(0, 0, 0), 0);
            Refuses(delegate { first.Then(CartesianTransform.FromYawDegrees("Other", "C", P(0, 0, 0), 0)); });
            Refuses(delegate { first.Then(CartesianTransform.FromYawDegrees("base", "C", P(0, 0, 0), 0)); });
        });
        Test("blank and padded frame names refuse", delegate {
            foreach (string name in new string[] { null, "", " ", "\t", " Base", "Base " })
            {
                string current = name;
                Refuses(delegate { CartesianTransform.Identity(current); });
                Refuses(delegate { CartesianTransform.FromYawDegrees("A", current, P(0, 0, 0), 0); });
            }
        });
        Test("null origin axes points and next transform refuse", delegate {
            CartesianTransform transform = CartesianTransform.Identity("Base");
            Refuses(delegate { CartesianTransform.FromAxes("A", "B", null, D(1, 0, 0), D(0, 1, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(null, D(0, 1, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), null, D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), D(0, 1, 0), null); });
            Refuses(delegate { transform.TransformPoint(null); });
            Refuses(delegate { transform.InverseTransformPoint(null); });
            Refuses(delegate { transform.Then(null); });
        });
        Test("scaled and zero axes refuse instead of silently normalizing", delegate {
            Refuses(delegate { Axes(D(2, 0, 0), D(0, 1, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), D(0, 0.5, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), D(0, 1, 0), D(0, 0, 0)); });
            Refuses(delegate { Axes(D(Double.MaxValue, 0, 0), D(0, 1, 0), D(0, 0, 1)); });
        });
        Test("nonorthogonal duplicate and left-handed unit axes refuse", delegate {
            double rootHalf = Math.Sqrt(0.5);
            Refuses(delegate { Axes(D(1, 0, 0), D(rootHalf, rootHalf, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), D(1, 0, 0), D(0, 0, 1)); });
            Refuses(delegate { Axes(D(1, 0, 0), D(0, 1, 0), D(0, 0, -1)); });
        });
        Test("nonfinite points directions and yaw refuse on every component", delegate {
            foreach (double invalid in new double[] { Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity })
            {
                double value = invalid;
                Refuses(delegate { P(value, 0, 0); }); Refuses(delegate { P(0, value, 0); }); Refuses(delegate { P(0, 0, value); });
                Refuses(delegate { D(value, 0, 0); }); Refuses(delegate { D(0, value, 0); }); Refuses(delegate { D(0, 0, value); });
                Refuses(delegate { CartesianTransform.FromYawDegrees("A", "B", P(0, 0, 0), value); });
            }
        });
        Test("finite transform inputs that overflow output refuse", delegate {
            CartesianTransform transform = CartesianTransform.FromYawDegrees("A", "B", P(Double.MaxValue, 0, 0), 0);
            Refuses(delegate { transform.TransformPoint(P(Double.MaxValue, 0, 0)); });
            Refuses(delegate { transform.InverseTransformPoint(P(-Double.MaxValue, 0, 0)); });
            CartesianTransform next = CartesianTransform.FromYawDegrees("B", "C", P(Double.MaxValue, 0, 0), 0);
            Refuses(delegate { transform.Then(next); });
        });
        Test("inverse origin overflow refuses instead of publishing infinity", delegate {
            CartesianTransform transform = CartesianTransform.FromYawDegrees("A", "B", P(Double.MaxValue, Double.MaxValue, 0), 45);
            Refuses(delegate { transform.Inverse(); });
        });
        Test("rotary reference count zero and explicit direction produce degrees", delegate {
            JointCalibration calibration = JointCalibration.Rotary(262144, 1, 1000, 1, 15);
            Assert(calibration.Kind == CartesianJointKind.Rotary && calibration.ReferenceEncoderCount == 1000 &&
                calibration.PositiveDirection == 1 && calibration.ReferenceJointPosition == 15, "calibration metadata mismatch");
            Near(calibration.ConvertEncoder(1000), 15, "reference degrees");
            Near(calibration.ConvertEncoder(263144), 375, "one motor revolution");
            Near(calibration.ConvertEncoder(-261144), -345, "negative encoder delta");
            calibration = JointCalibration.Rotary(262144, 1, 1000, -1, 15);
            Near(calibration.ConvertEncoder(263144), -345, "reversed direction");
        });
        Test("rotary motor to joint ratio is applied and multiple turns remain unwrapped", delegate {
            JointCalibration reduced = JointCalibration.Rotary(262144, 4, 0, 1, 0);
            Near(reduced.ConvertEncoder(262144), 90, "four motor revolutions per joint revolution");
            Near(reduced.ConvertEncoder(8 * 262144), 720, "multiple joint revolutions must not wrap");
            JointCalibration gearedUp = JointCalibration.Rotary(1000, 0.5, 0, 1, 0);
            Near(gearedUp.ConvertEncoder(1000), 720, "fractional motor to joint ratio");
        });
        Test("linear counts lead gearing zero and sign produce millimetres", delegate {
            JointCalibration calibration = JointCalibration.Linear(262144, 2, 5, 1000, 1, 12.5);
            Assert(calibration.Kind == CartesianJointKind.Linear, "linear kind mismatch");
            Near(calibration.ConvertEncoder(1000), 12.5, "reference mm");
            Near(calibration.ConvertEncoder(263144), 15, "motor revolution with screw gearing");
            Near(calibration.ConvertEncoder(525288), 17.5, "screw revolution lead");
            calibration = JointCalibration.Linear(262144, 2, 5, 1000, -1, 12.5);
            Near(calibration.ConvertEncoder(263144), 10, "negative linear direction");
        });
        Test("encoder subtraction widens before both Int32 boundary differences", delegate {
            JointCalibration rising = JointCalibration.Rotary(360, 1, Int32.MinValue, 1, 0);
            Assert(rising.ConvertEncoder(Int32.MaxValue) == 4294967295.0, "maximum positive difference lost exact count");
            JointCalibration falling = JointCalibration.Linear(1, 1, 1, Int32.MaxValue, 1, 0);
            Assert(falling.ConvertEncoder(Int32.MinValue) == -4294967295.0, "maximum negative difference lost exact count");
            falling = JointCalibration.Linear(1, 1, 1, Int32.MaxValue, -1, 0);
            Assert(falling.ConvertEncoder(Int32.MinValue) == 4294967295.0, "direction times widened difference lost exact count");
        });
        Test("missing calibration refuses and clears previous successful coordinate", delegate {
            double? position; string reason;
            Assert(JointCalibration.TryConvertEncoder(JointCalibration.Linear(1, 1, 1, 0, 1, 0), 3, out position, out reason), "valid calibration refused");
            Assert(position.HasValue && position.Value == 3 && reason == null, "valid result missing");
            Assert(!JointCalibration.TryConvertEncoder(null, 3, out position, out reason), "missing calibration accepted");
            Assert(!position.HasValue && !String.IsNullOrEmpty(reason), "missing calibration retained previous output or hid cause");
        });
        Test("calibrated zero remains a valid coordinate", delegate {
            double? position; string reason;
            Assert(JointCalibration.TryConvertEncoder(JointCalibration.Rotary(1000, 1, 123, -1, 0), 123, out position, out reason), "zero refused");
            Assert(position.HasValue && position.Value == 0 && reason == null, "zero mistaken for unknown");
        });
        Test("zero negative and nonfinite counts or ratios refuse", delegate {
            foreach (double invalid in new double[] { 0, -1, Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity })
            {
                double value = invalid;
                Refuses(delegate { JointCalibration.Rotary(value, 1, 0, 1, 0); });
                Refuses(delegate { JointCalibration.Rotary(1000, value, 0, 1, 0); });
                Refuses(delegate { JointCalibration.Linear(value, 1, 5, 0, 1, 0); });
                Refuses(delegate { JointCalibration.Linear(1000, value, 5, 0, 1, 0); });
                Refuses(delegate { JointCalibration.Linear(1000, 1, value, 0, 1, 0); });
            }
        });
        Test("only positive or negative one directions and finite references are allowed", delegate {
            foreach (int invalid in new int[] { 0, 2, -2, Int32.MinValue, Int32.MaxValue })
            {
                int direction = invalid;
                Refuses(delegate { JointCalibration.Rotary(1000, 1, 0, direction, 0); });
                Refuses(delegate { JointCalibration.Linear(1000, 1, 1, 0, direction, 0); });
            }
            foreach (double invalid in new double[] { Double.NaN, Double.PositiveInfinity, Double.NegativeInfinity })
            {
                double value = invalid;
                Refuses(delegate { JointCalibration.Rotary(1000, 1, 0, 1, value); });
                Refuses(delegate { JointCalibration.Linear(1000, 1, 1, 0, 1, value); });
            }
        });
        Test("extreme ratios that underflow or overflow unit scale refuse", delegate {
            Refuses(delegate { JointCalibration.Rotary(Double.MaxValue, Double.MaxValue, 0, 1, 0); });
            Refuses(delegate { JointCalibration.Linear(Double.MaxValue, 1, Double.Epsilon, 0, 1, 0); });
            Refuses(delegate { JointCalibration.Linear(1, Double.MaxValue, Double.Epsilon, 0, 1, 0); });
            Refuses(delegate { JointCalibration.Rotary(Double.Epsilon, 1, 0, 1, 0); });
            Refuses(delegate { JointCalibration.Linear(1, Double.Epsilon, 1, 0, 1, 0); });
        });
        Test("conversion overflow refuses and Try clears previous output", delegate {
            JointCalibration calibration = JointCalibration.Linear(1, 1, Double.MaxValue, 0, 1, 0);
            Near(calibration.ConvertEncoder(1), Double.MaxValue, "largest finite unit scale");
            Refuses(delegate { calibration.ConvertEncoder(2); });
            double? position = 123; string reason;
            Assert(!JointCalibration.TryConvertEncoder(calibration, 2, out position, out reason), "overflow accepted");
            Assert(!position.HasValue && !String.IsNullOrEmpty(reason), "overflow retained previous coordinate");
            calibration = JointCalibration.Linear(1, 1, Double.MaxValue, 0, 1, Double.MaxValue);
            Assert(!JointCalibration.TryConvertEncoder(calibration, 1, out position, out reason) && !position.HasValue, "reference addition overflow accepted");
        });
        Console.WriteLine("PASS CARTESIAN " + passed + " checks; pure math only, no controller connection.");
        return 0;
    }
}
