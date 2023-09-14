using System;
using System.IO;
using AssetManagementBase;
using DdsKtxSharp;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static DdsKtxSharp.DdsKtx;

namespace Water.FNA.Core
{
	public static class XNAssetsExt
	{
		private class FontSystemLoadingSettings : IAssetSettings
		{
			public Texture2D ExistingTexture { get; set; }
			public Rectangle ExistingTextureUsedSpace { get; set; }
			public string[] AdditionalFonts { get; set; }

			public string BuildKey() => string.Empty;
		}

		private static AssetLoader<FontSystem> _fontSystemLoader = (manager, assetName, settings, tag) =>
		{
			var fontSystemSettings = new FontSystemSettings();

			var fontSystemLoadingSettings = (FontSystemLoadingSettings)settings;
			if (fontSystemLoadingSettings != null)
			{
				fontSystemSettings.ExistingTexture = fontSystemLoadingSettings.ExistingTexture;
				fontSystemSettings.ExistingTextureUsedSpace = fontSystemLoadingSettings.ExistingTextureUsedSpace;
			};

			var fontSystem = new FontSystem(fontSystemSettings);
			var data = manager.ReadAssetAsByteArray(assetName);
			fontSystem.AddFont(data);
			if (fontSystemLoadingSettings != null && fontSystemLoadingSettings.AdditionalFonts != null)
			{
				foreach (var file in fontSystemLoadingSettings.AdditionalFonts)
				{
					data = manager.LoadByteArray(file, false);
					fontSystem.AddFont(data);
				}
			}

			return fontSystem;
		};

		private static AssetLoader<StaticSpriteFont> _staticFontLoader = (manager, assetName, settings, tag) =>
		{
			var fontData = manager.ReadAssetAsString(assetName);
			var graphicsDevice = (GraphicsDevice)tag;

			return StaticSpriteFont.FromBMFont(fontData,
						name =>
						{
							var imageData = manager.LoadByteArray(name, false);
							return new MemoryStream(imageData);
						},
						graphicsDevice);
		};

		private static SurfaceFormat ToSurfaceFormat(ddsktx_format src)
		{
			SurfaceFormat format = SurfaceFormat.Color;

			switch (src)
			{
				case ddsktx_format.DDSKTX_FORMAT_BC1:
					format = SurfaceFormat.Dxt1;
					break;

				case ddsktx_format.DDSKTX_FORMAT_BC2:
					format = SurfaceFormat.Dxt3;
					break;

				case ddsktx_format.DDSKTX_FORMAT_BC3:
					format = SurfaceFormat.Dxt5;
					break;
			}

			return format;
		}

		private static byte[] LoadFace(DdsKtxParser parser, int faceIndex)
		{
			ddsktx_texture_info info = parser.Info;

			ddsktx_sub_data sub_data;
			var imageData = parser.GetSubData(0, faceIndex, 0, out sub_data);

			switch (info.format)
			{
				case ddsktx_format.DDSKTX_FORMAT_BGRA8:
					// Switch B and R
					for (var i = 0; i < imageData.Length / 4; ++i)
					{
						var temp = imageData[i * 4];
						imageData[i * 4] = imageData[i * 4 + 2];
						imageData[i * 4 + 2] = temp;
						imageData[i * 4 + 3] = 255;
					}

					break;

				case ddsktx_format.DDSKTX_FORMAT_RGB8:
					// Add alpha channel
					var newImageData = new byte[info.width * info.height * 4];
					for (var i = 0; i < newImageData.Length / 4; ++i)
					{
						newImageData[i * 4] = imageData[i * 3 + 2];
						newImageData[i * 4 + 1] = imageData[i * 3 + 1];
						newImageData[i * 4 + 2] = imageData[i * 3];
						newImageData[i * 4 + 3] = 255;
					}

					imageData = newImageData;
					break;

				default:
					throw new Exception("Format " + info.format.ToString() + "isn't supported.");
			}

			return imageData;
		}

		private static AssetLoader<Texture2D> _ddsLoader = (manager, assetName, settings, tag) =>
		{
			DdsKtxParser parser = DdsKtxParser.FromMemory(manager.ReadAssetAsByteArray(assetName));
			ddsktx_texture_info info = parser.Info;

			var format = ToSurfaceFormat(info.format);
			var imageData = LoadFace(parser, 0);

			var graphicsDevice = (GraphicsDevice)tag;
			Texture2D texture = new Texture2D(graphicsDevice, info.width, info.height, false, format);
			texture.SetData(imageData);

			return texture;
		};

		private static AssetLoader<TextureCube> _ddsCubeLoader = (manager, assetName, settings, tag) =>
		{
			DdsKtxParser parser = DdsKtxParser.FromMemory(manager.ReadAssetAsByteArray(assetName));
			ddsktx_texture_info info = parser.Info;

			var graphicsDevice = (GraphicsDevice)tag;
			var format = ToSurfaceFormat(info.format);

			var texture = new TextureCube(graphicsDevice, info.width, false, format);
			texture.SetData(CubeMapFace.PositiveX, LoadFace(parser, 0));
			texture.SetData(CubeMapFace.NegativeX, LoadFace(parser, 1));
			texture.SetData(CubeMapFace.PositiveY, LoadFace(parser, 2));
			texture.SetData(CubeMapFace.NegativeY, LoadFace(parser, 3));
			texture.SetData(CubeMapFace.PositiveZ, LoadFace(parser, 4));
			texture.SetData(CubeMapFace.NegativeZ, LoadFace(parser, 5));

			return texture;
		};

		public static FontSystem LoadFontSystem(this AssetManager assetManager, string assetName, string[] additionalFonts = null, Texture2D existingTexture = null, Rectangle existingTextureUsedSpace = default(Rectangle))
		{
			FontSystemLoadingSettings settings = null;
			if (additionalFonts != null || existingTexture != null)
			{
				settings = new FontSystemLoadingSettings
				{
					AdditionalFonts = additionalFonts,
					ExistingTexture = existingTexture,
					ExistingTextureUsedSpace = existingTextureUsedSpace
				};
			}

			return assetManager.UseLoader(_fontSystemLoader, assetName, settings);
		}

		public static StaticSpriteFont LoadStaticSpriteFont(this AssetManager assetManager, GraphicsDevice graphicsDevice, string assetName)
		{
			return assetManager.UseLoader(_staticFontLoader, assetName, tag: graphicsDevice);
		}

		public static Texture2D LoadDds(this AssetManager assetManager, GraphicsDevice graphicsDevice, string assetName)
		{
			return assetManager.UseLoader(_ddsLoader, assetName, tag: graphicsDevice);
		}

		public static TextureCube LoadDdsCube(this AssetManager assetManager, GraphicsDevice graphicsDevice, string assetName)
		{
			return assetManager.UseLoader(_ddsCubeLoader, assetName, tag: graphicsDevice);
		}
	}
}