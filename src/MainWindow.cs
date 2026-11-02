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

        // Top Navigation Tabs
        private Button _tabKanaiBtn;
        private Button _tabRepeatBtn;
        private UIElement _kanaiDashboardView;
        private UIElement _repeatHelperView;
        private int _activeMainTab = 0; // 0: Kanai, 1: Repeat Helper

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

        // Hotkey Button Labels (Kanai Cards 1..4)
        private TextBlock _hk1Text;
        private TextBlock _hk2Text;
        private TextBlock _hk3Text;
        private TextBlock _hk4Text;

        // Skill Auto-Repeat UI Controls
        private Button[] _presetTabBtns = new Button[4];
        private TextBlock[] _presetTabTexts = new TextBlock[4];
        private Button _btnRepeatNew;
        private Button _btnRepeatSave;
        private Button _btnRepeatLoad;

        // Section 1: Toggle Skill Auto-Repeat
        private TextBlock _lblRepeatSkillsSection;
        private TextBlock _lblRepeatStartStop;
        private Button _btnRepeatToggleHk;
        private TextBlock _txtRepeatToggleHk;
        private Button _btnRepeatState;
        private TextBlock _txtRepeatState;

        private TextBlock _colHeaderEnable;
        private TextBlock _colHeaderSkill;
        private TextBlock _colHeaderKey;
        private TextBlock _colHeaderInterval;

        private CheckBox[] _skillCheckBoxes = new CheckBox[6];
        private TextBlock[] _skillRowLabels = new TextBlock[6];
        private Button[] _skillKeyBtns = new Button[6];
        private TextBlock[] _skillKeyTexts = new TextBlock[6];
        private TextBox[] _skillIntervalBoxes = new TextBox[6];

        // Section 2: Hold Mouse Rapid-Click
        private TextBlock _lblMouseHoldSection;
        private Border _leftHoldSubCard;
        private Border _rightHoldSubCard;
        private TextBlock _lblLeftClickTitle;
        private TextBlock _lblRightClickTitle;
        private TextBlock _lblLeftHoldKey;
        private TextBlock _lblRightHoldKey;
        private TextBlock _lblLeftHoldInterval;
        private TextBlock _lblRightHoldInterval;
        private Button _btnLeftClickHk;
        private TextBlock _txtLeftClickHk;
        private TextBox _txtLeftClickMs;
        private Button _btnRightClickHk;
        private TextBlock _txtRightClickHk;
        private TextBox _txtRightClickMs;

        private bool _isUpdatingPresetUi = false;
        private bool _isRepeatDirty = false;

        // Help (?) popup texts
        private TextBlock _helpSkillsTitle;
        private TextBlock _helpSkillsBody;
        private TextBlock _helpMouseTitle;
        private TextBlock _helpMouseBody;

        // Tray Menu Items
        private System.Windows.Forms.MenuItem _trayOpenItem;
        private System.Windows.Forms.MenuItem _trayExitItem;

        private Button _activeRecordingButton = null;
        private int _recordingIndex = 0;
        // _recordingIndex conventions:
        // 1..4   : Kanai Card Hotkeys
        // 10..15 : Repeat Skill 1..6 Keys
        // 100    : Repeat Start/Stop Toggle Key
        // 101    : Left Click Hold Key
        // 102    : Right Click Hold Key

        public MainWindow(AppConfig config, MacroEngine engine, GlobalHook hook)
        {
            _config = config;
            _engine = engine;
            _hook = hook;

            InitializeComponent();
            SetupEvents();
            SetupTrayIcon();
            StartD3Watcher();
            LoadPresetToUi(_config.ActiveRepeatPreset);
            ApplyLocalization();
        }

        private void InitializeComponent()
        {
            Title = "KanaiHelper v1.1";
            Width = 860;
            Height = 780;
            MinWidth = 820;
            MinHeight = 740;
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
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(54) }); // 0: Custom Title Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // 1: Mode Navigation Tabs
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: Content Area
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 3: Settings Bar
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(105) }); // 4: Activity Log Console

            // 0. Title Bar
            UIElement header = CreateHeader();
            mainGrid.Children.Add(header);
            Grid.SetRow(header, 0);

            // 1. Navigation Tab Bar
            UIElement navBar = CreateModeTabBar();
            mainGrid.Children.Add(navBar);
            Grid.SetRow(navBar, 1);

            // 2. Main Views (Kanai Dashboard & Repeat Helper)
            Grid contentContainer = new Grid();
            _kanaiDashboardView = CreateDashboard();
            _repeatHelperView = CreateRepeatHelperView();
            _repeatHelperView.Visibility = Visibility.Collapsed;

            contentContainer.Children.Add(_kanaiDashboardView);
            contentContainer.Children.Add(_repeatHelperView);
            mainGrid.Children.Add(contentContainer);
            Grid.SetRow(contentContainer, 2);

            // 3. Bottom Settings Bar
            UIElement settingsBar = CreateSettingsBar();
            mainGrid.Children.Add(settingsBar);
            Grid.SetRow(settingsBar, 3);

            // 4. Log Box
            UIElement logConsole = CreateLogConsole();
            mainGrid.Children.Add(logConsole);
            Grid.SetRow(logConsole, 4);

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
                Text = "v1.1 NATIVE",
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

            StackPanel rightStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

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
                _config.SaveGeneralSettings();
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

            // Minimize Button ('\u2500')
            Button btnMin = CreateFlatButton("\u2500", delegate
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

        private UIElement CreateModeTabBar()
        {
            Border navBorder = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 25, 33)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(36, 42, 56)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 6, 20, 6)
            };

            StackPanel tabStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _tabKanaiBtn = new Button
            {
                Template = CreateButtonTemplate(6),
                Content = I18n.TabKanai,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(18, 5, 18, 5),
                Margin = new Thickness(0, 0, 10, 0),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _tabKanaiBtn.Click += delegate { SwitchMainTab(0); };

            _tabRepeatBtn = new Button
            {
                Template = CreateButtonTemplate(6),
                Content = I18n.TabRepeat,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(18, 5, 18, 5),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _tabRepeatBtn.Click += delegate { SwitchMainTab(1); };

            tabStack.Children.Add(_tabKanaiBtn);
            tabStack.Children.Add(_tabRepeatBtn);

            navBorder.Child = tabStack;
            UpdateMainTabStyles();
            return navBorder;
        }

        private void SwitchMainTab(int tabIndex)
        {
            if (_activeRecordingButton != null)
            {
                CancelRecordingHotkey();
            }

            _activeMainTab = tabIndex;
            if (_kanaiDashboardView != null)
                _kanaiDashboardView.Visibility = (tabIndex == 0) ? Visibility.Visible : Visibility.Collapsed;
            if (_repeatHelperView != null)
                _repeatHelperView.Visibility = (tabIndex == 1) ? Visibility.Visible : Visibility.Collapsed;

            UpdateMainTabStyles();
        }

        private void UpdateMainTabStyles()
        {
            if (_tabKanaiBtn == null || _tabRepeatBtn == null) return;

            if (_activeMainTab == 0)
            {
                _tabKanaiBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 243, 156, 18));
                _tabKanaiBtn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                _tabKanaiBtn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));

                _tabRepeatBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 35, 46));
                _tabRepeatBtn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 56, 74));
                _tabRepeatBtn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 170, 190));
            }
            else
            {
                _tabRepeatBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 52, 152, 219));
                _tabRepeatBtn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                _tabRepeatBtn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250));

                _tabKanaiBtn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 35, 46));
                _tabKanaiBtn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 56, 74));
                _tabKanaiBtn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(160, 170, 190));
            }
        }

        private UIElement CreateRepeatHelperView()
        {
            Grid root = new Grid
            {
                Margin = new Thickness(20, 10, 20, 10)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Preset Tabs (세팅 1~4) + Reset/Save/Load
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 1: Main Toggle Skill Auto-Repeat Card
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 2: Independent Hold Mouse Rapid-Click Card

            // =========================================================================
            // Row 0: Top Preset Selector (세팅 1 ~ 4) + File Action Buttons
            // =========================================================================
            Grid topBar = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel presetTabStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            for (int p = 0; p < 4; p++)
            {
                int presetIndex = p;
                TextBlock tabText = new TextBlock
                {
                    Text = I18n.RepeatPresetTab(p + 1),
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                _presetTabTexts[p] = tabText;

                Button tabBtn = new Button
                {
                    Template = CreateButtonTemplate(5),
                    Content = tabText,
                    Width = 92,
                    Height = 32,
                    Margin = new Thickness(0, 0, 6, 0),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand
                };
                tabBtn.Click += delegate
                {
                    SwitchRepeatPreset(presetIndex);
                };

                _presetTabBtns[p] = tabBtn;
                presetTabStack.Children.Add(tabBtn);
            }
            topBar.Children.Add(presetTabStack);
            Grid.SetColumn(presetTabStack, 0);

            StackPanel actionBtns = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _btnRepeatNew = CreateSecondaryActionButton(I18n.RepeatBtnNew, delegate
            {
                RepeatPreset cur = _config.GetActivePreset();
                cur.ResetToBlank();
                LoadPresetToUi(_config.ActiveRepeatPreset);
                MarkRepeatDirty(true);
                _engine.EmitCustomLog(I18n.LogPresetReset(_config.ActiveRepeatPreset + 1));
            });
            _btnRepeatNew.Margin = new Thickness(0, 0, 6, 0);

            _btnRepeatSave = CreateSecondaryActionButton(I18n.RepeatBtnSave, delegate
            {
                _config.SaveRepeatSettings();
                MarkRepeatDirty(false);
                _engine.EmitCustomLog(I18n.LogPresetSaved);
            });
            _btnRepeatSave.Margin = new Thickness(0, 0, 6, 0);

            _btnRepeatLoad = CreateSecondaryActionButton(I18n.RepeatBtnLoad, delegate
            {
                if (_activeRecordingButton != null)
                {
                    CancelRecordingHotkey();
                }
                _config.LoadRepeatSettings();
                LoadPresetToUi(_config.ActiveRepeatPreset);
                MarkRepeatDirty(false);
                _engine.RefreshRepeatStatusBadge();
                _engine.EmitCustomLog(I18n.LogPresetLoaded);
            });

            actionBtns.Children.Add(_btnRepeatNew);
            actionBtns.Children.Add(_btnRepeatSave);
            actionBtns.Children.Add(_btnRepeatLoad);
            topBar.Children.Add(actionBtns);
            Grid.SetColumn(actionBtns, 2);

            root.Children.Add(topBar);
            Grid.SetRow(topBar, 0);

            // =========================================================================
            // Row 1: Section 1 — Toggle Skill Auto-Repeat Card (F3 ON/OFF + Skill 1..6)
            // =========================================================================
            Border skillsCard = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 30, 40)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 49, 66)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid skillsCardGrid = new Grid();
            skillsCardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 0: Header Banner (Title + F3 Toggle Key + STOP/RUNNING button)
            skillsCardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 1: Table Column Header
            skillsCardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 2: 6 Skill Rows

            // 0: Header Banner inside Skill Auto-Repeat Card
            Border toggleBanner = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 24, 33)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 64)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 7, 12, 7),
                Margin = new Thickness(0, 0, 0, 8)
            };

            Grid bannerGrid = new Grid();
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            StackPanel bannerLeft = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            _lblRepeatSkillsSection = new TextBlock
            {
                Text = I18n.RepeatSkillsSection,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 248)),
                VerticalAlignment = VerticalAlignment.Center
            };
            bannerLeft.Children.Add(_lblRepeatSkillsSection);
            bannerLeft.Children.Add(CreateHelpButton(out _helpSkillsTitle, out _helpSkillsBody));
            bannerGrid.Children.Add(bannerLeft);
            Grid.SetColumn(bannerLeft, 0);

            // Right of Banner: [시작/종료 단축키: F3] + [STOP / RUNNING] button
            StackPanel bannerRight = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel toggleKeyDescStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            _lblRepeatStartStop = new TextBlock
            {
                Text = I18n.RepeatStartStopLabel,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(200, 210, 225)),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            toggleKeyDescStack.Children.Add(_lblRepeatStartStop);
            bannerRight.Children.Add(toggleKeyDescStack);

            _txtRepeatToggleHk = new TextBlock();
            _btnRepeatToggleHk = CreateKeySlotButton(_txtRepeatToggleHk, _config.RepeatToggleKey.ToDisplayString(), 100, false);
            _btnRepeatToggleHk.Width = 86;
            _btnRepeatToggleHk.Height = 32;
            _btnRepeatToggleHk.Margin = new Thickness(0, 0, 10, 0);
            bannerRight.Children.Add(_btnRepeatToggleHk);

            _txtRepeatState = new TextBlock
            {
                Text = "■ STOP",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 87, 87))
            };

            _btnRepeatState = new Button
            {
                Template = CreateButtonTemplate(5),
                Content = _txtRepeatState,
                Width = 106,
                Height = 32,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 231, 76, 60)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            _btnRepeatState.Click += delegate
            {
                _engine.ToggleRepeat();
            };
            bannerRight.Children.Add(_btnRepeatState);

            bannerGrid.Children.Add(bannerRight);
            Grid.SetColumn(bannerRight, 2);

            toggleBanner.Child = bannerGrid;
            skillsCardGrid.Children.Add(toggleBanner);
            Grid.SetRow(toggleBanner, 0);

            // 1: Table Column Header
            Grid colHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            colHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(145) });

            _colHeaderEnable = CreateTableHeaderLabel(I18n.RepeatColEnable);
            _colHeaderSkill = CreateTableHeaderLabel(I18n.RepeatColSkill);
            _colHeaderKey = CreateTableHeaderLabel(I18n.RepeatColKey);
            _colHeaderInterval = CreateTableHeaderLabel(I18n.RepeatColInterval);

            colHeaderGrid.Children.Add(_colHeaderEnable);
            Grid.SetColumn(_colHeaderEnable, 0);
            colHeaderGrid.Children.Add(_colHeaderSkill);
            Grid.SetColumn(_colHeaderSkill, 1);
            colHeaderGrid.Children.Add(_colHeaderKey);
            Grid.SetColumn(_colHeaderKey, 2);
            colHeaderGrid.Children.Add(_colHeaderInterval);
            Grid.SetColumn(_colHeaderInterval, 4);

            skillsCardGrid.Children.Add(colHeaderGrid);
            Grid.SetRow(colHeaderGrid, 1);

            // 2: 6 Skill Rows
            Grid rowsGrid = new Grid();
            for (int i = 0; i < 6; i++)
            {
                rowsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                int slotIdx = i;

                Border rowBorder = new Border
                {
                    Background = new SolidColorBrush(i % 2 == 0
                        ? System.Windows.Media.Color.FromRgb(31, 36, 48)
                        : System.Windows.Media.Color.FromRgb(28, 33, 44)),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 1.5, 0, 1.5),
                    Padding = new Thickness(0, 1, 8, 1)
                };

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(137) });

                // Col 0: CheckBox
                CheckBox cb = new CheckBox
                {
                    IsChecked = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Cursor = Cursors.Hand
                };
                cb.Checked += delegate
                {
                    if (!_isUpdatingPresetUi)
                    {
                        _config.GetActivePreset().Skills[slotIdx].Enabled = true;
                        MarkRepeatDirty(true);
                    }
                };
                cb.Unchecked += delegate
                {
                    if (!_isUpdatingPresetUi)
                    {
                        _config.GetActivePreset().Skills[slotIdx].Enabled = false;
                        MarkRepeatDirty(true);
                    }
                };
                _skillCheckBoxes[i] = cb;
                rowGrid.Children.Add(cb);
                Grid.SetColumn(cb, 0);

                // Col 1: Skill Label ("기술 1" ~ "기술 6")
                TextBlock skillLbl = new TextBlock
                {
                    Text = I18n.RepeatSkillRowLabel(i + 1),
                    FontSize = 12.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 228, 240)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                _skillRowLabels[i] = skillLbl;
                rowGrid.Children.Add(skillLbl);
                Grid.SetColumn(skillLbl, 1);

                // Col 2: Repeat Key Button
                TextBlock keyTxt = new TextBlock();
                _skillKeyTexts[i] = keyTxt;
                Button keyBtn = CreateKeySlotButton(keyTxt, "", 10 + i, true);
                keyBtn.Height = 25;
                keyBtn.Margin = new Thickness(6, 0, 4, 0);
                _skillKeyBtns[i] = keyBtn;
                rowGrid.Children.Add(keyBtn);
                Grid.SetColumn(keyBtn, 2);

                // Col 3: Clear Key ('✕') Button
                Button clearBtn = CreateFlatButton("\u2715", delegate
                {
                    if (_activeRecordingButton == _skillKeyBtns[slotIdx])
                    {
                        CancelRecordingHotkey();
                    }
                    _config.GetActivePreset().Skills[slotIdx].Key = new HotkeyBinding();
                    _skillKeyTexts[slotIdx].Text = "";
                    MarkRepeatDirty(true);
                });
                clearBtn.Width = 22;
                clearBtn.Height = 22;
                clearBtn.FontSize = 10;
                rowGrid.Children.Add(clearBtn);
                Grid.SetColumn(clearBtn, 3);

                // Col 4: Interval(ms) TextBox
                TextBox intervalBox = CreateIntervalTextBox("0", delegate(int val)
                {
                    if (!_isUpdatingPresetUi)
                    {
                        _config.GetActivePreset().Skills[slotIdx].IntervalMs = val;
                        MarkRepeatDirty(true);
                    }
                });
                intervalBox.Height = 25;
                intervalBox.Margin = new Thickness(10, 0, 0, 0);
                _skillIntervalBoxes[i] = intervalBox;
                rowGrid.Children.Add(intervalBox);
                Grid.SetColumn(intervalBox, 4);

                rowBorder.Child = rowGrid;
                rowsGrid.Children.Add(rowBorder);
                Grid.SetRow(rowBorder, i);
            }

            skillsCardGrid.Children.Add(rowsGrid);
            Grid.SetRow(rowsGrid, 2);

            skillsCard.Child = skillsCardGrid;
            root.Children.Add(skillsCard);
            Grid.SetRow(skillsCard, 1);

            // =========================================================================
            // Row 2: Section 2 — Independent Hold-to-Repeat Mouse Rapid-Click Card
            // =========================================================================
            Border mouseHoldCard = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(26, 30, 40)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 49, 66)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10)
            };

            StackPanel mouseHoldStack = new StackPanel();

            // Header Row with Title & Explanation Badge
            Grid mouseHeaderRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            mouseHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            mouseHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _lblMouseHoldSection = new TextBlock
            {
                Text = I18n.RepeatMouseHoldSection,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 248)),
                VerticalAlignment = VerticalAlignment.Center
            };
            StackPanel mouseTitleStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            mouseTitleStack.Children.Add(_lblMouseHoldSection);
            mouseTitleStack.Children.Add(CreateHelpButton(out _helpMouseTitle, out _helpMouseBody));
            mouseHeaderRow.Children.Add(mouseTitleStack);
            Grid.SetColumn(mouseTitleStack, 0);

            mouseHoldStack.Children.Add(mouseHeaderRow);

            // 2-Column Sub-Cards: Left-Click Repeat (Loot) & Right-Click Repeat (Kadala Gamble)
            Grid holdColsGrid = new Grid();
            holdColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            holdColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            holdColsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left-Click Hold Sub-Card
            _txtLeftClickHk = new TextBlock();
            _leftHoldSubCard = CreateHoldMouseSubCard(
                I18n.RepeatLeftClickTitle,
                101,
                _txtLeftClickHk,
                out _lblLeftClickTitle,
                out _lblLeftHoldKey,
                out _lblLeftHoldInterval,
                out _btnLeftClickHk,
                out _txtLeftClickMs,
                delegate
                {
                    _config.GetActivePreset().LeftClickKey = new HotkeyBinding();
                    UpdateHoldKeyDisplay(101);
                    MarkRepeatDirty(true);
                },
                delegate(int val)
                {
                    if (!_isUpdatingPresetUi)
                    {
                        _config.GetActivePreset().LeftClickIntervalMs = val;
                        MarkRepeatDirty(true);
                    }
                }
            );
            holdColsGrid.Children.Add(_leftHoldSubCard);
            Grid.SetColumn(_leftHoldSubCard, 0);

            // Right-Click Hold Sub-Card
            _txtRightClickHk = new TextBlock();
            _rightHoldSubCard = CreateHoldMouseSubCard(
                I18n.RepeatRightClickTitle,
                102,
                _txtRightClickHk,
                out _lblRightClickTitle,
                out _lblRightHoldKey,
                out _lblRightHoldInterval,
                out _btnRightClickHk,
                out _txtRightClickMs,
                delegate
                {
                    _config.GetActivePreset().RightClickKey = new HotkeyBinding();
                    UpdateHoldKeyDisplay(102);
                    MarkRepeatDirty(true);
                },
                delegate(int val)
                {
                    if (!_isUpdatingPresetUi)
                    {
                        _config.GetActivePreset().RightClickIntervalMs = val;
                        MarkRepeatDirty(true);
                    }
                }
            );
            holdColsGrid.Children.Add(_rightHoldSubCard);
            Grid.SetColumn(_rightHoldSubCard, 2);

            mouseHoldStack.Children.Add(holdColsGrid);
            mouseHoldCard.Child = mouseHoldStack;

            root.Children.Add(mouseHoldCard);
            Grid.SetRow(mouseHoldCard, 2);

            return root;
        }

        private Border CreateHoldMouseSubCard(
            string titleText,
            int recordIdx,
            TextBlock keyDisplayBlock,
            out TextBlock titleLblOut,
            out TextBlock holdKeyLblOut,
            out TextBlock intervalLblOut,
            out Button keyBtnOut,
            out TextBox msBoxOut,
            Action onClearKey,
            Action<int> onIntervalChanged)
        {
            Border subCard = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 24, 33)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 64)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8)
            };

            StackPanel stack = new StackPanel();

            TextBlock titleLbl = new TextBlock
            {
                Text = titleText,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            titleLblOut = titleLbl;
            stack.Children.Add(titleLbl);

            Grid ctrlGrid = new Grid();
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // "누르고 있을 키"
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Key Button
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Clear '✕'
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // "간격"
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(64) }); // ms TextBox
            ctrlGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // "ms"

            TextBlock holdKeyLbl = new TextBlock
            {
                Text = I18n.RepeatHoldKeyLabel,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 185, 205)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            holdKeyLblOut = holdKeyLbl;
            ctrlGrid.Children.Add(holdKeyLbl);
            Grid.SetColumn(holdKeyLbl, 0);

            Button keyBtn = CreateKeySlotButton(keyDisplayBlock, "", recordIdx, false);
            keyBtn.Height = 26;
            keyBtnOut = keyBtn;
            ctrlGrid.Children.Add(keyBtn);
            Grid.SetColumn(keyBtn, 1);

            Button clearBtn = CreateFlatButton("\u2715", delegate
            {
                if (_activeRecordingButton == keyBtn)
                {
                    CancelRecordingHotkey();
                }
                onClearKey();
            });
            clearBtn.Width = 22;
            clearBtn.Height = 22;
            clearBtn.FontSize = 10;
            clearBtn.Margin = new Thickness(2, 0, 8, 0);
            ctrlGrid.Children.Add(clearBtn);
            Grid.SetColumn(clearBtn, 2);

            TextBlock intervalLbl = new TextBlock
            {
                Text = I18n.RepeatHoldIntervalLabel,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(175, 185, 205)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            intervalLblOut = intervalLbl;
            ctrlGrid.Children.Add(intervalLbl);
            Grid.SetColumn(intervalLbl, 3);

            TextBox msBox = CreateIntervalTextBox("0", onIntervalChanged);
            msBox.Height = 26;
            msBoxOut = msBox;
            ctrlGrid.Children.Add(msBox);
            Grid.SetColumn(msBox, 4);

            TextBlock msUnit = new TextBlock
            {
                Text = "ms",
                FontSize = 11.5,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(140, 150, 170)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            ctrlGrid.Children.Add(msUnit);
            Grid.SetColumn(msUnit, 5);

            stack.Children.Add(ctrlGrid);
            subCard.Child = stack;
            return subCard;
        }

        /// <summary>
        /// Creates a small round "?" help button. Hovering shows a popup layer immediately,
        /// clicking toggles it (so it can stay open while reading).
        /// </summary>
        private Button CreateHelpButton(out TextBlock titleOut, out TextBlock bodyOut)
        {
            SolidColorBrush normalBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 62));
            SolidColorBrush normalBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 84, 110));
            SolidColorBrush normalFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 182, 205));
            SolidColorBrush hoverBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250));
            SolidColorBrush hoverFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250));

            Button btn = new Button
            {
                Template = CreateButtonTemplate(9),
                Content = "?",
                Width = 18,
                Height = 18,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = normalFg,
                Background = normalBg,
                BorderBrush = normalBorder,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Help
            };

            TextBlock title = new TextBlock
            {
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250)),
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock body = new TextBlock
            {
                FontSize = 12,
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(215, 222, 235))
            };
            StackPanel content = new StackPanel { MaxWidth = 340 };
            content.Children.Add(title);
            content.Children.Add(body);

            ToolTip tip = new ToolTip
            {
                Content = content,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(24, 28, 38)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 84, 110)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 10, 14, 12),
                Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                HasDropShadow = true
            };
            btn.ToolTip = tip;
            ToolTipService.SetInitialShowDelay(btn, 0);
            ToolTipService.SetShowDuration(btn, 60000);
            ToolTipService.SetPlacement(btn, System.Windows.Controls.Primitives.PlacementMode.Bottom);

            btn.MouseEnter += delegate
            {
                btn.BorderBrush = hoverBorder;
                btn.Foreground = hoverFg;
            };
            btn.MouseLeave += delegate
            {
                btn.BorderBrush = normalBorder;
                btn.Foreground = normalFg;
            };
            btn.Click += delegate
            {
                tip.PlacementTarget = btn;
                tip.IsOpen = !tip.IsOpen;
            };

            titleOut = title;
            bodyOut = body;
            return btn;
        }

        private void UpdateHoldKeyDisplay(int index)
        {
            RepeatPreset preset = _config.GetActivePreset();
            if (index == 101 && _txtLeftClickHk != null)
            {
                if (preset.LeftClickKey.KeyCode == 0)
                {
                    _txtLeftClickHk.Text = I18n.HoldKeyPlaceholder;
                    _txtLeftClickHk.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 120, 140));
                }
                else
                {
                    _txtLeftClickHk.Text = preset.LeftClickKey.ToDisplayString(false);
                    _txtLeftClickHk.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 236, 245));
                }
            }
            else if (index == 102 && _txtRightClickHk != null)
            {
                if (preset.RightClickKey.KeyCode == 0)
                {
                    _txtRightClickHk.Text = I18n.HoldKeyPlaceholder;
                    _txtRightClickHk.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 120, 140));
                }
                else
                {
                    _txtRightClickHk.Text = preset.RightClickKey.ToDisplayString(false);
                    _txtRightClickHk.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 236, 245));
                }
            }
        }

        private TextBlock CreateTableHeaderLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(145, 155, 175)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private Button CreateSecondaryActionButton(string text, Action onClick)
        {
            SolidColorBrush normalBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 40, 54));
            SolidColorBrush normalBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90));
            SolidColorBrush normalFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 228, 240));

            SolidColorBrush hoverBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 58, 78));
            SolidColorBrush hoverBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(90, 110, 145));

            Button btn = new Button
            {
                Template = CreateButtonTemplate(5),
                Content = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = normalFg,
                Background = normalBg,
                BorderBrush = normalBorder,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(14, 5, 14, 5),
                Height = 32,
                Cursor = Cursors.Hand
            };
            btn.MouseEnter += delegate
            {
                btn.Background = hoverBg;
                btn.BorderBrush = hoverBorder;
            };
            btn.MouseLeave += delegate
            {
                btn.Background = normalBg;
                btn.BorderBrush = (_btnRepeatSave == btn && _isRepeatDirty)
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18))
                    : normalBorder;
            };
            btn.Click += delegate { onClick(); };
            return btn;
        }

        private Button CreateKeySlotButton(TextBlock displayBlock, string initialText, int recordIdx, bool allowMouseBind)
        {
            SolidColorBrush normalBg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 23, 31));
            SolidColorBrush normalBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90));
            SolidColorBrush normalFg = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 236, 245));

            SolidColorBrush hoverBorder = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 160, 235));

            Button btn = new Button
            {
                Template = CreateButtonTemplate(4),
                Background = normalBg,
                BorderBrush = normalBorder,
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            displayBlock.Text = initialText;
            displayBlock.FontSize = 12;
            displayBlock.FontWeight = FontWeights.SemiBold;
            displayBlock.Foreground = normalFg;
            displayBlock.HorizontalAlignment = HorizontalAlignment.Center;
            displayBlock.VerticalAlignment = VerticalAlignment.Center;
            btn.Content = displayBlock;

            btn.MouseEnter += delegate
            {
                if (_activeRecordingButton != btn)
                {
                    btn.BorderBrush = hoverBorder;
                }
            };
            btn.MouseLeave += delegate
            {
                if (_activeRecordingButton != btn)
                {
                    btn.BorderBrush = normalBorder;
                }
            };

            btn.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (_activeRecordingButton == btn && allowMouseBind)
                {
                    if (e.ChangedButton == MouseButton.Left)
                    {
                        e.Handled = true;
                        _hook.IsRecordingHotkey = false;
                        FinishRecordingHotkey(new HotkeyBinding(false, false, false, false, NativeMethods.VK_LBUTTON));
                        return;
                    }
                    else if (e.ChangedButton == MouseButton.Right)
                    {
                        e.Handled = true;
                        _hook.IsRecordingHotkey = false;
                        FinishRecordingHotkey(new HotkeyBinding(false, false, false, false, NativeMethods.VK_RBUTTON));
                        return;
                    }
                    else if (e.ChangedButton == MouseButton.Middle)
                    {
                        e.Handled = true;
                        _hook.IsRecordingHotkey = false;
                        FinishRecordingHotkey(new HotkeyBinding(false, false, false, false, NativeMethods.VK_MBUTTON));
                        return;
                    }
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    // Quick right-click to clear when not recording
                    e.Handled = true;
                    _recordingIndex = recordIdx;
                    _activeRecordingButton = btn;
                    FinishRecordingHotkey(new HotkeyBinding(false, false, false, false, 0));
                    return;
                }
            };

            btn.Click += delegate
            {
                StartRecordingHotkey(btn, recordIdx);
            };

            return btn;
        }

        private TextBox CreateIntervalTextBox(string initialVal, Action<int> onValueChanged)
        {
            TextBox tb = new TextBox
            {
                Text = initialVal,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 240, 248)),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 23, 31)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90)),
                BorderThickness = new Thickness(1),
                CaretBrush = System.Windows.Media.Brushes.White,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 2, 4, 2),
                MaxLength = 6
            };

            tb.PreviewTextInput += delegate(object sender, TextCompositionEventArgs e)
            {
                for (int i = 0; i < e.Text.Length; i++)
                {
                    if (!char.IsDigit(e.Text[i]))
                    {
                        e.Handled = true;
                        return;
                    }
                }
            };

            tb.TextChanged += delegate
            {
                int v;
                if (int.TryParse(tb.Text.Trim(), out v))
                {
                    onValueChanged(Math.Max(0, v));
                }
                else if (string.IsNullOrEmpty(tb.Text.Trim()))
                {
                    onValueChanged(0);
                }
            };

            tb.LostFocus += delegate
            {
                if (string.IsNullOrEmpty(tb.Text.Trim()))
                {
                    tb.Text = "0";
                }
            };

            return tb;
        }

        private void SwitchRepeatPreset(int presetIndex)
        {
            if (_activeRecordingButton != null)
            {
                CancelRecordingHotkey();
            }

            _config.ActiveRepeatPreset = presetIndex;
            LoadPresetToUi(presetIndex);
            _engine.RefreshRepeatStatusBadge();
        }

        private void MarkRepeatDirty(bool dirty)
        {
            _isRepeatDirty = dirty;
            if (_btnRepeatSave == null) return;

            if (dirty)
            {
                _btnRepeatSave.Content = I18n.RepeatBtnSaveDirty;
                _btnRepeatSave.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
                _btnRepeatSave.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 156, 18));
            }
            else
            {
                _btnRepeatSave.Content = I18n.RepeatBtnSave;
                _btnRepeatSave.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90));
                _btnRepeatSave.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 228, 240));
            }
        }

        private void LoadPresetToUi(int presetIndex)
        {
            _isUpdatingPresetUi = true;
            try
            {
                RepeatPreset preset = _config.RepeatPresets[presetIndex];

                if (_txtRepeatToggleHk != null)
                    _txtRepeatToggleHk.Text = _config.RepeatToggleKey.ToDisplayString(false);

                UpdateHoldKeyDisplay(101);
                if (_txtLeftClickMs != null)
                    _txtLeftClickMs.Text = preset.LeftClickIntervalMs.ToString();

                UpdateHoldKeyDisplay(102);
                if (_txtRightClickMs != null)
                    _txtRightClickMs.Text = preset.RightClickIntervalMs.ToString();

                for (int i = 0; i < 6; i++)
                {
                    SkillSlotConfig slot = preset.Skills[i];
                    if (_skillCheckBoxes[i] != null)
                        _skillCheckBoxes[i].IsChecked = slot.Enabled;
                    if (_skillKeyTexts[i] != null)
                        _skillKeyTexts[i].Text = slot.Key.ToDisplayString(true);
                    if (_skillIntervalBoxes[i] != null)
                        _skillIntervalBoxes[i].Text = slot.IntervalMs.ToString();
                }

                UpdatePresetTabStyles();
            }
            finally
            {
                _isUpdatingPresetUi = false;
            }
        }

        private void UpdatePresetTabStyles()
        {
            for (int p = 0; p < 4; p++)
            {
                if (_presetTabBtns[p] == null || _presetTabTexts[p] == null) continue;
                bool active = (p == _config.ActiveRepeatPreset);
                if (active)
                {
                    _presetTabBtns[p].Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 52, 152, 219));
                    _presetTabBtns[p].BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 152, 219));
                    _presetTabTexts[p].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(85, 185, 250));
                }
                else
                {
                    _presetTabBtns[p].Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 26, 35));
                    _presetTabBtns[p].BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(48, 56, 74));
                    _presetTabTexts[p].Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(155, 165, 185));
                }
            }
        }

        private void UpdateRepeatStateButton(bool running)
        {
            if (_btnRepeatState == null || _txtRepeatState == null) return;
            if (running)
            {
                _txtRepeatState.Text = "▶ RUNNING";
                _txtRepeatState.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                _btnRepeatState.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 46, 204, 113));
                _btnRepeatState.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
            }
            else
            {
                _txtRepeatState.Text = "■ STOP";
                _txtRepeatState.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(235, 87, 87));
                _btnRepeatState.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 231, 76, 60));
                _btnRepeatState.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            }
        }

        private UIElement CreateDashboard()
        {
            Grid grid = new Grid
            {
                Margin = new Thickness(20, 14, 20, 10)
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
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            cardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
                Text = "\u2022 " + subdesc,
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(115, 125, 145)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            subdescOut = subdescBlock;

            descStack.Children.Add(descBlock);
            descStack.Children.Add(subdescBlock);
            cardGrid.Children.Add(descStack);
            Grid.SetRow(descStack, 1);

            Grid footerGrid = new Grid();
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

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
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

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
                _config.SaveGeneralSettings();
                if (_sleepText != null)
                {
                    _sleepText.Text = val + " ms";
                }
            };
            sleepPanel.Children.Add(_sleepSlider);

            grid.Children.Add(sleepPanel);
            Grid.SetColumn(sleepPanel, 0);

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
                Text = "KanaiHelper v1.1 Ready.\r\n"
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
                case 1: return System.Windows.Media.Color.FromRgb(52, 152, 219);
                case 2: return System.Windows.Media.Color.FromRgb(243, 156, 18);
                case 3: return System.Windows.Media.Color.FromRgb(230, 126, 34);
                case 4: return System.Windows.Media.Color.FromRgb(155, 89, 182);
                default: return System.Windows.Media.Color.FromRgb(230, 236, 245);
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

            _engine.RepeatStatusChanged += delegate(bool repeatRunning)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    UpdateRepeatStateButton(repeatRunning);
                }));
            };

            _engine.HoldMouseStatusChanged += delegate(bool leftActive, bool rightActive)
            {
                Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (_leftHoldSubCard != null)
                    {
                        _leftHoldSubCard.BorderBrush = leftActive
                            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 64));
                    }
                    if (_rightHoldSubCard != null)
                    {
                        _rightHoldSubCard.BorderBrush = rightActive
                            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113))
                            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 46, 64));
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

            _hook.OnHotkeyCancelled = delegate
            {
                Dispatcher.Invoke(new Action(delegate
                {
                    CancelRecordingHotkey();
                }));
            };
        }

        private void ApplyLocalization()
        {
            if (_btnLang != null) _btnLang.Content = I18n.LangToggleBtn;
            if (_tabKanaiBtn != null) _tabKanaiBtn.Content = I18n.TabKanai;
            if (_tabRepeatBtn != null) _tabRepeatBtn.Content = I18n.TabRepeat;

            if (_card1Title != null) _card1Title.Text = I18n.Card1Title;
            if (_card1Desc != null) _card1Desc.Text = I18n.Card1Desc;
            if (_card1Sub != null) _card1Sub.Text = "\u2022 " + I18n.Card1Sub;
            if (_card1Run != null) _card1Run.Content = I18n.RunButton;

            if (_card2Title != null) _card2Title.Text = I18n.Card2Title;
            if (_card2Desc != null) _card2Desc.Text = I18n.Card2Desc;
            if (_card2Sub != null) _card2Sub.Text = "\u2022 " + I18n.Card2Sub;
            if (_card2Run != null) _card2Run.Content = I18n.RunButton;

            if (_card3Title != null) _card3Title.Text = I18n.Card3Title;
            if (_card3Desc != null) _card3Desc.Text = I18n.Card3Desc;
            if (_card3Sub != null) _card3Sub.Text = "\u2022 " + I18n.Card3Sub;
            if (_card3Run != null) _card3Run.Content = I18n.RunButton;

            if (_card4Title != null) _card4Title.Text = I18n.Card4Title;
            if (_card4Desc != null) _card4Desc.Text = I18n.Card4Desc;
            if (_card4Sub != null) _card4Sub.Text = "\u2022 " + I18n.Card4Sub;
            if (_card4Run != null) _card4Run.Content = I18n.RunButton;

            // Repeat Helper UI Localization
            if (_btnRepeatNew != null) _btnRepeatNew.Content = I18n.RepeatBtnNew;
            if (_btnRepeatSave != null) _btnRepeatSave.Content = _isRepeatDirty ? I18n.RepeatBtnSaveDirty : I18n.RepeatBtnSave;
            if (_btnRepeatLoad != null) _btnRepeatLoad.Content = I18n.RepeatBtnLoad;

            if (_lblRepeatSkillsSection != null) _lblRepeatSkillsSection.Text = I18n.RepeatSkillsSection;
            if (_lblRepeatStartStop != null) _lblRepeatStartStop.Text = I18n.RepeatStartStopLabel;

            if (_helpSkillsTitle != null) _helpSkillsTitle.Text = I18n.HelpSkillsTitle;
            if (_helpSkillsBody != null) _helpSkillsBody.Text = I18n.HelpSkillsBody;
            if (_helpMouseTitle != null) _helpMouseTitle.Text = I18n.HelpMouseTitle;
            if (_helpMouseBody != null) _helpMouseBody.Text = I18n.HelpMouseBody;

            if (_lblMouseHoldSection != null) _lblMouseHoldSection.Text = I18n.RepeatMouseHoldSection;
            if (_lblLeftClickTitle != null) _lblLeftClickTitle.Text = I18n.RepeatLeftClickTitle;
            if (_lblRightClickTitle != null) _lblRightClickTitle.Text = I18n.RepeatRightClickTitle;
            if (_lblLeftHoldKey != null) _lblLeftHoldKey.Text = I18n.RepeatHoldKeyLabel;
            if (_lblRightHoldKey != null) _lblRightHoldKey.Text = I18n.RepeatHoldKeyLabel;
            if (_lblLeftHoldInterval != null) _lblLeftHoldInterval.Text = I18n.RepeatHoldIntervalLabel;
            if (_lblRightHoldInterval != null) _lblRightHoldInterval.Text = I18n.RepeatHoldIntervalLabel;
            UpdateHoldKeyDisplay(101);
            UpdateHoldKeyDisplay(102);

            for (int p = 0; p < 4; p++)
            {
                if (_presetTabTexts[p] != null) _presetTabTexts[p].Text = I18n.RepeatPresetTab(p + 1);
            }
            for (int i = 0; i < 6; i++)
            {
                if (_skillRowLabels[i] != null) _skillRowLabels[i].Text = I18n.RepeatSkillRowLabel(i + 1);
            }

            if (_colHeaderEnable != null) _colHeaderEnable.Text = I18n.RepeatColEnable;
            if (_colHeaderSkill != null) _colHeaderSkill.Text = I18n.RepeatColSkill;
            if (_colHeaderKey != null) _colHeaderKey.Text = I18n.RepeatColKey;
            if (_colHeaderInterval != null) _colHeaderInterval.Text = I18n.RepeatColInterval;

            if (_sleepLabelText != null) _sleepLabelText.Text = I18n.SleepLabel;
            if (_escNoticePrefix != null) _escNoticePrefix.Text = I18n.EscNoticePrefix;
            if (_escNoticeSuffix != null) _escNoticeSuffix.Text = I18n.EscNoticeSuffix;
            if (_trayCheckBox != null) _trayCheckBox.Content = I18n.TrayOption;

            if (_trayOpenItem != null) _trayOpenItem.Text = I18n.TrayOpen;
            if (_trayExitItem != null) _trayExitItem.Text = I18n.TrayExit;

            if (!_engine.IsRunning && _statusBadge != null)
            {
                if (_engine.IsRepeatRunning)
                {
                    _statusBadge.Text = I18n.StatusRepeatRunning(_config.ActiveRepeatPreset + 1);
                }
                else
                {
                    _statusBadge.Text = I18n.StatusIdle;
                }
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
                tb.Text = (index >= 10 && index < 100) ? I18n.PressKeyOrMouse : I18n.PressKey;
                tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            }
        }

        private void RestoreButtonStyle(Button btn, TextBlock tb, int index)
        {
            if (index >= 1 && index <= 4)
            {
                System.Windows.Media.Color accent = GetAccentColorForIndex(index);
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, accent.R, accent.G, accent.B));
                btn.BorderBrush = new SolidColorBrush(accent);
                if (tb != null) tb.Foreground = new SolidColorBrush(accent);
            }
            else if (index == 101 || index == 102)
            {
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 23, 31));
                btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90));
                UpdateHoldKeyDisplay(index);
            }
            else
            {
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 23, 31));
                btn.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(58, 68, 90));
                if (tb != null) tb.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(230, 236, 245));
            }
        }

        private string GetBindingDisplayForIndex(int index)
        {
            RepeatPreset preset = _config.GetActivePreset();
            if (index == 1) return _config.HotkeySalvage.ToDisplayString();
            if (index == 2) return _config.Hotkey1Slot.ToDisplayString();
            if (index == 3) return _config.Hotkey2Slot.ToDisplayString();
            if (index == 4) return _config.HotkeyReforge.ToDisplayString();
            if (index == 100) return _config.RepeatToggleKey.ToDisplayString(false);
            if (index == 101) return preset.LeftClickKey.KeyCode == 0 ? I18n.HoldKeyPlaceholder : preset.LeftClickKey.ToDisplayString(false);
            if (index == 102) return preset.RightClickKey.KeyCode == 0 ? I18n.HoldKeyPlaceholder : preset.RightClickKey.ToDisplayString(false);
            if (index >= 10 && index < 16)
            {
                return preset.Skills[index - 10].Key.ToDisplayString(true);
            }
            return "";
        }

        private void FinishRecordingHotkey(HotkeyBinding binding)
        {
            if (_activeRecordingButton != null)
            {
                TextBlock tb = _activeRecordingButton.Content as TextBlock;
                RepeatPreset preset = _config.GetActivePreset();

                if (_recordingIndex == 1) _config.HotkeySalvage = binding;
                else if (_recordingIndex == 2) _config.Hotkey1Slot = binding;
                else if (_recordingIndex == 3) _config.Hotkey2Slot = binding;
                else if (_recordingIndex == 4) _config.HotkeyReforge = binding;
                else if (_recordingIndex == 100) _config.RepeatToggleKey = binding;
                else if (_recordingIndex == 101) preset.LeftClickKey = binding;
                else if (_recordingIndex == 102) preset.RightClickKey = binding;
                else if (_recordingIndex >= 10 && _recordingIndex < 16)
                {
                    preset.Skills[_recordingIndex - 10].Key = binding;
                }

                if (tb != null)
                {
                    tb.Text = GetBindingDisplayForIndex(_recordingIndex);
                }

                RestoreButtonStyle(_activeRecordingButton, tb, _recordingIndex);
                if (_recordingIndex >= 1 && _recordingIndex <= 4)
                {
                    _config.SaveGeneralSettings();
                }
                else
                {
                    MarkRepeatDirty(true);
                }
                _activeRecordingButton = null;
            }
        }

        private void CancelRecordingHotkey()
        {
            if (_activeRecordingButton != null)
            {
                _hook.IsRecordingHotkey = false;
                TextBlock tb = _activeRecordingButton.Content as TextBlock;
                if (tb != null)
                {
                    tb.Text = GetBindingDisplayForIndex(_recordingIndex);
                }
                RestoreButtonStyle(_activeRecordingButton, tb, _recordingIndex);
                _activeRecordingButton = null;
            }
        }

        private void SetupTrayIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon();
            _notifyIcon.Text = "KanaiHelper v1.1";
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
            _engine.Shutdown();
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
