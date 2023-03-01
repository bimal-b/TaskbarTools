using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using static TaskbarTool.Constants;
using static TaskbarTool.Globals;
using TaskbarTool.Enums;
using TaskbarTool.Structs;
using Microsoft.Win32;
using System.Reflection;
using System.Text;

namespace TaskbarTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		#region Declarations
		// Main window initialization
		bool _windowInitialized;
		readonly string _applicationPath = Assembly.GetExecutingAssembly().Location;
		// Flag to close the application
		bool doExit = false;
		// Task bars
		static Task _applyTask;
		static bool _runApplyTask;
		public static bool FindTaskbarHandles = true;

		static System.Windows.Forms.NotifyIcon _sysTrayIcon;
		readonly ContextMenu _sysTrayContextMenu;

		private static bool _alphaDragStarted;

		// Explorer restarts and Windows Accent Color changes
		private static readonly uint TaskbarCreated = Externals.RegisterWindowMessage("TaskbarCreated");

		// Window state hook
		private static readonly Externals.WinEventDelegate ProcDelegate = WinEventProc;
		private static IntPtr _windowStateHook;
		private static IntPtr _lastClosedWindow;
		private static DateTime _lastClosedWindowTime;

		// Start with Windows registry key
		readonly RegistryKey _appRegistryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

		private readonly StringBuilder _windowClass = new StringBuilder(32);
		#endregion Declarations

		#region Initializations
		public MainWindow()
		{
			InitializeComponent();

			_sysTrayContextMenu = FindResource("TrayContextMenu") as ContextMenu;

			_sysTrayIcon = new System.Windows.Forms.NotifyIcon();
			var iconStream = Application.GetResourceStream(new Uri("Resources/tt-logo.ico", UriKind.Relative)).Stream;
			_sysTrayIcon.Icon = new System.Drawing.Icon(iconStream);
			_sysTrayIcon.Visible = true;
			_sysTrayIcon.MouseClick += SysTrayIcon_MouseClick;
			_sysTrayIcon.DoubleClick +=
				delegate
				{
					Show();
					WindowState = WindowState.Normal;
				};
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (!doExit)
			{
				e.Cancel = true;
				Hide();
			}
		}

		protected override void OnStateChanged(EventArgs e)
		{
			if (WindowState == WindowState.Minimized)
			{
				Hide();
			}

			base.OnStateChanged(e);
		}

		private void PopulateComboBoxes()
		{
			AccentStateComboBox.ItemsSource = Enum.GetValues(typeof(AccentState)).Cast<AccentState>();
			AccentStateComboBox.SelectedIndex = 0;
		}

		private void LoadSettings()
		{
			TT.InitializeOptions();

			SwitchTaskbarBeingEdited("Main");

			UseMaximizedSettingsCheckBox.IsChecked = TT.Options.Settings.UseDifferentSettingsWhenMaximized;
			StartMinimizedCheckBox.IsChecked = TT.Options.Settings.StartMinimized;
			StartWhenLaunchedCheckBox.IsChecked = TT.Options.Settings.StartWhenLaunched;

			if (TT.Options.Settings.StartMinimized)
			{
				Hide();
			}
		}

		private void SaveSettings()
		{
			TT.Options.Settings.MainTaskbarStyle.AccentState = (byte)(int)AccentStateComboBox.SelectedItem;
			TT.Options.Settings.MainTaskbarStyle.GradientColor = GradientColorPicker.SelectedColor.ToString();
			TT.Options.Settings.MainTaskbarStyle.WindowsAccentAlpha = (byte)WindowsAccentAlphaSlider.Value;
			TT.Options.Settings.MainTaskbarStyle.Colorize = ColorizeBlurCheckBox.IsChecked ?? false;
			TT.Options.Settings.MainTaskbarStyle.UseWindowsAccentColor = WindowsAccentColorCheckBox.IsChecked ?? false;

			TT.Options.Settings.UseDifferentSettingsWhenMaximized = UseMaximizedSettingsCheckBox.IsChecked ?? false;
			TT.Options.Settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
			TT.Options.Settings.StartWhenLaunched = StartWhenLaunchedCheckBox.IsChecked ?? false;

			TT.SaveOptions();
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			PopulateComboBoxes();
			LoadSettings();
			_windowInitialized = true;

			if (TT.Options.Settings.StartMinimized) { WindowState = WindowState.Minimized; }
			if (TT.Options.Settings.StartWhenLaunched) { StartStopButton_Click(null, null); }

			// Listen for name change changes across all processes/threads on current desktop
			_windowStateHook = Externals.SetWinEventHook(EVENT_MIN, EVENT_MAX, IntPtr.Zero, ProcDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
		}
		#endregion Initializations

		#region Destructor
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			_sysTrayIcon.Dispose();
			SaveSettings();
			_runApplyTask = false;
			Externals.UnhookWinEvent(_windowStateHook);
		}

		private void CloseMainWindow(object sender, RoutedEventArgs e)
		{
			SaveSettings();
			doExit = true;
			Close();
		}
		#endregion Destructor

		#region Functions
		private void ApplyToAllTaskbars()
		{
			Taskbars.Bars = new List<Taskbar>();

			while (_runApplyTask)
			{
				if (FindTaskbarHandles)
				{
					Taskbars.Bars.Add(new Taskbar(Externals.FindWindow("Shell_TrayWnd", null)));
					Taskbars.Bars.Add(new Taskbar(Externals.FindWindow("NotifyIconOverflowWindow", null)));
					Taskbars.Bars.Add(new Taskbar(Externals.FindWindow(NativeMethods.StartMenuClass, "Action Center")));
					//Taskbars.Bars.Add(new Taskbar(Externals.FindWindow("DV2ControlHost", null)));
					//Taskbars.Bars.Add(new Taskbar(Externals.FindWindow("DV2ControlHost", "Start menu")));
					Taskbars.Bars.Add(new Taskbar(Externals.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "", "Start")));

					var foregroundWindowHandle = NativeMethods.GetForegroundWindow();
					NativeMethods.GetClassName(foregroundWindowHandle, _windowClass, _windowClass.Capacity);
					// foreground window class equal start menu window class?                  
					if (_windowClass.ToString().Equals(NativeMethods.StartMenuClass))
					{
						Taskbars.Bars.Add(new Taskbar(foregroundWindowHandle));
					}

					//TaskbarList.Add(FindWindow(null, "Pins view"));
					var otherBars = IntPtr.Zero;

					//IntPtr cortana = FindWindowEx(hWndList[0], IntPtr.Zero, "TrayDummySearchControl", null);
					//hWndList.Add(cortana);

					while (true)
					{
						otherBars = Externals.FindWindowEx(IntPtr.Zero, otherBars, "Shell_SecondaryTrayWnd", "");
						if (otherBars == IntPtr.Zero) { break; }
						else { Taskbars.Bars.Add(new Taskbar(otherBars)); }
					}

					FindTaskbarHandles = false;

					Application.Current.Dispatcher.Invoke(UpdateAllSettings);
				}

				if (Taskbars.MaximizedStateChanged)
				{
					Taskbars.UpdateMaximizedState();
					Taskbars.UpdateAllSettings();
				}

				foreach (var taskbar in Taskbars.Bars)
				{
					Taskbars.ApplyStyles(taskbar);
				}

				Thread.Sleep(10);
			}
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var mainWindowPtr = new WindowInteropHelper(this).Handle;
			var mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
			mainWindowSrc.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == TaskbarCreated)
			{
				FindTaskbarHandles = true;
				handled = true;
			}
			else if (msg == WM_DWMCOLORIZATIONCOLORCHANGED)
			{
				Globals.WindowsAccentColor = WindowsAccentColor.GetColorAsInt(); // TODO: use colour from wParam
				handled = true;
			}

			return IntPtr.Zero;
		}

		static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			//if ((idObject != 0 || idChild != 0) { return; }

			var placement = new WindowPlacement();
			placement.length = Marshal.SizeOf(placement);
			Externals.GetWindowPlacement(hwnd, ref placement);

			// Window is closing
			if (idObject == -2 && idChild == 5)
			{
				if (MaximizedWindows.Contains(hwnd))
				{
					_lastClosedWindow = hwnd;
					_lastClosedWindowTime = DateTime.Now;
					MaximizedWindows.Remove(hwnd);
					Taskbars.MaximizedStateChanged = true;
				}
			}
			else if (placement.showCmd == WindowPlacementCommands.SW_MAXIMIZE || placement.showCmd == WindowPlacementCommands.SW_SHOWMAXIMIZED)
			{
				if (!MaximizedWindows.Contains(hwnd))
				{
					if (_lastClosedWindow == hwnd && ((TimeSpan)(DateTime.Now - _lastClosedWindowTime)).TotalSeconds < 1) { return; }

					MaximizedWindows.Add(hwnd);
					Taskbars.MaximizedStateChanged = true;
				}
			}
			else if (placement.showCmd == WindowPlacementCommands.SW_NORMAL)
			{
				if (MaximizedWindows.Contains(hwnd))
				{
					MaximizedWindows.Remove(hwnd);
					Taskbars.MaximizedStateChanged = true;
				}
			}
			else if (placement.showCmd == WindowPlacementCommands.SW_SHOWMINIMIZED || placement.showCmd == WindowPlacementCommands.SW_MINIMIZE)
			{
				if (MaximizedWindows.Contains(hwnd))
				{
					MaximizedWindows.Remove(hwnd);
					Taskbars.MaximizedStateChanged = true;
				}
			}


		}

		private void UpdateAllSettings()
		{
			SetAccentState((AccentState)AccentStateComboBox.SelectedItem);
			SetTaskbarColor(GradientColorPicker.SelectedColor ?? Color.FromArgb(255, 255, 255, 255));
			SetAccentFlags(ColorizeBlurCheckBox.IsChecked ?? false);
			WindowsAccentColorCheckBox_Changed(null, null);
			SetWindowsAccentAlpha((byte)WindowsAccentAlphaSlider.Value);

			Taskbars.UpdateAllSettings();
		}

		private void SwitchTaskbarBeingEdited(string switchTo)
		{
			TaskbarBeingEdited = switchTo;
			ShowTaskbarSettings(TaskbarBeingEdited);
			EditSwitchButton.Content = $"{TaskbarBeingEdited} Taskbar";
		}

		private void ShowTaskbarSettings(string tb)
		{
			if (tb == "Main")
			{
				AccentStateComboBox.SelectedItem = (AccentState)TT.Options.Settings.MainTaskbarStyle.AccentState;
				GradientColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(TT.Options.Settings.MainTaskbarStyle.GradientColor);
				WindowsAccentAlphaSlider.Value = TT.Options.Settings.MainTaskbarStyle.WindowsAccentAlpha;
				ColorizeBlurCheckBox.IsChecked = TT.Options.Settings.MainTaskbarStyle.Colorize;
				WindowsAccentColorCheckBox.IsChecked = TT.Options.Settings.MainTaskbarStyle.UseWindowsAccentColor;
			}
			else if (tb == "Maximized")
			{
				AccentStateComboBox.SelectedItem = (AccentState)TT.Options.Settings.MaximizedTaskbarStyle.AccentState;
				GradientColorPicker.SelectedColor = (Color)ColorConverter.ConvertFromString(TT.Options.Settings.MaximizedTaskbarStyle.GradientColor);
				WindowsAccentAlphaSlider.Value = TT.Options.Settings.MaximizedTaskbarStyle.WindowsAccentAlpha;
				ColorizeBlurCheckBox.IsChecked = TT.Options.Settings.MaximizedTaskbarStyle.Colorize;
				WindowsAccentColorCheckBox.IsChecked = TT.Options.Settings.MaximizedTaskbarStyle.UseWindowsAccentColor;
			}
		}

		#endregion Functions

		#region Control Handles
		private void StartStopButton_Click(object sender, RoutedEventArgs e)
		{
			if (_runApplyTask)
			{
				StartStopButton.Content = "Start";
				_runApplyTask = false;
			}
			else
			{
				StartStopButton.Content = "Stop";
				_runApplyTask = true;

				_applyTask = new Task(() => ApplyToAllTaskbars());
				_applyTask.Start();
			}
		}

		private void AccentStateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (!_windowInitialized) return;
			SetAccentState((AccentState)AccentStateComboBox.SelectedItem);
		}

		private void GradientColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
		{
			if (!_windowInitialized) return;

			SetTaskbarColor(GradientColorPicker.SelectedColor ?? Color.FromArgb(255, 255, 255, 255));
		}

		private void ColorizeBlurCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			if (!_windowInitialized) return;
			SetAccentFlags(ColorizeBlurCheckBox.IsChecked ?? false);
		}

		private void SysTrayIcon_MouseClick(object sender, EventArgs e)
		{
			var me = (System.Windows.Forms.MouseEventArgs)e;
			if (me.Button == System.Windows.Forms.MouseButtons.Right)
			{
				_sysTrayContextMenu.PlacementTarget = sender as Button;
				_sysTrayContextMenu.IsOpen = true;
				Activate();
			}
		}

		private void WindowsAccentColorCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			Globals.WindowsAccentColor = WindowsAccentColor.GetColorAsInt();

			if (!_windowInitialized) return;

			var use = WindowsAccentColorCheckBox.IsChecked ?? false;
			SetUseAccentColor(use);
			GradientColorPicker.IsEnabled = !use;
		}

		private void UseMaximizedSettingsCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			if (!_windowInitialized) return;
			TT.Options.Settings.UseDifferentSettingsWhenMaximized = UseMaximizedSettingsCheckBox.IsChecked ?? false;
			Taskbars.UpdateAllSettings();
		}

		private void WindowsAccentAlphaSlider_DragCompleted(object sender, RoutedEventArgs e)
		{
			_alphaDragStarted = false;
			SetWindowsAccentAlpha((byte)WindowsAccentAlphaSlider.Value);
		}

		private void WindowsAccentAlphaSlider_DragStarted(object sender, RoutedEventArgs e)
		{
			_alphaDragStarted = true;
		}

		private void WindowsAccentAlphaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!_windowInitialized) return;
			if (!_alphaDragStarted)
			{
				SetWindowsAccentAlpha((byte)WindowsAccentAlphaSlider.Value);
			}
		}

		private void OptionsButton_Click(object sender, RoutedEventArgs e)
		{
			if (MainGrid.RowDefinitions[2].Height == new GridLength(0))
			{
				MainGrid.RowDefinitions[2].Height = new GridLength(90);
				Height += 90;
			}
			else
			{
				MainGrid.RowDefinitions[2].Height = new GridLength(0);
				Height -= 90;
			}
		}

		private void StartWithWindowsCheckBox_Changed(object sender, RoutedEventArgs e)
		{
			if (!_windowInitialized) return;

			TT.Options.Settings.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;

			try
			{
				if (TT.Options.Settings.StartWithWindows) { _appRegistryKey.SetValue("TaskbarTools", $"\"{_applicationPath}\""); }
				else { _appRegistryKey.DeleteValue("TaskbarTools", false); }
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Failed to Set Registry Key");
			}
		}

		private void EditSwitchButton_Click(object sender, RoutedEventArgs e)
		{
			if (TaskbarBeingEdited == "Main")
			{
				SwitchTaskbarBeingEdited("Maximized");
			}
			else if (TaskbarBeingEdited == "Maximized")
			{
				SwitchTaskbarBeingEdited("Main");
			}
		}

		#endregion Control Handles
	}

	internal class NativeMethods
	{
		public const string StartMenuClass = "Windows.UI.Core.CoreWindow";

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		[DllImport("USER32.DLL")]
		public static extern bool IsWindowVisible(IntPtr IntPtr);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int uFlags);
		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
		public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);
		[DllImport("user32.dll")]
		public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, IntPtr dwExtraInfo);
		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
		public const int SWP_SHOWWINDOW = 0x0040;
		public const int SWP_HIDEWINDOW = 0x0080;
		public const int KEYEVENTF_KEYUP = 0x0002;
		public const int VK_ESCAPE = 27;
	}


}