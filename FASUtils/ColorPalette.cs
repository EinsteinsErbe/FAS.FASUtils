using System.Drawing;

namespace FASUtils
{
    public class ColorPalette
    {
        public static Color GOOD = Color.LightGreen;
        public static Color WARN = Color.Yellow;
        public static Color BAD = Color.Tomato; 

        public static Color FromBool(bool value)
        {
            return value ? GOOD : BAD;
        }

        public static Color FromBoolWarn(bool value)
        {
            return value ? GOOD : WARN;
        }

        public static Color FromBool(bool bad, bool warn)
        {
            return bad ? BAD : (warn ? WARN : GOOD);
        }
    }
}
