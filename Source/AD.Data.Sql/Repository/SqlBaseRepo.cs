using AD.Core.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AD.Data.Sql.Repository
{
    public abstract class SqlBaseRepo<T> where T : BaseEntity, new()
    {
        private string _connectionString;
        protected static SqlConnection _connection;
        protected string TableName { get; set; }
        public SqlBaseRepo(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new SqlConnection(connectionString);
            TableName = typeof(T).Name;
        }
        public virtual T PopulateRecord(DataRow row)
        {
            return SqlUtility.PopulateRecord<T>(row);
        }
        protected ICollection<T> GetRecords(SqlCommand command)
        {
            command.Connection = _connection;
            return SqlUtility.GetRecords<T>(command);
        }
        protected ICollection<T> ExecuteStoredProcedure(SqlCommand command)
        {
            command.Connection = _connection;
            return SqlUtility.ExecuteStoredProcedure<T>(command);
        }
        protected void ExecuteNonQuery(SqlCommand command)
        {
            command.Connection = _connection;
            SqlUtility.ExecuteNonQuery(command);
        }
        protected int ExecuteScalar(SqlCommand command)
        {
            command.Connection = _connection;
            return SqlUtility.ExecuteScalar(command);
        }
        protected T GetById(int id)
        {
            return SqlUtility.GetById<T>(id, TableName, _connection);
        }
        protected ICollection<T> GetAll()
        {
            return SqlUtility.GetAll<T>(TableName, _connection);
        }
        protected ICollection<T> GetByFilter(Func<T, bool> predicate)
        {
            return GetAll().Where(predicate).ToList();
        }
        protected T Add(T obj)
        {
            return SqlUtility.Add<T>(obj, TableName, _connection);
        }
        protected void Delete(T obj)
        {
            SqlUtility.Delete<T>(obj, TableName, _connection);
        }
        protected T Update(T obj)
        {
            return SqlUtility.Update<T>(obj, TableName, _connection);
        }
    }
}
