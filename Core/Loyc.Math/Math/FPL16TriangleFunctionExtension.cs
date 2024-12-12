using System;
using System.Runtime.CompilerServices;

namespace Loyc.Math
{
    public static class FPL16TriangleFunctionExtension
    {
		private const long PI_Prescaled = 205887;
		internal static readonly FPL16 PI = FPL16.Prescaled(205887);
		internal static readonly FPL16 PI2 = PI * 2L;
		private static readonly FPL16 TUO = PI / 2L;

		/// <summary>
		/// double: 2.718281828, fp: 2 + 47073/65536
		/// </summary>
		private static readonly FPL16 E = FPL16.Prescaled(178145);
		/// <summary>
		/// 0.001
		/// </summary>
		internal const long Interval_Prescaled = 66;
        private static readonly long[] SinFactorialTable;
        private static readonly long[] ArcSinFactorialTable;

		private static readonly FPL16 _0_94 = FPL16.Prescaled(61604);
		private static readonly FPL16 _0_93 = FPL16.Prescaled(60948);
		private static readonly FPL16 _0_025 = FPL16.Prescaled(1638);
		private static readonly FPL16 _0_86 = FPL16.Prescaled(56361);
        private static readonly FPL16 _0_023 = FPL16.Prescaled(1507);

        static FPL16TriangleFunctionExtension()
        {
            SinFactorialTable = new long[5];
            long Factorial = 1;
            for (int i = 0; i < SinFactorialTable.Length; i++)
            {
                Factorial *= (i * 2 + 2) * (i * 2 + 3);
                SinFactorialTable[i] = Factorial;
                if (i % 2 == 0)
                    SinFactorialTable[i] = -SinFactorialTable[i];
            }

            ArcSinFactorialTable = new long[5];
            for (int i = 0; i < ArcSinFactorialTable.Length; i++)
            {
                long numerator = 1;
                for (int j = 0; j < i; j++)
                    numerator *= 2 * j + 3;
                long denominator = 1;
                for (int j = 0; j < i + 1; j++)
                    denominator *= 2 * (j + 1);
                denominator *= 2 * i + 3;
                ArcSinFactorialTable[i] = (denominator << 16) / numerator;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Sin(this MathFPL16 math, FPL16 round)
        {
            // round 先约分到一个周期内，这样加快运算速度且后续直接long乘法不溢出
            long roundLong = round.N;
            if (roundLong > PI_Prescaled)
                roundLong -= PI_Prescaled * 2;
            else if (roundLong < -PI_Prescaled)
                roundLong += PI_Prescaled * 2;
            round.N = roundLong;

            int circleTimes = 0;
            long delta;
            FPL16 p = round;
            FPL16 taylorExpansion = round;
            FPL16 roundSquare = FPL16.Prescaled((round.N * round.N) >> 16);
            do
            {
                p.N = (p.N * roundSquare.N) >> 16;
                delta = p.N / SinFactorialTable[circleTimes];
                taylorExpansion.N += delta;
            } while (++circleTimes < 5 && (delta > 0 ? delta : -delta) > Interval_Prescaled);
            return taylorExpansion;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Cos(this MathFPL16 math, FPL16 round)
        {
            round.N = TUO.N - round.N;
            return math.Sin(round);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Tan(this MathFPL16 math, FPL16 round)
        {
            return math.Sin(round) / math.Cos(round);
        }

		/// <summary>
		/// [-Π/2, Π/2]
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 ArcSin(this MathFPL16 math, FPL16 value)
        {
            bool minus = value.N < 0;
            if (value == FPL16.MinValue)
                value = FPL16.MaxValue;
            else if (minus)
                value.N = -value.N;
            int circleTimes = 0;
            FPL16 p = value;
            FPL16 taylorExpansion = value;
            FPL16 square = FPL16.Prescaled((value.N * value.N) >> 16);
            long delta;
            do
            {
                p.N = (p.N * square.N) >> 16;
                delta = (p.N << 16) / ArcSinFactorialTable[circleTimes];
                taylorExpansion.N += delta;
            } while (++circleTimes < 5 && (delta > 0 ? delta : -delta) > Interval_Prescaled);

            if (value.N > _0_93.N)
			{
				long deltaN = value.N - _0_94.N;
				taylorExpansion.N += deltaN * deltaN / 1630 + _0_025.N;
				if (taylorExpansion.N > TUO.N)
					taylorExpansion.N = TUO.N;
			}
            else if (value.N > _0_86.N)
            {
                taylorExpansion.N += (value.N - _0_86.N) * _0_023.N / (_0_93.N - _0_86.N);
            }
            if (minus)
                taylorExpansion.N = -taylorExpansion.N;
            return taylorExpansion;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 ArcCos(this MathFPL16 math, FPL16 value)
        {
            FPL16 result = math.ArcSin(value);
            result.N = TUO.N - result.N;
            return result;
        }

		/// <summary>
		/// [-Π, Π]
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 ArcTan2(this MathFPL16 math, FPL16 y, FPL16 x)
        {
            bool smallX = x.N > -Interval_Prescaled && x.N < Interval_Prescaled;
            if (smallX)
            {
                if (y.N == 0)
                    return FPL16.Zero;
                else if (y.N > 0)
                    return TUO;
                else
                    return -TUO;
            }

            FPL16 q = FPL16.Zero;
            q.N = (y.N * y.N + x.N * x.N) >> 16;
            FPL16 sin = math.Div(y, math.Sqrt(q));
			FPL16 radius = ArcSin(math, sin);
			if (x < FPL16.Zero)
			{
				if (y > FPL16.Zero)
					radius = PI - radius;
				else
					radius = -PI - radius;
			}
			return radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Ln(this MathFPL16 math, FPL16 value, ushort error = 328)
        {
            if (value.N < 0)
                throw new ArgumentException(value.ToString());
            else if (value.N == 0)
                return FPL16.MinValue;
            // y 永远在[-1, 1]
            FPL16 y = (value - FPL16.One) / (value + FPL16.One);
            FPL16 taylorExpansion = FPL16.One;
            FPL16 roundSquare;
            roundSquare = FPL16.Prescaled((y.N * y.N) >> 2); // 16 + 16 - 2

            FPL16 s = FPL16.Prescaled(1L << 30);
            for (int i = 0; i < 50; i++)
            {
                s.N = (s.N * roundSquare.N) >> 30; // 30 + 30 - 30
                long delta = (s.N / (i + i + 3)) >> 14; // 30 - 0 - 14
                taylorExpansion.N += delta;
                if (delta <= error)
                    break;
            }
            return taylorExpansion * FPL16.Prescaled(y.N + y.N);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Clamp01(this MathFPL16 math, FPL16 value) => Clamp(math, value, FPL16.Zero, FPL16.One);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPL16 Clamp(this MathFPL16 math, FPL16 value, FPL16 min, FPL16 max)
        {
            if (min.N > max.N)
                throw new ArgumentException($"min: {min}, max: {max}, min > max");
            if (value.N < min.N)
                return min;
            else if (value.N > max.N)
                return max;
            else
                return value;
        }
    }
}