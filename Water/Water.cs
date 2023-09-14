using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AssetManagementBase;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Water.FNA.Core;
using Water.Utils;

namespace Water
{
	public class Water : Game
	{
		#region Fields

		readonly GraphicsDeviceManager _graphics;
		private GraphicsDevice _device;
		private RasterizerState _rasterizerState;
		SpriteBatch _spriteBatch;

		// Text
		private SpriteFontBase _font;
		private bool _displayInfo = true;

		private float _aspectRatio;
		private float _nearPlane;
		private float _farPlane;
		private float _fieldOfView;

		// Matrices
		private Matrix _projectionMatrix;
		private Matrix _viewMatrix;

		private Matrix _reflectionViewMatrix;

		// Input
		// Terrain
		private float[,] _terrainHeights;
		private Point _terrainSize;
		private const float TerrainMaxHeight = 50;
		private Texture2D _terrainTexture;
		private VertexPositionNormalTexture[] _terrainVertices;
		private int[] _terrainIndices;
		private Vector4 _refractionClippingPlane;
		private Vector4 _reflectionClippingPlane;

		// Water
		private Vector3 _waterColor;
		private float _waterHeight;
		private VertexPositionTexture[] _waterVertices;
		private int[] _waterIndices;
		private float _waveTextureScale;

		private bool _drawWater = true;
		private bool _enableWaves = true;
		private bool _enableRefraction = true;
		private bool _enableReflection = true;
		private bool _enableFresnel = true;
		private bool _enableSpecularLighting = true;
		private bool _enableLighting = true;
		private bool _drawSkybox = true;

		private float _refractionReflectionMergeTerm;

		// Wave normal maps
		private Texture2D _waveNormalMap0;
		private Texture2D _waveNormalMap1;

		// Waves velocity
		private Vector2 _waveVelocity0;
		private Vector2 _waveVelocity1;

		// Wave normal map offsets
		private Vector2 _waveNormalMapOffset0;
		private Vector2 _waveNormalMapOffset1;

		// Refraction
		RenderTarget2D _refractionRenderTarget;
		Texture2D _refractionTexture;

		// Refraction
		RenderTarget2D _reflectionRenderTarget;
		Texture2D _reflectionTexture;

		// Lighting
		private Vector4 _ambientColor;
		private float _ambientIntensity;
		private Vector4 _diffuseColor;
		private float _diffuseIntensity;

		// Sun
		private Vector3 _sunColor;
		private Vector3 _sunDirection;
		private float _sunFactor;
		private float _sunPower;

		// Skyboxes
		private MeshData _skyboxCube;
		private TextureCube _skyboxTexture;
		private Effect _skyboxEffect;
		private const float SkyboxSize = 5000f;

		// Shaders
		private Effect _basicEffect;
		private Effect _refractionEffect;
		private Effect _reflectionEffect;
		private Effect _waterEffect;
		private Effect _waterEffectWithoutWaves;
		private CameraInputController _controller;

		public Camera Camera { get; } = new Camera();

		private static Assembly Assembly => typeof(Water).Assembly;

		public static readonly string RootAssetFolder = Path.Combine(FileSystem.ExecutingAssemblyDirectory, "Assets");
		public static readonly AssetManager AssetManager = AssetManager.CreateFileAssetManager(RootAssetFolder);
#if FNA
		public static AssetManager AssetManagerEffects = AssetManager.CreateResourceAssetManager(Assembly, "Shaders.FNA");
#else
		public static AssetManager AssetManagerEffects = AssetManager.CreateResourceAssetManager(Assembly, "Shaders.MonoGameOGL");
#endif

		#endregion

