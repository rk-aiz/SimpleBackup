using System;
using System.ComponentModel;
using System.Resources;

namespace SimpleBackup
{
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        readonly ResourceManager _resourceManager;
        private readonly string _resourceKey;

        public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
        {
            this._resourceManager = new ResourceManager(resourceType);
            this._resourceKey = resourceKey;
        }

        public override string Description
        {
            get
            {
                string description = this._resourceManager.GetString(this._resourceKey);
                return string.IsNullOrWhiteSpace(description) ?
                    this._resourceKey : description;
            }
        }
    }
}
