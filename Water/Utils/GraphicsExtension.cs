using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Water.Vertices;

namespace Water.Utils
{
	public static class GraphicsExtension
	{
		public static VertexBuffer CreateVertexBuffer<T>(this T[] vertices, GraphicsDevice device) where T : struct, IVertexType
		{
			var result = new VertexBuffer(device,
				new T().VertexDeclaration,
				vertices.Length,
				BufferUsage.None);
			result.SetData(vertices);

			return result;
		}

		public static IndexBuffer CreateIndexBuffer(this short[] indices, GraphicsDevice device)
		{
			var result = new IndexBuffer(device,
				IndexElementSize.SixteenBits,
				indices.Length,
				BufferUsage.None);
			result.SetData(indices);
			return result;
		}

		public static IEnumerable<Vector3> GetPositions(this VertexPositionNormalTexture[] vertices) => (from v in vertices select v.Position);
		public static IEnumerable<Vector3> GetPositions(this VertexPositionTexture[] vertices) => (from v in vertices select v.Position);
		public static IEnumerable<Vector3> GetPositions(this VertexPosition[] vertices) => (from v in vertices select v.Position);
	}
}