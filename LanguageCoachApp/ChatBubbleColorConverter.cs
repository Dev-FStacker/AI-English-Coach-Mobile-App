using Microsoft.Maui.Graphics;
using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace LanguageCoachApp
{
    public class ChatBubbleColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isUser)
            {
                return isUser ? Colors.LightBlue : Colors.LightGray;
            }
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
