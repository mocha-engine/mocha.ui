﻿using Veldrid;
using Veldrid.Sdl2;

namespace Mocha;

public class MochaInputSnapshot : InputSnapshot
{
	#region "InputSnapshot Interface"
	public IReadOnlyList<KeyEvent> KeyEvents { get; internal set; }
	public IReadOnlyList<MouseEvent> MouseEvents { get; internal set; }
	public IReadOnlyList<char> KeyCharPresses { get; internal set; }
	public System.Numerics.Vector2 MousePosition { get; internal set; }
	public float WheelDelta { get; internal set; }
	public bool IsMouseDown( MouseButton button ) => button switch
	{
		MouseButton.Left => MouseLeft,
		MouseButton.Right => MouseRight,
		_ => false,
	};
	#endregion

	public System.Numerics.Vector2 MouseDelta { get; internal set; }

	public float Forward { get; set; }
	public float Left { get; set; }
	public float Up { get; set; }

	public bool MouseLeft { get; set; }
	public bool MouseRight { get; set; }

	internal List<SDL_Keysym> LastKeysDown { get; set; } = new();
	internal List<SDL_Keysym> KeysDown { get; set; } = new();
}