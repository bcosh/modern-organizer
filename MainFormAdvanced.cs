using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DofusOrganizer
{
    public partial class MainFormAdvanced : Form
    {
        // WinAPI imports
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_RESTORE = 9;
        private const string UNITY_CLASS = "UnityWndClass";

        private List<DofusWindow> dofusWindows = new List<DofusWindow>();
        private FlowLayoutPanel buttonPanel;
        private Button refreshButton;
        private Timer autoRefreshTimer;
        private CheckBox autoRefreshCheckbox;
        private bool isDragging = false;
        private Point dragOffset;
        private Panel headerPanel;
        private Panel settingsPanel;
        private bool settingsVisible = false;

        public MainFormAdvanced()
        {
            InitializeComponent();
            SetupOverlay();
            SetupAutoRefresh();
            RefreshWindows();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 150);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainFormAdvanced";
            this.Text = "Dofus Organizer Advanced";
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 340, 20);
            this.KeyPreview = true;

            this.ResumeLayout(false);
        }

        private void SetupOverlay()
        {
            this.TopMost = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0.95;

            // Header
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 35,
                BackColor = Color.FromArgb(45, 45, 48)
            };
            
            Label titleLabel = new Label
            {
                Text = "⚔️ Dofus Organizer",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 8)
            };
            headerPanel.Controls.Add(titleLabel);

            // Bouton refresh
            refreshButton = new Button
            {
                Text = "🔄",
                Width = 30,
                Height = 28,
                Location = new Point(this.Width - 105, 3),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += (s, e) => RefreshWindows();
            headerPanel.Controls.Add(refreshButton);

            // Bouton paramètres
            Button settingsBtn = new Button
            {
                Text = "⚙",
                Width = 30,
                Height = 28,
                Location = new Point(this.Width - 70, 3),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11)
            };
            settingsBtn.FlatAppearance.BorderSize = 0;
            settingsBtn.Click += ToggleSettings;
            headerPanel.Controls.Add(settingsBtn);

            // Bouton fermeture
            Button closeButton = new Button
            {
                Text = "✖",
                Width = 30,
                Height = 28,
                Location = new Point(this.Width - 35, 3),
                BackColor = Color.FromArgb(232, 17, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            headerPanel.Controls.Add(closeButton);

            // Events pour déplacer
            headerPanel.MouseDown += HeaderPanel_MouseDown;
            headerPanel.MouseMove += HeaderPanel_MouseMove;
            headerPanel.MouseUp += HeaderPanel_MouseUp;
            titleLabel.MouseDown += HeaderPanel_MouseDown;
            titleLabel.MouseMove += HeaderPanel_MouseMove;
            titleLabel.MouseUp += HeaderPanel_MouseUp;

            this.Controls.Add(headerPanel);

            // Panel de paramètres (caché par défaut)
            SetupSettingsPanel();

            // Panel pour les boutons
            buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true,
                Padding = new Padding(5)
            };
            this.Controls.Add(buttonPanel);
        }

        private void SetupSettingsPanel()
        {
            settingsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false
            };

            autoRefreshCheckbox = new CheckBox
            {
                Text = "Auto-refresh (5s)",
                ForeColor = Color.White,
                Location = new Point(10, 10),
                Width = 200,
                Checked = false
            };
            autoRefreshCheckbox.CheckedChanged += (s, e) =>
            {
                autoRefreshTimer.Enabled = autoRefreshCheckbox.Checked;
            };
            settingsPanel.Controls.Add(autoRefreshCheckbox);

            Label opacityLabel = new Label
            {
                Text = "Opacité:",
                ForeColor = Color.White,
                Location = new Point(10, 40),
                AutoSize = true
            };
            settingsPanel.Controls.Add(opacityLabel);

            TrackBar opacityTracker = new TrackBar
            {
                Location = new Point(70, 35),
                Width = 200,
                Minimum = 50,
                Maximum = 100,
                Value = 95,
                TickFrequency = 10
            };
            opacityTracker.ValueChanged += (s, e) =>
            {
                this.Opacity = opacityTracker.Value / 100.0;
            };
            settingsPanel.Controls.Add(opacityTracker);

            Label infoLabel = new Label
            {
                Text = "Cliquez sur les boutons pour switcher",
                ForeColor = Color.Gray,
                Location = new Point(10, 70),
                AutoSize = true,
                Font = new Font("Segoe UI", 8)
            };
            settingsPanel.Controls.Add(infoLabel);

            this.Controls.Add(settingsPanel);
        }

        private void SetupAutoRefresh()
        {
            autoRefreshTimer = new Timer
            {
                Interval = 5000,
                Enabled = false
            };
            autoRefreshTimer.Tick += (s, e) => RefreshWindows();
        }

        private void ToggleSettings(object sender, EventArgs e)
        {
            settingsVisible = !settingsVisible;
            
            if (settingsVisible)
            {
                settingsPanel.Height = 100;
                settingsPanel.Visible = true;
            }
            else
            {
                settingsPanel.Height = 0;
                settingsPanel.Visible = false;
            }

            AdjustFormSize();
        }

        private void HeaderPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = new Point(e.X, e.Y);
            }
        }

        private void HeaderPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = this.PointToScreen(e.Location);
                newLocation.Offset(-dragOffset.X, -dragOffset.Y);
                this.Location = newLocation;
            }
        }

        private void HeaderPanel_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void RefreshWindows()
        {
            dofusWindows.Clear();
            buttonPanel.Controls.Clear();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);

                    if (className.ToString() == UNITY_CLASS)
                    {
                        StringBuilder windowTitle = new StringBuilder(256);
                        GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                        if (!string.IsNullOrEmpty(windowTitle.ToString()))
                        {
                            uint processId;
                            GetWindowThreadProcessId(hWnd, out processId);

                            dofusWindows.Add(new DofusWindow
                            {
                                Handle = hWnd,
                                Title = windowTitle.ToString(),
                                ProcessId = processId
                            });
                        }
                    }
                }
                return true;
            }, IntPtr.Zero);

            for (int i = 0; i < dofusWindows.Count; i++)
            {
                CreateWindowButton(dofusWindows[i], i + 1);
            }

            AdjustFormSize();
        }

        private void CreateWindowButton(DofusWindow window, int index)
        {
            Button btn = new Button
            {
                Text = $"{index}. {TruncateTitle(window.Title, 25)}",
                Width = buttonPanel.Width - 20,
                Height = 45,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Tag = window
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 122, 204);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 100, 180);

            btn.Click += (s, e) => SwitchToWindow(window);

            // Indicateur fenêtre active
            if (GetForegroundWindow() == window.Handle)
            {
                btn.BackColor = Color.FromArgb(0, 100, 180);
                btn.Text = "▶ " + btn.Text;
                btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }

            // Tooltip avec titre complet
            ToolTip tooltip = new ToolTip();
            tooltip.SetToolTip(btn, window.Title);

            buttonPanel.Controls.Add(btn);
        }

        private string TruncateTitle(string title, int maxLength)
        {
            if (title.Length <= maxLength)
                return title;
            return title.Substring(0, maxLength - 3) + "...";
        }

        private void SwitchToWindow(DofusWindow window)
        {
            try
            {
                ShowWindow(window.Handle, SW_RESTORE);
                SetForegroundWindow(window.Handle);
                
                System.Threading.Thread.Sleep(100);
                RefreshWindows();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur: {ex.Message}", "Erreur", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AdjustFormSize()
        {
            int buttonCount = dofusWindows.Count;
            int baseHeight = headerPanel.Height + (settingsVisible ? settingsPanel.Height : 0);
            int newHeight = baseHeight + (buttonCount * 50) + 15;
            
            if (newHeight > Screen.PrimaryScreen.WorkingArea.Height - 100)
            {
                newHeight = Screen.PrimaryScreen.WorkingArea.Height - 100;
            }

            this.Height = Math.Max(100, newHeight);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                autoRefreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}