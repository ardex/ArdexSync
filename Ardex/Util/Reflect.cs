using System.Text;

namespace Ardex
{
    public static class Reflect
    {
        public static string ToString(object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties();
            var sb = new StringBuilder();

            sb.Append(type.Name);
            sb.Append(" { ");

            for (var i = 0; i < properties.Length; i++)
            {
                var prop = properties[i];

                sb.Append(prop.Name);
                sb.Append(" = ");
                sb.Append(prop.GetValue(obj, null));
                
                if (i != properties.Length - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.Append(" }");

            return sb.ToString();
        }

        public static bool Equals<T>(T x, T y)
        {
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) return true;
            if (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null)) return false;

            foreach (var prop in typeof(T).GetProperties())
            {
                if (prop.CanRead)
                {
                    var valueX = prop.GetValue(x, null);
                    var valueY = prop.GetValue(y, null);

                    if (!object.Equals(valueX, valueY))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
