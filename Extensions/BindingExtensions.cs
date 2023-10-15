using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace SimpleBackup.Extensions
{
    internal static class BindingExtensions
    {
        internal static T CreateCopy<T>(this Binding binding, string stringFormat = null) where T : Binding, new()
        {
            // must create a copy, because same binding ref but diff StringFormats
            var bindingCopy = new T
            {
                UpdateSourceTrigger = binding.UpdateSourceTrigger,
                ValidatesOnDataErrors = binding.ValidatesOnDataErrors,
                Mode = binding.Mode,
                Path = binding.Path,

                AsyncState = binding.AsyncState,
                BindingGroupName = binding.BindingGroupName,
                BindsDirectlyToSource = binding.BindsDirectlyToSource,
                Converter = binding.Converter,
                ConverterCulture = binding.ConverterCulture,
                ConverterParameter = binding.ConverterParameter,
                FallbackValue = binding.FallbackValue,
                IsAsync = binding.IsAsync,
                NotifyOnSourceUpdated = binding.NotifyOnSourceUpdated,
                NotifyOnTargetUpdated = binding.NotifyOnTargetUpdated,
                NotifyOnValidationError = binding.NotifyOnValidationError,
                //StringFormat = set below...
                TargetNullValue = binding.TargetNullValue,
                UpdateSourceExceptionFilter = binding.UpdateSourceExceptionFilter,
                ValidatesOnExceptions = binding.ValidatesOnExceptions,
                XPath = binding.XPath,
                //ValidationRules = binding.ValidationRules,
            };

            if (binding.ElementName != null)
                bindingCopy.ElementName = binding.ElementName;
            else if (binding.Source != null)
                bindingCopy.Source = binding.Source;
            else if (binding.RelativeSource != null)
                bindingCopy.RelativeSource = binding.RelativeSource;

            // mutex ElementName, so modify if needed
            // ElementName = binding.ElementName,
            // Source = binding.Source,
            // RelativeSource = binding.RelativeSource,

            if (stringFormat != null)
            {
                bindingCopy.StringFormat = stringFormat;
            }
            return bindingCopy;
        }
    }
}
