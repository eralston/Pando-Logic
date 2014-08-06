using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace PandoLogic
{
    /// <summary>
    /// A class for all extensions methods on the base object class
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// Shallow copies all properties from the source object to the destination
        /// </summary>
        /// <param name="objSource"></param>
        /// <param name="objDestination"></param>
        public static void CopyProperties(this object objSource, object objDestination)
        {
            //get the list of all properties in the destination object
            PropertyInfo[] destProps = objDestination.GetType().GetProperties();
            PropertyInfo[] sourceProps = objSource.GetType().GetProperties();
            Dictionary<string, PropertyInfo> destPropsDict = destProps.ToDictionary(sp => sp.Name);

            //get the list of all properties in the source object
            foreach (PropertyInfo sourceProp in sourceProps)
            {
                if(destPropsDict.ContainsKey(sourceProp.Name))
                {
                    PropertyInfo destProp = destPropsDict[sourceProp.Name];
                    if(destProp.PropertyType.IsAssignableFrom(sourceProp.PropertyType))
                    {
                        destProp.SetValue(objDestination, sourceProp.GetValue(objSource));
                    }
                }
            }
        }

    }
}