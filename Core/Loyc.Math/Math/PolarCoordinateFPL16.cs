using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Loyc.Math
{
	[Serializable]
	public struct PolarCoordinateFPL16
	{
		public static readonly PolarCoordinateFPL16 Zero = new PolarCoordinateFPL16(FPL16.Zero, FPL16.Zero);


		[SerializeField]
		public FPL16 Radian;
		[SerializeField]
		public FPL16 Length;

		public PolarCoordinateFPL16(FPL16 radian, FPL16 length)
		{
			Radian = radian;
			Length = length;
		}

		public Vector2FPL16 ToVector2(Vector2FPL16 centerPosition)
		{
			Vector2FPL16 vector2 = centerPosition;
			vector2.x += Length * MathFPL16.Value.Cos(Radian);
			vector2.y += Length * MathFPL16.Value.Sin(Radian);
			return vector2;
		}

		/// <summary>
		/// 返回两个极坐标点的角度差额
		/// </summary>
		/// <returns>[-π,π]</returns>
		public FPL16 MinusRandian(PolarCoordinateFPL16 another)
		{
			FPL16 delta = Radian - another.Radian;
			if (delta > FPL16.Zero)
				return (delta + FPL16TriangleFunctionExtension.PI) % FPL16TriangleFunctionExtension.PI2 - FPL16TriangleFunctionExtension.PI;
			else if (delta < FPL16.Zero)
				return (delta - FPL16TriangleFunctionExtension.PI) % FPL16TriangleFunctionExtension.PI2 + FPL16TriangleFunctionExtension.PI;
			else
				return FPL16.Zero;
		}

		public override string ToString() => $"Radian:{Radian},Length:{Length}";

		public static PolarCoordinateFPL16 FromCenterToPoint(Vector2FPL16 centerPosition, Vector2FPL16 position)
		{
			Vector2FPL16 delta = centerPosition - position;
			FPL16 length = delta.Magnitude;
			if (length.N < FPL16TriangleFunctionExtension.Interval_Prescaled)
				return PolarCoordinateFPL16.Zero;
			FPL16 radian = MathFPL16.Value.ArcTan2(delta.y, delta.x);
			return new PolarCoordinateFPL16(radian, length);
		}
	}
}