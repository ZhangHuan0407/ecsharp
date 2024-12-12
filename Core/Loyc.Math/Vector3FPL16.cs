using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Loyc.Math
{
	public struct Vector3FPL16
	{
		/// <summary>
		/// (0, 0, 0)
		/// </summary>
		public static readonly Vector3FPL16 Zero = new Vector3FPL16(FPL16.Zero, FPL16.Zero, FPL16.Zero);
		/// <summary>
		/// (-1, 0, 0)
		/// </summary>
		public static readonly Vector3FPL16 Left = new Vector3FPL16(-FPL16.One, FPL16.Zero, FPL16.Zero);
		/// <summary>
		/// (1, 0, 0)
		/// </summary>
		public static readonly Vector3FPL16 Right = new Vector3FPL16(FPL16.One, FPL16.Zero, FPL16.Zero);
		/// <summary>
		/// (0, 1, 0)
		/// </summary>
		public static readonly Vector3FPL16 Up = new Vector3FPL16(FPL16.Zero, FPL16.One, FPL16.Zero);
		/// <summary>
		/// (0, -1, 0)
		/// </summary>
		public static readonly Vector3FPL16 Down = new Vector3FPL16(FPL16.Zero, -FPL16.One, FPL16.Zero);
		/// <summary>
		/// (0, 0, 1)
		/// </summary>
		public static readonly Vector3FPL16 Forward = new Vector3FPL16(FPL16.Zero, FPL16.Zero, FPL16.One);
		/// <summary>
		/// (0, 0, -1)
		/// </summary>
		public static readonly Vector3FPL16 Back = new Vector3FPL16(FPL16.Zero, FPL16.Zero, -FPL16.One);

		private const long DotZeroOne_Prescaled = 655;
		private const long _10_Prescaled = 655360;

		public FPL16 x;
		public FPL16 y;
		public FPL16 z;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector3FPL16(FPL16 x, FPL16 y, FPL16 z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
		public static Vector3FPL16 Prescaled(long x, long y, long z)
		{
			Vector3FPL16 result;
			result.x.N = x;
			result.y.N = y;
			result.z.N = z;
			return result;
		}

		public FPL16 Magnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (x * x + y * y + z * z).Sqrt();
		}
		public FPL16 SqrMagnitude
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => x * x + y * y + z * z;
		}

		public Vector3FPL16 Normalized
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				Vector3FPL16 result = this;
				result.Normalize();
				return result;
			}
		}

		public Vector3 ToVector3
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector3((float)x, (float)y, (float)z);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Abs()
		{
			x.N = x.N < 0 ? -x.N : x.N;
			y.N = y.N < 0 ? -y.N : y.N;
			z.N = z.N < 0 ? -z.N : z.N;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Normalize()
		{
			long sumAbs = x.Abs().N + y.Abs().N + z.Abs().N;
			if (sumAbs > _10_Prescaled)
			{
				FPL16 magnitude = Magnitude;
				this /= magnitude;
			}
			else if (sumAbs > DotZeroOne_Prescaled)
			{
				long sqr = x.N * x.N + y.N * y.N + z.N * z.N;
				x.N = (x.N << 16) / sqr;
				y.N = (y.N << 16) / sqr;
				z.N = (z.N << 16) / sqr;
			}
			else
				this = Zero;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Vector3FPL16 left, Vector3FPL16 right) => left.x.N == right.x.N && left.y.N == right.y.N && left.z.N == right.z.N;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Vector3FPL16 left, Vector3FPL16 right) => left.x.N != right.x.N || left.y.N != right.y.N || left.z.N != right.z.N;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 operator +(Vector3FPL16 left, Vector3FPL16 right) => Prescaled(left.x.N + right.x.N, left.y.N + right.y.N, left.z.N + right.z.N);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 operator -(Vector3FPL16 left, Vector3FPL16 right) => Prescaled(left.x.N - right.x.N, left.y.N - right.y.N, left.z.N - right.z.N);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 operator *(Vector3FPL16 a, FPL16 d) => new Vector3FPL16(a.x * d, a.y * d, a.z * d);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 operator *(FPL16 d, Vector3FPL16 a) => new Vector3FPL16(a.x * d, a.y * d, a.z * d);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 operator /(Vector3FPL16 a, FPL16 d) => new Vector3FPL16(a.x / d, a.y / d, a.z / d);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vector3FPL16(Vector2FPL16 vector2FPL16) => new Vector3FPL16(vector2FPL16.x, vector2FPL16.y, FPL16.Zero);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Vector2FPL16(Vector3FPL16 vector3FPL16) => new Vector2FPL16(vector3FPL16.x, vector3FPL16.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FPL16 Dot(Vector3FPL16 lhs, Vector3FPL16 rhs) => lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3FPL16 Cross(Vector3FPL16 lhs, Vector3FPL16 rhs) => new Vector3FPL16(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
	}
}