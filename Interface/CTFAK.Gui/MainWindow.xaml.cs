using ColorPicker;
using ColorPicker.Models;
using CTFAK;
using CTFAK.CCN;
using CTFAK.CCN.Chunks.Banks;
using CTFAK.CCN.Chunks.Objects;
using CTFAK.Core.CCN.Chunks.Banks.ImageBank;
using CTFAK.Core.CCN.Chunks.Banks.SoundBank;
using CTFAK.EXE;
using CTFAK.FileReaders;
using CTFAK.MFA;
using CTFAK.Tools;
using CTFAK.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Action = System.Action;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using FontFamily = System.Windows.Media.FontFamily;

namespace Legacy_CTFAK_UI
{
    public partial class MainWindow : Window
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        public static IFileReader currentReader;
        public IFusionTool sortedImageDumper;
        public IFusionTool imageDumper;
        public IFusionTool soundDumper;
        public IFusionTool Decompiler;
        public IFusionTool Plugin;
        public int curAnimFrame;
        public SoundPlayer CurrentPlayingSound;
        public static NotifyableColor SetColor;
        public static SolidColorBrush SetColorBrush;
        public static NotifyableColor SetBGColor;
        public static SolidColorBrush SetBGColorBrush;
        public static string strLanguage = "Legacy_CTFAK_UI.Languages.English";
        public static string oldLanguage = "Legacy_CTFAK_UI.Languages.English";
        public static ResourceManager locRM = new ResourceManager(strLanguage, typeof(MainWindow).Assembly);
        public static ResourceManager oldRM = new ResourceManager(oldLanguage, typeof(MainWindow).Assembly);
        public System.Drawing.Bitmap bitmapToSave;
        public List<IFusionTool> availableTools = new List<IFusionTool>();
        public int animSpeed;
        public List<int> animFrames = new List<int>();
        public bool loopAnim;
        public bool playingAnim;

        // NOTE TO FUTURE SELF
        // THIS CODE IS FUCKING HORRIBLE
        // RECODE BEFORE PUSHING OR SO HELP ME

