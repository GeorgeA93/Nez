using System;
using Microsoft.Xna.Framework;
using Nez.PhysicsShapes;


namespace Nez
{
	/// <summary>
	/// Polygons should be defined in clockwise fashion.
	/// </summary>
	public class PolygonCollider : Collider
	{
		/// <summary>
		/// If the points are not centered they will be centered with the difference being applied to the localOffset.
		/// </summary>
		/// <param name="points">Points.</param>
		public PolygonCollider(Vector2[] points)
		{
			SetPoints(points);
		}

		public PolygonCollider(int vertCount, float radius)
		{
			Shape = new Polygon(vertCount, radius);
		}

		public PolygonCollider() : this(6, 40)
		{
		}

		public void SetPoints(Vector2[] points)
		{
			if(Enabled)
				UnregisterColliderWithPhysicsSystem();

			// first and last point must not be the same. we want an open polygon
			var isPolygonClosed = points[0] == points[points.Length - 1];

			if (isPolygonClosed)
				Array.Resize(ref points, points.Length - 1);

			if (Shape == null)
			{
				Shape = new Polygon(points);
			}
			else
			{
				((Polygon)Shape).SetPoints(points);
			}

			_isPositionDirty = true;
			if(Enabled)
				RegisterColliderWithPhysicsSystem();
		}

		public void FlipHorizontally()
		{
			var points = ((Polygon)Shape).Points;
			var newPoints = new Vector2[points.Length];

			for (var i = 0; i < points.Length; i ++)
			{
				var point = points[i];
				var newPoint = new Vector2(2 * 0 - point.X, point.Y);
				newPoints[i] = newPoint;
			}
			
			SetPoints(newPoints);
		}

		public void FlipVertically()
		{
			var points = ((Polygon)Shape).Points;
			var newPoints = new Vector2[points.Length];

			for (var i = 0; i < points.Length; i ++)
			{
				var point = points[i];
				var newPoint = new Vector2(point.X, 2 * 0 - point.Y);
				newPoints[i] = newPoint;
			}
			
			SetPoints(newPoints);
		}

		public override void DebugRender(Batcher batcher)
		{
			var poly = Shape as Polygon;
			batcher.DrawHollowRect(Bounds, Debug.Colors.ColliderBounds, Debug.Size.LineSizeMultiplier);
			batcher.DrawPolygon(Shape.Position, poly.Points, Debug.Colors.ColliderEdge, true,
				Debug.Size.LineSizeMultiplier);
			batcher.DrawPixel(Entity.Transform.Position, Debug.Colors.ColliderPosition,
				4 * Debug.Size.LineSizeMultiplier);
			batcher.DrawPixel(Shape.Position, Debug.Colors.ColliderCenter, 2 * Debug.Size.LineSizeMultiplier);

			// Normal debug code
			//for( var i = 0; i < poly.points.Length; i++ )
			//{
			//	Vector2 p2;
			//	var p1 = poly.points[i];
			//	if( i + 1 >= poly.points.Length )
			//		p2 = poly.points[0];
			//	else
			//		p2 = poly.points[i + 1];
			//	var perp = Vector2Ext.perpendicular( ref p1, ref p2 );
			//	Vector2Ext.normalize( ref perp );
			//	var mp = Vector2.Lerp( p1, p2, 0.5f ) + poly.position;
			//	batcher.drawLine( mp, mp + perp * 10, Color.White );
			//}
		}
	}
}