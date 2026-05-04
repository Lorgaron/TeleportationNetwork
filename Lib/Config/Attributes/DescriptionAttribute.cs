using System;

namespace TeleportationNetwork.Lib.Config
{
    /// <summary>
    /// More detailed value description
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class DescriptionAttribute : Attribute
    {
        public string Text { get; }

        public DescriptionAttribute(string text)
        {
            Text = text;
        }
    }
}
