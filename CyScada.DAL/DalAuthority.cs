﻿using System;
using System.Collections;
using System.Data;
using System.Text;

namespace CyScada.DAL
{
    public class DalAuthority
    {
        public DataTable GetAuthorityList()
        {
            return GetAuthorityList(new Hashtable());
        }



        public DataTable GetAuthorityList(Hashtable filterModel)
        {
            var sqlBuilder = new StringBuilder(@"SELECT * FROM lonni_f.ZQ_Authorities WITH(NOLOCK) where 1=1 ");
            foreach (DictionaryEntry pair in filterModel)
            {
                var value = pair.Value is string
                    ? "'" + pair.Value + "'"
                    : pair.Value is DateTime
                        ? ((DateTime)pair.Value).ToString("yyyy-MM-dd HH:mm:ss")
                        : pair.Value == null
                            ? string.Empty
                            : pair.Value.ToString();

                sqlBuilder.AppendFormat(@" AND {0}={1}", pair.Key.ToString(), value);
            }

            var result = SqlHelper.ExecuteDataset(SqlHelper.GetConnection(), CommandType.Text, sqlBuilder.ToString());
            return result == null || result.Tables.Count == 0 ? null : result.Tables[0];
        }



        public string CreateAuthority(Hashtable model)
        {
            var sql = string.Format(@"INSERT INTO lonni_f.ZQ_Authorities
        ( Name, Description,AuthorityId )
        SELECT   '{0}', -- Name - varchar(50)
          '{1}', -- Description - varchar(500)
                (SELECT ISNULL(MAX(AuthorityId)*2,1) FROM lonni_f.ZQ_Authorities WITH(NOLOCK))", model["Name"], model["Description"]);

            try
            {
                var result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, sql);

                if (result <= 0)
                {
                    return "添加失败!";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return string.Empty;
        }

        public string ModifyAuthority(Hashtable model)
        {
            var sql = string.Format(@"UPDATE  lonni_f.ZQ_Authorities
SET     Name = '{1}' ,
        Description = '{2}' 
WHERE   ID = {0}", model["Id"], model["Name"], model["Description"]);
            try
            {

                var result = SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, sql);
                if (result <= 0)
                {
                    return "修改失败!" + result;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            return string.Empty;
        }

        public void DeleteAuthority(int id)
        {
            var sql = string.Format(@"DELETE FROM lonni_f.ZQ_Authorities WHERE ID={0}", id);
            SqlHelper.ExecuteNonQuery(SqlHelper.GetConnection(), CommandType.Text, sql);
        }
    }
}