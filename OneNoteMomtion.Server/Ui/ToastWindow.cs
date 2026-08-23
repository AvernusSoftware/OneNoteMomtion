using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace OneNoteMomtion.Server.Ui
{
    internal sealed class ToastWindow : Form
    {
        private const int VisibleMilliseconds = 1600;

        private const int WsExNoActivate = 0x08000000;

        private const int WsExToolWindow = 0x00000080;

        private const int DwmWindowAttributeCornerPreference = 33;
        private const int DwmWindowCornerPreferenceRound = 2;

        private const int HorizontalPadding = 16;
        private const int VerticalPadding = 10;

        private const int CursorOffset = 18;

        private readonly Timer _timer;

        private ToastWindow(string message)
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            MinimizeBox = false;
            MaximizeBox = false;
            BackColor = Color.FromArgb(43, 43, 43);
            ForeColor = Color.FromArgb(245, 245, 245);
            Text = "OneNoteMomtion";

            Label label = new()
            {
                AutoSize = true,
                Text = message,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = ForeColor,
                Location = new Point(HorizontalPadding, VerticalPadding),
            };

            Controls.Add(label);

            ClientSize = new Size(label.PreferredWidth + (HorizontalPadding * 2), label.PreferredHeight + (VerticalPadding * 2));

            _timer = new Timer { Interval = VisibleMilliseconds };
            _timer.Tick += (_, _) =>
            {
                _timer.Stop();
                Close();
            };
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WsExNoActivate | WsExToolWindow;
                return parameters;
            }
        }
        public static ToastWindow Show(string message, int visibleMilliseconds = VisibleMilliseconds)
        {
            ToastWindow toast = new(string.IsNullOrWhiteSpace(message) ? "Done." : message);
            toast._timer.Interval = Math.Max(400, visibleMilliseconds);
            toast.PositionAtCursor();
            toast.Show();
            toast.ApplyRoundedCorners();
            toast._timer.Start();

            return toast;
        }

        private void PositionAtCursor()
        {
            Point cursor = Cursor.Position;
            Rectangle workingArea = Screen.FromPoint(cursor).WorkingArea;

            int x = cursor.X + CursorOffset;
            int y = cursor.Y + CursorOffset;

            x = Math.Min(x, workingArea.Right - Width);
            y = Math.Min(y, workingArea.Bottom - Height);

            Location = new Point(Math.Max(x, workingArea.Left), Math.Max(y, workingArea.Top));
        }

        private void ApplyRoundedCorners()
        {
            try
            {
                int preference = DwmWindowCornerPreferenceRound;
                DwmSetWindowAttribute(Handle, DwmWindowAttributeCornerPreference, ref preference, sizeof(int));
            }
            catch (Exception)
            {
                // The attribute only exists from Windows 11 - on older systems the window stays rectangular.
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
