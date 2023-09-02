using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Toolkit.Mvvm.Messaging.Internals;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public static class IMessengerExtensions
    {
        private static class MethodInfos
        {
            public static readonly MethodInfo RegisterIRecipient = new Action<IMessenger, IRecipient<object>, Unit>(Register).Method.GetGenericMethodDefinition();
        }

        private static class DiscoveredRecipients
        {
            public static readonly ConditionalWeakTable<Type, Action<IMessenger, object>?> RegistrationMethods = new();
        }

        private static class DiscoveredRecipients<TToken> where TToken : IEquatable<TToken>
        {
            public static readonly ConditionalWeakTable<Type, Action<IMessenger, object, TToken>> RegistrationMethods = new();
        }

        public static bool IsRegistered<TMessage>(this IMessenger messenger, object recipient) where TMessage : class
        {
            return messenger.IsRegistered<TMessage, Unit>(recipient, default);
        }

        public static void RegisterAll(this IMessenger messenger, object recipient)
        {
            Action<IMessenger, object> value = DiscoveredRecipients.RegistrationMethods.GetValue(recipient.GetType(), (Type t) => LoadRegistrationMethodsForType(t));
            if (value != null)
            {
                value(messenger, recipient);
            }
            else
            {
                messenger.RegisterAll(recipient, default(Unit));
            }
            static Action<IMessenger, object>? LoadRegistrationMethodsForType(Type recipientType)
            {
                Type type = recipientType.Assembly.GetType("Microsoft.Toolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions");
                if (type != null)
                {
                    MethodInfo method = type.GetMethod("CreateAllMessagesRegistrator", new Type[1] { recipientType });
                    if (method != null)
                    {
                        return (Action<IMessenger, object>)method.Invoke(null, new object[1]);
                    }
                }
                return null;
            }
        }

        public static void RegisterAll<TToken>(this IMessenger messenger, object recipient, TToken token) where TToken : IEquatable<TToken>
        {
            DiscoveredRecipients<TToken>.RegistrationMethods.GetValue(recipient.GetType(), (Type t) => LoadRegistrationMethodsForType(t))(messenger, recipient, token);
            static Action<IMessenger, object, TToken> LoadRegistrationMethodsForType(Type recipientType)
            {
                Type type = recipientType.Assembly.GetType("Microsoft.Toolkit.Mvvm.Messaging.__Internals.__IMessengerExtensions");
                if (type != null)
                {
                    MethodInfo method = type.GetMethod("CreateAllMessagesRegistratorWithToken", new Type[1] { recipientType });
                    if (method != null)
                    {
                        return (Action<IMessenger, object, TToken>)method.MakeGenericMethod(typeof(TToken)).Invoke(null, new object[1]);
                    }
                }
                return LoadRegistrationMethodsForTypeFallback(recipientType);
            }
            static Action<IMessenger, object, TToken> LoadRegistrationMethodsForTypeFallback(Type recipientType)
            {
                MethodInfo[] array = (from interfaceType in recipientType.GetInterfaces()
                                      where interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IRecipient<>)
                                      let messageType = interfaceType.GenericTypeArguments[0]
                                      select MethodInfos.RegisterIRecipient.MakeGenericMethod(messageType, typeof(TToken))).ToArray();
                if (array.Length == 0)
                {
                    return delegate
                    {
                    };
                }
                ParameterExpression arg0 = Expression.Parameter(typeof(IMessenger));
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
                ParameterExpression arg = Expression.Parameter(typeof(TToken));
                UnaryExpression inst1 = Expression.Convert(parameterExpression, recipientType);
                return Expression.Lambda<Action<IMessenger, object, TToken>>(Expression.Block(array.Select((MethodInfo registrationMethod) => Expression.Call(registrationMethod, new Expression[3] { arg0, inst1, arg }))), new ParameterExpression[3] { arg0, parameterExpression, arg }).Compile();
            }
        }

        public static void Register<TMessage>(this IMessenger messenger, IRecipient<TMessage> recipient) where TMessage : class
        {
            messenger.Register(recipient, default(Unit), delegate (IRecipient<TMessage> r, TMessage m)
            {
                r.Receive(m);
            });
        }

        public static void Register<TMessage, TToken>(this IMessenger messenger, IRecipient<TMessage> recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            messenger.Register(recipient, token, delegate (IRecipient<TMessage> r, TMessage m)
            {
                r.Receive(m);
            });
        }

        public static void Register<TMessage>(this IMessenger messenger, object recipient, MessageHandler<object, TMessage> handler) where TMessage : class
        {
            messenger.Register(recipient, default(Unit), handler);
        }

        public static void Register<TRecipient, TMessage>(this IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler) where TRecipient : class where TMessage : class
        {
            messenger.Register(recipient, default(Unit), handler);
        }

        public static void Register<TMessage, TToken>(this IMessenger messenger, object recipient, TToken token, MessageHandler<object, TMessage> handler) where TMessage : class where TToken : IEquatable<TToken>
        {
            messenger.Register(recipient, token, handler);
        }

        public static void Unregister<TMessage>(this IMessenger messenger, object recipient) where TMessage : class
        {
            messenger.Unregister<TMessage, Unit>(recipient, default);
        }

        public static TMessage Send<TMessage>(this IMessenger messenger) where TMessage : class, new()
        {
            return messenger.Send(new TMessage(), default(Unit));
        }

        public static TMessage Send<TMessage>(this IMessenger messenger, TMessage message) where TMessage : class
        {
            return messenger.Send(message, default(Unit));
        }

        public static TMessage Send<TMessage, TToken>(this IMessenger messenger, TToken token) where TMessage : class, new() where TToken : IEquatable<TToken>
        {
            return messenger.Send(new TMessage(), token);
        }
    }
}
