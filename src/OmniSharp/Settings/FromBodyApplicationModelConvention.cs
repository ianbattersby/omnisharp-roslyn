using System.Linq;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.AspNet.Mvc.ModelBinding;
using System.ComponentModel;
using System;
using System.Reflection;

namespace OmniSharp.Settings
{
    public class FromBodyApplicationModelConvention : IApplicationModelConvention
    {
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                foreach (var action in controller.Actions)
                {
                    foreach (var parameter in action.Parameters)
                    {
                        if (parameter.BindingInfo?.BindingSource != null ||
                            parameter.Attributes.OfType<IBindingSourceMetadata>().Any() ||
                            CanConvertFromString(parameter.ParameterInfo.ParameterType))
                        {
                            // behavior configured or simple type so do nothing
                        }
                        else
                        {
                            // Complex types are by-default from the body.
                            parameter.BindingInfo = parameter.BindingInfo ?? new BindingInfo();
                            parameter.BindingInfo.BindingSource = BindingSource.Body;
                        }
                    }
                }
            }
        }

        private bool CanConvertFromString(Type type)
        {
            Type underlyingType = Nullable.GetUnderlyingType(type);

            return ((underlyingType != null && (
                underlyingType.GetTypeInfo().IsPrimitive ||
                   underlyingType.Equals(typeof(string)) ||
                   underlyingType.Equals(typeof(DateTime)) ||
                   underlyingType.Equals(typeof(Decimal)) ||
                   underlyingType.Equals(typeof(Guid)) ||
                   underlyingType.Equals(typeof(DateTimeOffset)) ||
                   underlyingType.Equals(typeof(TimeSpan)))) || TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string)));
        }
    }
}