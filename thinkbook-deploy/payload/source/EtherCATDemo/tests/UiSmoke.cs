using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CSharpDemo;

internal static class UiSmoke
{
    private static object Field(Form1 form, string name)
    {
        return typeof(Form1).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
    }
    private static object Call(Form1 form, string name, params object[] args)
    {
        return typeof(Form1).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(form, args);
    }
    private static void Assert(bool valid, string message) { if (!valid) throw new Exception(message); }
    private static TextBox Editor(NumericUpDown input)
    {
        foreach (Control child in input.Controls) if (child is TextBox) return (TextBox)child;
        throw new Exception("numeric text editor missing");
    }
    private static void Draft(Form1 form, NumericUpDown input, string text)
    {
        TextBox editor = Editor(input);
        editor.SelectAll(); editor.SelectedText = text;
        int caret = editor.SelectionStart;
        for (int i = 0; i < 3; i++)
        {
            Call(form, "RefreshAxis2Controls"); Call(form, "RefreshDashboard"); Application.DoEvents();
        }
        Assert(editor.Text == text && editor.SelectionStart == caret, "refresh rewrote draft or moved caret: " + text + " -> " + editor.Text);
    }
    private static bool ReadInputs(Form1 form, out decimal turns, out decimal rpm)
    {
        object[] args = new object[] { 0M, 0M, null };
        bool valid = (bool)Call(form, "TryReadAxis2Inputs", args);
        turns = (decimal)args[0]; rpm = (decimal)args[1]; return valid;
    }
    private static void CheckTyping(Form1 form)
    {
        NumericUpDown turns = (NumericUpDown)Field(form, "axis2Turns"), rpm = (NumericUpDown)Field(form, "axis2Rpm");
        decimal t, r;
        foreach (NumericUpDown input in new NumericUpDown[] { turns, rpm })
        {
            foreach (string text in new string[] { "", "0", "0.", "0.5", "1", "1.", "1.25" }) Draft(form, input, text);
        }
        Assert(ReadInputs(form, out t, out r) && t == 1.25M && r == 1.25M, "typed decimals were not read as the requested values");
        Console.WriteLine("PASS UI typed integer/decimal drafts and caret survive refresh in both fields");
        turns.GetType().GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(turns, new object[] { new KeyEventArgs(Keys.Control | Keys.A) });
        Assert(Editor(turns).SelectionLength == Editor(turns).TextLength, "Ctrl+A did not select input");
        Editor(turns).SelectedText = "2.5";
        turns.GetType().GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(turns, new object[] { new KeyEventArgs(Keys.Enter) });
        Assert(turns.Value == 2.5M && !((Axis2Motion)Field(form, "axis2Motion")).Active, "Enter failed to commit or started motion");
        Console.WriteLine("PASS UI Ctrl+A replacement and Enter commit without motion");
        foreach (string text in new string[] { "20", "-1", "1.234", "invalid", "" })
        {
            Draft(form, turns, text); form.ValidateChildren();
            Assert(Editor(turns).Text == text && !ReadInputs(form, out t, out r), "invalid travel clamped or reused a prior value: " + text);
            Assert(!((Button)Field(form, "button4")).Enabled, "invalid travel enabled motion");
        }
        Draft(form, turns, "1"); Draft(form, rpm, "121"); form.ValidateChildren();
        Assert(Editor(rpm).Text == "121" && !ReadInputs(form, out t, out r), "invalid rpm clamped");
        Console.WriteLine("PASS UI invalid/blank/out-of-range drafts persist and refuse stale or clamped values");
        Draft(form, turns, "1.2"); Draft(form, rpm, "60");
        turns.UpButton(); Assert(turns.Value == 1.3M, "up arrow lost typed starting value");
        turns.DownButton(); Assert(turns.Value == 1.2M, "down arrow increment changed");
        rpm.UpButton(); Assert(rpm.Value == 61M, "rpm arrow failed");
        rpm.DownButton(); Assert(rpm.Value == 60M, "rpm down arrow failed");
        Assert(!(bool)Field(form, "cardOpened") && !((Axis2Motion)Field(form, "axis2Motion")).Active, "typing connected or moved controller");
        turns.Value = 0M; rpm.Value = 120M;
        Console.WriteLine("PASS UI arrow adjustment after typed input and no controller access");
    }
    private static void Render(Form1 form, string file)
    {
        form.PerformLayout();
        TableLayoutPanel root = null;
        foreach (Control child in form.Controls) if (child is TableLayoutPanel) root = (TableLayoutPanel)child;
        Assert(root != null, "missing dashboard root");
        using (Bitmap image = new Bitmap(root.Width, root.Height))
        {
            root.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
            image.Save(file, System.Drawing.Imaging.ImageFormat.Png);
        }
    }

