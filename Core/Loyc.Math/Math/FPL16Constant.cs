using System;

namespace Loyc.Math
{
    public static class FPL16Constant
	{
		public static readonly FPL16 _0_001 = FPL16.Prescaled(66);
		public static readonly FPL16 _0_01 = FPL16.Prescaled(655);
		public static readonly FPL16 _0_02 = FPL16.Prescaled(1311);
		public static readonly FPL16 _0_05 = FPL16.Prescaled(3276);
		public static readonly FPL16 _0_1 = FPL16.Prescaled(6553);
		public static readonly FPL16 _0_15 = FPL16.Prescaled(9830);
		public static readonly FPL16 _0_2 = FPL16.Prescaled(13107);
		public static readonly FPL16 _0_3 = FPL16.Prescaled(19661);
		public static readonly FPL16 _0_4 = FPL16.Prescaled(26214);
		public static readonly FPL16 _0_5 = FPL16.Prescaled(32768);
		public static readonly FPL16 _0_55 = FPL16.Prescaled(36045);
		public static readonly FPL16 _0_6 = FPL16.Prescaled(39322);
		public static readonly FPL16 _0_7 = FPL16.Prescaled(45875);
		public static readonly FPL16 _0_75 = FPL16.Prescaled(49152);
		public static readonly FPL16 _0_8 = FPL16.Prescaled(52429);
		public static readonly FPL16 _0_9 = FPL16.Prescaled(58982);

		public static readonly FPL16 _1_1 = FPL16.Prescaled(72090);
		public static readonly FPL16 _1_2 = FPL16.Prescaled(78643);
		public static readonly FPL16 _1_25 = FPL16.Prescaled(81920);
		public static readonly FPL16 _1_3 = FPL16.Prescaled(85197);
		public static readonly FPL16 _1_4 = FPL16.Prescaled(91750);
		public static readonly FPL16 _1_414 = FPL16.Prescaled(92668);
		public static readonly FPL16 _1_5 = FPL16.Prescaled(98304);
		public static readonly FPL16 _1_6 = FPL16.Prescaled(104858);

		/// <summary>
		/// double: 3.1415926, fp: 3 + 9279/65536
		/// </summary>
		public static readonly FPL16 PI = FPL16.Prescaled(205887);
		public static readonly FPL16 PI2 = PI * 2L;
		public static readonly FPL16 TUO = PI / 2L;

		public static readonly FPL16 Deg2Rad = FPL16.Prescaled(1144);
		/// <summary>
		/// 0.0174533
		/// </summary>
		public static readonly FPL16 Deg2Rad100 = FPL16.Prescaled(114382);
		public static readonly FPL16 Rad2Deg = FPL16.Prescaled(3754936);

		/// <summary>
		/// 0.001
		/// </summary>
		public const long Interval_Prescaled = 66;

		/// <summary>
		/// double: 2.718281828, fp: 2 + 47073/65536
		/// </summary>
		public static readonly FPL16 E = FPL16.Prescaled(178145);
	}
}