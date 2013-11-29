﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Amido.Testing.NAuto.Builders.Services
{
    public class PropertyPopulationService : IPropertyPopulationService
    {
        private readonly PopulateProperty<string> populateStringService;
        private readonly PopulateProperty<int> populateIntService;
        private readonly PopulateProperty<int?> populateNullableIntProperty;
        private readonly PopulateProperty<double> populateDoubleService;
        private readonly PopulateProperty<double?> populateNullableDoubleService;
        private readonly PopulateProperty<bool> populateBoolService;
        private readonly PopulateProperty<bool?> populateNullableBoolService;
        private readonly PopulateProperty<DateTime> populateDateTimeService;
        private readonly PopulateProperty<DateTime?> populateNullableDateTimeService;
        private readonly PopulateProperty<Uri> populateUriService;
        private readonly IPopulateEnumService populateEnumService;
        private AutoBuilderConfiguration configuration;

        public PropertyPopulationService(
            PopulateProperty<string> populateStringService, 
            PopulateProperty<int> populateIntService,
            PopulateProperty<int?> populateNullableIntProperty,
            PopulateProperty<double> populateDoubleService,
            PopulateProperty<double?> populateNullableDoubleService,
            PopulateProperty<bool> populateBoolService,
            PopulateProperty<bool?> populateNullableBoolService,
            PopulateProperty<DateTime> populateDateTimeService,
            PopulateProperty<DateTime?> populateNullableDateTimeService,
            PopulateProperty<Uri> populateUriService,
            IPopulateEnumService populateEnumService)
        {
            this.populateStringService = populateStringService;
            this.populateIntService = populateIntService;
            this.populateNullableIntProperty = populateNullableIntProperty;
            this.populateDoubleService = populateDoubleService;
            this.populateNullableDoubleService = populateNullableDoubleService;
            this.populateBoolService = populateBoolService;
            this.populateNullableBoolService = populateNullableBoolService;
            this.populateDateTimeService = populateDateTimeService;
            this.populateNullableDateTimeService = populateNullableDateTimeService;
            this.populateUriService = populateUriService;
            this.populateEnumService = populateEnumService;
        }

        public void AddConfiguration(AutoBuilderConfiguration autoBuilderConfiguration)
        {
            configuration = autoBuilderConfiguration;
            populateStringService.SetAutoBuilderConfiguration(autoBuilderConfiguration);
        }

        public void PopulateProperties(object objectToPopulate, int depth)
        {
            if (depth > configuration.MaxDepth)
            {
                return;
            }

            var properties = objectToPopulate.GetType().GetProperties();

            foreach (var propertyInfo in properties)
            {
                propertyInfo.SetValue(objectToPopulate, Populate(depth, propertyInfo.Name, propertyInfo.PropertyType, propertyInfo.GetValue(objectToPopulate)));
            }
        }

        public List<object> BuildConstructorParameters(ConstructorInfo[] constructors, int depth)
        {
            var constructorParameters = new List<object>();

            foreach (var parameter in constructors.First().GetParameters())
            {
                constructorParameters.Add(Populate(depth + 1, parameter.Name, parameter.ParameterType, null));
            }

            return constructorParameters;
        }

        private object Populate(int depth, string propertyName, Type propertyType, object value)
        {
            if (propertyType.GetInterfaces().Any(x => x == typeof(IList)))
            {
                if (propertyType.FullName.Contains("System.Collections.Generic.List`1"))
                {
                    return PopulateList(depth, propertyName, propertyType, value);
                }
                if (propertyType.BaseType == typeof(Array) && value == null)
                {
                    return PopulateArray(depth, propertyName, propertyType);
                }
            }

            if (propertyType == typeof (string))
            {
                return populateStringService.Populate(propertyName, (string)value);
            }

            if (propertyType == typeof(int))
            {
                var intValue = value == null ? 0 : (int) value;
                return populateIntService.Populate(propertyName, intValue);
            }

            if (propertyType == typeof(int?))
            {
                return populateNullableIntProperty.Populate(propertyName, (int?)value);
            }

            if (propertyType == typeof(double))
            {
                var doubleValue = value == null ? 0 : (double) value;
                return populateDoubleService.Populate(propertyName, doubleValue);
            }

            if (propertyType == typeof(double?))
            {
                return populateNullableDoubleService.Populate(propertyName, (double?)value);
            }

            if (propertyType == typeof(bool))
            {
                var boolValue = value != null && (bool) value;
                return populateBoolService.Populate(propertyName, boolValue);
            }

            if (propertyType == typeof(bool?) && value != null)
            {
                return populateNullableBoolService.Populate(propertyName, (bool?) value);
            }

            if (propertyType == typeof(DateTime))
            {
                var dateTimeValue = value == null ? default(DateTime) : (DateTime) value;
                return populateDateTimeService.Populate(propertyName, dateTimeValue);
            }

            if (propertyType == typeof(DateTime?))
            {
                return populateNullableDateTimeService.Populate(propertyName, (DateTime?) value);
            }

            if (propertyType == typeof(Uri))
            {
                return populateUriService.Populate(propertyName, (Uri)value);
            }

            if (propertyType.BaseType == typeof(Enum) && value != null)
            {
                return populateEnumService.Populate(propertyName, propertyType, value);
            }
            
            if (IsPotentialComplexType(propertyType))
            {
                try
                {
                    var constructorParameters = BuildConstructorParameters(propertyType.GetConstructors(), depth + 1);

                    object complexType;
                    if (constructorParameters.Count > 0)
                    {
                        complexType = Activator.CreateInstance(propertyType, constructorParameters.ToArray());
                    }
                    else
                    {
                        complexType = Activator.CreateInstance(propertyType);
                    }
                    PopulateProperties(complexType, depth + 1);
                    return complexType;
                }
                catch (Exception)
                {
                    // swallow error
                }
            }
            Console.WriteLine("Sorry, unable to fully build this model. Unsupported Type: " + propertyType);
            return null;
        }

        private static bool IsPotentialComplexType(Type propertyType)
        {
            return propertyType.BaseType != typeof(ValueType) && !propertyType.IsPrimitive && propertyType != typeof(string) && propertyType != typeof(Uri);
        }

        private object PopulateArray(int depth, string propertyName, Type propertyType)
        {
            var arrayElementType = propertyType.GetElementType();
            var newArray = Array.CreateInstance(arrayElementType, configuration.DefaultListItemCount);
            for (var i = 0; i < configuration.DefaultListItemCount; i++)
            {
                newArray.SetValue(Populate(depth + 1, propertyName, arrayElementType, null), i);
            }
            return newArray;
        }

        private object PopulateList(int depth, string propertyName, Type propertyType, object value)
        {
            IList newList;

            if (value != null && ((IList) value).Count == 0)
            {
                newList = (IList) value;
            }
            else
            {
                newList = (IList) Activator.CreateInstance(propertyType);
            }

            for (var i = 0; i < configuration.DefaultListItemCount; i++)
            {
                newList.Add(Populate(depth + 1, propertyName, propertyType.GenericTypeArguments[0], null));
            }
            return newList;
        }
    }
}