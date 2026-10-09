using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CSharpDemo
{
    internal static class DashboardTheme
    {
        public static readonly Color Background = Color.FromArgb(241, 245, 249);
        public static readonly Color Ink = Color.FromArgb(25, 42, 58);
        public static readonly Color Muted = Color.FromArgb(105, 122, 140);
        public static readonly Color Border = Color.FromArgb(219, 227, 236);
        public static readonly Color Accent = Color.FromArgb(13, 119, 112);
        public static readonly Color SoftAccent = Color.FromArgb(231, 247, 242);
        public static readonly Color Danger = Color.FromArgb(181, 53, 61);
        public static readonly Color SoftDanger = Color.FromArgb(254, 239, 238);
        public static readonly Font Body = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font Strong = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);

        public static GraphicsPath Round(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static string State(int status)
        {
            if ((status & 1) != 0) return "急停触发";
            if ((status & 2) != 0) return "驱动报警";
            if ((status & 16) != 0) return "跟随异常";
            if ((status & 108) != 0) return "限位触发";
            if ((status & 256) != 0) return "停止信号";
            if ((status & 4096) != 0) return "回零中";
            if ((status & 1024) != 0) return "运行中";
            return (status & 512) != 0 ? "已使能" : "未使能";
        }

        public static Color StateColor(int status)
        {
            if ((status & Axis2Motion.HazardBits) != 0) return Danger;
            return (status & (512 | 1024 | 4096)) != 0 ? Accent : Muted;
        }
    }

    internal sealed class DashboardPanel : Panel
    {
        public DashboardPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(18);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(DashboardTheme.Background);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (Width < 24 || Height < 24) return;
            using (GraphicsPath path = DashboardTheme.Round(new Rectangle(0, 0, Width - 1, Height - 1), 10))
            using (Brush fill = new SolidBrush(BackColor))
            using (Pen border = new Pen(DashboardTheme.Border))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);
            }
        }
    }

    internal sealed class AxisStatusCard : Button
    {
        public int Axis;
        public bool Selected, Valid;
        public int Position, Status;
        public AxisStatusCard(int axis)
        {
            Axis = axis;
            AccessibleName = "选择软件轴" + axis;
            TabStop = true;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(DashboardTheme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath shape = DashboardTheme.Round(new Rectangle(1, 1, Width - 3, Height - 3), 10))
            using (Brush background = new SolidBrush(Selected ? DashboardTheme.SoftAccent : Color.White))
            using (Pen border = new Pen(Selected ? DashboardTheme.Accent : DashboardTheme.Border, Selected ? 2 : 1))
            {
                g.FillPath(background, shape);
                g.DrawPath(border, shape);
            }
            string name = Axis == 2 ? "轴 2 · 升降" : "轴 " + Axis;
            TextRenderer.DrawText(g, name, DashboardTheme.Strong, new Point(16, 13), DashboardTheme.Ink);
            string state = Valid ? DashboardTheme.State(Status) : "未读取";
            Color color = Valid ? DashboardTheme.StateColor(Status) : DashboardTheme.Muted;
            using (Brush dot = new SolidBrush(color)) g.FillEllipse(dot, Width - 82, 20, 6, 6);
            TextRenderer.DrawText(g, state, DashboardTheme.Body, new Rectangle(Width - 73, 14, 66, 22), color, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            using (Font number = new Font("Segoe UI", 17F, FontStyle.Bold))
                TextRenderer.DrawText(g, Valid ? Position.ToString("N0") : "—", number, new Rectangle(14, 38, Width - 30, 30), DashboardTheme.Ink, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g, Selected ? "编码器 · 计数      当前选择" : "编码器 · 计数", DashboardTheme.Body, new Rectangle(16, 73, Width - 25, 19), DashboardTheme.Muted, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            if (Focused) ControlPaint.DrawFocusRectangle(g, new Rectangle(7, 7, Width - 14, Height - 14));
        }
    }
}
