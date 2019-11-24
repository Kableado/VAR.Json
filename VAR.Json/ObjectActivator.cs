using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace VAR.Json
{
    public class ObjectActivator
    {
        private static readonly Dictionary<Type, Func<object>> _creators = new Dictionary<Type, Func<object>>();

        public static Func<object> GetLambdaNew(Type type)
        {
            if (_creators.ContainsKey(type))
            {
                return _creators[type];
            }

            lock (_creators)
            {
                if (_creators.ContainsKey(type))
                {
                    return _creators[type];
                }

                NewExpression newExp = Expression.New(type);
                LambdaExpression lambda = Expression.Lambda(typeof(Func<object>), newExp);
                Func<object> compiledLambdaNew = (Func<object>)lambda.Compile();

                _creators.Add(type, compiledLambdaNew);
                return _creators[type];
            }
        }

        public static object CreateInstance(Type type)
        {
            Func<object> creator = GetLambdaNew(type);
            return creator();
        }
    }
}
