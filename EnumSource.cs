using System;
using System.Windows.Markup;

namespace SimpleBackup
{
    public class EnumSource : MarkupExtension
    {
        private readonly Type _enumType;

        public EnumSource(Type enumType)
        {
            if (!enumType.IsEnum)
            {
                throw new ArgumentException($"{enumType} is not enum.");
            }

            _enumType = enumType ?? throw new ArgumentNullException(nameof(enumType));
        }

        public override object ProvideValue(IServiceProvider serviceProvider) =>
            Enum.GetValues(_enumType);
    }
}