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

using ASC.Common.Data;
using ASC.Common.Data.Sql;
using System.Collections.Generic;
using System.Configuration;

namespace ASC.Core.Data
{
    public abstract class DbBaseService
    {
        private readonly string dbid;

        protected string TenantColumn
        {
            get;
            private set;
        }

        protected DbBaseService(ConnectionStringSettings connectionString, string tenantColumn)
        {
            dbid = connectionString.Name;
            TenantColumn = tenantColumn;
        }

        protected T ExecScalar<T>(ISqlInstruction sql)
        {
            using (var db = GetDb())
            {
                return db.ExecuteScalar<T>(sql);
            }
        }

        protected int ExecNonQuery(ISqlInstruction sql)
        {
            using (var db = GetDb())
            {
                return db.ExecuteNonQuery(sql);
            }
        }

        protected List<object[]> ExecList(ISqlInstruction sql)
        {
            using (var db = GetDb())
            {
                return db.ExecuteList(sql);
            }
        }

        protected void ExecBatch(params ISqlInstruction[] batch)
        {
            using (var db = GetDb())
            {
                db.ExecuteBatch(batch);
            }
        }

        protected void ExecBatch(IEnumerable<ISqlInstruction> batch)
        {
            using (var db = GetDb())
            {
                db.ExecuteBatch(batch);
            }
        }

        protected IDbManager GetDb()
        {
            return new DbManager(dbid);
        }

        protected SqlQuery Query(string table, int tenant)
        {
            return new SqlQuery(table).Where(GetTenantColumnName(table), tenant);
        }

        protected SqlInsert Insert(string table, int tenant)
        {
            return new SqlInsert(table, true).InColumnValue(GetTenantColumnName(table), tenant);
        }

        protected SqlUpdate Update(string table, int tenant)
        {
            return new SqlUpdate(table).Where(GetTenantColumnName(table), tenant);
        }

        protected SqlDelete Delete(string table, int tenant)
        {
            return new SqlDelete(table).Where(GetTenantColumnName(table), tenant);
        }

        private string GetTenantColumnName(string table)
        {
            var pos = table.LastIndexOf(' ');
            return (0 < pos ? table.Substring(pos).Trim() + '.' : string.Empty) + TenantColumn;
        }
    }
}