		public Water()
		{
			_graphics = new GraphicsDeviceManager(this);

			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void Initialize()
		{
			_device = _graphics.GraphicsDevice;

			// Graphics preferences
			_graphics.PreferredBackBufferWidth = 1600;
			_graphics.PreferredBackBufferHeight = 900;
			_graphics.IsFullScreen = false;
			_graphics.ApplyChanges();
			Window.Title = "Water project";

			// Camera init
			_nearPlane = 1.0f;
			_farPlane = 10000.0f;
			_fieldOfView = 45.0f;

			_aspectRatio = (float)_device.Viewport.Width / _device.Viewport.Height;

			_projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(_fieldOfView), _aspectRatio, _nearPlane, _farPlane);

			// Water
			_waterColor = new Vector3(0.5f, 0.79f, 0.75f);

			_refractionReflectionMergeTerm = 0.5f;

			// Waves
			_waveTextureScale = 2.5f;
			_waveVelocity0 = new Vector2(0.01f, 0.03f);
			_waveVelocity1 = new Vector2(-0.01f, 0.03f);

			// Lighting
			_ambientColor = new Vector4(.42f, .42f, .42f, 1.0f);
			_ambientIntensity = 1.0f;
			_diffuseColor = new Vector4(0.75f, 0.3f, 0.3f, 1.0f);
			_diffuseIntensity = 1.0f;

			// Sun
			_sunColor = new Vector3(1.0f, 0.8f, 0.4f);
			_sunDirection = new Vector3(-.85f, -.45f, -.25f);
			_sunFactor = 1.5f;
			_sunPower = 250.0f;


			// Components
			Components.Add(new FrameRateCounter(this));
			Components.Add(new InputManager(this));

			base.Initialize();

			Camera.SetLookAt(new Vector3(100, 100, 100), new Vector3(101, 90, 101));

			_controller = new CameraInputController(Camera);
		}

		protected override void LoadContent()
		{
			PresentationParameters pp = _device.PresentationParameters;

			// Render targets
			_refractionRenderTarget = new RenderTarget2D(
				_device,
				pp.BackBufferWidth,
				pp.BackBufferHeight,
				false,
				GraphicsDevice.PresentationParameters.BackBufferFormat,
				DepthFormat.Depth24);

			_reflectionRenderTarget = new RenderTarget2D(
				_device,
				pp.BackBufferWidth,
				pp.BackBufferHeight,
				false,
				GraphicsDevice.PresentationParameters.BackBufferFormat,
				DepthFormat.Depth24);

			CreateRasterizerState(FillMode.Solid);

			// Create a new SpriteBatch, which can be used to draw textures.
			_spriteBatch = new SpriteBatch(GraphicsDevice);

			// Load font file
			_font = AssetManager.LoadFontSystem("Fonts/DroidSans.ttf").GetFont(32);

			// Load textures
			_terrainTexture = AssetManager.LoadTexture2D(GraphicsDevice, @"Textures/terrain_texture.jpg");
			_waveNormalMap0 = AssetManager.LoadDds(GraphicsDevice, @"Textures/wave0.dds");
			_waveNormalMap1 = AssetManager.LoadDds(GraphicsDevice, @"Textures/wave1.dds");

			// Load height map
			LoadHeightData(Path.Combine(RootAssetFolder, "terrain_height.raw"));

			// Water
			_waterHeight = 20;
			_refractionClippingPlane = CreateClippingPlane(false);
			_reflectionClippingPlane = CreateClippingPlane(true);

			// Create vertex/index buffers
			SetUpVertices();
			SetUpIndices();

			// Shaders
			_basicEffect = AssetManagerEffects.LoadEffect(GraphicsDevice, "Lights.efb");
			_refractionEffect = AssetManagerEffects.LoadEffect(GraphicsDevice, "Refraction.efb");
			_reflectionEffect = AssetManagerEffects.LoadEffect(GraphicsDevice, "Reflection.efb");
			_waterEffect = AssetManagerEffects.LoadEffect(GraphicsDevice, "Water.efb", new Dictionary<string, string>
			{
				["WAVES"] = "1"
			});
			_waterEffectWithoutWaves = AssetManagerEffects.LoadEffect(GraphicsDevice, "Water.efb");
			_skyboxEffect = AssetManagerEffects.LoadEffect(GraphicsDevice, "Skybox.efb");

			// Skyboxes
			_skyboxCube = PrimitiveMeshes.CreateCubePositionFromMinusOneToOne(GraphicsDevice);
			_skyboxTexture = AssetManager.LoadDdsCube(GraphicsDevice, "Skyboxes/Islands.dds");
		}

