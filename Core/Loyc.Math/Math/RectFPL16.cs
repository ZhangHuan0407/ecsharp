using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Loyc.Math
{
	public struct RectFPL16
	{
		public static RectFPL16 Zero => new RectFPL16(FPL16.Zero, FPL16.Zero, FPL16.Zero, FPL16.Zero);

		public FPL16 XMin;
		public FPL16 XMax;
		public FPL16 YMin;
		public FPL16 YMax;

		public Vector2FPL16 Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2FPL16(XMax - XMin, YMax - YMin);
		}

		public Vector2FPL16 Min
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2FPL16(XMin, YMin);
		}

		public Vector2FPL16 Max
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2FPL16(XMax, YMax);
		}

		public Rect ToRect
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Rect(Min.ToVector2, Size.ToVector2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public RectFPL16(FPL16 xMin, FPL16 xMax, FPL16 yMin, FPL16 yMax)
		{
			XMin = xMin;
			XMax = xMax;
			YMin = yMin;
			YMax = yMax;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public RectFPL16(Vector2FPL16 min, Vector2FPL16 size)
		{
			XMin = min.x;
			YMin = min.y;
			XMax = min.x + size.x;
			YMax = min.y + size.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2FPL16 point) => point.x >= XMin && point.x < XMax && point.y >= YMin && point.y < YMax;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Overlaps(RectFPL16 other) => other.XMax > XMin && other.XMin < XMax && other.YMax> YMin && other.YMin < YMax;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2FPL16 ClampPoint(Vector2FPL16 point)
		{
            if (point.x < XMin)
				point.x = XMin;
			else if (point.x > XMax)
				point.x = XMax;
            if (point.y < YMin)
                point.y = YMin;
			else if (point.y > YMax)
				point.y = YMax;
			return point;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClampPoint(ref Vector2FPL16 point)
		{
			if (point.x < XMin)
				point.x = XMin;
			else if (point.x > XMax)
				point.x = XMax;
			if (point.y < YMin)
				point.y = YMin;
			else if (point.y > YMax)
				point.y = YMax;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Extend(FPL16 size)
		{
			XMin -= size;
			YMin -= size;
			XMax += size;
			YMax += size;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(RectFPL16 lhs, RectFPL16 rhs)
		{
			return lhs.XMin.N != rhs.XMin.N ||
				   lhs.YMin.N != rhs.YMin.N ||
				   lhs.XMax.N != rhs.XMax.N ||
				   lhs.YMax.N != rhs.YMax.N;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(RectFPL16 lhs, RectFPL16 rhs)
		{
			return lhs.XMin.N == rhs.XMin.N &&
				   lhs.YMin.N == rhs.YMin.N &&
				   lhs.XMax.N == rhs.XMax.N &&
				   lhs.YMax.N == rhs.YMax.N;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
        {
			return XMin.N.GetHashCode() ^ (XMax.N.GetHashCode() << 4) ^ (YMin.N.GetHashCode() >> 6) ^ (YMax.N.GetHashCode() >> 2);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object other)
		{
			if (!(other is RectFPL16 rect))
				return false;

			return this == rect;
		}

		public override string ToString() => $"Min: {Min}, Size: {Size}";

	}
}