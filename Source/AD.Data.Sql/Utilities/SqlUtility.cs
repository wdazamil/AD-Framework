using AD.Core.Attributes;
using AD.Core.Exceptions;
using AD.Core.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AD.Data.Sql.Utilities
{
    public static class SqlUtility
    {
        public static Dictionary<string, object> GetObjectPropertiesAsParameters(object obj)
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (!Attribute.IsDefined(property, typeof(NotMappedAttribute)))
                {
                    parameters[property.Name] = property.GetValue(obj, null);
                }
            }
            return parameters;
        }
        public static string GetQueryParameters(Dictionary<string, object> parameters)
        {
            string queryParameters = string.Empty;
            int pos = 0;
            foreach (KeyValuePair<string, object> entry in parameters)
            {
                if (pos != 0) queryParameters += " AND ";
                queryParameters += string.Format("{0} = '{1}'", entry.Key, entry.Value.ToString());
                pos++;
            }
            return queryParameters;
        }
        public static string GetUpdateParameters(Dictionary<string, object> parameters)
        {
            string updateParameters = string.Empty;
            int pos = 0;
            foreach (KeyValuePair<string, object> entry in parameters)
            {
                if (pos != 0) updateParameters += ", ";
                updateParameters += string.Format("{0} = '{1}'", entry.Key, entry.Value.ToString());
                pos++;
            }
            return updateParameters;
        }
        public static string GetInsertParameters(Dictionary<string, object> parameters)
        {
            if (parameters.Count == 0) return string.Empty;
            string insertParameters = string.Empty;
            string columns = string.Empty;
            string values = string.Empty;
            int pos = 0;
            foreach (KeyValuePair<string, object> entry in parameters)
            {
                if (pos != 0)
                {
                    columns += ", ";
                    values += ", ";
                }
                columns += entry.Key;
                values += string.Format("'{0}'", entry.Value.ToString());
                pos++;
            }
            columns = string.Format("({0})", columns);
            values = string.Format("VALUES ({0})", values);
            insertParameters = columns + " " + values;
            return insertParameters;
        }
        public static T PopulateRecord<T>(DataRow row) where T : BaseEntity, new()
        {
            T obj = new T();
            Type type = obj.GetType();
            foreach (PropertyInfo propertyInfo in type.GetProperties())
            {
                if (!Attribute.IsDefined(propertyInfo, typeof(NotMappedAttribute)))
                {
                    propertyInfo.SetValue(obj, row[propertyInfo.Name], null);
                }
            }
            return obj;
        }
        public static ICollection<T> GetRecords<T>(SqlCommand command) where T : BaseEntity, new()
        {
            List<T> list = new List<T>();
            DataRowCollection dataRowCollection = GetRows(command);
            list = (from DataRow dataRow in dataRowCollection select PopulateRecord<T>(dataRow)).ToList();
            return list;
        }
        public static ICollection<T> ExecuteStoredProcedure<T>(SqlCommand command) where T : BaseEntity, new()
        {
            List<T> list = new List<T>();
            DataTable dataTable = new DataTable();
            command.CommandType = CommandType.StoredProcedure;
            command.Connection.Open();
            try
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                }
                list = (from DataRow dataRow in dataTable.Rows select PopulateRecord<T>(dataRow)).ToList();
            }
            finally
            {
                command.Connection.Close();
            }
            return list;
        }
        public static T GetById<T>(int id, string tableName, SqlConnection connection) where T : BaseEntity, new()
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    command.CommandText = string.Format("SELECT * FROM {0} WHERE Id = {1}", tableName, id);
                    List<T> items = GetRecords<T>(command).ToList();
                    if (items == null || items.Count == 0) throw new RepoException(RepoExceptionType.ItemNotFound);
                    else return items[0];
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static ICollection<T> GetAll<T>(string tableName, SqlConnection connection) where T : BaseEntity, new()
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    command.CommandText = string.Format("SELECT * FROM {0}", tableName);
                    return GetRecords<T>(command).ToList();
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static DataRowCollection GetRows(SqlCommand command)
        {
            DataTable dataTable = new DataTable();
            command.CommandType = CommandType.Text;
            try
            {
                command.Connection.Open();
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
                {
                    dataAdapter.Fill(dataTable);
                    command.Connection.Close();
                }
                return dataTable.Rows;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                command.Connection.Close();
            }
        }
        public static void ExecuteNonQuery(SqlCommand command)
        {
            command.Connection.Open();
            try
            {
                command.ExecuteNonQuery();
            }
            finally
            {
                command.Connection.Close();
            }
        }
        public static int ExecuteScalar(SqlCommand command)
        {
            command.Connection.Open();
            try
            {
                return (int)command.ExecuteScalar();
            }
            finally
            {
                command.Connection.Close();
            }
        }
        public static T Add<T>(T obj, string tableName, SqlConnection connection) where T : BaseEntity, new()
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    Dictionary<string, object> parameters = SqlUtility.GetObjectPropertiesAsParameters(obj);
                    parameters.Remove("Id");
                    string insertParameters = SqlUtility.GetInsertParameters(parameters);
                    command.CommandText = string.Format("INSERT INTO {0} {1} SELECT CAST(SCOPE_IDENTITY() AS int)", tableName, insertParameters);
                    int id = ExecuteScalar(command);
                    return GetById<T>(id, tableName, connection);
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static void Delete<T>(T obj, string tableName, SqlConnection connection) where T : BaseEntity, new()
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    command.CommandText = string.Format("DELETE FROM {0} WHERE Id = {1}", tableName, obj.Id);
                    ExecuteNonQuery(command);
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static T Update<T>(T obj, string tableName, SqlConnection connection) where T : BaseEntity, new()
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    Dictionary<string, object> parameters = SqlUtility.GetObjectPropertiesAsParameters(obj);
                    parameters.Remove("Id");
                    string updateParameters = SqlUtility.GetUpdateParameters(parameters);
                    command.CommandText = string.Format("UPDATE {0} SET {1} WHERE Id = {2}", tableName, updateParameters, obj.Id);
                    ExecuteNonQuery(command);
                    return GetById<T>(obj.Id, tableName, connection);
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static void AddChild<TParent, TChild>(TParent parentObj, TChild childObj, SqlConnection connection)
            where TParent : BaseEntity, new()
            where TChild : BaseEntity, new()
        {
            string parentTableName = typeof(TParent).Name;
            string childTableName = typeof(TChild).Name;
            string parentChildTableName = parentTableName + childTableName;
            using (SqlCommand command = new SqlCommand())
            {
                command.Connection = connection;
                try
                {
                    Dictionary<string, object> parameters = new Dictionary<string, object>();
                    parameters.Add("Id_" + parentTableName, parentObj.Id);
                    parameters.Add("Id_" + childTableName, childObj.Id);
                    string insertParameters = SqlUtility.GetInsertParameters(parameters);
                    command.CommandText = string.Format("INSERT INTO {0} {1}", parentChildTableName, insertParameters);
                    SqlUtility.ExecuteNonQuery(command);
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
        public static ICollection<TChild> GetChilds<TParent, TChild>(TParent parentObj, SqlConnection connection)
            where TParent : BaseEntity, new()
            where TChild : BaseEntity, new()
        {
            try
            {
                List<TChild> list = new List<TChild>();
                string parentTableName = typeof(TParent).Name;
                string childTableName = typeof(TChild).Name;
                string parentChildTableName = parentTableName + childTableName;
                using (SqlCommand command = new SqlCommand())
                {
                    command.CommandText = string.Format("SELECT * FROM {0} WHERE Id_{1} = {2}", parentChildTableName, parentTableName, parentObj.Id);
                    command.Connection = connection;
                    DataRowCollection parentChildRows = SqlUtility.GetRows(command);
                    foreach (DataRow parentChildRow in parentChildRows)
                    {
                        int childId = (int)parentChildRow["Id_" + childTableName];
                        TChild child = SqlUtility.GetById<TChild>(childId, childTableName, connection);
                        list.Add(child);
                    }
                }
                return list;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public static void DeleteChilds<TParent, TChild>(TParent parentObj, SqlConnection connection)
            where TParent : BaseEntity, new()
            where TChild : BaseEntity, new()
        {
            string parentTableName = typeof(TParent).Name;
            string childTableName = typeof(TChild).Name;
            string parentChildTableName = parentTableName + childTableName;
            using (SqlCommand command = new SqlCommand())
            {
                try
                {
                    command.CommandText = string.Format("DELETE FROM {0} WHERE Id_{1} = {2}", parentChildTableName, parentTableName, parentObj.Id);
                    command.Connection = connection;
                    SqlUtility.ExecuteNonQuery(command);
                }
                catch (Exception ex)
                {
                    throw new RepoException(RepoExceptionType.General, ex.Message);
                }
            }
        }
    }
}
