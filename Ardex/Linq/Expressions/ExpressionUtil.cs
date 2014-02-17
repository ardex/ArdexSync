using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Ardex.Linq.Expressions
{
    public static class ExpressionUtil
    {
        public static MemberInfo GetMember<T, TMember>(Expression<Func<T, TMember>> expr)
        {
            var memberExpr = (MemberExpression)expr.Body;

            return memberExpr.Member;
        }

        public static PropertyInfo GetProperty<T, TProperty>(Expression<Func<T, TProperty>> expr)
        {
            var member = ExpressionUtil.GetMember(expr);
            var prop = member as PropertyInfo;

            if (prop == null)
            {
                throw new InvalidOperationException("Specified member is not a property.");
            }

            return prop;
        }
    }
}
