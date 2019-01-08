using System;
using Xunit;

namespace OpenWheels.Tests
{
    public class ColorTest
    {
        public static TheoryData<Color> Colors = new TheoryData<Color>
        {
            Color.Red,
            new Color(0, 0, 0, 0),
            new Color(255, 255, 255),
            new Color(182, 238, 42f),
            new Color(23, 51, 112),
        };

        [Theory]
        [MemberData(nameof(Colors))]
        public void RgbHsvRgb(Color c)
        {
            var hsv = c.ToHsv();
            var rgb = hsv.ToRgb();
            Assert.Equal(c.R, rgb.R);
            Assert.Equal(c.G, rgb.G);
            Assert.Equal(c.B, rgb.B);
        }
    }
}
