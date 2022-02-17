#nullable enable
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Catel;

namespace ModbusWpf.Common.Helpers
{
    public class EnumHelpers
    {

        /// <summary>
        /// Translates a given Enum Description Attribute to it's matching Enum value
        /// If there is no feasible description, matching is tried by EnumValue.ToString
        /// </summary>
        /// <typeparam name="T">Target enumeration type</typeparam>
        /// <param name="enumDescription">description string</param>
        /// <returns>Enumeration value or empty string as object</returns>
        public static object EnumDescriptionToEnumValue<T>(string enumDescription) where T : Enum
        {
            // get all enumeration values of the current type
            var enums = Enum.GetValues(typeof(T));
            foreach (var value in enums)
            {
                // Use Reflection to get the attributes of the current enum value
                var fieldInfo = typeof(T).GetField(value.ToString());
                var attribArray = fieldInfo.GetCustomAttributes(false);

                foreach (var descAtt in attribArray.OfType<DescriptionAttribute>())
                {
                    // if the current element has a fitting description, return it
                    if (descAtt.Description == enumDescription)
                    {
                        return value;
                    }
                }
            }

            // fallback rule: if there is no matching description, maybe there is a matching enum member
            return enums.Cast<object>().FirstOrDefault(value => value.ToString() == enumDescription) ?? string.Empty;
        }

        //https://stackoverflow.com/questions/17380900/enum-localization
        public static Array EnumTypeDescriptionToItemSourceArray(Type enumType)
        {
            var actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            var enumValues = Enum.GetValues(actualEnumType);

            if (actualEnumType == enumType)
                return (from object enumValue in enumValues select EnumHelpers.GetEnumTranslation(enumValue)).ToArray();

            var tempArray = Array.CreateInstance(actualEnumType, enumValues.Length + 1);
            enumValues.CopyTo(tempArray, 1);
            return tempArray;
        }

        /// <summary>
        /// Translate an enum value to the resource key as given by 
        /// [Display] attribute of an enum value
        /// If there is no [Display] attribute,
        /// enumValue.ToString is used as resource key
        /// If there is no resource with that key,
        /// enumValue.ToString is returned
        /// </summary>
        /// <param name="enumValue">enumeration value</param>
        /// <returns>translation from Resource dictionary</returns>
        /// <example>
        /// public enum GasTypes
        /// {
        ///     [Display(Description = nameof(Resources.GasTypeHydrogen), ResourceType = typeof(Resources))]
        ///     Hydrogen,
        ///     [Display(Description = nameof(Resources.GasTypeOxygen), ResourceType = typeof(Resources))]
        ///     Oxygen,
        ///     [Display(Description = nameof(Resources.GasTypeNitrogen), ResourceType = typeof(Resources))]
        ///     Nitrogen
        /// }
        /// [...]
        /// var theGas = GasTypes.Oxygen;
        /// 
        /// languageService.PreferredCulture = new CultureInfo("en-US");
        /// localizedGasName = EnumHelpers.GetEnumTranslation{GasTypes}(theGas);
        /// Assert.IsTrue(localizedGasName == "Oxygen");
        /// 
        /// languageService.PreferredCulture = new CultureInfo("de-DE");
        /// localizedGasName = EnumHelpers.GetEnumTranslation{GasTypes}(theGas);
        /// Assert.IsTrue(localizedGasName == "Sauerstoff");
        /// </example>
        public static string GetEnumTranslation(object? enumValue)
        {
            if (enumValue == null)
                return null!;

            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());

            // try to get the description from the DisplayAttribute from the enum member
            var fi = fieldInfo.GetCustomAttributes(false);

            var resourceLst = (from att in fi where att is DisplayAttribute select ((DisplayAttribute) att).Description)
                .ToList();
            var resourceKey = resourceLst.FirstOrDefault();

            // there is no Display attribute; fall back
            if (resourceKey == null)
                return enumValue.ToString();

            // get resourceKey translation (using Catel LanguageHelper)
            var translated = LanguageHelper.GetString(resourceKey);
            
            // fall back if empty/not found
            if (string.IsNullOrEmpty(translated))
                return enumValue.ToString();

            return translated;
        }

        /// <summary>
        /// Back translate a localized string to an enum value
        /// using the [Display] attribute
        /// </summary>
        /// <typeparam name="T">Type of target enumeration</typeparam>
        /// <param name="enumTranslation">localized Enum-Value</param>
        /// <returns>Enumeration value as object</returns>
        /// <example>
        /// public enum GasTypes
        /// {
        ///     [Display(Description = nameof(Resources.GasTypeHydrogen), ResourceType = typeof(Resources))]
        ///     Hydrogen,
        ///     [Display(Description = nameof(Resources.GasTypeOxygen), ResourceType = typeof(Resources))]
        ///     Oxygen,
        ///     [Display(Description = nameof(Resources.GasTypeNitrogen), ResourceType = typeof(Resources))]
        ///     Nitrogen
        /// }
        /// [...]
        ///
        /// languageService.PreferredCulture = new CultureInfo("en-US");
        /// var localizedGasName = "Oxygen";
        /// var theGas = EnumTranslationToEnumValue{GasTypes}(localizedName);
        /// Assert.IsTrue(theGas == GasTypes.Oxygen);
        /// 
        /// languageService.PreferredCulture = new CultureInfo("de-DE");
        /// localizedGasName = "Stickstoff";
        /// theGas = EnumTranslationToEnumValue{GasTypes}(localizedName);
        /// Assert.IsTrue(theGas == GasTypes.Nitrogen);
        /// </example>
        public static object? EnumTranslationToEnumValue<T>(string enumTranslation) where T : struct, Enum
        {
            // get all enum values of the type in question
            var enums = Enum.GetValues(typeof(T));
            foreach (var value in enums)
            {
                var fieldInfo = value.GetType().GetField(value.ToString());

                // get the enum member [Display] attributes and get its first description
                var fi = fieldInfo.GetCustomAttributes(false).OfType<DisplayAttribute>();

                var resourceLst = from att in fi select att.Description;
                var resourceKey = resourceLst.FirstOrDefault();

                // there is no Display attribute for this member
                if (resourceKey == null)
                    continue;

                // lookup the display translation and check for match
                var translated = LanguageHelper.GetString(resourceKey);
                if (translated == enumTranslation)
                    return value;
            }

            // no matching [Display (Description=...)] attribute found
            // fallback to direct parsing son the value
            return Enum.TryParse(enumTranslation, out T fallback) ? fallback : null;
        }

    }
}

