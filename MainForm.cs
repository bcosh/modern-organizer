using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace DofusOrganizer
{
    public enum DisplayMode
    {
        Configuration,  // Mode liste avec tous les personnages
        Compact        // Mode bouton rond pour cycler
    }

    public enum InteractionMode
    {
        Classique,  // Mode tray avec raccourcis clavier uniquement
        Tactile     // Mode overlay visible
    }

    public partial class MainForm : Form
    {
        // WinAPI imports pour la gestion des fenêtres
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

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int HOTKEY_CYCLE = 1;
        private const int HOTKEY_LEADER = 2;

        private const int SW_RESTORE = 9;
        private const string UNITY_CLASS = "UnityWndClass";

        private List<DofusWindow> dofusWindows = new List<DofusWindow>();
        private FlowLayoutPanel buttonPanel;
        private Button refreshButton;
        private Button settingsButton;
        private Button validateButton;
        private Panel settingsPanel;
        private bool settingsVisible = false;
        private bool isDragging = false;
        private Point dragOffset;
        private Panel headerPanel;

        // Mode et cycle
        private DisplayMode currentMode = DisplayMode.Configuration;
        private int currentWindowIndex = 0;
        private Button compactButton;
        private ContextMenuStrip contextMenu;
        private Point compactButtonPosition = new Point(10, 10); // Position par défaut: haut-gauche
        private bool isCompactButtonDragging = false;
        private Point compactButtonDragOffset;

        // Raccourcis clavier et chef de groupe
        private Keys cycleHotkey = Keys.None;
        private Keys leaderHotkey = Keys.None;
        private int leaderWindowIndex = -1; // -1 = pas de chef défini

        // Mode d'interaction
        private InteractionMode interactionMode = InteractionMode.Tactile;
        private NotifyIcon trayIcon;

        // Flag pour éviter de sauvegarder pendant le chargement
        private bool isLoading = false;
        private ToolTip toolTip;

        public MainForm()
        {
            isLoading = true;
            this.toolTip = new ToolTip();

            // Charger les paramètres AVANT de créer les contrôles
            UserSettings settings = UserSettings.Load();

            // Appliquer les paramètres de base
            cycleHotkey = (Keys)settings.CycleHotkey;
            leaderHotkey = (Keys)settings.LeaderHotkey;
            leaderWindowIndex = settings.LeaderWindowIndex;
            interactionMode = (InteractionMode)settings.InteractionMode;

            // Position du bouton compact
            if (settings.CompactButtonX >= 0 && settings.CompactButtonY >= 0)
            {
                compactButtonPosition = new Point(settings.CompactButtonX, settings.CompactButtonY);
            }
            else
            {
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                compactButtonPosition = new Point(screenWidth - 100, 10);
            }

            InitializeComponent();

            // Appliquer l'opacité après InitializeComponent
            this.Opacity = settings.Opacity;

            SetupOverlay();
            SetupCompactMode();
            SetupContextMenu();
            SetupTrayIcon();

            // Enregistrer les hotkeys après que le Handle soit créé
            if (cycleHotkey != Keys.None)
            {
                RegisterHotKey(this.Handle, HOTKEY_CYCLE, 0, (uint)cycleHotkey);
            }
            if (leaderHotkey != Keys.None)
            {
                RegisterHotKey(this.Handle, HOTKEY_LEADER, 0, (uint)leaderHotkey);
            }

            RefreshWindows();

            isLoading = false;
        }

        private void LoadSettings()
        {
            UserSettings settings = UserSettings.Load();

            // Debug: afficher les valeurs chargées
            System.Diagnostics.Debug.WriteLine($"Chargement settings: CycleHotkey={settings.CycleHotkey}, LeaderHotkey={settings.LeaderHotkey}, Opacity={settings.Opacity}, Mode={settings.InteractionMode}");

            // Charger les raccourcis clavier
            cycleHotkey = (Keys)settings.CycleHotkey;
            leaderHotkey = (Keys)settings.LeaderHotkey;

            // Enregistrer les hotkeys si définis
            if (cycleHotkey != Keys.None)
            {
                RegisterHotKey(this.Handle, HOTKEY_CYCLE, 0, (uint)cycleHotkey);
            }
            if (leaderHotkey != Keys.None)
            {
                RegisterHotKey(this.Handle, HOTKEY_LEADER, 0, (uint)leaderHotkey);
            }

            // Charger le chef de groupe
            leaderWindowIndex = settings.LeaderWindowIndex;

            // Charger le mode d'interaction
            interactionMode = (InteractionMode)settings.InteractionMode;

            // Charger l'opacité
            this.Opacity = settings.Opacity;

            // Charger la position du bouton compact
            if (settings.CompactButtonX >= 0 && settings.CompactButtonY >= 0)
            {
                compactButtonPosition = new Point(settings.CompactButtonX, settings.CompactButtonY);
            }
            else
            {
                // Position par défaut (haut-droite)
                int screenWidth = Screen.PrimaryScreen.WorkingArea.Width;
                compactButtonPosition = new Point(screenWidth - 100, 10);
            }
        }

        private void SaveSettings()
        {
            // Ne pas sauvegarder pendant le chargement initial
            if (isLoading)
                return;

            UserSettings settings = new UserSettings
            {
                CycleHotkey = (int)cycleHotkey,
                LeaderHotkey = (int)leaderHotkey,
                LeaderWindowIndex = leaderWindowIndex,
                InteractionMode = (int)interactionMode,
                Opacity = this.Opacity,
                CompactButtonX = compactButtonPosition.X,
                CompactButtonY = compactButtonPosition.Y
            };

            System.Diagnostics.Debug.WriteLine($"Sauvegarde settings: CycleHotkey={settings.CycleHotkey}, LeaderHotkey={settings.LeaderHotkey}, Opacity={settings.Opacity}, Mode={settings.InteractionMode}");

            settings.Save();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Configuration de la fenêtre principale
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 150);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.Text = "Dofus Organizer";
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 320, 20);

            this.ResumeLayout(false);
        }

        private void SetupOverlay()
        {
            // Rendre la fenêtre toujours au-dessus
            this.TopMost = true;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0.95;

            // Header avec possibilité de déplacer la fenêtre
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(45, 45, 48),
                Cursor = Cursors.SizeAll
            };
            
            Label titleLabel = new Label
            {
                Text = "⚔️ Dofus Organizer",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(10, 6)
            };
            headerPanel.Controls.Add(titleLabel);

            // Bouton de rafraîchissement
            refreshButton = new Button
            {
                Text = "🔄",
                Width = 30,
                Height = 25,
                Location = new Point(this.Width - 105, 2),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            refreshButton.FlatAppearance.BorderSize = 0;
            refreshButton.Click += (s, e) => RefreshWindows();
            headerPanel.Controls.Add(refreshButton);

            // Bouton de paramètres
            settingsButton = new Button
            {
                Text = "⚙",
                Width = 30,
                Height = 25,
                Location = new Point(this.Width - 70, 2),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 11)
            };
            settingsButton.FlatAppearance.BorderSize = 0;
            settingsButton.Click += ToggleSettings;
            headerPanel.Controls.Add(settingsButton);

            // Bouton de fermeture
            Button closeButton = new Button
            {
                Text = "✖",
                Width = 30,
                Height = 25,
                Location = new Point(this.Width - 35, 2),
                BackColor = Color.FromArgb(232, 17, 35),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => this.Close();
            headerPanel.Controls.Add(closeButton);

            // Events pour déplacer la fenêtre
            headerPanel.MouseDown += HeaderPanel_MouseDown;
            headerPanel.MouseMove += HeaderPanel_MouseMove;
            headerPanel.MouseUp += HeaderPanel_MouseUp;
            titleLabel.MouseDown += HeaderPanel_MouseDown;
            titleLabel.MouseMove += HeaderPanel_MouseMove;
            titleLabel.MouseUp += HeaderPanel_MouseUp;

            // Panel pour les boutons de fenêtres
            buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30),
                AutoScroll = true,
                Padding = new Padding(5)
            };
            this.Controls.Add(buttonPanel);

            // Panel de paramètres (caché par défaut)
            SetupSettingsPanel();

            this.Controls.Add(headerPanel);
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

        private void SetupSettingsPanel()
        {
            settingsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 0,
                BackColor = Color.FromArgb(40, 40, 40),
                Visible = false,
                AutoScroll = true,
                Padding = new Padding(5)
            };

            Label positionInfo = new Label
            {
                Text = "Déplacez le bouton par drag & drop",
                ForeColor = Color.FromArgb(150, 150, 150),
                Location = new Point(10, 148),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                Visible = interactionMode == InteractionMode.Tactile
            };
            settingsPanel.Controls.Add(positionInfo);

            // Opacité
            Label opacityLabel = new Label
            {
                Text = "Opacité:",
                ForeColor = Color.White,
                Location = new Point(10, 15),
                AutoSize = true
            };
            settingsPanel.Controls.Add(opacityLabel);

            TrackBar opacityTracker = new TrackBar
            {
                Location = new Point(140, 10),
                Width = 130,
                Minimum = 50,
                Maximum = 100,
                Value = (int)(this.Opacity * 100),
                TickFrequency = 10
            };
            opacityTracker.ValueChanged += (s, e) =>
            {
                this.Opacity = opacityTracker.Value / 100.0;
                SaveSettings();
            };
            settingsPanel.Controls.Add(opacityTracker);

            // Raccourci clavier pour cycler
            Label cycleHotkeyLabel = new Label
            {
                Text = "Touche pour cycler:",
                ForeColor = Color.White,
                Location = new Point(10, 55),
                AutoSize = true
            };
            settingsPanel.Controls.Add(cycleHotkeyLabel);

            TextBox cycleHotkeyBox = new TextBox
            {
                Location = new Point(140, 52),
                Width = 130,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = cycleHotkey == Keys.None ? "Non définie" : cycleHotkey.ToString()
            };
            cycleHotkeyBox.KeyDown += (s, e) =>
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu) return;
                UnregisterHotKey(this.Handle, HOTKEY_CYCLE);
                cycleHotkey = e.KeyCode;
                cycleHotkeyBox.Text = cycleHotkey.ToString();
                RegisterHotKey(this.Handle, HOTKEY_CYCLE, 0, (uint)cycleHotkey);
                SaveSettings();
            };
            cycleHotkeyBox.Enter += (s, e) => { cycleHotkeyBox.Text = "Appuyez sur une touche..."; };
            cycleHotkeyBox.Leave += (s, e) => { if (cycleHotkeyBox.Text == "Appuyez sur une touche...") { cycleHotkeyBox.Text = cycleHotkey == Keys.None ? "Non définie" : cycleHotkey.ToString(); } };
            settingsPanel.Controls.Add(cycleHotkeyBox);

            // Raccourci clavier pour le chef
            Label leaderHotkeyLabel = new Label
            {
                Text = "Touche chef de groupe:",
                ForeColor = Color.White,
                Location = new Point(10, 85),
                AutoSize = true
            };
            settingsPanel.Controls.Add(leaderHotkeyLabel);

            TextBox leaderHotkeyBox = new TextBox
            {
                Location = new Point(140, 82),
                Width = 130,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                ReadOnly = true,
                Text = leaderHotkey == Keys.None ? "Non définie" : leaderHotkey.ToString()
            };
            leaderHotkeyBox.KeyDown += (s, e) =>
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Alt || e.KeyCode == Keys.Menu) return;
                UnregisterHotKey(this.Handle, HOTKEY_LEADER);
                leaderHotkey = e.KeyCode;
                leaderHotkeyBox.Text = leaderHotkey.ToString();
                RegisterHotKey(this.Handle, HOTKEY_LEADER, 0, (uint)leaderHotkey);
                SaveSettings();
            };
            leaderHotkeyBox.Enter += (s, e) => { leaderHotkeyBox.Text = "Appuyez sur une touche..."; };
            leaderHotkeyBox.Leave += (s, e) => { if (leaderHotkeyBox.Text == "Appuyez sur une touche...") { leaderHotkeyBox.Text = leaderHotkey == Keys.None ? "Non définie" : leaderHotkey.ToString(); } };
            settingsPanel.Controls.Add(leaderHotkeyBox);

            // Mode d'interaction
            Label modeLabel = new Label
            {
                Text = "Mode d'interaction:",
                ForeColor = Color.White,
                Location = new Point(10, 115),
                AutoSize = true
            };
            settingsPanel.Controls.Add(modeLabel);

            ComboBox modeComboBox = new ComboBox
            {
                Location = new Point(140, 112),
                Width = 130,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            modeComboBox.Items.Add("Tactile (Overlay)");
            modeComboBox.Items.Add("Classique (Tray)");
            modeComboBox.SelectedIndex = interactionMode == InteractionMode.Tactile ? 0 : 1;
            modeComboBox.SelectedIndexChanged += (s, e) =>
            {
                interactionMode = modeComboBox.SelectedIndex == 0 ? InteractionMode.Tactile : InteractionMode.Classique;
                positionInfo.Visible = interactionMode == InteractionMode.Tactile;
                SaveSettings();
            };
            settingsPanel.Controls.Add(modeComboBox);

            this.Controls.Add(settingsPanel);
        }

        private void ToggleSettings(object sender, EventArgs e)
        {
            settingsVisible = !settingsVisible;

            if (settingsVisible)
            {
                settingsPanel.Height = 210;
                settingsPanel.Visible = true;
            }
            else
            {
                settingsPanel.Height = 0;
                settingsPanel.Visible = false;
            }

            AdjustFormSize();
        }

        private void RefreshWindows()
        {
            dofusWindows.Clear();
            buttonPanel.Controls.Clear();

            // Énumérer toutes les fenêtres
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    StringBuilder className = new StringBuilder(256);
                    GetClassName(hWnd, className, className.Capacity);

                    // Vérifier si c'est une fenêtre Unity (Dofus)
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

            // Créer les boutons pour chaque fenêtre
            for (int i = 0; i < dofusWindows.Count; i++)
            {
                CreateWindowButton(dofusWindows[i], i + 1);
            }

            // Ajouter le bouton Valider en mode Configuration
            if (currentMode == DisplayMode.Configuration && dofusWindows.Count > 0)
            {
                validateButton = new Button
                {
                    Text = "✓ Valider",
                    Width = buttonPanel.Width - 20,
                    Height = 45,
                    BackColor = Color.FromArgb(0, 180, 100),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font("Segoe UI", 11, FontStyle.Bold),
                    Margin = new Padding(0, 10, 0, 0)
                };
                validateButton.FlatAppearance.BorderSize = 0;
                validateButton.Click += (s, e) => SwitchToCompactMode();
                buttonPanel.Controls.Add(validateButton);
            }

            // Ajuster la taille de la fenêtre
            AdjustFormSize();
        }

        private void CreateWindowButton(DofusWindow window, int index)
        {
            // Panel conteneur pour le bouton + étoile
            Panel containerPanel = new Panel
            {
                Width = buttonPanel.Width - 20,
                Height = 40,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Bouton principal
            Button btn = new Button
            {
                Text = $"{index}. {ParseCharacterName(window.Title)}",
                Width = containerPanel.Width - 35,
                Height = 40,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Tag = window
            };

            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(0, 122, 204);

            btn.Click += (s, e) => SwitchToWindow(window);

            // Ajouter un indicateur si c'est la fenêtre active
            if (GetForegroundWindow() == window.Handle)
            {
                btn.BackColor = Color.FromArgb(0, 100, 180);
                btn.Text = "▶ " + btn.Text;
            }

            containerPanel.Controls.Add(btn);

            // Bouton étoile pour définir le chef
            Button starBtn = new Button
            {
                Text = leaderWindowIndex == index - 1 ? "★" : "☆",
                Width = 30,
                Height = 40,
                Location = new Point(containerPanel.Width - 30, 0),
                BackColor = leaderWindowIndex == index - 1 ? Color.FromArgb(255, 215, 0) : Color.FromArgb(60, 60, 60),
                ForeColor = leaderWindowIndex == index - 1 ? Color.Black : Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 14),
                Tag = index - 1 // Stocker l'index
            };
            this.toolTip.SetToolTip(starBtn, "Chef du groupe");

            starBtn.FlatAppearance.BorderSize = 1;
            starBtn.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 80);

            starBtn.Click += (s, e) =>
            {
                // Basculer le chef de groupe
                if (leaderWindowIndex == (int)starBtn.Tag)
                {
                    leaderWindowIndex = -1; // Retirer le chef
                }
                else
                {
                    leaderWindowIndex = (int)starBtn.Tag; // Définir nouveau chef
                }
                SaveSettings();
                RefreshWindows(); // Rafraîchir pour mettre à jour les étoiles
            };

            containerPanel.Controls.Add(starBtn);

            buttonPanel.Controls.Add(containerPanel);
        }

        private string ParseCharacterName(string title)
        {
            var parts = title.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                return $"{parts[0]} - {parts[1]}";
            }
            return title;
        }

        private void SwitchToWindow(DofusWindow window)
        {
            try
            {
                // Restaurer UNIQUEMENT si la fenêtre est minimisée
                if (IsIconic(window.Handle))
                {
                    ShowWindow(window.Handle, SW_RESTORE);
                }

                // Mettre la fenêtre au premier plan
                SetForegroundWindow(window.Handle);

                // Rafraîchir seulement en mode Configuration
                if (currentMode == DisplayMode.Configuration)
                {
                    System.Threading.Thread.Sleep(100);
                    RefreshWindows();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du switch: {ex.Message}", "Erreur",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AdjustFormSize()
        {
            if (currentMode == DisplayMode.Compact)
            {
                // Mode compact : 90x90 pixels
                this.ClientSize = new Size(90, 90);
                return;
            }

            // Mode Configuration
            int buttonCount = dofusWindows.Count;
            int baseHeight = headerPanel.Height + (settingsVisible ? settingsPanel.Height : 0);
            int extraHeight = (validateButton != null && buttonPanel.Controls.Contains(validateButton)) ? 55 : 0;

            // Limiter à 8 personnages maximum avant de scroller
            int maxDisplayedButtons = Math.Min(buttonCount, 8);
            int newHeight = baseHeight + (maxDisplayedButtons * 45) + 15 + extraHeight;

            // Hauteur maximale de l'écran
            int maxScreenHeight = Screen.PrimaryScreen.WorkingArea.Height - 100;
            if (newHeight > maxScreenHeight)
            {
                newHeight = maxScreenHeight;
            }

            this.Height = Math.Max(150, newHeight);
        }

        private void SetupCompactMode()
        {
            // Créer le bouton compact (caché initialement) - 90x90px
            compactButton = new Button
            {
                Width = 90,
                Height = 90,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 36, FontStyle.Bold),
                Text = "1",
                Location = new Point(0, 0),
                Visible = false
            };
            compactButton.FlatAppearance.BorderSize = 0;

            // Events pour dragging
            compactButton.MouseDown += CompactButton_MouseDown;
            compactButton.MouseMove += CompactButton_MouseMove;
            compactButton.MouseUp += CompactButton_MouseUp;

            // Rendre le bouton avec des coins arrondis
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 20; // Rayon des coins arrondis
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(90 - radius, 0, radius, radius, 270, 90);
            path.AddArc(90 - radius, 90 - radius, radius, radius, 0, 90);
            path.AddArc(0, 90 - radius, radius, radius, 90, 90);
            path.CloseFigure();
            compactButton.Region = new Region(path);

            this.Controls.Add(compactButton);
        }

        private Point compactButtonMouseDownLocation;

        private void CompactButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                compactButtonMouseDownLocation = e.Location;
                compactButtonDragOffset = e.Location;
            }
        }

        private void CompactButton_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Calculer la distance de déplacement
                int deltaX = Math.Abs(e.X - compactButtonMouseDownLocation.X);
                int deltaY = Math.Abs(e.Y - compactButtonMouseDownLocation.Y);

                // Si déplacement > 5 pixels, c'est un drag
                if (deltaX > 5 || deltaY > 5)
                {
                    isCompactButtonDragging = true;
                    Point newLocation = this.PointToScreen(e.Location);
                    newLocation.Offset(-compactButtonDragOffset.X, -compactButtonDragOffset.Y);
                    this.Location = newLocation;

                    // Sauvegarder la nouvelle position
                    compactButtonPosition = newLocation;
                }
            }
        }

        private void CompactButton_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!isCompactButtonDragging)
                {
                    // C'était un clic simple -> cycle
                    CycleToNextWindow(sender, e);
                }
                else
                {
                    // C'était un drag, sauvegarder la nouvelle position
                    SaveSettings();
                }
                isCompactButtonDragging = false;
            }
        }

        private void SetupContextMenu()
        {
            contextMenu = new ContextMenuStrip();

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("⚙ Réglages");
            settingsItem.Click += (s, e) => SwitchToConfigurationMode();

            contextMenu.Items.Add(settingsItem);
        }

        private void SetupTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = false,
                Text = "Dofus Organizer"
            };

            // Menu contextuel du tray
            ContextMenuStrip trayMenu = new ContextMenuStrip();

            ToolStripMenuItem showItem = new ToolStripMenuItem("Afficher");
            showItem.Click += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                trayIcon.Visible = false;
            };
            trayMenu.Items.Add(showItem);

            trayMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Quitter");
            exitItem.Click += (s, e) => Application.Exit();
            trayMenu.Items.Add(exitItem);

            trayIcon.ContextMenuStrip = trayMenu;

            // Double-clic pour afficher
            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                trayIcon.Visible = false;
            };
        }

        private void ApplyInteractionMode()
        {
            if (interactionMode == InteractionMode.Classique)
            {
                // Mode classique: cacher l'overlay et aller dans le tray
                this.Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "Dofus Organizer",
                    "L'application est maintenant dans la barre des tâches. Utilisez les raccourcis clavier pour changer de fenêtre.",
                    ToolTipIcon.Info);
            }
            else
            {
                // Mode tactile: afficher l'overlay
                trayIcon.Visible = false;
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }
        }

        private void SwitchToCompactMode()
        {
            if (dofusWindows.Count == 0)
            {
                MessageBox.Show("Aucune fenêtre Dofus détectée !", "Attention",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            currentMode = DisplayMode.Compact;
            currentWindowIndex = 0;

            // Appliquer le mode d'interaction choisi
            if (interactionMode == InteractionMode.Classique)
            {
                // Mode classique: aller dans le tray
                this.Hide();
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(2000, "Dofus Organizer",
                    "L'application est maintenant dans la barre des tâches. Utilisez les raccourcis clavier pour changer de fenêtre.",
                    ToolTipIcon.Info);
            }
            else
            {
                // Mode tactile: afficher le bouton compact
                // Masquer tous les contrôles du mode Configuration
                headerPanel.Visible = false;
                settingsPanel.Visible = false;
                buttonPanel.Visible = false;

                // Afficher le bouton compact
                compactButton.Visible = true;
                compactButton.ContextMenuStrip = contextMenu;

                // Redimensionner
                this.FormBorderStyle = FormBorderStyle.None;
                AdjustFormSize();

                // Positionner selon la préférence utilisateur
                this.Location = compactButtonPosition;
            }

            // Switch vers la première fenêtre
            if (dofusWindows.Count > 0)
            {
                SwitchToWindow(dofusWindows[currentWindowIndex]);
            }
        }

        private void SwitchToConfigurationMode()
        {
            currentMode = DisplayMode.Configuration;

            // Si on est en mode classique, sortir du tray
            if (interactionMode == InteractionMode.Classique)
            {
                trayIcon.Visible = false;
                this.Show();
                this.WindowState = FormWindowState.Normal;
            }

            // Masquer le bouton compact
            compactButton.Visible = false;

            // Afficher les contrôles du mode Configuration
            headerPanel.Visible = true;
            buttonPanel.Visible = true;

            // Redimensionner
            this.FormBorderStyle = FormBorderStyle.None;
            this.ClientSize = new Size(300, 150);
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - 320, 20);

            // Rafraîchir pour afficher la liste
            RefreshWindows();
        }

        private void CycleToNextWindow(object sender, EventArgs e)
        {
            if (dofusWindows.Count == 0)
                return;

            // Passer à la fenêtre suivante
            currentWindowIndex = (currentWindowIndex + 1) % dofusWindows.Count;

            // Mettre à jour le numéro affiché sur le bouton
            compactButton.Text = (currentWindowIndex + 1).ToString();

            // Switch vers cette fenêtre
            SwitchToWindow(dofusWindows[currentWindowIndex]);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;

            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();

                if (id == HOTKEY_CYCLE)
                {
                    // Cycler vers la fenêtre suivante
                    CycleToNextWindow(null, null);
                }
                else if (id == HOTKEY_LEADER)
                {
                    // Aller vers le chef de groupe
                    SwitchToLeader();
                }
            }

            base.WndProc(ref m);
        }

        private void SwitchToLeader()
        {
            if (leaderWindowIndex >= 0 && leaderWindowIndex < dofusWindows.Count)
            {
                currentWindowIndex = leaderWindowIndex;

                if (currentMode == DisplayMode.Compact)
                {
                    compactButton.Text = (currentWindowIndex + 1).ToString();
                }

                SwitchToWindow(dofusWindows[leaderWindowIndex]);
            }
            else
            {
                // Si en mode compact, afficher une notification subtile
                if (currentMode == DisplayMode.Compact)
                {
                    // Faire clignoter le bouton pour indiquer qu'il n'y a pas de chef défini
                    var originalColor = compactButton.BackColor;
                    compactButton.BackColor = Color.FromArgb(200, 50, 50);
                    var timer = new System.Windows.Forms.Timer { Interval = 200 };
                    timer.Tick += (s, e) =>
                    {
                        compactButton.BackColor = originalColor;
                        timer.Stop();
                        timer.Dispose();
                    };
                    timer.Start();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Sauvegarder les paramètres
            SaveSettings();

            // Désenregistrer les hotkeys
            if (cycleHotkey != Keys.None)
            {
                UnregisterHotKey(this.Handle, HOTKEY_CYCLE);
            }
            if (leaderHotkey != Keys.None)
            {
                UnregisterHotKey(this.Handle, HOTKEY_LEADER);
            }

            // Nettoyer le tray icon
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }

            base.OnFormClosing(e);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_TOPMOST pour rester au-dessus
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST
                return cp;
            }
        }
    }

    public class DofusWindow
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public uint ProcessId { get; set; }
    }

    public class UserSettings
    {
        public int CycleHotkey { get; set; } = 0; // Keys.None
        public int LeaderHotkey { get; set; } = 0; // Keys.None
        public int LeaderWindowIndex { get; set; } = -1;
        public int InteractionMode { get; set; } = 0; // Classique par défaut
        public double Opacity { get; set; } = 0.95;
        public int CompactButtonX { get; set; } = -1;
        public int CompactButtonY { get; set; } = -1;

        private static string GetSettingsPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appFolder = Path.Combine(appData, "DofusOrganizer");
            Directory.CreateDirectory(appFolder);
            return Path.Combine(appFolder, "settings.json");
        }

        public static UserSettings Load()
        {
            try
            {
                string path = GetSettingsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
                }
            }
            catch (Exception)
            {
                // Si erreur, retourner settings par défaut
            }
            return new UserSettings();
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsPath();
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception)
            {
                // Ignorer les erreurs de sauvegarde
            }
        }
    }
}