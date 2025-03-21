﻿using MapAMilepost.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.Utils
{
    public static class SOEResponseUtils
    {
        /// <summary>
        /// -   Used to copy the properties of the PointResponse object returned from the SOE response to the static PointResponse
        ///     instance in the viewmodel.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                {
                    continue;
                }
                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }
                if (!targetProperty.CanWrite)
                {
                    continue;
                }
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                {
                    continue;
                }
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }
                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }

        /// <summary>
        /// -   Checks whether or not the properties of the PointResponseModel are all null, indicating that the viewmodel's PointResponse
        ///     hasn't been updated yet.
        /// </summary>
        /// <param name="myObject"></param>
        /// <returns></returns>
        public static bool HasBeenUpdated(object myObject)
        {
            if(myObject == null)
            {
                return false;
            }
            foreach (PropertyInfo pi in myObject.GetType().GetProperties())
            {
                if (pi.PropertyType == typeof(string))
                {
                    string value = (string)pi.GetValue(myObject);
                    Console.WriteLine(pi.Name);
                    List<string> excludeList = new List<string> {"ReferenceDate", "PointFeatureID", "Error"};//list of keys not expected to be present in the Find Route Locations response.
                    if (string.IsNullOrEmpty(value)&&!excludeList.Contains(pi.Name))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Creates a PointResponseModel that either prepopulates the SRMP, Arm, Decrease, and Back information
        /// if the add in mode is Form, otherwise set all of the properties of the new object to null.
        /// </summary>
        /// <param name="VM">The target viewmodel</param>
        /// <returns></returns>
        public static PointResponseModel CreateInputConditionalPointModel(ViewModelBase VM)
        {
            PointResponseModel Point = new PointResponseModel()
            {
                Srmp = VM.IsMapMode ? null : VM.SRMPIsSelected ? 0 : null,
                Arm = VM.IsMapMode ? null : !VM.SRMPIsSelected ? 0 : null,
                Decrease = VM.IsMapMode ? null : false,
                Back = VM.IsMapMode ? null : false,
            };
            return Point;
        }
    }
}
