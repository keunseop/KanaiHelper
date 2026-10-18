using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
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

        // UI Controls
        private TextBlock _statusBadge;
        private Border _statusBorder;
        private TextBlock _d3Indicator;
        private ProgressBar _progressBar;
        private TextBox _logBox;
        private Slider _sleepSlider;
        private TextBlock _sleepText;
        private CheckBox _trayCheckBox;

        // Hotkey Button Labels
        private TextBlock _hk1Text;
        private TextBlock _hk2Text;
        private TextBlock _hk3Text;
        private TextBlock _hk4Text;

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
        }

        private void InitializeComponent()
        {
            Title = "KanaiHelper v2.0";
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
                    Opacity = 0.65
                }
            };

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(80) }); // Settings Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) }); // Log Console

            // 1. Header
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
                Text = "⚡",
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
                Text = "v2.0 NATIVE",
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
                Text = "● D3 감지 대기 중...",
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

            // Right Action Stack (Status Badge + Window Controls)
            StackPanel rightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

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
                Text = "대기 중 (IDLE)",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
            };
            _statusBorder.Child = _statusBadge;
            rightStack.Children.Add(_statusBorder);

            // Minimize Button
            Button btnMin = CreateFlatButton("─", delegate
            {
                WindowState = WindowState.Minimized;
            });
            btnMin.Margin = new Thickness(0, 0, 6, 0);
            rightStack.Children.Add(btnMin);

            // Close Button
            Button btnClose = CreateFlatButton("✕", delegate
            {
                if (_config.MinimizeToTray)
                {
                    Hide();
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.ShowBalloonTip(1500, "KanaiHelper", "백그라운드 트레이로 최소화되었습니다.", System.Windows.Forms.ToolTipIcon.Info);
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

            // Card 1: 1-Slot Cube
            _hk1Text = new TextBlock();
            UIElement card1 = CreateMacroCard(
                "💎  1칸 아이템 큐브 변환",
                "반지, 목걸이 등 1칸 아이템 60칸 일괄 변환",
                "세로 우선 탐색 (열 단위 순회)",
                _config.Hotkey1Slot.ToDisplayString(),
                _hk1Text,
                1,
                System.Windows.Media.Color.FromRgb(243, 156, 18),
                delegate { _engine.Start(MacroMode.Cube1Slot); }
            );
            grid.Children.Add(card1);
            Grid.SetRow(card1, 0);
            Grid.SetColumn(card1, 0);

            // Card 2: 2-Slot Cube
            _hk2Text = new TextBlock();
            UIElement card2 = CreateMacroCard(
                "⚔️  2칸 아이템 큐브 변환",
                "무기, 방어구 등 2칸 아이템 30칸 일괄 변환",
                "가로 우선 탐색 (2행씩 건너뜀)",
                _config.Hotkey2Slot.ToDisplayString(),
                _hk2Text,
                2,
                System.Windows.Media.Color.FromRgb(230, 126, 34),
                delegate { _engine.Start(MacroMode.Cube2Slot); }
            );
            grid.Children.Add(card2);
            Grid.SetRow(card2, 0);
            Grid.SetColumn(card2, 1);

            // Card 3: Salvage
            _hk3Text = new TextBlock();
            UIElement card3 = CreateMacroCard(
                "🔨  인벤토리 일괄 분해",
                "대장장이 분해 버튼 클릭 후 60칸 전체 분해",
                "가로 우선 탐색 (1행씩 순차 분해)",
                _config.HotkeySalvage.ToDisplayString(),
                _hk3Text,
                3,
                System.Windows.Media.Color.FromRgb(52, 152, 219),
                delegate { _engine.Start(MacroMode.Salvage); }
            );
            grid.Children.Add(card3);
            Grid.SetRow(card3, 1);
            Grid.SetColumn(card3, 0);

            // Card 4: Reforge
            _hk4Text = new TextBlock();
            UIElement card4 = CreateMacroCard(
                "✨  전설 아이템 재련 1회",
                "인벤토리 맨 왼쪽 위 첫 번째 슬롯 아이템 재련",
                "재련 후 첫 번째 슬롯으로 마우스 복귀",
                _config.HotkeyReforge.ToDisplayString(),
                _hk4Text,
                4,
                System.Windows.Media.Color.FromRgb(155, 89, 182),
                delegate { _engine.Start(MacroMode.Reforge); }
            );
            grid.Children.Add(card4);
            Grid.SetRow(card4, 1);
            Grid.SetColumn(card4, 1);

            return grid;
        }

        private UIElement CreateMacroCard(string title, string desc, string subdesc, string hotkeyStr,
            TextBlock hkDisplay, int hkIndex, System.Windows.Media.Color accentColor, Action onRun)
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
            TextBlock subdescBlock = new TextBlock
            {
                Text = "• " + subdesc,
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(115, 125, 145)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            descStack.Children.Add(descBlock);
            descStack.Children.Add(subdescBlock);
            cardGrid.Children.Add(descStack);
            Grid.SetRow(descStack, 1);

            // Footer Actions (Hotkey Badge Button + Run Test Button)
            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Hotkey Button
            Button hkBtn = new Button
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(1),
                Height = 32,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            hkDisplay.Text = hotkeyStr;
            hkDisplay.FontSize = 12;
            hkDisplay.FontWeight = FontWeights.Bold;
            hkDisplay.Foreground = new SolidColorBrush(accentColor);
            hkDisplay.Margin = new Thickness(10, 0, 10, 0);
            hkBtn.Content = hkDisplay;

            hkBtn.Click += delegate
            {
                StartRecordingHotkey(hkBtn, hkIndex);
            };

            footerGrid.Children.Add(hkBtn);
            Grid.SetColumn(hkBtn, 0);

            // Run Button
            Button runBtn = new Button
            {
                Content = "▶ 실행",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Colors.White),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 48, 65)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(60, 72, 95)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 4, 14, 4),
                Height = 32,
                Cursor = Cursors.Hand
            };
            runBtn.Click += delegate
            {
                onRun();
            };
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
            labelRow.Children.Add(new TextBlock
            {
                Text = "⏱ 클릭 간 대기 지연 (GlobalSleep): ",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 188, 205))
            });
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
            statusRow.Children.Add(new TextBlock
            {
                Text = "취소 안내: 언제든 게임 중 ",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170))
            });
            statusRow.Children.Add(new TextBlock
            {
                Text = "ESC",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60))
            });
            statusRow.Children.Add(new TextBlock
            {
                Text = " 키를 누르면 1ms 이내 즉각 중단",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170))
            });
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
                Content = "닫기(X) 시 트레이 아이콘으로 최소화 (백그라운드 유지)",
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
                Text = "KanaiHelper v2.0이 준비되었습니다.\r\n디아블로 III를 창 모드 또는 전체 창 모드로 실행한 후 단축키를 사용하세요.\r\n"
            };

            logBorder.Child = _logBox;
            return logBorder;
        }

        private Button CreateFlatButton(string text, Action onClick, bool isClose = false)
        {
            Button btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 32,
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 190, 205)),
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            btn.Click += delegate { onClick(); };

            btn.MouseEnter += delegate
            {
                btn.Background = isClose
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60))
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 48, 65));
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

        private void StartRecordingHotkey(Button btn, int index)
        {
            _activeRecordingButton = btn;
            _recordingIndex = index;
            _hook.IsRecordingHotkey = true;

            TextBlock tb = btn.Content as TextBlock;
            if (tb != null)
            {
                tb.Text = "키 입력 대기 중...";
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

                switch (_recordingIndex)
                {
                    case 1: _config.Hotkey1Slot = binding; break;
                    case 2: _config.Hotkey2Slot = binding; break;
                    case 3: _config.HotkeySalvage = binding; break;
                    case 4: _config.HotkeyReforge = binding; break;
                }
                _config.Save();
                _activeRecordingButton = null;
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "KanaiHelper v2.0";
            _notifyIcon.Icon = GetAppIcon();
            _notifyIcon.Visible = true;

            _notifyIcon.DoubleClick += delegate
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            System.Windows.Forms.ContextMenu menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("열기 (Show)", delegate
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            menu.MenuItems.Add("-");
            menu.MenuItems.Add("종료 (Exit)", delegate
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Application.Current.Shutdown();
            });

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
                        _d3Indicator.Text = string.Format("● D3 연결됨 ({0}x{1})", rc.Width, rc.Height);
                        _d3Indicator.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                    }
                }
                else
                {
                    _d3Indicator.Text = "○ Diablo III 미실행";
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
