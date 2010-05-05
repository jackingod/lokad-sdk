namespace Lokad.Forecasting.Client
{
	/// <summary>Helper for Forecasting API v3.</summary>
	public static class Constants
	{
		/// <summary>Root namespace.</summary>
		public const string Namespace = "http://schemas.lokad.com/";

		/// <summary>Compound methods of Forecasting API are nearly
		/// all upper bounded to collection of 100 items at most.</summary>
		public const int SeriesSliceLength = 100;
	}
}