    private static void CheckVisibleBounds(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            if (!child.Visible) continue;
            if (child is Button || child is NumericUpDown)
            {
                Assert(child.Left >= 0 && child.Top >= 0 && child.Right <= parent.ClientSize.Width + 1 && child.Bottom <= parent.ClientSize.Height + 1,
                    "clipped control " + child.Name + " " + child.Bounds + " in " + parent.ClientSize);
            }
            CheckVisibleBounds(child);
        }
    }
    [STAThread]
    private static int Main(string[] args)
    {
        try { Run(args[0]); return 0; }
        catch (Exception e) { Console.Error.WriteLine("FAIL UI: " + e.Message); return 1; }
    }
    private static void Run(string screenshot)
    {
        Application.EnableVisualStyles();
        using (Form1 form = new Form1())
        {
            Assert(!(bool)Field(form, "cardOpened"), "startup opened card");
            Assert(((NumericUpDown)Field(form, "axis2Rpm")).Value == 120M, "speed changed");
            Assert(((NumericUpDown)Field(form, "axis2Turns")).Value == 0M, "startup travel changed");
            ComboBox axes = (ComboBox)Field(form, "comboBoxAxisSel");
            Assert(axes.Items.Count == 4 && axes.SelectedIndex == 1, "four-axis selection");
            form.StartPosition = FormStartPosition.Manual; form.Location = Point.Empty;
            form.ClientSize = new Size(1000, 700); form.Show(); Application.DoEvents();
            AxisStatusCard[] cards = (AxisStatusCard[])Field(form, "axisCards");
            for (int i = 0; i < 4; i++)
            {
                cards[i].PerformClick();
                Assert(axes.SelectedIndex == i, "card selected wrong SDK index");
                Assert(((Panel)Field(form, "turnsPanel")).Visible == (i == 1), "wrong units panel");
                Assert(!((Button)Field(form, "button4")).Enabled && !((Button)Field(form, "buttonJogP")).Enabled,
                    "disconnected motion enabled");
            }
            Console.WriteLine("PASS UI four-card selection, axis-specific units, disconnected actions");
            cards[1].PerformClick();
            Assert(((Button)Field(form, "button4")).Text.Contains("上升") && ((Button)Field(form, "button4")).Text.Contains("正向"), "positive action is not labeled upward");
            Assert(((Button)Field(form, "button5")).Text.Contains("下降") && ((Button)Field(form, "button5")).Text.Contains("反向"), "negative action is not labeled downward");
            Assert(((Label)Field(form, "modeHint")).Text.Contains("正向向上，反向向下"), "lift direction guidance mismatch");
            Console.WriteLine("PASS UI positive-up and negative-down labels match existing direction handlers");
            cards[1].PerformClick(); CheckVisibleBounds(form); Render(form, screenshot);
            Console.WriteLine("PASS UI startup defaults and normal-window layout");
            form.ClientSize = new Size(984, 691); form.PerformLayout(); Application.DoEvents();
            CheckVisibleBounds(form); Render(form, Path.Combine(Path.GetDirectoryName(screenshot), "ui-compact.png"));
            Console.WriteLine("PASS UI compact minimum-window layout");
            ((NumericUpDown)Field(form, "axis2Turns")).Value = 1M;
            Assert(((Label)Field(form, "axis2Info")).Text.Contains(Axis2Motion.MakePlan(0, 0, 1M, 120M, 1).EstimatedSeconds.ToString("0.00")), "duration preview differs from planner");
            Assert(!((Axis2Motion)Field(form, "axis2Motion")).Active && !((Button)Field(form, "button4")).Enabled, "editing preview started or enabled disconnected motion");
            Render(form, Path.Combine(Path.GetDirectoryName(screenshot), "ui-preview.png"));
            ((NumericUpDown)Field(form, "axis2Turns")).Value = 0M;
            Assert(((Label)Field(form, "axis2Info")).Text.Contains("填写圈数"), "zero travel retained an old time estimate");
            Console.WriteLine("PASS UI nonzero duration preview and zero-input reset without card access");
            CheckTyping(form);
            form.ClientSize = new Size(1000, 700); form.PerformLayout(); Application.DoEvents();
            MultiCardCS.MultiCardCS.TAllSysStatusDataSX sample = (MultiCardCS.MultiCardCS.TAllSysStatusDataSX)Call(form, "CreateStatusBuffer");
            for (int i = 0; i < 4; i++) { sample.lAxisEncPos[i] = 100000 * (i + 1); sample.lAxisPrfPos[i] = sample.lAxisEncPos[i] + i; sample.lAxisStatus[i] = 0xA00; }
            Call(form, "UpdateDashboardSample", sample);
            Assert(((Label)Field(form, "actualValue")).Text == 200000.ToString("N0"), "selected feedback mismatch");
            Assert(((Label)Field(form, "differenceValue")).Text == "1", "difference mismatch");
            cards[3].PerformClick();
            Assert(((Label)Field(form, "actualValue")).Text == 400000.ToString("N0"), "axis4 feedback mapped incorrectly");
            Console.WriteLine("PASS UI feedback mapping across axis changes");
            sample.lAxisStatus[3] |= 1024;
            Call(form, "UpdateDashboardSample", sample);
            cards[0].PerformClick();
            Assert(axes.SelectedIndex == 3 && !cards[0].Enabled, "axis switching allowed during observed motion");
            sample.lAxisStatus[3] &= ~1024;
            Call(form, "UpdateDashboardSample", sample);
            Assert(cards[0].Enabled, "axis selection did not unlock after stop");
            Console.WriteLine("PASS UI axis selection locks during observed motion");
            sample.lAxisStatus[3] |= 2;
            Call(form, "UpdateDashboardSample", sample);
            Assert(((Label)Field(form, "feedbackState")).Text.Contains("驱动报警"), "alarm not visible");
            Call(form, "InvalidateDashboardSample");
            Assert(((Label)Field(form, "actualValue")).Text == "—" && !cards[3].Valid, "stale data presented as live");
            Console.WriteLine("PASS UI alarm and stale-feedback display");
            Call(form, "HandleAxis2Sample", -7, sample);
            Assert(((Label)Field(form, "noticeLabel")).Text.Contains("控制器无响应"), "communication code lacks explanation");
            Assert(!(bool)Field(form, "axis2BaselineValid"), "lost feedback retained turn baseline");
            Call(form, "HandleAxis2Sample", 0, sample);
            Assert(((Label)Field(form, "noticeLabel")).Text.Contains("状态读取已恢复"), "old communication error remained after recovery");
            Call(form, "InvalidateDashboardSample");
            Console.WriteLine("PASS UI communication loss and explicit recovery notice");
            Call(form, "DashboardNotice", "离线界面检查：模拟错误提示", true);
            Assert(((Label)Field(form, "noticeLabel")).ForeColor == DashboardTheme.Danger, "error not highlighted");
            Assert(((TextBox)Field(form, "historyBox")).Text.Contains("模拟错误提示"), "error history missing");
            Console.WriteLine("PASS UI error notice and history");
            cards[0].PerformClick(); Render(form, Path.Combine(Path.GetDirectoryName(screenshot), "ui-axis1.png"));
            ((TabControl)Field(form, "detailTabs")).SelectedIndex = 1;
            form.PerformLayout(); Application.DoEvents(); CheckVisibleBounds(form);
            Render(form, Path.Combine(Path.GetDirectoryName(screenshot), "ui-diagnostics.png"));
            Console.WriteLine("PASS UI diagnostics layout");
            Assert(!(bool)Field(form, "cardOpened"), "UI tests connected controller");
            form.Close();
        }
        Console.WriteLine("PASS UI verification; no controller connection or command requested.");
    }
}
