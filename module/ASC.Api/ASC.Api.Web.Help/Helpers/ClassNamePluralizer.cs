/*
 * 
 * (c) Copyright Ascensio System SIA 2010-2014
 * 
 * This program is a free software product.
 * You can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * (AGPL) version 3 as published by the Free Software Foundation. 
 * In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended to the effect 
 * that Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 * 
 * This program is distributed WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * For details, see the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
 * 
 * You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
 * 
 * The interactive user interfaces in modified source and object code versions of the Program 
 * must display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
 * 
 * Pursuant to Section 7(b) of the License you must retain the original Product logo when distributing the program. 
 * Pursuant to Section 7(e) we decline to grant you any rights under trademark law for use of our trademarks.
 * 
 * All the Product's GUI elements, including illustrations and icon sets, as well as technical 
 * writing content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0 International. 
 * See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode
 * 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Caching;
using ASC.Api.Collections;

namespace ASC.Api.Web.Help.Helpers
{
    [DataContract(Namespace = "")]
    public class TypeDescription:ICloneable
    {
        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Example { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public string Note { get; set; }

        public bool IsOptional { get; set; }

        public bool IsCollection { get; set; }

        public TypeDescription(string description, string example)
        {
            Description = description;
            Example = example;
        }

        public TypeDescription Clone()
        {
            return new TypeDescription(Description, Example);
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

    [DataContract(Namespace = "")]
    public class TypeDescriptor
    {
        internal const string SystemNullable = "System.Nullable`1[";
        internal const string SystemIEnumerable = "System.Collections.Generic.IEnumerable`1[";

        [DataMember(Name = "Names")]
        public ItemDictionary<string, TypeDescription> Names = new ItemDictionary<string, TypeDescription>();

        public TypeDescriptor()
        {
            
        }

        public TypeDescription Get(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
            {

                if (Names.ContainsKey(typeName))
                    return Names[typeName];

                if (typeName.StartsWith(SystemNullable))
                {
                    var optionalDescription =
                        Get(typeName.Substring(SystemNullable.Length, typeName.Length - 1 - SystemNullable.Length)).
                            Clone();
                    optionalDescription.IsOptional = true;
                    return optionalDescription;
                }
                if (typeName.StartsWith(SystemIEnumerable))
                {
                    var optionalDescription =
                        Get(typeName.Substring(SystemIEnumerable.Length, typeName.Length - 1 - SystemIEnumerable.Length))
                            .Clone();
                    optionalDescription.IsCollection = true;
                    optionalDescription.Description = string.Format("Collection of {0}s",
                                                                    optionalDescription.Description);
                    return optionalDescription;
                }
            }
            return new TypeDescription(typeName, string.Empty);
        }
    }

    public static class ClassNamePluralizer
    {
        private static TypeDescriptor _descriptor;

        public static void LoadClassNames(Stream data)
        {
            var serializer = new DataContractSerializer(typeof(TypeDescriptor));
            _descriptor = serializer.ReadObject(data) as TypeDescriptor;
        }

        public static bool IsOptional(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
                return typeName.StartsWith(TypeDescriptor.SystemNullable);
            return false;
        }

        public static bool IsCollection(string typeName)
        {
            if (!string.IsNullOrEmpty(typeName))
                return typeName.StartsWith(TypeDescriptor.SystemIEnumerable);
            return false;
        }

        public static TypeDescription ToHumanName(string typeName)
        {
            return _descriptor==null?new TypeDescription(typeName,""):_descriptor.Get(typeName);
        }

        public static void LoadAndWatch(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                using (var fs = File.OpenRead(path))
                {
                    LoadClassNames(fs);
                }
                HttpRuntime.Cache.Add("classnamesfile", path, new CacheDependency(path), Cache.NoAbsoluteExpiration,
                                      Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, OnRemove);
            }
        }

        private static void OnRemove(string key, object value, CacheItemRemovedReason reason)
        {
            if (reason == CacheItemRemovedReason.DependencyChanged)
            {
                //need http context to reload:(
                try
                {
                    LoadAndWatch(value as string);
                }
                catch (Exception)
                {

                }
            }
            else
            {
                if (!string.IsNullOrEmpty(value as string))
                {
                    //Insert again
                    HttpRuntime.Cache.Add("classnamesfile", value, new CacheDependency(value.ToString()),
                                          Cache.NoAbsoluteExpiration,
                                          Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, OnRemove);
                }
            }
        }
    }
}