		protected override void Update(GameTime gameTime)
		{
			if (!IsActive)
				return;

			MouseState mouseState = Mouse.GetState();

			// Allows the game to exit
			if (InputManager.KeyDown(Keys.Escape))
				Exit();

			#region Configuration region

			// Switch fill mode
			if (InputManager.KeyPressed(Keys.F2))
			{
				var newFillMode = (_rasterizerState.FillMode == FillMode.Solid)
					? FillMode.WireFrame
					: FillMode.Solid;

				CreateRasterizerState(newFillMode);
			}

			// Switch draw water
			if (InputManager.KeyPressed(Keys.F3))
			{
				_enableLighting = !_enableLighting;
			}

			// Switch draw water
			if (InputManager.KeyPressed(Keys.F4))
			{
				_drawWater = !_drawWater;
			}

			// Switch draw skybox
			if (InputManager.KeyPressed(Keys.F10))
			{
				_drawSkybox = !_drawSkybox;
			}

			// Switch enable info displaying
			if (InputManager.KeyPressed(Keys.F1))
			{
				_displayInfo = !_displayInfo;
			}

			// Change ambient intensity
			if (InputManager.KeyDown(Keys.Insert))
			{
				_ambientIntensity = MathHelper.Clamp(_ambientIntensity + 0.1f, 0f, 10f);
			}
			else if (InputManager.KeyDown(Keys.Delete))
			{
				_ambientIntensity = MathHelper.Clamp(_ambientIntensity - 0.1f, 0f, 10f);
			}

			// Change directionnal light intensity
			if (InputManager.KeyDown(Keys.Home))
			{
				_diffuseIntensity = MathHelper.Clamp(_diffuseIntensity + 0.1f, 0f, 10f);
			}
			else if (InputManager.KeyDown(Keys.End))
			{
				_diffuseIntensity = MathHelper.Clamp(_diffuseIntensity - 0.1f, 0f, 10f);
			}

			// Change directionnal light direction
			if (InputManager.KeyDown(Keys.NumPad8))
			{
				_sunDirection.Z += 0.1f;
			}
			else if (InputManager.KeyDown(Keys.NumPad5))
			{
				_sunDirection.Z -= 0.1f;
			}
			else if (InputManager.KeyDown(Keys.NumPad9))
			{
				_sunDirection.Y += 0.1f;
			}
			else if (InputManager.KeyDown(Keys.NumPad3))
			{
				_sunDirection.Y -= 0.1f;
			}
			else if (InputManager.KeyDown(Keys.NumPad6))
			{
				_sunDirection.X -= 0.1f;
			}
			else if (InputManager.KeyDown(Keys.NumPad4))
			{
				_sunDirection.X += 0.1f;
			}

			_sunDirection.Normalize();

			// Change water height
			if (InputManager.KeyDown(Keys.PageUp))
			{
				_waterHeight += 0.1f;
				_refractionClippingPlane = CreateClippingPlane(false);
				_reflectionClippingPlane = CreateClippingPlane(true);
				SetUpVertices();
				SetUpIndices();
			}
			else if (InputManager.KeyDown(Keys.PageDown))
			{
				_waterHeight -= 0.1f;
				_refractionClippingPlane = CreateClippingPlane(false);
				_reflectionClippingPlane = CreateClippingPlane(true);
				SetUpVertices();
				SetUpIndices();
			}

			// Change water texture scale
			if (InputManager.KeyDown(Keys.W))
			{
				_waveTextureScale = MathHelper.Clamp(_waveTextureScale - 1, 1, 500);
			}
			else if (InputManager.KeyDown(Keys.X))
			{
				_waveTextureScale = MathHelper.Clamp(_waveTextureScale + 1, 1, 500);
			}

			// Switch enable refraction
			if (InputManager.KeyPressed(Keys.F5))
			{
				_enableRefraction = !_enableRefraction;
			}

			// Switch enable reflection
			if (InputManager.KeyPressed(Keys.F6))
			{
				_enableReflection = !_enableReflection;
			}

			// Switch enable fresnel term
			if (InputManager.KeyPressed(Keys.F7))
			{
				_enableFresnel = !_enableFresnel;
			}

			// Switch enable waves
			if (InputManager.KeyPressed(Keys.F8))
			{
				_enableWaves = !_enableWaves;
			}

			// Switch enable specular lighting
			if (InputManager.KeyPressed(Keys.F9))
			{
				_enableSpecularLighting = !_enableSpecularLighting;
			}

			// Change water texture scale
			if (InputManager.KeyDown(Keys.C))
			{
				_refractionReflectionMergeTerm = MathHelper.Clamp(_refractionReflectionMergeTerm - 0.01f, 0, 1);
			}
			else if (InputManager.KeyDown(Keys.V))
			{
				_refractionReflectionMergeTerm = MathHelper.Clamp(_refractionReflectionMergeTerm + 0.01f, 0, 1);
			}

			// Change waves speed
			if (InputManager.KeyDown(Keys.N))
			{
				_waveVelocity0.X += 0.01f;
				_waveVelocity0.Y += 0.01f;

				_waveVelocity1.X += 0.01f;
				_waveVelocity1.Y += 0.01f;
			}
			else if (InputManager.KeyDown(Keys.B))
			{
				_waveVelocity0.X -= 0.01f;
				_waveVelocity0.Y -= 0.01f;

				_waveVelocity1.X -= 0.01f;
				_waveVelocity1.Y -= 0.01f;
			}

			#endregion

			#region Camera

			var keyboardState = Keyboard.GetState();

			// Manage camera input controller
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Left, keyboardState.IsKeyDown(Keys.A));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Right, keyboardState.IsKeyDown(Keys.D));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Forward, keyboardState.IsKeyDown(Keys.W));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Backward, keyboardState.IsKeyDown(Keys.S));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Up, keyboardState.IsKeyDown(Keys.Up));
			_controller.SetControlKeyState(CameraInputController.ControlKeys.Down, keyboardState.IsKeyDown(Keys.Down));

			_controller.SetTouchState(CameraInputController.TouchType.Rotate, mouseState.RightButton == ButtonState.Pressed);
			_controller.SetMousePosition(new Point(mouseState.X, mouseState.Y));

			_controller.Update();

			_viewMatrix = Camera.View;

			// Compute reflection view matrix
			Vector3 reflCameraPosition = Camera.Position;
			reflCameraPosition.Y = -Camera.Position.Y + _waterHeight * 2;
			Vector3 reflTargetPos = Camera.Target;
			reflTargetPos.Y = -Camera.Target.Y + _waterHeight * 2;

			Vector3 invUpVector = Vector3.Cross(Camera.Right, reflTargetPos - reflCameraPosition);

			_reflectionViewMatrix = Matrix.CreateLookAt(reflCameraPosition, reflTargetPos, invUpVector);

			#endregion

			// Update the wave map offsets so that they will scroll across the water
			var deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			_waveNormalMapOffset0 += _waveVelocity0 * deltaTime;
			_waveNormalMapOffset1 += _waveVelocity1 * deltaTime;

			if (_waveNormalMapOffset0.X >= 1.0f || _waveNormalMapOffset0.X <= -1.0f)
				_waveNormalMapOffset0.X = 0.0f;
			if (_waveNormalMapOffset1.X >= 1.0f || _waveNormalMapOffset1.X <= -1.0f)
				_waveNormalMapOffset1.X = 0.0f;
			if (_waveNormalMapOffset0.Y >= 1.0f || _waveNormalMapOffset0.Y <= -1.0f)
				_waveNormalMapOffset0.Y = 0.0f;
			if (_waveNormalMapOffset1.Y >= 1.0f || _waveNormalMapOffset1.Y <= -1.0f)
				_waveNormalMapOffset1.Y = 0.0f;

			base.Update(gameTime);
		}

		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			// Change rasterization setup
			_device.RasterizerState = _rasterizerState;

			_device.BlendState = BlendState.Opaque;
			_device.DepthStencilState = DepthStencilState.Default;
			_device.SamplerStates[0] = SamplerState.LinearWrap;

			_device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);

			// Generate refraction texture
			DrawRefractionMap();

			// Generate reflection texture
			DrawReflectionMap();

			_device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);

			// Draw skybox
			if (_drawSkybox)
			{
				DrawSkybox(_viewMatrix, _projectionMatrix, Camera.Position);
			}

			// Draw terrain
			DrawTerrain(_basicEffect, _viewMatrix);

			// Draw water
			if (_drawWater)
			{
				DrawWater();
			}

			// Display text
			if (_displayInfo)
			{
				_spriteBatch.Begin();

				_spriteBatch.DrawString(_font, "Position: " + Camera.Position.ToString(), new Vector2(0, 30), Color.White);
				_spriteBatch.DrawString(_font, "Target: " + Camera.Target.ToString(), new Vector2(0, 60), Color.White);
				_spriteBatch.DrawString(_font, "Yaw: " + Camera.YawAngle, new Vector2(0, 90), Color.White);
				_spriteBatch.DrawString(_font, "Pitch: " + Camera.PitchAngle, new Vector2(0, 120), Color.White);
				_spriteBatch.DrawString(_font, "Water height: " + _waterHeight, new Vector2(0, 150), Color.White);
				_spriteBatch.DrawString(_font, "Ambient intensity: " + _ambientIntensity, new Vector2(0, 180), Color.White);
				_spriteBatch.DrawString(_font, "Diffuse light intensity: " + _diffuseIntensity, new Vector2(0, 210), Color.White);
				_spriteBatch.DrawString(_font, "Lightning: " + _enableLighting, new Vector2(0, 240), Color.White);
				_spriteBatch.DrawString(_font, "Water: " + _drawWater, new Vector2(0, 270), Color.White);
				_spriteBatch.DrawString(_font, "Refraction: " + _enableRefraction, new Vector2(0, 300), Color.White);
				_spriteBatch.DrawString(_font, "Reflection: " + _enableReflection, new Vector2(0, 330), Color.White);
				_spriteBatch.DrawString(_font, "Fresnel: " + _enableFresnel, new Vector2(0, 360), Color.White);
				_spriteBatch.DrawString(_font, "Waves: " + _enableWaves, new Vector2(0, 390), Color.White);
				_spriteBatch.DrawString(_font, "Specular: " + _enableSpecularLighting, new Vector2(0, 420), Color.White);
				_spriteBatch.DrawString(_font, "Skybox: " + _drawSkybox, new Vector2(0, 450), Color.White);

				_spriteBatch.End();
			}

			base.Draw(gameTime);
		}

		#region Draws

		private void DrawTerrain(Effect effect, Matrix viewMatrix)
		{
			effect.Parameters["Projection"].SetValue(_projectionMatrix);
			effect.Parameters["View"].SetValue(viewMatrix);
			effect.Parameters["World"].SetValue(Matrix.Identity);
			effect.Parameters["Texture"].SetValue(_terrainTexture);

			// Lighting
			effect.Parameters["EnableLighting"].SetValue(_enableLighting);

			// Ambient
			effect.Parameters["AmbientColor"].SetValue(_ambientColor);
			effect.Parameters["AmbientIntensity"].SetValue(_ambientIntensity);

			// Diffuse
			effect.Parameters["DiffuseLightDirection"].SetValue(_sunDirection);
			effect.Parameters["DiffuseColor"].SetValue(_diffuseColor);
			effect.Parameters["DiffuseIntensity"].SetValue(_diffuseIntensity);

			if (effect == _refractionEffect)
			{
				effect.Parameters["ClippingPlane"].SetValue(_refractionClippingPlane);
			}
			else if (effect == _reflectionEffect)
			{
				effect.Parameters["ClippingPlane"].SetValue(_reflectionClippingPlane);
			}

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				_device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _terrainVertices, 0, _terrainVertices.Length, _terrainIndices, 0, _terrainIndices.Length / 3, VertexPositionNormalTexture.VertexDeclaration);
			}
		}

		private void DrawWater()
		{
			var effect = _enableWaves ? this._waterEffect : _waterEffectWithoutWaves;
			effect.Parameters["Projection"].SetValue(_projectionMatrix);
			effect.Parameters["View"].SetValue(_viewMatrix);
			effect.Parameters["World"].SetValue(Matrix.CreateScale(256, 1, 256));

			effect.Parameters["TextureRefraction"].SetValue(_refractionTexture);

			effect.Parameters["TextureReflection"].SetValue(_reflectionTexture);
			effect.Parameters["ReflectionMatrix"].SetValue(_reflectionViewMatrix);

			effect.Parameters["WaterColor"].SetValue(_waterColor);

			effect.Parameters["EnableRefraction"].SetValue(_enableRefraction);
			effect.Parameters["EnableReflection"].SetValue(_enableReflection);
			effect.Parameters["EnableFresnel"].SetValue(_enableFresnel);
			effect.Parameters["EnableSpecularLighting"].SetValue(_enableSpecularLighting);
			effect.Parameters["RefractionReflectionMergeTerm"].SetValue(_refractionReflectionMergeTerm);

			effect.Parameters["WaveTextureScale"].SetValue(_waveTextureScale);

			if (_enableWaves)
			{
				effect.Parameters["TextureWaveNormalMap0"].SetValue(_waveNormalMap0);
				effect.Parameters["TextureWaveNormalMap1"].SetValue(_waveNormalMap1);
			}

			effect.Parameters["WaveMapOffset0"].SetValue(_waveNormalMapOffset0);
			effect.Parameters["WaveMapOffset1"].SetValue(_waveNormalMapOffset1);

			effect.Parameters["CameraPosition"].SetValue(Camera.Position);

			// Sun
			effect.Parameters["SunColor"].SetValue(_sunColor);
			effect.Parameters["SunDirection"].SetValue(_sunDirection);
			effect.Parameters["SunFactor"].SetValue(_sunFactor);
			effect.Parameters["SunPower"].SetValue(_sunPower);

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				_device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _waterVertices, 0, _waterVertices.Length, _waterIndices, 0, _waterIndices.Length / 3, VertexPositionTexture.VertexDeclaration);
			}
		}

		private void DrawRefractionMap()
		{
			_device.SetRenderTarget(_refractionRenderTarget);

			_device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);

			if (_drawSkybox)
				DrawSkybox(_viewMatrix, _projectionMatrix, Camera.Position);

			DrawTerrain(_refractionEffect, _viewMatrix);

			_device.SetRenderTarget(null);
			_refractionTexture = _refractionRenderTarget;

		}

		private void DrawReflectionMap()
		{
			_device.SetRenderTarget(_reflectionRenderTarget);

			_device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);

			if (_drawSkybox)
				DrawSkybox(_reflectionViewMatrix, _projectionMatrix, Camera.Position);

			DrawTerrain(_reflectionEffect, _reflectionViewMatrix);

			_device.SetRenderTarget(null);
			_reflectionTexture = _reflectionRenderTarget;
		}

		private void DrawSkybox(Matrix view, Matrix projection, Vector3 cameraPosition)
		{
			var device = GraphicsDevice;

			device.SetVertexBuffer(_skyboxCube.VertexBuffer);
			device.Indices = _skyboxCube.IndexBuffer;

			// Go through each pass in the effect, but we know there is only one...
			foreach (EffectPass pass in _skyboxEffect.CurrentTechnique.Passes)
			{
				Matrix skyboxWorld = Matrix.CreateScale(SkyboxSize) * Matrix.CreateTranslation(Camera.Position);

				var effect = _skyboxEffect;
				effect.Parameters["World"].SetValue(skyboxWorld);
				effect.Parameters["View"].SetValue(view);
				effect.Parameters["Projection"].SetValue(projection);
				effect.Parameters["SkyboxTexture"].SetValue(_skyboxTexture);
				effect.Parameters["CameraPosition"].SetValue(Camera.Position);

				pass.Apply();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
					0,
					_skyboxCube.VertexCount,
					0,
					_skyboxCube.PrimitiveCount);
			}
		}

		#endregion

		private void LoadHeightData(string heightMapFile)
		{
			var stream = new FileStream(heightMapFile, FileMode.Open, FileAccess.Read);
			var reader = new BinaryReader(stream);

			// Read terrain dimension from raw file
			_terrainSize = new Point(reader.ReadUInt16(), reader.ReadUInt16());
			int size = _terrainSize.X * _terrainSize.Y;
			_terrainHeights = new float[_terrainSize.X, _terrainSize.Y];

			// Read height data from raw file
			var data = reader.ReadBytes(size);

			int i = 0;
			for (int y = 0; y < _terrainSize.Y; y++)
			{
				for (int x = 0; x < _terrainSize.X; x++, i++)
				{
					_terrainHeights[x, y] = (TerrainMaxHeight * data[i]) / 255.0f;
				}
			}

			reader.Close();
			stream.Close();
		}

		private void SetUpVertices()
		{
			// Terrain
			_terrainVertices = new VertexPositionNormalTexture[_terrainSize.X * _terrainSize.Y];
			for (int x = 0; x < _terrainSize.X; x++)
			{
				for (int y = 0; y < _terrainSize.Y; y++)
				{
					int i = x + y * _terrainSize.X;
					_terrainVertices[i].Position = new Vector3(x, _terrainHeights[x, y], y);
					_terrainVertices[i].TextureCoordinate = new Vector2((x / (float)_terrainSize.X),
						1 - (y / (float)_terrainSize.Y));

					// Compute normals
					_terrainVertices[i].Normal = Vector3.Zero;

					float deltaHeight;
					if (x > 0)
					{
						if (x + 1 < _terrainSize.X)
						{
							deltaHeight = _terrainHeights[x - 1, y] - _terrainHeights[x + 1, y];
						}
						else
						{
							deltaHeight = _terrainHeights[x - 1, y] - _terrainHeights[x, y];
						}
					}
					else
						deltaHeight = _terrainHeights[x, y] - _terrainHeights[x + 1, y];

					var normalizedVector = new Vector3(0.0f, 1.0f, deltaHeight);
					normalizedVector.Normalize();
					_terrainVertices[i].Normal += normalizedVector;
					if (y > 0)
					{
						if (y + 1 < _terrainSize.Y)
							deltaHeight = _terrainHeights[x, y - 1] - _terrainHeights[x, y + 1];
						else
							deltaHeight = _terrainHeights[x, y - 1] - _terrainHeights[x, y];
					}
					else
					{
						deltaHeight = _terrainHeights[x, y] - _terrainHeights[x, y + 1];
					}

					normalizedVector = new Vector3(deltaHeight, 1.0f, 0.0f);
					normalizedVector.Normalize();
					_terrainVertices[i].Normal += normalizedVector;
					_terrainVertices[i].Normal.Normalize();
				}
			}

			// Water
			_waterVertices = new VertexPositionTexture[4];

			// Bottom left
			_waterVertices[0].Position = new Vector3(0, _waterHeight, 0);
			_waterVertices[0].TextureCoordinate = new Vector2(0, 0);

			// Top left
			_waterVertices[1].Position = new Vector3(0, _waterHeight, 1);
			_waterVertices[1].TextureCoordinate = new Vector2(0, 1);

			// Top right
			_waterVertices[2].Position = new Vector3(1, _waterHeight, 1);
			_waterVertices[2].TextureCoordinate = new Vector2(1, 1);

			// Bottom right
			_waterVertices[3].Position = new Vector3(1, _waterHeight, 0);
			_waterVertices[3].TextureCoordinate = new Vector2(1, 0);
		}

		private void SetUpIndices()
		{
			// Terrain
			_terrainIndices = new int[(_terrainSize.X - 1) * (_terrainSize.Y - 1) * 6];
			int counter = 0;
			for (int y = 0; y < _terrainSize.Y - 1; y++)
			{
				for (int x = 0; x < _terrainSize.X - 1; x++)
				{
					int lowerLeft = x + y * _terrainSize.X;
					int lowerRight = (x + 1) + y * _terrainSize.X;
					int topLeft = x + (y + 1) * _terrainSize.X;
					int topRight = (x + 1) + (y + 1) * _terrainSize.X;

					// First triangle
					_terrainIndices[counter++] = topLeft;
					_terrainIndices[counter++] = lowerRight;
					_terrainIndices[counter++] = lowerLeft;

					// Seconde triangle
					_terrainIndices[counter++] = topLeft;
					_terrainIndices[counter++] = topRight;
					_terrainIndices[counter++] = lowerRight;
				}
			}

			// Water
			_waterIndices = new int[6];

			_waterIndices[0] = 0;
			_waterIndices[1] = 1;
			_waterIndices[2] = 2;
			_waterIndices[3] = 2;
			_waterIndices[4] = 3;
			_waterIndices[5] = 0;
		}

		private void CreateRasterizerState(FillMode fillMode)
		{
			_rasterizerState = new RasterizerState()
			{
				CullMode = CullMode.CullClockwiseFace,
				FillMode = fillMode
			};
		}

		private Vector4 CreateClippingPlane(bool showUp)
		{
			var clippingPlane = new Vector4(0.0f, -1.0f, 0.0f, _waterHeight + 0.1f);

			return (showUp) ? -clippingPlane : clippingPlane;
		}
	}
}
