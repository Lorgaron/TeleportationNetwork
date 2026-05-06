using System;

namespace TeleportationNetwork
{
	/// <summary>
	/// This value will not be synchronized from server to client
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ClientOnlyAttribute : Attribute { }
}