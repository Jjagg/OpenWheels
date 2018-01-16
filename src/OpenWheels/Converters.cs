#if NETSTANDARD2_0
using System;
using System.ComponentModel;
using System.Globalization;

namespace OpenWheels
{
    public class ColorConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
             if (destinationType == typeof(string))
                return true;

            return base.CanConvertFrom(context, destinationType);
            
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var color = (Color) value;
                return '#' + color.Packed.ToString("X");
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                var packed = uint.Parse(s, NumberStyles.HexNumber);
                return new Color(packed);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class RectangleConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
             if (destinationType == typeof(string))
                return true;

            return base.CanConvertFrom(context, destinationType);
            
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var rect = (Rectangle) value;
                return $"{rect.X} {rect.Y} {rect.Width} {rect.Height}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                var pieces = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                return new Rectangle(int.Parse(pieces[0]), int.Parse(pieces[1]), int.Parse(pieces[2]), int.Parse(pieces[3]));
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class RectangleFConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
             if (destinationType == typeof(string))
                return true;

            return base.CanConvertFrom(context, destinationType);
            
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var rect = (RectangleF) value;
                return $"{rect.X:R} {rect.Y:R} {rect.Width:R} {rect.Height:R}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                var pieces = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                return new RectangleF(float.Parse(pieces[0]), float.Parse(pieces[1]), float.Parse(pieces[2]), float.Parse(pieces[3]));
            }

            return base.ConvertFrom(context, culture, value);
        }
    }

    public class Point2Converter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
             if (destinationType == typeof(string))
                return true;

            return base.CanConvertFrom(context, destinationType);
            
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                var pt = (Point2) value;
                return $"{pt.X} {pt.Y}";
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string s)
            {
                var pieces = s.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                return new Point2(int.Parse(pieces[0]), int.Parse(pieces[1]));
            }

            return base.ConvertFrom(context, culture, value);
        }
    }


}
#endif