namespace TaskbarTool.Enums
{
	// AccentPolicy = packed record
	//	AccentState: Integer;
	//  AccentFlags: Integer;
	//  GradientColor: Integer;
	//  AnimationId: Integer;
	//  end;
	// ACCENT_ENABLE_ACRYLICBLURBEHIND = 4;
	//and the state can have any of these values:

	public enum AccentState
	{
		ACCENT_DISABLED = 0,
		ACCENT_ENABLE_GRADIENT = 1,
		ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
		ACCENT_ENABLE_BLURBEHIND = 3,
		ACCENT_ENABLE_ACRYLIC = 4,
		ACCENT_INVALID_STATE = 5
	}

	public enum WindowCompositionAttribute
	{
		WCA_ACCENT_POLICY = 19
	}

	public enum WindowPlacementCommands
	{
		SW_HIDE = 0,
		SW_SHOWNORMAL = 1,
		SW_NORMAL = 1,
		SW_SHOWMINIMIZED = 2,
		SW_SHOWMAXIMIZED = 3,
		SW_MAXIMIZE = 3,
		SW_SHOWNOACTIVATE = 4,
		SW_SHOW = 5,
		SW_MINIMIZE = 6,
		SW_SHOWMINNOACTIVE = 7,
		SW_SHOWNA = 8,
		SW_RESTORE = 9
	}
}
