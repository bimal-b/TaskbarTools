using System;
using System.IO;
using System.Xml.Serialization;

namespace TaskbarTool
{
	public static class TT
	{
		// Options
		public static Options Options = new Options();

		// My Documents
		private static readonly string MyDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		private static readonly string FilePath = MyDocuments + "\\TaskbarTools\\Options.xml";

		public static void InitializeOptions()
		{
			if (!LoadOptions())
			{
				AssignDefaults();
			}
		}

		public static bool SaveOptions()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Options));

			try
			{
				var dirPath = Path.GetDirectoryName(FilePath);

				if (!Directory.Exists(dirPath))
				{
					Directory.CreateDirectory(dirPath);
				}

				using (FileStream fstream = new FileStream(FilePath, FileMode.Create))
				{
					serializer.Serialize(fstream, Options);
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		private static bool LoadOptions()
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Options));
			if (!File.Exists(FilePath)) { return false; }

			try
			{
				using (FileStream reader = new FileStream(FilePath, FileMode.Open))
				{
					Options = serializer.Deserialize(reader) as Options;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("!! Error loading Options.xml");
				Console.WriteLine(ex.Message);
				return false;
			}
			return true;
		}

		private static void AssignDefaults()
		{
			Options.Settings = new OptionsSettings
			{
				StartMinimized = false,
				StartWhenLaunched = true,
				StartWithWindows = false,
				UseDifferentSettingsWhenMaximized = false,

				MainTaskbarStyle = new OptionsSettingsMainTaskbarStyle()
			};
			Options.Settings.MainTaskbarStyle.AccentState = 3;
			Options.Settings.MainTaskbarStyle.GradientColor = "#804080FF";
			Options.Settings.MainTaskbarStyle.Colorize = true;
			Options.Settings.MainTaskbarStyle.UseWindowsAccentColor = true;
			Options.Settings.MainTaskbarStyle.WindowsAccentAlpha = 127;

			Options.Settings.MaximizedTaskbarStyle = new OptionsSettingsMaximizedTaskbarStyle
			{
				AccentState = 2,
				GradientColor = "#FF000000",
				Colorize = false,
				UseWindowsAccentColor = true,
				WindowsAccentAlpha = 255
			};
		}
	}

	#region XML Classes

	/// <remarks/>
	[Serializable()]
	[System.ComponentModel.DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	[XmlRoot(Namespace = "", IsNullable = false)]
	public partial class Options
	{

		private OptionsSettings settingsField;

		private byte versionField;

		/// <remarks/>
		public OptionsSettings Settings
		{
			get
			{
				return settingsField;
			}
			set
			{
				settingsField = value;
			}
		}

		/// <remarks/>
		[XmlAttribute()]
		public byte Version
		{
			get
			{
				return versionField;
			}
			set
			{
				versionField = value;
			}
		}
	}

	/// <remarks/>
	[Serializable()]
	[System.ComponentModel.DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public partial class OptionsSettings
	{

		private bool startMinimizedField;

		private bool startWhenLaunchedField;

		private bool startWithWindowsField;

		private bool useDifferentSettingsWhenMaximizedField;

		private OptionsSettingsMainTaskbarStyle mainTaskbarStyleField;

		private OptionsSettingsMaximizedTaskbarStyle maximizedTaskbarStyleField;

		/// <remarks/>
		public bool StartMinimized
		{
			get
			{
				return startMinimizedField;
			}
			set
			{
				startMinimizedField = value;
			}
		}

		/// <remarks/>
		public bool StartWhenLaunched
		{
			get
			{
				return startWhenLaunchedField;
			}
			set
			{
				startWhenLaunchedField = value;
			}
		}

		/// <remarks/>
		public bool StartWithWindows
		{
			get
			{
				return startWithWindowsField;
			}
			set
			{
				startWithWindowsField = value;
			}
		}

		/// <remarks/>
		public bool UseDifferentSettingsWhenMaximized
		{
			get
			{
				return useDifferentSettingsWhenMaximizedField;
			}
			set
			{
				useDifferentSettingsWhenMaximizedField = value;
			}
		}

		/// <remarks/>
		public OptionsSettingsMainTaskbarStyle MainTaskbarStyle
		{
			get
			{
				return mainTaskbarStyleField;
			}
			set
			{
				mainTaskbarStyleField = value;
			}
		}

		/// <remarks/>
		public OptionsSettingsMaximizedTaskbarStyle MaximizedTaskbarStyle
		{
			get
			{
				return maximizedTaskbarStyleField;
			}
			set
			{
				maximizedTaskbarStyleField = value;
			}
		}
	}

	/// <remarks/>
	[Serializable()]
	[System.ComponentModel.DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public partial class OptionsSettingsMainTaskbarStyle
	{

		private byte accentStateField;

		private string gradientColorField;

		private bool colorizeField;

		private bool useWindowsAccentColorField;

		private byte windowsAccentAlphaField;

		/// <remarks/>
		public byte AccentState
		{
			get
			{
				return accentStateField;
			}
			set
			{
				accentStateField = value;
				Taskbars.UpdateAccentState();
			}
		}

		/// <remarks/>
		public string GradientColor
		{
			get
			{
				return gradientColorField;
			}
			set
			{
				gradientColorField = value;
				Taskbars.UpdateColor();
			}
		}

		/// <remarks/>
		public bool Colorize
		{
			get
			{
				return colorizeField;
			}
			set
			{
				colorizeField = value;
				Taskbars.UpdateAccentFlags();
			}
		}

		/// <remarks/>
		public bool UseWindowsAccentColor
		{
			get
			{
				return useWindowsAccentColorField;
			}
			set
			{
				useWindowsAccentColorField = value;
				Taskbars.UpdateColor();
			}
		}

		/// <remarks/>
		public byte WindowsAccentAlpha
		{
			get
			{
				return windowsAccentAlphaField;
			}
			set
			{
				windowsAccentAlphaField = value;
				if (UseWindowsAccentColor) { Taskbars.UpdateColor(); }
			}
		}
	}

	/// <remarks/>
	[Serializable()]
	[System.ComponentModel.DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public partial class OptionsSettingsMaximizedTaskbarStyle
	{

		private byte accentStateField;

		private string gradientColorField;

		private bool colorizeField;

		private bool useWindowsAccentColorField;

		private byte windowsAccentAlphaField;

		/// <remarks/>
		public byte AccentState
		{
			get
			{
				return accentStateField;
			}
			set
			{
				accentStateField = value;
				Taskbars.UpdateAccentState();
			}
		}

		/// <remarks/>
		public string GradientColor
		{
			get
			{
				return gradientColorField;
			}
			set
			{
				gradientColorField = value;
				Taskbars.UpdateColor();
			}
		}

		/// <remarks/>
		public bool Colorize
		{
			get
			{
				return colorizeField;
			}
			set
			{
				colorizeField = value;
				Taskbars.UpdateAccentFlags();
			}
		}

		/// <remarks/>
		public bool UseWindowsAccentColor
		{
			get
			{
				return useWindowsAccentColorField;
			}
			set
			{
				useWindowsAccentColorField = value;
				Taskbars.UpdateColor();
			}
		}

		/// <remarks/>
		public byte WindowsAccentAlpha
		{
			get
			{
				return windowsAccentAlphaField;
			}
			set
			{
				windowsAccentAlphaField = value;
				if (UseWindowsAccentColor) { Taskbars.UpdateColor(); }
			}
		}
	}


	#endregion XML Classes
}