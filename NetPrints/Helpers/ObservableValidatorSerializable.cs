using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.ComponentModel
{
    public abstract class ObservableValidatorSerializable : ObservableObjectSerializable, INotifyDataErrorInfo
    {
        private static readonly ConditionalWeakTable<Type, Action<object>> EntityValidatorMap = new();

        private static readonly ConditionalWeakTable<Type, Dictionary<string, string>> DisplayNamesMap = new();

        private static readonly PropertyChangedEventArgs HasErrorsChangedEventArgs = new(nameof(HasErrors));

        private readonly ValidationContext validationContext;

        private readonly Dictionary<string, List<ValidationResult>> errors = new();

        private int totalErrors;

        public bool HasErrors => totalErrors > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected ObservableValidatorSerializable()
        {
            validationContext = new ValidationContext(this);
        }

        protected ObservableValidatorSerializable(IDictionary<object, object?>? items)
        {
            validationContext = new ValidationContext(this, items);
        }

        protected ObservableValidatorSerializable(IServiceProvider? serviceProvider, IDictionary<object, object?>? items)
        {
            validationContext = new ValidationContext(this, serviceProvider, items);
        }

        protected ObservableValidatorSerializable(ValidationContext validationContext)
        {
            this.validationContext = validationContext;
        }

        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, bool validate, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(ref field, newValue, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>([NotNullIfNotNull("newValue")] ref T field, T newValue, IEqualityComparer<T> comparer, bool validate, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(ref field, newValue, comparer, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, Action<T> callback, bool validate, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(oldValue, newValue, callback, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, bool validate, [CallerMemberName()] string? propertyName = null)
        {
            bool num = SetProperty(oldValue, newValue, comparer, callback, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, bool validate, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            bool num = SetProperty(oldValue, newValue, model, callback, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool SetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, bool validate, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            bool num = SetProperty(oldValue, newValue, comparer, model, callback, propertyName);
            if (num && validate)
            {
                ValidateProperty(newValue, propertyName);
            }
            return num;
        }

        protected bool TrySetProperty<T>(ref T field, T newValue, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null)
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(ref field, newValue, propertyName);
            }
            return false;
        }

        protected bool TrySetProperty<T>(ref T field, T newValue, IEqualityComparer<T> comparer, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null)
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(ref field, newValue, comparer, propertyName);
            }
            return false;
        }

        protected bool TrySetProperty<T>(T oldValue, T newValue, Action<T> callback, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null)
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(oldValue, newValue, callback, propertyName);
            }
            return false;
        }

        protected bool TrySetProperty<T>(T oldValue, T newValue, IEqualityComparer<T> comparer, Action<T> callback, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null)
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(oldValue, newValue, comparer, callback, propertyName);
            }
            return false;
        }

        protected bool TrySetProperty<TModel, T>(T oldValue, T newValue, TModel model, Action<TModel, T> callback, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(oldValue, newValue, model, callback, propertyName);
            }
            return false;
        }

        protected bool TrySetProperty<TModel, T>(T oldValue, T newValue, IEqualityComparer<T> comparer, TModel model, Action<TModel, T> callback, out IReadOnlyCollection<ValidationResult>? errors, [CallerMemberName()] string? propertyName = null) where TModel : class
        {
            if (TryValidateProperty(newValue, propertyName, out errors))
            {
                return SetProperty(oldValue, newValue, comparer, model, callback, propertyName);
            }
            return false;
        }

        protected void ClearErrors(string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                ClearAllErrors();
            }
            else
            {
                ClearErrorsForProperty(propertyName);
            }
        }

        public IEnumerable<ValidationResult> GetErrors(string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return GetAllErrors();
            }
            if (errors.TryGetValue(propertyName, out var value))
            {
                return value;
            }
            return Array.Empty<ValidationResult>();
            [MethodImpl(MethodImplOptions.NoInlining)]
            IEnumerable<ValidationResult> GetAllErrors()
            {
                return errors.Values.SelectMany((List<ValidationResult> errors) => errors);
            }
        }

        IEnumerable INotifyDataErrorInfo.GetErrors(string? propertyName)
        {
            return GetErrors(propertyName);
        }

        protected void ValidateAllProperties()
        {
#pragma warning disable CS8603
            EntityValidatorMap.GetValue(GetType(), (Type t) => GetValidationAction(t))(this);
#pragma warning restore CS8603

            static Action<object>? GetValidationAction(Type type)
            {
                if (type != null && type.Assembly != null)
                {
                    Type? type2 = type.Assembly.GetType("Microsoft.Toolkit.Mvvm.ComponentModel.__Internals.__ObservableValidatorExtensions");
                    if (type2 != null)
                    {
                        MethodInfo? method = type2.GetMethod("CreateAllPropertiesValidator", new Type[1] { type });
                        if (method != null)
                        {
                            return (Action<object>?)method.Invoke(null, new object[1]);
                        }
                    }
                    return GetValidationActionFallback(type);
                }
                return null;
            }

            static Action<object>? GetValidationActionFallback(Type type)
            {
                (string, MethodInfo)[] array = (from property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                                where property.GetIndexParameters().Length == 0 && property.GetCustomAttributes<ValidationAttribute>(inherit: true).Any()
                                                let getMethod = property.GetMethod
                                                where getMethod != null
                                                select (property.Name, getMethod)).ToArray();
                if (array.Length == 0)
                {
                    return delegate
                    {
                    };
                }
                ParameterExpression parameterExpression = Expression.Parameter(typeof(object));
                UnaryExpression inst0 = Expression.Convert(parameterExpression, type);
                MethodInfo? validateMethod = typeof(ObservableValidatorSerializable).GetMethod("ValidateProperty", BindingFlags.Instance | BindingFlags.NonPublic);
                if (validateMethod != null)
                {
                    return Expression.Lambda<Action<object>>(Expression.Block(array.Select(((string Name, MethodInfo GetMethod) property) => Expression.Call(inst0, validateMethod, new Expression[2]
                    {
                    Expression.Convert(Expression.Call(inst0, property.GetMethod), typeof(object)),
                    Expression.Constant(property.Name)
                    }))), new ParameterExpression[1] { parameterExpression }).Compile();
                }
                return null;
            }
        }

        protected internal void ValidateProperty(object? value, [CallerMemberName()] string? propertyName = null)
        {
            if (propertyName == null)
            {
                ThrowArgumentNullExceptionForNullPropertyName();
                return;
            }
            if (!errors.TryGetValue(propertyName, out List<ValidationResult>? value2))
            {
                value2 = new List<ValidationResult>();
                errors.Add(propertyName, value2);
            }
            bool flag = false;
            if (value2.Count > 0)
            {
                value2.Clear();
                flag = true;
            }
            validationContext.MemberName = propertyName;
            validationContext.DisplayName = GetDisplayNameForProperty(propertyName);
            bool flag2 = Validator.TryValidateProperty(value, validationContext, value2);
            if (flag2)
            {
                if (flag)
                {
                    totalErrors--;
                    if (totalErrors == 0)
                    {
                        OnPropertyChanged(HasErrorsChangedEventArgs);
                    }
                }
            }
            else if (!flag)
            {
                totalErrors++;
                if (totalErrors == 1)
                {
                    OnPropertyChanged(HasErrorsChangedEventArgs);
                }
            }
            if (flag || !flag2)
            {
                this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private bool TryValidateProperty(object? value, string? propertyName, out IReadOnlyCollection<ValidationResult>? errors)
        {
            if (propertyName == null)
            {
                ThrowArgumentNullExceptionForNullPropertyName();
                errors = null;
                return false;
            }
            if (!this.errors.TryGetValue(propertyName, out List<ValidationResult>? value2))
            {
                value2 = new List<ValidationResult>();
                this.errors.Add(propertyName, value2);
            }
            bool flag = value2.Count > 0;
            List<ValidationResult> list = new();
            validationContext.MemberName = propertyName;
            validationContext.DisplayName = GetDisplayNameForProperty(propertyName);
            bool num = Validator.TryValidateProperty(value, validationContext, list);
            if (num && flag)
            {
                value2.Clear();
                totalErrors--;
                if (totalErrors == 0)
                {
                    OnPropertyChanged(HasErrorsChangedEventArgs);
                }
                this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
            errors = list;
            return num;
        }

        private void ClearAllErrors()
        {
            if (totalErrors == 0)
            {
                return;
            }
            foreach (KeyValuePair<string, List<ValidationResult>> error in errors)
            {
                bool num = error.Value.Count > 0;
                error.Value.Clear();
                if (num)
                {
                    this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(error.Key));
                }
            }
            totalErrors = 0;
            OnPropertyChanged(HasErrorsChangedEventArgs);
        }

        private void ClearErrorsForProperty(string propertyName)
        {
            if (errors.TryGetValue(propertyName, out var value) && value.Count != 0)
            {
                value.Clear();
                totalErrors--;
                if (totalErrors == 0)
                {
                    OnPropertyChanged(HasErrorsChangedEventArgs);
                }
                this.ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private string GetDisplayNameForProperty(string propertyName)
        {
            DisplayNamesMap.GetValue(GetType(), (Type t) => GetDisplayNames(t)).TryGetValue(propertyName, out var value);
            return value ?? propertyName;
            static Dictionary<string, string> GetDisplayNames(Type type)
            {
                Dictionary<string, string> dictionary = new();
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (PropertyInfo propertyInfo in properties)
                {
                    DisplayAttribute? customAttribute = propertyInfo.GetCustomAttribute<DisplayAttribute>();
                    if (customAttribute != null)
                    {
                        string? name = customAttribute.GetName();
                        if (name != null)
                        {
                            dictionary.Add(propertyInfo.Name, name);
                        }
                    }
                }
                return dictionary;
            }
        }

        private static void ThrowArgumentNullExceptionForNullPropertyName()
        {
            throw new ArgumentNullException("propertyName", "The input property name cannot be null when validating a property");
        }
    }
}
