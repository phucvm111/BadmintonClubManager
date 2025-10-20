using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using BadmintonClub.Models;

namespace BadmintonClub.Converters
{
    /// <summary>
    /// Converter để hiển thị màu cho đội thắng/thua trong bracket
    /// </summary>
    public class MatchResultConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return Brushes.Transparent;

            var match = values[0] as Match;
            var teamId = values[1] as int?;
            var isTeamA = values[2] as bool?;

            if (match == null || !teamId.HasValue || !isTeamA.HasValue)
                return Brushes.Transparent;

            // Nếu trận chưa hoàn thành
            if (!match.Completed || !match.WinnerTeamId.HasValue)
                return Brushes.Transparent;

            // Kiểm tra Vô địch (Round 0 - Chung kết)
            if (match.Round == 0 && match.WinnerTeamId == teamId)
                return new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Vàng Gold

            // Kiểm tra Á quân (Round 0 nhưng thua)
            if (match.Round == 0 && match.WinnerTeamId != teamId)
                return new SolidColorBrush(Color.FromRgb(192, 192, 192)); // Bạc Silver

            // Đội thắng - màu xanh lá nhạt
            if (match.WinnerTeamId == teamId)
                return new SolidColorBrush(Color.FromRgb(200, 230, 201)); // #C8E6C9

            // Đội thua - màu đỏ nhạt
            return new SolidColorBrush(Color.FromRgb(255, 205, 210)); // #FFCDD2
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
