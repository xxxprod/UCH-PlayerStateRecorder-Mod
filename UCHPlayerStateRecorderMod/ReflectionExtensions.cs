using System;
using System.Collections.Generic;
using System.Reflection;

namespace UCHPlayerStateRecorderMod;

public static class ReflectionExtensions
{
	private static readonly Dictionary<string, FieldInfo> FieldCache = new();

	public static T GetField<T>(this object obj, string fieldName)
	{
		if (!FieldCache.TryGetValue(fieldName, out FieldInfo field))
		{
			FieldCache[fieldName] = field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception($"Field {fieldName} not found");
		}

		return (T)field.GetValue(obj);
	}

	public static void SetField<T>(this object obj, string fieldName, T value)
	{
		if (!FieldCache.TryGetValue(fieldName, out FieldInfo field))
		{
			FieldCache[fieldName] = field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) ?? throw new Exception($"Field {fieldName} not found");
		}

		field.SetValue(obj, value);
	}
}