using System;

namespace TeleportationNetwork
{
	/// <summary>
	/// This property will be ignored
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ConfigIgnoreAttribute : Attribute { }
}