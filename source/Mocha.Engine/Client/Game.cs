﻿namespace Bango.Engine;

/// <summary>
/// Handles the creation of various game systems.
/// </summary>
internal class Game
{
	private RendererInstance renderer;
	private EditorInstance editor;

	internal Game()
	{
		if ( Veldrid.RenderDoc.Load( out var renderDoc ) )
		{
			renderDoc.OverlayEnabled = false;
			Log.Trace( "Loaded RenderDoc" );
		}

		using ( var _ = new Stopwatch( "Game init" ) )
		{
			Log.Trace( "Game init" );
			renderer = new();
			editor = new();

			// Must be called before everything else
			renderer.PreUpdate += Input.Update;
			renderer.RenderOverlays += editor.Render;
		}

		renderer.Run();
	}
}
