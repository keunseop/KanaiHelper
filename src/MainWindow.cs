using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace KanaiHelper
{
    public class MainWindow : Window
    {
        private readonly AppConfig _config;
        private readonly MacroEngine _engine;
        private readonly GlobalHook _hook;
        private System.Windows.Forms.NotifyIcon _notifyIcon;

        // UI Controls - Header & Status
        private TextBlock _statusBadge;
        private Border _statusBorder;
        private TextBlock _d3Indicator;
        private Button _btnLang;
        private ProgressBar _progressBar;
        private TextBox _logBox;
        private Slider _sleepSlider;
        private TextBlock _sleepText;
        private TextBlock _sleepLabelText;
        private CheckBox _trayCheckBox;
        private TextBlock _escNoticePrefix;
        private TextBlock _escNoticeSuffix;

        // Card Text Elements (for dynamic language switching)
        private TextBlock _card1Title;
        private TextBlock _card1Desc;
        private TextBlock _card1Sub;
        private Button _card1Run;

        private TextBlock _card2Title;
        private TextBlock _card2Desc;
        private TextBlock _card2Sub;
        private Button _card2Run;

        private TextBlock _card3Title;
        private TextBlock _card3Desc;
        private TextBlock _card3Sub;
        private Button _card3Run;

        private TextBlock _card4Title;
        private TextBlock _card4Desc;
        private TextBlock _card4Sub;
        private Button _card4Run;

        // Hotkey Button Labels
        private TextBlock _hk1Text;
        private TextBlock _hk2Text;
        private TextBlock _hk3Text;
        private TextBlock _hk4Text;

        // Tray Menu Items
        private System.Windows.Forms.MenuItem _trayOpenItem;
        private System.Windows.Forms.MenuItem _trayExitItem;

        private Button _activeRecordingButton = null;
        private int _recordingIndex = 0;

        public MainWindow(AppConfig config, MacroEngine engine, GlobalHook hook)
        {
            _config = config;
            _engine = engine;
            _hook = hook;

            InitializeComponent();
            SetupEvents();
            SetupTrayIcon();
            StartD3Watcher();
            ApplyLocalization();
        }

        private void InitializeComponent()
        {
            Title = "KanaiHelper v1.0";
            Width = 840;
            Height = 680;
            MinWidth = 800;
            MinHeight = 640;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 19, 23));
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
                }
            }
            catch { }

            // Outer Root Border for rounded window corners and border shadow
            Border rootBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 22, 28)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(45, 52, 68)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Effect = new DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    BlurRadius = 24,
                    ShadowDepth = 4,
                    Opacity = 0.5
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(56) }); // Custom Title Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Macro Dashboard Cards
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Settings Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(125) }); // Activity Log Console

            // 1. Title Bar
            mainGrid.Children.Add(CreateHeader());
            Grid.SetRow(mainGrid.Children[0], 0);

            // 2. Main Dashboard (Cards)
            mainGrid.Children.Add(CreateDashboard());
            Grid.SetRow(mainGrid.Children[1], 1);

            // 3. Bottom Settings Bar
            mainGrid.Children.Add(CreateSettingsBar());
            Grid.SetRow(mainGrid.Children[2], 2);

            // 4. Log Box
            mainGrid.Children.Add(CreateLogConsole());
            Grid.SetRow(mainGrid.Children[3], 3);

            rootBorder.Child = mainGrid;
            Content = rootBorder;
        }

        private UIElement CreateHeader()
        {
            Border headerBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 29, 38)),
                CornerRadius = new CornerRadius(12, 12, 0, 0),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 44, 58)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 0, 16, 0)
            };

            // Enable window dragging
            headerBorder.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            Grid headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Title & Subtitle Stack
            StackPanel titleStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock logoIcon = new TextBlock
            {
                Text = "\u26A1",
                FontSize = 22,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18)),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock title = new TextBlock
            {
                Text = "KANAI HELPER",
                FontWeight = FontWeights.Bold,
                FontSize = 17,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 243, 246)),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Malgun Gothic")
            };

            TextBlock badgeVersion = new TextBlock
            {
                Text = "v1.0 NATIVE",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18)),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 243, 156, 18)),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(12, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _d3Indicator = new TextBlock
            {
                Text = I18n.D3Detecting,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 148, 165)),
                VerticalAlignment = VerticalAlignment.Center
            };

            titleStack.Children.Add(logoIcon);
            titleStack.Children.Add(title);
            titleStack.Children.Add(badgeVersion);
            titleStack.Children.Add(_d3Indicator);
            headerGrid.Children.Add(titleStack);
            Grid.SetColumn(titleStack, 0);

            // Right Action Stack (Language Toggle + Status Badge + Window Controls)
            StackPanel rightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            // Language Toggle Button
            SolidColorBrush normalLangBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 62));
            SolidColorBrush normalLangBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(62, 76, 102));
            SolidColorBrush normalLangFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 225, 235));

            SolidColorBrush hoverLangBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(53, 65, 90));
            SolidColorBrush hoverLangBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
            SolidColorBrush hoverLangFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));

            _btnLang = new Button
            {
                Template = CreateButtonTemplate(6),
                Content = I18n.LangToggleBtn,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = normalLangFg,
                Background = normalLangBg,
                BorderBrush = normalLangBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 12, 0),
                Cursor = Cursors.Hand
            };
            _btnLang.MouseEnter += delegate
            {
                _btnLang.Background = hoverLangBg;
                _btnLang.BorderBrush = hoverLangBorder;
                _btnLang.Foreground = hoverLangFg;
            };
            _btnLang.MouseLeave += delegate
            {
                _btnLang.Background = normalLangBg;
                _btnLang.BorderBrush = normalLangBorder;
                _btnLang.Foreground = normalLangFg;
            };
            _btnLang.Click += delegate
            {
                _config.Language = I18n.IsKorean ? "en" : "ko";
                I18n.Language = _config.Language;
                _config.Save();
                ApplyLocalization();
            };
            rightStack.Children.Add(_btnLang);

            _statusBorder = new Border
            {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 46, 204, 113)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            _statusBadge = new TextBlock
            {
                Text = I18n.StatusIdle,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
            };
            _statusBorder.Child = _statusBadge;
            rightStack.Children.Add(_statusBorder);

            // Minimize Button
            Button btnMin = CreateFlatButton("?", delegate
            {
                WindowState = WindowState.Minimized;
            });
            btnMin.Margin = new Thickness(0, 0, 6, 0);
            rightStack.Children.Add(btnMin);

            // Close Button
            Button btnClose = CreateFlatButton("\u2715", delegate
            {
                if (_config.MinimizeToTray)
                {
                    Hide();
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.ShowBalloonTip(1500, I18n.TrayBalloonTitle, I18n.TrayBalloonText, System.Windows.Forms.ToolTipIcon.Info);
                    }
                }
                else
                {
                    Close();
                }
            }, true);
            rightStack.Children.Add(btnClose);

            headerGrid.Children.Add(rightStack);
            Grid.SetColumn(rightStack, 1);

            headerBorder.Child = headerGrid;
            return headerBorder;
        }

        private UIElement CreateDashboard()
        {
            Grid grid = new Grid
            {
                Margin = new Thickness(20, 16, 20, 10)
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Card 1: Salvage (Row 0, Col 0 - Most used)
            _hk1Text = new TextBlock();
            UIElement card1 = CreateMacroCard(
                I18n.Card1Title,
                I18n.Card1Desc,
                I18n.Card1Sub,
                _config.HotkeySalvage.ToDisplayString(),
                _hk1Text,
                1,
                System.Windows.Media.Color.FromRgb(52, 152, 219),
                delegate { _engine.Start(MacroMode.Salvage); },
                out _card1Title, out _card1Desc, out _card1Sub, out _card1Run
            );
            grid.Children.Add(card1);
            Grid.SetRow(card1, 0);
            Grid.SetColumn(card1, 0);

            // Card 2: 1-Slot Cube (Row 0, Col 1)
            _hk2Text = new TextBlock();
            UIElement card2 = CreateMacroCard(
                I18n.Card2Title,
                I18n.Card2Desc,
                I18n.Card2Sub,
                _config.Hotkey1Slot.ToDisplayString(),
                _hk2Text,
                2,
                System.Windows.Media.Color.FromRgb(243, 156, 18),
                delegate { _engine.Start(MacroMode.Cube1Slot); },
                out _card2Title, out _card2Desc, out _card2Sub, out _card2Run
            );
            grid.Children.Add(card2);
            Grid.SetRow(card2, 0);
            Grid.SetColumn(card2, 1);

            // Card 3: 2-Slot Cube (Row 1, Col 0)
            _hk3Text = new TextBlock();
            UIElement card3 = CreateMacroCard(
                I18n.Card3Title,
                I18n.Card3Desc,
                I18n.Card3Sub,
                _config.Hotkey2Slot.ToDisplayString(),
                _hk3Text,
                3,
                System.Windows.Media.Color.FromRgb(230, 126, 34),
                delegate { _engine.Start(MacroMode.Cube2Slot); },
                out _card3Title, out _card3Desc, out _card3Sub, out _card3Run
            );
            grid.Children.Add(card3);
            Grid.SetRow(card3, 1);
            Grid.SetColumn(card3, 0);

            // Card 4: Reforge (Row 1, Col 1)
            _hk4Text = new TextBlock();
            UIElement card4 = CreateMacroCard(
                I18n.Card4Title,
                I18n.Card4Desc,
                I18n.Card4Sub,
                _config.HotkeyReforge.ToDisplayString(),
                _hk4Text,
                4,
                System.Windows.Media.Color.FromRgb(155, 89, 182),
                delegate { _engine.Start(MacroMode.Reforge); },
                out _card4Title, out _card4Desc, out _card4Sub, out _card4Run
            );
            grid.Children.Add(card4);
            Grid.SetRow(card4, 1);
            Grid.SetColumn(card4, 1);

            return grid;
        }

        private UIElement CreateMacroCard(string title, string desc, string subdesc, string hotkeyStr,
            TextBlock hkDisplay, int hkIndex, System.Windows.Media.Color accentColor, Action onRun,
            out TextBlock titleOut, out TextBlock descOut, out TextBlock subdescOut, out Button runBtnOut)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 30, 40)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 46, 62)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(6),
                Padding = new Thickness(16, 14, 16, 14)
            };

            Grid cardGrid = new Grid();
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Desc
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Spacer
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer actions

            // Title
            TextBlock titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 245)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            titleOut = titleBlock;
            cardGrid.Children.Add(titleBlock);
            Grid.SetRow(titleBlock, 0);

            // Description
            StackPanel descStack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
            TextBlock descBlock = new TextBlock
            {
                Text = desc,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 168, 185)),
                TextWrapping = TextWrapping.Wrap
            };
            descOut = descBlock;

            TextBlock subdescBlock = new TextBlock
            {
                Text = "??" + subdesc,
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(115, 125, 145)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            subdescOut = subdescBlock;

            descStack.Children.Add(descBlock);
            descStack.Children.Add(subdescBlock);
            cardGrid.Children.Add(descStack);
            Grid.SetRow(descStack, 1);

            // Footer Actions (Hotkey Badge Button + Run Test Button)
            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Hotkey Button
            SolidColorBrush normalHkBg = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, accentColor.R, accentColor.G, accentColor.B));
            SolidColorBrush normalHkBorder = new SolidColorBrush(accentColor);
            SolidColorBrush normalHkFg = new SolidColorBrush(accentColor);

            SolidColorBrush hoverHkBg = new SolidColorBrush(accentColor);
            SolidColorBrush hoverHkBorder = new SolidColorBrush(System.Windows.Media.Colors.White);
            SolidColorBrush hoverHkFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 19, 23));

            Button hkBtn = new Button
            {
                Template = CreateButtonTemplate(6),
                Background = normalHkBg,
                BorderBrush = normalHkBorder,
                BorderThickness = new Thickness(1),
                Height = 32,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            hkDisplay.Text = hotkeyStr;
            hkDisplay.FontSize = 12;
            hkDisplay.FontWeight = FontWeights.Bold;
            hkDisplay.Foreground = normalHkFg;
            hkDisplay.Margin = new Thickness(12, 0, 12, 0);
            hkBtn.Content = hkDisplay;

            hkBtn.MouseEnter += delegate
            {
                if (_activeRecordingButton != hkBtn)
                {
                    hkBtn.Background = hoverHkBg;
                    hkBtn.BorderBrush = hoverHkBorder;
                    hkDisplay.Foreground = hoverHkFg;
                }
            };
            hkBtn.MouseLeave += delegate
            {
                if (_activeRecordingButton != hkBtn)
                {
                    hkBtn.Background = normalHkBg;
                    hkBtn.BorderBrush = normalHkBorder;
                    hkDisplay.Foreground = normalHkFg;
                }
            };

            hkBtn.Click += delegate
            {
                StartRecordingHotkey(hkBtn, hkIndex);
            };

            footerGrid.Children.Add(hkBtn);
            Grid.SetColumn(hkBtn, 0);

            // Run Button
            SolidColorBrush normalRunBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 44, 60));
            SolidColorBrush normalRunBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 70, 94));
            SolidColorBrush normalRunFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(225, 232, 242));

            SolidColorBrush hoverRunBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(39, 174, 96));
            SolidColorBrush hoverRunBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            SolidColorBrush hoverRunFg = new SolidColorBrush(System.Windows.Media.Colors.White);

            Button runBtn = new Button
            {
                Template = CreateButtonTemplate(6),
                Content = I18n.RunButton,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = normalRunFg,
                Background = normalRunBg,
                BorderBrush = normalRunBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 4, 14, 4),
                Height = 32,
                Cursor = Cursors.Hand
            };
            runBtn.MouseEnter += delegate
            {
                runBtn.Background = hoverRunBg;
                runBtn.BorderBrush = hoverRunBorder;
                runBtn.Foreground = hoverRunFg;
            };
            runBtn.MouseLeave += delegate
            {
                runBtn.Background = normalRunBg;
                runBtn.BorderBrush = normalRunBorder;
                runBtn.Foreground = normalRunFg;
            };
            runBtn.Click += delegate
            {
                onRun();
            };
            runBtnOut = runBtn;
            footerGrid.Children.Add(runBtn);
            Grid.SetColumn(runBtn, 1);

            cardGrid.Children.Add(footerGrid);
            Grid.SetRow(footerGrid, 3);

            card.Child = cardGrid;
            return card;
        }

        private UIElement CreateSettingsBar()
        {
            Border barBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 27, 35)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 44, 58)),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(24, 10, 24, 10)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Sleep slider
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Progress & Tray

            // Left: GlobalSleep Slider
            StackPanel sleepPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel labelRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 4) };
            
            _sleepLabelText = new TextBlock
            {
                Text = I18n.SleepLabel,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 188, 205))
            };
            labelRow.Children.Add(_sleepLabelText);

            _sleepText = new TextBlock
            {
                Text = _config.GlobalSleep + " ms",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18))
            };
            labelRow.Children.Add(_sleepText);
            sleepPanel.Children.Add(labelRow);

            _sleepSlider = new Slider
            {
                Minimum = 5,
                Maximum = 200,
                Value = _config.GlobalSleep,
                TickFrequency = 5,
                IsSnapToTickEnabled = true,
                Margin = new Thickness(0, 2, 0, 0)
            };
            _sleepSlider.ValueChanged += delegate(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int val = (int)e.NewValue;
                _config.GlobalSleep = val;
                _config.Save();
                if (_sleepText != null)
                {
                    _sleepText.Text = val + " ms";
                }
            };
            sleepPanel.Children.Add(_sleepSlider);

            grid.Children.Add(sleepPanel);
            Grid.SetColumn(sleepPanel, 0);

            // Right: Progress Bar & Tray Checkbox
            StackPanel rightPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            StackPanel statusRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            _escNoticePrefix = new TextBlock
            {
                Text = I18n.EscNoticePrefix,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170))
            };
            statusRow.Children.Add(_escNoticePrefix);

            statusRow.Children.Add(new TextBlock
            {
                Text = "ESC",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60))
            });

            _escNoticeSuffix = new TextBlock
            {
                Text = I18n.EscNoticeSuffix,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170))
            };
            statusRow.Children.Add(_escNoticeSuffix);
            rightPanel.Children.Add(statusRow);

            _progressBar = new ProgressBar
            {
                Height = 6,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 42, 56)),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113)),
                BorderThickness = new Thickness(0)
            };
            rightPanel.Children.Add(_progressBar);

            _trayCheckBox = new CheckBox
            {
                Content = I18n.TrayOption,
                IsChecked = _config.MinimizeToTray,
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170)),
                Margin = new Thickness(0, 6, 0, 0)
            };
            _trayCheckBox.Checked += delegate { _config.MinimizeToTray = true; };
            _trayCheckBox.Unchecked += delegate { _config.MinimizeToTray = false; };
            rightPanel.Children.Add(_trayCheckBox);

            grid.Children.Add(rightPanel);
            Grid.SetColumn(rightPanel, 2);

            barBorder.Child = grid;
            return barBorder;
        }

        private UIElement CreateLogConsole()
        {
            Border logBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(14, 15, 19)),
                CornerRadius = new CornerRadius(0, 0, 12, 12),
                Padding = new Thickness(14, 8, 14, 10)
            };

            _logBox = new TextBox
            {
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 165, 185)),
                FontFamily = new System.Windows.Media.FontFamily("Consolas, Courier New"),
                FontSize = 11,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = "KanaiHelper v1.0 Ready.\r\n"
            };

            logBorder.Child = _logBox;
            return logBorder;
        }

        private static ControlTemplate CreateButtonTemplate(int cornerRadius = 6)
        {
            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(cornerRadius));
            border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(contentPresenter);
            template.VisualTree = border;
            return template;
        }

        private System.Windows.Media.Color GetAccentColorForIndex(int index)
        {
            switch (index)
            {
                case 1: return System.Windows.Media.Color.FromRgb(52, 152, 219); // 1: Salvage (Blue)
                case 2: return System.Windows.Media.Color.FromRgb(243, 156, 18); // 2: 1-Slot (Gold)
                case 3: return System.Windows.Media.Color.FromRgb(230, 126, 34); // 3: 2-Slot (Orange)
                case 4: return System.Windows.Media.Color.FromRgb(155, 89, 182); // 4: Reforge (Purple)
                default: return System.Windows.Media.Colors.White;
            }
        }

        private Button CreateFlatButton(string text, Action onClick, bool isClose = false)
        {
            Button btn = new Button
            {
                Template = CreateButtonTemplate(4),
                Content = text,
                Width = 32,
                Height = 32,
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205)),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            btn.Click += delegate { onClick(); };

            btn.MouseEnter += delegate
            {
                btn.Background = isClose
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 56, 75));
                btn.Foreground = System.Windows.Media.Brushes.White;
            };
            btn.MouseLeave += delegate
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205));
            };

            return btn;
        }

        private void SetupEvents()
        {
            _engine.Log += delegate(string msg)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _logBox.AppendText(msg + "\r\n");
                    _logBox.ScrollToEnd();
                }));
            };

            _engine.StatusChanged += delegate(string status, bool running)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    _statusBadge.Text = status;
                    if (running)
                    {
                        _statusBadge.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                        _statusBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                        _statusBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 243, 156, 18));
                    }
                    else
                    {
                        _statusBadge.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                        _statusBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                        _statusBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 46, 204, 113));
                    }
                }));
            };

            _engine.ProgressChanged += delegate(int cur, int total)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (total > 0)
                    {
                        _progressBar.Value = (double)cur / total * 100.0;
                    }
                    else
                    {
                        _progressBar.Value = 0;
                    }
                }));
            };

            _hook.OnHotkeyRecorded = delegate(HotkeyBinding binding)
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    FinishRecordingHotkey(binding);
                }));
            };
        }

        private void ApplyLocalization()
        {
            if (_btnLang != null) _btnLang.Content = I18n.LangToggleBtn;

            if (_card1Title != null) _card1Title.Text = I18n.Card1Title;
            if (_card1Desc != null) _card1Desc.Text = I18n.Card1Desc;
            if (_card1Sub != null) _card1Sub.Text = "??" + I18n.Card1Sub;
            if (_card1Run != null) _card1Run.Content = I18n.RunButton;

            if (_card2Title != null) _card2Title.Text = I18n.Card2Title;
            if (_card2Desc != null) _card2Desc.Text = I18n.Card2Desc;
            if (_card2Sub != null) _card2Sub.Text = "??" + I18n.Card2Sub;
            if (_card2Run != null) _card2Run.Content = I18n.RunButton;

            if (_card3Title != null) _card3Title.Text = I18n.Card3Title;
            if (_card3Desc != null) _card3Desc.Text = I18n.Card3Desc;
            if (_card3Sub != null) _card3Sub.Text = "??" + I18n.Card3Sub;
            if (_card3Run != null) _card3Run.Content = I18n.RunButton;

            if (_card4Title != null) _card4Title.Text = I18n.Card4Title;
            if (_card4Desc != null) _card4Desc.Text = I18n.Card4Desc;
            if (_card4Sub != null) _card4Sub.Text = "??" + I18n.Card4Sub;
            if (_card4Run != null) _card4Run.Content = I18n.RunButton;

            if (_sleepLabelText != null) _sleepLabelText.Text = I18n.SleepLabel;
            if (_escNoticePrefix != null) _escNoticePrefix.Text = I18n.EscNoticePrefix;
            if (_escNoticeSuffix != null) _escNoticeSuffix.Text = I18n.EscNoticeSuffix;
            if (_trayCheckBox != null) _trayCheckBox.Content = I18n.TrayOption;

            if (_trayOpenItem != null) _trayOpenItem.Text = I18n.TrayOpen;
            if (_trayExitItem != null) _trayExitItem.Text = I18n.TrayExit;

            if (!_engine.IsRunning && _statusBadge != null)
            {
                _statusBadge.Text = I18n.StatusIdle;
            }
        }

        private void StartRecordingHotkey(Button btn, int index)
        {
            if (_activeRecordingButton != null)
            {
                CancelRecordingHotkey();
            }

            _activeRecordingButton = btn;
            _recordingIndex = index;
            _hook.IsRecordingHotkey = true;

            btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 231, 76, 60));
            btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));

            TextBlock tb = btn.Content as TextBlock;
            if (tb != null)
            {
                tb.Text = I18n.PressKey;
                tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            }
        }

        private void FinishRecordingHotkey(HotkeyBinding binding)
        {
            if (_activeRecordingButton != null)
            {
                TextBlock tb = _activeRecordingButton.Content as TextBlock;
                if (tb != null)
                {
                    tb.Text = binding.ToDisplayString();
                }

                System.Windows.Media.Color accent = GetAccentColorForIndex(_recordingIndex);
                _activeRecordingButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, accent.R, accent.G, accent.B));
                _activeRecordingButton.BorderBrush = new SolidColorBrush(accent);
                if (tb != null) tb.Foreground = new SolidColorBrush(accent);

                switch (_recordingIndex)
                {
                    case 1:
                        _config.HotkeySalvage = binding;
                        break;
                    case 2:
                        _config.Hotkey1Slot = binding;
                        break;
                    case 3:
                        _config.Hotkey2Slot = binding;
                        break;
                    case 4:
                        _config.HotkeyReforge = binding;
                        break;
                }

                _config.Save();
                _activeRecordingButton = null;
            }
        }

        private void CancelRecordingHotkey()
        {
            if (_activeRecordingButton != null)
            {
                _hook.IsRecordingHotkey = false;
                TextBlock tb = _activeRecordingButton.Content as TextBlock;
                System.Windows.Media.Color accent = GetAccentColorForIndex(_recordingIndex);
                _activeRecordingButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, accent.R, accent.G, accent.B));
                _activeRecordingButton.BorderBrush = new SolidColorBrush(accent);

                if (tb != null)
                {
                    switch (_recordingIndex)
                    {
                        case 1:
                            tb.Text = _config.HotkeySalvage.ToDisplayString();
                            break;
                        case 2:
                            tb.Text = _config.Hotkey1Slot.ToDisplayString();
                            break;
                        case 3:
                            tb.Text = _config.Hotkey2Slot.ToDisplayString();
                            break;
                        case 4:
                            tb.Text = _config.HotkeyReforge.ToDisplayString();
                            break;
                    }
                    tb.Foreground = new SolidColorBrush(accent);
                }
                _activeRecordingButton = null;
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "KanaiHelper v1.0";
            _notifyIcon.Icon = GetAppIcon();
            _notifyIcon.Visible = true;

            _notifyIcon.DoubleClick += delegate
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            _trayOpenItem = new System.Windows.Forms.MenuItem(I18n.TrayOpen, delegate
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            _trayExitItem = new System.Windows.Forms.MenuItem(I18n.TrayExit, delegate
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Application.Current.Shutdown();
            });

            menu.MenuItems.Add(_trayOpenItem);
            menu.MenuItems.Add("-");
            menu.MenuItems.Add(_trayExitItem);

            _notifyIcon.ContextMenu = menu;
        }

        private System.Drawing.Icon GetAppIcon()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string iconPath = Path.Combine(baseDir, "assets", "app.ico");
                if (File.Exists(iconPath))
                {
                    return new System.Drawing.Icon(iconPath);
                }

                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                System.Drawing.Icon exeIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                if (exeIcon != null)
                {
                    return exeIcon;
                }
            }
            catch
            {
                // ignore
            }
            return SystemIcons.Application;
        }

        private void StartD3Watcher()
        {
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1.5);
            timer.Tick += delegate
            {
                IntPtr hWnd = NativeMethods.FindWindow(MacroEngine.D3_WINDOW_CLASS, null);
                if (hWnd != IntPtr.Zero)
                {
                    NativeMethods.RECT rc;
                    if (NativeMethods.GetClientRect(hWnd, out rc))
                    {
                        _d3Indicator.Text = I18n.D3Detected(rc.Width, rc.Height);
                        _d3Indicator.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                    }
                }
                else
                {
                    _d3Indicator.Text = I18n.D3Detecting;
                    _d3Indicator.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 148, 165));
                }
            };
            timer.Start();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
            _hook.Dispose();
            base.OnClosing(e);
        }
    }
}