        public void UpdateProgress(double incrementBy, double maximum, string loadType)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                if (ProgressBar.Maximum != maximum)
                    ProgressBar.Value = 0;
                ProgressBar.Maximum = maximum;
                ProgressBar.Value += incrementBy;
                int percentage = (int)(ProgressBar.Value / ProgressBar.Maximum * 100);
                if (percentage < 0 || percentage > 100)
                    percentage = 100;
                ProgressLabel.Content = $"{locRM.GetString("Loading")} {loadType}. {percentage}%";
                if (loadType == locRM.GetString("Idle"))
                    ProgressLabel.Content = loadType;
            }));
        }

        public MainWindow()
        {
            InitializeComponent();
            CTFAKCore.Init();

            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
                ShowWindow(hWnd, 0);

            ImageBank.OnImageLoaded += (current, all) =>
            {
                UpdateProgress(1, all, locRM.GetString("LoadImages"));
            };
            GameData.OnChunkLoaded += (current, all) =>
            {
                UpdateProgress(1, all, locRM.GetString("LoadChunks"));
            };
            GameData.OnFrameLoaded += (current, all) =>
            {
                UpdateProgress(1, all, locRM.GetString("LoadFrames"));
            };
            SoundBank.OnSoundLoaded += (current, all) =>
            {
                UpdateProgress(1, all, locRM.GetString("LoadSounds"));
            };

            VersionLabel.Content = $"CTFAK 2.2 ({GameData.builddate})";

            var resourceDictionary = new ResourceDictionary();
            resourceDictionary.Source = new System.Uri(
                "pack://application:,,,/ColorPicker;component/Styles/DefaultColorPickerStyle.xaml",
                System.UriKind.RelativeOrAbsolute);


            ColorPicker.Color.RGB_R = 223;
            ColorPicker.Color.RGB_G = 114;
            ColorPicker.Color.RGB_B = 38;
            ColorPicker.SecondColor.RGB_R = 1;
            ColorPicker.SecondColor.RGB_G = 1;
            ColorPicker.SecondColor.RGB_B = 1;
            ColorPicker.Style = (Style)resourceDictionary["DefaultColorPickerStyle"];

            SetColor = ColorPicker.Color;
            SetBGColor = ColorPicker.SecondColor;
            SetColorBrush = new SolidColorBrush(Color.FromRgb((byte)((SetColor.RGB_R + 3) % 255), 0, 0));
            SetBGColorBrush = new SolidColorBrush(Color.FromRgb((byte)((SetBGColor.RGB_R + 3) % 255), 0, 0));
            UpdateSettings(null, null);

            int toolID = 0;
            foreach (var item in Directory.GetFiles("Plugins", "*.dll"))
            {
                var newAsm = Assembly.LoadFrom(Path.GetFullPath(item));
                foreach (var pluginType in newAsm.GetTypes())
                {
                    if (pluginType.GetInterface(typeof(IFusionTool).FullName) != null)
                    {
                        availableTools.Add((IFusionTool)Activator.CreateInstance(pluginType));
                        TreeViewItem TreeItem = new TreeViewItem();
                        TreeItem.Header = availableTools[toolID].Name;
                        TreeItem.Foreground = SetColorBrush;
                        TreeItem.FontFamily = new FontFamily("Courier New");
                        TreeItem.FontSize = 14;
                        TreeItem.Padding = new Thickness(1, 1, 0, 0);
                        TreeItem.Tag = toolID;
                        PluginsTreeView.Items.Add(TreeItem);
                        toolID++;
                    }
                }
            }

            //Sets the scaling properly so everything looks right.
            Width += 16;
            Height += 19;
        }

        //I've had issues with memory leaking with Anaconda's GUI so I had to add this.
        private void WindowClosing(object sender, System.EventArgs e)
        {
            Environment.Exit(0);
        }

        //All the tabs
        int tab = 0;

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            Button tabBtn = (Button)sender;
            if (tabBtn.Tag.ToString() != tab.ToString())
            {
                tab = int.Parse(tabBtn.Tag.ToString());
                MainGrid.Visibility = Visibility.Hidden;
                PluginsGrid.Visibility = Visibility.Hidden;
                PackDataGrid.Visibility = Visibility.Hidden;
                ObjectsGrid.Visibility = Visibility.Hidden;
                SoundsGrid.Visibility = Visibility.Hidden;
                SettingsGrid.Visibility = Visibility.Hidden;
                switch (tab)
                {
                    case 0:
                        MainGrid.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        PluginsGrid.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        PackDataGrid.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        ObjectsGrid.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        SoundsGrid.Visibility = Visibility.Visible;
                        break;
                    case 5:
                        SettingsGrid.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        void AfterGUILoaded()
        {
            PluginsTabButton.Visibility = Visibility.Visible;
            PackDataTabButton.Visibility = Visibility.Visible;
            ObjectsTabButton.Visibility = Visibility.Visible;
            SoundsTabButton.Visibility = Visibility.Visible;

            MFAInfoTextBlock.Visibility = Visibility.Visible;
            MFATreeView.Visibility = Visibility.Visible;
            var game = currentReader.getGameData();
            MFAInfoTextBlock.Content = $"{locRM.GetString("Title")}: {game.name}\n";
            if (!string.IsNullOrEmpty(game.author))
                MFAInfoTextBlock.Content += $"{locRM.GetString("Author")}: {game.author}\n";
            if (!string.IsNullOrEmpty(game.copyright))
                MFAInfoTextBlock.Content += $"{locRM.GetString("Copyright")}: {game.copyright}\n";
            if (!string.IsNullOrEmpty(game.aboutText))
                MFAInfoTextBlock.Content += $"{locRM.GetString("About")}: {game.aboutText}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("Build")}: {Settings.Build}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("BuildType")}: {Settings.GetGameTypeStr()}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("Exporter")}: {Settings.GetExporterStr()}\n";
            MFAInfoTextBlock.Content += "\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("NumFrames")}: {game.frames.Count}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("NumObjects")}: {game?.frameitems?.Count ?? 0}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("NumImages")}: {game?.Images?.Items.Count ?? 0}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("NumSounds")}: {game?.Sounds?.Items.Count ?? 0}\n";
            MFAInfoTextBlock.Content += $"{locRM.GetString("NumMusic")}: {game?.Music?.Items.Count ?? 0}\n";

            int frameCount = 0;
            int packCount = 0;
            int soundCount = 0;
            ObjectsTreeView.Items.Clear();
            MFATreeView.Items.Clear();
            SoundsTreeView.Items.Clear();
            PackedTreeView.Items.Clear();
            TreeViewItem FrameParent = new TreeViewItem();
            FrameParent.Header = locRM.GetString("Frames");
            FrameParent.Foreground = SetColorBrush;
            FrameParent.FontFamily = new FontFamily("Courier New");
            FrameParent.FontSize = 14;
            FrameParent.Padding = new Thickness(1, 1, 0, 0);
            MFATreeView.Items.Add(FrameParent);
            foreach (var frame in game.frames)
            {
                if (frame.name == null || frame.name.Length == 0) continue;
                TreeViewItem frameItem = new TreeViewItem();
                TreeViewItem frameItem2 = new TreeViewItem();
                frameItem.Header = frame.name;
                frameItem2.Header = frame.name;
                frameItem.Foreground = SetColorBrush;
                frameItem2.Foreground = SetColorBrush;
                frameItem.FontFamily = new FontFamily("Courier New");
                frameItem2.FontFamily = new FontFamily("Courier New");
                frameItem.FontSize = 14;
                frameItem2.FontSize = 14;
                frameItem.Padding = new Thickness(1, 1, 0, 0);
                frameItem2.Padding = new Thickness(1, 1, 0, 0);
                frameItem.Tag = $"{frameCount}Frame";
                frameItem2.Tag = $"{frameCount}Frame";
                FrameParent.Items.Add(frameItem);
                ObjectsTreeView.Items.Add(frameItem2);
                frameCount++;
                try
                {
                    foreach (var item in frame.objects)
                    {
                        TreeViewItem objectItem = new TreeViewItem();
                        TreeViewItem objectItem2 = new TreeViewItem();
                        objectItem.Header = game.frameitems[item.objectInfo].name;
                        objectItem2.Header = game.frameitems[item.objectInfo].name;
                        objectItem.Foreground = SetColorBrush;
                        objectItem2.Foreground = SetColorBrush;
                        objectItem.FontFamily = new FontFamily("Courier New");
                        objectItem2.FontFamily = new FontFamily("Courier New");
                        objectItem.FontSize = 14;
                        objectItem2.FontSize = 14;
                        objectItem.Padding = new Thickness(1, 1, 0, 0);
                        objectItem2.Padding = new Thickness(1, 1, 0, 0);
                        objectItem.Tag = $"{item.objectInfo}Object";
                        objectItem2.Tag = $"{item.objectInfo}Object";
                        frameItem.Items.Add(objectItem);
                        frameItem2.Items.Add(objectItem2);
                        if (game.frameitems[item.objectInfo].properties is ObjectCommon common)
                        {
                            if (common.Identifier != "SPRI" && common.Identifier != "SP") continue;
                            if (!Settings.TwoFivePlus && common.Parent.ObjectType != 2) continue;
                            int i = 0;
                            foreach (var anim in common.Animations.AnimationDict)
                            {
                                if (common.Animations.AnimationDict[i].DirectionDict == null)
                                {
                                    i++;
                                    continue;
                                }
                                TreeViewItem animItem = new TreeViewItem();
                                animItem.Header = $"{locRM.GetString("Animation")} {i}";
                                animItem.Foreground = SetColorBrush;
                                animItem.FontFamily = new FontFamily("Courier New");
                                animItem.FontSize = 14;
                                animItem.Padding = new Thickness(1, 1, 0, 0);
                                animItem.Tag = $"{anim.Key}Animation";
                                objectItem2.Items.Add(animItem);
                                i++;
                            }
                            if (objectItem2.Items.Count <= 1) objectItem2.Items.Clear();
                        }
                    }
                }
                catch
                {

                }
            }

            try
            {
                if (game.Sounds.Items != null)
                {
                    foreach (var sound in game.Sounds.Items)
                    {
                        TreeViewItem soundItem = new TreeViewItem();
                        soundItem.Header = sound.Name;
                        soundItem.Foreground = SetColorBrush;
                        soundItem.FontFamily = new FontFamily("Courier New");
                        soundItem.FontSize = 14;
                        soundItem.Padding = new Thickness(1, 1, 0, 0);
                        soundItem.Tag = soundCount;
                        SoundsTreeView.Items.Add(soundItem);
                        soundCount++;
                    }
                }
            }
            catch { }

            TreeViewItem extItemHeader = new TreeViewItem();
            extItemHeader.Header = locRM.GetString("Extensions");
            extItemHeader.Foreground = SetColorBrush;
            extItemHeader.FontFamily = new FontFamily("Courier New");
            extItemHeader.FontSize = 14;
            extItemHeader.Padding = new Thickness(1, 1, 0, 0);
            PackedTreeView.Items.Add(extItemHeader);

            TreeViewItem dllItemHeader = new TreeViewItem();
            dllItemHeader.Header = locRM.GetString("Libraries");
            dllItemHeader.Foreground = SetColorBrush;
            dllItemHeader.FontFamily = new FontFamily("Courier New");
            dllItemHeader.FontSize = 14;
            dllItemHeader.Padding = new Thickness(1, 1, 0, 0);
            PackedTreeView.Items.Add(dllItemHeader);

            TreeViewItem filterItemHeader = new TreeViewItem();
            filterItemHeader.Header = locRM.GetString("Filters");
            filterItemHeader.Foreground = SetColorBrush;
            filterItemHeader.FontFamily = new FontFamily("Courier New");
            filterItemHeader.FontSize = 14;
            filterItemHeader.Padding = new Thickness(1, 1, 0, 0);
            PackedTreeView.Items.Add(filterItemHeader);

            TreeViewItem movementItemHeader = new TreeViewItem();
            movementItemHeader.Header = locRM.GetString("Movements");
            movementItemHeader.Foreground = SetColorBrush;
            movementItemHeader.FontFamily = new FontFamily("Courier New");
            movementItemHeader.FontSize = 14;
            movementItemHeader.Padding = new Thickness(1, 1, 0, 0);
            PackedTreeView.Items.Add(movementItemHeader);
            try
            {
                if (game.packData.Items != null)
                {
                    foreach (var item in game.packData.Items)
                    {
                        if (item.PackFilename == null || item.PackFilename.Length == 0) continue;
                        TreeViewItem dataItem = new TreeViewItem();
                        dataItem.Header = item.PackFilename;
                        dataItem.Foreground = SetColorBrush;
                        dataItem.FontFamily = new FontFamily("Courier New");
                        dataItem.FontSize = 14;
                        dataItem.Padding = new Thickness(1, 1, 0, 0);
                        dataItem.Tag = $"{packCount}Packdata";
                        if (Path.GetExtension(item.PackFilename) == ".mfx")
                            extItemHeader.Items.Add(dataItem);
                        else if (Path.GetExtension(item.PackFilename) == ".dll")
                            dllItemHeader.Items.Add(dataItem);
                        else if (Path.GetExtension(item.PackFilename) == ".ift" || Path.GetExtension(item.PackFilename) == ".sft")
                            filterItemHeader.Items.Add(dataItem);
                        else if (Path.GetExtension(item.PackFilename) == ".mvx")
                            movementItemHeader.Items.Add(dataItem);
                        else
                            PackedTreeView.Items.Add(dataItem);
                        packCount++;
                    }
                }
            }
            catch
            {

            }

            UpdateProgress(0, 1, locRM.GetString("Idle"));
            Directory.CreateDirectory("Dumps");
        }

        private void SelectFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var fileSelector = new OpenFileDialog();
            fileSelector.Title = locRM.GetString("SelectGame");
            fileSelector.CheckFileExists = true;
            fileSelector.CheckPathExists = true;
            fileSelector.Filter = $"{locRM.GetString("FusionSelector")}|*.exe;*.ccn;*.apk;*.dat;*.fusion-xbox";
            if (fileSelector.ShowDialog().Value)
            {
                if (fileSelector.FileName.EndsWith(".exe"))
                    currentReader = new ExeFileReader();
                else
                    currentReader = new CCNFileReader();

                CTFAKCore.parameters = "";

                if (fileSelector.FileName.EndsWith(".apk"))
                    CTFAKCore.path = ApkFileReader.ExtractCCN(fileSelector.FileName);
                else
                    CTFAKCore.path = fileSelector.FileName;

                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (o,e) =>
                {
                    Console.WriteLine($"Reading game with \"{currentReader.Name}\"");
                    currentReader.PatchMethods();
                    currentReader.LoadGame(CTFAKCore.path);
                };
                backgroundWorker.RunWorkerCompleted += (o, e) =>
                {
                    AfterGUILoaded();
                };
                
                backgroundWorker.RunWorkerAsync();
            }
        }

        private void SoundTreeViewChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SoundsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)SoundsTreeView.SelectedItem;
            PlaySoundButton.Visibility = Visibility.Visible;
            PlaySoundButton.Content = locRM.GetString("PlaySound");
            if (CurrentPlayingSound != null)
                PlaySound(sender, e);
            SoundInfoText.Content = $"{locRM.GetString("Name")}: {currentReader.getGameData().Sounds.Items[int.Parse(SelectedItem.Tag.ToString())].Name}\n{locRM.GetString("Size")}: {currentReader.getGameData().Sounds.Items[int.Parse(SelectedItem.Tag.ToString())].Size/1000}kb";
        }

        private void PlaySound(object sender, RoutedEventArgs e)
        {
            if (CurrentPlayingSound == null)
            {
                PlaySoundButton.Content = locRM.GetString("StopSound");
                MemoryStream bytes = new MemoryStream(currentReader.getGameData().Sounds.Items[int.Parse(((TreeViewItem)SoundsTreeView.SelectedItem).Tag.ToString())].Data);
                CurrentPlayingSound = new SoundPlayer(bytes);
                CurrentPlayingSound.Play();
            }
            else
            {
                PlaySoundButton.Content = locRM.GetString("PlaySound");
                CurrentPlayingSound.Stop();
                CurrentPlayingSound = null;
            }
        }

        private void UpdateSettings(object sender, RoutedEventArgs e)
        {
            SolidColorBrush newColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)SetColor.A, (byte)SetColor.RGB_R, (byte)SetColor.RGB_G, (byte)SetColor.RGB_B));
            if (newColorBrush.Color != SetColorBrush.Color)
            {
                SetColorBrush = newColorBrush;

                // Bottom Bar
                VersionLabel.Foreground = SetColorBrush;
                ProgressLabel.Foreground = SetColorBrush;
                ProgressBar.Foreground = SetColorBrush;

                // Main
                SelectFileButton.Foreground = SetColorBrush;
                SelectFileButton.BorderBrush = SetColorBrush;
                MFAInfoTextBlock.Foreground = SetColorBrush;
                ItemInfoTextBlock.Foreground = SetColorBrush;
                foreach (TreeViewItem item in MFATreeView.Items)
                {
                    item.Foreground = SetColorBrush;
                    foreach (TreeViewItem subitem in item.Items)
                    {
                        subitem.Foreground = SetColorBrush;
                        foreach (TreeViewItem obj in subitem.Items)
                            obj.Foreground = SetColorBrush;
                    }
                }

                // Plugins
                ActivateButton.Foreground = SetColorBrush;
                ActivateButton.BorderBrush = SetColorBrush;
                PluginInfoText.Foreground = SetColorBrush;
                DumpWarningLabel.Foreground = SetColorBrush;
                OpenDumpFolderButton.Foreground = SetColorBrush;
                OpenDumpFolderButton.BorderBrush = SetColorBrush;
                foreach (TreeViewItem item in PluginsTreeView.Items)
                    item.Foreground = SetColorBrush;

                // Pack Dump
                DumpPackedButton.Foreground = SetColorBrush;
                DumpPackedButton.BorderBrush = SetColorBrush;
                PackedInfoText.Foreground = SetColorBrush;
                foreach (TreeViewItem item in PackedTreeView.Items)
                {
                    item.Foreground = SetColorBrush;
                    foreach (TreeViewItem subitem in item.Items)
                        subitem.Foreground = SetColorBrush;
                }

                // Objects
                DumpImageButton.Foreground = SetColorBrush;
                DumpImageButton.BorderBrush = SetColorBrush;
                PlayAnimationButton.Foreground = SetColorBrush;
                PlayAnimationButton.BorderBrush = SetColorBrush;
                AnimationLeft.Foreground = SetColorBrush;
                AnimationLeft.BorderBrush = SetColorBrush;
                AnimationRight.Foreground = SetColorBrush;
                AnimationRight.BorderBrush = SetColorBrush;
                AnimationCurrentFrame.Foreground = SetColorBrush;
                ObjectInfoText.Foreground = SetColorBrush;
                foreach (TreeViewItem item in ObjectsTreeView.Items)
                {
                    item.Foreground = SetColorBrush;
                    foreach (TreeViewItem obj in item.Items)
                    {
                        obj.Foreground = SetColorBrush;
                        foreach (TreeViewItem obj2 in obj.Items)
                            obj2.Foreground = SetColorBrush;
                    }
                }

                // Sounds
                PlaySoundButton.Foreground = SetColorBrush;
                PlaySoundButton.BorderBrush = SetColorBrush;
                SoundInfoText.Foreground = SetColorBrush;
                foreach (TreeViewItem item in SoundsTreeView.Items)
                    item.Foreground = SetColorBrush;

                // Settings
                UpdateButton.Foreground = SetColorBrush;
                UpdateButton.BorderBrush = SetColorBrush;
                SettingsInfoText.Foreground = SetColorBrush;
            }

            SolidColorBrush newBGColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)SetBGColor.A, (byte)SetBGColor.RGB_R, (byte)SetBGColor.RGB_G, (byte)SetBGColor.RGB_B));
            if (newBGColorBrush.Color != SetBGColorBrush.Color)
            {
                SetBGColorBrush = newBGColorBrush;
                AppGrid.Background = newBGColorBrush;

                SetBGColor.HSL_L += 10;
                SetBGColor.UpdateEverything(new ColorState());
                SolidColorBrush lighterBGColorBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb((byte)SetBGColor.A, (byte)SetBGColor.RGB_R, (byte)SetBGColor.RGB_G, (byte)SetBGColor.RGB_B));
                SetBGColor.RGB_R = SetBGColorBrush.Color.R;
                SetBGColor.RGB_G = SetBGColorBrush.Color.G;
                SetBGColor.RGB_B = SetBGColorBrush.Color.B;
                SetBGColor.UpdateEverything(new ColorState());

                // Tabs
                MainTabButton.Background = lighterBGColorBrush;
                PluginsTabButton.Background = lighterBGColorBrush;
                PackDataTabButton.Background = lighterBGColorBrush;
                ObjectsTabButton.Background = lighterBGColorBrush;
                SoundsTabButton.Background = lighterBGColorBrush;
                SettingsTabButton.Background = lighterBGColorBrush;
                TopBorder.Fill = lighterBGColorBrush;
                BottomBorder.Fill = lighterBGColorBrush;

                // Main
                SelectFileButton.Background = lighterBGColorBrush;
                MFATreeView.BorderBrush = lighterBGColorBrush;

                // Plugins
                ActivateButton.Background = lighterBGColorBrush;
                OpenDumpFolderButton.Background = lighterBGColorBrush;
                PluginsTreeView.BorderBrush = lighterBGColorBrush;

                // Pack Dump
                DumpPackedButton.Background = lighterBGColorBrush;
                PackedTreeView.BorderBrush = lighterBGColorBrush;

                // Objects
                DumpImageButton.Background = lighterBGColorBrush;
                PlayAnimationButton.Background = lighterBGColorBrush;
                AnimationLeft.Background = lighterBGColorBrush;
                AnimationRight.Background = lighterBGColorBrush;
                ObjectsGrid.Background = lighterBGColorBrush;
                ObjectsTreeView.Background = SetBGColorBrush;
                ObjectsTreeView.BorderBrush = lighterBGColorBrush;

                // Sounds
                PlaySoundButton.Background = lighterBGColorBrush;
                SoundsTreeView.BorderBrush = lighterBGColorBrush;

                // Settings
                UpdateButton.Background = lighterBGColorBrush;
            }

            // Language
            strLanguage = "Legacy_CTFAK_UI.Languages." + ((ComboBoxItem)LanguageCombo.SelectedItem).Content.ToString();
            locRM = new ResourceManager(strLanguage, typeof(MainWindow).Assembly);

            if (strLanguage == "Legacy_CTFAK_UI.Languages.English")
                VersionLabel.Content = $"CTFAK 2.2 ({GameData.builddate})";
            else
            {
                string[] date = GameData.builddate.Split('/');
                VersionLabel.Content = $"CTFAK 2.2 ({date[1]}/{date[0]}/{date[2]})";
            }

            // Tabs
            MainTabButton.Content = locRM.GetString("Main");
            PluginsTabButton.Content = locRM.GetString("Plugins");
            PackDataTabButton.Content = locRM.GetString("PackData");
            ObjectsTabButton.Content = locRM.GetString("Objects");
            SoundsTabButton.Content = locRM.GetString("Sounds");
            SettingsTabButton.Content = locRM.GetString("Settings");

            // Main Tab
            SelectFileButton.Content = locRM.GetString("SelectFile");
            MFAInfoTextBlock.Content = MFAInfoTextBlock.Content.ToString().
                Replace(oldRM.GetString("Title"), locRM.GetString("Title")).
                Replace(oldRM.GetString("Copyright"), locRM.GetString("Copyright")).
                Replace(oldRM.GetString("ProductVer"), locRM.GetString("ProductVer")).
                Replace(oldRM.GetString("Build"), locRM.GetString("Build")).
                Replace(oldRM.GetString("RuntimeVer"), locRM.GetString("RuntimeVer")).
                Replace(oldRM.GetString("NumFrames"), locRM.GetString("NumFrames")).
                Replace(oldRM.GetString("NumObjects"), locRM.GetString("NumObjects")).
                Replace(oldRM.GetString("NumImages"), locRM.GetString("NumImages")).
                Replace(oldRM.GetString("NumSounds"), locRM.GetString("NumSounds")).
                Replace(oldRM.GetString("NumMusic"), locRM.GetString("NumMusic"));
            foreach (TreeViewItem item in MFATreeView.Items)
                if (item.Header.ToString() == oldRM.GetString("Frames"))
                    item.Header = locRM.GetString("Frames");

            // Plugins
            ActivateButton.Content = locRM.GetString("OpenPlugin");
            OpenDumpFolderButton.Content = locRM.GetString("OpenDumpFolder");
            PluginInfoText.Content = PluginInfoText.Content.ToString().
                Replace(oldRM.GetString("Name"), locRM.GetString("Name"));
            DumpWarningLabel.Content = locRM.GetString("DumpWarning");

            // Pack Data
            DumpPackedButton.Content = locRM.GetString("Dump");
            PackedInfoText.Content = PackedInfoText.Content.ToString().
                Replace(oldRM.GetString("Name"), locRM.GetString("Name")).
                Replace(oldRM.GetString("Size"), locRM.GetString("Size"));
            foreach (TreeViewItem item in PackedTreeView.Items)
                if (item.Header.ToString() == oldRM.GetString("Extensions"))
                    item.Header = locRM.GetString("Extensions");
                else if (item.Header.ToString() == oldRM.GetString("Libraries"))
                    item.Header = locRM.GetString("Libraries");
                else if (item.Header.ToString() == oldRM.GetString("Filters"))
                    item.Header = locRM.GetString("Filters");

            //Objects
            DumpImageButton.Content = locRM.GetString("DumpSelected");
            PlayAnimationButton.Content = locRM.GetString("PlayAnimation");
            ObjectInfoText.Content = ObjectInfoText.Content.ToString().
                Replace(oldRM.GetString("Name"), locRM.GetString("Name")).
                Replace(oldRM.GetString("Type"), locRM.GetString("Type")).
                Replace(oldRM.GetString("Active"), locRM.GetString("Active")).
                Replace(oldRM.GetString("Size"), locRM.GetString("Size")).
                Replace(oldRM.GetString("Animations"), locRM.GetString("Animations")).
                Replace(oldRM.GetString("Frame"), locRM.GetString("Frame")).
                Replace(oldRM.GetString("Objects"), locRM.GetString("Objects")).
                Replace(oldRM.GetString("Backdrop"), locRM.GetString("Backdrop")).
                Replace(oldRM.GetString("QuickBackdrop"), locRM.GetString("QuickBackdrop")).
                Replace(oldRM.GetString("Counter"), locRM.GetString("Counter")).
                Replace(oldRM.GetString("String"), locRM.GetString("String")).
                Replace(oldRM.GetString("Identifier"), locRM.GetString("Identifier"));
            foreach (TreeViewItem item in ObjectsTreeView.Items)
                foreach (TreeViewItem obj in item.Items)
                    foreach (TreeViewItem obj2 in obj.Items)
                        obj2.Header = obj2.Header.ToString()
                            .Replace(oldRM.GetString("Animation"), locRM.GetString("Animation"));

            //Sounds
            PlaySoundButton.Content = PlaySoundButton.Content.ToString().
                Replace(oldRM.GetString("PlaySound"), locRM.GetString("StopSound"));
            SoundInfoText.Content = SoundInfoText.Content.ToString().
                Replace(oldRM.GetString("Name"), locRM.GetString("Name")).
                Replace(oldRM.GetString("Size"), locRM.GetString("Size"));

            //Settings
            UpdateButton.Content = locRM.GetString("Update");
            SettingsInfoText.Content = locRM.GetString("ColorLang");

            // Progress Bar
            ProgressLabel.Content = ProgressLabel.Content.ToString().Replace(oldRM.GetString("Idle"), locRM.GetString("Idle"));

            //Language
            oldLanguage = "Legacy_CTFAK_UI.Languages." + ((ComboBoxItem)LanguageCombo.SelectedItem).Content.ToString();
            oldRM = new ResourceManager(oldLanguage, typeof(MainWindow).Assembly);
        }

        private void ParameterToggle(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            if ((bool)cb.IsChecked)
                CTFAKCore.parameters = CTFAKCore.parameters + cb.Tag.ToString();
            else
                CTFAKCore.parameters = CTFAKCore.parameters.Replace(cb.Tag.ToString(), "");
        }

        private void SelectObject(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            curAnimFrame = 0;
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem.Tag.ToString().Contains("Object"))
            {
                AnimationLeft.Visibility = Visibility.Visible;
                AnimationRight.Visibility = Visibility.Visible;
                DumpImageButton.Visibility = Visibility.Hidden;
                PlayAnimationButton.Visibility = Visibility.Hidden;
            }
            else if (SelectedItem.Tag.ToString().Contains("Animation"))
            {
                AnimationLeft.Visibility = Visibility.Visible;
                AnimationRight.Visibility = Visibility.Visible;
                DumpImageButton.Visibility = Visibility.Visible;
                TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                if (animInfo.properties is ObjectCommon anim)
                {
                    var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        curAnimFrame = 0;
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        bitmapToSave = bmp;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{frm.Count}";
                        if (frm.Count > 1)
                            PlayAnimationButton.Visibility = Visibility.Visible;
                        else
                            PlayAnimationButton.Visibility = Visibility.Hidden;
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {ItemParent.Header}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Active")}\n" +
                            $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}\n" +
                            $"{locRM.GetString("Animations")}: {anim.Animations.AnimationDict.Count}\n" +
                            $"Speed: {anim.Animations.AnimationDict[0].DirectionDict[0].MaxSpeed}%";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                return;
            }
            else
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                DumpImageButton.Visibility = Visibility.Hidden;
                AnimationCurrentFrame.Content = "";
                ObjectPicture.Source = null;
                try
                {
                    ObjectInfoText.Content =
                        $"{locRM.GetString("Name")}: {SelectedItem.Header}\n" +
                        $"{locRM.GetString("Type")}: {locRM.GetString("Frame")}\n" +
                        $"{locRM.GetString("Objects")}: {SelectedItem.Items.Count}";
                }
                catch (Exception exc) { Logger.Log(exc); }
                return;
            }
            var objectInfo = currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))];

            if (objectInfo.properties is Backdrop bd)
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                DumpImageButton.Visibility = Visibility.Visible;
                if (bd.Image == null) return;
                System.Drawing.Bitmap bmp = null;
                try
                {
                    bmp = currentReader.getGameData().Images.Items[bd.Image].bitmap;
                    bitmapToSave = bmp;
                    var handle = bmp.GetHbitmap();
                    ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    UpdateImagePreview();
                    AnimationCurrentFrame.Content = "";
                }
                catch (Exception exc) { Logger.Log(exc); }
                try
                {
                    ObjectInfoText.Content =
                        $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                        $"{locRM.GetString("Type")}: {locRM.GetString("Backdrop")}\n" +
                        $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}";
                }
                catch (Exception exc) { Logger.Log(exc); }
            }

            if (objectInfo.properties is Quickbackdrop qbd)
            {
                AnimationLeft.Visibility = Visibility.Hidden;
                AnimationRight.Visibility = Visibility.Hidden;
                DumpImageButton.Visibility = Visibility.Visible;
                if (qbd.Image == null) return;
                System.Drawing.Bitmap bmp = null;
                try
                {
                    bmp = currentReader.getGameData().Images.Items[qbd.Image].bitmap;
                    bitmapToSave = bmp;
                    var handle = bmp.GetHbitmap();
                    ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    UpdateImagePreview();
                    AnimationCurrentFrame.Content = "";
                }
                catch (Exception exc) { Logger.Log(exc); }
                try
                {
                    ObjectInfoText.Content =
                        $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                        $"{locRM.GetString("Type")}: {locRM.GetString("QuickBackdrop")}\n" +
                        $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}";
                }
                catch (Exception exc) { Logger.Log(exc); }
            }

            if (objectInfo.properties is ObjectCommon common)
            {
                if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.TwoFivePlus && common.Parent.ObjectType == 2)
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    DumpImageButton.Visibility = Visibility.Visible;
                    if (common.Animations.AnimationDict == null) return;
                    if (common.Animations.AnimationDict[0].DirectionDict == null) return;
                    if (common.Animations.AnimationDict[0].DirectionDict[0].Frames == null) return;
                    var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        bitmapToSave = bmp;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{frm.Count}";

                        if (frm.Count > 1)
                            PlayAnimationButton.Visibility = Visibility.Visible;
                        else
                            PlayAnimationButton.Visibility = Visibility.Hidden;
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Active")}\n" +
                            $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}\n" +
                            $"{locRM.GetString("Animations")}: {common.Animations.AnimationDict.Count}\n" +
                            $"Speed: {common.Animations.AnimationDict[0].DirectionDict[0].MaxSpeed}%";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.TwoFivePlus && common.Parent.ObjectType == 7)
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    DumpImageButton.Visibility = Visibility.Visible;
                    var counter = common.Counters;
                    if (counter == null)
                    {
                        AnimationLeft.Visibility = Visibility.Hidden;
                        AnimationRight.Visibility = Visibility.Hidden;
                        ObjectPicture.Source = null;
                        AnimationCurrentFrame.Content = "";
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Counter")}\n";
                        return;
                    }
                    if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                    if (counter.Frames == null) return;
                    var frm = counter.Frames;
                    System.Drawing.Bitmap bmp = null;
                    try
                    {
                        bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                        bitmapToSave = bmp;
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Counter")}\n" +
                            $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else if (common.Identifier == "TEXT" || common.Identifier == "TE" || !Settings.TwoFivePlus && common.Parent.ObjectType == 3)
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    DumpImageButton.Visibility = Visibility.Hidden;
                    ObjectPicture.Source = null;
                    AnimationCurrentFrame.Content = "";
                    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)ObjectInfoText.ActualWidth, (int)ObjectInfoText.ActualHeight);
                    try
                    {
                        System.Drawing.RectangleF rectf = new System.Drawing.RectangleF(0, 0, bmp.Width, bmp.Height);
                        System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        System.Drawing.StringFormat format = new System.Drawing.StringFormat()
                        {
                            Alignment = System.Drawing.StringAlignment.Center,
                            LineAlignment = System.Drawing.StringAlignment.Center
                        };

                        System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((byte)SetColor.A, (byte)SetColor.RGB_R, (byte)SetColor.RGB_G, (byte)SetColor.RGB_B));

                        if (common.Text != null)
                            g.DrawString(common.Text.Items[curAnimFrame].Value, new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                        else
                            g.DrawString("Invalid String", new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                        g.Flush();
                        var handle = bmp.GetHbitmap();
                        ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        UpdateImagePreview();
                        if (common.Text != null)
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Text.Items.Count}";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                    try
                    {
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("String")}\n"/* +
                            $"{locRM.GetString("Paragraphs")}: {common.Text.Items.Count}"*/;
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
                else
                {
                    AnimationLeft.Visibility = Visibility.Hidden;
                    AnimationRight.Visibility = Visibility.Hidden;
                    DumpImageButton.Visibility = Visibility.Hidden;
                    ObjectPicture.Source = null;
                    AnimationCurrentFrame.Content = "";
                    try
                    {
                        ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {objectInfo.name}\n" +
                            $"{locRM.GetString("Identifier")}: {common.Identifier}\n";
                    }
                    catch (Exception exc) { Logger.Log(exc); }
                }
            }
            //MemoryStream bytes = new MemoryStream(currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString())]));
            
        }

        private void AnimationLeft_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem != null)
            {
                if (SelectedItem.Tag.ToString().Contains("Animation"))
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                    var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                    if (animInfo.properties is ObjectCommon anim)
                    {
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count - 1;
                        var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                        System.Drawing.Bitmap bmp = null;
                        try
                        {
                            bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            bitmapToSave = bmp;
                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                        try
                        {
                            ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {ItemParent.Header}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Active")}\n" +
                            $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}\n" +
                            $"{locRM.GetString("Animations")}: {anim.Animations.AnimationDict.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    return;
                }
                if (currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))].properties is ObjectCommon common)
                {
                    if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.TwoFivePlus && common.Parent.ObjectType == 2)
                    {
                        if (common.Animations?.AnimationDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0].Frames[0] == null) return;
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count - 1;
                        var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames[curAnimFrame];
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm].bitmap.GetHbitmap();
                            bitmapToSave = currentReader.getGameData().Images.Items[frm].bitmap;
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.TwoFivePlus && common.Parent.ObjectType == 7)
                    {
                        var counter = common.Counters;
                        if (counter == null) return;
                        if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                        if (counter.Frames == null) return;
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = counter.Frames.Count - 1;
                        var frm = counter.Frames;
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap.GetHbitmap();
                            bitmapToSave = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "TEXT" || common.Identifier == "TE" || !Settings.TwoFivePlus && common.Parent.ObjectType == 3)
                    {
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)ObjectInfoText.Width, (int)ObjectInfoText.Height);
                        curAnimFrame--;
                        if (curAnimFrame < 0) curAnimFrame = common.Text.Items.Count - 1;
                        try
                        {
                            System.Drawing.RectangleF rectf = new System.Drawing.RectangleF(0, 0, bmp.Width, bmp.Height);
                            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                            System.Drawing.StringFormat format = new System.Drawing.StringFormat()
                            {
                                Alignment = System.Drawing.StringAlignment.Center,
                                LineAlignment = System.Drawing.StringAlignment.Center
                            };

                            System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((byte)SetColor.A, (byte)SetColor.RGB_R, (byte)SetColor.RGB_G, (byte)SetColor.RGB_B)); if (common.Text != null)
                                g.DrawString(common.Text.Items[curAnimFrame].Value, new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                            else
                                g.DrawString("Invalid String", new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                            g.Flush();

                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            if (common.Text != null)
                                AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Text.Items.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                }
            }
        }

        private void AnimationRight_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectsTreeView.Items.Count == 0) return;
            TreeViewItem SelectedItem = (TreeViewItem)ObjectsTreeView.SelectedItem;
            if (SelectedItem != null)
            {
                if (SelectedItem.Tag.ToString().Contains("Animation"))
                {
                    AnimationLeft.Visibility = Visibility.Visible;
                    AnimationRight.Visibility = Visibility.Visible;
                    TreeViewItem ItemParent = (TreeViewItem)SelectedItem.Parent;
                    var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                    if (animInfo.properties is ObjectCommon anim)
                    {
                        curAnimFrame++;
                        if (curAnimFrame > anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count - 1) curAnimFrame = 0;
                        var frm = anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames;
                        System.Drawing.Bitmap bmp = null;
                        try
                        {
                            bmp = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            bitmapToSave = bmp;
                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{anim.Animations.AnimationDict[int.Parse(SelectedItem.Tag.ToString().Replace("Animation", ""))].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                        try
                        {
                            ObjectInfoText.Content =
                            $"{locRM.GetString("Name")}: {ItemParent.Header}\n" +
                            $"{locRM.GetString("Type")}: {locRM.GetString("Active")}\n" +
                            $"{locRM.GetString("Size")}: {bmp.Width}x{bmp.Height}\n" +
                            $"{locRM.GetString("Animations")}: {anim.Animations.AnimationDict.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    return;
                }
                if (currentReader.getGameData().frameitems[int.Parse(SelectedItem.Tag.ToString().Replace("Object", ""))].properties is ObjectCommon common)
                {
                    if (common.Identifier == "SPRI" || common.Identifier == "SP" || !Settings.TwoFivePlus && common.Parent.ObjectType == 2)
                    {
                        if (common.Animations?.AnimationDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0] == null) return;
                        if (common.Animations?.AnimationDict[0].DirectionDict[0].Frames[0] == null) return;
                        curAnimFrame++;
                        if (curAnimFrame > common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count - 1) curAnimFrame = 0;
                        var frm = common.Animations.AnimationDict[0].DirectionDict[0].Frames[curAnimFrame];
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm].bitmap.GetHbitmap();
                            bitmapToSave = currentReader.getGameData().Images.Items[frm].bitmap;
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Animations.AnimationDict[0].DirectionDict[0].Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "CNTR" || common.Identifier == "CN" || !Settings.TwoFivePlus && common.Parent.ObjectType == 7)
                    {
                        var counter = common.Counters;
                        if (counter == null) return;
                        if (!(counter.DisplayType == 1 || counter.DisplayType == 4 || counter.DisplayType == 50)) return;
                        if (counter.Frames == null) return;
                        curAnimFrame++;
                        if (curAnimFrame > counter.Frames.Count - 1) curAnimFrame = 0;
                        var frm = counter.Frames;
                        try
                        {
                            var handle = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap.GetHbitmap();
                            bitmapToSave = currentReader.getGameData().Images.Items[frm[curAnimFrame]].bitmap;
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{counter.Frames.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                    else if (common.Identifier == "TEXT" || common.Identifier == "TE" || !Settings.TwoFivePlus && common.Parent.ObjectType == 3)
                    {
                        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)ObjectInfoText.Width, (int)ObjectInfoText.Height);
                        curAnimFrame++;
                        if (curAnimFrame > common.Text.Items.Count - 1) curAnimFrame = 0;
                        try
                        {
                            System.Drawing.RectangleF rectf = new System.Drawing.RectangleF(0, 0, bmp.Width, bmp.Height);
                            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp);
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                            System.Drawing.StringFormat format = new System.Drawing.StringFormat()
                            {
                                Alignment = System.Drawing.StringAlignment.Center,
                                LineAlignment = System.Drawing.StringAlignment.Center
                            };

                            System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb((byte)SetColor.A, (byte)SetColor.RGB_R, (byte)SetColor.RGB_G, (byte)SetColor.RGB_B)); if (common.Text != null)
                                g.DrawString(common.Text.Items[curAnimFrame].Value, new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                            else
                                g.DrawString("Invalid String", new System.Drawing.Font("Courier New", (int)ObjectPicture.Width / 25, System.Drawing.FontStyle.Bold), brush, rectf, format);
                            g.Flush();

                            var handle = bmp.GetHbitmap();
                            ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                            UpdateImagePreview();
                            if (common.Text != null)
                                AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{common.Text.Items.Count}";
                        }
                        catch (Exception exc) { Logger.Log(exc); }
                    }
                }
            }
        }

        private void OpenDumpFolder(object sender, RoutedEventArgs e)
        {
            var outPath = currentReader.getGameData().name ?? "Unknown Game";
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            outPath = rgx.Replace(outPath, "").Trim(' ');
            Directory.CreateDirectory("Dumps\\" + outPath);
            Process.Start("explorer.exe", Directory.GetCurrentDirectory() + "\\Dumps\\" + outPath + "\\");
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)PluginsTreeView.SelectedItem;
            if (item == null) return;
            Plugin = availableTools[int.Parse(item.Tag.ToString())];
            ActivateButton.IsEnabled = false;
            Thread pluginsThread = new Thread(PluginThread);
            pluginsThread.Name = "Plugin";
            pluginsThread.Start();
        }

        void PluginThread()
        {
            Plugin.Execute(currentReader);
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                ActivateButton.IsEnabled = true;
            }));
        }

        private void UpdateImagePreview()
        {
            RenderOptions.SetBitmapScalingMode(ObjectPicture, BitmapScalingMode.NearestNeighbor);
            if (ObjectPicture.Source.Width > ObjectPicture.Width || ObjectPicture.Source.Height > ObjectPicture.Height)
                ObjectPicture.Stretch = Stretch.Uniform;
            else
                ObjectPicture.Stretch = Stretch.None;
        }

        private void DumpPackedItem(object sender, RoutedEventArgs e)
        {
            if (PackedTreeView.SelectedItem == null) return;

            var outPath = currentReader.getGameData().name ?? "Unknown Game";
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            outPath = rgx.Replace(outPath, "").Trim(' ');
            string dir = $"Dumps\\{outPath}\\Pack Data\\";
            var packItem = currentReader.getGameData().packData.Items[int.Parse((PackedTreeView.SelectedItem as TreeViewItem).Tag.ToString().Replace("Packdata", ""))];

            if (Path.GetExtension(packItem.PackFilename) == ".mfx")
                dir += "Extensions\\";
            else if (Path.GetExtension(packItem.PackFilename) == ".dll")
                dir += "Libraries\\";
            else if (Path.GetExtension(packItem.PackFilename) == ".ift" || Path.GetExtension(packItem.PackFilename) == ".sft")
                dir += "Filters\\";
            else if (Path.GetExtension(packItem.PackFilename) == ".mvx")
                dir += "Movements\\";

            Directory.CreateDirectory(dir);
            File.WriteAllBytes(dir + packItem.PackFilename, packItem.Data);
        }

        private void DumpSelectedImage(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fdlg = new SaveFileDialog();
            fdlg.Title = "Save Image";
            fdlg.InitialDirectory = Directory.GetCurrentDirectory();
            fdlg.FileName = ((TreeViewItem)ObjectsTreeView.SelectedItem).Header.ToString();
            fdlg.DefaultExt = ".png";
            fdlg.Filter = "Image File (*.png)|*.png";
            fdlg.FilterIndex = 2;
            fdlg.RestoreDirectory = true;
            if (fdlg.ShowDialog() == true)
                bitmapToSave.Save(fdlg.FileName);
        }

        private void PlayAnimation(object sender, RoutedEventArgs e)
        {
            if (playingAnim == true)
                playingAnim = false;
            else
            {
                TreeViewItem item = (TreeViewItem)ObjectsTreeView.SelectedItem;
                if (item == null) return;
                if (item.Tag.ToString().Contains("Object"))
                {
                    var objectInfo = currentReader.getGameData().frameitems[int.Parse(item.Tag.ToString().Replace("Object", ""))];
                    if (objectInfo.properties is ObjectCommon common)
                    {
                        animFrames = common.Animations.AnimationDict[0].DirectionDict[0].Frames;
                        animSpeed = common.Animations.AnimationDict[0].DirectionDict[0].MinSpeed;
                        if (common.Animations.AnimationDict[0].DirectionDict[0].Repeat > 0)
                            loopAnim = false;
                        else
                            loopAnim = true;
                    }
                }
                else if (item.Tag.ToString().Contains("Animation"))
                {
                    TreeViewItem ItemParent = (TreeViewItem)item.Parent;
                    int animNum = int.Parse(item.Tag.ToString().Replace("Animation", ""));
                    var animInfo = currentReader.getGameData().frameitems[int.Parse(ItemParent.Tag.ToString().Replace("Object", ""))];
                    if (animInfo.properties is ObjectCommon anim)
                    {
                        animFrames = anim.Animations.AnimationDict[animNum].DirectionDict[0].Frames;
                        animSpeed = anim.Animations.AnimationDict[animNum].DirectionDict[0].MinSpeed;
                        if (anim.Animations.AnimationDict[animNum].DirectionDict[0].Repeat > 0)
                            loopAnim = false;
                        else
                            loopAnim = true;
                    }
                }
                PlayAnimationButton.Content = "Stop Animation";
                Thread animationThread = new Thread(AnimationThread);
                animationThread.Name = "Animation";
                animationThread.Start();
            }
        }

        private void AnimationThread()
        {
            playingAnim = true;
            while (true)
            {
                if (animSpeed == 0)
                    break;

                curAnimFrame++;
                if (curAnimFrame >= animFrames.Count && !loopAnim)
                    break;
                else if (curAnimFrame >= animFrames.Count)
                    curAnimFrame = 0;

                System.Drawing.Bitmap bmp = currentReader.getGameData().Images.Items[animFrames[curAnimFrame]].bitmap;
                bitmapToSave = bmp;
                var handle = bmp.GetHbitmap();

                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                {
                    ObjectPicture.Source = Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    UpdateImagePreview();
                    AnimationCurrentFrame.Content = $"{curAnimFrame + 1}/{animFrames.Count}";
                }));

                Thread.Sleep((int)Math.Round(1 / (60 * ((float)animSpeed / 100)) * 1000));

                if (playingAnim == false)
                    break;
            }
            playingAnim = false;
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
            {
                PlayAnimationButton.Content = "Play Animation";
            }));
        }

        private void OnResize(object sender, SizeChangedEventArgs e)
        {
            // Tabs
            double newTabSize = Math.Min(990 / 8, e.NewSize.Width / 8);
            Thickness newTabMargin = new Thickness(5, 1, 0, 0);
            MainTabButton.Width = newTabSize;
            MainTabButton.Margin = newTabMargin;
            newTabMargin.Left += newTabSize + 5;
            PluginsTabButton.Width = newTabSize;
            PluginsTabButton.Margin = newTabMargin;
            newTabMargin.Left += newTabSize + 5;
            PackDataTabButton.Width = newTabSize;
            PackDataTabButton.Margin = newTabMargin;
            newTabMargin.Left += newTabSize + 5;
            ObjectsTabButton.Width = newTabSize;
            ObjectsTabButton.Margin = newTabMargin;
            newTabMargin.Left += newTabSize + 5;
            SoundsTabButton.Width = newTabSize;
            SoundsTabButton.Margin = newTabMargin;
            SettingsTabButton.Width = newTabSize;

            // Objects
            ObjectPicture.Width = ObjectInfoText.ActualWidth;
            ObjectPicture.Height = ObjectInfoText.ActualHeight - (29 * 3);
            ObjectPicture.Margin = new Thickness(0, 0, 0, 29 * 3);
        }
    }
}
