using System;
using SixLabors.Fonts;

namespace OpenWheels.Fonts
{
    /// <summary>
    /// Contains information on a specific font.
    /// </summary>
    public struct FontData : IEquatable<FontData>
    {
        /// <summary>
        /// Name of the family of the font.
        /// </summary>
        public string FamilyName { get; }

        /// <summary>
        /// Size of the font.
        /// </summary>
        public float Size { get; }

        /// <summary>
        /// Style of the font.
        /// </summary>
        public FontStyle Style { get; }

        /// <summary>
        /// Create a new <see cref="FontData"/> instance.
        /// </summary>
        /// <param name="familyName">Name of the family of the font.</param>
        /// <param name="size">Size of the font.</param>
        /// <param name="style">Style of the font.</param>
        public FontData(string familyName, float size, FontStyle style)
        {
            FamilyName = familyName;
            Size = size;
            Style = style;
        }

        public bool Equals(FontData other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(FamilyName, other.FamilyName) && Size == other.Size && Style == other.Style;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FontData) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = FamilyName.GetHashCode();
                hashCode = (hashCode * 397) ^ Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) Style;
                return hashCode;
            }
        }

        public static bool operator ==(FontData left, FontData right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(FontData left, FontData right)
        {
            return !Equals(left, right);
        }
    }
}