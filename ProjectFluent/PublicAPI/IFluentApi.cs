﻿using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData;
using System;

namespace Shockah.ProjectFluent
{
	public interface IFluentApi
	{
		#region Locale

		IGameLocale DefaultLocale { get; }
		IGameLocale CurrentLocale { get; }

		IGameLocale GetBuiltInLocale(LocalizedContentManager.LanguageCode languageCode);
		IGameLocale GetModLocale(ModLanguage language);

		#endregion

		#region Localizations

		IFluent<string> GetLocalizations(IGameLocale locale, IManifest mod, string? name = null);
		IFluent<string> GetLocalizationsForCurrentLocale(IManifest mod, string? name = null);

		#endregion

		#region Specialized types

		IEnumFluent<EnumType> GetEnumFluent<EnumType>(IFluent<string> baseFluent, string keyPrefix = "") where EnumType : Enum;
		IFluent<T> GetMappingFluent<T>(IFluent<string> baseFluent, Func<T, string> mapper);

		#endregion

		#region Custom Fluent functions

		IFluentFunctionValue CreateStringValue(string value);
		IFluentFunctionValue CreateIntValue(int value);
		IFluentFunctionValue CreateLongValue(long value);
		IFluentFunctionValue CreateFloatValue(float value);
		IFluentFunctionValue CreateDoubleValue(double value);

		void RegisterFunction(IManifest mod, string name, FluentFunction function);
		void UnregisterFunction(IManifest mod, string name);

		#endregion
	}
}