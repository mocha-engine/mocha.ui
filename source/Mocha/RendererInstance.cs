﻿using Veldrid;
using Veldrid.StartupUtilities;

namespace Mocha.Renderer;

public class RendererInstance
{
	public Window window;

	private SceneWorld world;
	private DateTime lastFrame;

	private CommandList commandList;
	private ImGuiRenderer imguiRenderer;

	private Material gbufferCombineMaterial;
	private Model fullscreenQuad;

	public Action PreUpdate;
	public Action OnUpdate;
	public Action PostUpdate;
	public Action OnRender;

	public RendererInstance()
	{
		Event.Register( this );

		Init();
		lastFrame = DateTime.Now;
	}

	private void Init()
	{
		window = new();

		CreateGraphicsDevice();
		commandList = Device.ResourceFactory.CreateCommandList();

		imguiRenderer = new( Device,
			  Device.SwapchainFramebuffer.OutputDescription,
			  window.SdlWindow.Width,
			  window.SdlWindow.Height );

		world = new();
	}

	public void Run()
	{
		gbufferCombineMaterial = new Material()
		{
			Shader = ShaderBuilder.Default.FromMoyaiShader( "content/shaders/combine.mshdr" )
											.WithFramebuffer( Device.SwapchainFramebuffer )
											.WithFaceCullMode( FaceCullMode.None )
											.Build(),
			UniformBufferType = typeof( EmptyUniformBuffer ),
			DiffuseTexture = world.Camera.ColorTexture
		};

		fullscreenQuad = Primitives.Plane.GenerateModel( gbufferCombineMaterial );

		while ( window.SdlWindow.Exists )
		{
			Update();
			PreRender();

			world.Sun.CalcViewProjMatrix();

			RenderPass( Renderer.RenderPass.ShadowMap, world.Sun.ViewMatrix * world.Sun.ProjMatrix, world.Sun.ShadowBuffer );
			RenderPass( Renderer.RenderPass.Main, world.Camera.ViewMatrix * world.Camera.ProjMatrix, world.Camera.Framebuffer );

			PostRender();
		}
	}

	private void PreRender()
	{
		// TODO: Make this nicer
		// Check each shader, if it's dirty then recompile it
		foreach ( var shader in Shader.All.Where( x => x.IsDirty ) )
		{
			shader.Recompile();
		}

		commandList.Begin();
	}

	private void PostRender()
	{
		commandList.SetFramebuffer( Device.SwapchainFramebuffer );
		commandList.SetViewport( 0, new Viewport( 0, 0, Device.SwapchainFramebuffer.Width, Device.SwapchainFramebuffer.Height, 0, 1 ) );
		commandList.SetFullViewports();
		commandList.SetFullScissorRects();
		commandList.ClearColorTarget( 0, RgbaFloat.Black );
		commandList.ClearDepthStencil( 1 );

		commandList.PushDebugGroup( "GBuffer Combine" );
		fullscreenQuad.Draw( Renderer.RenderPass.Combine, new EmptyUniformBuffer(), commandList );
		commandList.PopDebugGroup();

		commandList.PushDebugGroup( "ImGUI" );
		imguiRenderer?.Render( Device, commandList );
		commandList.PopDebugGroup();

		commandList.End();

		Device.SubmitCommands( commandList );
		Device.SwapBuffers();
	}

	private void RenderPass( RenderPass renderPass, Matrix4x4 viewProjMatrix, Framebuffer framebuffer )
	{
		commandList.PushDebugGroup( renderPass.ToString() );
		commandList.SetFramebuffer( framebuffer );
		commandList.SetViewport( 0, new Viewport( 0, 0, framebuffer.Width, framebuffer.Height, 0, 1 ) );
		commandList.SetFullViewports();
		commandList.SetFullScissorRects();

		for ( uint i = 0; i < framebuffer.ColorTargets.Count; ++i )
			commandList.ClearColorTarget( i, RgbaFloat.Black );

		commandList.ClearDepthStencil( 1 );

		world.Render( viewProjMatrix, renderPass, commandList );
		commandList.PopDebugGroup();
	}

	private void Update()
	{
		float deltaTime = (float)(DateTime.Now - lastFrame).TotalSeconds;
		lastFrame = DateTime.Now;

		Time.UpdateFrom( deltaTime );

		PreUpdate?.Invoke();
		OnUpdate?.Invoke();
		imguiRenderer.Update( Time.Delta, Input.Snapshot );
		PostUpdate?.Invoke();
	}

	private void CreateGraphicsDevice()
	{
		var options = new GraphicsDeviceOptions()
		{
			PreferStandardClipSpaceYDirection = true,
			PreferDepthRangeZeroToOne = true,
			SwapchainDepthFormat = PixelFormat.D24_UNorm_S8_UInt,
			SwapchainSrgbFormat = false,
			SyncToVerticalBlank = false
		};

		var preferredBackend = GraphicsBackend.Direct3D11;
		Device = VeldridStartup.CreateGraphicsDevice( Window.Current.SdlWindow, options, preferredBackend );

		Device.SyncToVerticalBlank = true;

		var windowTitle = $"Mocha | {Device.BackendType}";
		Window.Current.SdlWindow.Title = windowTitle;
	}

	public IntPtr GetImGuiBinding( Texture texture )
	{
		return imguiRenderer.GetOrCreateImGuiBinding( Device.ResourceFactory, texture.VeldridTextureView );
	}

	[Event.Window.Resized]
	public void OnWindowResized( Point2 newSize )
	{
		imguiRenderer.WindowResized( newSize.X, newSize.Y );
		Device.MainSwapchain.Resize( (uint)newSize.X, (uint)newSize.Y );
	}
}