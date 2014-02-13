using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ardex.Util
{
    /// <summary>
    /// Common text methods.
    /// </summary>
    public static class Text
    {
        /// <summary>
        /// Strips text in brackets, trims spaces
        /// at the start and end of the name and
        /// leaves the rest of the string intact.
        /// </summary>
        public static string StripNameStrict(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            
            var rebuiltNameChars = Text.StripNameStrictYield(name).ToArray();
            var rebuiltName = new string(rebuiltNameChars);
            
            return rebuiltName.Trim(' ');
        }
        
        /// <summary>
        /// Yields all legal characters outside
        /// of brackets and strips out duplicate
        /// spaces in the middle of the string.
        /// </summary>
        private static IEnumerable<char> StripNameStrictYield(string name)
        {
            var openBracketCount = 0;
            var spaceCount = 0;
            
            foreach (var c in name)
            {
                if (c == '(')
				{
                    openBracketCount++;
				}
                else if (c == ')')
				{
                    openBracketCount--;
				}
                else if (openBracketCount == 0 && c != '\'')
                {
                    if (c == ' ')
                    {
                        spaceCount++;
                        
                        // Strip out any unnecessary spaces.
                        if (spaceCount < 2)
                            yield return c;
                    }
                    else
                    {
                        spaceCount = 0;
                        
                        yield return c;
                    }
                }
            }
        }

        /// <summary>
        /// Joins the non empty strings with the given separator between them.
        /// </summary>
        public static string JoinNonEmpty(string separator, params string[] values)
        {
            var clean = true;
            var sb = new StringBuilder();

            foreach (var v in values)
            {
                if (!string.IsNullOrEmpty(v))
                {
                    if (!clean)
                        sb.Append(separator);

                    sb.Append(v);

                    clean = false;
                }
            }

            return sb.ToString();
        }
    }
}

