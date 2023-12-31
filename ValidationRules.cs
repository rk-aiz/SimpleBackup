﻿using SimpleBackup.Converters;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Controls;

namespace SimpleBackup
{
    /// <summary>
    /// 正の整数かどうかを検証するValidationRule
    /// </summary>
    class PositiveIntegerRule : ValidationRule
    {
        private const ValidationMessage MESSAGE = ValidationMessage.PositiveIntegerOnly;
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (Int32.TryParse(value.ToString(), out int res) && 1 <= res)
            {
                return ValidationResult.ValidResult;
            }
            else
                return new ValidationResult(false, MESSAGE);
        }
    }

    class PositiveNumberRule : ValidationRule
    {
        private const ValidationMessage MESSAGE = ValidationMessage.PositiveNumberOnly;
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (float.TryParse(value.ToString(), out float res) && 0 < res)
            {
                return ValidationResult.ValidResult;
            }
            else
                return new ValidationResult(false, MESSAGE);
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum ValidationMessage
    {
        [LocalizedDescription("String_Positive_Integer_Only",
        typeof(SimpleBackup.Properties.Resources))]
        PositiveIntegerOnly,
        [LocalizedDescription("String_Positive_Number_Only",
        typeof(SimpleBackup.Properties.Resources))]
        PositiveNumberOnly,
    }
}
